using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Bootstrap;
using Newtonsoft.Json;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using SkinRecolor.RecoloredSkins;
using UnityEngine;

namespace SkinRecolor
{
    [BepInDependency("iDeathHD.UnityHotReload", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class SkinRecolor : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "kina";
        private const string PluginName = "SkinRecolor";
        private const string PluginVersion = "1.0.0";

        private static bool UHRInstalled => Chainloader.PluginInfos.ContainsKey("iDeathHD.UnityHotReload");
        
        public void Awake()
        {
            Log.Init(Logger);
            
            RecoloredSkinManager.Start();
            SkinVFXManager.Hook();
            //start loading vfx here rather than content addition so you can async 
            RoR2.Run.onRunStartGlobal += run =>
            {
                commands.originalSkinIndex = -1;
            };
        }
        
        private void Update()
        {
#if DEBUG
            if (Input.GetKeyUp(KeyCode.F7))
            {
                if (UHRInstalled)
                {
                    Log.Debug(nameof(SkinRecolor) + ".dll");
                    UHRSupport.hotReload(typeof(SkinRecolor).Assembly, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, nameof(SkinRecolor) + ".dll"));
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
