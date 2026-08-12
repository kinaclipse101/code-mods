using System;
using BNR.patches;
using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;

namespace BNR;

public class coloredits : PatchBase<coloredits>
{
    public override void Init()
    {
        applyHooks();
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            int colorTypes = ColorCatalog.indexToColor32.Length - 1;
            for (int i = 1; i < colorTypes; i++)
            {
                Color initialColor = ColorCatalog.indexToColor32[i];
                
                ConfigEntry<Color> newColor = butterscotchnroses.instance.Config.Bind("BNR - coloredits",
                    $"new color for {Enum.GetName(typeof(ColorCatalog.ColorIndex), i)}",
                    initialColor,
                    "");
                ModSettingsManager.AddOption(new ColorOption(newColor));
                int i1 = i; // i love rider ,. 
                newColor.SettingChanged += (sender, args) =>
                {
                    ColorCatalog.indexToColor32[i1] = newColor.Value;
                };

                ColorCatalog.indexToColor32[i] = newColor.Value;
            }
        }
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - coloredits",
            "enable patches for coloredits",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) => { applyHooks(); };
    }

    private ConfigEntry<bool> enabled;
}