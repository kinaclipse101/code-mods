using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using questshrine.bases;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace questshrine.content.quests;

public class NoDamage : QuestItemBase<NoDamage>
{
    public override string ItemName => "No Damage";
    public override string ItemLangTokenName => "NODAMAGE";
    public override Type BehaviorType => typeof(NoDamageBehaviorBase);
    public override Sprite ItemIcon => questshrine.bundle.LoadAsset<Sprite>("default");

    public override void CreateConfig(ConfigFile config)
    {
    }

    public class NoDamageBehaviorBase : QuestBehaviorBase, IOnTakeDamageServerReceiver
    {
        public override ItemDef ItemDef => instance.ItemDef;
        public override Type objectiveType => typeof(NoDamageObjective);
        public override string titleText => "<style=cWorldEvent>the planet seeks entertainment .,,..</style>";
        public string descText = "take no damage for {0} seconds,.,.";
        public static string tags => "noStack";

        public float timer;
        public float startingTime;
        
        public override void OnEnable()
        {
            startingTime = Run.instance.runRNG.RangeFloat(15, 45) - 0.5f;
            timer = startingTime + 2;
            HG.ArrayUtils.ArrayAppend(ref body.healthComponent.onTakeDamageReceivers, this);
            internalDesc = string.Format(descText, startingTime.ToString("0"));
            base.OnEnable();
        }

        public void FixedUpdate()
        {
            timer -= Time.fixedDeltaTime;
            if (timer < 0.5)
            {
                GiveReward(body);
                gaveReward = true;
                Destroy(this);
            }
        }

        public void OnTakeDamageServer(DamageReport damageReport)
        {
            timer = startingTime;
        }
    }
    
    public class NoDamageObjective : ObjectivePanelController.ObjectiveTracker
    {
        NoDamageBehaviorBase noDamageQuest;
        private float localTimer;
        private string name;
        
        public override string GenerateString()
        {
            if (noDamageQuest == null)
            {
                noDamageQuest = (NoDamageBehaviorBase)sourceDescriptor.source;
            }

            localTimer = noDamageQuest.timer;
            string text = string.Format(noDamageQuest.descText, localTimer.ToString("0"));
            return text;
        }

        public override bool IsDirty()
        {
            return (noDamageQuest == null || !Mathf.Approximately(localTimer, noDamageQuest.timer));
        }
    }
}