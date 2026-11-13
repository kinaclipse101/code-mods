using System;
using System.Collections.Generic;

namespace SkinRecolor;

public class VFXOverrideJson
{
    public string guid;
    public string configurationType;
    public string fieldName;
    public List<TypeToPath> typesAndPaths;
    public List<SubPrefab> subPrefabs;
}