using System;
using System.Collections.Generic;
using HG;

namespace SkinRecolor;

public class ProjectileGhostJson : JsonBase
{
    public string ghostGUID;

    public bool Equals(ProjectileGhostJson other)
    {
        return ghostGUID == other.ghostGUID && 
                configurationType == other.configurationType &&
               fieldName == other.fieldName && 
               Equals(subPrefabs, other.subPrefabs) &&
               Equals(typesAndPaths, other.typesAndPaths);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ghostGUID, configurationType, fieldName, subPrefabs, typesAndPaths);
    }
}