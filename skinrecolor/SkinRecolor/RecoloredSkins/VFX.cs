using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using R2API;
using R2API.Utils;
using RoR2;
using SkinRecolor.RecoloredSkins;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace SkinRecolor.SkinManager;

public class VFX
{
    public static async Task AddVFXOverride(RecoloredSkinJson recoloredSkinJson, VFXOverrideJson jsonOverride, SkinDef recoloredSkinDef, bool direct = false)
    {
        try
        {
            Log.Debug($"attempting to adde effect {jsonOverride.guid} to skin {recoloredSkinDef.name}");

            if (direct)
            {
                Log.Debug("direct");
                if (jsonOverride.subPrefabs.Count > 0)
                {
                    var baseSubprefab = await Ghosts.TryGetPrefabFromESC(jsonOverride.configurationType, jsonOverride.fieldName);
                    foreach (var subPrefab in jsonOverride.subPrefabs)
                    {
                        GameObject subEffectBasePrefab = baseSubprefab;
                        Log.Debug($"attempting to add subprefab {subPrefab.prefabName} {baseSubprefab.name}");
                       
                        for (int j = 0; j < subPrefab.type.Count; j++)
                        {
                            subEffectBasePrefab = subPrefab.varType[j] switch
                            {
                                varType.field => subEffectBasePrefab.GetComponent(subPrefab.type[j]).GetFieldValue<GameObject>(subPrefab.varName[j]),
                                varType.property => subEffectBasePrefab.GetComponent(subPrefab.type[j]).GetPropertyValue<GameObject>(subPrefab.varName[j])
                            };
                        }
                        
                        string subPrefabName = $"jsonPrefab_{subPrefab.prefabName}_{recoloredSkinJson.saturation}{recoloredSkinJson.value}{recoloredSkinJson.skinName}";
                        GameObject subEffectPrefab = RecoloredSkinContent.RecolorEffectPrefab(subEffectBasePrefab, subPrefabName, jsonOverride, recoloredSkinJson);

                        if (subPrefab.hasEffectData)
                        {
                            if (!EffectCatalog.nameToEffectIndex.TryGetValue(subPrefabName, out var effectIndex))
                            {
                                Log.Error($"couldnt find subprefab in the effect catalog {subPrefabName} !!");
                                return;
                            }

                            EffectComponent data = subEffectPrefab.GetComponent<EffectComponent>();
                            data.effectIndex = effectIndex;
                            EffectDef def = EffectCatalog.GetEffectDef(effectIndex);
                            def.prefab = subEffectPrefab;
                            Log.Debug($"replaced prefab {subEffectPrefab} !!");
                        }
                        else
                        {
                            Log.Debug($"subeffect had no effectdata {subPrefabName}");
                        }
                        
                        SkinVFX.AddSkinVFX(recoloredSkinDef, subEffectBasePrefab, subEffectPrefab);
                    }
                }

                return;
            }
            
            if (jsonOverride.guid != null)
            {
                GameObject basePrefab = await Addressables.LoadAssetAsync<GameObject>(jsonOverride.guid).Task;
                string prefabName = $"jsonPrefab_{jsonOverride.guid}_{recoloredSkinJson.saturation}{recoloredSkinJson.value}{recoloredSkinJson.skinName}";
                SkinVFX.AddSkinVFX(recoloredSkinDef, basePrefab, RecoloredSkinContent.RecolorEffectPrefab(basePrefab, prefabName, jsonOverride, recoloredSkinJson));
                Log.Message($"added vfx override for non effect data {prefabName}");
            }
            else
            {
                var basePrefab = await Ghosts.TryGetPrefabFromESC(jsonOverride.configurationType, jsonOverride.fieldName);

                if (jsonOverride.typesAndPaths.Count > 0)
                {
                    string prefabName = $"jsonPrefab_{jsonOverride.guid}_{recoloredSkinJson.saturation}{recoloredSkinJson.value}{recoloredSkinJson.skinName}";
                    SkinVFX.AddSkinVFX(recoloredSkinDef, basePrefab, RecoloredSkinContent.RecolorEffectPrefab(basePrefab, prefabName, jsonOverride, recoloredSkinJson));
                    Log.Message($"added vfx override for non effect data {prefabName}");
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"error while adding vfx to skin {recoloredSkinJson.baseSkinName} !!");
            Log.Error(e);
        }
    }
}