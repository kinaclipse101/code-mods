using System;
using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace SkinRecolor.SkinManager;

public class VFX
{
    public static async void AddVFXOverride(RecoloredSkinJson recoloredSkinJson, VFXOverrideJson jsonOverride, SkinDef recoloredSkinDef)
    {
        try
        {
            Log.Debug($"attempting to adde effect {jsonOverride.effectName} to skin {recoloredSkinDef.name}");
            EffectIndex effectIndex = EffectCatalog.nameToEffectIndex[jsonOverride.effectName];
            EffectDef effectDef = EffectCatalog.GetEffectDef(effectIndex);
            if(effectIndex == EffectIndex.Invalid || effectDef == null)
            {
                Log.Error("couldnt find effect" + effectIndex);
                return;
            }

            string prefabName = $"jsonPrefab{effectIndex}{recoloredSkinJson}{recoloredSkinJson.saturation}{recoloredSkinJson.value}{recoloredSkinJson.skinName}";
            if (EffectCatalog.nameToEffectIndex.TryGetValue(prefabName, out _))
            {
                Log.Debug("effect already exists in catalog");
                return;
            }
                
            GameObject basePrefab = await Addressables.LoadAssetAsync<GameObject>(effectDef._prefabAssetOrDirectReference.address.AssetGUID).Task;
            var effectPrefab = basePrefab.InstantiateClone(prefabName, false);

            foreach (string lightPath in jsonOverride.lightPaths)
            {
                Transform lightObject = effectPrefab.transform.Find(lightPath);
                if (!lightObject)
                {
                    Log.Warning("umm bad something went bad " + lightPath);
                    continue;
                }

                Light light = lightObject.GetComponent<Light>();
                light.color = Utils.RecolorColor(recoloredSkinJson, light.color);
            }

            foreach (string trailPath in jsonOverride.trailPaths)
            {
                Transform trailObject = effectPrefab.transform.Find(trailPath);
                if (!trailObject)
                {
                    Log.Warning("umm bad something went bad " + trailPath);
                    continue;
                }

                List<Transform> trailTransforms = []; // evil fuckigns arti fireball with same heirarchy trail .,,.
                string childName = trailPath.Split("/")[^1];
                for (int i = 0; i < trailObject.parent.childCount; i++)
                {
                    Transform child = trailObject.parent.GetChild(i);
                    if (child.gameObject.name == childName)
                    {
                        trailTransforms.Add(child);
                    }
                }

                foreach (var trailTransform in trailTransforms)
                {
                    TrailRenderer[] trails = trailTransform.gameObject.GetComponents<TrailRenderer>();
                    foreach (var trail in trails)
                    {
                        trail.startColor = Utils.RecolorColor(recoloredSkinJson, trail.startColor);
                        trail.endColor = Utils.RecolorColor(recoloredSkinJson, trail.endColor);
                        for (int i = 0; i < trail.colorGradient.colorKeys.Length; i++)
                        {
                            trail.colorGradient.colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson,
                                trail.colorGradient.colorKeys[i].color);
                        }

                        Material recoloredMat = Object.Instantiate(trail.material);
                        trail.material = recoloredMat;
                        Utils.RecolorMaterial(recoloredMat, recoloredSkinJson, false);
                    }
                }
            }

            foreach (string meshPath in jsonOverride.meshPaths)
            {
                Transform meshObject = effectPrefab.transform.Find(meshPath);
                if (!meshObject)
                {
                    Log.Warning("umm bad something went bad " + meshPath);
                    continue;
                }

                MeshRenderer[] meshes = meshObject.GetComponents<MeshRenderer>();
                foreach (var mesh in meshes)
                {
                    Material recoloredMat = Object.Instantiate(mesh.material);
                    mesh.material = recoloredMat;
                    Utils.RecolorMaterial(recoloredMat, recoloredSkinJson, false);
                }
            }

            foreach (string skinnedMeshPath in jsonOverride.skinnedMeshPaths)
            {
                Transform skinnedMeshObject = effectPrefab.transform.Find(skinnedMeshPath);
                if (!skinnedMeshObject)
                {
                    Log.Warning("umm bad something went bad " + skinnedMeshPath);
                    continue;
                }

                MeshRenderer[] skinnedMeshes = skinnedMeshObject.GetComponents<MeshRenderer>();
                foreach (var skinnedMesh in skinnedMeshes)
                {
                    Material skinnedMecoloredMat = Object.Instantiate(skinnedMesh.material);
                    skinnedMesh.material = skinnedMecoloredMat;
                    Utils.RecolorMaterial(skinnedMecoloredMat, recoloredSkinJson, false);
                }
            }

            foreach (string particlePath in jsonOverride.particlePaths)
            {
                Transform particleObject = effectPrefab.transform.Find(particlePath);
                if (!particleObject)
                {
                    Log.Warning("umm bad something went bad " + particlePath);
                    continue;
                }

                ParticleSystem[] particleSystems = particleObject.GetComponents<ParticleSystem>();
                foreach (var particleSystem in particleSystems)
                {
                    var main = particleSystem.main;

                    var minMaxGradient = main.startColor;
                    minMaxGradient.color = Utils.RecolorColor(recoloredSkinJson, minMaxGradient.color);
                    minMaxGradient.colorMin = Utils.RecolorColor(recoloredSkinJson, minMaxGradient.colorMin);
                    minMaxGradient.colorMax = Utils.RecolorColor(recoloredSkinJson, minMaxGradient.colorMax);
                    if (minMaxGradient.gradient?.colorKeys != null)
                    {
                        for (int i = 0; i < minMaxGradient.gradient.colorKeys.Length; i++)
                        {
                            minMaxGradient.gradient.colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson, minMaxGradient.gradient.colorKeys[i].color);
                        }
                    }

                    main.startColor = minMaxGradient;

                    var colorOverLifetime = particleSystem.colorOverLifetime;
                    var lifetimeMinMaxGradient = colorOverLifetime.color;
                    lifetimeMinMaxGradient.color =
                        Utils.RecolorColor(recoloredSkinJson, lifetimeMinMaxGradient.color);
                    lifetimeMinMaxGradient.colorMin =
                        Utils.RecolorColor(recoloredSkinJson, lifetimeMinMaxGradient.colorMin);
                    lifetimeMinMaxGradient.colorMax =
                        Utils.RecolorColor(recoloredSkinJson, lifetimeMinMaxGradient.colorMax);
                    if (lifetimeMinMaxGradient.gradient?.colorKeys != null)
                    {
                        for (int i = 0; i < lifetimeMinMaxGradient.gradient.colorKeys.Length; i++)
                        {
                            lifetimeMinMaxGradient.gradient.colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson,
                                lifetimeMinMaxGradient.gradient.colorKeys[i].color);
                        }
                    }

                    colorOverLifetime.color = lifetimeMinMaxGradient;
                }

                ParticleSystemRenderer[] particleSystemRenderers =
                    particleObject.GetComponents<ParticleSystemRenderer>();
                foreach (var particleSystemRenderer in particleSystemRenderers)
                {
                    if (particleSystemRenderer.material)
                    {
                        Material recoloredMat = Object.Instantiate(particleSystemRenderer.material);
                        particleSystemRenderer.material = recoloredMat;
                        Utils.RecolorMaterial(recoloredMat,
                            recoloredSkinJson,
                            false);
                    }

                    if (particleSystemRenderer.trailMaterial)
                    {
                        Material recoloredMat = Object.Instantiate(particleSystemRenderer.trailMaterial);
                        particleSystemRenderer.trailMaterial = recoloredMat;
                        Utils.RecolorMaterial(recoloredMat,
                            recoloredSkinJson,
                            false);
                    }
                }
            }

            if (!EffectCatalog.nameToEffectIndex.TryGetValue(effectPrefab.name, out EffectIndex existingEffectIndex))
            {
                EffectDef newEffect = new EffectDef(effectPrefab)
                {
                    index = (EffectIndex)EffectCatalog.effectCount
                };
                effectPrefab.GetComponent<EffectComponent>().effectIndex = newEffect.index;
                HG.ArrayUtils.ArrayAppend(ref EffectCatalog.entries, newEffect);
                EffectCatalog.nameToEffectIndex.Add(effectPrefab.name, newEffect.index);
            }
            else
            {
                effectPrefab.GetComponent<EffectComponent>().effectIndex = existingEffectIndex;
            }
           
            Log.Debug($"Added effect: {effectPrefab.name} with index {EffectCatalog.effectCount}");
            SkinVFX.AddSkinVFX(recoloredSkinDef, effectIndex, effectPrefab);
        }
        catch (Exception e)
        {
            Log.Error($"error while adding vfx to skin {recoloredSkinJson.baseSkinName} !!");
            Log.Error(e);
        }
    }
}