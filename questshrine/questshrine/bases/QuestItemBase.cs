using System;
using BepInEx.Configuration;
using questshrine.content.itemtiers;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace questshrine.bases;

public abstract class QuestItemBase<T> : QuestItemBase where T : QuestItemBase<T>
{
    //This, which you will see on all the -base classes, will allow both you and other modders to enter through any class with this to access internal fields/properties/etc as if they were a member inheriting this -Base too from this class.
    public static T instance { get; private set; }

    public QuestItemBase()
    {
        if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting QuestItemBase was instantiated twice");
        instance = this as T;
    }
}

public abstract class QuestItemBase : ItemBase
{
    public override string ItemPickupDesc => "";
    public override string ItemFullDescription => "";
    public override string ItemLore => "";
    public virtual ItemTag[] ItemTags => [ItemTag.WorldUnique, ItemTag.AIBlacklist];
    public override ItemTier Tier => QuestTier.instance.tierDef.tier;
    public override GameObject ItemModel => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
    public virtual bool CanRemove => false;
    public abstract Type ComponentType { get; }

    public override void Init(ConfigFile config)
    {
        if (!enabled) return;
        
        CreateConfig(config);
        CreateLang();
        CreateItem();
        Hooks();
    }
    
    protected override void CreateItem()
    {
        ItemDef = ScriptableObject.CreateInstance<ItemDef>();
        ItemDef.name = "ITEM_" + ItemLangTokenName;
        ItemDef.nameToken = "ITEM_" + ItemLangTokenName + "_NAME";
        ItemDef.pickupToken = "ITEM_" + ItemLangTokenName + "_PICKUP";
        ItemDef.descriptionToken = "ITEM_" + ItemLangTokenName + "_DESCRIPTION";
        ItemDef.loreToken = "ITEM_" + ItemLangTokenName + "_LORE";
        ItemDef.pickupModelPrefab = ItemModel;
        ItemDef.pickupIconSprite = ItemIcon;
        ItemDef.hidden = true;
        ItemDef.canRemove = CanRemove;
        ItemDef.deprecatedTier = Tier;

        if (ItemTags.Length > 0) { ItemDef.tags = ItemTags; }

        ItemAPI.Add(new CustomItem(ItemDef, CreateItemDisplayRules()));
    }
    
    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        return new ItemDisplayRuleDict();
    }

    public static void GiveReward(CharacterBody body)
    {
        PickupIndex final = PickupCatalog.FindPickupIndex(RoR2Content.Items.Bear.itemIndex);
        PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo
        {
            pickup = new UniquePickup
            {
                pickupIndex = final,
                decayValue = 0f,
            },
        }, body.transform.position, Vector3.up * 20f);
    }
}