using System;
using System.Collections;
using System.Collections.Generic;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.ContentManagement;
using SkinRecolor.RecoloredSkins;
using SkinRecolor.SkinManager;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace SkinRecolor;

public class RecoloredSkinContent : IContentPackProvider
{
    public static Dictionary<RecoloredSkinJson, List<VFXOverride>> vfxOverrides = [];
    public static ContentPack RecoloredSkinContentPack { get; } = new();

    public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
    {
        List<EffectDef> effectDefs = [];
        
        foreach (var recoloredSkinJson in RecoloredSkinManager.skinJsons)
        {
            if(recoloredSkinJson.vfxOverrides == null) continue;
            foreach (var jsonOverride in recoloredSkinJson.vfxOverrides)
            {
                if (jsonOverride.guid == null) continue;
                
                var basePrefab = Addressables.LoadAssetAsync<GameObject>(jsonOverride.guid).WaitForCompletion();
                
                string prefabName = $"jsonPrefab_{basePrefab}_{recoloredSkinJson.saturation}{recoloredSkinJson.value}{recoloredSkinJson.skinName}";
                if (jsonOverride.typesAndPaths.Count > 0)
                {
                    var effectPrefab = RecolorEffectPrefab(basePrefab, prefabName, jsonOverride, recoloredSkinJson);
                    
                    VFXOverride vfx = new VFXOverride()
                    {
                        jsonOverride = jsonOverride,
                        basePrefab = basePrefab,
                        overridePrefab = effectPrefab
                    };
                    if (vfxOverrides.TryGetValue(recoloredSkinJson, out var vfxList))
                        vfxList.Add(vfx);
                    else
                        vfxOverrides.Add(recoloredSkinJson, [vfx]);
                    
                    effectDefs.Add(new EffectDef(effectPrefab));
                }
                
                foreach (var subPrefab in jsonOverride.subPrefabs)
                {
                    Log.Debug($"Prefab: {basePrefab}, SubPrefab: {subPrefab.type}  subPrefab: {subPrefab.varName}");

                    GameObject prefabDummyStandin = PrefabAPI.CreateEmptyPrefab($"jsonPrefab_{subPrefab.prefabName}_{recoloredSkinJson.saturation}{recoloredSkinJson.value}{recoloredSkinJson.skinName}");
                    prefabDummyStandin.AddComponent<EffectComponent>();
                    effectDefs.Add(new EffectDef(prefabDummyStandin));
                }
                
                yield return null;
            }
        }
        
        RecoloredSkinContentPack.effectDefs.Add(effectDefs.ToArray());
        yield break;
    }

    public static GameObject RecolorEffectPrefab(GameObject basePrefab, string prefabName, VFXOverrideJson jsonOverride, RecoloredSkinJson recoloredSkinJson)
    {
        var effectPrefab = basePrefab.InstantiateClone(prefabName, false);

        foreach (TypeToPath typeToPath in jsonOverride.typesAndPaths)
        {
            var objectTransform = typeToPath.isChild ? effectPrefab.transform.Find(typeToPath.path) : effectPrefab.transform;
            if (!objectTransform)
            {
                Log.Warning("umm bad something went bad " + typeToPath.path + " prefab name " + prefabName);
                continue;
            }
            
            List<Transform> transforms = []; // evil fuckigns arti fireball with same heirarchy trail .,,.
            string childName = typeToPath.path.Split("/")[^1];
            for (int i = 0; i < objectTransform.parent.childCount; i++)
            {
                Transform child = objectTransform.parent.GetChild(i);
                if (child.gameObject.name == childName)
                {
                    transforms.Add(child);
                }
            }

            foreach (var trailTransform in transforms)
            {
                var components = objectTransform.GetComponents(typeToPath.type);
                foreach (var component in components)
                {
                    RecolorComponentBehavior(recoloredSkinJson, component, typeToPath, objectTransform);
                }
            }
        }

        return effectPrefab;
    }

    public static void RecolorComponentBehavior(RecoloredSkinJson recoloredSkinJson, Component component, TypeToPath typeToPath, Transform objectTransform)
    {
        switch (component)
        {
            case Light light:
                light.color = Utils.RecolorColor(recoloredSkinJson, light.color);
                break;
                    
            case TrailRenderer trailRenderer:
                
                Color newTrailStart = Utils.RecolorColor(recoloredSkinJson, trailRenderer.startColor);
                newTrailStart.a = trailRenderer.startColor.a;
                trailRenderer.startColor = newTrailStart;
                
                Color newTrailEnd = Utils.RecolorColor(recoloredSkinJson, trailRenderer.endColor);
                newTrailEnd.a = trailRenderer.endColor.a;
                trailRenderer.endColor = newTrailEnd;
                
                for (int i = 0; i < trailRenderer.colorGradient.colorKeys.Length; i++)
                {
                    Color newTrailColor = Utils.RecolorColor(recoloredSkinJson, trailRenderer.colorGradient.colorKeys[i].color);
                    newTrailColor.a = trailRenderer.colorGradient.colorKeys[i].color.a;
                    trailRenderer.colorGradient.colorKeys[i].color = newTrailColor;
                }

                Material recoloredMatTrail = Object.Instantiate(trailRenderer.material);
                trailRenderer.material = recoloredMatTrail;
                Utils.RecolorMaterial(recoloredMatTrail, recoloredSkinJson, false);
                break;
                    
            case MeshRenderer meshRenderer:
                Material recoloredMatMesh = Object.Instantiate(meshRenderer.material);
                meshRenderer.material = recoloredMatMesh;
                Utils.RecolorMaterial(recoloredMatMesh, recoloredSkinJson, false);
                break;
                    
            case SkinnedMeshRenderer skinnedMshRenderer:
                Material skinnedMecoloredMat = Object.Instantiate(skinnedMshRenderer.material);
                skinnedMshRenderer.material = skinnedMecoloredMat;
                Utils.RecolorMaterial(skinnedMecoloredMat, recoloredSkinJson, false);
                break;
                    
            case ParticleSystem ps:
                //add alpha to the color of all of these .., 
                var colormodule = ps.colorOverLifetime.color;
                colormodule.color = Utils.RecolorColor(recoloredSkinJson, colormodule.color);
                colormodule.colorMin = Utils.RecolorColor(recoloredSkinJson, colormodule.colorMin);
                colormodule.colorMax = Utils.RecolorColor(recoloredSkinJson, colormodule.colorMax);
                if (colormodule.gradient?.colorKeys != null)
                {
                    for (int i = 0; i < colormodule.gradient.colorKeys.Length; i++)
                    {
                        colormodule.gradient.colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson, colormodule.gradient.colorKeys[i].color);
                    }
                }
                        
                var main = ps.main;

                if (ps.lights.enabled && ps.lights.light)
                {
                    ps.lights.light.color = Utils.RecolorColor(recoloredSkinJson, ps.lights.light.color);
                }
                
                var minMaxGradient = main.startColor;
                if (minMaxGradient.mode == ParticleSystemGradientMode.Color)
                {
                    main.startColor = Utils.RecolorColor(recoloredSkinJson, minMaxGradient.color);
                }
                else
                {
                    minMaxGradient.colorMin = Utils.RecolorColor(recoloredSkinJson, minMaxGradient.colorMin);
                    minMaxGradient.colorMax = Utils.RecolorColor(recoloredSkinJson, minMaxGradient.colorMax);
                    if (minMaxGradient.gradient?.colorKeys != null)
                    {
                        for (int i = 0; i < minMaxGradient.gradient.colorKeys.Length; i++)
                        {
                            minMaxGradient.gradient.colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson, minMaxGradient.gradient.colorKeys[i].color);
                        }
                    }
                }
                        
                var colorOverLifetime = ps.colorOverLifetime;
                var lifetimeMinMaxGradient = colorOverLifetime.color;
                lifetimeMinMaxGradient.color = Utils.RecolorColor(recoloredSkinJson, lifetimeMinMaxGradient.color);
                lifetimeMinMaxGradient.colorMin = Utils.RecolorColor(recoloredSkinJson, lifetimeMinMaxGradient.colorMin);
                lifetimeMinMaxGradient.colorMax = Utils.RecolorColor(recoloredSkinJson, lifetimeMinMaxGradient.colorMax);
                if (lifetimeMinMaxGradient.gradient?.colorKeys != null)
                {
                    for (int i = 0; i < lifetimeMinMaxGradient.gradient.colorKeys.Length; i++)
                    {
                        lifetimeMinMaxGradient.gradient.colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson,
                            lifetimeMinMaxGradient.gradient.colorKeys[i].color);
                    }
                }

                colorOverLifetime.color = lifetimeMinMaxGradient;
                break;
            
            case ParticleSystemRenderer particleSystemRenderer:
                if (particleSystemRenderer.material)
                {
                    Material recoloredMat = Object.Instantiate(particleSystemRenderer.material);
                    particleSystemRenderer.material = recoloredMat;
                    Utils.RecolorMaterial(recoloredMat, recoloredSkinJson);
                }
                
                if (particleSystemRenderer.trailMaterial)
                {
                    Material recoloredMat = Object.Instantiate(particleSystemRenderer.trailMaterial);
                    particleSystemRenderer.trailMaterial = recoloredMat;
                    Utils.RecolorMaterial(recoloredMat, recoloredSkinJson);
                }
                break;
        }
    }

    public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
    {
        ContentPack.Copy(RecoloredSkinContentPack, args.output);
        args.ReportProgress(1f);
        yield break;
    }

    public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
    {
        args.ReportProgress(1f);
        yield break;
    }
    
    private void AddSelf(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
    {
        addContentPackProvider(this);
    }
        
    internal RecoloredSkinContent()
    {
        ContentManager.collectContentPackProviders += AddSelf;
    }

    public string identifier => "RecoloredSkinContent";
}