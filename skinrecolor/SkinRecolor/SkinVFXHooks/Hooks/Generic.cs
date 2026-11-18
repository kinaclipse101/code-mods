using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using HG;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using On.EntityStates;
using R2API;
using RiskOfOptions.Options;
using RoR2;
using RoR2.AsyncManagement;
using RoR2.Orbs;
using RoR2.Projectile;
using SkinRecolor.RecoloredSkins;
using SkinRecolor.SkinVFXHooks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using CharacterModel = On.RoR2.CharacterModel;
using GenericDamageOrb = On.RoR2.Orbs.GenericDamageOrb;
using Object = UnityEngine.Object;
using OrbEffect = On.RoR2.Orbs.OrbEffect;
using ProjectileGhostReplacementManager = On.RoR2.ProjectileGhostReplacementManager;
using ProjectileManager = On.RoR2.Projectile.ProjectileManager;
using TemporaryOverlayInstance = On.RoR2.TemporaryOverlayInstance;

namespace SkinRecolor.SkinVFXHooks;

public static class Generic
{
    public static void Hook()
    {
        Harmony harmony = new Harmony(SkinRecolor.instance.Info.Metadata.GUID);
        harmony.CreateClassProcessor(typeof(ROOFixNRE)).Patch(); // lma ,..o.
        
        IL.RoR2.Projectile.ProjectileExplosion.DetonateServer += ProjectileImpactExplosionOnDetonate;
        IL.RoR2.OverlapAttack.ProcessHits += OverlapAttackSkinned;
        On.RoR2.Projectile.ProjectileSingleTargetImpact.OnProjectileImpact += ProjectileSingleTargetImpactOnOnProjectileImpact;
        On.RoR2.Projectile.ProjectileSimple.Start += ProjectileSimpleOnStart ;
        On.EntityStates.AimThrowableBaseAimOnly.OnEnter += AimThrowableBaseAimOnlyOnOnEnter;
        On.EntityStates.GenericProjectileBaseState.OnEnter += GenericProjectileBaseStateOnOnEnter;
        On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo += ProjectileManagerOnFireProjectile_FireProjectileInfo;
        IL.RoR2.Orbs.LightningOrb.Begin += LightningOrbOnBegin;
        On.RoR2.TemporaryOverlayInstance.AddToCharacterModel += TemporaryOverlayInstanceOnAddToCharacterModel;
        On.EntityStates.GenericBulletBaseState.OnEnter += GenericBulletBaseStateOnOnEnter;
        IL.RoR2.Orbs.GenericDamageOrb.Begin += GenericDamageOrbOnBegin; // r2api skins has a hook here too but they dont replace start/end effects <//3 .,,
        //On.RoR2.ProjectileGhostReplacementManager.TryFindProjectileGhostPrefab += ProjectileGhostReplacementManagerOnTryFindProjectileGhostPrefab;
    }

    [HarmonyPatch]
    public class ROOFixNRE
    {
        [HarmonyPatch(typeof(StringInputFieldOption), "ValueChanged")]
        [HarmonyPrefix]
        public static bool StringInputFieldOptionPrefix(StringInputFieldOption __instance, bool __result)
        {
            if (__instance is not HSVOption hsvOption) return true;
            
            __result = !string.Equals(hsvOption.replacementHSV.ToString(), __instance.originalValue, StringComparison.InvariantCulture);
            return false;

        }
    }

    private static bool ProjectileGhostReplacementManagerOnTryFindProjectileGhostPrefab(ProjectileGhostReplacementManager.orig_TryFindProjectileGhostPrefab orig, ProjectileController projectileController, out GameObject ghostResult, out AssetReferenceT<GameObject> ghostReference)
    {
        ghostResult = null;
        ghostReference = null;
        if (!projectileController.owner || !projectileController.owner.TryGetComponent<CharacterBody>(out var component))
        {
            Log.Debug("1");
            return false;
        }
        SkinDef bodySkinDef = SkinCatalog.GetBodySkinDef(component.bodyIndex, (int)component.skinIndex);
        if (!bodySkinDef)
        {
            Log.Debug($"{SkinCatalog.skinsByBody[(int)component.bodyIndex][(int)component.skinIndex].name}");
            return false;
        }
        ReadOnlyArray<SkinDef.GhostReplacementTemplate> ghostReplacementTemplates = bodySkinDef.runtimeSkin.ghostReplacementTemplates;
        for (int i = 0; i < ghostReplacementTemplates.Length; i++)
        {
            SkinDef.GhostReplacementTemplate ghostReplacementTemplate = ghostReplacementTemplates[(Index)i];
            if (ghostReplacementTemplate.projectileIndex == projectileController.catalogIndex && (bool)ghostReplacementTemplate.ghostReference.Result)
            {
                ghostResult = ghostReplacementTemplate.ghostReference.Result;
                ghostReference = ghostReplacementTemplate.ghostReference.address;
                Log.Debug("3");
                return true;
            }
        }
        Log.Debug("4");
        return false;
    }

    private static void GenericDamageOrbOnBegin(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        /* // EffectManager.SpawnEffect(GetOrbEffect(), effectData, transmit: true);
           IL_0056: ldarg.0
           IL_0057: callvirt instance class [UnityEngine.CoreModule]UnityEngine.GameObject RoR2.Orbs.GenericDamageOrb::GetOrbEffect()
         */
        
        int effectLoc = -1;
        bool found = c.TryGotoNext(MoveType.After,
            x => x.MatchLdarg(out _),
            x => x.MatchCallOrCallvirt(typeof(RoR2.Orbs.GenericDamageOrb), nameof(RoR2.Orbs.GenericDamageOrb.GetOrbEffect))
        );

        if (!found)
        {
            Log.Error($"Failed to apply SkinVFX IL hook on GenericDamageOrbOnBegin");
            return;
        }

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<GameObject, RoR2.Orbs.GenericDamageOrb, GameObject>>((orbEffect, genericOrb) =>
        {
            SkinVFXInfo skinVFXInfo = SkinVFXManager.FindSkinVFXInfo(genericOrb.attacker, orbEffect);
            Log.Debug($"found replacement >? {skinVFXInfo?.ReplacementEffectPrefab != null}");
            if (!skinVFXInfo?.ReplacementEffectPrefab) return orbEffect;

            if (!skinVFXInfo?.ReplacementEffectPrefab) return skinVFXInfo.ReplacementEffectPrefab;
            
            if(skinVFXInfo.ReplacementEffectPrefab?.TryGetComponent<RoR2.Orbs.OrbEffect>(out var orbEffectComponent) == true)
            {
                SkinVFXInfo startInfo = SkinVFXManager.FindSkinVFXInfo(genericOrb.attacker, orbEffectComponent.startEffect);
                if(startInfo?.ReplacementEffectPrefab)
                    orbEffectComponent.startEffect = startInfo.ReplacementEffectPrefab;
                    
                SkinVFXInfo endInfo = SkinVFXManager.FindSkinVFXInfo(genericOrb.attacker, orbEffectComponent.endEffect);
                if(endInfo?.ReplacementEffectPrefab)
                    orbEffectComponent.endEffect = endInfo.ReplacementEffectPrefab;
                
                Log.Debug($"found startInfo >? {startInfo?.ReplacementEffectPrefab} {orbEffectComponent.startEffect}");
                Log.Debug($"found endInfo >? {endInfo?.ReplacementEffectPrefab} {EffectCatalog.FindEffectIndexFromPrefab(orbEffectComponent.endEffect)}");
            }

            return skinVFXInfo.ReplacementEffectPrefab;
        });
    }

    private static GameObject GenericDamageOrbOnGetOrbEffect(GenericDamageOrb.orig_GetOrbEffect orig, RoR2.Orbs.GenericDamageOrb self)
    {
        GameObject result = orig(self);
        SkinVFXManager.ApplyVFXInfo(self.attacker, ref result);
        return result;
    }

    private static void GenericBulletBaseStateOnOnEnter(GenericBulletBaseState.orig_OnEnter orig, EntityStates.GenericBulletBaseState self)
    {
        SkinVFXManager.ApplyVFXInfo(self.gameObject, ref self.hitEffectPrefab);
        SkinVFXManager.ApplyVFXInfo(self.gameObject, ref self.muzzleFlashPrefab);
        SkinVFXManager.ApplyVFXInfo(self.gameObject, ref self.tracerEffectPrefab);

        orig(self);
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

    private static void LightningOrbOnBegin(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        /* // EffectManager.SpawnEffect(OrbStorageUtility.Get(path), effectData, transmit: true);
           IL_0170: ldloc.0
           IL_0171: call class [UnityEngine.CoreModule]UnityEngine.GameObject RoR2.Orbs.OrbStorageUtility::Get(string)
         */
        
        int effectLoc = -1;
        bool found = c.TryGotoNext(MoveType.After,
            x => x.MatchLdloc(out effectLoc),
            x => x.MatchCallOrCallvirt(typeof(RoR2.Orbs.OrbStorageUtility), nameof(RoR2.Orbs.OrbStorageUtility.Get))
        );

        if (!found)
        {
            Log.Error($"Failed to apply SkinVFX IL hook on LightningOrbOnBegin");
            return;
        }

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<GameObject, RoR2.Orbs.GenericDamageOrb, GameObject>>((orbEffect, genericOrb) =>
        {
            SkinVFXInfo skinVFXInfo = SkinVFXManager.FindSkinVFXInfo(genericOrb.attacker, orbEffect);
            Log.Debug($"found replacement >? {skinVFXInfo?.ReplacementEffectPrefab != null}");
            if (!skinVFXInfo?.ReplacementEffectPrefab) return orbEffect;

            if (!skinVFXInfo?.ReplacementEffectPrefab) return skinVFXInfo.ReplacementEffectPrefab;
            
            if(skinVFXInfo.ReplacementEffectPrefab?.TryGetComponent<RoR2.Orbs.OrbEffect>(out var orbEffectComponent) == true)
            {
                SkinVFXInfo startInfo = SkinVFXManager.FindSkinVFXInfo(genericOrb.attacker, orbEffectComponent.startEffect);
                if(startInfo?.ReplacementEffectPrefab)
                    orbEffectComponent.startEffect = startInfo.ReplacementEffectPrefab;
                    
                SkinVFXInfo endInfo = SkinVFXManager.FindSkinVFXInfo(genericOrb.attacker, orbEffectComponent.endEffect);
                if(endInfo?.ReplacementEffectPrefab)
                    orbEffectComponent.endEffect = endInfo.ReplacementEffectPrefab;
                
                Log.Debug($"found startInfo >? {startInfo?.ReplacementEffectPrefab} {orbEffectComponent.startEffect}");
                Log.Debug($"found endInfo >? {endInfo?.ReplacementEffectPrefab} {EffectCatalog.FindEffectIndexFromPrefab(orbEffectComponent.endEffect)}");
            }

            return skinVFXInfo.ReplacementEffectPrefab;
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

            overlapAttack.hitEffectPrefab = skinVFXInfo.ReplacementEffectPrefab;
            Log.Message("aaaa 1");

            //data.genericUInt = skinVFXInfo.Identifier;
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

            overlapAttack.hitEffectPrefab = skinVFXInfo.ReplacementEffectPrefab;
        });
    }
}