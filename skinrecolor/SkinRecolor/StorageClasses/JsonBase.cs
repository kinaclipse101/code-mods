using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace SkinRecolor;

public class JsonBase
{
    public string guid;
    public string configurationType;
    public string fieldName;
    public List<TypeToPath> typesAndPaths;
    public List<SubPrefab> subPrefabs;

    public string configName;
    public ReplacementHSV specificHSV;
}