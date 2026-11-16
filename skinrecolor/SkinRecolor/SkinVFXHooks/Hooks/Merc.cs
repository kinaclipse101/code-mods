using System;
using System.Collections.Generic;
using HG.GeneralSerializer;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using On.EntityStates.Merc;
using R2API;
using RoR2;
using SkinRecolor.RecoloredSkins;
using SkinRecolor.SkinManager;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace SkinRecolor.SkinVFXHooks;

public class Merc : SurvivorHookBase
{
    public override string survName => "MercBody";
    
    public override void Hooks()
    {
        AddRecoloredMat(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Merc.matMercEnergized_mat);
        
        On.EntityStates.Merc.FocusedAssaultPrep.OnEnter += FocusedAssaultPrepOnOnEnter;
    }

    private void FocusedAssaultPrepOnOnEnter(FocusedAssaultPrep.orig_OnEnter orig, EntityStates.Merc.FocusedAssaultPrep self)
    {
        // why are you like this merc .,,
        SkinDef skinDef = SkinCatalog.FindCurrentSkinDefForBodyInstance(self.gameObject);
        if (RecoloredSkinManager.recoloredSkinDefs.TryGetValue(skinDef, out var recoloredSkinJson))
        {
            GameObject preDash = self.FindModelChildGameObject("PreDashEffect");
            if (preDash)
            {
                Transform contracting = preDash.transform.Find("ContractingEnergy");
                if (contracting)
                {
                    RecoloredSkinContent.RecolorComponentBehavior(recoloredSkinJson, contracting.GetComponent<ParticleSystem>(), null);
                    RecoloredSkinContent.RecolorComponentBehavior(recoloredSkinJson, contracting.GetComponent<ParticleSystemRenderer>(), null);
                    contracting.name = "ContractingEnergyRecolored";
                }
                
                Transform point = preDash.transform.Find("Point Light");
                if (point)
                {
                    RecoloredSkinContent.RecolorComponentBehavior(recoloredSkinJson, point.GetComponent<Light>(), null);
                    point.name = "Point Light Recolored";
                }
                
                Transform fire = preDash.transform.Find("FireEmission");
                if (fire)
                {
                    RecoloredSkinContent.RecolorComponentBehavior(recoloredSkinJson, fire.GetComponent<ParticleSystem>(), null);
                    RecoloredSkinContent.RecolorComponentBehavior(recoloredSkinJson, fire.GetComponent<ParticleSystemRenderer>(), null);
                    fire.name = "FireEmissionRecolored";
                }
                
                Transform ring = preDash.transform.Find("BrightRing");
                if (ring)
                {
                    RecoloredSkinContent.RecolorComponentBehavior(recoloredSkinJson, ring.GetComponent<ParticleSystem>(), null);
                    RecoloredSkinContent.RecolorComponentBehavior(recoloredSkinJson, ring.GetComponent<ParticleSystemRenderer>(), null);
                    ring.name = "BrightRingRecolored";
                }
            }
        }
        
        orig(self);
    }
}