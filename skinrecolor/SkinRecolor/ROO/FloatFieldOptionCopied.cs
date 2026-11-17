using System.IO;
using BepInEx.Configuration;
using Newtonsoft.Json;
using RiskOfOptions.Components.Options;
using RiskOfOptions.OptionConfigs;
using SkinRecolor;
using SkinRecolor.RecoloredSkins;
using TMPro;
using UnityEngine;

namespace RiskOfOptions.Options;

public class FloatFieldOptionCopied : BaseOption, ITypedValueHolder<ReplacementHSV>
{
    private readonly string hsvName;
    private readonly ReplacementHSV hsvEntry;
    private readonly InputFieldConfig config;
    
    public FloatFieldOptionCopied(ReplacementHSV hsvEntry, string hsvName)
    {
        this.hsvName = hsvName;
        this.hsvEntry = hsvEntry;
        config = new InputFieldConfig();
    }

    public override string OptionTypeName { get; set; } = "replacement_hsv";
    public override ConfigEntryBase ConfigEntry { get; }

    public override GameObject CreateOptionGameObject(GameObject prefab, Transform parent)
    {
        GameObject button = Object.Instantiate(prefab, parent);

        var controller = button.GetComponentInChildren<InputFieldController>();

        controller.nameToken = GetNameToken();
        controller.settingToken = Identifier;

        controller.submitOn = InputFieldConfig.SubmitEnum.OnExitOrSubmit;
        controller.lineType = TMP_InputField.LineType.SingleLine;
        controller.richText = true;
        controller.characterValidation = TMP_InputField.CharacterValidation.None;

        controller.name = $"Mod Options HSV Field, {Name}";

        return button;
    }

    public override BaseOptionConfig GetConfig() => config;

    public override void SetProperties()
    {
        SetCategoryName("test", config);
        SetName(hsvName, config);
        SetDescription(hsvName, config);
    }
    
    public ReplacementHSV GetOriginalValue() => hsvEntry;

    public virtual ReplacementHSV Value
    {
        get => hsvEntry;
        set
        {
            hsvEntry.hue = value.hue;
            hsvEntry.saturation = value.saturation;
            hsvEntry.value = value.value;
            hsvEntry.multiplySat = value.multiplySat;
            
            File.WriteAllTextAsync(RecoloredSkinManager.jsonPath, JsonConvert.SerializeObject(RecoloredSkinManager.skinJsons, Formatting.Indented));
        }
    }

    // ReSharper disable once CompareOfFloatsByEqualityOperator
    public bool ValueChanged() => Value != GetOriginalValue();
}