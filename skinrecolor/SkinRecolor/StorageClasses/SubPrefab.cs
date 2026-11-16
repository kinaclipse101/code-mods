using System;
using System.Collections.Generic;

namespace SkinRecolor;

public class SubPrefab
{
    public Type varType;
    public string varName;
    public varType fieldOrProperty;
    public string prefabName;
    public bool hasEffectData;
    public ProjectileGhostJson projectileGhostJson;
    public VFXOverrideJson vfxOverrideJson;
    
    public bool Equals(SubPrefab other)
    {
        if (other == null)
            return false;
        return varType == other?.varType && 
               varName == other.varName && 
               fieldOrProperty == other?.fieldOrProperty && 
               prefabName == other?.prefabName && 
               hasEffectData == other?.hasEffectData && 
               projectileGhostJson?.Equals(other?.projectileGhostJson) == true && 
               vfxOverrideJson?.Equals(other?.vfxOverrideJson) == true;
    }
}

public enum varType
{
    field,
    property
}