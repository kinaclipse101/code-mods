using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
                if (jsonOverride.guid == null)
                {
                    if (jsonOverride.typesAndPaths.Count > 0)
                    {
                        GameObject prefabDummyStandin = PrefabAPI.CreateEmptyPrefab(GetPrefabNameForTemp(recoloredSkinJson, jsonOverride));
                        prefabDummyStandin.AddComponent<EffectComponent>();
                        effectDefs.Add(new EffectDef(prefabDummyStandin));
                    }
                    
                    foreach (var subPrefab in jsonOverride.subPrefabs)
                    {
                        GameObject prefabDummyStandin2 = PrefabAPI.CreateEmptyPrefab(GetPrefabNameForTemp(recoloredSkinJson, jsonOverride, subPrefab));
                        prefabDummyStandin2.AddComponent<EffectComponent>();
                        effectDefs.Add(new EffectDef(prefabDummyStandin2));
                    }
                }
                else // just create it now thens ,., .
                {
                    var basePrefab = Addressables.LoadAssetAsync<GameObject>(jsonOverride.guid).WaitForCompletion();
                
                    string prefabName = $"jsonPrefab_{basePrefab}_{recoloredSkinJson.saturation}{recoloredSkinJson.value}{recoloredSkinJson.skinName}";
                    if (jsonOverride.typesAndPaths.Count > 0)
                    {
                        var effectPrefab = RecolorEffectPrefab(basePrefab, prefabName, jsonOverride.typesAndPaths, recoloredSkinJson);
                    
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
                        if (subPrefab.vfxOverrideJson == null) continue;
                        
                        var effectPrefab = RecolorEffectPrefab(basePrefab, prefabName, jsonOverride.typesAndPaths, recoloredSkinJson);
                    
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
                }
                
                yield return null;
            }
            
            foreach (var jsonOverride in recoloredSkinJson.projectileGhosts)
            {
                List<SubPrefab> effectSubPrefabs = [];
                foreach (var subPrefab in jsonOverride.subPrefabs)
                {
                    if(subPrefab.vfxOverrideJson != null)
                        effectSubPrefabs.Add(subPrefab);
                }
                if (effectSubPrefabs.Count == 0) continue;

                foreach (var subPrefab in effectSubPrefabs)
                {
                    if (subPrefab.vfxOverrideJson == null) continue;
                        
                    GameObject prefabDummyStandin2 = PrefabAPI.CreateEmptyPrefab(GetPrefabNameForTemp(recoloredSkinJson, subPrefab.vfxOverrideJson, subPrefab));
                    prefabDummyStandin2.AddComponent<EffectComponent>();
                    effectDefs.Add(new EffectDef(prefabDummyStandin2));
                }
                
                yield return null;
            }
        }
        
        RecoloredSkinContentPack.effectDefs.Add(effectDefs.ToArray());
        yield break;
    }

    public static string GetPrefabNameForTemp(RecoloredSkinJson recoloredSkinJson, VFXOverrideJson vfxOverrideJson, SubPrefab subPrefab = null)
    {
        StringBuilder builder = new StringBuilder();

        builder.Append("JSON_");
        
        builder.Append(vfxOverrideJson.configurationType.Split(",")[0]);
        builder.Append(vfxOverrideJson.fieldName);

        if (subPrefab != null)
        {
            foreach (var var in subPrefab.varName)
            {
                builder.Append(var);
            }
        }

        builder.Append("_");
        builder.Append(recoloredSkinJson.hue);
        builder.Append("_");
        builder.Append(recoloredSkinJson.saturation);
        builder.Append("_");
        builder.Append(recoloredSkinJson.value);
        
        builder.Append("_");
        builder.Append(recoloredSkinJson.skinName);

        return builder.ToString();
    }

    public static GameObject RecolorEffectPrefab(GameObject basePrefab, string prefabName, List<TypeToPath> typesAndPaths, RecoloredSkinJson recoloredSkinJson)
    {
        var effectPrefab = basePrefab.InstantiateClone(prefabName, false);

        foreach (TypeToPath typeToPath in typesAndPaths)
        {
            var objectTransform = typeToPath.isChild ? effectPrefab.transform.Find(typeToPath.path) : effectPrefab.transform;
            if (!objectTransform)
            {
                Log.Warning("umm bad something went bad " + typeToPath.path + " prefab name " + basePrefab.name);
                continue;
            }
            
            List<Transform> transforms = []; // evil fuckigns arti fireball with same heirarchy trail .,,.
            string childName = typeToPath.path.Split("/")[^1];
            if (typeToPath.isChild)
            {
                Transform parentTransform = objectTransform.parent;
                for (int i = 0; i < parentTransform.childCount; i++)
                {
                    Transform child = parentTransform.GetChild(i);
                    if (child.name == childName)
                        transforms.Add(child);
                }
            }
            else
            {
                transforms.Add(objectTransform);
            }

            foreach (var individualTransform in transforms)
            {
                var components = individualTransform.GetComponents(typeToPath.type);
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
                trailRenderer.startColor = Utils.RecolorColor(recoloredSkinJson, trailRenderer.startColor);
                trailRenderer.endColor = Utils.RecolorColor(recoloredSkinJson, trailRenderer.endColor);
                
                for (int i = 0; i < trailRenderer.colorGradient?.colorKeys?.Length; i++)
                {
                    trailRenderer.colorGradient.colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson, trailRenderer.colorGradient.colorKeys[i].color);
                }

                Material recoloredMatTrail = Object.Instantiate(trailRenderer.material);
                Utils.RecolorMaterial(ref recoloredMatTrail, trailRenderer.material, recoloredSkinJson, false);
                trailRenderer.material = recoloredMatTrail;
                break;
            
            case LineRenderer lineRenderer:
                lineRenderer.startColor = Utils.RecolorColor(recoloredSkinJson, lineRenderer.startColor);
                lineRenderer.endColor = Utils.RecolorColor(recoloredSkinJson, lineRenderer.endColor);
                
                for (int i = 0; i < lineRenderer.colorGradient?.colorKeys?.Length; i++)
                {
                    lineRenderer.colorGradient.colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson, lineRenderer.colorGradient.colorKeys[i].color);
                }

                Material recoloredMatLine = Object.Instantiate(lineRenderer.material);
                Utils.RecolorMaterial(ref recoloredMatLine, lineRenderer.material, recoloredSkinJson, false);
                lineRenderer.material = recoloredMatLine;
                break;
                    
            case MeshRenderer meshRenderer:
                Material recoloredMatMesh = Object.Instantiate(meshRenderer.material);
                Utils.RecolorMaterial(ref recoloredMatMesh, meshRenderer.material, recoloredSkinJson, false);
                meshRenderer.material = recoloredMatMesh;
                break;
                    
            case SkinnedMeshRenderer skinnedMshRenderer:
                Material skinnedMecoloredMat = Object.Instantiate(skinnedMshRenderer.material);
                Utils.RecolorMaterial(ref skinnedMecoloredMat, skinnedMshRenderer.material, recoloredSkinJson, false);
                skinnedMshRenderer.material = skinnedMecoloredMat;
                break;
                    
            case ParticleSystem ps:
                var main = ps.main;

                if (ps.lights.enabled && ps.lights.light)
                {
                    ps.lights.light.color = Utils.RecolorColor(recoloredSkinJson, ps.lights.light.color);
                }
                
                var minMaxGradient = main.startColor;
                switch (minMaxGradient.mode)
                {
                    case ParticleSystemGradientMode.Color:
                        main.startColor = Utils.RecolorColor(recoloredSkinJson, minMaxGradient.color);
                        break;
                    case ParticleSystemGradientMode.TwoColors:
                        minMaxGradient.colorMin = Utils.RecolorColor(recoloredSkinJson, minMaxGradient.colorMin);
                        minMaxGradient.colorMax = Utils.RecolorColor(recoloredSkinJson, minMaxGradient.colorMax);
                        main.startColor = minMaxGradient;
                        break;
                    default:
                    {
                        if (minMaxGradient.gradient?.colorKeys != null)
                        {
                            for (int i = 0; i < minMaxGradient.gradient.colorKeys.Length; i++)
                            {
                                minMaxGradient.gradient.colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson, minMaxGradient.gradient.colorKeys[i].color);
                            }
                        }
                    
                        if (minMaxGradient.gradientMin?.colorKeys != null)
                        {
                            for (int i = 0; i < minMaxGradient.gradientMin.colorKeys.Length; i++)
                            {
                                minMaxGradient.gradientMin.colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson, minMaxGradient.gradientMin.colorKeys[i].color);
                            }
                        }
                    
                        if (minMaxGradient.gradientMax?.colorKeys != null)
                        {
                            for (int i = 0; i < minMaxGradient.gradientMax.colorKeys.Length; i++)
                            {
                                minMaxGradient.gradientMax.colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson, minMaxGradient.gradientMax.colorKeys[i].color);
                            }
                        }
                        
                        main.startColor = minMaxGradient;
                        break;
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
                        lifetimeMinMaxGradient.gradient.colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson, lifetimeMinMaxGradient.gradient.colorKeys[i].color);
                    }
                }

                //colorOverLifetime.color = lifetimeMinMaxGradient;
                break;
            
            case ParticleSystemRenderer particleSystemRenderer:
                if (particleSystemRenderer.material)
                {
                    Material recoloredMat = Object.Instantiate(particleSystemRenderer.material);
                    Utils.RecolorMaterial(ref recoloredMat, particleSystemRenderer.material, recoloredSkinJson);
                    particleSystemRenderer.material = recoloredMat;
                }
                
                if (particleSystemRenderer.trailMaterial)
                {
                    Material recoloredMat = Object.Instantiate(particleSystemRenderer.trailMaterial);
                    Utils.RecolorMaterial(ref recoloredMat, particleSystemRenderer.trailMaterial, recoloredSkinJson);
                    particleSystemRenderer.trailMaterial = recoloredMat;
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