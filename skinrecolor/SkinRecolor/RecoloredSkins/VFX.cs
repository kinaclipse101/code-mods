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
    /*
    public static async Task AddVFXOverride(RecoloredSkinJson recoloredSkinJson, VFXOverrideJson jsonOverride, SkinDef recoloredSkinDef = null, GameObject directSubprefab = null, SubPrefab subPrefab = null)
    {
        try
        {
            if (!recoloredSkinDef)
            {
                foreach (var kvp in RecoloredSkinManager.recoloredSkinDefs)
                {
                    if (kvp.Value == recoloredSkinJson)
                    {
                        recoloredSkinDef = kvp.Key;
                    }
                }
            }

            if (!recoloredSkinDef)
            {
                Log.Error("couldnt find skindef !!");
            }
            
            Log.Debug($"attempting to adde effect {jsonOverride.guid} to skin {recoloredSkinDef.name}");

            if (directSubprefab)
            {
                Log.Debug("direct");
                
                string prefabName = RecoloredSkinContent.GetPrefabNameForTemp(recoloredSkinJson, jsonOverride, subPrefab);
                GameObject subEffectPrefab = RecoloredSkinContent.RecolorEffectPrefab(directSubprefab, prefabName, jsonOverride.typesAndPaths, recoloredSkinJson);

                if (subPrefab != null && subPrefab.hasEffectData || subEffectPrefab.GetComponent<EffectComponent>())
                {
                    if (!EffectCatalog.nameToEffectIndex.TryGetValue(prefabName, out var effectIndex))
                    {
                        Log.Error($"couldnt find subprefab in the effect catalog {prefabName} !!");
                        return;
                    }
                    
                    EffectComponent data = subEffectPrefab.GetComponent<EffectComponent>();
                    if(data)
                        data.effectIndex = effectIndex;
                    else
                    {
                        EffectComponent effect = subEffectPrefab.AddComponent<EffectComponent>();
                        effect.effectIndex = effectIndex;
                    }
                    EffectDef def = EffectCatalog.GetEffectDef(effectIndex);
                    Object.Destroy(def.prefab);
                    def.prefab = subEffectPrefab;
                    Log.Debug($"replaced prefab {subEffectPrefab} !!");
                }
                
               
                Log.Debug($"attempting to adde effect finalized {jsonOverride.guid} objname[{subEffectPrefab.name}] to skin {recoloredSkinDef.name}");
                SkinVFXManager.AddSkinVFXInfo(recoloredSkinDef, directSubprefab, subEffectPrefab);
                //SkinVFX.AddSkinVFX(recoloredSkinDef, directSubprefab, subEffectPrefab);
            
                return;
            }

            GameObject basePrefab;
            if (jsonOverride.guid != null)
            {
                basePrefab = await Addressables.LoadAssetAsync<GameObject>(jsonOverride.guid).Task;
            }
            else
            {
                basePrefab = await Utils.TryGetPrefabFromESC(jsonOverride.configurationType, jsonOverride.fieldName);
            }
            
            if (jsonOverride.typesAndPaths.Count > 0)
            {
                string prefabName = RecoloredSkinContent.GetPrefabNameForTemp(recoloredSkinJson, jsonOverride, subPrefab);
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
*/
}