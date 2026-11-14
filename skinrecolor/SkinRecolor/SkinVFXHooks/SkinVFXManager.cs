using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using R2API;
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
    };
    
    private static MethodInfo FindSkinVFXBodyToReplacementMethodInfo;
    private static MethodInfo FindSkinVFXEffectUintMethodInfo;

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
        foreach (var methodInfo in AccessTools.GetDeclaredMethods(typeof(SkinVFX)))
        {
            if (methodInfo.Name != "FindSkinVFXInfo") continue;

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
        
        Generic.Hook();

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
    private static List<GameObject> esgoRefsAppliedSkinObj = [];
    
    public static void ApplyVFXInfo(GameObject gameObject, ref EntityStateGameObject skinObject)
    {
        SkinVFXInfo skinInfo = FindSkinVFXInfo(gameObject, skinObject);
        if (skinInfo?.ReplacementEffectPrefab)
        {
            if (skinObject.addressableReference != null)
            {
                esgoRefs.TryAdd(skinObject, skinObject.addressableReference);
            }
            
            skinObject.SetDirectRef(skinInfo.ReplacementEffectPrefab, true); 
            esgoRefsAppliedSkinObj.Add(skinInfo.ReplacementEffectPrefab);
        }
        else if (skinObject.addressableReference == null && !esgoRefsAppliedSkinObj.Contains(skinObject.directReference) && esgoRefs.TryGetValue(skinObject, out var addressableReference))
        {
            skinObject.addressableReference = addressableReference;
            skinObject.directReference = null;
        }
    }
    
    
}