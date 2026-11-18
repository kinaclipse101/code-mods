using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IL.EntityStates.Engi.EngiMissilePainter;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Experimental.Playables;
using BuffDef = On.RoR2.BuffDef;

#pragma warning disable CS0618 // Type or member is obsolete

namespace SkinRecolor.SkinManager;

public static class Materials
{
    public static async Task MeshMatReplacements(RecoloredSkinJson recoloredSkinJson, bool dontAdd, SkinDef recoloredSkinDef, SkinDef originalSkin, GameObject body)
    {
        Log.Debug($"getting mat meshes for {originalSkin.name} for {recoloredSkinDef.name}");
        
        if (recoloredSkinDef.skinDefParams == null && recoloredSkinDef.skinDefParamsAddress == null)
        {
            //legacy skins use this i think .,.,
            CharacterModel.RendererInfo[] newRenderers = new CharacterModel.RendererInfo[recoloredSkinDef.rendererInfos.Length];

            for (int i = 0; i < recoloredSkinDef.rendererInfos.Length; i++)
            {
                newRenderers[i] = RecolorFromRenderInfo(recoloredSkinDef.rendererInfos[i], recoloredSkinJson, dontAdd, body);
            }

            recoloredSkinDef.rendererInfos = newRenderers;
            await LoadSkinDefParams(recoloredSkinDef, originalSkin);
        }
        else
        {
            SkinDefParams newParams = null;
            if (recoloredSkinDef.skinDefParams != null)
                newParams = recoloredSkinDef.skinDefParams;
            else
            {
                if (recoloredSkinDef.skinDefParamsAddress != null && recoloredSkinDef.skinDefParamsAddress.ToString() != "[]")
                {
                    Log.Debug(recoloredSkinDef.skinDefParamsAddress);
                    newParams = await Addressables.LoadAssetAsync<SkinDefParams>(recoloredSkinDef.skinDefParamsAddress.AssetGUID).Task;
                }
                else if (originalSkin.skinDefParams)
                {
                    newParams = SkinDefParams.FromSkinDef(originalSkin);
                }
            }

            newParams = Object.Instantiate(newParams);

            List<CharacterModel.RendererInfo> infos = [];
            if (recoloredSkinJson.matReplacements == null)
            {
                List<Renderer> newRenderers = new List<Renderer>();
                recoloredSkinJson.matReplacements = [];
                Renderer[] renderers = body.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    string matPath = Utils.GetTransformPath(body, renderer.gameObject).ToString();
                    string configName = GetConfigName(recoloredSkinJson, matPath);

                    MatReplacement matre = new MatReplacement()
                    {
                        configName = configName,
                        matPath = matPath,
                        specificHSV = recoloredSkinJson.hsv,
                    };
                    recoloredSkinJson.matReplacements.Add(matre);
                    newRenderers.Add(renderer);
                }

                foreach (var renderer in newParams.rendererInfos)
                {
                    if (newRenderers.Contains(renderer.renderer)) continue;
                    
                    string matPath = Utils.GetTransformPath(body, renderer.renderer.gameObject).ToString();
                    string configName = GetConfigName(recoloredSkinJson, renderer.renderer.gameObject.name);
                    MatReplacement matre = new MatReplacement()
                    {
                        configName = configName,
                        matPath = matPath,
                        specificHSV = recoloredSkinJson.hsv,
                    };
                    recoloredSkinJson.matReplacements.Add(matre);
                    newRenderers.Add(renderer.renderer);
                }
                
                SkinDef defaultSkin = Utils.GetModelLocator(BodyCatalog.FindBodyPrefab(recoloredSkinJson.baseSkinBody)).skins[0];
                SkinDefParams defaultSkinParams = null;
                if (defaultSkin.skinDefParams != null)
                    defaultSkinParams = defaultSkin.skinDefParams;
                else
                {
                    if (defaultSkin.skinDefParamsAddress != null && defaultSkin.skinDefParamsAddress.ToString() != "[]")
                    {
                        Log.Debug(defaultSkin.skinDefParamsAddress);
                        defaultSkinParams = await Addressables.LoadAssetAsync<SkinDefParams>(defaultSkin.skinDefParamsAddress.AssetGUID).Task;
                    }
                    else if (defaultSkin.skinDefParams)
                    {
                        defaultSkinParams = SkinDefParams.FromSkinDef(defaultSkin);
                    }
                }

                foreach (var rendererInfoIterate in defaultSkinParams.rendererInfos)
                {
                    if (newRenderers.Contains(rendererInfoIterate.renderer)) continue;

                    Log.Debug("had mat default skin renderer !!! " + rendererInfoIterate.renderer);
                    string matPath = Utils.GetTransformPath(body, rendererInfoIterate.renderer.gameObject).ToString();
                    string configName = GetConfigName(recoloredSkinJson, rendererInfoIterate.renderer.gameObject.name);
                    MatReplacement matre = new MatReplacement()
                    {
                        configName = configName,
                        matPath = matPath,
                        specificHSV = recoloredSkinJson.hsv,
                    };
                    recoloredSkinJson.matReplacements.Add(matre);
                    newRenderers.Add(rendererInfoIterate.renderer);
                }
            }

            List<Renderer> renderersReplaced = new List<Renderer>();
            foreach (var matReplacement in recoloredSkinJson.matReplacements)
            {
                var hsvOption = new HSVOption(matReplacement.specificHSV,
                    matReplacement.configName,
                    recoloredSkinJson.sectionName,
                    $"mat replacement on base skin .,,. path {matReplacement.matPath}");
                bool hasOption = false;
                foreach (var option in options)
                {
                    if (option.sectionName != recoloredSkinJson.sectionName || option.name != hsvOption.name) continue;
                    hasOption = true;
                    break;
                }

                if (!hasOption && !dontAdd)
                {
                    Log.Warning("adding option to roo " + matReplacement.configName + " " + recoloredSkinJson.sectionName);
                    ModSettingsManager.AddOption(hsvOption);
                    options.Add(hsvOption);
                }

                SkinDef defaultSkin = Utils.GetModelLocator(BodyCatalog.FindBodyPrefab(recoloredSkinJson.baseSkinBody)).skins[0];
                SkinDefParams defaultSkinParams = null;
                if (defaultSkin.skinDefParams != null)
                    defaultSkinParams = defaultSkin.skinDefParams;
                else
                {
                    if (defaultSkin.skinDefParamsAddress != null && defaultSkin.skinDefParamsAddress.ToString() != "[]")
                    {
                        Log.Debug(defaultSkin.skinDefParamsAddress);
                        defaultSkinParams = await Addressables.LoadAssetAsync<SkinDefParams>(defaultSkin.skinDefParamsAddress.AssetGUID).Task;
                    }
                    else if (defaultSkin.skinDefParams)
                    {
                        defaultSkinParams = SkinDefParams.FromSkinDef(defaultSkin);
                    }
                }
                
                CharacterModel.RendererInfo rendererInfo = new CharacterModel.RendererInfo();
                Renderer renderer = null;
                Material baseMat = null;
                if (matReplacement.matPath != "rendererInfo")
                {
                    Transform rendererObj = body.transform.Find(matReplacement.matPath);
                    if (!rendererObj)
                    {
                        Log.Warning("umm bad something went bad when light replacing " + matReplacement.matPath);
                        continue;
                    }

                    renderer = rendererObj.GetComponent<Renderer>();

                    baseMat = renderer.sharedMaterial;
                    foreach (var rendererInfoIterate in newParams.rendererInfos)
                    {
                        if (rendererInfoIterate.renderer != renderer) continue;

                        Log.Debug("had same renderer !!! " + rendererInfoIterate.renderer);
                        if (!rendererInfoIterate.defaultMaterial)
                        {
                            if (rendererInfoIterate.defaultMaterialAddress.Asset != null)
                            {
                                baseMat = (Material)rendererInfoIterate.defaultMaterialAddress.Asset;
                            }
                            else
                            {
                                baseMat = await rendererInfoIterate.defaultMaterialAddress.LoadAssetAsync().Task;
                            }
                        }
                        else
                        {
                            baseMat = rendererInfoIterate.defaultMaterial;
                        }
                        
                        rendererInfo.renderer = rendererInfoIterate.renderer;
                        rendererInfo.defaultMaterialAddress = rendererInfoIterate.defaultMaterialAddress;
                        rendererInfo.defaultMaterial = rendererInfoIterate.defaultMaterial;
                        rendererInfo.ignoreOverlays = rendererInfoIterate.ignoreOverlays;
                        rendererInfo.hideOnDeath = rendererInfoIterate.hideOnDeath;
                        rendererInfo.ignoresMaterialOverrides = rendererInfoIterate.ignoresMaterialOverrides;
                        rendererInfo.defaultShadowCastingMode = rendererInfoIterate.defaultShadowCastingMode;
                        Log.Debug("base mat = " + baseMat);
                        break;
                    }
                }
                else // catch ones only on default ksin 
                {
                    foreach (var rendererInfoIterate in newParams.rendererInfos)
                    {
                        if(renderersReplaced.Contains(rendererInfoIterate.renderer)) continue;
                        Log.Debug($"comapring {rendererInfoIterate.defaultMaterialAddress.AssetGUID} tp {matReplacement.configName}");
                        if (rendererInfoIterate.defaultMaterialAddress.AssetGUID != matReplacement.configName) continue;

                        Log.Debug("had mat renderer !!! " + rendererInfoIterate.renderer);
                        if (!rendererInfoIterate.defaultMaterial)
                        {
                            if (rendererInfoIterate.defaultMaterialAddress.Asset != null)
                            {
                                baseMat = (Material)rendererInfoIterate.defaultMaterialAddress.Asset;
                            }
                            else
                            {
                                baseMat = await rendererInfoIterate.defaultMaterialAddress.LoadAssetAsync().Task;
                            }
                        }
                        else
                        {
                            baseMat = rendererInfoIterate.defaultMaterial;
                        }
                        Log.Debug("base mat = " + baseMat);
                        break;
                    }

                    if (baseMat == null)
                    {
                        foreach (var rendererInfoIterate in defaultSkinParams.rendererInfos)
                        {
                            if(renderersReplaced.Contains(rendererInfoIterate.renderer)) continue;
                            Log.Debug($"comapring {rendererInfoIterate.defaultMaterialAddress.AssetGUID} tp {matReplacement.configName}");
                            if (rendererInfoIterate.defaultMaterialAddress.AssetGUID != matReplacement.configName) continue;

                            Log.Debug("had mat renderer !!! " + rendererInfoIterate.renderer);
                            if (!rendererInfoIterate.defaultMaterial)
                            {
                                if (rendererInfoIterate.defaultMaterialAddress.Asset != null)
                                {
                                    baseMat = (Material)rendererInfoIterate.defaultMaterialAddress.Asset;
                                }
                                else
                                {
                                    baseMat = await rendererInfoIterate.defaultMaterialAddress.LoadAssetAsync().Task;
                                }
                            }
                            else
                            {
                                baseMat = rendererInfoIterate.defaultMaterial;

                            }
                            Log.Debug("base mat = " + baseMat);
                            break;
                        }
                    }
                }

                if (baseMat == null)
                {
                    Log.Error($"somethign went bad base mat null on {matReplacement.matPath}");
                    continue;
                }
                
                Material newMat = Object.Instantiate(baseMat);
                if (!matReplacement.specificHSV.SameAsOriginal())
                    Utils.RecolorMaterial(ref newMat, baseMat, matReplacement.specificHSV, recoloredSkinJson, dontAdd);

                rendererInfo.renderer = renderer;
                rendererInfo.defaultMaterial = newMat;
                rendererInfo.defaultMaterialAddress = new AssetReferenceT<Material>("");
                infos.Add(rendererInfo);
                renderersReplaced.Add(renderer);
            }
            
            newParams.rendererInfos = infos.ToArray();
            
            recoloredSkinDef.optimizedSkinDefParams = newParams; 
            recoloredSkinDef.skinDefParams = newParams;
            recoloredSkinDef.skinDefParamsAddress = new AssetReferenceT<SkinDefParams>("");
        }
    }

    public static List<HSVOption> options = [];
    public static CharacterModel.RendererInfo RecolorFromRenderInfo(CharacterModel.RendererInfo rendererInfo, RecoloredSkinJson recoloredSkinJson, bool dontAdd, GameObject body)
    {
        string matPath = Utils.GetTransformPath(body, rendererInfo.renderer.gameObject).ToString();

        bool foundHsv = false;
        ReplacementHSV hsv = dontAdd ? recoloredSkinJson.hsv : null;
        MatReplacement matre = null;
        recoloredSkinJson.matReplacements ??= [];
        foreach (var matReplacement in recoloredSkinJson.matReplacements)
        {
            if (matReplacement.matPath != matPath) continue;

            foundHsv = true;
            matre = matReplacement;
            hsv = matre.specificHSV;
        }

        if (!foundHsv)
        {
            string configName = GetConfigName(recoloredSkinJson, matPath);

            matre = new MatReplacement()
            {
                configName = configName,
                matPath = matPath,
                specificHSV = hsv,
            };
            recoloredSkinJson.matReplacements.Add(matre);
        }
        
        if(!dontAdd && hsv != null)
        {
            var hsvOption = new HSVOption(hsv,
                matre.configName,
                recoloredSkinJson.sectionName,
                $"mat replacement on base skin .,,. path {matPath}");
            bool hasOption = false;
            foreach (var option in options)
            {
                if (option.sectionName != recoloredSkinJson.sectionName || option.name != hsvOption.name) continue;
                hasOption = true;
                break;
            }

            if (!hasOption)
            {
                Log.Warning("adding option to roo " + matre.configName + " " + recoloredSkinJson.sectionName);
                ModSettingsManager.AddOption(hsvOption);
                options.Add(hsvOption);
            }
        }
        else
        {
            hsv = recoloredSkinJson.hsv;
        }

        if (hsv == null)
        {
            Log.Debug("aaa");
            return rendererInfo;
        }
        
        Material baseMat = !rendererInfo.defaultMaterial
            ? rendererInfo.defaultMaterialAddress.LoadAssetAsync().WaitForCompletion()
            : rendererInfo.defaultMaterial;
        Material newMat = new Material(baseMat);
        if(!hsv.SameAsOriginal())
            Utils.RecolorMaterial(ref newMat, baseMat, hsv, recoloredSkinJson, dontAdd);
        rendererInfo.defaultMaterial = newMat;
        rendererInfo.defaultMaterialAddress = new AssetReferenceT<Material>("");

        return rendererInfo;
    }

    private static string GetConfigName(RecoloredSkinJson recoloredSkinJson, string matPath)
    {
        List<string> newRenderers = [];
        foreach (var matReplacement in recoloredSkinJson.matReplacements)
        {
            newRenderers.Add(matReplacement.configName);
        }
            
        StringBuilder fullPath = new StringBuilder();
        string[] split = matPath.Split('/');
        for (int i = split.Length - 1; i >= 0; i--)
        {
            fullPath.Append(split[i]);
            if (i == 0)
            {
                if (newRenderers.Contains(fullPath.ToString()))
                {
                    while (newRenderers.Contains(fullPath.ToString()))
                    {
                        fullPath.Append(" - Clone");
                    }
                }

                return fullPath.ToString();
            }
            else
            {
                if (newRenderers.Contains(fullPath.ToString())) continue;
                        
                return fullPath.ToString();
                break;
            }
        }

        return "";
    }

    public static async Task LoadSkinDefParams(SkinDef recoloredSkinDef, SkinDef originalSkin)
    {
        if (!recoloredSkinDef.skinDefParams)
        {
            SkinDefParams newParams = null;
            if (recoloredSkinDef.skinDefParamsAddress != null && recoloredSkinDef.skinDefParamsAddress.ToString() != "[]")
            {
                Log.Debug(recoloredSkinDef.skinDefParamsAddress);
                newParams = await Addressables.LoadAssetAsync<SkinDefParams>(recoloredSkinDef.skinDefParamsAddress.AssetGUID).Task;
            }
            else if (recoloredSkinDef.skinDefParams)
            {
                newParams = SkinDefParams.FromSkinDef(originalSkin);
            }
            
            recoloredSkinDef.optimizedSkinDefParams = newParams; 
            recoloredSkinDef.skinDefParams = newParams;
            recoloredSkinDef.skinDefParamsAddress = new AssetReferenceT<SkinDefParams>("");
        }
    }
}