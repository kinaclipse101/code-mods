using System.Collections.Generic;
using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;

namespace questshrine;

public static class Utils
{
    private static List<string> addedConfigs = [];
    public static void SliderConfig(float min, float max, ConfigEntry<float> config)
    {
        StepSliderConfig stepSliderConfig = new StepSliderConfig
        {
            max = max,
            min = min,
            FormatString = "{0:0}"
        };
        StepSliderOption stepSliderOption = new StepSliderOption(config, stepSliderConfig);
        ModSettingsManager.AddOption(stepSliderOption);
    }

    public static ConfigEntry<int> SliderConfig(ConfigEntry<int> config, int min = -1, int max = -1)
    {
        if (addedConfigs.Contains(config.Definition.Key))
        {
            return config;
        }
        addedConfigs.Add(config.Definition.Key);

        if (min == -1)
        {
            min = ((int)(config.DefaultValue))/2;
        }
        if (max == -1)
        {
            max = ((int)(config.DefaultValue)) * 2;
        }
        IntSliderConfig intSliderConfig = new IntSliderConfig
        {
            max = max,
            min = min,
            formatString = "{0:0}"
        };
        IntSliderOption intSliderOption = new IntSliderOption(config, intSliderConfig);
        ModSettingsManager.AddOption(intSliderOption);
        
        return config;
    }

    public static ConfigEntry<bool> CheckboxConfig(ConfigEntry<bool> config, bool restartRequired = false)
    {
        if (addedConfigs.Contains(config.Definition.Key))
        {
            return config;
        }
        addedConfigs.Add(config.Definition.Key);
        
        CheckBoxConfig checkBoxConfig = new CheckBoxConfig();
        checkBoxConfig.restartRequired = restartRequired;
        CheckBoxOption checkBoxOption = new CheckBoxOption(config, checkBoxConfig);
        ModSettingsManager.AddOption(checkBoxOption);

        return config;
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