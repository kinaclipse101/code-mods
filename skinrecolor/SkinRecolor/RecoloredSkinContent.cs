using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Projectile;
using SkinRecolor.RecoloredSkins;
using SkinRecolor.SkinManager;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace SkinRecolor;

public class RecoloredSkinContent : IContentPackProvider
{
    public static ContentPack RecoloredSkinContentPack { get; } = new();
    public static List<EffectDef> tempVFXForRuntimeEdits = [];

    public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
    {
        List<EffectDef> effectDefs = [];
        
        foreach (var recoloredSkinJson in RecoloredSkinManager.skinJsons)
        {
            if (recoloredSkinJson.projectileGhosts != null && recoloredSkinJson.vfxOverrides != null)
            {
                SetVFXDummies(recoloredSkinJson, effectDefs);
            }
        }

        foreach (var recoloredSkinVFX in RecoloredSkinManager.vfxJsons)
        {
            if(recoloredSkinVFX.vfxOverrides != null)
            {
                SetVFXDummies(recoloredSkinVFX, effectDefs);
            }
        }
        
        RecoloredSkinContentPack.effectDefs.Add(effectDefs.ToArray());
        yield break;
    }
    
    private static void SetVFXDummies(RecoloredSkinJson recoloredSkinJson, List<EffectDef> effectDefs)
    {
        foreach (var jsonGhost in recoloredSkinJson.projectileGhosts)
        {
            RecursiveFindVFXInSubPrefabsDummy(recoloredSkinJson, jsonGhost, [], effectDefs);
        }
        foreach (var jsonGhost in recoloredSkinJson.vfxOverrides)
        {
            RecursiveFindVFXInSubPrefabsDummy(recoloredSkinJson, jsonGhost, [], effectDefs);
        }
    }

    private static void RecursiveFindVFXInSubPrefabsDummy(RecoloredSkinJson recoloredSkinJson, JsonBase jsonBase, List<SubPrefab> subPrefabPath, List<EffectDef> effectDefs)
    {
        if (jsonBase is VFXOverrideJson vfxJson)
        {
            string prefabName = GetPrefabNameForTemp(recoloredSkinJson, vfxJson, subPrefabPath);
            GameObject prefabDummyStandin2 = new GameObject(prefabName);
            EffectComponent component = prefabDummyStandin2.AddComponent<EffectComponent>();
            EffectDef def = new EffectDef(prefabDummyStandin2);
            effectDefs.Add(def);
            Log.Debug($"added standin prefab to catalog {prefabName}");

            EffectCatalog.availability.onAvailable += () =>
            {
                CreateVFX(recoloredSkinJson, subPrefabPath, vfxJson);
            };
        }
        
        foreach (var subPrefab in jsonBase.subPrefabs)
        {
            subPrefabPath.Add(subPrefab);
            if(subPrefab.projectileGhostJson != null)
                RecursiveFindVFXInSubPrefabsDummy(recoloredSkinJson, subPrefab.projectileGhostJson, subPrefabPath, effectDefs);
            else if(subPrefab.vfxOverrideJson != null)
                RecursiveFindVFXInSubPrefabsDummy(recoloredSkinJson, subPrefab.vfxOverrideJson, subPrefabPath, effectDefs);
        }
    }
 

    private static async Task CreateVFX(RecoloredSkinJson recoloredSkinJson, List<SubPrefab> subPrefabPath, VFXOverrideJson vfxJson)
    {
        if (vfxJson.guid != null)
        {
            var prefab = await Addressables.LoadAssetAsync<GameObject>(vfxJson.guid).Task;
            string prefabName = GetPrefabNameForTemp(recoloredSkinJson, vfxJson, subPrefabPath);
            GameObject effectPrefab = RecolorEffectPrefab(prefab, prefabName, vfxJson, recoloredSkinJson);
            await AddVFXFromJson(recoloredSkinJson, vfxJson, prefab, prefabName, effectPrefab);
        }
        else
        {
            if (EntityStateCatalog.availability.available)
            {
                WaitForESCAsync(recoloredSkinJson, subPrefabPath, vfxJson);
            }
            else
            {
                EntityStateCatalog.availability.onAvailable += () =>
                {
                    WaitForESCAsync(recoloredSkinJson, subPrefabPath, vfxJson);
                };
            }
        }
    }

    private static async Task WaitForESCAsync(RecoloredSkinJson recoloredSkinJson, List<SubPrefab> subPrefabPath, VFXOverrideJson vfxJson)
    {
        var prefab = await Utils.TryGetPrefabFromESC(vfxJson.configurationType, vfxJson.fieldName);
        Log.Debug($"got prefab {prefab} from {subPrefabPath} {vfxJson.configurationType} {vfxJson.fieldName}");
        foreach (var subPrefab in subPrefabPath)
        {
            prefab = prefab.GetComponent(subPrefab.varType).GetFieldValue<GameObject>(subPrefab.varName);
        }
        Log.Debug($"after subprefab path processing {prefab} from {subPrefabPath} {vfxJson.configurationType} {vfxJson.fieldName}");
     
        string prefabName = GetPrefabNameForTemp(recoloredSkinJson, vfxJson, subPrefabPath);
        GameObject effectPrefab = RecolorEffectPrefab(prefab, prefabName, vfxJson, recoloredSkinJson);
        Log.Debug($"effect prefab {prefab} from {subPrefabPath} {vfxJson.configurationType} {vfxJson.fieldName}");
        await AddVFXFromJson(recoloredSkinJson, vfxJson, prefab, prefabName, effectPrefab);
    }

    private static async Task AddVFXFromJson(RecoloredSkinJson recoloredSkinJson, VFXOverrideJson vfxJson, GameObject prefab, string prefabName, GameObject effectPrefab)
    {
        Log.Debug($"processing base prefab {prefab} from {vfxJson.configurationType} {vfxJson.fieldName}");
        
        if (effectPrefab.TryGetComponent(out EffectComponent effectComponent))
        {
            if (EffectCatalog.nameToEffectIndex.TryGetValue(prefabName, out var effectIndex))
            {
                effectComponent.effectIndex = effectIndex;
                EffectDef def = EffectCatalog.GetEffectDef(effectIndex);
                Object.Destroy(def.prefab);
                def.prefab = effectPrefab;
                Log.Message($"replaced prefab {prefab} with {effectPrefab} !!");
            }
            else
            {
                Log.Warning($"effect had effect component but wasnt in catalog {prefab.name} !!");
            }
        }
        
        if (RecoloredSkinManager.skinRecolorsFinished)
        {
            FinishedSkinRecolors();
        }
        else
        {
            Log.Debug($"waiting for skin catalog on prefab {prefab} with {effectPrefab} ");
            RecoloredSkinManager.finishedSkinRecolors += FinishedSkinRecolors;
        }
        
        void FinishedSkinRecolors()
        {
            AddSkinVFXInfo(recoloredSkinJson, prefab, effectPrefab);
        }
    }

    public static void AddSkinVFXInfo(RecoloredSkinJson recoloredSkinJson, GameObject prefab, GameObject effectPrefab)
    {
        foreach (var kvp in RecoloredSkinManager.recoloredSkinDefs)
        {
            if (kvp.Value != recoloredSkinJson) continue;
            Log.Debug($"found skin for prefab  1 {prefab} with {effectPrefab} ");
            if (!SkinVFXManager.AddSkinVFXInfo(kvp.Key, prefab, effectPrefab))
            {
                Log.Error($"couldnt replace effect with skin {prefab} with {effectPrefab} ");
            }
        }
    }

    public static string GetPrefabNameForTemp(RecoloredSkinJson recoloredSkinJson, VFXOverrideJson vfxOverrideJson, List<SubPrefab> subPrefabs)
    {
        StringBuilder builder = new StringBuilder();

        builder.Append("JSON_");
        
        if (vfxOverrideJson.guid != null)
        {
            builder.Append("_");
            builder.Append(vfxOverrideJson.guid[..8]);
        }
        
        //possible on overrides for specific survs huntress 
        if(vfxOverrideJson.configurationType != null)
        {
            builder.Append("_");
            builder.Append(vfxOverrideJson.configurationType.Split(",")[0]);
        }
        if(vfxOverrideJson.fieldName != null)
        {
            builder.Append("_");
            builder.Append(vfxOverrideJson.fieldName);
        }

        foreach (var subPrefab in subPrefabs)
        {
            builder.Append(subPrefab.varName);
            builder.Append(subPrefab.varType);

            if (subPrefab.vfxOverrideJson != null)
            {
                builder.Append(subPrefab.vfxOverrideJson.fieldName);
                builder.Append(subPrefab.vfxOverrideJson.configurationType);
            }
            if (subPrefab.projectileGhostJson != null)
            {
                builder.Append(subPrefab.projectileGhostJson.fieldName);
                builder.Append(subPrefab.projectileGhostJson.configurationType);
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

    public static GameObject RecolorEffectPrefab(GameObject basePrefab, string prefabName, JsonBase jsonBase, RecoloredSkinJson recoloredSkinJson)
    {
        var effectPrefab = basePrefab.InstantiateClone(prefabName, false);

        foreach (TypeToPath typeToPath in jsonBase.typesAndPaths)
        {
            var objectTransform = typeToPath.isChild ? effectPrefab.transform.Find(typeToPath.path) : effectPrefab.transform;
            if (!objectTransform)
            {
                if (typeToPath.path == basePrefab.name)
                {
                    objectTransform = effectPrefab.transform;
                }
                else
                {
                    Log.Warning("umm bad something went bad " + typeToPath.path + " prefab name " + basePrefab.name);
                    continue;
                }
            }
            
            //this code needs to be redone because of freakings captain alt util
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
                    RecolorComponentBehavior(recoloredSkinJson, component, jsonBase);
                }
            }
        }

        return effectPrefab;
    }

    public static void RecolorComponentBehavior(RecoloredSkinJson recoloredSkinJson, Component component, JsonBase jsonBase)
    {
        switch (component)
        {
            case Light light:
                light.color = Utils.RecolorColor(recoloredSkinJson, light.color);
                break;
                    
            case TrailRenderer trailRenderer:
                trailRenderer.startColor = Utils.RecolorColor(recoloredSkinJson, trailRenderer.startColor, false);
                trailRenderer.endColor = Utils.RecolorColor(recoloredSkinJson, trailRenderer.endColor, false);
                
                for (int i = 0; i < trailRenderer.colorGradient?.colorKeys?.Length; i++)
                {
                    trailRenderer.colorGradient.colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson, trailRenderer.colorGradient.colorKeys[i].color);
                }

                Material recoloredMatTrail = new Material(trailRenderer.material);
                Utils.RecolorMaterial(ref recoloredMatTrail, trailRenderer.material, recoloredSkinJson, false);
                trailRenderer.material = recoloredMatTrail;
                break;
            
            case LineRenderer lineRenderer:
                lineRenderer.startColor = Utils.RecolorColor(recoloredSkinJson, lineRenderer.startColor, false);
                lineRenderer.endColor = Utils.RecolorColor(recoloredSkinJson, lineRenderer.endColor, false);
                
                for (int i = 0; i < lineRenderer.colorGradient?.colorKeys?.Length; i++)
                {
                    lineRenderer.colorGradient.colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson, lineRenderer.colorGradient.colorKeys[i].color, false);
                }

                Material recoloredMatLine = new Material(lineRenderer.material);
                Utils.RecolorMaterial(ref recoloredMatLine, lineRenderer.material, recoloredSkinJson, false);
                lineRenderer.material = recoloredMatLine;
                break;
                    
            case MeshRenderer meshRenderer:
                Material recoloredMatMesh = new Material(meshRenderer.material);
                Utils.RecolorMaterial(ref recoloredMatMesh, meshRenderer.material, recoloredSkinJson, false);
                meshRenderer.material = recoloredMatMesh;
                break;
                    
            case SkinnedMeshRenderer skinnedMshRenderer:
                Material skinnedMecoloredMat = new Material(skinnedMshRenderer.material);
                Utils.RecolorMaterial(ref skinnedMecoloredMat, skinnedMshRenderer.material, recoloredSkinJson, false);
                skinnedMshRenderer.material = skinnedMecoloredMat;
                break;
                    
            case ParticleSystem ps:
                var main = ps.main;

                if (ps.lights.enabled && ps.lights.light)
                {
                    ps.lights.light.color = Utils.RecolorColor(recoloredSkinJson, ps.lights.light.color, false);
                }
                
                var startColor = main.startColor;
                switch (startColor.mode)
                {
                    case ParticleSystemGradientMode.Color:
                        main.startColor = Utils.RecolorColor(recoloredSkinJson, startColor.color, false);
                        break;
                    case ParticleSystemGradientMode.TwoColors:
                        //Log.Debug($"two color start color on component {component}");
                        //Log.Debug($"prev color 1 {main.startColor.colorMin}");
                        startColor.colorMin = Utils.RecolorColor(recoloredSkinJson, main.startColor.colorMin, false);
                        startColor.colorMax = Utils.RecolorColor(recoloredSkinJson, main.startColor.colorMax, false);
                        main.startColor = startColor;
                        //main.startColor = new ParticleSystem.MinMaxGradient(, Utils.RecolorColor(recoloredSkinJson, main.startColor.colorMax));
                        //Log.Debug($"post color 1 {main.startColor.colorMin}");
                        break;
                    case ParticleSystemGradientMode.Gradient:
                    case ParticleSystemGradientMode.RandomColor:
                    case ParticleSystemGradientMode.TwoGradients:
                    default:
                        if (startColor.gradient?.colorKeys != null)
                        {
                            GradientColorKey[] colorKeys = new GradientColorKey[startColor.gradient.colorKeys.Length];
                            
                            for (int i = 0; i < startColor.gradient.colorKeys.Length; i++)
                            {
                                colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson, startColor.gradient.colorKeys[i].color, false);
                            }
                            
                            startColor.gradient.SetKeys(colorKeys, startColor.gradient.alphaKeys);
                        }

                        if (startColor.gradientMin?.colorKeys != null)
                        {
                            GradientColorKey[] colorKeys = new GradientColorKey[startColor.gradientMin.colorKeys.Length];
                            
                            for (int i = 0; i < startColor.gradientMin.colorKeys.Length; i++)
                            {
                                colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson, startColor.gradientMin.colorKeys[i].color, false);
                            }
                            
                            startColor.gradientMin.SetKeys(colorKeys, startColor.gradientMin.alphaKeys);
                        }
                        
                        if (startColor.gradientMax?.colorKeys != null)
                        {
                            GradientColorKey[] colorKeys = new GradientColorKey[startColor.gradientMax.colorKeys.Length];
                            
                            for (int i = 0; i < startColor.gradientMax.colorKeys.Length; i++)
                            {
                                colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson, startColor.gradientMax.colorKeys[i].color, false);
                            }
                            
                            startColor.gradientMax.SetKeys(colorKeys, startColor.gradientMax.alphaKeys);
                        }
                        
                        break;
                }
                
                var colorOverLifetime = ps.colorOverLifetime;
                var colorOverLifetimeColor = colorOverLifetime.color;
                switch (colorOverLifetimeColor.mode)
                {
                    case ParticleSystemGradientMode.Color:
                        colorOverLifetimeColor.color = Utils.RecolorColor(recoloredSkinJson, colorOverLifetimeColor.color, false);
                        break;
                    case ParticleSystemGradientMode.TwoColors:
                        //Log.Debug($"two color start color on component {component}");
                        //Log.Debug($"prev color 1 {main.startColor.colorMin}");
                        colorOverLifetimeColor.colorMin = Utils.RecolorColor(recoloredSkinJson, colorOverLifetimeColor.colorMin, false);
                        colorOverLifetimeColor.colorMax = Utils.RecolorColor(recoloredSkinJson, colorOverLifetimeColor.colorMax, false);
                        colorOverLifetime.color = colorOverLifetimeColor;
                        //colorOverLifetimeColor = startColor;
                        //main.startColor = new ParticleSystem.MinMaxGradient(, Utils.RecolorColor(recoloredSkinJson, main.startColor.colorMax));
                        //Log.Debug($"post color 1 {main.startColor.colorMin}");
                        break;
                    case ParticleSystemGradientMode.Gradient:
                    case ParticleSystemGradientMode.RandomColor:
                    case ParticleSystemGradientMode.TwoGradients:
                    default:
                        if (colorOverLifetimeColor.gradient?.colorKeys != null)
                        {
                            GradientColorKey[] colorKeys = new GradientColorKey[colorOverLifetimeColor.gradient.colorKeys.Length];
                            
                            for (int i = 0; i < colorOverLifetimeColor.gradient.colorKeys.Length; i++)
                            {
                                colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson, colorOverLifetimeColor.gradient.colorKeys[i].color, false);
                            }
                            
                            colorOverLifetimeColor.gradient.SetKeys(colorKeys, colorOverLifetimeColor.gradient.alphaKeys);
                        }

                        if (colorOverLifetimeColor.gradientMin?.colorKeys != null)
                        {
                            GradientColorKey[] colorKeys = new GradientColorKey[colorOverLifetimeColor.gradientMin.colorKeys.Length];
                            
                            for (int i = 0; i < colorOverLifetimeColor.gradientMin.colorKeys.Length; i++)
                            {
                                colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson, colorOverLifetimeColor.gradientMin.colorKeys[i].color, false);
                            }
                            
                            colorOverLifetimeColor.gradientMin.SetKeys(colorKeys, colorOverLifetimeColor.gradientMin.alphaKeys);
                        }
                        
                        if (colorOverLifetimeColor.gradientMax?.colorKeys != null)
                        {
                            GradientColorKey[] colorKeys = new GradientColorKey[colorOverLifetimeColor.gradientMax.colorKeys.Length];
                            
                            for (int i = 0; i < colorOverLifetimeColor.gradientMax.colorKeys.Length; i++)
                            {
                                colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson, colorOverLifetimeColor.gradientMax.colorKeys[i].color, false);
                            }
                            
                            colorOverLifetimeColor.gradientMax.SetKeys(colorKeys, colorOverLifetimeColor.gradientMax.alphaKeys);
                        }
                        break;
                }

                colorOverLifetime.color = colorOverLifetimeColor;
                break;
            
            case ParticleSystemRenderer particleSystemRenderer:
                if (particleSystemRenderer.material)
                {
                    Material recoloredMat = new Material(particleSystemRenderer.material);
                    Utils.RecolorMaterial(ref recoloredMat, particleSystemRenderer.material, recoloredSkinJson);
                    particleSystemRenderer.material = recoloredMat;
                }
                
                if (particleSystemRenderer.trailMaterial)
                {
                    Material recoloredMat = new Material(particleSystemRenderer.trailMaterial);
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