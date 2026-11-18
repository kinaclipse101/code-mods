using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace SkinRecolor;

public class RecoloredSkinJson
{
    public string baseSkinName;
    public string sectionName;
    public string baseSkinBody;
    
    public string skinName;
    public string prefix;

    public ReplacementHSV hsv;
    public bool replaceSkin;

    public List<MatReplacement> matReplacements;
    public List<LightReplacement> lightReplacements;
    public List<ProjectileGhostJson> projectileGhosts;
    public List<VFXOverrideJson> vfxOverrides;
}