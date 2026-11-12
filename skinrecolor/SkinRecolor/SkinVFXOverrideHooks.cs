using System.Reflection;
using HarmonyLib;
using On.EntityStates;
using R2API;
using RoR2;
using UnityEngine;

namespace SkinRecolor;

public class SkinVFXOverrideHooks
{
    private static MethodInfo findskinvfxmethod;
    
    public static void Hook()
    {
        foreach (var methodInfo in AccessTools.GetDeclaredMethods(typeof(R2API.SkinVFX)))
        {
            if (methodInfo.Name != "FindSkinVFXInfo") continue;
            
            findskinvfxmethod = methodInfo;
            Log.Debug("found FindSkinVFXInfo");
        }
        
        On.EntityStates.Acolyte.Weapon.BaseBladeCombo.OnEnter += BaseBladeComboOnOnEnter;
        On.EntityStates.AimThrowableBaseAimOnly.OnEnter += AimThrowableBaseAimOnlyOnOnEnter;
    }

    private static void AimThrowableBaseAimOnlyOnOnEnter(AimThrowableBaseAimOnly.orig_OnEnter orig, EntityStates.AimThrowableBaseAimOnly self)
    {
        SkinVFXInfo projectilePrefab = FindSkinVFXInfo(self.gameObject, self.projectilePrefab);
        SkinVFXInfo arcVisualizerPrefab = FindSkinVFXInfo(self.gameObject, self.arcVisualizerPrefab);
        SkinVFXInfo endpointVisualizerPrefab = FindSkinVFXInfo(self.gameObject, self.endpointVisualizerPrefab);

        if(projectilePrefab?.ReplacementEffectPrefab)
            self.projectilePrefab = projectilePrefab.ReplacementEffectPrefab;
        if(arcVisualizerPrefab?.ReplacementEffectPrefab)
            self.arcVisualizerPrefab = arcVisualizerPrefab.ReplacementEffectPrefab;
        if(endpointVisualizerPrefab?.ReplacementEffectPrefab)
            self.endpointVisualizerPrefab = endpointVisualizerPrefab.ReplacementEffectPrefab;
        
        orig(self);
    }

    private static SkinVFXInfo FindSkinVFXInfo(GameObject gameObject, GameObject skinObject)
    {
        return findskinvfxmethod.Invoke(null, [gameObject, skinObject]) as SkinVFXInfo;
    }
    
    private static void BaseBladeComboOnOnEnter(On.EntityStates.Acolyte.Weapon.BaseBladeCombo.orig_OnEnter orig, EntityStates.Acolyte.Weapon.BaseBladeCombo self)
    {
        SkinVFXInfo swingEffectInfo = FindSkinVFXInfo(self.gameObject, self.swingEffectPrefab);
        SkinVFXInfo hitEffectInfo = FindSkinVFXInfo(self.gameObject, self.hitEffectPrefab);

        if (swingEffectInfo?.ReplacementEffectPrefab)
            self.swingEffectPrefab.SetDirectRef(swingEffectInfo.ReplacementEffectPrefab, true); 
        
        if (hitEffectInfo?.ReplacementEffectPrefab)
            self.hitEffectPrefab.SetDirectRef(hitEffectInfo.ReplacementEffectPrefab, true); 

        orig(self);
    }
}