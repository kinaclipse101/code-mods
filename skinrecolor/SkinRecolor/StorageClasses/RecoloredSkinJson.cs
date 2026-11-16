using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace SkinRecolor;

public class RecoloredSkinJson
{
    public string baseSkinName;
    public string baseSkinBody;
    
    public string skinName;
    public string prefix;

    public float hue;
    public float saturation;
    public float value;
    public bool multiplySat;

    public List<LightReplacement> lightReplacements;
    public List<ProjectileGhostJson> projectileGhosts;
    public List<VFXOverrideJson> vfxOverrides;
}