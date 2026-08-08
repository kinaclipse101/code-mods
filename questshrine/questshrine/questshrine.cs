using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using questshrine.bases;
using questshrine.content.componentns;
using R2API;
using RoR2;
using ShaderSwapper;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace questshrine
{
    [BepInDependency("iDeathHD.UnityHotReload", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class questshrine : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "kina";
        private const string PluginName = "questshrine";
        private const string PluginVersion = "1.0.0";

        private static bool UHRInstalled => Chainloader.PluginInfos.ContainsKey("iDeathHD.UnityHotReload");

        public static questshrine instance;
        public static AssetBundle bundle;
        private static Material shrineMat = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_35_0.RoR2_Base_ShrineBlood.matShrineBlood_mat).WaitForCompletion(); 

        public void Awake()
        {
            Log.Init(Logger);

            instance = this;
            bundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "questshrinebundle"));
            StartCoroutine(bundle.UpgradeStubbedShadersAsync());

            LoadBases();
            
            GameObject questshrine = bundle.LoadAsset<GameObject>("questshrineprefab");
            questshrine.transform.Find("chanceshrine").Find("shrine").GetComponent<MeshRenderer>().material = shrineMat;
            
            QuestShrineComponent qsc = questshrine.AddComponent<QuestShrineComponent>();
            PurchaseInteraction interaction = questshrine.GetComponent<PurchaseInteraction>();
            qsc.purchaseInteraction = interaction;
            
            InteractableSpawnCard questisc = bundle.LoadAsset<InteractableSpawnCard>("questshrineisc");
            DirectorCard directorCard = new DirectorCard
            {
                selectionWeight = 100, // The higher this number the more common it'll be, for reference a normal chest is about 230
                spawnCard = questisc,
            };

            DirectorAPI.DirectorCardHolder directorCardHolder = new DirectorAPI.DirectorCardHolder
            {
                Card = directorCard,
                InteractableCategory = DirectorAPI.InteractableCategory.Shrines
            };
            
            // Registers the interactable on every stage
            DirectorAPI.Helpers.AddNewInteractable(directorCardHolder);
            // Or create your stage list and register it on each of those stages
            List<DirectorAPI.Stage> stageList =
            [
                DirectorAPI.Stage.DistantRoost,
                DirectorAPI.Stage.AbyssalDepthsSimulacrum
            ];

            foreach (DirectorAPI.Stage stage in stageList)
            {
                DirectorAPI.Helpers.AddNewInteractableToStage(directorCardHolder, stage);
            }
        }
        
        private void Update()
        {
#if DEBUG
            if (Input.GetKeyUp(KeyCode.F7))
            {
                if (UHRInstalled)
                {
                    Log.Debug(nameof(questshrine) + ".dll");
                    UHRSupport.hotReload(typeof(questshrine).Assembly, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, nameof(questshrine) + ".dll"));
                }
                else
                {
                    Log.Debug("couldnt finds unity hot reload !!");
                }
            }
#endif  
        }

        public readonly List<Type> questComponents = [];
        private void LoadBases()
        {
            IEnumerable<Type> itemTiers = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ItemTierBase)));
            foreach (Type itemTierType in itemTiers)
            {
                ItemTierBase itemTier = (ItemTierBase)Activator.CreateInstance(itemTierType);
                itemTier.Create();
            }
            
            IEnumerable<Type> itemTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(QuestItemBase)));
            foreach (Type questItem in itemTypes)
            {
                QuestItemBase item = (QuestItemBase)Activator.CreateInstance(questItem);
                item.Init(Config);
                if (item.enabled)
                {
                    questComponents.Add(item.ComponentType);
                }
            }
        }
    }
}
