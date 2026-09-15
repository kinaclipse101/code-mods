using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BNR.patches;
using BNR.items;
using butterscotchnroses;
using butterscotchnroses.artifacts;
using butterscotchnroses.skills;
using HarmonyLib;
using RiskOfOptions;
using UnityEngine;
using ShaderSwapper;

namespace BNR
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.Viliger.EnemiesReturns", BepInDependency.DependencyFlags.SoftDependency)]
    public class butterscotchnroses : BaseUnityPlugin
    {
        private const string PluginGUID = "zzz" + PluginAuthor + "." + PluginName;

        private const string PluginAuthor = "icebro";
        private const string PluginName = "BNR";
        private const string PluginVersion = "0.3.2";

        public static AssetBundle carvingKitBundle;
        public static AssetBundle bnrBundle;
        public static butterscotchnroses instance;
        public static List<PatchBase> patchBases = [];
        public static List<ModdedPatchBase> moddedPatchBases = [];
        public static Harmony harmony;
        public static ConfigEntry<bool> clientSide;
        public static ConfigEntry<bool> logDebug;
        
        public void Awake()
        {
            instance = this;
            Log.Init(Logger);
            
            clientSide = Config.Bind("BNR", "client side", false, "whether the mod should run in clientside mode or not .,,. disables content/changed behavior that would otherwise cause desyncs automatically !!!");
            Utils.CheckboxConfig(clientSide);
            logDebug = Config.Bind("BNR", "debug logs", false, "whether the mod should log debug logs.,.,");

            carvingKitBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "carvingkit_assets"));
            StartCoroutine(carvingKitBundle.UpgradeStubbedShadersAsync());
            bnrBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "bnrbundle"));
            
            harmony = new Harmony(Info.Metadata.GUID);
            
            IEnumerable<Type> patches = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(PatchBase)));
            foreach (Type patch in patches)
            {
                try
                {
                    PatchBase patchBase = (PatchBase)Activator.CreateInstance(patch);
                    patchBase.Config(Config);
                    patchBase.Init();
                    patchBases.Add(patchBase);
                }
                catch (Exception e)
                {
                    Log.Warning("failed to patch something ! probably fine if you dont have whatever mod that was attempted to be patched enabled ,..,,.");
                    Log.Warning(e);
                }
            }
            
#if !HOTRELOAD
            IEnumerable<Type> moddedPatches = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ModdedPatchBase)));
            foreach (Type patch in moddedPatches)
            {
                try
                {
                    ModdedPatchBase patchBase = (ModdedPatchBase)Activator.CreateInstance(patch);
                    patchBase.Config(Config);
                    patchBase.PreInit();
                    moddedPatchBases.Add(patchBase);
                }
                catch (Exception e)
                {
                    Log.Warning("failed to patch something ! probably fine if you dont have whatever mod that was attempted to be patched enabled ,..,,.");
                    Log.Warning(e);
                }
            }
#endif

            if (!clientSide.Value)
            {
                IEnumerable<Type> buffTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(BuffBase)));
                foreach (Type buffType in buffTypes)
                {
                    BuffBase buff = (BuffBase)Activator.CreateInstance(buffType);
                    buff.AddBuff();
                }
            
                IEnumerable<Type> itemTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ItemBase)));
                foreach (Type itemType in itemTypes)
                {
                    ItemBase item = (ItemBase)Activator.CreateInstance(itemType);
                    item.Init(Config);
                }
            
                IEnumerable<Type> skills = Assembly.GetExecutingAssembly().GetTypes().Where(x => !x.IsAbstract && x.IsSubclassOf(typeof(SkillBase)));
                //Log.Debug($"skills loaded: {skills.Count()}");
                foreach (Type skill in skills) {
                    SkillBase skillBase = (SkillBase)Activator.CreateInstance(skill);
                    skillBase.Init();
                }
            
                IEnumerable<Type> artifacts = Assembly.GetExecutingAssembly().GetTypes().Where(x => !x.IsAbstract && x.IsSubclassOf(typeof(ArtifactBase)));
                //Log.Debug($"skills loaded: {skills.Count()}");
                foreach (Type artifact in artifacts) {
                    ArtifactBase artifactBase = (ArtifactBase)Activator.CreateInstance(artifact);
                    artifactBase.Init(Config);
                }
            }
            
            oldconfigs.fixOldConfigs();

            ModSettingsManager.SetModIcon(bnrBundle.LoadAsset<Sprite>("icon"));
        }
        
        private void Update()
        {
#if HOTRELOAD
            if (Input.GetKeyUp(KeyCode.F5))
            {
                Log.Debug("hjotreloaing!");
                UnityHotReloadNS.UnityHotReload.LoadNewAssemblyVersion(typeof(butterscotchnroses).Assembly, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "butterscotchnroses.dll"));
            }
#endif  
            foreach (PatchBase patch in patchBases)
            {
                patch.Update();
            }
        }

        private void FixedUpdate()
        {
            foreach (PatchBase patch in patchBases)
            {
                patch.FixedUpdate();
            }
        }
    }
}