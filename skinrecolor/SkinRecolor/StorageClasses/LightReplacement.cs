using BepInEx.Configuration;
using UnityEngine;

namespace SkinRecolor;

public class LightReplacement
{
    public string lightPath;
    public ConfigEntry<Color> recolorConfig;
}