using System;
using System.Collections.Generic;
using HG;

namespace SkinRecolor;

//note that  guids for ghostjsons arent particularly guids to the controller, just the ghost skin .,,. 
// this can be confusing when the configurationtype/fieldname point to the controller while the guid points to the ghost ,..,
public class ProjectileGhostJson : JsonBase
{
    public bool Equals(ProjectileGhostJson other)
    {
        return guid == other.guid && 
                configurationType == other.configurationType &&
               fieldName == other.fieldName && 
               Equals(subPrefabs, other.subPrefabs) &&
               Equals(typesAndPaths, other.typesAndPaths);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(guid, configurationType, fieldName, subPrefabs, typesAndPaths);
    }
}