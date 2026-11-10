using System.Collections;
using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using On.RoR2;
using On.RoR2.UI;
using RiskOfOptions;
using RiskOfOptions.Options;
using UnityEngine;
using UnityEngine.SceneManagement;
using Stage = RoR2.Stage;

namespace BNR;

public class testsceneskybox : PatchBase<testsceneskybox>
{
    public override void Init()
    {
        applyHooks();
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            //On.RoR2.SceneDirector.Start += SceneDirectorOnStart;
            RoR2.Stage.onStageStartGlobal += StageOnStart;
        }
        else
        {
            //On.RoR2.SceneDirector.Start -= SceneDirectorOnStart;
            RoR2.Stage.onStageStartGlobal -= StageOnStart;
        }
    }

    private void StageOnStart(Stage stage)
    {
        if (SceneManager.GetActiveScene().name != "testscene") return;
        
        //Log.Debug($"new scene !! {RenderSettings.skybox} {SceneManager.GetActiveScene().name}");
                
        if (!skyboxMaterial)
        {
            skyboxMaterial = Object.Instantiate(RenderSettings.skybox);
            skyboxMaterial.SetColor(Tint, skyboxColor.Value);
        }
                
        RenderSettings.skybox = skyboxMaterial;
    }

    private static Material skyboxMaterial;

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - testscene",
            "enable patches for testscene",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) =>
        {
            applyHooks();
        };
        
        skyboxColor = config.Bind("BNR - testscene", 
            "skyboxColor", 
            Utils.Color255(152, 122, 144),
            "tint of skybox in testscene !!!");
        ModSettingsManager.AddOption(new ColorOption(skyboxColor));
    }
    
    private static ConfigEntry<Color> skyboxColor;
    private static readonly int Tint = Shader.PropertyToID("_Tint");
    private ConfigEntry<bool> enabled;
}