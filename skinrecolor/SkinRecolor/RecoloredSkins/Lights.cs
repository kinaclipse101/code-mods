using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using SkinRecolor.RecoloredSkins;
using UnityEngine;

namespace SkinRecolor.SkinManager;

public class Lights
{
    // add edge case for light replacements already existing on target skin
    public static async Task LightReplacements(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, GameObject body)
    {
        bool addedROO = false;
        if (recoloredSkinJson.lightReplacements == null)
        {
            recoloredSkinJson.lightReplacements = [];
            recoloredSkinDef.skinDefParams.lightReplacements = [];
            Light[] lights = body.GetComponentsInChildren<Light>(true); // im killing an angel with this and i know it <3 .,., 

            List<string> newLights = [];
            foreach (Light light in lights)
            {
                var lightReplacement = new LightReplacement()
                {
                    lightPath = Utils.GetTransformPath(body, light.gameObject).ToString(),
                };

                StringBuilder fullPath = new StringBuilder();
                string[] split = lightReplacement.lightPath.Split('/');
                for (int i = split.Length - 1; i >= 0; i--)
                {
                    fullPath.Append(split[i]);
                    if (i == 0)
                    {
                        if (newLights.Contains(fullPath.ToString()))
                        {
                            while (newLights.Contains(fullPath.ToString()))
                            {
                                fullPath.Append(" - Clone");
                            }
                        }

                        lightReplacement.configName = fullPath.ToString();
                    }
                    else
                    {
                        if (newLights.Contains(fullPath.ToString())) continue;
                        
                        lightReplacement.configName = fullPath.ToString();
                        break;
                    }
                }

                lightReplacement.specificHSV = recoloredSkinJson.hsv;
                
                //RecoloredSkinManager.hsvConfigWorkaround.TryAdd(lightReplacement, recoloredSkinJson.hsv);
                Log.Warning("adding option to roo " + lightReplacement.configName);
                ModSettingsManager.AddOption(new HSVOption(lightReplacement.specificHSV, 
                    lightReplacement.configName, 
                    recoloredSkinJson.sectionName, 
                    $"light override on base skin .,,. path {lightReplacement.lightPath}"));
                addedROO = true;
                
                recoloredSkinJson.lightReplacements.Add(lightReplacement);
                newLights.Add(lightReplacement.configName);
            }
            
            //await File.WriteAllTextAsync(RecoloredSkinManager.jsonPath, JsonConvert.SerializeObject(RecoloredSkinManager.skinJsons, Formatting.Indented));
        } 
        
        foreach (LightReplacement lightReplacement in recoloredSkinJson.lightReplacements)
        {
            Transform lightObject = body.transform.Find(lightReplacement.lightPath);
            if (!lightObject)
            {
                Log.Warning("umm bad something went bad when light replacing " + lightReplacement.lightPath);
                continue;
            }

            Light light = lightObject.GetComponent<Light>();
            var lightInfo = new CharacterModel.LightInfo(light);
            
            //RecoloredSkinManager.hsvConfigWorkaround.TryAdd(lightReplacement, lightReplacement.specificHSV);
            if (!addedROO)
            {
                Log.Warning("adding option to roo " + lightReplacement.configName);
                ModSettingsManager.AddOption(new HSVOption(lightReplacement.specificHSV, 
                    lightReplacement.configName, 
                    recoloredSkinJson.sectionName, 
                    $"light override on base skin .,,. path {lightReplacement.lightPath}"));
            }
            
            
            lightInfo.defaultColor = Utils.RecolorColor(lightReplacement.specificHSV, light.color);
            HG.ArrayUtils.ArrayAppend(ref recoloredSkinDef.skinDefParams.lightReplacements, lightInfo);
        }
    }
}