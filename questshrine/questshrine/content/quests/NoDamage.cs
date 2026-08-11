using System;
using BepInEx.Configuration;
using questshrine.bases;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace questshrine.content.quests;

public class NoDamage : QuestBase<NoDamage>
{
    public override string QuestName => "No Damage";
    public override string QuestTitle => "<style=cWorldEvent>the planet seeks entertainment .,,..</style>";
    public override string QuestDesc => "take no damage for {0} seconds,.,.";
    public override Sprite QuestIcon => questshrine.bundle.LoadAsset<Sprite>("nodamage");
    public override bool useListeners => true;
    public override string[] Tags => ["noStack"];
    public override Type Behavior => typeof(NoDamageBehaviorBase);

    private static ConfigEntry<int> minTimer;
    private static ConfigEntry<int> maxTimer;
    private static ConfigEntry<bool> ss1Behavior;
    
    public override void CreateConfig(ConfigFile config)
    {
        minTimer = Utils.SliderConfig(ConfigHelper("min timer value", 15, "minimum timer value for damage not being taken .,,."));
        maxTimer = Utils.SliderConfig(ConfigHelper("max timer value", 45, "max timer value for damage not being taken .,,."));
        ss1Behavior = Utils.CheckboxConfig(ConfigHelper("starstorm 1 behavior", true, "should use starstorm 1 behavior (reset timer on hit),., disabling will cause the quest to fail upon taking damage with no reward,.,."));
    }

    public class NoDamageBehaviorBase : QuestBehaviorBase
    {
        public override QuestBase QuestBase => instance;
        public override Type ObjectiveType => typeof(NoDamageObjective);

        public float timer;
        public float startingTime;
        
        public override void OnEnable()
        {
            TakeDamageServer += OnTakeDamageServer;
            
            startingTime = Run.instance.runRNG.RangeFloat(minTimer.Value, maxTimer.Value) - 0.5f;
            timer = startingTime + 2;
            QuestDescInternal = string.Format(instance.QuestDesc, startingTime.ToString("0"));
            base.OnEnable();
        }

        public void FixedUpdate()
        {
            timer -= Time.fixedDeltaTime;
            if (timer < 0.5)
            {
                instance.GiveReward(body);
                Destroy(this);
            }
        }

        public void OnTakeDamageServer(DamageReport damageReport)
        {
            if (ss1Behavior.Value)
            {
                timer = startingTime;
            }
            else
            {
                Destroy(this);
            }
        }

        public override void OnDisable()
        {
            TakeDamageServer -= OnTakeDamageServer;
            base.OnDisable();
        }
    }

    private class NoDamageObjective : ObjectivePanelController.ObjectiveTracker
    {
        NoDamageBehaviorBase noDamageQuest;
        private float localTimer;
        
        public override string GenerateString()
        {
            if (noDamageQuest == null)
            {
                noDamageQuest = (NoDamageBehaviorBase)sourceDescriptor.source;
            }

            localTimer = noDamageQuest.timer;
            string text = string.Format(instance.QuestDesc, localTimer.ToString("0"));
            return text;
        }

        public override bool IsDirty()
        {
            return (noDamageQuest == null || !Mathf.Approximately(localTimer, noDamageQuest.timer));
        }
    }
}