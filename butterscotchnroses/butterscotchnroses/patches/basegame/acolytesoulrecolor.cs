using System.Collections.Generic;
using BNR.patches;
using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using On.RoR2.UI;
using RoR2;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Analytics;

namespace BNR;

public class acolytesoulrecolor : PatchBase<acolytesoulrecolor>
{
    private static readonly int Color = Shader.PropertyToID("_Color");
    private static readonly int PrintRamp = Shader.PropertyToID("_PrintRamp");
    private static readonly int ColorRamp = Shader.PropertyToID("_RemapTex");
    
    public override void Init()
    {
        applyHooks();

        if (!enabled.Value) return;
        
        Texture engiRamp = Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_ColorRamps.texRampEngi_png).WaitForCompletion();
        GameObject soulBodyPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC4/Acolyte/AISoulBody.prefab").WaitForCompletion();
        CharacterModel soulModel = soulBodyPrefab.transform.Find("Model/mdlSoulBody").GetComponent<CharacterModel>();
        
        Material soulMaterialRecolored1 = Object.Instantiate(soulModel.baseRendererInfos[0].defaultMaterial);
        soulMaterialRecolored1.SetColor(Color, new Color32(56, 255, 48, 255));
        soulMaterialRecolored1.SetTexture(PrintRamp, engiRamp);
        
        Material soulMaterialRecolored2 = Object.Instantiate(soulModel.baseRendererInfos[1].defaultMaterial);
        soulMaterialRecolored2.SetColor(Color, new Color32(56, 255, 48, 255));
        soulMaterialRecolored2.SetTexture(ColorRamp, engiRamp);

        soulModel.baseRendererInfos[0].defaultMaterial = soulMaterialRecolored1;
        soulModel.baseRendererInfos[1].defaultMaterial = soulMaterialRecolored2;
        soulModel.baseLightInfos[0].defaultColor = new Color32(56, 255, 48, 255);
        
        GameObject explodePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC4/Acolyte/AcolyteSoulDeathEffect.prefab").WaitForCompletion();
        
        ParticleSystem psSpark = explodePrefab.transform.Find("Transform/Sparks").GetComponent<ParticleSystem>();
        ParticleSystem.MainModule mainModuleSpark = psSpark.main;
        mainModuleSpark.startColor = new ParticleSystem.MinMaxGradient
        {
            colorMin = new Color32(107, 255, 0, 255),
            colorMax = new Color32(0, 255, 33, 255)
        };
        
        ParticleSystem psTriangleSparks = explodePrefab.transform.Find("Transform/TriangleSparks").GetComponent<ParticleSystem>();
        ParticleSystem.MainModule mainModuleTriangleSparks = psTriangleSparks.main;
        mainModuleTriangleSparks.startColor = new ParticleSystem.MinMaxGradient
        {
            colorMin = new Color32(107, 255, 0, 255),
            colorMax = new Color32(0, 255, 33, 255)
        };
        
        ParticleSystem psFlames = explodePrefab.transform.Find("Transform/Unscaled Flames (1)").GetComponent<ParticleSystem>();
        ParticleSystem.MainModule mainModuleFlames = psFlames.main;
        mainModuleFlames.startColor = new ParticleSystem.MinMaxGradient
        {
            colorMin = new Color32(46, 191, 0, 255),
            colorMax = new Color32(28, 72, 0, 255)
        };
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            IL.RoR2.AcolyteExtractedSoulController.OnIncomingDamageServer += AcolyteExtractedSoulControllerOnIncomingDamageServer;
        }
        else
        {
            IL.RoR2.AcolyteExtractedSoulController.OnIncomingDamageServer -= AcolyteExtractedSoulControllerOnIncomingDamageServer;
        }
    }

    private void AcolyteExtractedSoulControllerOnIncomingDamageServer(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        int damageOrbIndex = -1;

        /* // DamageOrb damageOrb = new DamageOrb();
           IL_0011: newobj instance void RoR2.AcolyteExtractedSoulController/DamageOrb::.ctor()
           IL_0016: stloc.1
         */
        
        if (!cursor.TryGotoNext(MoveType.After,
                x => x.MatchNewobj(out _),
                x => x.MatchStloc(out damageOrbIndex)))
        {
            Log.Error("failed il");
            return;
        }
            
        /* // damageOrb.damageColorIndex = DamageColorIndex.Electrocution;
           IL_0096: ldloc.1
           IL_0097: ldc.i4.s 16
           IL_0099: stfld valuetype RoR2.DamageColorIndex RoR2.Orbs.GenericDamageOrb::damageColorIndex
         */
        if(!cursor.TryGotoNext(MoveType.After,
               x => x.MatchLdloc(damageOrbIndex),
               x => x.MatchLdcI4(out int _),
               x => x.MatchStfld<GenericDamageOrb>("damageColorIndex")))
        {
            Log.Error("failed il 2");
            return;
        }

        cursor.Index -= 2;
        cursor.Remove();
        cursor.Emit(OpCodes.Ldc_I4_S, (sbyte)DamageColorIndex.Poison);
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - acolytesoulrecolor",
            "enable patches for acolytesoulrecolor",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) => { applyHooks(); };
    }

    private ConfigEntry<string> pronounOverrides;
    private ConfigEntry<bool> descriptionOverride;
    private ConfigEntry<bool> enabled;
}