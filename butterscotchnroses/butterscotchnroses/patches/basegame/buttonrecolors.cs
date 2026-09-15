using static BNR.butterscotchnroses;
using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using On.RoR2.UI.SkinControllers;
using RiskOfOptions;
using RiskOfOptions.Components.Options;
using RiskOfOptions.Options;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using CustomScrollbar = On.RoR2.UI.CustomScrollbar;
using MPDropdown = On.RoR2.UI.MPDropdown;

namespace BNR;

public class buttonrecolors : PatchBase<buttonrecolors>
{
    public override void Init()
    {
        if (!enabled.Value)
        {
            return;
        }
        
        On.RoR2.UI.SkinControllers.ButtonSkinController.Awake += ButtonSkinControllerOnAwake;
        On.RoR2.UI.SkinControllers.PanelSkinController.Awake += PanelSkinControllerOnAwake;
        On.RoR2.UI.MPDropdown.Awake += MPDropdownOnAwake;
        On.RoR2.UI.CustomScrollbar.Awake += CustomScrollbarOnAwake;
        
        if (!colorButtons.Value) return;
        Addressables.LoadAssetAsync<UISkinData>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_UI.skinCleanButton_asset).Completed += (handle) =>
            {
                ColorBlock colorVar = handle.Result.buttonStyle.colors;
                colorVar.normalColor = buttonNorm.Value;
                colorVar.highlightedColor = buttonHigh.Value;
                colorVar.pressedColor = buttonPress.Value;
                colorVar.selectedColor = buttonSelect.Value;
                colorVar.disabledColor = buttonDisabled.Value;
                handle.Result.buttonStyle.colors = colorVar;
            };
        harmony.CreateClassProcessor(typeof(RooDropdownPatch)).Patch();
    }
    
    [HarmonyPatch]
    public class RooDropdownPatch
    {
        [HarmonyPatch(typeof(RiskOfOptions.Components.Options.RooDropdown), "Awake")]
        [HarmonyPostfix]
        public static void CoolerEclipseAddSkyboxPreFix(RooDropdown __instance)
        {
            ColorBlock colorVar = __instance.colors;
            colorVar.normalColor = buttonNorm.Value;
            colorVar.highlightedColor = buttonHigh.Value;
            colorVar.pressedColor = buttonPress.Value;
            colorVar.selectedColor = buttonSelect.Value;
            colorVar.disabledColor = buttonDisabled.Value;
            __instance.colors = colorVar;
        }
    }

    private void MPDropdownOnAwake(MPDropdown.orig_Awake orig, RoR2.UI.MPDropdown self)
    {
        orig(self);
        if (!colorButtons.Value) return;
        
        ColorBlock colorVar = self.colors;
        colorVar.normalColor = buttonNorm.Value;
        colorVar.highlightedColor = buttonHigh.Value;
        colorVar.pressedColor = buttonPress.Value;
        colorVar.selectedColor = buttonSelect.Value;
        colorVar.disabledColor = buttonDisabled.Value;
        self.colors = colorVar;
    } 

    private void CustomScrollbarOnAwake(CustomScrollbar.orig_Awake orig, RoR2.UI.CustomScrollbar self)
    {
        orig(self);
        if (!colorButtons.Value) return;
        
        ColorBlock colorVar = self.colors;
        colorVar.normalColor = buttonNorm.Value;
        colorVar.highlightedColor = buttonHigh.Value;
        colorVar.pressedColor = buttonPress.Value;
        colorVar.selectedColor = buttonSelect.Value;
        colorVar.disabledColor = buttonDisabled.Value;
        self.colors = colorVar;
    }

    private static void PanelSkinControllerOnAwake(PanelSkinController.orig_Awake orig, RoR2.UI.SkinControllers.PanelSkinController self)
    {
        orig(self);
        if (!colorButtons.Value) return;
                    
        if (self.gameObject.name.Contains("ObjectivePanel")) return;//objective panel
        if (self.gameObject.name.Contains("NakedButton")) return; //back button css
        Image image = self.gameObject.GetComponent<Image>();
        if (image.gameObject.name.Contains("RuleBook")) return;

        image.color = Utils.Color255(buttonNorm.Value.r, buttonNorm.Value.g, buttonNorm.Value.b, image.color.a);
    }

    private static void ButtonSkinControllerOnAwake(ButtonSkinController.orig_Awake orig, RoR2.UI.SkinControllers.ButtonSkinController self)
    {
        orig(self);
        if (!colorButtons.Value) return;
                
        HGButton hgButton = self.gameObject.GetComponent<HGButton>();
        if (!hgButton || hgButton.name.Contains("SurvivorIcon")) return;
        if (hgButton.gameObject.name.Contains("Choice")) return; //difficulty icons
        if (hgButton.gameObject.name.Contains("Loadout")) return; //css
        if (hgButton.gameObject.name.Contains("ObjectivePanel")) return; 
        if (hgButton.gameObject.name.Contains("NakedButton")) return;
        if (hgButton.gameObject.transform.parent?.parent?.parent?.name.Contains("CommandPickerPanel(Clone)") == true) return;
                
        if (hgButton.gameObject.name.Contains("Music&More"))
        {
            hgButton.gameObject.SetActive(false); // sorry chris chris please dont kill me !!
        }
                
        ColorBlock colorVar = hgButton.colors;
        colorVar.normalColor = buttonNorm.Value;
        colorVar.highlightedColor = buttonHigh.Value;
        colorVar.pressedColor = buttonPress.Value;
        colorVar.selectedColor = buttonSelect.Value;
        colorVar.disabledColor = buttonDisabled.Value;
        hgButton.colors = colorVar;
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - UI",
            "enable patches for button recolors",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) =>
        {
            if (enabled.Value)
            {
                On.RoR2.UI.SkinControllers.ButtonSkinController.Awake += ButtonSkinControllerOnAwake;
                On.RoR2.UI.SkinControllers.PanelSkinController.Awake += PanelSkinControllerOnAwake;
            }
            else
            {
                On.RoR2.UI.SkinControllers.ButtonSkinController.Awake -= ButtonSkinControllerOnAwake;
                On.RoR2.UI.SkinControllers.PanelSkinController.Awake -= PanelSkinControllerOnAwake;
            }
        };
        
        colorButtons = config.Bind("BNR - UI",
            "change buttons colors", 
            false, 
            "whether or not to use custom menu button colors,..,"); 
        ModSettingsManager.AddOption(new CheckBoxOption(colorButtons));
            
        buttonNorm = config.Bind("BNR - UI",
            "normal button color",
            Utils.Color255(110, 83, 120),
            ""); 
        ModSettingsManager.AddOption(new ColorOption(buttonNorm));
            
        buttonHigh = config.Bind("BNR - UI",
            "highlighted button coor",
            Utils.Color255(255, 177, 245),
            ""); 
        ModSettingsManager.AddOption(new ColorOption(buttonHigh));
            
        buttonPress = config.Bind("BNR - UI",
            "pressed button color",
            Utils.Color255(192, 113, 182),
            ""); 
        ModSettingsManager.AddOption(new ColorOption(buttonPress));
            
        buttonSelect = config.Bind("BNR - UI",
            "selected button color",
            Utils.Color255(255, 177, 238),
            ""); 
        ModSettingsManager.AddOption(new ColorOption(buttonSelect));
        
        buttonDisabled = config.Bind("BNR - UI",
            "disabled button color",
            Utils.Color255(226, 197, 219),
            ""); 
        ModSettingsManager.AddOption(new ColorOption(buttonDisabled));
    }

    private ConfigEntry<bool> enabled;
    private static ConfigEntry<bool> colorButtons;
    private static ConfigEntry<Color> buttonNorm;
    private static ConfigEntry<Color> buttonHigh;
    private static ConfigEntry<Color> buttonPress;
    private static ConfigEntry<Color> buttonSelect;
    private static ConfigEntry<Color> buttonDisabled;
}