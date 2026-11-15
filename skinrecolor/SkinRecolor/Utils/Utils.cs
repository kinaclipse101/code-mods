using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using Newtonsoft.Json;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using SkinRecolor.RecoloredSkins;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using Formatting = System.Xml.Formatting;
using Path = System.IO.Path;

namespace SkinRecolor;

public static class Utils
{
    public static void SliderConfig(float min, float max, ConfigEntry<float> config)
    {
        StepSliderConfig stepSliderConfig = new StepSliderConfig
        {
            max = max,
            min = min,
            FormatString = "{0:0}"
        };
        StepSliderOption stepSliderOption = new StepSliderOption(config, stepSliderConfig);
        ModSettingsManager.AddOption(stepSliderOption);
    }

    public static void SliderConfig(int min, int max, ConfigEntry<int> config)
    {
        IntSliderConfig intSliderConfig = new IntSliderConfig
        {
            max = max,
            min = min,
            formatString = "{0:0}"
        };
        IntSliderOption intSliderOption = new IntSliderOption(config, intSliderConfig);
        ModSettingsManager.AddOption(intSliderOption);
    }

    public static void CheckboxConfig(ConfigEntry<bool> config, bool restartRequired = false)
    {
        CheckBoxConfig checkBoxConfig = new CheckBoxConfig();
        checkBoxConfig.restartRequired = restartRequired;
        CheckBoxOption checkBoxOption = new CheckBoxOption(config, checkBoxConfig);
        ModSettingsManager.AddOption(checkBoxOption);
    }

    public static void StringConfig(ConfigEntry<string> config)
    {
        InputFieldConfig inputFieldConfig = new InputFieldConfig();
        StringInputFieldOption stringInputFieldOption = new StringInputFieldOption(config, inputFieldConfig);
        ModSettingsManager.AddOption(stringInputFieldOption);
    }

    public static void KeyboardConfig(ConfigEntry<KeyboardShortcut> config)
    {
        KeyBindConfig keyBindConfig = new KeyBindConfig();
        KeyBindOption keyBindOption = new KeyBindOption(config, keyBindConfig);
        ModSettingsManager.AddOption(keyBindOption);
    }

    public static Color RecolorColor(RecoloredSkinJson recoloredSkinJson, Color color, bool useValue = true)
    {
        UnityEngine.Color.RGBToHSV(color, out float h, out float s, out float v);
        
        h = (h + recoloredSkinJson.hue / 360f) % 1f;
        if (h < 0f) h += 1f;
        if(useValue)
            v += recoloredSkinJson.value/100f;
        if (recoloredSkinJson.multiplySat)
            s *= recoloredSkinJson.saturation;
        else
            s += recoloredSkinJson.saturation/100f;

        Color newColor = UnityEngine.Color.HSVToRGB(h, s, v);
        newColor.a = color.a;
        return newColor;
    }

    public static RecoloredSkinJson TryGetSkinJson(SkinDef skinDef)
    {
        if (skinDef && RecoloredSkinManager.recoloredSkinDefs.TryGetValue(skinDef, out RecoloredSkinJson recoloredSkinJson))
        {
            return recoloredSkinJson;
        }
        
        return null;
    }

    public class SkinJsonMats(RecoloredSkinJson skinJson)
    {
        public Dictionary<Material, Material> recoloredMats = [];
        public RecoloredSkinJson skinJson = skinJson;
    }
    
    public static Dictionary<RecoloredSkinJson, SkinJsonMats> recoloredMats = [];
    public static void RecolorMaterial(ref Material recoloredMat, Material origMat, RecoloredSkinJson skinJson, bool dontAdd = false, bool useValue = true)
    {
        try
        {
            if (!origMat)
            {
                Log.Error("orig mat was null !!");
            }

            if (!recoloredMats.TryGetValue(skinJson, out var jsonMats))
            {
                jsonMats = new SkinJsonMats(skinJson);
                recoloredMats.Add(skinJson, jsonMats);
            }
            
            if (origMat && jsonMats.recoloredMats.ContainsKey(origMat))
            {
                jsonMats.recoloredMats.TryGetValue(origMat, out recoloredMat);
                Log.Debug($"getting cached mat {recoloredMat}");
                return;
            }
            
            Log.Debug($"recoloring mat {origMat.name} on skinjson {skinJson.baseSkinBody}");
            
            if (recoloredMat.HasTexture(MainTex) && recoloredMat.GetTexture(MainTex))
                RecolorTexture(recoloredMat, MainTex, skinJson, dontAdd, useValue);
        
            if (recoloredMat.HasTexture(EmTex) && recoloredMat.GetTexture(EmTex))
                RecolorTexture(recoloredMat, EmTex, skinJson, dontAdd, useValue);
        
            if (recoloredMat.HasTexture(RemapTex) && recoloredMat.GetTexture(RemapTex))
                RecolorTexture(recoloredMat, RemapTex, skinJson, dontAdd, useValue);
        
            if (recoloredMat.HasTexture(FresnelRamp) && recoloredMat.GetTexture(FresnelRamp))
                RecolorTexture(recoloredMat, FresnelRamp, skinJson, dontAdd, useValue);
            
            if (recoloredMat.HasTexture(MatCapTexture) && recoloredMat.GetTexture(MatCapTexture))
                RecolorTexture(recoloredMat, MatCapTexture, skinJson, dontAdd, useValue);
            if (recoloredMat.HasTexture(PrimaryMistTexture) && recoloredMat.GetTexture(PrimaryMistTexture))
                RecolorTexture(recoloredMat, PrimaryMistTexture, skinJson, dontAdd, useValue);
            if (recoloredMat.HasTexture(SecondaryMistTexture) && recoloredMat.GetTexture(SecondaryMistTexture))
                RecolorTexture(recoloredMat, SecondaryMistTexture, skinJson, dontAdd, useValue);
            if (recoloredMat.HasTexture(CausticsTextureA) && recoloredMat.GetTexture(CausticsTextureA))
                RecolorTexture(recoloredMat, CausticsTextureA, skinJson, dontAdd, useValue);
            if (recoloredMat.HasTexture(CausticsTextureB) && recoloredMat.GetTexture(CausticsTextureB))
                RecolorTexture(recoloredMat, CausticsTextureB, skinJson, dontAdd, useValue);
            if (recoloredMat.HasTexture(PrintRamp) && recoloredMat.GetTexture(PrintRamp))
                RecolorTexture(recoloredMat, PrintRamp, skinJson, dontAdd, useValue);
            
            TryRecolorColor(recoloredMat, Color, skinJson, useValue);
            TryRecolorColor(recoloredMat, EmColor, skinJson, useValue);
            TryRecolorColor(recoloredMat, TintColor, skinJson, useValue);
            TryRecolorColor(recoloredMat, WireframeColor, skinJson, useValue);
            TryRecolorColor(recoloredMat, VertColor, skinJson, useValue);
            TryRecolorColor(recoloredMat, MainColor, skinJson, useValue);
            TryRecolorColor(recoloredMat, GlowColor, skinJson, useValue);
            TryRecolorColor(recoloredMat, MatCapColor, skinJson, useValue);
            TryRecolorColor(recoloredMat, PrimaryColor, skinJson, useValue);
            TryRecolorColor(recoloredMat, SecondaryColor, skinJson, useValue);
            TryRecolorColor(recoloredMat, CausticsColor, skinJson, useValue);
            TryRecolorColor(recoloredMat, FresnelColor, skinJson, useValue);
            TryRecolorColor(recoloredMat, InnerGlowColor, skinJson, useValue);
            TryRecolorColor(recoloredMat, FlashColor, skinJson, useValue);
            
            jsonMats.recoloredMats.Add(origMat, recoloredMat);
        }
        catch (Exception e)
        {
            Log.Error("error while recoloring recoloredMat !!");
            Log.Error(e);
        }
    }

    private static async Task RecolorTexture(Material mat, int nameID, RecoloredSkinJson skinJson, bool dontAdd, bool useValue = true)
    {
        mat.SetTexture(nameID, await hsvModifyTexture(mat.GetTexture(nameID) as Texture2D, skinJson, dontAdd, useValue));
    }
    
    private static void TryRecolorColor(Material mat, int nameID, RecoloredSkinJson recoloredSkinJson, bool useValue = true)
    {
        if (!mat.HasColor(nameID)) return;
        mat.SetColor(nameID, RecolorColor(recoloredSkinJson, mat.GetColor(nameID), useValue));
    }

    public static string bwTexturesSaved = Path.Combine(Path.GetDirectoryName(Paths.BepInExConfigPath)!, "skintextures", "bwTextures.json");
    public static List<string> bwTextures = [];
    public static async Task<Texture2D> hsvModifyTexture(Texture2D texture, RecoloredSkinJson skinJson, bool dontExport = false, bool useValue = true)
    {
        string textureFileName = $"{texture.name}_RecolorH{skinJson.hue}S{skinJson.saturation}V{skinJson.value}_{skinJson.baseSkinBody}.png";
        string texturePath = Path.Combine(RecoloredSkinManager.textureDirs, textureFileName);
        Texture2D returnTexture;
        
        if (bwTextures.Count == 0 && File.Exists(bwTexturesSaved))
        {
            bwTextures = JsonConvert.DeserializeObject<List<string>>(await File.ReadAllTextAsync(bwTexturesSaved));
        }

        if (bwTextures.Contains(texture.name))
        {
            textureFileName = $"{texture.name}_Recolor_BW.png";
            texturePath = Path.Combine(RecoloredSkinManager.textureDirs, textureFileName);
        }

        if (File.Exists(texturePath))
        {
            if (!RecoloredSkinManager.loadedSkinTextures.TryGetValue(texturePath, out returnTexture))
            {
                Log.Warning($"{textureFileName} wasnt in cache");    
                byte[] bytes = await File.ReadAllBytesAsync(texturePath);
                returnTexture = new Texture2D(2, 2);
                returnTexture.LoadImage(bytes);
                RecoloredSkinManager.loadedSkinTextures.TryAdd(texturePath, returnTexture);
            }
        }
        else
        {
            //create recolored texture if it doesnt exist .,,.
            returnTexture = makeReadable(texture);
            Color[] texPixels = returnTexture.GetPixels(0, 0, returnTexture.width, returnTexture.height);

            bool isBW = true;
            for (int i = 0; i < texPixels.Length; i++)
            {
                if (isBW)
                {
                    if (Math.Abs(texPixels[i].r - texPixels[i].g) > 0.1 || Math.Abs(texPixels[i].r - texPixels[i].b) > 0.1 || Math.Abs(texPixels[i].b - texPixels[i].g) > 0.1)
                    {
                        isBW = false;
                    }
                }
                
                Color pixelColor = texPixels[i];
                Color newColor = RecolorColor(skinJson, pixelColor, useValue);
                newColor.a = pixelColor.a;
                texPixels[i] = newColor;
            }
    
            if(isBW)
                returnTexture.SetPixels(returnTexture.GetPixels(0, 0, returnTexture.width, returnTexture.height));
            else
                returnTexture.SetPixels(texPixels);
            
            returnTexture.Apply();

            if (!dontExport)
            {
                if (isBW)
                {
                    textureFileName = $"{texture.name}_Recolor_BW.png";
                    texturePath = Path.Combine(RecoloredSkinManager.textureDirs, textureFileName);
                    bwTextures.Add(texture.name);
                    Log.Debug($"{textureFileName} bw texture was created");
                    await File.WriteAllTextAsync(bwTexturesSaved, JsonConvert.SerializeObject(bwTextures, (Newtonsoft.Json.Formatting)Formatting.Indented));
                }
                
                Log.Debug($"created return texture !!! {texture.name} {dontExport}"); 
                await File.WriteAllBytesAsync(texturePath, returnTexture.EncodeToPNG());
            }
        }
        
        returnTexture.name = textureFileName;
        returnTexture.anisoLevel = texture.anisoLevel;
        returnTexture.filterMode = texture.filterMode;
        returnTexture.wrapMode = texture.wrapMode;
        returnTexture.wrapModeU = texture.wrapModeU;
        returnTexture.wrapModeV = texture.wrapModeV;
        returnTexture.wrapModeW = texture.wrapModeW;
        
        return returnTexture;
    }
    
    //unity is a fuck ,.. readable textur ebypass .,,.
    public static Texture2D makeReadable(Texture texture)
    {
        var tmp = RenderTexture.GetTemporary(texture.width, texture.height, 32);
        tmp.name = "Whatever";
        tmp.enableRandomWrite = true;
        tmp.Create();
            
        // Create a temporary RenderTexture of the same size as the texture
        // RenderTexture tmp = RenderTexture.GetTemporary(
        //     texture.width,
        //     texture.height,
        //     0,
        //     RenderTextureFormat.Default,
        //     RenderTextureReadWrite.Linear);

        // Blit the pixels on texture to the RenderTexture
        UnityEngine.Graphics.Blit(texture, tmp);
        // Backup the currently set RenderTexture
        RenderTexture previous = RenderTexture.active;
        // Set the current RenderTexture to the temporary one we created
        RenderTexture.active = tmp;
        // Create a new readable Texture2D to copy the pixels to it
        Texture2D myTexture2D = new Texture2D(texture.width, texture.height);
        // Copy the pixels from the RenderTexture to the new Texture
        myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        myTexture2D.Apply();
        // Reset the active RenderTexture
        RenderTexture.active = previous;
        // Release the temporary RenderTexture
        RenderTexture.ReleaseTemporary(tmp);

        return myTexture2D;
    }

    public static StringBuilder GetTransformPath(GameObject body, GameObject child)
    {
        StringBuilder transformPath = new StringBuilder();
        transformPath.Append(child.transform.name);
        Transform pathFinder = child.transform.parent; // hehe ./.p athfinder ,.., 
                    
        while (pathFinder != body.transform)
        {
            if (!pathFinder) break;
            
            transformPath.Insert(0, "/");
            transformPath.Insert(0, pathFinder.name);
            pathFinder = pathFinder.parent;
        }
                    
        return transformPath;
    }

    public static ModelSkinController GetModelLocator(GameObject characterbody)
    {
        //stolen from keb skin builder script ,.,.
        if (!characterbody)
        {
            Log.Warning($"failed to get model locator from null ,.,.");
            return null;
        }

        var modelLocator = characterbody.GetComponent<ModelLocator>();
        if (!modelLocator)
        {
            Log.Warning($"failed to get model skin controller since couldnts find model locator on {characterbody.name},.,,. ");
            return null;
        }

        var mdl = modelLocator.modelTransform.gameObject;
        var skinController = mdl ? mdl.GetComponent<ModelSkinController>() : null;
        if (!skinController)
        {
            Log.Warning($"failed to get model skin controlelr since couldnts find model skin controller components on {characterbody.name} ,.,..");
            return null;
        }

        return skinController;
    }

    private static Dictionary<string, GameObject> escToObj = [];
    public static async Task<GameObject> TryGetPrefabFromESC(string configType, string fieldName)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(configType);
        builder.Append(fieldName);

        if (escToObj.TryGetValue(builder.ToString(), out GameObject obj))
        {
            Log.Debug("found prefab in cache !!");
            return obj;
        }
        
        if (!RecoloredSkinManager.escDict.TryGetValue(configType, out var targetEsc))
        {
            foreach (var esc in RoR2.ContentManagement.ContentManager.entityStateConfigurations)
            {
                if (esc.targetType.assemblyQualifiedName != configType) continue;
                targetEsc = esc;
                break;
            }
        }
        if (!targetEsc)
        {
            Log.Warning($"couldnt find esc for {configType}");
            return null;
        }

        GameObject prefab = null;
        foreach (var field in targetEsc.serializedFieldsCollection.serializedFields)
        {
            if(field.fieldName != fieldName) continue;
            prefab = field.fieldValue.objectValue as GameObject;
            if (!prefab)
                prefab = await Addressables.LoadAssetAsync<GameObject>(field.fieldValue.stringValue).Task;
                
            break;
        }
        if (!prefab)
        {
            Log.Warning($"couldnt find GameObject for {fieldName}");
            return null;
        }
        
        
        
        escToObj.TryAdd(builder.ToString(), prefab);

        return prefab;
    }
    
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int RemapTex = Shader.PropertyToID("_RemapTex");
    private static readonly int EmTex = Shader.PropertyToID("_EmTex");
    private static readonly int FresnelRamp = Shader.PropertyToID("_FresnelRamp");
    private static readonly int EmColor = Shader.PropertyToID("_EmColor");
    private static readonly int Color = Shader.PropertyToID("_Color");
    private static readonly int TintColor = Shader.PropertyToID("_TintColor");
    private static readonly int WireframeColor = Shader.PropertyToID("WireframeColor");
    private static readonly int VertColor = Shader.PropertyToID("_VertColor");

    private static readonly int MatCapTexture = Shader.PropertyToID("_MatCapTexture");
    private static readonly int PrimaryMistTexture = Shader.PropertyToID("_PrimaryMistTexture");
    private static readonly int SecondaryMistTexture = Shader.PropertyToID("_SecondaryMistTexture");
    private static readonly int SliceAlphaTex = Shader.PropertyToID("_SliceAlphaTex");
    private static readonly int CausticsTextureA = Shader.PropertyToID("_CausticsTextureA");
    private static readonly int CausticsTextureB = Shader.PropertyToID("_CausticsTextureB");
    private static readonly int PrintRamp = Shader.PropertyToID("_PrintRamp");
    
    private static readonly int MainColor = Shader.PropertyToID("_MainColor");
    private static readonly int GlowColor = Shader.PropertyToID("_GlowColor");
    private static readonly int MatCapColor = Shader.PropertyToID("_MatCapColor");
    private static readonly int PrimaryColor = Shader.PropertyToID("_PrimaryColor");
    private static readonly int SecondaryColor = Shader.PropertyToID("_SecondaryColor");
    private static readonly int CausticsColor = Shader.PropertyToID("_CausticsColor");
    private static readonly int FresnelColor = Shader.PropertyToID("_FresnelColor");
    private static readonly int InnerGlowColor = Shader.PropertyToID("_InnerGlowColor");
    private static readonly int FlashColor = Shader.PropertyToID("_FlashColor");

}