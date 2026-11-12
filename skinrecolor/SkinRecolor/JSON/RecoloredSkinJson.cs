using System.Collections.Generic;
using RoR2;

namespace SkinRecolor;

public class RecoloredSkinJson
{
    public string baseSkinName;
    public string baseSkinBody;
    
    public string skinName;
    public string prefix;
    
    public float hue;
    public float saturation;
    public bool multiplySat;
    public float value;

    public List<string> lightReplacements;
    public List<ProjectileGhostJson> projectileGhosts;
    public List<VFXOverrideJson> vfxOverrides;
}