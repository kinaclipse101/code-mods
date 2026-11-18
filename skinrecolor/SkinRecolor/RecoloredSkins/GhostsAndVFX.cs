using System;
using System.Threading.Tasks;
using R2API.Utils;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SkinRecolor.RecoloredSkins;

public static class GhostsAndVFX
{
    public static async Task ProjectileGhostReplacementsAndVFXOverrides(RecoloredSkinJson recoloredSkinJson, bool dontAdd, GameObject body, SkinDef recoloredSkinDef)
    {
        if (recoloredSkinJson.vfxOverrides == null || recoloredSkinJson.projectileGhosts == null)
        {
            await GhostsFirstLoad.HandleFirstLoad(recoloredSkinJson, body);
        }

        Log.Debug($"setting ghostts .,., {recoloredSkinDef.skinDefParams == null}");
        if (dontAdd)
        {
            await SetProjectileGhostReplacements(recoloredSkinJson, recoloredSkinDef, dontAdd);
        }
        else
        {
            SetProjectileGhostReplacements(recoloredSkinJson, recoloredSkinDef, dontAdd);
        }
    }
    
    private static async Task SetProjectileGhostReplacements(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, bool dontAdd)
    {
        recoloredSkinDef.skinDefParams.projectileGhostReplacements = [];
        foreach (var jsonGhost in recoloredSkinJson.projectileGhosts)
        {
            await RecursiveFindGhostsInSubPrefabs(recoloredSkinJson, recoloredSkinDef, jsonGhost, await Utils.TryGetPrefabFromESC(jsonGhost.configurationType, jsonGhost.fieldName), dontAdd);
        }
        foreach (var jsonGhost in recoloredSkinJson.vfxOverrides)
        {
            GameObject prefab;
            if(jsonGhost.guid != null)
                prefab = await Addressables.LoadAssetAsync<GameObject>(jsonGhost.guid).Task;
            else
                prefab = await Utils.TryGetPrefabFromESC(jsonGhost.configurationType, jsonGhost.fieldName);
            
            await RecursiveFindGhostsInSubPrefabs(recoloredSkinJson, recoloredSkinDef, jsonGhost, prefab, dontAdd);
        }
    }

    private static async Task RecursiveFindGhostsInSubPrefabs(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, JsonBase jsonBase, GameObject prefab, bool dontAdd)
    {
        try
        {
            if (jsonBase is ProjectileGhostJson projectileGhostJson)
            {
                Log.Debug($"adding ghost {jsonBase}");
                await AddGhostReplacementFromGhostJson(recoloredSkinJson, recoloredSkinDef, projectileGhostJson, prefab, dontAdd);
            }
        
            foreach (var subPrefab in jsonBase.subPrefabs)
            {
                if (subPrefab.projectileGhostJson != null)
                {
                    Log.Debug($"getting {subPrefab.varType} from {prefab?.name} with ame {subPrefab.varName}");
                    GameObject subPrefabBase = prefab?.GetComponent(subPrefab.varType)?.GetFieldValue<GameObject>(subPrefab.varName);
                    Log.Debug($"new ghost json gkdfg d {subPrefabBase}");
        
                    if (subPrefabBase)
                    {
                        Log.Debug($"found subprefab {subPrefabBase.name} on {prefab.name} {subPrefab.varName} {subPrefab.varType}");
                        await RecursiveFindGhostsInSubPrefabs(recoloredSkinJson, recoloredSkinDef, subPrefab.projectileGhostJson, subPrefabBase, dontAdd);
                    }
                    else
                    {
                        Log.Error($"couldnt get subprefab obj in {subPrefab.projectileGhostJson.configurationType} {subPrefab.projectileGhostJson.fieldName}");
                    }
                }

                if (subPrefab.vfxOverrideJson != null)
                {
                    GameObject subPrefabBase = prefab?.GetComponent(subPrefab.varType)?.GetFieldValue<GameObject>(subPrefab.varName);
                    await RecursiveFindGhostsInSubPrefabs(recoloredSkinJson, recoloredSkinDef, subPrefab.vfxOverrideJson, subPrefabBase, dontAdd);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error("error while recursvie find ghost ");
            Log.Error(e);
        }
    }

    private static async Task AddGhostReplacementFromGhostJson(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, ProjectileGhostJson jsonGhost, GameObject prefab, bool dontAdd)
    {
        GameObject origGhostPrefab = jsonGhost.guid != null ? await Addressables.LoadAssetAsync<GameObject>(jsonGhost.guid).Task : prefab.GetComponent<ProjectileController>().ghostPrefab;
        if (!origGhostPrefab)
        {
            Log.Error($"error while trying to add ghost replacement for {recoloredSkinJson.baseSkinBody} {jsonGhost.configurationType} {jsonGhost.fieldName}");
            return;
        }
        
        if(!dontAdd)
        {
            Log.Warning("adding option to roo " + jsonGhost.configName);
            ModSettingsManager.AddOption(new HSVOption(jsonGhost.specificHSV,
                jsonGhost.configName,
                recoloredSkinJson.sectionName,
                $"projectile ghost on base skin .,,."));
        }
        GameObject ghostPrefab = RecoloredSkinContent.RecolorEffectPrefab(origGhostPrefab, $"jsonGhost_{origGhostPrefab.name}_{recoloredSkinJson.hsv.hue}{recoloredSkinJson.hsv.saturation}{recoloredSkinJson.hsv.value}_{recoloredSkinJson.skinName}", jsonGhost, jsonGhost.specificHSV, recoloredSkinJson);
        SkinDefParams.ProjectileGhostReplacement replacement = new SkinDefParams.ProjectileGhostReplacement
        {
            projectilePrefab = prefab,
            projectileGhostReplacementPrefab = ghostPrefab
        };
        HG.ArrayUtils.ArrayAppend(ref recoloredSkinDef.skinDefParams.projectileGhostReplacements, replacement);
        Log.Debug($"added projectile ghost !! {origGhostPrefab}");
    }
}