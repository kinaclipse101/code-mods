/*using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HG.GeneralSerializer;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Projectile;
using SkinRecolor.RecoloredSkins;
using SkinRecolor.SkinVFXHooks;
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
            
            Log.Warning($"finding new vfx/ghosts for skin {recoloredSkinJson.skinName}");

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
        
            //Log.Debug($"looking for {internalName}");
            foreach (var esc in RoR2.ContentManagement.ContentManager.entityStateConfigurations)
            {
                if(esc.targetType.assemblyQualifiedName == null) continue;
                if (!esc.targetType.assemblyQualifiedName.Contains(internalName)) continue;
            
                foreach (var field in esc.serializedFieldsCollection.serializedFields)
                {
                    if (field.fieldValue.stringValue.Length == 32)
                    {
                        try
                        {
                            GameObject guidTest = await Addressables.LoadAssetAsync<GameObject>(field.fieldValue.stringValue).Task;
                            await CheckEffectComponentAndAttributes(recoloredSkinJson, recoloredSkinDef, guidTest, esc, field);
                            //await CheckProjectileController(recoloredSkinJson, recoloredSkinDef, guidTest, esc, field);
                        }
                        catch (Exception e)
                        {
                            Log.Error($"uh oh 32 line string value wasnt ghostGUID {field.fieldValue.stringValue}");
                            if (field.fieldValue.objectValue as GameObject)
                            {
                                var gameObject = (GameObject)field.fieldValue.objectValue;
                                await CheckEffectComponentAndAttributes(recoloredSkinJson, recoloredSkinDef, gameObject, esc, field);
                                //await CheckProjectileController(recoloredSkinJson, recoloredSkinDef, gameObject, esc, field);
                            }
                        }
                    }
                    else if (field.fieldValue.objectValue as GameObject)
                    {
                        var gameObject = (GameObject)field.fieldValue.objectValue;
                        await CheckEffectComponentAndAttributes(recoloredSkinJson, recoloredSkinDef, gameObject, esc, field);
                        //await CheckProjectileController(recoloredSkinJson, recoloredSkinDef, gameObject, esc, field);
                    }
                }
            }
        }

        await SetProjectileGhostReplacements(recoloredSkinJson, dontAdd, recoloredSkinDef);
    }

    private static async Task SetProjectileGhostReplacements(RecoloredSkinJson recoloredSkinJson, bool dontAdd, SkinDef recoloredSkinDef)
    {
        recoloredSkinDef.skinDefParams.projectileGhostReplacements = [];
        foreach (var jsonGhost in recoloredSkinJson.projectileGhosts)
        {
            GameObject prefab = await TryGetPrefabFromESC(jsonGhost.configurationType, jsonGhost.controllerFieldName);
            
            await AddGhostReplacementFromGhostJson(recoloredSkinJson, recoloredSkinDef, jsonGhost, prefab);
            
            foreach (var subPrefab in jsonGhost.subPrefabs)
            {
                GameObject subPrefabBase = prefab;
                for (int j = 0; j < subPrefab.varType.Count; j++)
                {
                    Log.Debug($"subprefab iterate cound {j}");
                    subPrefabBase = subPrefab.fieldOrProperty[j] switch
                    {
                        varType.field => subPrefabBase.GetComponent(subPrefab.varType[j]).GetFieldValue<GameObject>(subPrefab.varName[j]),
                        varType.property => subPrefabBase.GetComponent(subPrefab.varType[j]).GetPropertyValue<GameObject>(subPrefab.varName[j])
                    };
                }
                
                Log.Debug($"found subprefab {subPrefabBase.name} on {prefab.name} {subPrefab.varName[0]} {subPrefab.varType[0]}");
                
                if (subPrefab.projectileGhostJson != null)
                {
                    await AddGhostReplacementFromGhostJson(recoloredSkinJson, recoloredSkinDef, subPrefab.projectileGhostJson, subPrefabBase);
                }

                //if (subPrefab.vfxOverrideJson != null)
                //{
                //    await VFX.AddVFXOverride(recoloredSkinJson, subPrefab.vfxOverrideJson, recoloredSkinDef, subPrefabBase, subPrefab);
                //}
            }
        }
    }

    private static async Task AddGhostReplacementFromGhostJson(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, ProjectileGhostJson jsonGhost, GameObject prefab)
    {
        GameObject origGhostPrefab = jsonGhost.ghostGUID != null ? await Addressables.LoadAssetAsync<GameObject>(jsonGhost.ghostGUID).Task : prefab.GetComponent<ProjectileController>().ghostPrefab;
        if (!origGhostPrefab)
        {
            Log.Error($"error while trying to add ghost replacement for {recoloredSkinJson.baseSkinBody} {jsonGhost.configurationType} {jsonGhost.controllerFieldName}");
            return;
        }
        
        GameObject ghostPrefab = RecoloredSkinContent.RecolorEffectPrefab(origGhostPrefab, $"jsonGhost_{origGhostPrefab.name}_{recoloredSkinJson.hue}{recoloredSkinJson.saturation}{recoloredSkinJson.value}_{recoloredSkinJson.skinName}", jsonGhost.typesAndPaths, recoloredSkinJson);

        SkinDefParams.ProjectileGhostReplacement replacement = new SkinDefParams.ProjectileGhostReplacement
        {
            projectilePrefab = prefab,
            projectileGhostReplacementPrefab = ghostPrefab
        };
        HG.ArrayUtils.ArrayAppend(ref recoloredSkinDef.skinDefParams.projectileGhostReplacements, replacement);
        Log.Debug($"added projectile ghost !! {origGhostPrefab}");
    }

    private static async Task CheckEffectComponentAndAttributes(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, GameObject gameObject, EntityStateConfiguration esc, SerializedField field, ProjectileGhostJson parentGhost = null, VFXOverrideJson parentVFX = null)
    {
        if (gameObject.TryGetComponent(out ProjectileController projectileController))
        {
            ProjectileGhostJson jsonGhostJson = new ProjectileGhostJson
            {
                typesAndPaths = [],
                configurationType = esc.targetType.assemblyQualifiedName,
                controllerFieldName = field.fieldName,
                subPrefabs = []
            };
            
            await CheckChildProjectiles(recoloredSkinJson, recoloredSkinDef, gameObject, esc, field, jsonGhostJson, null);
            //CheckOverlapAttacks(recoloredSkinJson, recoloredSkinDef, gameObject, esc, field, jsonGhostJson, parentSubPrefab);

            Log.Debug($"found projectile controller on {gameObject.name}");
            GameObject ghostPrefab;
            //modded ,..,
            if (projectileController.ghostPrefab)
            {
                Log.Warning("found deprecatedd ghost prefab " + projectileController.name);
                jsonGhostJson.ghostGUID = null;
                ghostPrefab = projectileController.ghostPrefab;
            }
            else
            {
                //Log.Debug($"default ghost ghostGUID {projectileController.ghostPrefabAddress?.AssetGUID}");
                jsonGhostJson.ghostGUID = projectileController.ghostPrefabAddress?.AssetGUID;
                ghostPrefab = await Addressables.LoadAssetAsync<GameObject>(jsonGhostJson.ghostGUID).Task;
            }
                        
            if (!ghostPrefab)
            {
                Log.Warning($"{gameObject.name} had no ghost prefab !!");
                return;
            }
            
            if (parentGhost != null)
            {
                parentGhost.subPrefabs.Add(new SubPrefab([], [], [], [], ghostPrefab.GetComponent<EffectComponent>()));
            }
                        
            AddSupportedTypes(ghostPrefab, jsonGhostJson.typesAndPaths);
            
            AddIfValidGhost(recoloredSkinJson, jsonGhostJson, gameObject, parentGhost, parentVFX);
            
            Log.Message($"added ghost {ghostPrefab.name} to {recoloredSkinJson.skinName}");
            
            VFXOverrideJson vfxOverrideJson = new VFXOverrideJson
            {
                typesAndPaths = [],
                subPrefabs = [],
                fieldName = field.fieldName,
                configurationType = esc.targetType.assemblyQualifiedName
            };

            if (!VFXOverridePathfinder(recoloredSkinJson, gameObject, vfxOverrideJson))
            {
                AddIfValidVFX(recoloredSkinJson, vfxOverrideJson, gameObject);
            };
            
            applyVfxAttributes(gameObject);
        };
        
        if (gameObject.TryGetComponent(out EffectComponent effectComponent))
        {
            VFXOverrideJson vfxOverrideJson = new VFXOverrideJson
            {
                typesAndPaths = [],
                subPrefabs = [],
                guid = EffectCatalog.GetEffectDef(effectComponent.effectIndex)?._prefabAssetOrDirectReference?.address?.AssetGUID,
                fieldName = field.fieldName,
                configurationType = esc.targetType.assemblyQualifiedName
            };
            
            if (vfxOverrideJson.guid == null)
            {
                Log.Warning($"effect ghostGUID was null fro {gameObject}");
                applyVfxAttributes(gameObject);
            }
            else
            {
                Log.Debug($"checking {gameObject} 1");
                if(VFXOverridePathfinder(recoloredSkinJson, gameObject, vfxOverrideJson, effectComponent)) return;

                AddIfValidVFX(recoloredSkinJson, vfxOverrideJson, gameObject);
            }
        }
        else if (gameObject.TryGetComponent(out VFXAttributes vfxAttributes))
        {
            applyVfxAttributes(gameObject);
        } 
        else if (gameObject.TryGetComponent(out EffectManagerHelper effectManagerHelper))
        {
            applyVfxAttributes(gameObject);
        }
        else if (gameObject.TryGetComponent(out TrailRenderer trailRenderer))
        {
            applyVfxAttributes(gameObject);
        }

        void applyVfxAttributes(GameObject specificObject)
        {
            VFXOverrideJson vfxOverrideJson = new VFXOverrideJson
            {
                typesAndPaths = [],
                subPrefabs = [],
                fieldName = field.fieldName,
                configurationType = esc.targetType.assemblyQualifiedName
            };
            
            Log.Debug($"checking {specificObject} vfxAttributes");
            if(VFXOverridePathfinder(recoloredSkinJson, gameObject, vfxOverrideJson)) return;

            AddIfValidVFX(recoloredSkinJson, vfxOverrideJson, specificObject);
            Log.Warning($"found vfxattributes on an object {specificObject} without effect data .,,. mmmmm adding it anyway ,,.");
        }
    }

    public static bool VFXOverridePathfinder(RecoloredSkinJson recoloredSkinJson, GameObject gameObject, VFXOverrideJson vfxOverrideJson, EffectComponent effectComponent = null, SubPrefab subPrefab = null)
    {
        if (!effectComponent)
        {
            effectComponent = gameObject.GetComponent<EffectComponent>();
        }

        string guid = null;
        if (effectComponent && effectComponent.effectIndex != EffectIndex.Invalid)
        {
            guid = EffectCatalog.GetEffectDef(effectComponent.effectIndex)?._prefabAssetOrDirectReference?.address?.AssetGUID;
            vfxOverrideJson.guid = guid;
        }
        
        /*foreach (var ghostJson in recoloredSkinJson.projectileGhosts)
        {
            if (!HasGuidInSubPrefabs(recoloredSkinJson, ghostJson, guid, vfxOverrideJson, subPrefab, null, null))
            {
                return true;
            }
        }
            
        foreach (var ghostJson in recoloredSkinJson.vfxOverrides)
        {
            if (!HasGuidInSubPrefabs(recoloredSkinJson, ghostJson, guid, vfxOverrideJson, subPrefab))
            {
                return true;
            }
        }#1#

        AddSupportedTypes(gameObject, vfxOverrideJson.typesAndPaths);

        return false;
    }

    public static bool HasGuidInSubPrefabs(RecoloredSkinJson recoloredSkinJson, ProjectileGhostJson ghostJson, string guid, VFXOverrideJson newVfx, SubPrefab targetSubPrefab = null, SubPrefab currentSubPrefab = null, ProjectileGhostJson newGhost = null)
    {
        /*if (targetSubPrefab != null && currentSubPrefab != null && newGhost != null)
        {
            if (currentSubPrefab.prefabName == targetSubPrefab.prefabName &&
                currentSubPrefab.varType == targetSubPrefab.varType &&
                currentSubPrefab.varName == targetSubPrefab.varName &&
                currentSubPrefab.fieldOrProperty == targetSubPrefab.fieldOrProperty &&
                (currentSubPrefab.newGhost.Equals(targetSubPrefab.newGhost) || 
                 currentSubPrefab.newGhost?.ghostGUID == targetSubPrefab.newGhost?.ghostGUID && targetSubPrefab.newGhost?.ghostGUID != null))
            {
                Log.Debug($"found matching subprefab ghost {currentSubPrefab.prefabName}");
                return true;
            }
        }#1#

        if (newGhost != null)
        {
            if (ghostJson.Equals(newGhost))
            {
                Log.Warning($"found existing guid on projectile ghost {ghostJson.ghostGUID}");
                return true;
            }
        }
        
        foreach (var subPrefab in ghostJson.subPrefabs)
        {
            if (subPrefab.vfxOverrideJson != null && HasGuidInSubPrefabs(recoloredSkinJson, subPrefab.vfxOverrideJson, guid, newVfx, targetSubPrefab, subPrefab, newGhost))
            {
                return true;
            } 
            
            if (subPrefab.projectileGhostJson != null && HasGuidInSubPrefabs(recoloredSkinJson, subPrefab.projectileGhostJson, guid, newVfx, targetSubPrefab, subPrefab, newGhost))
            {
                return true;
            }
        }

        return false;
    }
    
    public static bool HasGuidInSubPrefabs(RecoloredSkinJson recoloredSkinJson, VFXOverrideJson vfxJson, string guid, VFXOverrideJson newVfx, SubPrefab targetSubPrefab = null, SubPrefab currentSubPrefab = null, ProjectileGhostJson newGhost = null)
    {
        /*if (targetSubPrefab != null && currentSubPrefab != null && newVfx != null)
        {
            if (currentSubPrefab.prefabName == targetSubPrefab.prefabName &&
                currentSubPrefab.varType == targetSubPrefab.varType &&
                currentSubPrefab.varName == targetSubPrefab.varName &&
                currentSubPrefab.fieldOrProperty == targetSubPrefab.fieldOrProperty &&
                (currentSubPrefab.vfxOverrideJson.Equals(newVfx) || 
                 currentSubPrefab.newGhost?.ghostGUID == newVfx?.guid && newVfx?.guid != null))
            {
                Log.Debug($"found matching subprefab {currentSubPrefab.prefabName}");
                return true;
            }
        }#1#
        
        if (newVfx != null)
        {
            if (newVfx.Equals(vfxJson))
            {
                Log.Debug($"found matching vfx {vfxJson.fieldName}");
                return true;
            }
        }
        
        foreach (var subPrefab in vfxJson.subPrefabs)
        {
            if (subPrefab.vfxOverrideJson != null && HasGuidInSubPrefabs(recoloredSkinJson, subPrefab.vfxOverrideJson, guid, newVfx, targetSubPrefab, subPrefab, newGhost))
            {
                return true;
            } 
            
            if (subPrefab.projectileGhostJson != null && HasGuidInSubPrefabs(recoloredSkinJson, subPrefab.projectileGhostJson, guid, newVfx, targetSubPrefab, subPrefab, newGhost))
            {
                return true;
            }
        }
        
        return false;
    }

    private static void AttemptAddTypeAndPath<T>(List<TypeToPath> paths, GameObject gameObject)
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

    private static async Task CheckProjectileController(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, GameObject gameObject, EntityStateConfiguration esc, SerializedField field, SubPrefab prefabNesting = null, ProjectileGhostJson parentGhost = null)
    {
        
        
        
    }

    private static void AddSupportedTypes(GameObject gameObject, List<TypeToPath> typesAndPaths)
    {
        Log.Debug($"looking for supported types on {gameObject} ,,");
        AttemptAddTypeAndPath<Light>(typesAndPaths, gameObject);
        Log.Debug($"looking for supported types on {gameObject} {typesAndPaths.Count} ,,");
        AttemptAddTypeAndPath<MeshRenderer>(typesAndPaths, gameObject);
        Log.Debug($"looking for supported types on {gameObject} {typesAndPaths.Count} ,,");
        AttemptAddTypeAndPath<SkinnedMeshRenderer>(typesAndPaths, gameObject);
        Log.Debug($"looking for supported types on {gameObject} {typesAndPaths.Count} ,,");
        AttemptAddTypeAndPath<ParticleSystem>(typesAndPaths, gameObject);
        Log.Debug($"looking for supported types on {gameObject} {typesAndPaths.Count} ,,");
        AttemptAddTypeAndPath<ParticleSystemRenderer>(typesAndPaths, gameObject);
        Log.Debug($"looking for supported types on {gameObject} {typesAndPaths.Count} ,,");
        AttemptAddTypeAndPath<TrailRenderer>(typesAndPaths, gameObject);
        Log.Debug($"looking for supported types on {gameObject} {typesAndPaths.Count} ,,");
        AttemptAddTypeAndPath<LineRenderer>(typesAndPaths, gameObject);
        Log.Debug($"looking for supported types on {gameObject} {typesAndPaths.Count} ,,");
    }

    private static async Task CheckChildProjectiles(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, GameObject gameObject, EntityStateConfiguration esc, SerializedField field, ProjectileGhostJson parentGhostJson, VFXOverrideJson parentVFXJson)
    {
        
        return;

        async Task addEffectSubPrefab(Type type, string varName, GameObject effectObject, ProjectileGhostJson newGhost = null)
        {
            EffectComponent effectComponent = effectObject.GetComponent<EffectComponent>();

            SubPrefab subPrefab = new SubPrefab([], [], [], [], effectComponent);
            if (parentGhostJson != null || parentVFXJson != null)
            {
                HandleSubPrefab(type, subPrefab, effectObject.name, varName, varType.field);

                if (newGhost != null)
                {
                    subPrefab.projectileGhostJson = newGhost;
                    await CheckEffectComponentAndAttributes(recoloredSkinJson, recoloredSkinDef, projectileImpactExplosion.childrenProjectilePrefab, esc, field, subPrefab.projectileGhostJson, parentVFXJson);
                }
                else
                {
                    subPrefab.vfxOverrideJson = new VFXOverrideJson
                    {
                        typesAndPaths = [],
                        subPrefabs = [],
                        guid = effectComponent ? EffectCatalog.GetEffectDef(effectComponent.effectIndex)?._prefabAssetOrDirectReference?.address?.AssetGUID : null,
                        configurationType = esc.targetType.assemblyQualifiedName,
                        fieldName = field.fieldName,
                    };
                }
            }


            Log.Message( $"{gameObject.name} had a {type} {varName} prefab ,., {effectObject}");
            if (newGhost != null)
            {
                AddIfValidGhost(recoloredSkinJson, subPrefab.projectileGhostJson, projectileImpactExplosion.childrenProjectilePrefab, parentGhostJson, parentVFXJson);
                //parentGhostJson.subPrefabs.Add(subPrefab);
            }
            else
            {
                await CheckChildProjectiles(recoloredSkinJson, recoloredSkinDef, effectObject, esc, field, parentGhostJson, subPrefab.vfxOverrideJson);
                if (!VFXOverridePathfinder(recoloredSkinJson, effectObject, subPrefab.vfxOverrideJson, effectComponent))
                {
                    AddIfValidVFX(recoloredSkinJson, subPrefab.vfxOverrideJson, effectObject, parentGhostJson, parentVFXJson);
                };  
            }
                                   
            //AddSupportedTypes(effectObject, subPrefab.vfxOverrideJson.typesAndPaths);

            Log.Debug($"{gameObject.name} added to ghgost dfg fdg {type} {varName} prefab ,., {effectObject}");
            //AddIfValidVFX();
            //parentGhostJson.subPrefabs.Add(subPrefab);
        }
    }

    private static void HandleSubPrefab(Type subType, SubPrefab subPrefab, string subPrefabName, string variableName, varType variableType)
    {
        /*if (nestedPrefab != null)
        {
            foreach (var varType in nestedPrefab.varType)
            {
                subPrefab.varType.Add(varType);
            }
            foreach (var varName in nestedPrefab.varName)
            {
                subPrefab.varName.Add(varName);
            }
            foreach (var prefabName in nestedPrefab.prefabName)
            {
                subPrefab.prefabName.Add(prefabName);
            }
            foreach (var fieldOrProperty in nestedPrefab.fieldOrProperty)
            {
                subPrefab.fieldOrProperty.Add(fieldOrProperty);
            }
        }#1#
        subPrefab.varType.Add(subType);
        subPrefab.varName.Add(variableName);
        subPrefab.prefabName.Add(subPrefabName);
        subPrefab.fieldOrProperty.Add(variableType);
    }

    public static void AddIfValidVFX(RecoloredSkinJson skinJson, VFXOverrideJson vfxOverrideJson, GameObject gameObject, ProjectileGhostJson parentGhost, VFXOverrideJson parentVFXJson)
    {
        if (vfxOverrideJson.typesAndPaths.Count != 0)
        {
            Log.Debug($"added {gameObject} vfx valid !!");
            skinJson.vfxOverrides.Add(vfxOverrideJson);
        }
        else
        {
            Log.Warning($"vfx {gameObject.name} has nothing to override !!");
        }
    }
    
    public static void AddIfValidGhost(RecoloredSkinJson skinJson, ProjectileGhostJson ghostJson, GameObject gameObject, ProjectileGhostJson parentGhost, VFXOverrideJson parentVFXJson)
    {
        if (ghostJson.typesAndPaths.Count != 0)
        {
            /*foreach (var instanceGhostJson in skinJson.projectileGhosts)
            {
                if (HasGuidInSubPrefabs(skinJson, instanceGhostJson, instanceGhostJson.ghostGUID, null, null, null, ghostJson))
                {
                    return;
                }
            }
            foreach (var instanceVfxJson in skinJson.vfxOverrides)
            {
                if (HasGuidInSubPrefabs(skinJson, instanceVfxJson, null, null, null, null, ghostJson))
                {
                    return;
                }
            }#1#

            if (parentGhost != null)
            {
                SubPrefab subPrefab = new SubPrefab([], [], [], [], gameObject?.GetComponent<EffectComponent>());
                parentGhost.subPrefabs.Add(subPrefab);
            }
            else if (parentVFXJson != null)
            {
                SubPrefab subPrefab = new SubPrefab([], [], [], [], gameObject?.GetComponent<EffectComponent>());
                parentVFXJson.subPrefabs.Add(subPrefab);
            }
            else
            {
                skinJson.projectileGhosts.Add(ghostJson);
            }
        }
        else
        {
            Log.Warning($"projectile {gameObject.name} has nothing to override !!");
        }
    }
}*/