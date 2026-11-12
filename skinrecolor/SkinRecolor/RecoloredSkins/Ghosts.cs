using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HG.GeneralSerializer;
using Newtonsoft.Json;
using R2API;
using RoR2;
using RoR2.Projectile;
using SkinRecolor.RecoloredSkins;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SkinRecolor.SkinManager;

public class Ghosts
{
    public static async Task ProjectileGhostReplacementsAndVFXOverrides(RecoloredSkinJson recoloredSkinJson, bool dontAdd, GameObject body, SkinDef recoloredSkinDef)
    {
        if (recoloredSkinJson.vfxOverrides == null || recoloredSkinJson.projectileGhosts == null)
        {
            recoloredSkinJson.vfxOverrides = [];
            recoloredSkinJson.projectileGhosts = [];
        
            string internalName = "";
            GenericSkill[] skills = body.GetComponents<GenericSkill>();
            foreach (var skill in skills)
            {
                if (skill.skillFamily.variants[0].skillDef.activationState.typeName != "")
                {
                    internalName = skill.skillFamily.variants[0].skillDef.activationState.typeName.Replace("EntityStates.", "").Split(".")[0];
                    if (internalName != "") break;
                }
            }
            if (internalName == "")
            {
                Log.Warning("couldnt find interanl name for projectile ghosts .,,. ");
                return;
            }
        
            Log.Debug($"looking for {internalName}");
            foreach (var esc in RoR2.ContentManagement.ContentManager.entityStateConfigurations)
            {
                if(esc.targetType.LookupType() == null) continue;
                if (!esc.targetType.LookupType().ToString().Contains(internalName)) continue;
            
                foreach (var field in esc.serializedFieldsCollection.serializedFields)
                {
                    if (!(field.fieldValue.objectValue as GameObject)) continue;
                    
                    var gameObject = (GameObject)field.fieldValue.objectValue;
                    CheckEffectComponent(recoloredSkinJson, recoloredSkinDef, gameObject);
                    await CheckProjectileController(recoloredSkinJson, recoloredSkinDef, gameObject, esc, field);
                }
            }
            
            await File.WriteAllTextAsync(RecoloredSkinManager.jsonPath, JsonConvert.SerializeObject(RecoloredSkinManager.skinJsons, Formatting.Indented));
        }

        await SetProjectileGhostReplacements(recoloredSkinJson, dontAdd, recoloredSkinDef);
    }

    private static async Task SetProjectileGhostReplacements(RecoloredSkinJson recoloredSkinJson, bool dontAdd, SkinDef recoloredSkinDef)
    {
        recoloredSkinDef.skinDefParams.projectileGhostReplacements = [];
        foreach (var jsonGhost in recoloredSkinJson.projectileGhosts)
        {
            EntityStateConfiguration targetEsc = null;
            foreach (var esc in RoR2.ContentManagement.ContentManager.entityStateConfigurations)
            {
                if (esc.targetType.LookupType() != jsonGhost.configurationType) continue;
                targetEsc = esc;
                break;
            }
            if (!targetEsc)
            {
                Log.Warning($"couldnt find esc for {jsonGhost.controllerFieldName}");
                continue;
            }
            GameObject prefab = null;
            foreach (var field in targetEsc.serializedFieldsCollection.serializedFields)
            {
                if(field.fieldName != jsonGhost.controllerFieldName) continue;
                prefab = field.fieldValue.objectValue as GameObject;
                break;
            }
            if (!prefab)
            {
                Log.Warning($"couldnt find GameObject for {jsonGhost.controllerFieldName}");
                continue;
            }

            if (jsonGhost.childProjectile)
            {
                ProjectileImpactExplosion explosion = prefab.GetComponent<ProjectileImpactExplosion>();
                prefab = explosion.childrenProjectilePrefab;
            }
            
            GameObject origGhostPrefab = await Addressables.LoadAssetAsync<GameObject>(jsonGhost.GUID).Task;
            GameObject ghostPrefab = origGhostPrefab.InstantiateClone("jsonProjectileGhost", false);
                
            foreach (string lightPath in jsonGhost.lightPaths)
            {
                Transform lightObject = ghostPrefab.transform.Find(lightPath);
                if (!lightObject)
                {
                    Log.Warning("umm bad something went bad " + lightPath);
                    continue;
                }
                    
                Light light = lightObject.GetComponent<Light>();
                light.color = Utils.RecolorColor(recoloredSkinJson, light.color);
            }

            foreach (string trailPath in jsonGhost.trailPaths)
            {
                Transform trailObject = ghostPrefab.transform.Find(trailPath);
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
                            trail.colorGradient.colorKeys[i].color = Utils.RecolorColor(recoloredSkinJson, trail.colorGradient.colorKeys[i].color);
                        }

                        Material recoloredMat = Object.Instantiate(trail.material);
                        trail.material = recoloredMat;
                        Utils.RecolorMaterial(recoloredMat, recoloredSkinJson, dontAdd);
                    }
                }
            }

            foreach (string meshPath in jsonGhost.meshPaths)
            {
                Transform meshObject = ghostPrefab.transform.Find(meshPath);
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
                    Utils.RecolorMaterial(recoloredMat, recoloredSkinJson, dontAdd);
                }
            }
                
            foreach (string skinnedMeshPath in jsonGhost.skinnedMeshPaths)
            {
                Transform skinnedMeshObject = ghostPrefab.transform.Find(skinnedMeshPath);
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
                    Utils.RecolorMaterial(skinnedMecoloredMat, recoloredSkinJson, dontAdd);
                }
            }
                
            foreach (string particlePath in jsonGhost.particlePaths)
            {
                Transform particleObject = ghostPrefab.transform.Find(particlePath);
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
                    colorOverLifetime.color = lifetimeMinMaxGradient;
                }
                
                ParticleSystemRenderer[] particleSystemRenderers = particleObject.GetComponents<ParticleSystemRenderer>();
                foreach (var particleSystemRenderer in particleSystemRenderers)
                {
                    if (particleSystemRenderer.material)
                    {
                        Material recoloredMat = Object.Instantiate(particleSystemRenderer.material);
                        particleSystemRenderer.material = recoloredMat; 
                        Utils.RecolorMaterial(recoloredMat, recoloredSkinJson, dontAdd);
                    }
                    if (particleSystemRenderer.trailMaterial)
                    {
                        Material recoloredMat = Object.Instantiate(particleSystemRenderer.trailMaterial);
                        particleSystemRenderer.trailMaterial = recoloredMat;
                        Utils.RecolorMaterial(recoloredMat, recoloredSkinJson, dontAdd);
                    }
                }
            }

            SkinDefParams.ProjectileGhostReplacement replacement = new SkinDefParams.ProjectileGhostReplacement
            {
                projectilePrefab = prefab,
                projectileGhostReplacementPrefab = ghostPrefab
            };
            HG.ArrayUtils.ArrayAppend(ref recoloredSkinDef.skinDefParams.projectileGhostReplacements, replacement);
            Log.Debug($"added projectile ghost !! {origGhostPrefab}");
        }
    }

    private static void CheckEffectComponent(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, GameObject gameObject)
    {
        if (!gameObject.TryGetComponent(out EffectComponent effectComponent)) return;
        
        VFXOverrideJson vfxOverrideJson = new VFXOverrideJson
        {
            lightPaths = [],
            meshPaths = [],
            skinnedMeshPaths = [],
            particlePaths = [],
            trailPaths = [],
            effectName = effectComponent.name
        };
            
        if(VFXOverridePathfinder(recoloredSkinJson, recoloredSkinDef, effectComponent, gameObject, vfxOverrideJson)) return;

        recoloredSkinJson.vfxOverrides.Add(vfxOverrideJson);
    }

    public static bool VFXOverridePathfinder(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, EffectComponent effectComponent, GameObject gameObject, VFXOverrideJson vfxOverrideJson)
    {
        bool exitEarly = false;
        foreach (var effect in recoloredSkinJson.vfxOverrides)
        {
            if (effect.effectName == effectComponent.name)
            {
                exitEarly = true;
                break;
            }
        }
        if (exitEarly)
            return true;

        Log.Debug($"found effectDef on {gameObject.name} for skin {recoloredSkinDef.name}");
        Log.Debug($"effectDef prefabName {effectComponent} for skin {recoloredSkinDef.name}");
        //vfxOverrideJson.GUID = projectileController.ghostPrefabAddress.AssetGUID;
                    
        Light[] effectLights = gameObject.GetComponentsInChildren<Light>();
        foreach (Light light in effectLights)
        {
            if(!vfxOverrideJson.lightPaths.Contains(Utils.GetTransformPath(gameObject, light.gameObject).ToString()))
                vfxOverrideJson.lightPaths.Add(Utils.GetTransformPath(gameObject, light.gameObject).ToString());
        }
        MeshRenderer[] effectMeshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer mesh in effectMeshRenderers)
        {
            if(!vfxOverrideJson.meshPaths.Contains(Utils.GetTransformPath(gameObject, mesh.gameObject).ToString()))
                vfxOverrideJson.meshPaths.Add(Utils.GetTransformPath(gameObject, mesh.gameObject).ToString());
        }
        SkinnedMeshRenderer[] effectSkinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer skinnedMesh in effectSkinnedMeshRenderers)
        {
            if(!vfxOverrideJson.skinnedMeshPaths.Contains(Utils.GetTransformPath(gameObject, skinnedMesh.gameObject).ToString())) 
                vfxOverrideJson.skinnedMeshPaths.Add(Utils.GetTransformPath(gameObject, skinnedMesh.gameObject).ToString());
        }
        ParticleSystem[] effectParticleRenderers = gameObject.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem particleRenderer in effectParticleRenderers)
        {
            if(!vfxOverrideJson.particlePaths.Contains(Utils.GetTransformPath(gameObject, particleRenderer.gameObject).ToString()))
                vfxOverrideJson.particlePaths.Add(Utils.GetTransformPath(gameObject, particleRenderer.gameObject).ToString());
        }
        TrailRenderer[] effectTrailRenderers = gameObject.GetComponentsInChildren<TrailRenderer>();
        foreach (TrailRenderer trailRenderer in effectTrailRenderers)
        {
            if(!vfxOverrideJson.trailPaths.Contains(Utils.GetTransformPath(gameObject, trailRenderer.gameObject).ToString()))
                vfxOverrideJson.trailPaths.Add(Utils.GetTransformPath(gameObject, trailRenderer.gameObject).ToString());
        }

        return false;
    }

    private static async Task CheckProjectileController(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, GameObject gameObject, EntityStateConfiguration esc, SerializedField field, bool isChild = false)
    {
        if (!gameObject.TryGetComponent(out ProjectileController projectileController)) return;
    
        await CheckChildProjectiles(recoloredSkinJson, recoloredSkinDef, gameObject, esc, field);
        CheckOverlapAttacks(recoloredSkinJson, recoloredSkinDef, gameObject);
        
        ProjectileGhostJson jsonGhostJson = new ProjectileGhostJson
        {
            lightPaths = [],
            meshPaths = [],
            skinnedMeshPaths = [],
            particlePaths = [],
            trailPaths = [],
            configurationType = esc.targetType.LookupType(),
            controllerFieldName = field.fieldName
        };
        if(isChild)
            jsonGhostJson.childProjectile = true;

        Log.Debug($"found projectile controller on {gameObject.name}");
        Log.Debug($"default ghost guid {projectileController.ghostPrefabAddress.AssetGUID}");
        jsonGhostJson.GUID = projectileController.ghostPrefabAddress.AssetGUID;
                        
        GameObject ghostPrefab = await Addressables.LoadAssetAsync<GameObject>(jsonGhostJson.GUID).Task;
        if (!ghostPrefab)
        {
            Log.Warning($"{gameObject.name} had no ghost prefab !!");
            return;
        }
                        
        ProjectileGhostPathfinder(ghostPrefab, jsonGhostJson);
        recoloredSkinJson.projectileGhosts.Add(jsonGhostJson);
    }

    private static void CheckOverlapAttacks(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, GameObject gameObject)
    {
        if (gameObject.TryGetComponent(out ProjectileOverlapAttack projectileOverlapAttack))
        {
            if (projectileOverlapAttack.impactEffect)
            {
                VFXOverrideJson vfxOverrideJson = new VFXOverrideJson
                {
                    lightPaths = [],
                    meshPaths = [],
                    skinnedMeshPaths = [],
                    particlePaths = [],
                    trailPaths = [],
                    effectName = projectileOverlapAttack.impactEffect.name
                };

                Log.Message($"adding overlap attack {projectileOverlapAttack.impactEffect.name}");
                VFXOverridePathfinder(recoloredSkinJson, recoloredSkinDef, projectileOverlapAttack.impactEffect.GetComponent<EffectComponent>(), projectileOverlapAttack.impactEffect, vfxOverrideJson);                             
                recoloredSkinJson.vfxOverrides.Add(vfxOverrideJson);
                //await AddVFXOverride(recoloredSkinJson, vfxOverrideJson, recoloredSkinDef);
            }
        }
    }

    private static async Task CheckChildProjectiles(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, GameObject gameObject, EntityStateConfiguration esc, SerializedField field)
    {
        if (gameObject.TryGetComponent(out ProjectileImpactExplosion projectileImpactExplosion))
        {
            if (projectileImpactExplosion.childrenProjectilePrefab)
            {
                if (projectileImpactExplosion.childrenProjectilePrefab.GetComponent<ProjectileController>())
                {
                    Log.Message(gameObject.name + " had a child projectile prefab ,.,");
                    await CheckProjectileController(recoloredSkinJson, recoloredSkinDef, projectileImpactExplosion.childrenProjectilePrefab, esc, field, true);
                }
                
                //VFXOverridePathfinder(recoloredSkinJson, recoloredSkinDef, projectileOverlapAttack.impactEffect.GetComponent<EffectComponent>(), projectileOverlapAttack.impactEffect, vfxOverrideJson);                             
                //recoloredSkinJson.vfxOverrides.Add(vfxOverrideJson);
            }
            
            if (projectileImpactExplosion.impactEffect)
            {
                if (projectileImpactExplosion.impactEffect.GetComponent<EffectComponent>())
                {
                    VFXOverrideJson vfxOverrideJson = new VFXOverrideJson
                    {
                        lightPaths = [],
                        meshPaths = [],
                        skinnedMeshPaths = [],
                        particlePaths = [],
                        trailPaths = [],
                        effectName = projectileImpactExplosion.impactEffect.name
                    };
                
                    Log.Message(gameObject.name + " had a projectileImpactExplosion.impactEffect prefab ,.,");
                    VFXOverridePathfinder(recoloredSkinJson, recoloredSkinDef, projectileImpactExplosion.impactEffect.GetComponent<EffectComponent>(), projectileImpactExplosion.impactEffect, vfxOverrideJson);                             
                    recoloredSkinJson.vfxOverrides.Add(vfxOverrideJson);
                }
            }
        }
    }

    public static void ProjectileGhostPathfinder(GameObject ghostPrefab, ProjectileGhostJson jsonGhostJson)
    {
        Light[] lights = ghostPrefab.GetComponentsInChildren<Light>();
        foreach (Light light in lights)
        {
            if(!jsonGhostJson.lightPaths.Contains(Utils.GetTransformPath(ghostPrefab, light.gameObject).ToString()))
                jsonGhostJson.lightPaths.Add(Utils.GetTransformPath(ghostPrefab, light.gameObject).ToString());
        }
        MeshRenderer[] meshRenderers = ghostPrefab.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer mesh in meshRenderers)
        {
            if(!jsonGhostJson.meshPaths.Contains(Utils.GetTransformPath(ghostPrefab, mesh.gameObject).ToString()))
                jsonGhostJson.meshPaths.Add(Utils.GetTransformPath(ghostPrefab, mesh.gameObject).ToString());
        }
        SkinnedMeshRenderer[] skinnedMeshRenderers = ghostPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshRenderers)
        {
            if(!jsonGhostJson.skinnedMeshPaths.Contains(Utils.GetTransformPath(ghostPrefab, skinnedMesh.gameObject).ToString())) 
                jsonGhostJson.skinnedMeshPaths.Add(Utils.GetTransformPath(ghostPrefab, skinnedMesh.gameObject).ToString());
        }
        ParticleSystem[] particleRenderers = ghostPrefab.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem particleRenderer in particleRenderers)
        {
            if(!jsonGhostJson.particlePaths.Contains(Utils.GetTransformPath(ghostPrefab, particleRenderer.gameObject).ToString()))
                jsonGhostJson.particlePaths.Add(Utils.GetTransformPath(ghostPrefab, particleRenderer.gameObject).ToString());
        }
        TrailRenderer[] trailRenderers = ghostPrefab.GetComponentsInChildren<TrailRenderer>();
        foreach (TrailRenderer trailRenderer in trailRenderers)
        {
            if(!jsonGhostJson.trailPaths.Contains(Utils.GetTransformPath(ghostPrefab, trailRenderer.gameObject).ToString()))
                jsonGhostJson.trailPaths.Add(Utils.GetTransformPath(ghostPrefab, trailRenderer.gameObject).ToString());
        }
    }
}