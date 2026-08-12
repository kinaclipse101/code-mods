using System.Collections;
using BNR.patches;
using BepInEx.Configuration;
using MonoMod.Cil;
using On.RoR2;
using RoR2.UI;
using UnityEngine;

namespace BNR;

public class loadingscreen : PatchBase<loadingscreen>
{
    public override void Init()
    {
        applyHooks();
    }

    private void applyHooks()
    {
        if (enabled.Value && Random.Range(0f, 100f) <= chanceForRandom.Value)
        {
            RoR2Application.Update += RoR2ApplicationOnUpdate;
            On.LoadingScreenCanvas.OnLoadComplete += LoadingScreenCanvasOnOnLoadComplete;

            void LoadingScreenCanvasOnOnLoadComplete(On.LoadingScreenCanvas.orig_OnLoadComplete orig, LoadingScreenCanvas self)
            {
                orig(self);
                RoR2Application.Update -= RoR2ApplicationOnUpdate;
            }
        }
    }

    private void RoR2ApplicationOnUpdate(RoR2Application.orig_Update orig, RoR2.RoR2Application self)
    {
        if (LoadingScreenCanvas.Instance?.percentage)
        {
            LoadingScreenCanvas.Instance.percentage.SetPercentValue(UnityEngine.Random.Range(int.MinValue,int.MaxValue));
        }
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - loadingscreen",
            "enable patches for loadingscreen",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) => { applyHooks(); };
        
        chanceForRandom = config.Bind("BNR - loadingscreen",
            "chance for loading screen to show random percents for no reason.,.,",
            0.5f,
            "hehe,. .,");
        Utils.SliderConfig(0, 100, chanceForRandom);
    }

    private ConfigEntry<float> chanceForRandom;
    private ConfigEntry<bool> enabled;
}