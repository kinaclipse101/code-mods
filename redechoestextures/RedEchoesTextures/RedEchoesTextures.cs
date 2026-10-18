using BepInEx;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RedEchoesTextures
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class RedEchoesTextures : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "kina";
        private const string PluginName = "RedEchoesTextures";
        private const string PluginVersion = "1.0.0";
        
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int NormalTex = Shader.PropertyToID("_NormalTex");
        private static readonly int EmTex = Shader.PropertyToID("_EmTex");

        private AssetBundle fixedBundle;
        
        public void Awake()
        {
            Log.Init(Logger);
            
            fixedBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "redechoesbundle"));
            
            Material capeMat = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC4.matProvidenceCape_mat).WaitForCompletion();
            capeMat.SetTexture(MainTex, fixedBundle.LoadAsset<Texture>("texShadowProvidenceDiffuse1"));
            capeMat.SetTexture(NormalTex, fixedBundle.LoadAsset<Texture>("texShadowProvidenceNormal1"));
        
            Material bodyMat = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC4.matProvidenceBody_mat).WaitForCompletion();
            bodyMat.SetTexture(MainTex, fixedBundle.LoadAsset<Texture>("texShadowProvidenceDiffuse1-2MERGE"));
            bodyMat.SetTexture(NormalTex, fixedBundle.LoadAsset<Texture>("texShadowProvidenceDiffuse1-2MERGENORAML"));
            
            Material innerCapeMat = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC4.matProvidenceInnerCape_mat).WaitForCompletion();
            innerCapeMat.SetTexture(MainTex, fixedBundle.LoadAsset<Texture>("texShadowProvidenceDiffuse3"));
            innerCapeMat.SetTexture(NormalTex, fixedBundle.LoadAsset<Texture>("texShadowProvidenceNormal3"));
            innerCapeMat.SetTexture(EmTex, fixedBundle.LoadAsset<Texture>("texShadowProvidenceEmission3"));
        }
    }
}
