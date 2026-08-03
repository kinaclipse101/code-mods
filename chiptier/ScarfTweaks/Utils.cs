using System.Collections.Generic;
using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;

namespace ScarfTweaks;

public static class Utils
{
    private static List<string> addedConfigs = [];
    public static ConfigEntry<float> SliderConfig(float min, float max, ConfigEntry<float> config)
    {
        if (addedConfigs.Contains(config.Definition.Key))
        {
            return config;
        }
        addedConfigs.Add(config.Definition.Key);
        
        StepSliderConfig stepSliderConfig = new StepSliderConfig
        {
            max = max,
            min = min,
            FormatString = "{0:0}"
        };
        StepSliderOption stepSliderOption = new StepSliderOption(config, stepSliderConfig);
        ModSettingsManager.AddOption(stepSliderOption);
        
        return config;
    }
    
    public static ConfigEntry<float> SliderConfig(ConfigEntry<float> config)
    {
        if (addedConfigs.Contains(config.Definition.Key))
        {
            return config;
        }
        addedConfigs.Add(config.Definition.Key);
        
        StepSliderConfig stepSliderConfig = new StepSliderConfig
        {
            max = (float)config.DefaultValue * 2f,
            min = 0,
            FormatString = "{0:0}"
        };
        StepSliderOption stepSliderOption = new StepSliderOption(config, stepSliderConfig);
        ModSettingsManager.AddOption(stepSliderOption);
        
        return config;
    }

    public static void SliderConfig(int min, int max, ConfigEntry<int> config)
    {
        IntSliderConfig intSliderConfig = new IntSliderConfig
        {
            max = max,
            min = min,
            formatString = "{0:0}"
        };
        IntSliderOption intSliderOption = new IntSliderOption(config, intSliderConfig);
        ModSettingsManager.AddOption(intSliderOption);
    }

    public static void CheckboxConfig(ConfigEntry<bool> config, bool restartRequired = false)
    {
        CheckBoxConfig checkBoxConfig = new CheckBoxConfig();
        checkBoxConfig.restartRequired = restartRequired;
        CheckBoxOption checkBoxOption = new CheckBoxOption(config, checkBoxConfig);
        ModSettingsManager.AddOption(checkBoxOption);
    }

    public static void StringConfig(ConfigEntry<string> config)
    {
        InputFieldConfig inputFieldConfig = new InputFieldConfig();
        StringInputFieldOption stringInputFieldOption = new StringInputFieldOption(config, inputFieldConfig);
        ModSettingsManager.AddOption(stringInputFieldOption);
    }

    public static void KeyboardConfig(ConfigEntry<KeyboardShortcut> config)
    {
        KeyBindConfig keyBindConfig = new KeyBindConfig();
        KeyBindOption keyBindOption = new KeyBindOption(config, keyBindConfig);
        ModSettingsManager.AddOption(keyBindOption);
    }

}