using System;
using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace questshrine.bases;

public abstract class QuestBase<T> : QuestBase where T : QuestBase<T>
{
    //This, which you will see on all the -base classes, will allow both you and other modders to enter through any class with this to access internal fields/properties/etc as if they were a member inheriting this -Base too from this class.
    public static T instance { get; private set; }

    public QuestBase()
    {
        if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting QuestItemBase was instantiated twice");
        instance = this as T;
    }
}

public abstract class QuestBase
{
    public abstract string QuestName { get; }
    public abstract string QuestTitle { get; }
    public abstract string QuestDesc { get; }
    public abstract Sprite QuestIcon { get; }
    public abstract Type Behavior { get; }
    public virtual bool useListeners { get; }
    public virtual string[] Tags { get; }

    public bool enabled => Utils.CheckboxConfig(questshrine.instance.Config.Bind($"Quest Shrine - {QuestName}", $"Enable {QuestName}", true, "")).Value;

    public virtual void Init(ConfigFile config)
    {
        if (!enabled) return;

        CreateConfig(config);
        Hooks();
    }

    public virtual void Hooks() { }
    
    public virtual void CreateConfig(ConfigFile config) { }

    public ConfigEntry<T> ConfigHelper<T>(string name, T value, string desc)
    {
        return questshrine.instance.Config.Bind($"Quest Shrine - {QuestName}", name, value, desc);
    }

    private static BasicPickupDropTable chest1DT = Addressables.LoadAssetAsync<BasicPickupDropTable>(RoR2BepInExPack.GameAssetPaths.Version_1_35_0.RoR2_Base_Chest1.dtChest1_asset).WaitForCompletion(); 
    public virtual void GiveReward(CharacterBody body)
    {
        PickupIndex final = chest1DT.GeneratePickup(Run.instance.runRNG).pickupIndex;
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