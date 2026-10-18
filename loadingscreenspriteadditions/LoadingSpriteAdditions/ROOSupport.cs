using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;

namespace LoadingSpriteAdditions;

internal static class ROOSupport
{
    internal static void AddConfig(ConfigEntry<LoadingSpriteAdditions.spriteType> configEntry)
    {
        ModSettingsManager.AddOption(new ChoiceOption(configEntry));
    }
    
    internal static void AddConfig(ConfigEntry<bool> configEntry)
    {
        ModSettingsManager.AddOption(new CheckBoxOption(configEntry));
    }
    
    internal static void AddConfig(ConfigEntry<string> configEntry)
    {
        ModSettingsManager.AddOption(new StringInputFieldOption(configEntry));
    }
    
    internal static void AddConfig(ConfigEntry<float> configEntry)
    {
        ModSettingsManager.AddOption(new SliderOption(configEntry, new SliderConfig()
        {
            min = 0,
            max = 100,
        }));
    }
}
