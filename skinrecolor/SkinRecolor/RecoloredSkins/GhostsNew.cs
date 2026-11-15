using System;
using System.Threading.Tasks;
using R2API.Utils;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SkinRecolor.RecoloredSkins;

public static class GhostsNew
{
    public static async Task ProjectileGhostReplacementsAndVFXOverrides(RecoloredSkinJson recoloredSkinJson, bool dontAdd, GameObject body, SkinDef recoloredSkinDef)
    {
        if (recoloredSkinJson.vfxOverrides == null || recoloredSkinJson.projectileGhosts == null)
        {
            await GhostsFirstLoad.HandleFirstLoad(recoloredSkinJson, dontAdd, body, recoloredSkinDef);
        }

        await SetProjectileGhostReplacements(recoloredSkinJson, dontAdd, recoloredSkinDef);
    }
    
    private static async Task SetProjectileGhostReplacements(RecoloredSkinJson recoloredSkinJson, bool dontAdd, SkinDef recoloredSkinDef)
    {
        recoloredSkinDef.skinDefParams.projectileGhostReplacements = [];
        foreach (var jsonGhost in recoloredSkinJson.projectileGhosts)
        {
            await RecursiveFindGhostsInSubPrefabs(recoloredSkinJson, recoloredSkinDef, jsonGhost, await Utils.TryGetPrefabFromESC(jsonGhost.configurationType, jsonGhost.fieldName));
        }
        foreach (var jsonGhost in recoloredSkinJson.vfxOverrides)
        {
            GameObject prefab;
            if(jsonGhost.guid != null)
                prefab = await Addressables.LoadAssetAsync<GameObject>(jsonGhost.guid).Task;
            else
                prefab = await Utils.TryGetPrefabFromESC(jsonGhost.configurationType, jsonGhost.fieldName);
            
            await RecursiveFindGhostsInSubPrefabs(recoloredSkinJson, recoloredSkinDef, jsonGhost, prefab);
        }
    }

    private static async Task RecursiveFindGhostsInSubPrefabs(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, JsonBase jsonBase, GameObject prefab)
    {
        if (jsonBase is ProjectileGhostJson projectileGhostJson)
        {
            await AddGhostReplacementFromGhostJson(recoloredSkinJson, recoloredSkinDef, projectileGhostJson, prefab);
        }
        
        foreach (var subPrefab in jsonBase.subPrefabs)
        {
            if (subPrefab.projectileGhostJson != null)
            {
                Log.Debug($"getting {subPrefab.varType} from {prefab.name} with ame {subPrefab.varName}");
                GameObject subPrefabBase = prefab?.GetComponent(subPrefab.varType)?.GetFieldValue<GameObject>(subPrefab.varName);
                Log.Debug($"new ghost json gkdfg d {subPrefabBase}");
        
                if (subPrefabBase)
                {
                    Log.Debug($"found subprefab {subPrefabBase.name} on {prefab.name} {subPrefab.varName} {subPrefab.varType}");
                    await RecursiveFindGhostsInSubPrefabs(recoloredSkinJson, recoloredSkinDef, subPrefab.projectileGhostJson, subPrefabBase);
                }
                else
                {
                    Log.Error($"couldnt get subprefab obj in {subPrefab.projectileGhostJson.configurationType} {subPrefab.projectileGhostJson.fieldName}");
                }
            };

            if (subPrefab.vfxOverrideJson != null)
            {
                GameObject subPrefabBase = prefab?.GetComponent(subPrefab.varType)?.GetFieldValue<GameObject>(subPrefab.varName);
                await RecursiveFindGhostsInSubPrefabs(recoloredSkinJson, recoloredSkinDef, subPrefab.vfxOverrideJson, subPrefabBase);
            }
            
            
        }
    }

    private static async Task AddGhostReplacementFromGhostJson(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, ProjectileGhostJson jsonGhost, GameObject prefab)
    {
        GameObject origGhostPrefab = jsonGhost.ghostGUID != null ? await Addressables.LoadAssetAsync<GameObject>(jsonGhost.ghostGUID).Task : prefab.GetComponent<ProjectileController>().ghostPrefab;
        if (!origGhostPrefab)
        {
            Log.Error($"error while trying to add ghost replacement for {recoloredSkinJson.baseSkinBody} {jsonGhost.configurationType} {jsonGhost.fieldName}");
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
}