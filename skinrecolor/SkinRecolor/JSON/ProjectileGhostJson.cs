using System;
using System.Collections.Generic;
using HG;

namespace SkinRecolor;

public class ProjectileGhostJson
{
    public string GUID;
    public Type configurationType;
    public string controllerFieldName;
    public bool childProjectile;
    public List<string> lightPaths;
    public List<string> particlePaths;
    public List<string> meshPaths;
    public List<string> skinnedMeshPaths;
    public List<string> trailPaths;
}