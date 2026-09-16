using System.Collections;
using System.Collections.Generic;
using BNR.patches;
using BepInEx.Configuration;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BNR;

public class loadingscreen : PatchBase<loadingscreen>
{
    private Dictionary<string, SimpleSpriteAnimation> spriteAnimations = new Dictionary<string, SimpleSpriteAnimation>();
    public override void Init()
    {
        AddSpriteReplacement("CommandoWalk");
        AddSpriteReplacement("DroneHover");
        AddSpriteReplacement("GolemWalk");
        AddSpriteReplacement("LemurianWalk");
        AddSpriteReplacement("LoaderWalk");
        AddSpriteReplacement("ChildWalk");
        
        applyHooks();
    }
    
    private void AddSpriteReplacement(string ssaName)
    {
        bool enableReplacement = butterscotchnroses.instance.Config.Bind("BNR - loadingscreen", $"replace {ssaName.Replace("Walk", "").Replace("Hover", "")} on loading screen", true, "").Value;

        if (enableReplacement)
        {
            spriteAnimations.Add(ssaName, butterscotchnroses.bnrBundle.LoadAsset<SimpleSpriteAnimation>(ssaName));
        }
    }

    private void applyHooks()
    {
        if (enabled.Value && Random.Range(0f, 100f) <= chanceForRandom.Value)
        {
            RoR2Application.onUpdate += RoR2ApplicationOnUpdate;
            On.LoadingScreenCanvas.OnLoadComplete += LoadingScreenCanvasOnOnLoadComplete;

            void LoadingScreenCanvasOnOnLoadComplete(On.LoadingScreenCanvas.orig_OnLoadComplete orig, LoadingScreenCanvas self)
            {
                orig(self);
                RoR2Application.onUpdate -= RoR2ApplicationOnUpdate;
            }
        }

        if (enabled.Value)
        {
            On.RoR2.PickRandomObjectOnAwake.OnEnable += AddBlackBackgroundAndSpriteAnimations;
        }
        else
        {
            On.RoR2.PickRandomObjectOnAwake.OnEnable -= AddBlackBackgroundAndSpriteAnimations;
        }
    }

    private void AddBlackBackgroundAndSpriteAnimations(On.RoR2.PickRandomObjectOnAwake.orig_OnEnable orig, PickRandomObjectOnAwake self)
    {
        if (self.gameObject.name != "MiniScene")
        {
            orig(self);
            return;
        }

        foreach (GameObject gameObject in self.ObjectsToSelect)
        {
            if (spriteAnimations.TryGetValue(gameObject.name, out SimpleSpriteAnimation spriteAnimation))
            {
                gameObject.transform.GetChild(0).gameObject.GetComponent<SimpleSpriteAnimator>().animation = spriteAnimation;
                gameObject.transform.GetChild(0).gameObject.GetComponent<Image>().sprite = spriteAnimation.frames[0].sprite;
            }
        }
        
        orig(self);
    }

    private void RoR2ApplicationOnUpdate()
    {
        if (LoadingScreenCanvas.Instance?.percentage)
        {
            LoadingScreenCanvas.Instance.percentage.SetPercentValue(UnityEngine.Random.Range(int.MinValue,int.MaxValue));
        }
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - loadingscreen",
            "enable patches for loadingscreen",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) => { applyHooks(); };
        
        chanceForRandom = config.Bind("BNR - loadingscreen",
            "chance for loading screen to show random percents for no reason.,.,",
            0.5f,
            "hehe,. .,");
        Utils.SliderConfig(0, 100, chanceForRandom);
    }

    private ConfigEntry<float> chanceForRandom;
    private ConfigEntry<bool> enabled;
}