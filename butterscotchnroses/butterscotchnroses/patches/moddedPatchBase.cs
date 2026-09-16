using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BNR.patches
{
    public abstract class ModdedPatchBase<T> : ModdedPatchBase where T : ModdedPatchBase<T>
    {
        public static T instance { get; private set; }

        public ModdedPatchBase()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ItemBase was instantiated twice");
            instance = this as T;
        }
    }
    public abstract class ModdedPatchBase
    {
        public virtual string chainLoaderKey => "";

        public void PreInit()
        {
            if (chainLoaderKey.IsNullOrWhiteSpace() || Chainloader.PluginInfos.ContainsKey(chainLoaderKey))
            {
                Init();
            }
            else
            {
                Log.Debug($"didnt finds {chainLoaderKey} loaded !!! not applyings patches ,.,,.");
            }
        }
        public abstract void Init();

        public abstract void Config(ConfigFile config);
        
        public virtual void Hooks() { }
        
        public virtual void FixedUpdate() { }
        public virtual void Update() { }
    }
}
