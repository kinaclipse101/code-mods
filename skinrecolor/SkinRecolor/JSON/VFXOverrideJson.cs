using System;
using System.Collections.Generic;

namespace SkinRecolor;

public class VFXOverrideJson : JsonBase
{
    public string guid;

    public bool Equals(VFXOverrideJson other)
    {
        if (guid == other.guid && guid != null && other.guid != null)
            return true;
        
        return (guid == other.guid || other.guid == null) && 
               configurationType == other.configurationType &&
               fieldName == other.fieldName && 
               Equals(subPrefabs, other.subPrefabs) &&
               Equals(typesAndPaths, other.typesAndPaths);
    }
}