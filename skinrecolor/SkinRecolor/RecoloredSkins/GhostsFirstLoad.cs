using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HG.GeneralSerializer;
using RoR2;
using RoR2.Projectile;
using SkinRecolor.SkinVFXHooks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SkinRecolor.RecoloredSkins;

public class GhostsFirstLoad
{
    public static async Task HandleFirstLoad(RecoloredSkinJson recoloredSkinJson, bool dontAdd, GameObject body, SkinDef recoloredSkinDef)
    {
        recoloredSkinJson.vfxOverrides = [];
        recoloredSkinJson.projectileGhosts = [];
        
        if (SkinVFXManager.survivorHooks.TryGetValue(recoloredSkinJson.baseSkinBody, out SurvivorHookBase survivorHook))
        {
            survivorHook.SpecificReskinnedObjects(recoloredSkinJson);
        }
        
        Log.Warning($"finding new vfx/ghosts for skin {recoloredSkinJson.skinName}");

        //get internal name to look up escs with 
        string internalName = "";
        GenericSkill[] skills = body.GetComponents<GenericSkill>();
        foreach (var skill in skills)
        {
            if (skill.skillFamily.variants[0].skillDef.skillNameToken.Contains("PASSIVE")) continue;
            
            if (skill.skillFamily.variants[0].skillDef.activationState.typeName != "")
            {
                internalName = skill.skillFamily.variants[0].skillDef.activationState.typeName.Split(".")[1];
                if (internalName != "") break;
            }
        }
        if (internalName == "")
        {
            Log.Warning($"couldnt find interanl name for projectile ghosts .,,. on {recoloredSkinJson.skinName}");
            return;
        }
    
        //get all objects relating to survivor
        foreach (var esc in RoR2.ContentManagement.ContentManager.entityStateConfigurations)
        {
            if(esc.targetType.assemblyQualifiedName == null) continue;
            if (!esc.targetType.assemblyQualifiedName.Contains(internalName)) continue;
        
            foreach (var field in esc.serializedFieldsCollection.serializedFields)
            {
                GameObject effectObject = null;
                
                if (field.fieldValue.stringValue.Length == 32)
                {
                    try
                    {
                        effectObject = await Addressables.LoadAssetAsync<GameObject>(field.fieldValue.stringValue).Task;
                    }
                    catch (Exception e)
                    {
                        Log.Error($"uh oh 32 line string value wasnt ghostGUID {field.fieldValue.stringValue}");
                        if (field.fieldValue.objectValue is GameObject value)
                            effectObject = value;
                    }
                }
                else if (field.fieldValue.objectValue is GameObject value)
                {
                    effectObject = value;
                }
                
                Log.Debug($"checking effect {effectObject}");
                await CheckEffectsAndProjectiles(recoloredSkinJson, recoloredSkinDef, effectObject, esc, field, null, [], [..recoloredSkinJson.vfxOverrides]);
            }
        }
    }

    public static async Task CheckEffectsAndProjectiles(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, GameObject effectObject, EntityStateConfiguration esc, SerializedField field, JsonBase parentOverride, List<SubPrefab> totalSubPrefabs, List<JsonBase> totalOverrides, Type parentType = null, string parentName = null)
    {
        if (!effectObject) return;
        
        List<SubPrefab> instanceSubPrefabs = [];
        List<JsonBase> instanceOverrides = [];
        bool ensuredOnBase = false;
        
        //check projectile ghosts .,.,., 
        if (effectObject.TryGetComponent(out ProjectileController projectileController))
        {
            Log.Message(effectObject.name + $" had a projectile controller ,2.,parent override ?  {parentOverride != null}");
            if (parentOverride != null)
            {
                Log.Debug($"parent override: {parentType}");
                Log.Debug($"parent override: {parentName}");
            }
            await addProjectileGhost(parentType, parentName, effectObject, projectileController, true);
            await addEffect(parentType, parentName, effectObject);
        }

        if (effectObject.TryGetComponent(out ProjectileImpactExplosion projectileImpactExplosion) && projectileImpactExplosion?.childrenProjectilePrefab?.TryGetComponent(out ProjectileController childProjectileController) == true)
        {
            if (projectileController)
            {
                foreach (var jsonBase in instanceOverrides)
                {
                    if (jsonBase is ProjectileGhostJson projectileGhostJson)
                    {
                        Log.Message(effectObject.name + $" had a child projectile prefab ,., parent override ? {jsonBase != null}");
                        await CheckEffectsAndProjectiles(recoloredSkinJson, recoloredSkinDef, effectObject, esc, field, projectileGhostJson, totalSubPrefabs, totalOverrides, typeof(ProjectileImpactExplosion), nameof(projectileImpactExplosion.childrenProjectilePrefab));
                        break;
                    }
                }
            }
        }
        
        foreach (var baseSurvHooks in SkinVFXManager.survivorHooks.Values)
        {
            if(baseSurvHooks.survName != recoloredSkinJson.baseSkinBody) continue;
            
            var components = baseSurvHooks.GetSurvivorSpecificComponents(effectObject);

            foreach (var survSpecific in components)
            {
                Log.Warning($"found surv specific {survSpecific.varName}");
                await addEffect(survSpecific.type, survSpecific.varName, survSpecific.gameObject);
            }
        }
        
        if (effectObject.TryGetComponent(out EffectComponent effectComponent))
        {
            await addEffect(typeof(EffectComponent), "SELFGAMEOBJECT", effectObject, true);
        }
        
        if (effectObject.TryGetComponent(out VFXAttributes vfxAttributes))
        {
            await addEffect(typeof(VFXAttributes), "SELFGAMEOBJECT", effectObject, true);
        }
        
        if (effectObject.TryGetComponent(out EffectManagerHelper effectManagerHelper))
        {
            await addEffect(typeof(EffectManagerHelper), "SELFGAMEOBJECT", effectObject, true);
        }
        if (effectObject.TryGetComponent(out TrailRenderer trailRenderer))
        {
            await addEffect(typeof(TrailRenderer), "SELFGAMEOBJECT", effectObject, true);
        }
        
        if (effectObject.TryGetComponent(out ProjectileSingleTargetImpact projectileSingleTargetImpact) && projectileSingleTargetImpact.impactEffect)
        {
            await addEffect(typeof(ProjectileSingleTargetImpact), nameof(projectileSingleTargetImpact.impactEffect), projectileSingleTargetImpact.impactEffect);
        }
        
        if (effectObject.TryGetComponent(out ProjectileDotZone projectileDotZone) && projectileDotZone.impactEffect)
        {
            await addEffect(typeof(ProjectileDotZone), nameof(projectileDotZone.impactEffect), projectileDotZone.impactEffect);
        }
        
        if (effectObject.TryGetComponent(out ProjectileOverlapAttack projectileOverlapAttack) && projectileOverlapAttack.impactEffect)
        {
            await addEffect(typeof(ProjectileOverlapAttack), nameof(projectileOverlapAttack.impactEffect), projectileOverlapAttack.impactEffect);
        }
        
        if (effectObject.TryGetComponent(out RoR2.Orbs.OrbEffect orbEffect))
        {
            if(orbEffect.startEffect)
                await addEffect(typeof(RoR2.Orbs.OrbEffect), nameof(orbEffect.startEffect), orbEffect.startEffect);
            if(orbEffect.endEffect)
                await addEffect(typeof(RoR2.Orbs.OrbEffect), nameof(orbEffect.endEffect), orbEffect.endEffect);
        }
        
        if (projectileImpactExplosion)
        {
            if (projectileImpactExplosion.impactEffect)
            {
                await addEffect(typeof(ProjectileImpactExplosion), nameof(projectileImpactExplosion.impactEffect), projectileImpactExplosion.impactEffect);
            }
            if (projectileImpactExplosion.childrenProjectilePrefab)
            {
                await addEffect(typeof(ProjectileImpactExplosion), nameof(projectileImpactExplosion.childrenProjectilePrefab), projectileImpactExplosion.childrenProjectilePrefab);
            }
        }
        
       
        
        
        
        return;
        
        async Task addEffect(Type type, string varName, GameObject prefabObject, bool ensureOnBase = false)
        {
            var subPrefab = new SubPrefab()
            {
                hasEffectData = prefabObject.GetComponent<EffectComponent>(),
                varName = varName,
                varType = type,
                fieldOrProperty = varType.field,
            };
            
            var vfxOverride = new VFXOverrideJson()
            {
                configurationType = esc?.targetType != null ? esc.targetType.assemblyQualifiedName : null,
                fieldName = field.fieldName,
                guid = prefabObject.TryGetComponent<EffectComponent>(out var effectComponent)
                    ? EffectCatalog.GetEffectDef(effectComponent.effectIndex)?._prefabAssetOrDirectReference?.address?.AssetGUID
                    : null,
                subPrefabs = [],
                typesAndPaths = []
            };
            
            AddSupportedTypes(prefabObject, vfxOverride.typesAndPaths);

            if (ensureOnBase)
            {
                if (!ensuredOnBase)
                {
                    if (parentOverride != null)
                    {
                        if (parentOverride is VFXOverrideJson vfxOverrideJson)
                        {
                            if (vfxOverrideJson.typesAndPaths.Count == 0)
                            {
                                Log.Warning($"ensuring on base for prefab {prefabObject}");
                                AddSupportedTypes(prefabObject, vfxOverride.typesAndPaths);
                            }
                        }
                    }
                    else
                    {
                        foreach (var existingOverride in totalOverrides)
                        {
                            if(existingOverride is not VFXOverrideJson existingVfx) continue;
                            if (!existingVfx.Equals(vfxOverride)) continue;
                    
                            Log.Debug($"already had vfx override in {vfxOverride.guid} z!");
                            return;
                        }
                
                        Log.Debug($"adding vfxOverride {prefabObject} {vfxOverride.configurationType} {vfxOverride.fieldName} z!");
                        totalOverrides.Add(vfxOverride);
                        recoloredSkinJson.vfxOverrides.Add(vfxOverride);
                        instanceOverrides.Add(vfxOverride);
                    }

                    ensuredOnBase = true;
                }
            }
            else
            {
                if (parentOverride != null)
                {
                    subPrefab.prefabName = prefabObject.name;
                    subPrefab.vfxOverrideJson = vfxOverride;
                
                    //make sure the override/subprefab doesnt already exist
                    foreach (var existingSubPrefab in totalSubPrefabs)
                    {
                        if (!existingSubPrefab.Equals(subPrefab)) continue;
                    
                        Log.Debug($"already had vfx override in subprefab {existingSubPrefab.prefabName} z!");
                        return;
                    }
                
                    totalSubPrefabs.Add(subPrefab);
                    totalOverrides.Add(vfxOverride);
                    parentOverride.subPrefabs.Add(subPrefab);
                    instanceSubPrefabs.Add(subPrefab);
                }
                else
                {
                    //make sure the override doesnt already exist
                    foreach (var existingOverride in totalOverrides)
                    {
                        if(existingOverride is not VFXOverrideJson existingVfx) continue;
                        if (!existingVfx.Equals(vfxOverride)) continue;
                    
                        Log.Debug($"already had vfx override in {vfxOverride.guid} z!");
                        return;
                    }
                
                    Log.Debug($"adding vfxOverride {prefabObject} {vfxOverride.configurationType} {vfxOverride.fieldName} z!");
                    totalOverrides.Add(vfxOverride);
                    recoloredSkinJson.vfxOverrides.Add(vfxOverride);
                    instanceOverrides.Add(vfxOverride);
                }
            
                if (effectObject != prefabObject)
                {
                    Log.Debug($"iterating again from vfx ,., using {prefabObject.name } from {effectObject?.name} as a base now !! ");
                    await CheckEffectsAndProjectiles(recoloredSkinJson, recoloredSkinDef, prefabObject, esc, field, vfxOverride, totalSubPrefabs, totalOverrides, type, varName);
                }
            }
            
        }
        
        async Task addProjectileGhost(Type type, string varName, GameObject prefabObject, ProjectileController projectileControllerGhost, bool ensureOnBase = false)
        {
            Log.Debug($"adding a projectile ghost to {effectObject} from controller {projectileControllerGhost} ensure on bae ? {ensureOnBase} parent override ? {parentOverride != null}");
            
            var subPrefab = new SubPrefab()
            {
                hasEffectData = true,
                varName = varName,
                varType = type,
                fieldOrProperty = varType.field,
            };
            
            var projectileGhost = new ProjectileGhostJson()
            {
                configurationType = esc.targetType.assemblyQualifiedName,
                fieldName = field.fieldName,
                ghostGUID = !projectileControllerGhost.ghostPrefab ? projectileControllerGhost.ghostPrefabAddress?.AssetGUID : null,
                subPrefabs = [],
                typesAndPaths = []
            };

            GameObject ghostObject;
            if (projectileGhost.ghostGUID != null)
            {
                ghostObject = await Addressables.LoadAssetAsync<GameObject>(projectileGhost.ghostGUID).Task;
            }
            else
            {
                ghostObject = projectileControllerGhost.ghostPrefab;
            }

            if (!ghostObject)
            {
                Log.Error($"prefab {prefabObject?.name} has no ghost prefab assigned !!");
                return;
            }

            subPrefab.hasEffectData = ghostObject.GetComponent<EffectComponent>();
            subPrefab.projectileGhostJson = projectileGhost;
            
            AddSupportedTypes(ghostObject, projectileGhost.typesAndPaths);
            
            if (ensureOnBase)
            {
                if (!ensuredOnBase)
                {
                    if (parentOverride != null && parentType != null && parentName != null)
                    {
                        AddSupportedTypes(prefabObject, projectileGhost.typesAndPaths);
                        totalSubPrefabs.Add(subPrefab);
                        totalOverrides.Add(projectileGhost);
                        parentOverride.subPrefabs.Add(subPrefab);
                        instanceSubPrefabs.Add(subPrefab);
                    }
                    else
                    {
                        //make sure the override doesnt already exist
                        foreach (var existingOverride in totalOverrides)
                        {
                            if(existingOverride is not ProjectileGhostJson existingGhost) continue;
                            if (existingGhost.Equals(projectileGhost) || existingGhost.configurationType == projectileGhost.configurationType && existingGhost.fieldName == field.fieldName)
                            {
                                Log.Debug($"already had ghost in {projectileGhost.ghostGUID} z! adding as a subprefab to that instead ,.,. ");
                                Log.Debug($"had parent override {parentOverride != null}");
                                
                                totalSubPrefabs.Add(subPrefab);
                                totalOverrides.Add(projectileGhost);
                                existingGhost.subPrefabs.Add(subPrefab);
                                instanceSubPrefabs.Add(subPrefab);

                                return;
                            }
                        }
                
                        Log.Debug($"adding projectile ghost {prefabObject} {projectileGhost.configurationType} {projectileGhost.fieldName} z!");
                        totalOverrides.Add(projectileGhost);
                        recoloredSkinJson.projectileGhosts.Add(projectileGhost);
                        instanceOverrides.Add(projectileGhost);
                    }

                    ensuredOnBase = true;
                }
            }
            else
            {
                if (parentOverride != null)
                {
                    subPrefab.projectileGhostJson = projectileGhost;
                
                    //make sure the override/subprefab doesnt already exist
                    foreach (var existingSubPrefab in totalSubPrefabs)
                    {
                        if (!existingSubPrefab.Equals(subPrefab)) continue;
                    
                        Log.Debug($"already had projectile ghost override in subprefab {existingSubPrefab.prefabName} z!");
                        return;
                    }
                
                    Log.Debug($"adding subprefab {prefabObject} to parent {parentOverride.configurationType} {parentOverride.fieldName}");
                    totalSubPrefabs.Add(subPrefab);
                    totalOverrides.Add(projectileGhost);
                    parentOverride.subPrefabs.Add(subPrefab);
                    instanceSubPrefabs.Add(subPrefab);
                }
                else
                {
                    //make sure the override doesnt already exist
                    foreach (var existingOverride in totalOverrides)
                    {
                        if(existingOverride is not ProjectileGhostJson existingGhost) continue;
                        if (!existingGhost.Equals(projectileGhost)) continue;
                    
                        Log.Debug($"already had ghost in {projectileGhost.ghostGUID} z!");
                        return;
                    }
                
                    Log.Debug($"adding ghost realllll {prefabObject}");
                    totalOverrides.Add(projectileGhost);
                    recoloredSkinJson.projectileGhosts.Add(projectileGhost);
                    instanceOverrides.Add(projectileGhost);
                }

                if (ghostObject != prefabObject)
                {
                    Log.Debug($"iterating again from ghost ,., using {ghostObject?.name} as a base now !! ");
                    await CheckEffectsAndProjectiles(recoloredSkinJson, recoloredSkinDef, ghostObject, esc, field, projectileGhost, totalSubPrefabs, totalOverrides, type, varName);
                }
            }
        }
    }
    
    
    
    
    
    
    
    
    
    public static void AddSupportedTypes(GameObject gameObject, List<TypeToPath> typesAndPaths)
    {
        if (!gameObject)
        {
            Log.Error("couldnt get game object when trying to add supported types !!");
            return;
        }
        
        Log.Debug($"looking for supported types on {gameObject} ,,");
        AttemptAddTypeAndPath<Light>(gameObject, typesAndPaths);
        Log.Debug($"looking for supported types on {gameObject} {typesAndPaths.Count} ,,");
        AttemptAddTypeAndPath<MeshRenderer>(gameObject, typesAndPaths);
        Log.Debug($"looking for supported types on {gameObject} {typesAndPaths.Count} ,,");
        AttemptAddTypeAndPath<SkinnedMeshRenderer>(gameObject, typesAndPaths);
        Log.Debug($"looking for supported types on {gameObject} {typesAndPaths.Count} ,,");
        AttemptAddTypeAndPath<ParticleSystem>(gameObject, typesAndPaths);
        Log.Debug($"looking for supported types on {gameObject} {typesAndPaths.Count} ,,");
        AttemptAddTypeAndPath<ParticleSystemRenderer>(gameObject, typesAndPaths);
        Log.Debug($"looking for supported types on {gameObject} {typesAndPaths.Count} ,,");
        AttemptAddTypeAndPath<TrailRenderer>(gameObject, typesAndPaths);
        Log.Debug($"looking for supported types on {gameObject} {typesAndPaths.Count} ,,");
        AttemptAddTypeAndPath<LineRenderer>(gameObject, typesAndPaths);
        Log.Debug($"looking for supported types on {gameObject} {typesAndPaths.Count} ,,");
    }
    
    private static void AttemptAddTypeAndPath<T>(GameObject gameObject, List<TypeToPath> paths)
    {
        var components = gameObject.GetComponentsInChildren<T>(true);
        foreach (T component in components)
        {
            SaveComponentAndPathLocation(component, true);
        }
        
        components = gameObject.GetComponents<T>();
        foreach (T component in components)
        {
            SaveComponentAndPathLocation(component, false);
        }

        return;

        void SaveComponentAndPathLocation(T component, bool isChild)
        {
            var behavior = component as Component;
            if (!behavior)
            {
                Log.Error($"component {component.GetType()} was null on {gameObject}");
                return;
            }
            string path = Utils.GetTransformPath(gameObject, behavior.gameObject).ToString();
            
            bool found = false;
            foreach (var typeToPath in paths)
            {
                if (typeToPath.path == path && typeToPath.isChild == isChild && typeToPath.type == typeof(T))
                {
                    Log.Debug($"found subprefab with same chjild path and varType on {gameObject}");
                    found = true;
                    break;
                };
                
            }
            if(found)
                return;
            
            paths.Add(new TypeToPath(typeof(T), path, isChild));
        }
    }
}