using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HG;
using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using R2API;
using RoR2;
using SkinRecolor.SkinManager;
using SkinRecolor.SkinVFXHooks;
using UnityEngine;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace SkinRecolor.RecoloredSkins;

public static class RecoloredSkinManager
{
    public static string jsonPath;
    public static string textureDirs;
    public static List<RecoloredSkinJson> skinJsons = [];
    public static List<RecoloredSkinJson> vfxJsons = [];
    public static Dictionary<string, Texture2D> loadedSkinTextures = [];
    public static Dictionary<string, EntityStateConfiguration> escDict = []; // sorry chinchi i lied ,., easier to implement ,..,
    public static Dictionary<SkinDef, RecoloredSkinJson> recoloredSkinDefs = [];
    public static Dictionary<object, ReplacementHSV> hsvConfigWorkaround = [];
    public static Action finishedSkinRecolors;
    public static bool skinRecolorsFinished;
    
    public static void Start()
    {
        textureDirs = Path.Combine(Path.GetDirectoryName(Paths.BepInExConfigPath)!, "skintextures");
        if (!Directory.Exists(textureDirs))
            Directory.CreateDirectory(textureDirs);

        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            //TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
        };
        
        jsonPath = Path.Combine(Path.GetDirectoryName(Paths.BepInExConfigPath)!, "skinRecolors.json");
        if (File.Exists(jsonPath))
        {
            skinJsons = JsonConvert.DeserializeObject<List<RecoloredSkinJson>>(File.ReadAllText(jsonPath), settings);
        }
        else
        {
            RecoloredSkinJson skinJson = new RecoloredSkinJson
            {
                baseSkinName = "skinCommandoDefault",
                baseSkinBody = "CommandoBody",
                skinName = "test",
                prefix = "",
                lightReplacements = null,
                hsv = new ReplacementHSV()
                {
                    hue = 0,
                    saturation = 0,
                    value = 0,
                    multiplySat = true
                }
            };

            skinJsons.Add(skinJson);
            
            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(skinJsons, Formatting.Indented));
        }

        foreach (string file in Directory.GetFiles(Paths.PluginPath, "*.vfxjson", SearchOption.AllDirectories))
        {
            try
            {
                var vfxOnlyJson = JsonConvert.DeserializeObject<List<RecoloredSkinJson>>(File.ReadAllText(file), settings);
                if (vfxOnlyJson == null) continue;
                
                foreach (var skinJson in vfxOnlyJson)
                {
                    vfxJsons.Add(skinJson);
                }
            }
            catch (Exception e)
            {
                Log.Error($"erorr while parsing skinvfxonly json {file}");
                Log.Error(e);
            }
        }
        
        BodyCatalog.availability.onAvailable += () =>
        {
            foreach (var skinJson in skinJsons)
            {
                GameObject bodyPrefab = BodyCatalog.FindBodyPrefab(skinJson.baseSkinBody);
                if (!bodyPrefab) continue;
                
                StringBuilder sb = new StringBuilder();
                sb.Append(Language.GetStringFormatted(bodyPrefab.GetComponent<CharacterBody>().baseNameToken, Language.english));
                sb.Append(" - ");
                sb.Append(skinJson.skinName);
                skinJson.sectionName = sb.ToString();
            }
            
            foreach (var skinJson in vfxJsons)
            {
                GameObject bodyPrefab = BodyCatalog.FindBodyPrefab(skinJson.baseSkinBody);
                if (!bodyPrefab) continue;
                
                StringBuilder sb = new StringBuilder();
                sb.Append(Language.GetStringFormatted(bodyPrefab.GetComponent<CharacterBody>().baseNameToken, Language.english));
                sb.Append(" - ");
                sb.Append(skinJson.skinName);
                skinJson.sectionName = sb.ToString();
            }
        };

        cacheCount = skinJsons.Count + vfxJsons.Count;
        foreach (var skin in skinJsons)
        {
            TryLoadRelatedTextures(skin);
        }
        foreach (var skin in vfxJsons)
        {
            TryLoadRelatedTextures(skin);
        }

        TryLoadBWTextures();
        
        RoR2.SkinCatalog.availability.onAvailable += AvailabilityOnonAvailable;
        RoR2.EntityStateCatalog.availability.onAvailable += CreateESCDict;

        new RecoloredSkinContent();
    }

    private static async void TryLoadBWTextures()
    {
        try
        {
            string [] fileEntries = Directory.GetFiles(textureDirs, "*_BW.png", SearchOption.AllDirectories);
            foreach (string fileName in fileEntries)
            {
                byte[] bytes = await File.ReadAllBytesAsync(fileName);
                Texture2D returnTexture = new Texture2D(2, 2);
                returnTexture.LoadImage(bytes);
                loadedSkinTextures.TryAdd(fileName, returnTexture);
            }
        }
        catch (Exception e)
        {
            Log.Error("error while loading bw texture !!!");
            Log.Error(e);
        }
    }

    private static int cacheCount;

    private static void CreateESCDict()
    {
        foreach (var esc in RoR2.ContentManagement.ContentManager.entityStateConfigurations)
        {
            string target = esc.targetType.assemblyQualifiedName;
            foreach (var skinJson in skinJsons)
            {
                if(skinJson.projectileGhosts != null)
                    foreach (var ghostJson in skinJson.projectileGhosts)
                    {
                        if (target != null && target == ghostJson.configurationType)
                        {
                            escDict.TryAdd(target, esc);
                        }
                    }
                if(skinJson.vfxOverrides != null)
                    foreach (var vfxOverrideJson in skinJson.vfxOverrides)
                    {
                        if (target != null && target == vfxOverrideJson.configurationType)
                        {
                            escDict.TryAdd(target, esc);
                        }
                    }
            }
            
            foreach (var skinJson in vfxJsons)
            {
                if(skinJson.projectileGhosts != null)
                    foreach (var ghostJson in skinJson.projectileGhosts)
                    {
                        if (target != null && target == ghostJson.configurationType)
                        {
                            escDict.TryAdd(target, esc);
                        }
                    }
                if(skinJson.vfxOverrides != null)
                    foreach (var vfxOverrideJson in skinJson.vfxOverrides)
                    {
                        if (target != null && target == vfxOverrideJson.configurationType)
                        {
                            escDict.TryAdd(target, esc);
                        }
                    }
            }
        }
    }

    public static async void TryLoadRelatedTextures(RecoloredSkinJson skinJson)
    {
        try
        {
            Log.Debug($"trying to precache {skinJson.skinName}");
            string endsWith = $"H{skinJson.hsv.hue}S{skinJson.hsv.saturation}V{skinJson.hsv.value}_{skinJson.baseSkinBody}.png";
            string [] fileEntries = Directory.GetFiles(textureDirs, "*.*", SearchOption.AllDirectories);
            foreach (string fileName in fileEntries)
            {
                if (!fileName.EndsWith(endsWith)) continue;
            
                byte[] bytes = await File.ReadAllBytesAsync(fileName);
                Texture2D returnTexture = new Texture2D(2, 2);
                returnTexture.LoadImage(bytes);
                loadedSkinTextures.TryAdd(fileName, returnTexture);
                //Log.Debug($"added texture to loaded skin {fileName}");
            }
        }
        catch (Exception e)
        {
            Log.Error("error while loading textures related to a skin");
            Log.Error(e);
        }

        cacheCount--;
        if (cacheCount == 0)
        {
            Log.Message("finished caching all textures !!");
        }
    }

    private static int skinCount;
    private static void AvailabilityOnonAvailable()
    {
        skinCount = skinJsons.Count + vfxJsons.Count;
        foreach (RecoloredSkinJson recoloredSkinJson in skinJsons)
        {
            skinRecolor(recoloredSkinJson);
        }
        
        foreach (RecoloredSkinJson recoloredSkinJson in vfxJsons)
        {
            skinRecolor(recoloredSkinJson);
        }
    }

    private static List<SkinDef> replacedSkins = [];
    
    public static async Task<SkinDef> skinRecolor(RecoloredSkinJson recoloredSkinJson, SkinDef skinDef = null, bool dontAdd = false, GameObject body = null)
    {
        try
        {
            //load model skin controller and get original skin ,..
            if (body == null)
            {
                body = BodyCatalog.FindBodyPrefab(recoloredSkinJson.baseSkinBody);
            }
            ModelSkinController skinController = Utils.GetModelLocator(body);
            if (!skinController)
            {
                Log.Warning("couldnt find modelskincontroller on " + recoloredSkinJson.baseSkinBody);
                return null;
            }
            
            SkinDef originalSkin = null;
            foreach (var iterateSkinDef in skinController.skins)
            {
                Log.Debug(iterateSkinDef.name);
                if (iterateSkinDef.name != recoloredSkinJson.baseSkinName) continue;
                originalSkin = iterateSkinDef;
                break;
            }
            if (!originalSkin)
            {
                Log.Warning("couldnt find originalSkin on " + recoloredSkinJson.baseSkinBody);
                return null;
            }

            if (recoloredSkinJson.replaceSkin)
            {
                if (!replacedSkins.Contains(originalSkin))
                {
                    replacedSkins.Add(originalSkin);
                }
                else
                {
                    Log.Error("skin was already replaced !!! ummm .,,. exiting !!");
                    return null;
                }
            }

            SkinDef recoloredSkinDef;
            if (recoloredSkinJson.replaceSkin)
            {
                recoloredSkinDef = Object.Instantiate(originalSkin);  //originalSkin;
            }
            else
            {
                if (skinDef != null)
                {
                    recoloredSkinDef = skinDef;
                }
                else
                {
                    recoloredSkinDef = Object.Instantiate(originalSkin);
                }
            }
            
            await Materials.LoadSkinDefParams(recoloredSkinDef, originalSkin);
            //await SkinManager.Icons.IconReplacement(recoloredSkinJson, dontAdd, recoloredSkinDef);
            Log.Debug("abc 2");
            await SkinManager.Lights.LightReplacements(recoloredSkinJson, recoloredSkinDef, body, dontAdd);
            
            await SkinManager.Materials.MeshMatReplacements(recoloredSkinJson, dontAdd, recoloredSkinDef, originalSkin, body);
            
            Log.Debug("abc 3");
            await GhostsAndVFX.ProjectileGhostReplacementsAndVFXOverrides(recoloredSkinJson, dontAdd, body, recoloredSkinDef);
            
            string internalName = recoloredSkinJson.skinName.Replace(" ", "");
            recoloredSkinDef.name = recoloredSkinDef.name.Replace("(Clone)", "");
            recoloredSkinDef.name += $"Recolored_{internalName}";
            recoloredSkinDef.name = recoloredSkinJson.prefix + recoloredSkinDef.name; // if someone wants ot add like Red or something to check for wolfo ,.,.
            recoloredSkinDef.nameToken += $"_SR_{internalName.ToUpper()}";
            LanguageAPI.Add(recoloredSkinDef.nameToken, recoloredSkinJson.skinName);

            if (!dontAdd)
            {
                if (recoloredSkinJson.replaceSkin)
                {
                    Log.Debug($"replacing skin {originalSkin.name}");
                    recoloredSkinDef.BakeAsync();
                    if (SurvivorCatalog.availability.available)
                    {
                        ModelSkinController displaySkins = SurvivorCatalog.FindSurvivorDefFromBody(body)?.displayPrefab?.transform.GetChild(0)?.GetComponent<ModelSkinController>();
                        if (displaySkins) 
                            displaySkins.skins[displaySkins.skins.IndexOf(def => def == originalSkin)] = recoloredSkinDef;
                    }
                    else
                    {
                        SurvivorCatalog.availability.onAvailable += () =>
                        {
                            ModelSkinController displaySkins = SurvivorCatalog.FindSurvivorDefFromBody(body)?.displayPrefab?.transform.GetChild(0)?.GetComponent<ModelSkinController>();
                            if (displaySkins) 
                                displaySkins.skins[displaySkins.skins.IndexOf(def => def == originalSkin)] = recoloredSkinDef;
                        };
                    }
                    skinController.skins[skinController.skins.IndexOf(def => def == originalSkin)] = recoloredSkinDef;
                    SkinCatalog.allSkinDefs[SkinCatalog.allSkinDefs.IndexOf(def => def == originalSkin)] = recoloredSkinDef;
                    SkinCatalog.skinsByBody[(int)BodyCatalog.nameToIndexMap[recoloredSkinJson.baseSkinBody]][SkinCatalog.skinsByBody[(int)BodyCatalog.nameToIndexMap[recoloredSkinJson.baseSkinBody]].IndexOf(def => def == originalSkin)] = recoloredSkinDef;
                }
                else
                {
                    HG.ArrayUtils.ArrayAppend(ref skinController.skins, recoloredSkinDef);
                    HG.ArrayUtils.ArrayAppend(ref SkinCatalog.allSkinDefs, recoloredSkinDef);
                    HG.ArrayUtils.ArrayAppend(ref SkinCatalog.skinsByBody[(int)BodyCatalog.nameToIndexMap[recoloredSkinJson.baseSkinBody]], recoloredSkinDef);
                }
            }
            
            Log.Debug($"added {recoloredSkinJson.skinName} to {recoloredSkinJson.baseSkinBody} !!!!");
            recoloredSkinDefs.TryAdd(recoloredSkinDef, recoloredSkinJson);
            
            skinCount--;
            if (skinCount == 0)
            {
                File.WriteAllTextAsync(jsonPath, JsonConvert.SerializeObject(skinJsons, Formatting.Indented));
                Log.Error("saved to disk !!");
                skinRecolorsFinished = true;
                finishedSkinRecolors?.Invoke();
            }
            
            return recoloredSkinDef;
        }
        catch (Exception e)
        {
            Log.Warning($"faileds to add {recoloredSkinJson.skinName} skin to {recoloredSkinJson.baseSkinBody} ,.,.,.");
            Log.Error(e);
        }

        skinCount--;
        if (skinCount == 0)
        {
            File.WriteAllTextAsync(jsonPath, JsonConvert.SerializeObject(skinJsons, Formatting.Indented));
            Log.Error("saved to disk !!");
            skinRecolorsFinished = true;
            finishedSkinRecolors?.Invoke();
        }

        return null;
    }
}