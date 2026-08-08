using System;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace questshrine.bases;

public abstract class ItemTierBase<T> : ItemTierBase where T : ItemTierBase<T>
{
    //This, which you will see on all the -base classes, will allow both you and other modders to enter through any class with this to access internal fields/properties/etc as if they were a member inheriting this -Base too from this class.
    public static T instance { get; private set; }

    public ItemTierBase()
    {
        if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ItemTierBase was instantiated twice");
        instance = this as T;
    }
}

public abstract class ItemTierBase
{
    protected abstract string Name { get; }

    public ItemTierDef tierDef;
    protected virtual ColorCatalog.ColorIndex Color => ColorCatalog.ColorIndex.None;
    protected virtual ColorCatalog.ColorIndex DarkColor => ColorCatalog.ColorIndex.None;

    protected virtual GameObject HighlightPrefab => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/HighlightTier1Item.prefab").WaitForCompletion();//Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/GenericPickup.prefab").WaitForCompletion();
    protected virtual GameObject DropletDisplayPrefab => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/LunarOrb.prefab").WaitForCompletion();
    protected virtual Texture IconBackgroundTexture => null;

    protected virtual bool CanBeDropped => true;
    protected virtual bool CanBeScrapped => true;
    protected virtual bool CanBeRestacked => true;

    protected virtual ItemTierDef.PickupRules PickupRules => ItemTierDef.PickupRules.Default;

    public void Create()
    {
        tierDef = ScriptableObject.CreateInstance<ItemTierDef>();

        if (tierDef)
        {
            tierDef.name = Name;

            tierDef.colorIndex = Color;
            tierDef.darkColorIndex = DarkColor;

            tierDef.highlightPrefab = HighlightPrefab;
            tierDef.dropletDisplayPrefab = DropletDisplayPrefab;
            tierDef.bgIconTexture = IconBackgroundTexture;

            tierDef.isDroppable = CanBeDropped;
            tierDef.canScrap = CanBeScrapped;
            tierDef.canRestack = CanBeRestacked;

            tierDef.pickupRules = PickupRules;

            tierDef.tier = ItemTier.AssignedAtRuntime;
        }

        ContentAddition.AddItemTierDef(tierDef);
    }
}