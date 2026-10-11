using BepInEx;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BetterHexOfLoss
{
    [BepInDependency("iDeathHD.UnityHotReload", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class BetterHexOfLoss : BaseUnityPlugin
    {
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int EmTex = Shader.PropertyToID("_EmTex");
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "kina";
        private const string PluginName = "BetterHexOfLoss";
        private const string PluginVersion = "1.0.0";
        
        private static AssetBundle lossBundle;
        
        public void Awake()
        {
            Log.Init(Logger);
            
            lossBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "lossbundle"));
            Texture runesDiffuse = lossBundle.LoadAsset<Texture>("TexHexRunesDiffuse");
            Sprite lossIcon = lossBundle.LoadAsset<Sprite>("loss");
            
            GameObject pickupPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC4/RoamingItemDisable/PickupRoamingItemDisable.prefab").WaitForCompletion();
            MeshRenderer runeRenderer = pickupPrefab.transform.Find("mdlHexrunes/rune8").GetComponent<MeshRenderer>();
            Material newRuneMat = Instantiate(runeRenderer.material);
            newRuneMat.SetTexture(MainTex, runesDiffuse);
            newRuneMat.SetTexture(EmTex, runesDiffuse);
            runeRenderer.material = newRuneMat;

            GameObject displayPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC4/RoamingItemDisable/DisplayRoamingItemDisable.prefab").WaitForCompletion();
            ItemDisplay itemDisplay = displayPrefab.GetComponent<ItemDisplay>();
            itemDisplay.rendererInfos[1].defaultMaterial = newRuneMat;
            displayPrefab.transform.Find("mdlHexrunes/rune8").GetComponent<MeshRenderer>().material = newRuneMat;
            
            ItemDef hexLoss = Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC4/RoamingItemDisable/RoamingItemDisable.asset").WaitForCompletion();
            hexLoss.pickupIconSpriteReference = null;
            hexLoss.pickupIconSprite = lossIcon;
        }
    }
}
