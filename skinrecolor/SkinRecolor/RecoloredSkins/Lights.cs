using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RoR2;
using SkinRecolor.RecoloredSkins;
using UnityEngine;

namespace SkinRecolor.SkinManager;

public class Lights
{
    // add edge case for light replacements already existing on target skin
    public static async Task LightReplacements(RecoloredSkinJson recoloredSkinJson, SkinDef recoloredSkinDef, GameObject body)
    {
        if (recoloredSkinJson.lightReplacements == null)
        {
            recoloredSkinJson.lightReplacements = [];
            recoloredSkinDef.skinDefParams.lightReplacements = [];
            Light[] lights = body.GetComponentsInChildren<Light>(true); // im killing an angel with this and i know it <3 .,., 

            foreach (Light light in lights)
            {
                recoloredSkinJson.lightReplacements.Add(new LightReplacement()
                {
                    lightPath = Utils.GetTransformPath(body, light.gameObject).ToString()
                });
            }
            
            await File.WriteAllTextAsync(RecoloredSkinManager.jsonPath, JsonConvert.SerializeObject(RecoloredSkinManager.skinJsons, Formatting.Indented));
        } 
        
        foreach (LightReplacement lightReplacement in recoloredSkinJson.lightReplacements)
        {
            Transform lightObject = body.transform.Find(lightReplacement.lightPath);
            if (!lightObject)
            {
                Log.Warning("umm bad something went bad " + lightReplacement.lightPath);
                continue;
            }

            Light light = lightObject.GetComponent<Light>();
            var lightInfo = new CharacterModel.LightInfo(light);
            lightInfo.defaultColor = Utils.RecolorColor(recoloredSkinJson, light.color);
            HG.ArrayUtils.ArrayAppend(ref recoloredSkinDef.skinDefParams.lightReplacements, lightInfo);
        }
    }
}