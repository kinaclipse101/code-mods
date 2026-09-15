using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Color = UnityEngine.Color;

namespace BNR;

public class coloredits : PatchBase<coloredits>
{
    private static Dictionary<int, ConfigEntry<Color>> replacementStyleLookupDict = [];
    private static TMP_StyleSheet styleSheet;
    public override void Init()
    {
        ApplyHooks();
    }

    private static void ApplyHooks()
    {
        if (!enabled.Value) return;
        
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

        RoR2Application.onLoadFinished += OnLoadFinished;
    }

    private static void OnLoadFinished()
    {
        Addressables.LoadAssetAsync<TMP_StyleSheet>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.TextMesh_Pro_FormerResources.TMP_Default_Style_Sheet_asset).Completed +=
            handle =>
            {
                ConfigApplyStyleSheed(handle.Result);
            };
        return;

        void ConfigApplyStyleSheed(TMP_StyleSheet styleSheet)
        {
            foreach (var tmpStyle in styleSheet.styles.ToArray())
            {
                ColorUtility.TryParseHtmlString(tmpStyle.styleOpeningDefinition.Replace("<color=", "").Replace(">", ""), out Color initialColor);
                
                ConfigEntry<Color> newColor = butterscotchnroses.instance.Config.Bind("BNR - coloredits",
                    $"tmp style tag recolor for {(tmpStyle.name.StartsWith('c') ? tmpStyle.name[1..].Replace(")", "") : tmpStyle.name.Replace(")", ""))}",
                    initialColor,
                    "");
                ModSettingsManager.AddOption(new ColorOption(newColor));
                newColor.SettingChanged += (sender, args) =>
                {
                    styleSheet.styles.Remove(tmpStyle);
                    TMP_Style newStyle = new TMP_Style(tmpStyle.name, $"<color=#{ColorUtility.ToHtmlStringRGBA(newColor.Value)}>", tmpStyle.styleClosingDefinition);
                    styleSheet.styles.Add(newStyle);
                };
                
                styleSheet.styles.Remove(tmpStyle);
                TMP_Style newStyle = new TMP_Style(tmpStyle.name, $"<color=#{ColorUtility.ToHtmlStringRGBA(newColor.Value)}>", tmpStyle.styleClosingDefinition);
                styleSheet.styles.Add(newStyle);
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
        enabled.SettingChanged += (_, _) => { ApplyHooks(); };
    }

    private static ConfigEntry<bool> enabled;
}