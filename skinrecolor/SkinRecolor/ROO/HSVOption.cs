using System.IO;
using Newtonsoft.Json;
using RiskOfOptions.OptionConfigs;
using SkinRecolor;
using SkinRecolor.RecoloredSkins;

namespace RiskOfOptions.Options;

public class HSVOption : StringInputFieldOption
{
    private readonly ReplacementHSV replacementHSV;
    private string name;
    private string sectionName;
    private string desc;
    
    public HSVOption(ReplacementHSV replacementHSV, string name, string sectionName, string desc) : base(new InputFieldConfig() { submitOn = InputFieldConfig.SubmitEnum.OnSubmit, restartRequired = true}, replacementHSV.ToString())
    {
        this.replacementHSV = replacementHSV;
        this.name = name;
        this.sectionName = sectionName;
        this.desc = desc;
    }
    
    public override void SetProperties()
    {
        Log.Debug($"setting properties for {replacementHSV}");
        SetCategoryName(sectionName, config);
        SetName(name, config);
        SetDescription(desc, config);
    }
    
    public override string Value
    {
        get => replacementHSV.ToString();
        set
        {
            replacementHSV.SetFromString(value);
            File.WriteAllTextAsync(RecoloredSkinManager.jsonPath, JsonConvert.SerializeObject(RecoloredSkinManager.skinJsons, Formatting.Indented));
        }
    }
}