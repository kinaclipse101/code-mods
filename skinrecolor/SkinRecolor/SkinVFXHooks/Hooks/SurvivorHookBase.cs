using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using R2API;
using RoR2;
using RoR2.AsyncManagement;
using UnityEngine;

namespace SkinRecolor.SkinVFXHooks;

public class SurvivorHookBase
{
    public virtual string survName => "";
    public bool hasPatched;
    
    public virtual void Hooks()
    {
    }

    public virtual List<BaseSurvivorSpecificComponent> GetSurvivorSpecificComponents(GameObject gameObject)
    {
        return [];
    }
    
    public virtual async Task<List<VFXOverrideJson>> SpecificReskinnedObjects(RecoloredSkinJson recoloredSkinJson)
    {
        return [];
    }
    
    public static void ApplyVFXInfo(GameObject gameObject, ref GameObject skinObject)
    {
        SkinVFXManager.ApplyVFXInfo(gameObject, ref skinObject);
    }
    
    public static void ApplyVFXInfo(GameObject gameObject, ref EntityStateGameObject skinObject)
    {
        SkinVFXManager.ApplyVFXInfo(gameObject, ref skinObject);
    }

    public void AddRecoloredMat(string guid)
    {
        SkinVFXManager.AddRecoloredMat(guid, survName);
    }
}

public class BaseSurvivorSpecificComponent(Type type, string varName, GameObject gameObject)
{
    public Type type = type;
    public string varName = varName;
    public GameObject gameObject = gameObject;
}