using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;
using R2API;
using RoR2;
using UnityEngine;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace SkinRecolor.RecoloredSkins;

public static class RecoloredSkinManager
{
    public static string jsonPath;
    public static string textureDirs;
    public static List<RecoloredSkinJson> skinJsons = [];
    public static List<RecoloredSkin> completeSkins = [];
    public static List<SkinDef> skinDefs = [];
    
    public static void Start()
    {
        textureDirs = Path.Combine(Path.GetDirectoryName(Paths.BepInExConfigPath)!, "skintextures");
        if (!Directory.Exists(textureDirs))
            Directory.CreateDirectory(textureDirs);

        jsonPath = Path.Combine(Path.GetDirectoryName(Paths.BepInExConfigPath)!, "skinRecolors.json");
        if (File.Exists(jsonPath))
            skinJsons = JsonConvert.DeserializeObject<List<RecoloredSkinJson>>(File.ReadAllText(jsonPath));
        else
        {
            RecoloredSkinJson skinJson = new RecoloredSkinJson
            {
                baseSkinName = "skinCommandoDefault",
                baseSkinBody = "CommandoBody",
                skinName = "test",
                prefix = "",
                lightReplacements = null,
                hue = 0,
                saturation = 0,
                value = 0,
                multiplySat = true
            };

            skinJsons.Add(skinJson);
            
            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(skinJsons, Formatting.Indented));
        }

        for(int i = 0 ; i < skinJsons.Count; i++)
        {
            TryLoadRelatedTextures(i);
        }

        // foreach (var skin in skinJsons)
        // {
        //      skin.projectileGhosts = null;
        //      skin.lightReplacements = null;
        //      skin.vfxOverrides = null;
        // }
        
        RoR2.SkinCatalog.availability.onAvailable += AvailabilityOnonAvailable;
    }
    
    public static Dictionary<string, Texture2D> loadedSkinTextures = [];

    public static async void TryLoadRelatedTextures(int arrayIndex)
    {
        try
        {
            if (arrayIndex >= skinJsons.Count)
                return;
            var skinJson = skinJsons[arrayIndex];
            Log.Debug($"trying to precache {skinJson.skinName}");
            string endsWith = $"H{skinJson.hue}S{skinJson.saturation}V{skinJson.value}_{skinJson.baseSkinBody}.png";
            string [] fileEntries = Directory.GetFiles(textureDirs, "*.*", SearchOption.AllDirectories);
            foreach (string fileName in fileEntries)
            {
                if (!fileName.EndsWith(endsWith)) continue;
            
                byte[] bytes = await File.ReadAllBytesAsync(fileName);
                Texture2D returnTexture = new Texture2D(2, 2);
                returnTexture.LoadImage(bytes);
                loadedSkinTextures.Add(fileName, returnTexture);
                Log.Debug($"added texture to loaded skin {fileName}");
            }
        }
        catch (Exception e)
        {
            Log.Error("error while loading textures related to a skin");
            Log.Error(e);
        }
    }

    private static void AvailabilityOnonAvailable()
    {
        foreach (RecoloredSkinJson recoloredSkinJson in skinJsons)
        {
            skinRecolor(recoloredSkinJson);
        }
    }
    
    public static async void skinRecolor(RecoloredSkinJson recoloredSkinJson, bool dontAdd = false)
    {
        try
        {
            //load model skin controller and get original skin ,..
            GameObject body = BodyCatalog.FindBodyPrefab(recoloredSkinJson.baseSkinBody);
            ModelSkinController skinController = Utils.GetModelLocator(body);
            if (!skinController)
            {
                Log.Warning("couldnt find modelskincontroller on " + recoloredSkinJson.baseSkinBody);
                return;
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
                return;
            }
            
            SkinDef recoloredSkinDef = Object.Instantiate(originalSkin);
            
            //await SkinManager.Icons.IconReplacement(recoloredSkinJson, dontAdd, recoloredSkinDef);

            await SkinManager.Materials.MeshMatReplacements(recoloredSkinJson, dontAdd, recoloredSkinDef, originalSkin);
            
            await SkinManager.Lights.LightReplacements(recoloredSkinJson, recoloredSkinDef, body);
            
            await SkinManager.Ghosts.ProjectileGhostReplacementsAndVFXOverrides(recoloredSkinJson, dontAdd, body, recoloredSkinDef);
            
            string internalName = recoloredSkinJson.skinName.Replace(" ", "");
            recoloredSkinDef.name = recoloredSkinDef.name.Replace("(Clone)", "");
            recoloredSkinDef.name += $"Recolored_{internalName}";
            recoloredSkinDef.name = recoloredSkinJson.prefix + recoloredSkinDef.name; // if someone wants ot add like Red or something to check for wolfo ,.,.
            recoloredSkinDef.nameToken += $"_SR_{internalName.ToUpper()}";
            LanguageAPI.Add(recoloredSkinDef.nameToken, recoloredSkinJson.skinName);

            if (!dontAdd)
            {
                HG.ArrayUtils.ArrayAppend(ref SkinCatalog.allSkinDefs, recoloredSkinDef);
                HG.ArrayUtils.ArrayAppend(ref skinController.skins, recoloredSkinDef);
                HG.ArrayUtils.ArrayAppend(ref SkinCatalog.skinsByBody[(int)BodyCatalog.nameToIndexMap[recoloredSkinJson.baseSkinBody]], recoloredSkinDef);
                
                Log.Debug($"added {recoloredSkinJson.skinName} to {recoloredSkinJson.baseSkinBody} !!!!");
                skinDefs.Add(recoloredSkinDef);
                RecoloredSkin recoloredSkin = new RecoloredSkin
                {
                    skin = recoloredSkinDef,
                    jsonSkin = recoloredSkinJson
                };
                completeSkins.Add(recoloredSkin);
            }
            
            foreach (var jsonOverride in recoloredSkinJson.vfxOverrides)
            {
                SkinManager.VFX.AddVFXOverride(recoloredSkinJson, jsonOverride, recoloredSkinDef);
            }
        }
        catch (Exception e)
        {
            Log.Warning($"faileds to add {recoloredSkinJson.skinName} skin to {recoloredSkinJson.baseSkinBody} ,.,.,.");
            Log.Error(e);
        }
    }
}