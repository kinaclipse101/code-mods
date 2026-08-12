using System.Collections.Generic;
using BNR.patches;
using BepInEx.Configuration;
using On.RoR2.UI;
using RoR2;
using UnityEngine;
using UnityEngine.Analytics;

namespace BNR;

public class ukulelepronouns : PatchBase<ukulelepronouns>
{
    public override void Init()
    {
        applyHooks();
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            On.RoR2.UI.GenericNotification.SetItem += GenericNotificationOnSetItem; 
            On.RoR2.UI.ItemIcon.SetItemIndex_ItemIndex_int_float += ItemIconOnSetItemIndex_ItemIndex_int_float;
            On.RoR2.UI.HUD.ActivateScoreboard += HUDOnActivateScoreboard;
        }
        else
        {
            On.RoR2.UI.GenericNotification.SetItem -= GenericNotificationOnSetItem; 
            On.RoR2.UI.ItemIcon.SetItemIndex_ItemIndex_int_float -= ItemIconOnSetItemIndex_ItemIndex_int_float;
        }
    }

    private void HUDOnActivateScoreboard(HUD.orig_ActivateScoreboard orig, RoR2.UI.HUD self)
    {
        orig(self);

        if (!descriptionOverride.Value) return;
        
        foreach (RoR2.UI.ItemIcon itemIcon in self.itemInventoryDisplay.itemIcons)
        {
            if (itemIcon.itemIndex != RoR2.RoR2Content.Items.ChainLightning.itemIndex) return;
            itemIcon.tooltipProvider.overrideBodyText = Genderify(self.itemInventoryDisplay._characterBody, itemIcon.tooltipProvider.overrideBodyText.Replace(Language.GetString(ItemCatalog.GetItemDef(RoR2Content.Items.ChainLightning.itemIndex).descriptionToken), Language.GetString(ItemCatalog.GetItemDef(RoR2Content.Items.ChainLightning.itemIndex).pickupToken)));
            break;
        }
    }

    private void ItemIconOnSetItemIndex_ItemIndex_int_float(ItemIcon.orig_SetItemIndex_ItemIndex_int_float orig, RoR2.UI.ItemIcon self, ItemIndex newitemindex, int newitemcount, float newdurationpercent)
    {
        orig(self, newitemindex, newitemcount, newdurationpercent);
        if (ItemCatalog.GetItemDef(newitemindex).itemIndex != RoR2.RoR2Content.Items.ChainLightning.itemIndex) return;
        
        GameObject initialParent = self.transform.GetParent().gameObject;
        if (initialParent.name is not ("ItemInventoryDisplay" or "ItemsBackground")) return; // inventory display 
        
        CharacterBody localBody = initialParent.GetComponent<RoR2.UI.ItemInventoryDisplay>()?._characterBody;
        if (localBody == null) return;
            
        self.tooltipProvider.bodyToken = Genderify(localBody, Language.GetString(ItemCatalog.GetItemDef(newitemindex).pickupToken));
        
        if (!descriptionOverride.Value) return;
        if (self.tooltipProvider.overrideBodyText != "")
        {
            self.tooltipProvider.overrideBodyText = Genderify(localBody, self.tooltipProvider.overrideBodyText.Replace(Language.GetString(ItemCatalog.GetItemDef(newitemindex).descriptionToken), Language.GetString(ItemCatalog.GetItemDef(newitemindex).pickupToken)));
        }
    }

    private Dictionary<string, string> pronouns = new Dictionary<string, string>
    {
        {" she ", "her" },
        {" it ", "its" },
        {" they ", "their" },
    };

    private void GenericNotificationOnSetItem(GenericNotification.orig_SetItem orig, RoR2.UI.GenericNotification self, ItemDef itemdef)
    {
        orig(self, itemdef);
        
        if (itemdef.itemIndex != RoR2Content.Items.ChainLightning.itemIndex) return;
        CharacterBody localBody = LocalUserManager.GetFirstLocalUser()?.cachedBody;
        if(localBody == null) return;
        if (descriptionOverride.Value)
        {
            self.descriptionText.token = Genderify(localBody, Language.GetString(itemdef.pickupToken));
        }
        else
        {
            self.OverrideDescription(Genderify(localBody, Language.GetString(itemdef.pickupToken)));
        }
    }

    private string Genderify(CharacterBody body, string pickupText)
    {
        string[] overridePronouns = pronounOverrides.Value.Split(';');
        foreach (string overridePronoun in overridePronouns)
        {
            if(overridePronoun.Split(",")[0] != body.name.Replace("(Clone)", "")) continue;
            return pickupText.Replace("his", overridePronoun.Split(",")[1]);
        }
        
        SurvivorIndex survivorIndex = SurvivorCatalog.GetSurvivorIndexFromBodyIndex(body.bodyIndex);
        if (survivorIndex == SurvivorIndex.None || SurvivorCatalog.GetSurvivorDef(survivorIndex) == null) return pickupText;

        string outro = Language.GetString(SurvivorCatalog.GetSurvivorDef(survivorIndex)!.outroFlavorToken);
        Log.Debug($"ending string {outro}");
        foreach (string pronoun in pronouns.Keys)
        {
            if (outro.Contains(pronoun))
            {
                return pickupText.Replace("his", pronouns[pronoun]);
            }
        }

        return pickupText;
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - ukulelepronouns",
            "enable patches for ukulelepronouns",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) => { applyHooks(); };
        
        pronounOverrides = config.Bind("BNR - ukulelepronouns",
            "custom overrides",
            "",
            "example - CommandoBody,their;RailgunnerBody,their");
        Utils.StringConfig(pronounOverrides);
        
        descriptionOverride = config.Bind("BNR - ukulelepronouns",
            "whether to override hover text entirely",
            false,
            "should make it compatible with mods like looking glass without having to disable description text for everything there ,..,");
        Utils.CheckboxConfig(descriptionOverride);
    }

    private ConfigEntry<string> pronounOverrides;
    private ConfigEntry<bool> descriptionOverride;
    private ConfigEntry<bool> enabled;
}