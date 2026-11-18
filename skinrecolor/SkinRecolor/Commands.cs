using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using Newtonsoft.Json;
using R2API;
using RoR2;
using SkinRecolor.RecoloredSkins;
using SkinRecolor.SkinManager;
using UnityEngine;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace SkinRecolor;

public class Commands
{
    public static void Hook()
    {
        RoR2.Run.onRunStartGlobal += run => { CleanupTempSkin(); };
        RoR2.Run.onRunDestroyGlobal += run => { CleanupTempSkin(); };
    }

    public static void CleanupTempSkin(ModelSkinController skinController = null)
    {
        effectIndex = 0;
        if (vfxJson != null)
        {
            Log.Debug("deleting skin !! ");

            if (skinController)
            {
                HG.ArrayUtils.ArrayRemoveAtAndResize(ref skinController.skins, skinController.skins.Length);
            }
            
            foreach (var ghosts in vfxSkinDef.skinDefParams.projectileGhostReplacements)
            {
                Object.Destroy(ghosts.projectileGhostReplacementPrefab);
            }

            List<SkinVFXInfo> cachedInfos = (List<SkinVFXInfo>)SkinVFXManager.FindSkinVFXInfos.GetValue(null);
            uint currentID = (uint)SkinVFXManager.findCurrentIdentifier.GetValue(null);
            foreach (var skinVFX in cachedInfos.ToArray())
            {
                if (skinVFX.RequiredSkin != vfxSkinDef) continue;
                if (!skinVFX.ReplacementEffectPrefab) continue;
                
                Log.Debug($"ad removing replacement prefab {skinVFX.ReplacementEffectPrefab}");
                
                GameObject prefabDummyStandin2 = new GameObject(skinVFX.ReplacementEffectPrefab.name);
                EffectComponent effectComponent = prefabDummyStandin2.AddComponent<EffectComponent>();
                Object.DontDestroyOnLoad(prefabDummyStandin2);
                prefabDummyStandin2.SetActive(false);
                
                Object.Destroy(skinVFX.ReplacementEffectPrefab);

                var realEffectIndex = EffectCatalog.nameToEffectIndex[skinVFX.ReplacementEffectPrefab.name];
                effectComponent.effectIndex = realEffectIndex;
                EffectDef def = EffectCatalog.GetEffectDef(realEffectIndex);
                Object.Destroy(def._prefabAssetOrDirectReference.directRef);
                def.prefab = prefabDummyStandin2;
                
                currentID--;
                cachedInfos.Remove(skinVFX);
            }
            SkinVFXManager.findCurrentIdentifier.SetValue(null, currentID);
            SkinVFXManager.FindSkinVFXInfos.SetValue(null, cachedInfos);

            CharacterBody characterBody = BodyCatalog.FindBodyPrefab(vfxJson.baseSkinBody).GetComponent<CharacterBody>();
            HG.ArrayUtils.ArrayRemoveAtAndResize(ref SkinCatalog.skinsByBody[(int)characterBody.bodyIndex], SkinCatalog.skinsByBody[(int)characterBody.bodyIndex].Length);
            for (int i = 0; i < SkinCatalog.skinsByBody[(int)characterBody.bodyIndex].Length; i++)
            {
                Log.Debug($"skin aray {i} name {SkinCatalog.skinsByBody[(int)characterBody.bodyIndex][i].name}");
            }
            
            Object.Destroy(vfxSkinDef);
            vfxJson = null;
            vfxSkinDef = null;
        }
    }
    
    public static int originalSkinIndex = -1;
    [ConCommand(commandName = "recolor_skin_new", flags = ConVarFlags.None, helpText = "recolor current skins.,,.")]
    public static void recolorSkin(ConCommandArgs args)
    {
        ModelSkinController skinController = Utils.GetModelLocator(args.GetSenderBody().gameObject);
        CleanupTempSkin(skinController);

        int currentIndex = skinController.currentSkinIndex;
        if(originalSkinIndex == -1)
            originalSkinIndex = currentIndex;
        SkinDef baseSkin = skinController.skins[originalSkinIndex];
        
        float HSVsat = 0;
        float HSVvalue = 0;
        bool multiplySaturation = false;
        if (float.TryParse(args[0], out float HSVhue))
        {
            if (args.Count >= 2)
            {
                HSVsat = float.Parse(args[1]);
            }
        
            if (args.Count >= 3)
            {
                HSVvalue = float.Parse(args[2]);
            }

            if (args.Count >= 4)
            {
                multiplySaturation = bool.Parse(args[3]);
            }
        }
        else
        {
            if (args.Count >= 2)
            {
                HSVhue = float.Parse(args[1]);
            }
        
            if (args.Count >= 3)
            {
                HSVsat = float.Parse(args[2]);
            }
        
            if (args.Count >= 4)
            {
                HSVvalue = float.Parse(args[3]);
            }
            
            if (args.Count >= 5)
            {
                multiplySaturation = bool.Parse(args[4]);
            }
        }

        Log.Debug("base skin = " + baseSkin.name);
        SkinDef newSkin = Object.Instantiate(baseSkin);
        newSkin.name = "generatedSkin";
        RecoloredSkinJson skinJson = new RecoloredSkinJson
        {
            baseSkinName = baseSkin.name,
            baseSkinBody = args.GetSenderMaster().bodyPrefab.name,
            skinName = "generated",
            prefix = "",
            hsv = new ReplacementHSV()
            {
                hue = HSVhue,
                saturation = HSVsat,
                value = HSVvalue,
                multiplySat = multiplySaturation
            },
        };
        vfxJson = skinJson;
        vfxSkinDef = newSkin;

        RecoloredSkinManager.recoloredSkinDefs.Add(newSkin, skinJson);
        RunVFXTasks(args, newSkin, baseSkin, skinJson, skinController, true);
    }
    
    [ConCommand(commandName = "recolor_skin_new_vfx", flags = ConVarFlags.None, helpText = "recolor current skins.,,.")]
    public static void recolorSkinVFXNew(ConCommandArgs args)
    {
        float HSVsat = 0;
        float HSVvalue = 0;
        bool multiplySaturation = false;
        if (float.TryParse(args[0], out float HSVhue))
        {
            if (args.Count >= 2)
            {
                HSVsat = float.Parse(args[1]);
            }
        
            if (args.Count >= 3)
            {
                HSVvalue = float.Parse(args[2]);
            }

            if (args.Count >= 4)
            {
                multiplySaturation = bool.Parse(args[3]);
            }
        }
        else
        {
            if (args.Count >= 2)
            {
                HSVhue = float.Parse(args[1]);
            }
        
            if (args.Count >= 3)
            {
                HSVsat = float.Parse(args[2]);
            }
        
            if (args.Count >= 4)
            {
                HSVvalue = float.Parse(args[3]);
            }
            
            if (args.Count >= 5)
            {
                multiplySaturation = bool.Parse(args[4]);
            }
        }

        if (vfxSkinDef == null)
        {
            Debug.Log("make a skin recolor first !!");
            return;
        }

        vfxJson.projectileGhosts = null;
        vfxJson.vfxOverrides = null;
        vfxJson.hsv = new ReplacementHSV()
        {
            hue = HSVsat,
            saturation = HSVsat,
            value = HSVvalue,
            multiplySat = multiplySaturation
        };

        RunVFXTasks();
        
        async void RunVFXTasks()
        {
            Utils.recoloredMats.Remove(vfxJson);
            
            await GhostsAndVFX.ProjectileGhostReplacementsAndVFXOverrides(vfxJson, true, BodyCatalog.FindBodyPrefab(vfxJson.baseSkinBody), vfxSkinDef);
            Log.Debug("handleded first load !!");
            await SetVFXDummies(vfxJson);
    
            Log.Error("realoding sdfkjsdkf sdf");
        
            SkinVFXManager.FindEffectIndexesMethodInfo.Invoke(null, []);
            //ApplySkin(skinController, vfxJson, args.senderBody, vfxSkinDef);
        }
    }

    private static RecoloredSkinJson vfxJson;
    private static SkinDef vfxSkinDef;
    public static int effectIndex = 0;
    [ConCommand(commandName = "recolor_skin_vfx", flags = ConVarFlags.None, helpText = "recolor current skins.,,.")]
    public static void recolorVfx(ConCommandArgs args)
    {
        ModelSkinController skinController = Utils.GetModelLocator(args.GetSenderBody().gameObject);
        CleanupTempSkin(skinController);

        int currentIndex = skinController.currentSkinIndex;
        if(originalSkinIndex == -1)
            originalSkinIndex = currentIndex;
        SkinDef baseSkin = skinController.skins[originalSkinIndex];
        
        float HSVsat = 0;
        float HSVvalue = 0;
        bool multiplySaturation = false;
        if (float.TryParse(args[0], out float HSVhue))
        {
            if (args.Count >= 2)
            {
                HSVsat = float.Parse(args[1]);
            }
        
            if (args.Count >= 3)
            {
                HSVvalue = float.Parse(args[2]);
            }

            if (args.Count >= 4)
            {
                multiplySaturation = bool.Parse(args[3]);
            }
        }
        else
        {
            if (args.Count >= 2)
            {
                HSVhue = float.Parse(args[1]);
            }
        
            if (args.Count >= 3)
            {
                HSVsat = float.Parse(args[2]);
            }
        
            if (args.Count >= 4)
            {
                HSVvalue = float.Parse(args[3]);
            }
            
            if (args.Count >= 5)
            {
                multiplySaturation = bool.Parse(args[4]);
            }
        }

        SkinDef newSkin = Object.Instantiate(baseSkin);
        newSkin.name = "akslfakjsfasfa";
        RecoloredSkinJson skinJson = new RecoloredSkinJson
        {
            baseSkinName = newSkin.name,
            baseSkinBody = args.GetSenderMaster().bodyPrefab.name,
            skinName = "test" + DateTime.Now,
            prefix = "",
            lightReplacements = null,
            hsv = new ReplacementHSV()
            {
                hue = HSVhue,
                saturation = HSVsat,
                value = HSVvalue,
                multiplySat = multiplySaturation
            },
            replaceSkin = true
        };
        vfxJson = skinJson;
        vfxSkinDef = newSkin;

        RecoloredSkinManager.recoloredSkinDefs.Add(newSkin, skinJson);
        RunVFXTasks(args, newSkin, baseSkin, skinJson, skinController);
    }

    private static async void RunVFXTasks(ConCommandArgs args, SkinDef newSkin, SkinDef baseSkin, RecoloredSkinJson skinJson, ModelSkinController skinController, bool recolor = false)
    {
        if(recolor)
            await RecoloredSkinManager.skinRecolor(skinJson, newSkin, true);
        else
            await Materials.LoadSkinDefParams(newSkin, baseSkin);
        
        await GhostsAndVFX.ProjectileGhostReplacementsAndVFXOverrides(skinJson, true, BodyCatalog.FindBodyPrefab(skinJson.baseSkinBody), newSkin);
        Log.Debug("handleded first load !!");
        await SetVFXDummies(skinJson);
    
        Log.Error("realoding sdfkjsdkf sdf");
        
        SkinVFXManager.FindEffectIndexesMethodInfo.Invoke(null, []);
        ApplySkin(skinController, skinJson, args.senderBody, newSkin);
    }

    private static async Task SetVFXDummies(RecoloredSkinJson recoloredSkinJson)
    {
        foreach (var jsonGhost in recoloredSkinJson.projectileGhosts)
        {
            await RecursiveFindVFXInSubPrefabsDummy(recoloredSkinJson, jsonGhost, []);
        }
        foreach (var jsonGhost in recoloredSkinJson.vfxOverrides)
        {
            await RecursiveFindVFXInSubPrefabsDummy(recoloredSkinJson, jsonGhost, []);
        }
    }

    private static async Task RecursiveFindVFXInSubPrefabsDummy(RecoloredSkinJson recoloredSkinJson, JsonBase jsonBase, List<SubPrefab> subPrefabPath)
    {
        if (jsonBase is VFXOverrideJson vfxJson)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("dummystandineffect");
            sb.Append(effectIndex);
            effectIndex++;
            
            Log.Debug("tryring to adding fefefc., .,");
            if(vfxJson.specificHSV == null)
                vfxJson.specificHSV = recoloredSkinJson.hsv;
            await RecoloredSkinContent.CreateVFX(recoloredSkinJson, subPrefabPath, vfxJson, sb.ToString(), true);
        }
        
        foreach (var subPrefab in jsonBase.subPrefabs)
        {
            subPrefabPath.Add(subPrefab);
            if(subPrefab.projectileGhostJson != null)
                await RecursiveFindVFXInSubPrefabsDummy(recoloredSkinJson, subPrefab.projectileGhostJson, subPrefabPath);
            else if(subPrefab.vfxOverrideJson != null)
                await RecursiveFindVFXInSubPrefabsDummy(recoloredSkinJson, subPrefab.vfxOverrideJson, subPrefabPath);
        }
    }
    
    private static async void ApplySkin(ModelSkinController skinController, RecoloredSkinJson skinJson, CharacterBody characterBody, SkinDef existingSkin = null)
    {
        try
        {
            int indexPos = skinController.skins.Length;
            
            if (!existingSkin)
            {
                existingSkin = await RecoloredSkinManager.skinRecolor(skinJson, null, true);
            }

            existingSkin.BakeAsync();
            Log.Debug("boke");
            
            HG.ArrayUtils.ArrayAppend(ref skinController.skins, existingSkin);
            HG.ArrayUtils.ArrayAppend(ref SkinCatalog.allSkinDefs, existingSkin);
            HG.ArrayUtils.ArrayAppend(ref SkinCatalog.skinsByBody[(int)characterBody.bodyIndex], existingSkin);
            
            skinController.currentSkinIndex = indexPos;
            characterBody.skinIndex = (uint)(indexPos);
        
#pragma warning disable CS0618 // Type or member is obsolete
            skinController.ApplySkin(skinController.currentSkinIndex);
#pragma warning restore CS0618 // Type or member is obsolete
            
            RoR2.Console.instance.RunCmd(LocalUserManager.GetFirstLocalUser(), "apply_skin", [(indexPos).ToString()]);
        }
        catch (Exception e)
        {
            Log.Error($"error while setting skin {e}");
        }
    }

    [ConCommand(commandName = "recolor_skin_clear", flags = ConVarFlags.None, helpText = "recolor current skins.,,.")]
    public static void ClearSkin(ConCommandArgs args)
    {
        originalSkinIndex = -1;
    }
    
    [ConCommand(commandName = "recolor_skin_create", flags = ConVarFlags.None, helpText = "recolor current skins.,,.")]
    public static void createRecolorSkin(ConCommandArgs args)
    {
        string skinName = args.TryGetArgString(0);
        if (skinName != null)
        {
            if (vfxJson != null)
            {
                bool? replaceSkin = args.TryGetArgBool(1);
                if (replaceSkin == true)
                {
                    vfxJson.replaceSkin = true;
                    vfxJson.skinName = Language.GetStringFormatted(SkinCatalog.allSkinDefs.FirstOrDefault(def => def.name == vfxJson.baseSkinName).nameToken, Language.english);
                }
                
                bool? dontReplaceMats = args.TryGetArgBool(2);
                if (dontReplaceMats == true)
                {
                    vfxJson.matReplacements = [];
                }
                
                bool? dontReplaceLights = args.TryGetArgBool(3);
                if (dontReplaceLights == true)
                {
                    vfxJson.lightReplacements = [];
                }
                
                vfxJson.skinName = skinName;
                RecoloredSkinManager.skinJsons.Add(vfxJson);
                File.WriteAllText(RecoloredSkinManager.jsonPath, JsonConvert.SerializeObject(RecoloredSkinManager.skinJsons, Formatting.Indented));
                Debug.Log($"saved skin {skinName} !!! restart the game to see it ,.,.");
            }
            else
            {
                Debug.Log("generate a skin first with recolor_skin_new or recolor_skin_vfx !!");
            }
            
        }
        else
        {
            Debug.Log("please put a name for the skin ,..,");
        }
    }
    
    [ConCommand(commandName = "recolor_vfx_create", flags = ConVarFlags.None, helpText = "recolor current skins.,,.")]
    public static void createRecolorVFX(ConCommandArgs args)
    {
        string fileName = args.TryGetArgString(0);
        if (fileName != null)
        {
            if (vfxJson != null)
            {
                string vfxPath = Path.Combine(Path.GetDirectoryName(Paths.BepInExConfigPath)!, fileName, ".vfxjson");
                if (!Directory.Exists(vfxPath))
                    Directory.CreateDirectory(vfxPath);

                List<RecoloredSkinJson> existing = [];
                //RecoloredSkinManager.skinJsons.Add(vfxJson);
                if (File.Exists(vfxPath))
                {
                    existing = JsonConvert.DeserializeObject<List<RecoloredSkinJson>>(File.ReadAllText(vfxPath));
                }
                existing.Add(vfxJson);

                File.WriteAllText(vfxPath, JsonConvert.SerializeObject(existing, Formatting.Indented));
                Debug.Log($"saved vfx override to {fileName} !!! restart the game to see it ,.,.");
            }
            else
            {
                Debug.Log("generate a skin first with recolor_skin_new or recolor_skin_vfx !!");
            }
        }
        else
        {
            Debug.Log("please put a name for the .vfxjson ,..,");
        }
    }
}