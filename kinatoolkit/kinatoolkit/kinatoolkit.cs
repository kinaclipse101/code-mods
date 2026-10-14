using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using kinatoolkit.patches;
using LOP;
using R2API;
using UnityEngine;
using UnityEngine.Networking;

namespace kinatoolkit
{
    [BepInDependency(DebugToolkit.DebugToolkit.GUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class kinatoolkit : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;

        private const string PluginAuthor = "kina";
        private const string PluginName = "kinatoolkit";
        private const string PluginVersion = "0.1.6";
        
        public static kinatoolkit instance;
        public GameObject artifactPortalPrefab;
        
        public void Awake()
        {
            instance = this;
            Log.Init(Logger);

            CreatePortalPrefab();
            
            commands.Init();
            
            IEnumerable<Type> patches = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(PatchBase)));
            foreach (Type patch in patches)
            {
                try
                {
                    PatchBase patchBase = (PatchBase)Activator.CreateInstance(patch);
                    patchBase.Config(Config);
                    patchBase.PreInit();
                }
                catch (Exception e)
                {
                    Log.Warning("failed to patch something !");
                    Log.Warning(e);
                }
            }
        }

        private void CreatePortalPrefab()
        {
            artifactPortalPrefab = PrefabAPI.CreateEmptyPrefab("TestingPortalPrefab", true);

            artifactPortalPrefab.AddComponent<NetworkIdentity>();
            var instantiateComponent = artifactPortalPrefab.AddComponent<InstantiateArtifactPortal>();

            var buttonList = new List<Transform>
            {
                AddButton(artifactPortalPrefab, new Vector3(5.09000015f, -19.5699997f, 20.9699993f), Vector3.zero),
                AddButton(artifactPortalPrefab, new Vector3(2f, -19.5699997f, 16.75f), Vector3.zero),
                AddButton(artifactPortalPrefab, new Vector3(-1.23000002f, -19.5699997f, 12.46f), Vector3.zero),
                AddButton(artifactPortalPrefab, new Vector3(0.949976802f, -19.5699997f, 23.9400082f), Vector3.zero),
                AddButton(artifactPortalPrefab, new Vector3(-2.0999999f, -19.5699997f, 19.7900009f), Vector3.zero),
                AddButton(artifactPortalPrefab, new Vector3(-5.41002321f, -19.5699997f, 15.6100082f), Vector3.zero),
                AddButton(artifactPortalPrefab, new Vector3(-3.0999999f, -19.5699997f, 26.7299995f), Vector3.zero),
                AddButton(artifactPortalPrefab, new Vector3(-5.90000629f, -19.5699997f, 22.8800106f), Vector3.zero),
                AddButton(artifactPortalPrefab, new Vector3(-9.36000633f, -19.5699997f, 18.6800117f), Vector3.zero)
            };

            instantiateComponent.artifactButtons = buttonList.ToArray();
            instantiateComponent.portalLocation = AddButton(artifactPortalPrefab, new Vector3(36.9399986f, -8.72000027f, -18.5f), new Vector3(0, 126.947906f, 0));
            instantiateComponent.laptopLocation = AddButton(artifactPortalPrefab, new Vector3(-13, -20, 16), new Vector3(0, -30, 0));

            artifactPortalPrefab.RegisterNetworkPrefab();
            
            Transform AddButton(GameObject parent, Vector3 position, Vector3 rotation)
            {
                var button = new GameObject();
                button.transform.parent = parent.transform;
                button.transform.localPosition = position;
                button.transform.rotation = Quaternion.Euler(rotation);

                return button.transform;
            }
        }

#if DEBUG
        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.F7))
            {
                UHRSupport.hotReload(typeof(kinatoolkit).Assembly, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "kinatoolkit.dll"));
            }
        }
#endif  
    }
}