using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BNR;
using BNR.items;
using GoldenCoastPlusRevived.Buffs;
using R2API;
using RoR2;
using RoR2.Items;
using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.UI;
using AllyCardManager = On.RoR2.UI.AllyCardManager;
using BuffBase = BNR.items.BuffBase;
using Object = System.Object;
using ShrineHealingBehavior = On.RoR2.ShrineHealingBehavior;

namespace BNR.items;

public class Crown : ItemBase<Crown>
{
    public override string ItemName => "crown";
    public override string ItemLangTokenName => "crown";
    public override string ItemPickupDesc => "";
    public override string ItemFullDescription => "";
    public override string ItemLore => "";
    public override ItemTier Tier => ItemTier.NoTier;
    public override GameObject ItemModel => butterscotchnroses.carvingKitBundle.LoadAsset<GameObject>("carvingkit.prefab");
    public override Sprite ItemIcon => butterscotchnroses.carvingKitBundle.LoadAsset<Sprite>("carvingkit.png");
    public override ItemTag[] ItemTags => [ItemTag.WorldUnique];
    
    public override void Init(ConfigFile config)
    {
        CreateConfig(config);
        CreateLang();
        CreateItem();
        Hooks();
        
        instance.ItemDef.hidden = true;
    }

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        return null;
    }

    public override void Hooks()
    {
        On.RoR2.UI.ScoreboardStrip.SetMaster += ScoreboardStripOnSetMaster;
        On.RoR2.UI.AllyCardController.UpdateInfo += AllyCardControllerOnUpdateInfo;
        RoR2.Stage.onStageStartGlobal += StageOnonStageStartGlobal;
    }

    public int updateOnStageStart;
    private void StageOnonStageStartGlobal(Stage obj)
    {
        updateOnStageStart = 0;
    }

    private void AllyCardControllerOnUpdateInfo(On.RoR2.UI.AllyCardController.orig_UpdateInfo orig, AllyCardController self)
    {
        orig(self);

        if (updateOnStageStart >= 10) return;
        
        updateOnStageStart++;
        Behavior.AddCrowns();
    }
    

    private void ScoreboardStripOnSetMaster(On.RoR2.UI.ScoreboardStrip.orig_SetMaster orig, ScoreboardStrip self, CharacterMaster newmaster)
    {
        orig(self, newmaster);
        
        Behavior.AddCrowns();
    }

    public sealed class Behavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnClient = true, useOnServer = true)]
        private static ItemDef GetItemDef() => instance?.ItemDef;
        
        public void OnEnable()
        {
            AddCrowns();
        }

        public static void AddCrowns()
        {
            if (Util.GetItemCountGlobal(instance.ItemDef.itemIndex, false) == 0) return;
            
            foreach (HUD hud in HUD.instancesList)
            {
                for (int i = 0; i < hud.scoreboardPanel?.transform.Find("Container/StripContainer").childCount; i++)
                {
                    Log.Debug("aa" + hud.scoreboardPanel.transform.Find("Container/StripContainer").GetChild(i).name);
                    
                    Transform panel = hud.scoreboardPanel.transform.Find("Container/StripContainer").GetChild(i);
                    ScoreboardStrip sbs = panel.gameObject.GetComponent<ScoreboardStrip>();

                    if (sbs?.master?.inventory?.GetItemCountEffective(instance.ItemDef) is 0 or null) continue;
                    
                    GameObject crownObj = new GameObject("crown");
                    crownObj.transform.parent = panel.Find("LongBackground/ClassBackground/ClassIcon").transform;
                    crownObj.transform.localPosition = Vector3.zero;
                
                    RawImage rawImage = crownObj.AddComponent<RawImage>();
                    rawImage.texture = whodiddamage.CrownIcon;//LocalUserManager.GetFirstLocalUser().userProfile.portraitTexture;
                    if (whodiddamage.crownColorBasedOffBody.Value)
                    {
                        rawImage.color = sbs.master.bodyPrefab.GetComponent<CharacterBody>().bodyColor;
                    }
                    if (whodiddamage.crownColorOverrides.Value != "")
                    {
                        string? steamID = sbs.master?.playerCharacterMasterController?.networkUser?.id.steamId.ToSteamID();
                        if (steamID != null)
                        {
                            Log.Debug($"target steamID: {steamID.Split(':')[^1]}");
                            string[] values = whodiddamage.crownColorOverrides.Value.Split(',');
                            foreach (var value in values)
                            {
                                Log.Debug($"config: {value.Split(':')[^1]}");
                            }
                            for (int j = 0; j < values.Length; j += 2)
                            {
                                values[j] = values[j].Trim();
                                if (values[j].Split(':')[^1] == steamID.Split(':')[^1])
                                {
                                    rawImage.color = whodiddamage.GetHex(values[j + 1]);
                                }
                            }
                        }
                    }
            
                    RectTransform rectTransform = crownObj.GetComponent<RectTransform>();
                    rectTransform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                    rectTransform.pivot = new Vector2(0.5f, -0.5f);
                }
                
                if (hud?.allyCardManager?.cardAllocator?.elements == null) continue;
                
                foreach (AllyCardController cardController in hud.allyCardManager.cardAllocator.elements)
                {
                    GameObject crownDisplay = cardController.portraitIconImage.transform.Find("crown")?.gameObject;
                    bool hasItem = cardController.sourceMaster?.inventory?.GetItemCountEffective(instance.ItemDef) > 0;

                    if (hasItem && !crownDisplay)
                    {
                        //create
                        GameObject crownObj = new GameObject("crown");
                        crownObj.transform.parent = cardController.portraitIconImage.transform;
                        crownObj.transform.localPosition = Vector3.zero;
            
                        RawImage rawImage = crownObj.AddComponent<RawImage>();
                        rawImage.texture = whodiddamage.CrownIcon;//LocalUserManager.GetFirstLocalUser().userProfile.portraitTexture;
                        if (whodiddamage.crownColorBasedOffBody.Value)
                        {
                            rawImage.color = cardController.sourceMaster.bodyPrefab.GetComponent<CharacterBody>().bodyColor;
                        }
                        if (whodiddamage.crownColorOverrides.Value != "")
                        {
                            string? steamID = cardController.sourceMaster?.playerCharacterMasterController?.networkUser?.id.steamId.ToSteamID();
                            if (steamID != null)
                            {
                                Log.Debug($"target steamID: {steamID.Split(':')[^1]}");
                                string[] values = whodiddamage.crownColorOverrides.Value.Split(',');
                                foreach (var value in values)
                                {
                                    Log.Debug($"config: {value.Split(':')[^1]}");
                                }
                                for (int i = 0; i < values.Length; i += 2)
                                {
                                    values[i] = values[i].Trim();
                                    if (values[i].Split(':')[^1] == steamID.Split(':')[^1])
                                    {
                                        rawImage.color = whodiddamage.GetHex(values[i + 1]);
                                    }
                                }
                            }
                        }
        
                        RectTransform rectTransform = crownObj.GetComponent<RectTransform>();
                        rectTransform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                        rectTransform.pivot = new Vector2(0.5f, -0.5f);
                    }

                    if (!hasItem && crownDisplay)
                    {
                        //destroy
                        Destroy(crownDisplay);
                    }
                }
            }
        }

        public void OnDisable()
        {
            foreach (HUD hud in HUD.instancesList)
            {
                for (int i = 0; i < hud.scoreboardPanel.transform.Find("Container/StripContainer").childCount; i++)
                {
                    Transform panel = hud.scoreboardPanel.transform.Find("Container/StripContainer").GetChild(i);
                    ScoreboardStrip sbs = panel.gameObject.GetComponent<ScoreboardStrip>();
                    Transform icon = panel.Find("LongBackground/ClassBackground/ClassIcon");
                    
                    if (icon.transform.Find("crown") && sbs.master.inventory.GetItemCountEffective(instance.ItemDef) == 0)
                    {
                        Log.Debug(sbs.master.bodyPrefab.name + " had crown but no item !");
                        Destroy(icon.transform.Find("crown").gameObject);
                    }
                }
                
                if (hud?.allyCardManager?.cardAllocator?.elements == null) continue;
                
                foreach (AllyCardController cardController in hud.allyCardManager.cardAllocator.elements)
                {
                    GameObject crownDisplay = cardController.portraitIconImage.transform.Find("crown")?.gameObject;
                    bool hasItem = cardController.sourceMaster?.inventory?.GetItemCountEffective(instance.ItemDef) > 0;

                    if (!hasItem && crownDisplay)
                    {
                        //destroy
                        Destroy(crownDisplay);
                    }
                }
            }
        }
    }
}