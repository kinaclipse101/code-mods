using BepInEx;
using BepInEx.Configuration;
using BNR.patches;
using static BNR.butterscotchnroses;
using HarmonyLib;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using BossGroup = On.RoR2.BossGroup;

namespace BNR;

public class quickerhalcshrine : PatchBase<quickerhalcshrine>
{
    private GameObject bluePortalRef;
    public override void Init()
    {
        if (!enabled.Value)
            return;
        
        
        bluePortalRef = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_PortalShop.PortalShop_prefab).WaitForCompletion();
        
        On.RoR2.BossGroup.OnDefeatedServer += BossGroupOnOnDefeatedServer;
    }

    private void BossGroupOnOnDefeatedServer(BossGroup.orig_OnDefeatedServer orig, RoR2.BossGroup self)
    {
        orig(self);

        if (!greenToBlue.Value)
        {
            return;
        }
        
        GameObject greenportal = GameObject.Find("PortalColossus(Clone)");
        if (!greenportal)
        {
            return;
        }
        
        GameObject blueportal = Object.Instantiate(bluePortalRef);
        blueportal.transform.position = greenportal.transform.position;
        NetworkServer.Spawn(blueportal);
        Object.Destroy(greenportal);
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - Halyc Shrines",
            "enable patches for halyc shrines",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) =>
        {
            if (enabled.Value)
            {
                On.RoR2.BossGroup.OnDefeatedServer += BossGroupOnOnDefeatedServer;
            }
            else
            {
                On.RoR2.BossGroup.OnDefeatedServer -= BossGroupOnOnDefeatedServer;
            }
        };
        
        greenToBlue = config.Bind("BNR - Halyc Shrines",
            "turn green portal to blue",
            true,
            "makes green portals become blue after the boss has been defeated !!!");
        Utils.CheckboxConfig(greenToBlue);
    }

    private ConfigEntry<bool> enabled;
    private ConfigEntry<bool> greenToBlue;
}