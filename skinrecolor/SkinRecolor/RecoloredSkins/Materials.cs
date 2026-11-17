using System.Threading.Tasks;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
#pragma warning disable CS0618 // Type or member is obsolete

namespace SkinRecolor.SkinManager;

public static class Materials
{
    public static async Task MeshMatReplacements(RecoloredSkinJson recoloredSkinJson, bool dontAdd, SkinDef recoloredSkinDef, SkinDef originalSkin, GameObject body)
    {
        if (recoloredSkinDef.skinDefParams == null && recoloredSkinDef.skinDefParamsAddress == null)
        {
            //legacy skins use this i think .,.,
            CharacterModel.RendererInfo[] newRenderers = new CharacterModel.RendererInfo[recoloredSkinDef.rendererInfos.Length];

            for (int i = 0; i < recoloredSkinDef.rendererInfos.Length; i++)
            {
                CharacterModel.RendererInfo renderer = recoloredSkinDef.rendererInfos[i];
                Material newMat = Object.Instantiate(renderer.defaultMaterial);
                //note to add specific hsv here
                Utils.RecolorMaterial(ref newMat, renderer.defaultMaterial, recoloredSkinJson.hsv, recoloredSkinJson, dontAdd);
                renderer.defaultMaterial = newMat;
                renderer.defaultMaterialAddress = new AssetReferenceT<Material>("");
                newRenderers[i] = renderer;
            }

            recoloredSkinDef.rendererInfos = newRenderers;
        }
        else
        {
            var newParams = Object.Instantiate(recoloredSkinDef.skinDefParamsAddress != null && recoloredSkinDef.skinDefParamsAddress.ToString() != "[]" ? recoloredSkinDef.skinDefParamsAddress.LoadAssetAsync().WaitForCompletion() : (recoloredSkinDef.skinDefParams == null ? SkinDefParams.FromSkinDef(originalSkin) : recoloredSkinDef.skinDefParams));
            
            for (int i = 0; i < newParams.rendererInfos.Length; i++)
            {
                Material baseMat = !newParams.rendererInfos[i].defaultMaterial
                    ? newParams.rendererInfos[i].defaultMaterialAddress.LoadAssetAsync().WaitForCompletion()
                    : newParams.rendererInfos[i].defaultMaterial;
                Material newMat = Object.Instantiate(baseMat);
                //Log.Debug($"new mat skinName = {newMat.name}");
                Utils.RecolorMaterial(ref newMat, baseMat, recoloredSkinJson.hsv, recoloredSkinJson, dontAdd);
                newParams.rendererInfos[i].defaultMaterial = newMat;
                newParams.rendererInfos[i].defaultMaterialAddress = new AssetReferenceT<Material>("");
            }
            
            recoloredSkinDef.optimizedSkinDefParams = newParams; 
            recoloredSkinDef.skinDefParams = newParams;
            recoloredSkinDef.skinDefParamsAddress = new AssetReferenceT<SkinDefParams>("");
        }
    }
}