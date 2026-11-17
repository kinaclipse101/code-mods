using System;
using System.Collections.Generic;
using HG.GeneralSerializer;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using On.EntityStates;
using On.EntityStates.Acolyte;
using On.EntityStates.Acolyte.Weapon;
using On.RoR2;
using On.RoR2.Projectile;
using R2API;
using SkinRecolor.RecoloredSkins;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = System.Object;

namespace SkinRecolor.SkinVFXHooks;

public class Acolyte : SurvivorHookBase
{
    public override string survName => "AcolyteBody";

    public override List<BaseSurvivorSpecificComponent> GetSurvivorSpecificComponents(GameObject gameObject)
    {
        List<BaseSurvivorSpecificComponent> components = [];
        
        if (gameObject.TryGetComponent(out RoR2.Projectile.BolaProjectileController bolaProjectileController))
        {
            if (bolaProjectileController.impactEffect)
            {
                components.Add(new BaseSurvivorSpecificComponent(typeof(RoR2.Projectile.BolaProjectileController), nameof(bolaProjectileController.impactEffect), bolaProjectileController.impactEffect));
                components.Add(new BaseSurvivorSpecificComponent(typeof(RoR2.Projectile.BolaProjectileController), nameof(bolaProjectileController.entangleEffect), bolaProjectileController.entangleEffect));
                components.Add(new BaseSurvivorSpecificComponent(typeof(RoR2.Projectile.BolaProjectileController), nameof(bolaProjectileController.absorbEffect), bolaProjectileController.absorbEffect));
                components.Add(new BaseSurvivorSpecificComponent(typeof(RoR2.Projectile.BolaProjectileController), nameof(bolaProjectileController.deathEffect), bolaProjectileController.deathEffect));
                components.Add(new BaseSurvivorSpecificComponent(typeof(RoR2.Projectile.BolaProjectileController), nameof(bolaProjectileController.pullEffect), bolaProjectileController.pullEffect));
            }
        }
        
        if (gameObject.TryGetComponent(out RoR2.Projectile.AcolyteJavelinProjectile acolyteJavelinProjectile))
        {
            if (acolyteJavelinProjectile.explosionEffect)
            {
                components.Add(new BaseSurvivorSpecificComponent(typeof(RoR2.Projectile.AcolyteJavelinProjectile), nameof(acolyteJavelinProjectile.explosionEffect), acolyteJavelinProjectile.explosionEffect));
                components.Add(new BaseSurvivorSpecificComponent(typeof(RoR2.Projectile.AcolyteJavelinProjectile), nameof(acolyteJavelinProjectile.blastImpactEffect), acolyteJavelinProjectile.blastImpactEffect));
                components.Add(new BaseSurvivorSpecificComponent(typeof(RoR2.Projectile.AcolyteJavelinProjectile), nameof(acolyteJavelinProjectile.lifetimeExpiredEffect), acolyteJavelinProjectile.lifetimeExpiredEffect));
            }
        }

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
                guid = RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC4_Acolyte.AcoThrowableVisualizer_prefab,
                configurationType = "AcolyteSpecific",
                fieldName = "AcoThrowableVisualizer",
            }
        ];

        foreach (var overrideJson in returnOverrides)
        {
            var vfxOverride = new VFXOverrideJson()
            {
                configurationType = overrideJson.configurationType,
                fieldName = overrideJson.fieldName,
                guid = overrideJson.guid,
                subPrefabs = [],
                typesAndPaths = []
            };
            
            GhostsFirstLoad.AddSupportedTypes(Addressables.LoadAssetAsync<GameObject>(overrideJson.guid).WaitForCompletion(), vfxOverride.typesAndPaths);
            recoloredSkinJson.vfxOverrides.Add(vfxOverride);
        }

        return returnOverrides;
    }
    
    public override void Hooks()
    {
        On.EntityStates.Acolyte.Weapon.BaseBladeCombo.OnEnter += BaseBladeComboOnOnEnter;
        On.EntityStates.Acolyte.BolaWhip.OnEnter += BolaWhipOnOnEnter;
        On.EntityStates.Acolyte.BladeDash.OnEnter += BladeDashOnOnEnter;
        BolaProjectileController.OnOwnerChanged += BolaProjectileControllerOnOnOwnerChanged;
        On.RoR2.Projectile.AcolyteBladeProjectile.Awake += AcolyteBladeProjectileOnAwake;
        On.RoR2.Projectile.AcolyteJavelinProjectile.Detonate += AcolyteJavelinProjectileOnDetonate;
        On.EntityStates.Acolyte.Weapon.Sacrifice.OnEnter += SacrificeOnOnEnter;
        On.RoR2.AcolyteFormController.OnEnable += AcolyteFormControllerOnOnEnable;
    }

    private void AcolyteFormControllerOnOnEnable(AcolyteFormController.orig_OnEnable orig, RoR2.AcolyteFormController self)
    {
        orig(self);

        Log.Debug("form controller 1");

        var msc = self.characterBody?.modelLocator?.modelTransform?.gameObject.GetComponent<RoR2.ModelSkinController>();
        if (msc)
        {
            msc.onSkinApplied += i =>
            {
                RoR2.SkinDef skindef = RoR2.SkinCatalog.FindCurrentSkinDefForBodyInstance(self.characterBody.gameObject);
                if (skindef && RecoloredSkinManager.recoloredSkinDefs.TryGetValue(skindef, out RecoloredSkinJson recoloredSkinJson))
                {
                    Log.Debug("form controller2");
                    Transform rightHand = self.characterBody?.modelLocator?.modelTransform?.Find("mdlAcolyte/right_arm");
                    if (!rightHand) return;
                    Log.Debug("form controller4");

                    var smr = rightHand.gameObject.GetComponent<SkinnedMeshRenderer>();
                    Material newMat = UnityEngine.Object.Instantiate(smr.material);
                    //note to add specific hsv here
                    Utils.RecolorMaterial(ref newMat, smr.material, recoloredSkinJson.hsv, recoloredSkinJson);
                    smr.material = newMat;
                    
                    Transform horns = self.characterBody?.modelLocator?.modelTransform?.Find("mdlAcolyte/horns");
                    var hornsRenderer = horns.gameObject.GetComponent<SkinnedMeshRenderer>();
                    hornsRenderer.material = newMat;

                    Transform right = self.characterBody?.modelLocator?.modelTransform?.Find("mdlAcolyte/mdlAcolyteBackpack/right_arm");
                    var rightRenderer = right.gameObject.GetComponent<SkinnedMeshRenderer>();
                    rightRenderer.material = newMat;

                    Transform left = self.characterBody?.modelLocator?.modelTransform?.Find("mdlAcolyte/mdlAcolyteBackpack/left_arm");
                    var leftRenderer = left.gameObject.GetComponent<SkinnedMeshRenderer>();
                    leftRenderer.material = newMat;


                }
            };
        }
        else
        {
            Log.Debug("form cdfasfasfdsdfsdfsdfdsdfdsdfontroller 2");
        }
        
    }

    private void SacrificeOnOnEnter(Sacrifice.orig_OnEnter orig, EntityStates.Acolyte.Weapon.Sacrifice self)
    {
        ApplyVFXInfo(self.gameObject, ref self.explosionEffectPrefab);
        ApplyVFXInfo(self.gameObject, ref self.armEffectPrefab);
        ApplyVFXInfo(self.gameObject, ref self.orbEffectPrefab);
        ApplyVFXInfo(self.gameObject, ref self.tetherEffectPrefab);
        
        orig(self);
    }
    
    private void AcolyteJavelinProjectileOnDetonate(AcolyteJavelinProjectile.orig_Detonate orig, RoR2.Projectile.AcolyteJavelinProjectile self)
    {
        ApplyVFXInfo(self.projectileController?.owner, ref self.explosionEffect);
        ApplyVFXInfo(self.projectileController?.owner, ref self.blastImpactEffect);

        orig(self);
    }

    private void BolaProjectileControllerOnOnOwnerChanged(BolaProjectileController.orig_OnOwnerChanged orig, RoR2.Projectile.BolaProjectileController self, GameObject newowner)
    {
        orig(self, newowner);
        
        ApplyVFXInfo(newowner, ref self.impactEffect);
        ApplyVFXInfo(newowner, ref self.entangleEffect);
        ApplyVFXInfo(newowner, ref self.absorbEffect);
        ApplyVFXInfo(newowner, ref self.deathEffect);
        ApplyVFXInfo(newowner, ref self.pullEffect);
    }

    private void AcolyteBladeProjectileOnAwake(AcolyteBladeProjectile.orig_Awake orig, RoR2.Projectile.AcolyteBladeProjectile self)
    {
        orig(self);
        
        ApplyVFXInfo(self.projectileController?.owner, ref self.lifetimeExpiredEffect);
    }
    
    private static void BladeDashOnOnEnter(BladeDash.orig_OnEnter orig, EntityStates.Acolyte.BladeDash self)
    {
        ApplyVFXInfo(self.gameObject, ref self.dashIntroVFX);
        ApplyVFXInfo(self.gameObject, ref self.dashLoopingVFX);
        
        orig(self);
    }

    private static void BolaWhipOnOnEnter(BolaWhip.orig_OnEnter orig, EntityStates.Acolyte.BolaWhip self)
    {
        ApplyVFXInfo(self.gameObject, ref self.projectilePrefab);
        
        orig(self);
    }
    
    private static void BaseBladeComboOnOnEnter(On.EntityStates.Acolyte.Weapon.BaseBladeCombo.orig_OnEnter orig, EntityStates.Acolyte.Weapon.BaseBladeCombo self)
    {
        ApplyVFXInfo(self.gameObject, ref self.swingEffectPrefab);
        ApplyVFXInfo(self.gameObject, ref self.hitEffectPrefab);
        
        orig(self);
    }
}