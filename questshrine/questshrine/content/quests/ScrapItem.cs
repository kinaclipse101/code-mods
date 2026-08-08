using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using questshrine.bases;
using RoR2;
using RoR2.UI;
using UnityEngine;
using static RoR2.ItemTier;
using Object = UnityEngine.Object;

namespace questshrine.content.quests;

public class ScrapItem : QuestItemBase<ScrapItem>
{
    public override string ItemName => "Scrap Item";
    public override string ItemLangTokenName => "SCRAPITEM";
    public override Type BehaviorType => typeof(ScrapItemBehaviorBase);
    public override Sprite ItemIcon => questshrine.bundle.LoadAsset<Sprite>("default");
    
    public static ConfigEntry<bool> specificItem;

    public override void CreateConfig(ConfigFile config)
    {
        specificItem = config.Bind($"Quest Shrine - {ItemName}", 
            "Should the quest be a specific item to scrap .,.,", 
            false, 
            "");
        Utils.CheckboxConfig(specificItem);
    }

    public override void Hooks()
    {
        On.RoR2.ScrapperController.BeginScrapping_UniquePickup += QuestScrapper;
    }

    private void QuestScrapper(On.RoR2.ScrapperController.orig_BeginScrapping_UniquePickup orig, RoR2.ScrapperController self, UniquePickup pickuptotake)
    {
        if (!self.interactor.gameObject.TryGetComponent(out ScrapItemBehaviorBase scrapQuest))
        {
            orig(self, pickuptotake);
            return;
        };

        if (specificItem.Value && scrapQuest.targetIndex != ItemIndex.None)
        {
            if (scrapQuest.targetIndex != PickupCatalog.GetPickupDef(pickuptotake.pickupIndex)!.itemIndex)
            {
                orig(self, pickuptotake);
                return;
            };
        }
        else
        {
            if (scrapQuest.targetTier != PickupCatalog.GetPickupDef(pickuptotake.pickupIndex)!.itemTier)
            {
                orig(self, pickuptotake);
                return;
            };
        }

        GiveReward(scrapQuest.body);
        scrapQuest.gaveReward = true;
        Object.Destroy(scrapQuest);

        int prevMaxScraps = self.maxItemsToScrapAtATime;
        self.maxItemsToScrapAtATime = 1;
        orig(self, pickuptotake);
        self.maxItemsToScrapAtATime = prevMaxScraps;
    }

    public class ScrapItemBehaviorBase : QuestBehaviorBase
    {
        public override ItemDef ItemDef => instance.ItemDef;
        public override Type objectiveType => typeof(ScrapItemObjective);
        public override string titleText => "<style=cWorldEvent>the planet hungers .,,..</style>";
        public string descText = "scrap a {0} item,.,.";
        public string descTextSpecific = "scrap a {0},.,.";
        public static string tags => "requireScrapper";

        public ItemTier targetTier;
        public ItemIndex targetIndex = ItemIndex.None;
        
        public override void OnEnable()
        {
            BasicPickupDropTable dropTable = ScriptableObject.CreateInstance<BasicPickupDropTable>();
            
            ReadOnlySpan<ItemIndex> spanStacks = body.inventory.effectiveItemStacks.GetNonZeroIndicesSpan();
            foreach (ItemIndex index in spanStacks)
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(index);
                ItemTier tier = ItemCatalog.GetItemDef(index).tier;
                if (!itemDef.canRemove || itemDef.hidden || !itemDef.DoesNotContainTag(ItemTag.Scrap) || !itemDef.DoesNotContainTag(ItemTag.CannotCopy)) continue;
                switch (tier)
                {
                    case Tier1:
                        Log.Debug($"had item {itemDef.name} with tier {tier}");
                        dropTable.tier1Weight = 0.8f;
                        break;
                    case Tier2:
                        Log.Debug($"had item {itemDef.name} with tier {tier}");
                        dropTable.tier2Weight = 0.2f;
                        break;
                    case Tier3:
                        Log.Debug($"had item {itemDef.name} with tier {tier}");
                        dropTable.tier3Weight = 0.01f;
                        break;
                }
            }

            if (dropTable.tier1Weight == 0 && dropTable.tier2Weight == 0 && dropTable.tier3Weight == 0)
            {
                targetTier = Tier1;
            }
            else
            {
                targetTier = PickupCatalog.GetPickupDef(dropTable.GeneratePickup(Run.instance.runRNG).pickupIndex)!.itemTier;
            }

            if (specificItem.Value)
            {
                WeightedSelection<ItemIndex> indexSelection = new WeightedSelection<ItemIndex>();
                
                bool availchoice = false;
                ReadOnlySpan<ItemIndex> span = body.inventory.effectiveItemStacks.GetNonZeroIndicesSpan();
                foreach (ItemIndex index in span)
                {
                    ItemDef itemDef = ItemCatalog.GetItemDef(index);
                    ItemTier tier = ItemCatalog.GetItemDef(index).tier;
                    if (tier is not Tier1 and not Tier2 and not Tier3) continue;
                    if (!itemDef.canRemove || itemDef.hidden || !itemDef.DoesNotContainTag(ItemTag.Scrap) || !itemDef.DoesNotContainTag(ItemTag.CannotCopy)) continue;
                    
                    int weight = tier switch
                    {
                        Tier1 => 80,
                        Tier2 => 20,
                        Tier3 => 1,
                    };
                    indexSelection.AddChoice(index, weight);
                    availchoice = true;
                }

                Log.Debug($"choice length {indexSelection.choices.Length}");
                if (availchoice)
                {
                    targetIndex = indexSelection.Evaluate(Run.instance.treasureRng.nextNormalizedFloat);
                }
            }

            string tierName = targetTier switch
            {
                Tier1 => "Common",
                Tier2 => "Uncommon",
                Tier3 => "Legendary",
            };
            internalDesc = string.Format(descText, tierName);
            if (specificItem.Value && targetIndex != ItemIndex.None)
            {
                internalDesc = string.Format(descTextSpecific, Language.GetString(ItemCatalog.GetItemDef(targetIndex).nameToken));
            }
            
            base.OnEnable();
        }
    }
    
    public class ScrapItemObjective : ObjectivePanelController.ObjectiveTracker
    {
        ScrapItemBehaviorBase scrapQuest;
        
        public override string GenerateString()
        {
            if (scrapQuest == null)
            {
                scrapQuest = (ScrapItemBehaviorBase)sourceDescriptor.source;
            }

            string tierName = scrapQuest.targetTier switch
            {
                Tier1 => "Common",
                Tier2 => "Uncommon",
                Tier3 => "Legendary",
            };
            string text = string.Format(scrapQuest.descText, tierName);
            
            if (specificItem.Value && scrapQuest.targetIndex != ItemIndex.None)
            {
                text = string.Format(scrapQuest.descTextSpecific, Language.GetString(ItemCatalog.GetItemDef(scrapQuest.targetIndex).nameToken));
            }

            if (scrapQuest.gaveReward)
            {
                text = text.Replace("scrap", "scrapped");
            }
            return text;
        }

        public override bool IsDirty()
        {
            return !scrapQuest || (scrapQuest?.gaveReward == true);
        }
    }
}