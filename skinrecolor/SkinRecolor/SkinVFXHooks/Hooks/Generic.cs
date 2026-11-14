using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using On.EntityStates;
using R2API;
using RoR2;
using RoR2.AsyncManagement;
using RoR2.Orbs;
using RoR2.Projectile;
using SkinRecolor.RecoloredSkins;
using SkinRecolor.SkinVFXHooks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using CharacterModel = On.RoR2.CharacterModel;
using Object = UnityEngine.Object;
using ProjectileManager = On.RoR2.Projectile.ProjectileManager;
using TemporaryOverlayInstance = On.RoR2.TemporaryOverlayInstance;

namespace SkinRecolor.SkinVFXHooks;

public static class Generic
{
    public static void Hook()
    {
        IL.RoR2.Projectile.ProjectileExplosion.DetonateServer += ProjectileImpactExplosionOnDetonate;
        IL.RoR2.OverlapAttack.ProcessHits += OverlapAttackSkinned;
        On.RoR2.Projectile.ProjectileSingleTargetImpact.OnProjectileImpact += ProjectileSingleTargetImpactOnOnProjectileImpact;
        On.RoR2.Projectile.ProjectileSimple.Start += ProjectileSimpleOnStart ;
        On.EntityStates.AimThrowableBaseAimOnly.OnEnter += AimThrowableBaseAimOnlyOnOnEnter;
        On.EntityStates.GenericProjectileBaseState.OnEnter += GenericProjectileBaseStateOnOnEnter;
        On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo += ProjectileManagerOnFireProjectile_FireProjectileInfo;
        IL.RoR2.Orbs.LightningOrb.Begin += LightningOrbOnBegin;
        On.RoR2.CharacterModel.AddTempOverlay += CharacterModelOnAddTempOverlay;
        //IL.RoR2.TemporaryOverlayInstance.SetupMaterial += TemporaryOverlayInstanceOnSetupMaterial;
        //IL.RoR2.TemporaryOverlayInstance.AddToCharacterModel += TemporaryOverlayInstanceOnAddToCharacterModel;
        On.RoR2.TemporaryOverlayInstance.AddToCharacterModel += TemporaryOverlayInstanceOnAddToCharacterModel;
    }

    private static void TemporaryOverlayInstanceOnAddToCharacterModel(TemporaryOverlayInstance.orig_AddToCharacterModel orig, RoR2.TemporaryOverlayInstance self, RoR2.CharacterModel charactermodel)
    {
        Material newMat = SkinVFXManager.ApplyVFXInfo(charactermodel.body?.gameObject, self.originalMaterial);
        if (newMat)
        {
            self.originalMaterial = Object.Instantiate(newMat);
        }
        
        orig(self, charactermodel);
    }

    private static void TemporaryOverlayInstanceOnAddToCharacterModel(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        /* // if ((bool)characterModel)
           IL_0006: ldarg.1
           IL_0007: call bool [UnityEngine.CoreModule]UnityEngine.Object::op_Implicit(class [UnityEngine.CoreModule]UnityEngine.Object)
           IL_000c: brfalse.s IL_002a
         */
        
        bool found = c.TryGotoNext(MoveType.After,
            x => x.MatchLdarg(out _),
            x => x.MatchCallOrCallvirt<UnityEngine.Object>("op_Implicit"),
            x => x.MatchBrfalse(out _)
        );

        if (!found)
        {
            Log.Error($"Failed to apply SkinVFX IL hook on LightningOrbOnBegin");
            return;
        }

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldarg_1);
        c.EmitDelegate<Action<RoR2.TemporaryOverlayInstance, RoR2.CharacterModel>>((overlay, model) =>
        {
            Log.Debug("aaa 1");
            Material newMat = SkinVFXManager.ApplyVFXInfo(model.body?.gameObject, overlay.materialInstance);
            if (newMat)
            {
                //Object.Destroy(overlay.materialInstance);
                //Object.Destroy(overlay.componentReference.materialInstance);
                Material instanceMat = new Material(newMat);
                overlay.materialInstance = instanceMat;
                overlay.originalMaterial = newMat;
                //overlay.componentReference.materialInstance = instanceMat;
            }
            Log.Debug("aaa 2");
        });
    }

    private static void TemporaryOverlayInstanceOnSetupMaterial(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        /* // materialInstance = new Material(originalMaterial);
           IL_000d: ldarg.0
           IL_000e: ldarg.0
           IL_000f: ldfld class [UnityEngine.CoreModule]UnityEngine.Material RoR2.TemporaryOverlayInstance::originalMaterial
           IL_0014: newobj instance void [UnityEngine.CoreModule]UnityEngine.Material::.ctor(class [UnityEngine.CoreModule]UnityEngine.Material)
           IL_0019: stfld class [UnityEngine.CoreModule]UnityEngine.Material RoR2.TemporaryOverlayInstance::materialInstance
         */
        
        bool found = c.TryGotoNext(MoveType.After,
            x => x.MatchLdarg(out _),
            x => x.MatchLdarg(out _),
            x => x.MatchLdfld<RoR2.TemporaryOverlayInstance>("originalMaterial")
            );

        if (!found)
        {
            Log.Error($"Failed to apply SkinVFX IL hook on LightningOrbOnBegin");
            return;
        }

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<Material, RoR2.TemporaryOverlayInstance, Material>>((baseMat, tempOverlay) =>
        {
            Log.Debug("aaa 1");
            Material newMat = SkinVFXManager.ApplyVFXInfo(tempOverlay.assignedCharacterModel?.body?.gameObject, baseMat);
            Log.Debug("aaa 2");
            Log.Debug($"newMat nunll ? {newMat?.name}");
            Log.Debug($"body name ? {tempOverlay.assignedCharacterModel?.body?.gameObject?.name}");
            Log.Debug($"component {tempOverlay?.componentReference?.gameObject?.name}");
            
            return newMat ? newMat : baseMat;
        });
    }

    private static void CharacterModelOnAddTempOverlay(CharacterModel.orig_AddTempOverlay orig, RoR2.CharacterModel self, RoR2.TemporaryOverlayInstance overlay)
    {
        if(overlay.materialInstance)
            SkinVFXManager.ApplyVFXInfo(self.body?.gameObject, ref overlay.materialInstance);
 
        orig(self, overlay);
    }

    private static void OrbEffectOnUpdateOrb(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        /* // effectData.SetHurtBoxReference(target);
           IL_0164: ldloc.1
           IL_0165: ldarg.0
           IL_0166: ldfld class RoR2.HurtBox RoR2.Orbs.Orb::target
           IL_016b: callvirt instance void RoR2.EffectData::SetHurtBoxReference(class RoR2.HurtBox)
         */
        
        int effectLoc = -1;
        bool found = c.TryGotoNext(MoveType.After,
            x => x.MatchLdloc(out effectLoc),
            x => x.MatchLdarg(out _),
            x => x.MatchLdfld<RoR2.Orbs.Orb>("target"),
            x => x.MatchCallOrCallvirt(typeof(EffectData), nameof(EffectData.SetHurtBoxReference))
        );

        if (!found)
        {
            Log.Error($"Failed to apply SkinVFX IL hook on LightningOrbOnBegin");
            return;
        }

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc, 0); // path
        c.Emit(OpCodes.Ldloc, effectLoc);
        c.EmitDelegate<Action<LightningOrb, string, EffectData>>((lightningOrb, path, effectData) =>
        {
            SkinVFXInfo skinVFXInfo = SkinVFXManager.FindSkinVFXInfo(lightningOrb.attacker, OrbStorageUtility.Get(path));
            Log.Debug("aaaa" + skinVFXInfo?.ReplacementEffectPrefab == null);
            
            if (!skinVFXInfo?.ReplacementEffectPrefab) return;
            effectData.genericUInt = skinVFXInfo.Identifier;
        });
    }

    // private static void TemporaryOverlayInstanceOnSetupMaterial(TemporaryOverlayInstance.orig_SetupMaterial orig, RoR2.TemporaryOverlayInstance self)
    // {
    //     if(self.materialInstance)
    //         SkinVFXManager.ApplyVFXInfo(self.charactermo?.body?.gameObject, ref self.materialInstance);
    //     else
    //         self.materialInstance = SkinVFXManager.ApplyVFXInfo(self.assignedCharacterModel?.body?.gameObject, self.originalMaterial);
    //     
    //     orig(self);
    //     
    //     //SkinVFXManager.ApplyVFXInfo(self.assignedCharacterModel?.body?.gameObject, ref self.materialInstance);
    // }

    private static void LightningOrbOnBegin(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        /* // effectData.SetHurtBoxReference(target);
           IL_0164: ldloc.1
           IL_0165: ldarg.0
           IL_0166: ldfld class RoR2.HurtBox RoR2.Orbs.Orb::target
           IL_016b: callvirt instance void RoR2.EffectData::SetHurtBoxReference(class RoR2.HurtBox)
         */
        
        int effectLoc = -1;
        bool found = c.TryGotoNext(MoveType.After,
            x => x.MatchLdloc(out effectLoc),
            x => x.MatchLdarg(out _),
            x => x.MatchLdfld<RoR2.Orbs.Orb>("target"),
            x => x.MatchCallOrCallvirt(typeof(EffectData), nameof(EffectData.SetHurtBoxReference))
        );

        if (!found)
        {
            Log.Error($"Failed to apply SkinVFX IL hook on LightningOrbOnBegin");
            return;
        }

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc, 0); // path
        c.Emit(OpCodes.Ldloc, effectLoc);
        c.EmitDelegate<Action<LightningOrb, string, EffectData>>((lightningOrb, path, effectData) =>
        {
            SkinVFXInfo skinVFXInfo = SkinVFXManager.FindSkinVFXInfo(lightningOrb.attacker, OrbStorageUtility.Get(path));
            Log.Debug("aaaa" + skinVFXInfo?.ReplacementEffectPrefab == null);
            
            if (!skinVFXInfo?.ReplacementEffectPrefab) return;
            effectData.genericUInt = skinVFXInfo.Identifier;
        });
        
        /*
         * IL_0170: ldloc.0
           IL_0171: call class [UnityEngine.CoreModule]UnityEngine.GameObject RoR2.Orbs.OrbStorageUtility::Get(string)
         */
        
        bool found2 = c.TryGotoNext(MoveType.After,
            x => x.MatchLdloc(out _),
            x => x.MatchCallOrCallvirt(typeof(OrbStorageUtility), nameof(OrbStorageUtility.Get))
        );

        if (!found2)
        {
            Log.Error($"Failed to apply SkinVFX IL hook on LightningOrbOnBegin 2");
            return;
        }

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<GameObject, LightningOrb, GameObject>>((orbEffect, lightningOrb) =>
        {
            SkinVFXInfo skinVFXInfo = SkinVFXManager.FindSkinVFXInfo(lightningOrb.attacker, orbEffect);
            return skinVFXInfo == null ? orbEffect : skinVFXInfo.ReplacementEffectPrefab;
        });
    }

    private static void ProjectileManagerOnFireProjectile_FireProjectileInfo(ProjectileManager.orig_FireProjectile_FireProjectileInfo orig, RoR2.Projectile.ProjectileManager self, FireProjectileInfo fireprojectileinfo)
    {
        SkinVFXManager.ApplyVFXInfo(fireprojectileinfo.owner, ref fireprojectileinfo.projectilePrefab);
        
        orig(self, fireprojectileinfo);
    }

    private static void GenericProjectileBaseStateOnOnEnter(GenericProjectileBaseState.orig_OnEnter orig, EntityStates.GenericProjectileBaseState self)
    {
        SkinVFXManager.ApplyVFXInfo(self.gameObject, ref self.projectilePrefab);
        
        orig(self);
    }
    
    private static void AimThrowableBaseAimOnlyOnOnEnter(AimThrowableBaseAimOnly.orig_OnEnter orig, EntityStates.AimThrowableBaseAimOnly self)
    {
        SkinVFXManager.ApplyVFXInfo(self.gameObject, ref self.projectilePrefab);
        SkinVFXManager.ApplyVFXInfo(self.gameObject, ref self.arcVisualizerPrefab);
        SkinVFXManager.ApplyVFXInfo(self.gameObject, ref self.endpointVisualizerPrefab);
        
        orig(self);
    }
    
    private static void ProjectileSingleTargetImpactOnOnProjectileImpact(On.RoR2.Projectile.ProjectileSingleTargetImpact.orig_OnProjectileImpact orig, RoR2.Projectile.ProjectileSingleTargetImpact self, ProjectileImpactInfo impactinfo)
    {
        SkinVFXManager.ApplyVFXInfo(self.projectileController?.owner, ref self.impactEffect);
        
        orig(self, impactinfo);
    }

    private static void ProjectileSimpleOnStart(On.RoR2.Projectile.ProjectileSimple.orig_Start orig, ProjectileSimple self)
    {
        SkinVFXManager.ApplyVFXInfo(self.gameObject.GetComponent<ProjectileController>()?.owner, ref self.lifetimeExpiredEffect);
        
        orig(self);
    }
    
    private static void ProjectileImpactExplosionOnDetonate(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        int effectLoc = -1;
        bool found = c.TryGotoNext(MoveType.After,
            x => x.MatchStfld<RoR2.EffectData>(nameof(EffectData.scale)),
            x => x.MatchStloc(out effectLoc)
        );

        if (!found)
        {
            Log.Error($"Failed to apply SkinVFX IL hook on ProjectileImpactExplosion.Detonate");
            return;
        }

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc, effectLoc);
        c.EmitDelegate<Action<ProjectileExplosion, EffectData>>((projectileExplosion, data) =>
        {
            SkinVFXInfo skinVFXInfo = SkinVFXManager.FindSkinVFXInfo(projectileExplosion.projectileController.owner, projectileExplosion.explosionEffect);

            if (skinVFXInfo == null)
            {
                return;
            }
            
            data.genericUInt = skinVFXInfo.Identifier;
        });
    }
    
    private static void OverlapAttackSkinned(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        /*
         * // effectData.SetHurtBoxReference(overlapInfo.hurtBox);
           IL_0095: ldloc.s 6
           IL_0097: ldloc.3
           IL_0098: ldfld class RoR2.HurtBox RoR2.OverlapAttack/OverlapInfo::hurtBox
           IL_009d: callvirt instance void RoR2.EffectData::SetHurtBoxReference(class RoR2.HurtBox)
         */

        int effectLoc = -1;
        bool found = c.TryGotoNext(MoveType.After,
            x => x.MatchLdloc(out effectLoc),
            x => x.MatchLdloc(out _),
            x => x.MatchLdfld<RoR2.OverlapAttack.OverlapInfo>(nameof(OverlapAttack.OverlapInfo.hurtBox)),
            x => x.MatchCallOrCallvirt<EffectData>(nameof(EffectData.SetHurtBoxReference))
        );

        if (!found)
        {
            Log.Error($"Failed to apply SkinVFX IL hook on OverlapAttackSkinned 1");
            return;
        }

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc, effectLoc);
        c.EmitDelegate<Action<OverlapAttack, EffectData>>((overlapAttack, data) =>
        {
            // Log.LogMessage(ProjectileOverlapAttack);
            // Log.LogMessage(ProjectileOverlapAttack.projectileController);
            SkinVFXInfo skinVFXInfo = SkinVFXManager.FindSkinVFXInfo(overlapAttack.attacker, overlapAttack.hitEffectPrefab);

            if (skinVFXInfo == null)
            {
                return;
            }

            Log.Message("aaaa 1");

            data.genericUInt = skinVFXInfo.Identifier;
        });

        /*
         * IL_018f: dup
           IL_0190: ldc.r4 2
           IL_0195: stfld float32 RoR2.EffectData::scale
         */

        bool found2 = c.TryGotoNext(MoveType.After,
            x => x.MatchDup(),
            x => x.MatchLdcR4(2),
            x => x.MatchStfld<RoR2.EffectData>(nameof(EffectData.scale))
        );

        if (!found2)
        {
            Log.Error($"Failed to apply SkinVFX IL hook on OverlapAttackSkinned 2");
            return;
        }

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Dup);
        c.EmitDelegate<Action<OverlapAttack, EffectData>>((overlapAttack, data) =>
        {
            // Log.LogMessage(ProjectileOverlapAttack);
            // Log.LogMessage(ProjectileOverlapAttack.projectileController);
            SkinVFXInfo skinVFXInfo = SkinVFXManager.FindSkinVFXInfo(overlapAttack.attacker, overlapAttack.hitEffectPrefab);

            if (skinVFXInfo == null)
            {
                return;
            }

            Log.Message("aaaa 2");

            data.genericUInt = skinVFXInfo.Identifier;
        });
    }
}