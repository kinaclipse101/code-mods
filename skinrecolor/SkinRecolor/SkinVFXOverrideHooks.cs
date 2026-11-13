using System;
using System.Reflection;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using On.EntityStates;
using R2API;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace SkinRecolor;

public class SkinVFXOverrideHooks
{
    private static MethodInfo FindSkinVFXMethodInfo;
    
    private static SkinVFXInfo FindSkinVFXInfo(GameObject gameObject, GameObject skinObject)
    {
        return FindSkinVFXMethodInfo.Invoke(null, [gameObject, skinObject]) as SkinVFXInfo;
    }
    
    public static void Hook()
    {
        foreach (var methodInfo in AccessTools.GetDeclaredMethods(typeof(SkinVFX)))
        {
            if (methodInfo.Name != "FindSkinVFXInfo") continue;
            
            FindSkinVFXMethodInfo = methodInfo;
            Log.Debug("found FindSkinVFXInfo");
        }
        
        //general .,,.
        IL.RoR2.Projectile.ProjectileExplosion.DetonateServer += ProjectileImpactExplosionOnDetonate;
        IL.RoR2.OverlapAttack.ProcessHits += OverlapAttackSkinned;
        On.RoR2.Projectile.ProjectileSingleTargetImpact.OnProjectileImpact += ProjectileSingleTargetImpactOnOnProjectileImpact;
        
        //survivor specific .,., 
        
        //acolyt ,e.,,.
        On.EntityStates.Acolyte.Weapon.BaseBladeCombo.OnEnter += BaseBladeComboOnOnEnter;
        On.EntityStates.AimThrowableBaseAimOnly.OnEnter += AimThrowableBaseAimOnlyOnOnEnter;
        
    }

    private static void ProjectileSingleTargetImpactOnOnProjectileImpact(On.RoR2.Projectile.ProjectileSingleTargetImpact.orig_OnProjectileImpact orig, RoR2.Projectile.ProjectileSingleTargetImpact self, ProjectileImpactInfo impactinfo)
    {
        SkinVFXInfo impactEffect = FindSkinVFXInfo(self.projectileController?.owner, self.impactEffect);
        
        if(impactEffect?.ReplacementEffectPrefab)
            self.impactEffect = impactEffect.ReplacementEffectPrefab;
        
        orig(self, impactinfo);
    }

    private static void AimThrowableBaseAimOnlyOnOnEnter(AimThrowableBaseAimOnly.orig_OnEnter orig, EntityStates.AimThrowableBaseAimOnly self)
    {
        SkinVFXInfo projectilePrefab = FindSkinVFXInfo(self.gameObject, self.projectilePrefab);
        SkinVFXInfo arcVisualizerPrefab = FindSkinVFXInfo(self.gameObject, self.arcVisualizerPrefab);
        SkinVFXInfo endpointVisualizerPrefab = FindSkinVFXInfo(self.gameObject, self.endpointVisualizerPrefab);

        if(projectilePrefab?.ReplacementEffectPrefab)
            self.projectilePrefab = projectilePrefab.ReplacementEffectPrefab;
        if(arcVisualizerPrefab?.ReplacementEffectPrefab)
            self.arcVisualizerPrefab = arcVisualizerPrefab.ReplacementEffectPrefab;
        if(endpointVisualizerPrefab?.ReplacementEffectPrefab)
            self.endpointVisualizerPrefab = endpointVisualizerPrefab.ReplacementEffectPrefab;
        
        orig(self);
    }
    
    private static void BaseBladeComboOnOnEnter(On.EntityStates.Acolyte.Weapon.BaseBladeCombo.orig_OnEnter orig, EntityStates.Acolyte.Weapon.BaseBladeCombo self)
    {
        SkinVFXInfo swingEffectInfo = FindSkinVFXInfo(self.gameObject, self.swingEffectPrefab);
        SkinVFXInfo hitEffectInfo = FindSkinVFXInfo(self.gameObject, self.hitEffectPrefab);

        if (swingEffectInfo?.ReplacementEffectPrefab)
            self.swingEffectPrefab.SetDirectRef(swingEffectInfo.ReplacementEffectPrefab, true); 
        
        if (hitEffectInfo?.ReplacementEffectPrefab)
            self.hitEffectPrefab.SetDirectRef(hitEffectInfo.ReplacementEffectPrefab, true); 

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
            SkinVFXInfo skinVFXInfo = FindSkinVFXInfo(projectileExplosion.projectileController.owner, projectileExplosion.explosionEffect);

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
            SkinVFXInfo skinVFXInfo = FindSkinVFXInfo(overlapAttack.attacker, overlapAttack.hitEffectPrefab);

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
            SkinVFXInfo skinVFXInfo = FindSkinVFXInfo(overlapAttack.attacker, overlapAttack.hitEffectPrefab);

            if (skinVFXInfo == null)
            {
                return;
            }

            Log.Message("aaaa 2");

            data.genericUInt = skinVFXInfo.Identifier;
        });
    }
}