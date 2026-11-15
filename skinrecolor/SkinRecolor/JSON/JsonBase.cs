using System.Collections.Generic;

namespace SkinRecolor;

public class JsonBase
{
    public string configurationType;
    public string fieldName;
    public List<TypeToPath> typesAndPaths;
    public List<SubPrefab> subPrefabs;
}