using System;
using System.Collections.Generic;
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

    public override List<BaseSurvivorSpecificComponent> GetSurvivorSpecificComponents(GameObject gameObject)
    {
        List<BaseSurvivorSpecificComponent> components = [];
        
        // if (gameObject.TryGetComponent(out RoR2.Projectile.AcolyteJavelinProjectile acolyteJavelinProjectile))
        // {
        //     if (acolyteJavelinProjectile.explosionEffect)
        //     {
        //         components.Add(new BaseSurvivorSpecificComponent(typeof(RoR2.Projectile.AcolyteJavelinProjectile), nameof(acolyteJavelinProjectile.explosionEffect), acolyteJavelinProjectile.explosionEffect));
        //         components.Add(new BaseSurvivorSpecificComponent(typeof(RoR2.Projectile.AcolyteJavelinProjectile), nameof(acolyteJavelinProjectile.blastImpactEffect), acolyteJavelinProjectile.blastImpactEffect));
        //         components.Add(new BaseSurvivorSpecificComponent(typeof(RoR2.Projectile.AcolyteJavelinProjectile), nameof(acolyteJavelinProjectile.lifetimeExpiredEffect), acolyteJavelinProjectile.lifetimeExpiredEffect));
        //     }
        // }

        return components;
    }

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
            }
        ];

        foreach (var overrideJson in returnOverrides)
        {
            Ghosts.VFXOverridePathfinder(recoloredSkinJson, Addressables.LoadAssetAsync<GameObject>(overrideJson.guid).WaitForCompletion(), overrideJson);
        }

        return returnOverrides;
    }
    
    public override void Hooks()
    {
        AddRecoloredMat(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Huntress.matHuntressFlashExpanded_mat);
        AddRecoloredMat(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Huntress.matHuntressFlashBright_mat);
        
        IL.EntityStates.Huntress.BlinkState.CreateBlinkEffect += BlinkStateOnCreateBlinkEffect;
        //IL.EntityStates.Huntress.BlinkState.OnExit += BlinkStateOnOnExit;
    }

    private void BlinkStateOnOnExit(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        
        /*
         * IL_00a2: stfld class [UnityEngine.CoreModule]UnityEngine.Material RoR2.TemporaryOverlayInstance::originalMaterial
         */

        bool found = c.TryGotoNext(MoveType.Before,
            x => x.MatchStfld<RoR2.TemporaryOverlayInstance>("originalMaterial")
        );

        if (!found)
        {
            Log.Error($"Failed to apply SkinVFX IL hook on BlinkStateOnOnExit");
            return;
        }

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<Material, EntityStates.Huntress.BlinkState, Material>>((overlayMat, blinkState) =>
        {
            RoR2.SkinDef skindef = RoR2.SkinCatalog.FindCurrentSkinDefForBodyInstance(blinkState.characterBody.gameObject);
            if (skindef && RecoloredSkinManager.recoloredSkinDefs.TryGetValue(skindef, out RecoloredSkinJson recoloredSkinJson))
            {
                Log.Debug("AAA");
                Material newMat = UnityEngine.Object.Instantiate(overlayMat);
                Utils.RecolorMaterial(ref newMat, overlayMat, recoloredSkinJson);
                return newMat;
            }

            Log.Message("couldnt replace mat");
            return overlayMat;
        });
        
        bool found2 = c.TryGotoNext(MoveType.Before,
            x => x.MatchStfld<RoR2.TemporaryOverlayInstance>("originalMaterial")
        );

        if (!found2)
        {
            Log.Error($"Failed to apply SkinVFX IL hook on BlinkStateOnOnExit 2");
            return;
        }

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<Material, EntityStates.Huntress.BlinkState, Material>>((overlayMat, blinkState) =>
        {
            RoR2.SkinDef skindef = RoR2.SkinCatalog.FindCurrentSkinDefForBodyInstance(blinkState.characterBody.gameObject);
            if (skindef && RecoloredSkinManager.recoloredSkinDefs.TryGetValue(skindef, out RecoloredSkinJson recoloredSkinJson))
            {
                Log.Debug("AAA");
                Material newMat = UnityEngine.Object.Instantiate(overlayMat);
                Utils.RecolorMaterial(ref newMat, overlayMat, recoloredSkinJson);
                return newMat;
            }

            Log.Message("couldnt replace mat");
            return overlayMat;
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
            SkinVFXInfo skinVFXInfo = SkinVFXManager.FindSkinVFXInfo(blinkState.gameObject, blinkPrefab);

            if (skinVFXInfo == null)
            {
                return blinkPrefab;
            }

            Log.Message("aaaa 12");

            return skinVFXInfo.ReplacementEffectPrefab;
        });
    }
}