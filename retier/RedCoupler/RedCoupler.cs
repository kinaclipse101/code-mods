using BepInEx;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RedCoupler
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class RedCoupler : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "kina";
        private const string PluginName = "RedCoupler";
        private const string PluginVersion = "1.0.0";
        
        private static AssetBundle assetbundle;
        private ExplicitPickupDropTable dropTable;
        private ItemTierDef t3Def;
        
        public void Awake()
        {
            Log.Init(Logger);
            assetbundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "redcoupler"));
            
            Addressables.LoadAssetAsync<ExplicitPickupDropTable>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC3_SolusAmalgamator.dtBossSolusAmalgamator_asset).Completed += handle =>
            {
                dropTable = handle.Result;
            };
            Addressables.LoadAssetAsync<ItemTierDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common.Tier3Def_asset).Completed += handle =>
            {
                t3Def = handle.Result;
            };

            ItemCatalog.availability.onAvailable += UpgradeCoupler;
        }

        private void UpgradeCoupler()
        {
            DLC3Content.Items.ExtraEquipment._itemTierDef = t3Def;
            DLC3Content.Items.ExtraEquipment.pickupIconSprite = assetbundle.LoadAsset<Sprite>("texIconsTemplate"); 
            
            dropTable.pickupEntries[0].pickupDef = RoR2Content.Items.RoboBallBuddy;
        }
    }
}
