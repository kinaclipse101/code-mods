using System;
using System.Collections.Generic;
using HG.GeneralSerializer;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using SkinRecolor.RecoloredSkins;
using SkinRecolor.SkinManager;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace SkinRecolor.SkinVFXHooks;

public class Huntress : SurvivorHookBase
{
    public override string survName => "HuntressBody";

    public override List<VFXOverrideJson> SpecificReskinnedObjects(RecoloredSkinJson recoloredSkinJson)
    {
        List<VFXOverrideJson> returnOverrides =
        [
            new()
            {
                typesAndPaths = [],
                subPrefabs = [],
                guid = RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Huntress.ArrowOrbEffect_prefab,
                configurationType = "HuntressSpecific",
                fieldName = "ArrowBase",
            },
            new()
            {
                typesAndPaths = [],
                subPrefabs = [],
                guid = RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Huntress.FlurryArrowOrbEffect_prefab,
                configurationType = "HuntressSpecific",
                fieldName = "ArrowFlurry",
            },
            new()
            {
                typesAndPaths = [],
                subPrefabs = [],
                guid = RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Huntress.FlurryArrowCritOrbEffect_prefab,
                configurationType = "HuntressSpecific",
                fieldName = "ArrowFlurryCrit",
            },
            new()
            {
                typesAndPaths = [],
                subPrefabs = [],
                guid = RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Huntress.HuntressGlaiveOrbEffect_prefab,
                configurationType = "HuntressSpecific",
                fieldName = "Glaive",
            },
            new()
            {
                typesAndPaths = [],
                subPrefabs = [],
                guid = RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Huntress.OmniImpactVFXHuntress_prefab,
                configurationType = "HuntressSpecific",
                fieldName = "OmniImpactVFX",
            }
        ];

        foreach (var overrideJson in returnOverrides)
        {
            GhostsFirstLoad.CheckEffectsAndProjectiles(recoloredSkinJson, null, Addressables.LoadAssetAsync<GameObject>(overrideJson.guid).WaitForCompletion(), null, new SerializedField(), null, [], [..recoloredSkinJson.vfxOverrides]);
        }

        return returnOverrides;
    }
    
    public override void Hooks()
    {
        AddRecoloredMat(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Huntress.matHuntressFlashExpanded_mat);
        AddRecoloredMat(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Huntress.matHuntressFlashBright_mat);
        
        IL.EntityStates.Huntress.BlinkState.CreateBlinkEffect += BlinkStateOnCreateBlinkEffect;
        IL.EntityStates.Huntress.BaseBeginArrowBarrage.CreateBlinkEffect += BaseBeginArrowBarrageOnCreateBlinkEffect;
    }

    private void BaseBeginArrowBarrageOnCreateBlinkEffect(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        
        /*
         * IL_001e: ldsfld class [UnityEngine.CoreModule]UnityEngine.GameObject EntityStates.Huntress.BaseBeginArrowBarrage::blinkPrefab
         */

        int effectLoc = -1;
        bool found = c.TryGotoNext(MoveType.After,
            x => x.MatchLdsfld<EntityStates.Huntress.BaseBeginArrowBarrage>("blinkPrefab")
        );

        if (!found)
        {
            Log.Error($"Failed to apply SkinVFX IL hook on BlinkStateOnCreateBlinkEffect");
            return;
        }

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<GameObject, EntityStates.Huntress.BlinkState, GameObject>>((blinkPrefab, blinkState) =>
        {
            SkinVFXInfo skinVFXInfo = SkinVFXManager.FindSkinVFXInfo(blinkState.gameObject, EntityStates.Huntress.BaseBeginArrowBarrage.blinkPrefab);

            Log.Debug("checking blnk satta");
            
            if (skinVFXInfo == null)
            {
                return blinkPrefab;
            }

            Log.Message("aaaa 12e");

            return skinVFXInfo.ReplacementEffectPrefab;
        });
    }

    private void BlinkStateOnCreateBlinkEffect(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        
        /*
         * IL_001e: ldsfld class [UnityEngine.CoreModule]UnityEngine.GameObject EntityStates.Huntress.BlinkState::blinkPrefab
         */

        int effectLoc = -1;
        bool found = c.TryGotoNext(MoveType.After,
            x => x.MatchLdsfld<EntityStates.Huntress.BlinkState>("blinkPrefab")
        );

        if (!found)
        {
            Log.Error($"Failed to apply SkinVFX IL hook on BlinkStateOnCreateBlinkEffect");
            return;
        }

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<GameObject, EntityStates.Huntress.BlinkState, GameObject>>((blinkPrefab, blinkState) =>
        {
            SkinVFXInfo skinVFXInfo = SkinVFXManager.FindSkinVFXInfo(blinkState.gameObject, EntityStates.Huntress.BlinkState.blinkPrefab);

            Log.Debug("checking blnk satta");
            
            if (skinVFXInfo == null)
            {
                return blinkPrefab;
            }

            Log.Message("aaaa 12");

            return skinVFXInfo.ReplacementEffectPrefab;
        });
    }
}