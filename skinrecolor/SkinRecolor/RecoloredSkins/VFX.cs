using System;
using System.Collections.Generic;
using System.Text;
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
    public static async Task AddVFXOverride(RecoloredSkinJson recoloredSkinJson, VFXOverrideJson jsonOverride, SkinDef recoloredSkinDef, GameObject directSubprefab = null, SubPrefab subPrefab = null)
    {
        try
        {
            Log.Debug($"attempting to adde effect {jsonOverride.guid} to skin {recoloredSkinDef.name}");

            if (directSubprefab)
            {
                Log.Debug("direct");
                
                string prefabName = RecoloredSkinContent.GetPrefabNameForTemp(recoloredSkinJson, jsonOverride, subPrefab);
                GameObject subEffectPrefab = RecoloredSkinContent.RecolorEffectPrefab(directSubprefab, prefabName, jsonOverride.typesAndPaths, recoloredSkinJson);
                
                if (!EffectCatalog.nameToEffectIndex.TryGetValue(prefabName, out var effectIndex))
                {
                    Log.Error($"couldnt find subprefab in the effect catalog {prefabName} !!");
                    return;
                }

                EffectComponent data = subEffectPrefab.GetComponent<EffectComponent>();
                data.effectIndex = effectIndex;
                EffectDef def = EffectCatalog.GetEffectDef(effectIndex);
                def.prefab = subEffectPrefab;
                Log.Debug($"replaced prefab {subEffectPrefab} !!");
               
                SkinVFX.AddSkinVFX(recoloredSkinDef, directSubprefab, subEffectPrefab);
            
                return;
            }

            GameObject basePrefab;
            if (jsonOverride.guid != null)
            {
                basePrefab = await Addressables.LoadAssetAsync<GameObject>(jsonOverride.guid).Task;
                
            }
            else
            {
                basePrefab = await Ghosts.TryGetPrefabFromESC(jsonOverride.configurationType, jsonOverride.fieldName);
            }
            
            if (jsonOverride.typesAndPaths.Count > 0)
            {
                string prefabName = RecoloredSkinContent.GetPrefabNameForTemp(recoloredSkinJson, jsonOverride);
                SkinVFX.AddSkinVFX(recoloredSkinDef, basePrefab, RecoloredSkinContent.RecolorEffectPrefab(basePrefab, prefabName, jsonOverride.typesAndPaths, recoloredSkinJson));
                Log.Message($"added vfx override for non effect data {prefabName}");
            }
        }
        catch (Exception e)
        {
            Log.Error($"error while adding vfx to skin {recoloredSkinJson.baseSkinName} !!");
            Log.Error(e);
        }
    }
}