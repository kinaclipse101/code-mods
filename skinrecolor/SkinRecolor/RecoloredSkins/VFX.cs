using System;
using System.Collections.Generic;
using R2API;
using RoR2;
using SkinRecolor.RecoloredSkins;
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
            Log.Debug($"attempting to adde effect {jsonOverride.guid} to skin {recoloredSkinDef.name}");

            if (jsonOverride.guid != null)
            {
                if (!RecoloredSkinContent.vfxOverrides.TryGetValue(recoloredSkinJson, out var vfxOverride)) return;
            
                foreach (var vfx in vfxOverride)
                {
                    Log.Debug($"Added override: {recoloredSkinJson.baseSkinName} - {vfx.basePrefab}");
                    SkinVFX.AddSkinVFX(recoloredSkinDef, vfx.basePrefab, vfx.overridePrefab);
                }
            }
            else
            {
                if (!RecoloredSkinManager.escDict.TryGetValue(jsonOverride.configurationType, out var targetEsc))
                {
                    Log.Warning($"couldnt get type {jsonOverride.configurationType} in dict");
                    foreach (var esc in RoR2.ContentManagement.ContentManager.entityStateConfigurations)
                    {
                        if (esc.targetType.assemblyQualifiedName != jsonOverride.configurationType) continue;
                        targetEsc = esc;
                        break;
                    }
                }
                if (!targetEsc)
                {
                    Log.Warning($"couldnt find esc for {jsonOverride.fieldName} while trying to vfx override");
                    return;
                }
            
                GameObject basePrefab = null;
                foreach (var field in targetEsc.serializedFieldsCollection.serializedFields)
                {
                    if(field.fieldName != jsonOverride.fieldName) continue;
                    basePrefab = field.fieldValue.objectValue as GameObject;
                    if (!basePrefab)
                        basePrefab = await Addressables.LoadAssetAsync<GameObject>(field.fieldValue.stringValue).Task;

                    break;
                }
                if (!basePrefab)
                {
                    Log.Warning($"couldnt find GameObject for {jsonOverride.fieldName} while trying to vfx override");
                    return;
                }
                
                string prefabName = $"jsonPrefab_{jsonOverride.guid}_{recoloredSkinJson.saturation}{recoloredSkinJson.value}{recoloredSkinJson.skinName}";
                SkinVFX.AddSkinVFX(recoloredSkinDef, basePrefab, RecoloredSkinContent.RecolorEffectPrefab(basePrefab, prefabName, jsonOverride, recoloredSkinJson));
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