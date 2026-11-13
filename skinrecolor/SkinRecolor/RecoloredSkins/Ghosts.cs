using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HG.GeneralSerializer;
using R2API;
using R2API.Utils;
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
                if (!esc.targetType.assemblyQualifiedName.ToString().Contains(internalName)) continue;
            
                foreach (var field in esc.serializedFieldsCollection.serializedFields)
                {
                    if (field.fieldValue.stringValue.Length == 32)
                    {
                        try
                        {
                            GameObject guidTest = await Addressables.LoadAssetAsync<GameObject>(field.fieldValue.stringValue).Task;
                            CheckEffectComponentAndAttributes(recoloredSkinJson, recoloredSkinDef, guidTest, esc, field);
                            await CheckProjectileController(recoloredSkinJson, recoloredSkinDef, guidTest, esc, field);
                        }
                        catch (Exception e)
                        {
                            Log.Error($"uh oh 32 line string value wasnt guid {field.fieldValue.stringValue}");
                            if (field.fieldValue.objectValue as GameObject)
                            {
                                var gameObject = (GameObject)field.fieldValue.objectValue;
                                CheckEffectComponentAndAttributes(recoloredSkinJson, recoloredSkinDef, gameObject, esc, field);
                                await CheckProjectileController(recoloredSkinJson, recoloredSkinDef, gameObject, esc, field);
                            }
                        }
                    }
                    else if (field.fieldValue.objectValue as GameObject)
                    {
                        var gameObject = (GameObject)field.fieldValue.objectValue;
                        CheckEffectComponentAndAttributes(recoloredSkinJson, recoloredSkinDef, gameObject, esc, field);
                        await CheckProjectileController(recoloredSkinJson, recoloredSkinDef, gameObject, esc, field);
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
            var prefab = await TryGetPrefabFromESC(jsonGhost.configurationType, jsonGhost.controllerFieldName);

            foreach (var subPrefab in jsonGhost.subPrefabs)
            {
                GameObject subPrefabBase = prefab;
                for (int j = 0; j < subPrefab.type.Count; j++)
                {
                    subPrefabBase = subPrefab.varType[j] switch
                    {
                        varType.field => subPrefabBase.GetComponent(subPrefab.type[j]).GetFieldValue<GameObject>(subPrefab.varName[j]),
                        varType.property => subPrefabBase.GetComponent(subPrefab.type[j]).GetPropertyValue<GameObject>(subPrefab.varName[j])
                    };
                }
                
                if (subPrefab.projectileGhostJson != null)
                {
                    await AddGhostReplacementFromGhostJson(recoloredSkinJson, recoloredSkinDef, subPrefab.projectileGhostJson, subPrefabBase);
                }

                if (subPrefab.vfxOverrideJson != null)
                {
                    await VFX.AddVFXOverride(recoloredSkinJson, subPrefab.vfxOverrideJson, recoloredSkinDef, true);
                }
            }

            await AddGhostReplacementFromGhostJson(recoloredSkinJson, recoloredSkinDef, jsonGhost, prefab);
        }
    }

    private static async Task AddGhostReplacementFromGhostJson(RecoloredSkinJson recoloredSkinJson,
        SkinDef recoloredSkinDef, ProjectileGhostJson jsonGhost, GameObject prefab)
    {
        GameObject origGhostPrefab = jsonGhost.guid != null ? await Addressables.LoadAssetAsync<GameObject>(jsonGhost.guid).Task : prefab.GetComponent<ProjectileController>().ghostPrefab;
        GameObject ghostPrefab = origGhostPrefab.InstantiateClone($"jsonGhost_{origGhostPrefab.name}_{recoloredSkinJson.hue}{recoloredSkinJson.saturation}{recoloredSkinJson.value}_{recoloredSkinJson.skinName}", false);
                
        foreach (TypeToPath typeToPath in jsonGhost.typesAndPaths)
        {
            var objectTransform = typeToPath.isChild ? ghostPrefab.transform.Find(typeToPath.path) : ghostPrefab.transform;
            if (!objectTransform)
            {
                Log.Warning("umm bad something went bad " + typeToPath.path + " on " + ghostPrefab.name);
                continue;
            }
            
            var components = objectTransform.GetComponents(typeToPath.type);
            foreach (var component in components)
            {
                RecoloredSkinContent.RecolorComponentBehavior(recoloredSkinJson, component, typeToPath, objectTransform);
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

    public static async Task<GameObject> TryGetPrefabFromESC(string configType, string fieldName)
    {
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
            return prefab;
        }

        return prefab;
    }

    private static void CheckEffectComponentAndAttributes(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, GameObject gameObject, EntityStateConfiguration esc, SerializedField field)
    {
        if (gameObject.GetComponent<ProjectileController>()) return;
        
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
                Log.Warning($"effect guid was null fro {gameObject}");
                applyVfxAttributes();
            }
            else
            {
                if(VFXOverridePathfinder(recoloredSkinJson, recoloredSkinDef, gameObject, vfxOverrideJson, effectComponent)) return;

                AddIfValidVFX(recoloredSkinJson, vfxOverrideJson, gameObject);
            }
        }
        else if (gameObject.TryGetComponent(out VFXAttributes vfxAttributes))
        {
            applyVfxAttributes();
        } 
        else if (gameObject.TryGetComponent(out EffectManagerHelper effectManagerHelper))
        {
            applyVfxAttributes();
        }
        else if (gameObject.TryGetComponent(out TrailRenderer trailRenderer))
        {
            applyVfxAttributes();
        }

        void applyVfxAttributes()
        {
            Log.Warning($"found vfxattributes on an object {gameObject} without effect data .,,. mmmmm adding it anyway ,,.");
            VFXOverrideJson vfxOverrideJson = new VFXOverrideJson
            {
                typesAndPaths = [],
                subPrefabs = [],
                fieldName = field.fieldName,
                configurationType = esc.targetType.assemblyQualifiedName
            };
            
            if(VFXOverridePathfinder(recoloredSkinJson, recoloredSkinDef, gameObject, vfxOverrideJson)) return;

            AddIfValidVFX(recoloredSkinJson, vfxOverrideJson, gameObject);
        }
    }

    public static bool VFXOverridePathfinder(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, GameObject gameObject, VFXOverrideJson vfxOverrideJson, EffectComponent effectComponent = null)
    {
        if (effectComponent)
        {
            string guid = EffectCatalog.GetEffectDef(effectComponent.effectIndex)?._prefabAssetOrDirectReference?.address?.AssetGUID;
            if (guid != null)
            {
                bool exitEarly = false;
                foreach (var effect in recoloredSkinJson.vfxOverrides)
                {
                    if (effect.guid != guid) continue;
                    
                    exitEarly = true;
                    break;
                }
                if (exitEarly)
                    return true;
            }
        }
        
        AddSupportedTypes(gameObject, vfxOverrideJson.typesAndPaths);

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
                    found = true;
                    break;
                };
                
            }
            if(found)
                return;
            
            paths.Add(new TypeToPath(typeof(T), path, isChild));
        }
    }

    private static async Task CheckProjectileController(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, GameObject gameObject, EntityStateConfiguration esc, SerializedField field, SubPrefab prefabNesting = null)
    {
        if (!gameObject.TryGetComponent(out ProjectileController projectileController)) return;
    
        ProjectileGhostJson jsonGhostJson = new ProjectileGhostJson
        {
            typesAndPaths = [],
            configurationType = esc.targetType.assemblyQualifiedName,
            controllerFieldName = field.fieldName,
            subPrefabs = []
        };
        if (prefabNesting != null)
        {
            jsonGhostJson = prefabNesting.projectileGhostJson;
        }
        
        await CheckChildProjectiles(recoloredSkinJson, recoloredSkinDef, gameObject, esc, field, jsonGhostJson, prefabNesting);
        CheckOverlapAttacks(recoloredSkinJson, recoloredSkinDef, gameObject, esc, field, jsonGhostJson, prefabNesting);

        Log.Debug($"found projectile controller on {gameObject.name}");
        GameObject ghostPrefab;
        //modded ,..,
        if (projectileController.ghostPrefab)
        {
            Log.Warning("found deprecatedd ghost prefab " + projectileController.name);
            jsonGhostJson.guid = null;
            ghostPrefab = projectileController.ghostPrefab;
        }
        else
        {
            //Log.Debug($"default ghost guid {projectileController.ghostPrefabAddress?.AssetGUID}");
            jsonGhostJson.guid = projectileController.ghostPrefabAddress?.AssetGUID;
            ghostPrefab = await Addressables.LoadAssetAsync<GameObject>(jsonGhostJson.guid).Task;
        }
                        
        if (!ghostPrefab)
        {
            Log.Warning($"{gameObject.name} had no ghost prefab !!");
            return;
        }
                        
        AddSupportedTypes(ghostPrefab, jsonGhostJson.typesAndPaths);

        AddIfValidGhost(recoloredSkinJson, jsonGhostJson, gameObject);
        Log.Message($"added ghost {ghostPrefab.name} to {recoloredSkinJson.skinName}");
    }

    private static void AddSupportedTypes(GameObject gameObject, List<TypeToPath> typesAndPaths)
    {
        AttemptAddTypeAndPath<Light>(typesAndPaths, gameObject);
        AttemptAddTypeAndPath<MeshRenderer>(typesAndPaths, gameObject);
        AttemptAddTypeAndPath<SkinnedMeshRenderer>(typesAndPaths, gameObject);
        AttemptAddTypeAndPath<ParticleSystem>(typesAndPaths, gameObject);
        AttemptAddTypeAndPath<ParticleSystemRenderer>(typesAndPaths, gameObject);
        AttemptAddTypeAndPath<TrailRenderer>(typesAndPaths, gameObject);
        AttemptAddTypeAndPath<LineRenderer>(typesAndPaths, gameObject);
    }

    private static void CheckOverlapAttacks(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, GameObject gameObject, EntityStateConfiguration esc, SerializedField field, ProjectileGhostJson ghostJson, SubPrefab nestedPrefab = null)
    {
        if (gameObject.TryGetComponent(out ProjectileOverlapAttack projectileOverlapAttack))
        {
            if (projectileOverlapAttack.impactEffect && projectileOverlapAttack.impactEffect.TryGetComponent(out EffectComponent effectComponent))
            {
                SubPrefab subPrefab = new SubPrefab([], [], [], [], projectileOverlapAttack.impactEffect.GetComponent<EffectComponent>() != null);
                HandleSubPrefab(typeof(ProjectileOverlapAttack), nestedPrefab, subPrefab, projectileOverlapAttack.impactEffect.name, "impactEffect", varType.field);
                subPrefab.vfxOverrideJson = new VFXOverrideJson
                {
                    typesAndPaths = [],
                    subPrefabs = [],
                    guid = EffectCatalog.GetEffectDef(effectComponent.effectIndex)?._prefabAssetOrDirectReference?.address?.AssetGUID,
                    configurationType = esc.targetType.assemblyQualifiedName,
                    fieldName = field.fieldName,
                };
                
                if (subPrefab.vfxOverrideJson.guid == null)
                {
                    Log.Warning($"found projectileOverlapAttack on an object {gameObject} without proper guid .,,. mmmmm adding it anyway ,,.");
                    subPrefab.vfxOverrideJson.configurationType = esc.targetType.assemblyQualifiedName;
                    subPrefab.vfxOverrideJson.fieldName = field.fieldName;
                }

                ghostJson.subPrefabs.Add(subPrefab);

                Log.Message($"adding overlap attack {projectileOverlapAttack.impactEffect.name}");
                VFXOverridePathfinder(recoloredSkinJson, recoloredSkinDef, projectileOverlapAttack.impactEffect, subPrefab.vfxOverrideJson, effectComponent);                             
                AddIfValidVFX(recoloredSkinJson, subPrefab.vfxOverrideJson, projectileOverlapAttack.impactEffect);
                //await AddVFXOverride(recoloredSkinJson, vfxOverrideJson, recoloredSkinDef);
            }
            else
            {
                Log.Warning($"couldnt get effect component on impact effect {projectileOverlapAttack.impactEffect} from {projectileOverlapAttack.gameObject.name} 1");
            }
        }
    }

    private static async Task CheckChildProjectiles(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, GameObject gameObject, EntityStateConfiguration esc, SerializedField field, ProjectileGhostJson ghostJson, SubPrefab nestedPrefab = null)
    {
        if (gameObject.TryGetComponent(out ProjectileImpactExplosion projectileImpactExplosion))
        {
            if (projectileImpactExplosion.childrenProjectilePrefab)
            {
                if (projectileImpactExplosion.childrenProjectilePrefab.TryGetComponent(out ProjectileController projectileController))
                {
                    Log.Message(gameObject.name + " had a child projectile prefab ,.,");
                    
                    SubPrefab subPrefab = new SubPrefab([], [], [], [], projectileImpactExplosion.childrenProjectilePrefab.GetComponent<EffectComponent>() != null);
                    HandleSubPrefab(typeof(ProjectileImpactExplosion), nestedPrefab, subPrefab, projectileImpactExplosion.childrenProjectilePrefab.name, "childrenProjectilePrefab", varType.field);
                    subPrefab.projectileGhostJson = new ProjectileGhostJson
                    {
                        configurationType = esc.targetType.assemblyQualifiedName,
                        controllerFieldName = field.fieldName,
                        typesAndPaths = [],
                        subPrefabs = [],
                    };
                    ghostJson.subPrefabs.Add(subPrefab);

                    await CheckProjectileController(recoloredSkinJson, recoloredSkinDef, projectileImpactExplosion.childrenProjectilePrefab, esc, field, subPrefab);
                }
            }
            
            if (projectileImpactExplosion.impactEffect && projectileImpactExplosion.impactEffect.TryGetComponent(out EffectComponent effectComponent))
            {
                SubPrefab subPrefab = new SubPrefab([], [], [], [], true);
                HandleSubPrefab(typeof(ProjectileImpactExplosion), nestedPrefab, subPrefab, projectileImpactExplosion.impactEffect.name, "impactEffect", varType.field);
                subPrefab.vfxOverrideJson = new VFXOverrideJson
                {
                    typesAndPaths = [],
                    subPrefabs = [],
                    guid = EffectCatalog.GetEffectDef(effectComponent.effectIndex)._prefabAssetOrDirectReference.address.AssetGUID,
                    configurationType = esc.targetType.assemblyQualifiedName,
                    fieldName = field.fieldName,
                };
                ghostJson.subPrefabs.Add(subPrefab);

                Log.Message(gameObject.name + " had a projectileImpactExplosion.impactEffect prefab ,.,");
                VFXOverridePathfinder(recoloredSkinJson, recoloredSkinDef, projectileImpactExplosion.impactEffect, subPrefab.vfxOverrideJson, effectComponent);                             
                AddIfValidVFX(recoloredSkinJson, subPrefab.vfxOverrideJson, projectileImpactExplosion.impactEffect);
            }
            else
            {
                Log.Warning($"couldnt get effect component on impact effect {projectileImpactExplosion.impactEffect} from {projectileImpactExplosion.gameObject.name} 2");
            }
        }
        
        if (gameObject.TryGetComponent(out ProjectileSingleTargetImpact projectileSingleTargetImpact))
        {
            if (projectileSingleTargetImpact.impactEffect && projectileSingleTargetImpact.impactEffect.TryGetComponent(out EffectComponent effectComponent))
            {
                SubPrefab subPrefab = new SubPrefab([], [], [], [], true);
                HandleSubPrefab(typeof(ProjectileSingleTargetImpact), nestedPrefab, subPrefab, projectileSingleTargetImpact.impactEffect.name, "impactEffect", varType.field);
                subPrefab.vfxOverrideJson = new VFXOverrideJson
                {
                    typesAndPaths = [],
                    subPrefabs = [],
                    guid = EffectCatalog.GetEffectDef(effectComponent.effectIndex)._prefabAssetOrDirectReference.address.AssetGUID,
                    configurationType = esc.targetType.assemblyQualifiedName,
                    fieldName = field.fieldName,
                };
                ghostJson.subPrefabs.Add(subPrefab);

                Log.Message(gameObject.name + " had a projectileSingleTargetImpact.impactEffect prefab ,.,");
                VFXOverridePathfinder(recoloredSkinJson, recoloredSkinDef, projectileSingleTargetImpact.impactEffect, subPrefab.vfxOverrideJson, effectComponent);                             
                AddIfValidVFX(recoloredSkinJson, subPrefab.vfxOverrideJson, projectileSingleTargetImpact.impactEffect);
            }
        }
    }

    private static void HandleSubPrefab(Type subType, SubPrefab nestedPrefab, SubPrefab subPrefab, string subPrefabName, string variableName, varType variableType)
    {
        if (nestedPrefab != null)
        {
            foreach (var type in nestedPrefab.type)
            {
                subPrefab.type.Add(type);
            }
            foreach (var varName in nestedPrefab.varName)
            {
                subPrefab.varName.Add(varName);
            }
            foreach (var prefabName in nestedPrefab.prefabName)
            {
                subPrefab.prefabName.Add(prefabName);
            }
            foreach (var varType in nestedPrefab.varType)
            {
                subPrefab.varType.Add(varType);
            }
        }
        subPrefab.type.Add(subType);
        subPrefab.varName.Add(variableName);
        subPrefab.prefabName.Add(subPrefabName);
        subPrefab.varType.Add(variableType);
    }

    public static void AddIfValidVFX(RecoloredSkinJson skinJson, VFXOverrideJson vfxOverrideJson, GameObject gameObject)
    {
        if (vfxOverrideJson.typesAndPaths.Count != 0)
        {
            skinJson.vfxOverrides.Add(vfxOverrideJson);
        }
        else
        {
            Log.Warning($"vfx {gameObject.name} has nothing to override !!");
        }
    }
    
    public static void AddIfValidGhost(RecoloredSkinJson skinJson, ProjectileGhostJson ghostJson, GameObject gameObject)
    {
        if (ghostJson.typesAndPaths.Count != 0)
        {
            skinJson.projectileGhosts.Add(ghostJson);
        }
        else
        {
            Log.Warning($"projectile {gameObject.name} has nothing to override !!");
        }
    }
}