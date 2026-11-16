using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.AsyncManagement;
using SkinRecolor.RecoloredSkins;
using SkinRecolor.SkinVFXHooks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SkinRecolor;

public static class SkinVFXManager
{
    public static Dictionary<string, SurvivorHookBase> survivorHooks = new()
    {
        { "AcolyteBody", new Acolyte() },
        { "HuntressBody", new Huntress() },
        { "MercBody", new Merc() },
    };
    
    private static MethodInfo FindSkinVFXBodyToReplacementMethodInfo;
    private static MethodInfo FindSkinVFXEffectUintMethodInfo;
    private static MethodInfo FindEffectIndexesMethodInfo;
    private static MethodInfo FindSetHooksMethodInfo;
    private static FieldInfo findCurrentIdentifier;
    private static FieldInfo FindSkinVFXInfos;
    
    public static bool AddSkinVFXInfo(SkinDef skinDef, GameObject targetObject, GameObject replacementObject)
    {
        SkinVFXInfo skinVFXInfo = new SkinVFXInfo {
            RequiredSkin = skinDef,
            EffectPrefab = targetObject,
            ReplacementEffectPrefab = replacementObject,
        };

        List<SkinVFXInfo> cachedInfos = (List<SkinVFXInfo>)FindSkinVFXInfos.GetValue(null);
        if (cachedInfos == null)
        {
            Log.Error("failed to find skinVFXInfos");
            return false;
        }

        if (findCurrentIdentifier == null)
        {
            Log.Error("failed to find findCurrentIdentifier");
            return false;
        }

        uint currentID = (uint)findCurrentIdentifier.GetValue(null);
        
        skinVFXInfo.Identifier = currentID;
        cachedInfos.Add(skinVFXInfo);

        currentID++;
        findCurrentIdentifier.SetValue(null, currentID);
        FindSkinVFXInfos.SetValue(null, cachedInfos);
        
        Log.Debug($"added replacement on {skinDef.nameToken} for obj {skinVFXInfo.EffectPrefab} - {skinVFXInfo.ReplacementEffectPrefab} \n current id: {currentID}");
          
        return true;
    }

    public static SkinVFXInfo FindSkinVFXInfo(GameObject gameObject, GameObject skinObject)
    {
        return FindSkinVFXBodyToReplacementMethodInfo.Invoke(null, [gameObject, skinObject]) as SkinVFXInfo;
    }
    
    public static SkinVFXInfo FindSkinVFXInfo(uint effectUint)
    {
        return FindSkinVFXBodyToReplacementMethodInfo.Invoke(null, [effectUint]) as SkinVFXInfo;
    }
    
    public static void Hook()
    {
        foreach (var fieldInfo in AccessTools.GetDeclaredFields(typeof(SkinVFX)))
        {
            if (fieldInfo.Name == "currentIdentifier")
            {
                Log.Debug("found currentIdentifier !!");
                findCurrentIdentifier = fieldInfo;
            }
            else if (fieldInfo.Name == "skinVFXInfos")
            {
                Log.Debug("found skinVFXInfos !!");
                FindSkinVFXInfos = fieldInfo;
            }
        }

        //typeof(SkinVFX).GetMethod("FindSkinVFXInfo", BindingFlags.NonPublic | BindingFlags.Static);
        foreach (var methodInfo in AccessTools.GetDeclaredMethods(typeof(SkinVFX)))
        {
            if (methodInfo.Name == "FindSkinVFXInfo")
            {
                if (methodInfo.GetParameters().Length == 1)
                {
                    FindSkinVFXEffectUintMethodInfo = methodInfo;
                }
                else
                {
                    FindSkinVFXBodyToReplacementMethodInfo = methodInfo;
                }
                Log.Debug("found FindSkinVFXInfo");
            } 
            else if (methodInfo.Name == "FindEffectIndexes")
            {
                Log.Debug("found FindEffectIndexes !!");
                FindEffectIndexesMethodInfo = methodInfo;
            }
            else if (methodInfo.Name == "SetHooks")
            {
                Log.Debug("found SetHooks !!");
                FindSetHooksMethodInfo = methodInfo;
            }
        }
        
        Generic.Hook();
        Run.onRunStartGlobal += run =>
        {
            FindEffectIndexesMethodInfo.Invoke(null, []);
        };

        //survivor specific .,., 
        foreach (var skinJson in RecoloredSkinManager.skinJsons)
        {
            if (!survivorHooks.TryGetValue(skinJson.baseSkinBody, out var survHooks) || survHooks.hasPatched) continue;
            
            survHooks.Hooks();
            survHooks.hasPatched = true;
        }
    }

    public static void AddRecoloredMat(string guid, string survName)
    {
        Addressables.LoadAssetAsync<Material>(guid).Completed += handle =>
        {
            foreach (var skinJson in RecoloredSkinManager.skinJsons)
            {
                if (skinJson.baseSkinBody != survName) continue;

                Material recoloredMat = Object.Instantiate(handle.Result);
                Utils.RecolorMaterial(ref recoloredMat, handle.Result, skinJson);
            }
        };
    }
    
    public static void ApplyVFXInfo(GameObject gameObject, ref GameObject skinObject)
    {
        SkinVFXInfo skinInfo = FindSkinVFXInfo(gameObject, skinObject);
        if (skinInfo?.ReplacementEffectPrefab)
            skinObject = skinInfo.ReplacementEffectPrefab;
    }
    
    public static void ApplyVFXInfo(GameObject gameObject, ref Material material)
    {
        if(!gameObject) return;
        
        SkinDef skindef = SkinCatalog.FindCurrentSkinDefForBodyInstance(gameObject);
        RecoloredSkinJson json = Utils.TryGetSkinJson(skindef);
        if (json == null) return;
        
        if (!Utils.recoloredMats.TryGetValue(json, out var recoloredMatDict)) return;
        if(recoloredMatDict.recoloredMats.TryGetValue(material, out var recoloredMat))
            material = recoloredMat;
        else
        {
            foreach (var kvp in recoloredMatDict.recoloredMats)
            {
                if (material.name.StartsWith(kvp.Key.name))
                {
                    material = kvp.Value;
                    break;
                }
            }
        }
    }
    
    public static Material ApplyVFXInfo(GameObject gameObject, Material material)
    {
        if(!gameObject) return null;
        
        SkinDef skindef = SkinCatalog.FindCurrentSkinDefForBodyInstance(gameObject);
        RecoloredSkinJson json = Utils.TryGetSkinJson(skindef);
        if (json == null) return null;
        
        if (!Utils.recoloredMats.TryGetValue(json, out var recoloredMatDict)) return null;
        return recoloredMatDict.recoloredMats.GetValueOrDefault(material);
    }

    private static Dictionary<EntityStateGameObject, AssetReferenceT<GameObject>> esgoRefs = [];
    private static Dictionary<SkinDef, GameObject> skinRefs = [];
    public static void ApplyVFXInfo(GameObject gameObject, ref EntityStateGameObject skinObject)
    {
        SkinVFXInfo skinInfo = FindSkinVFXInfo(gameObject, skinObject);
        SkinDef skinDef = SkinCatalog.FindCurrentSkinDefForBodyInstance(gameObject);
        if (skinInfo?.ReplacementEffectPrefab)
        {
            if (skinObject.addressableReference != null)
            {
                esgoRefs.TryAdd(skinObject, skinObject.addressableReference);
            }
            
            skinObject.SetDirectRef(skinInfo.ReplacementEffectPrefab, true); 
            skinRefs.TryAdd(SkinCatalog.FindCurrentSkinDefForBodyInstance(gameObject), skinInfo.ReplacementEffectPrefab);
        }
        else if (skinObject.addressableReference == null)
        {
            if (esgoRefs.TryGetValue(skinObject, out var addressableReference) &&
                ((skinRefs.TryGetValue(skinDef, out var skinned) && skinned != skinObject.directReference) || !skinRefs.TryGetValue(skinDef, out skinned)))
            {
                skinObject.addressableReference = addressableReference;
                skinObject.directReference = null;
                
                skinInfo = FindSkinVFXInfo(gameObject, skinObject);
                if (skinObject.addressableReference != null)
                {
                    esgoRefs.TryAdd(skinObject, skinObject.addressableReference);
                }
            
                skinObject.SetDirectRef(skinInfo.ReplacementEffectPrefab, true); 
                skinRefs.TryAdd(SkinCatalog.FindCurrentSkinDefForBodyInstance(gameObject), skinInfo.ReplacementEffectPrefab);
            }
            

        }
    }
    
    
}