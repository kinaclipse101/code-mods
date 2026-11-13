using System;
using System.Collections.Generic;

namespace SkinRecolor;

public class SubPrefab(List<Type> type, List<string> varName, List<varType> varType, List<string> prefabName, bool hasEffectData)
{
    public List<Type> type = type;
    public List<string> varName = varName;
    public List<varType> varType = varType;
    public List<string> prefabName = prefabName;
    public bool hasEffectData = hasEffectData;
    public ProjectileGhostJson projectileGhostJson;
    public VFXOverrideJson vfxOverrideJson;
}

public enum varType
{
    field,
    property
}