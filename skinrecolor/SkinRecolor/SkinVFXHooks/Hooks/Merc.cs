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
    
    //only thing left for merc is expose effect ?
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
                //note to add specific hsv here
                Transform contracting = preDash.transform.Find("ContractingEnergy");
                if (contracting)
                {
                    RecoloredSkinContent.RecolorComponentBehavior(recoloredSkinJson.hsv, contracting.GetComponent<ParticleSystem>(), null, recoloredSkinJson);
                    RecoloredSkinContent.RecolorComponentBehavior(recoloredSkinJson.hsv, contracting.GetComponent<ParticleSystemRenderer>(), null, recoloredSkinJson);
                    contracting.name = "ContractingEnergyRecolored";
                }
                
                Transform point = preDash.transform.Find("Point Light");
                if (point)
                {
                    RecoloredSkinContent.RecolorComponentBehavior(recoloredSkinJson.hsv, point.GetComponent<Light>(), null, recoloredSkinJson);
                    point.name = "Point Light Recolored";
                }
                
                Transform fire = preDash.transform.Find("FireEmission");
                if (fire)
                {
                    RecoloredSkinContent.RecolorComponentBehavior(recoloredSkinJson.hsv, fire.GetComponent<ParticleSystem>(), null, recoloredSkinJson);
                    RecoloredSkinContent.RecolorComponentBehavior(recoloredSkinJson.hsv, fire.GetComponent<ParticleSystemRenderer>(), null, recoloredSkinJson);
                    fire.name = "FireEmissionRecolored";
                }
                
                Transform ring = preDash.transform.Find("BrightRing");
                if (ring)
                {
                    RecoloredSkinContent.RecolorComponentBehavior(recoloredSkinJson.hsv, ring.GetComponent<ParticleSystem>(), null, recoloredSkinJson);
                    RecoloredSkinContent.RecolorComponentBehavior(recoloredSkinJson.hsv, ring.GetComponent<ParticleSystemRenderer>(), null, recoloredSkinJson);
                    ring.name = "BrightRingRecolored";
                }
            }
        }
        
        orig(self);
    }
}