using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

//make a ppf in code then apply it (look at primal birthright for example
// fixed update random counter do it on a network behavior empty prefab object spawned and deleted on run start
// look at railgunner scope and see how boxes are drawn around headshot positions - use for eyes 
// skeleton gif 
namespace DidYouSeeThat
{
    [BepInDependency("iDeathHD.UnityHotReload", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class DidYouSeeThat : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "kina";
        private const string PluginName = "DidYouSeeThat";
        private const string PluginVersion = "1.0.0";
        
        public static DidYouSeeThat instance;
        public GameObject lookerControllerPrefab;
        public GameObject lookerOverlayPrefab;
        public GameObject lookerVisualizerPrefab;
        
        public GameObject runningPrefab;
        public GameObject grayscalePrefab;
        public Sprite eyes;
        public AssetBundle skeleBundle;

        public ConfigEntry<float> lookerChance;
        
        private static bool UHRInstalled => Chainloader.PluginInfos.ContainsKey("iDeathHD.UnityHotReload");
        
        public void Awake()
        {
            Log.Init(Logger);

            instance = this;

            lookerChance = Config.Bind("DidYouSeeThat", "odds for the looker ..,,.,", 0.1f, "odds for :eyes: .,,. 1 = 1%.,..,");
            
            skeleBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "skelebundle"));
            eyes = skeleBundle.LoadAsset<Sprite>("eyes");
            runningPrefab = skeleBundle.LoadAsset<GameObject>("RunningSkelly");
            grayscalePrefab = skeleBundle.LoadAsset<GameObject>("grayscaleObject");
            
            lookerControllerPrefab = PrefabAPI.CreateEmptyPrefab("lookerController");
            lookerControllerPrefab.AddComponent<LookerController>();
            
            lookerVisualizerPrefab = PrefabAPI.CreateEmptyPrefab("lookerVisualizer", false);
            lookerVisualizerPrefab.layer = LayerIndex.ui.intVal;
            if (lookerVisualizerPrefab.TryGetComponent(out NetworkIdentity netIdVisualizer))
                Destroy(netIdVisualizer);
            Image lookerImage = lookerVisualizerPrefab.AddComponent<Image>();
            lookerImage.sprite = eyes;
            lookerVisualizerPrefab.GetComponent<RectTransform>().localScale = new Vector3(10, 10, 10);
            
            lookerOverlayPrefab = PrefabAPI.CreateEmptyPrefab("lookerOverlay", false);
            lookerOverlayPrefab.layer = LayerIndex.ui.intVal;
            if (lookerOverlayPrefab.TryGetComponent(out NetworkIdentity netIdOverlay))
                Destroy(netIdOverlay);
            LookerTargetViewer stv = lookerOverlayPrefab.AddComponent<LookerTargetViewer>();
            stv.visualizerPrefab = lookerVisualizerPrefab;
            stv.enabled = false;
            lookerOverlayPrefab.AddComponent<SkeleRunner>();
            
            RoR2.Run.onRunStartGlobal += CreateLookerController;
        }

        private void CreateLookerController(Run obj)
        {
            if (!NetworkServer.active) return;
            
            GameObject lookerController = Instantiate(lookerControllerPrefab);
            NetworkServer.Spawn(lookerController);
        }

        private void Update()
        {
#if DEBUG
            if (Input.GetKeyUp(KeyCode.F7))
            {
                if (UHRInstalled)
                {
                    Log.Debug(nameof(DidYouSeeThat) + ".dll");
                    UHRSupport.hotReload(typeof(DidYouSeeThat).Assembly, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, nameof(DidYouSeeThat) + ".dll"));
                }
                else
                {
                    Log.Debug("couldnt finds unity hot reload !!");
                }
            }
#endif  
        }
    }
}
