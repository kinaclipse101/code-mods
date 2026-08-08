using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using questshrine.bases;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace questshrine.content.quests;

public class DisableSkills : QuestItemBase<DisableSkills>
{
    public override string ItemName => "Disable Skills";
    public override string ItemLangTokenName => "DISABLESKILLS";
    public override Type BehaviorType => typeof(DisableSkillsBehaviorBase);
    public override Sprite ItemIcon => questshrine.bundle.LoadAsset<Sprite>("default");

    public override void CreateConfig(ConfigFile config)
    {
    }

    public class DisableSkillsBehaviorBase : QuestBehaviorBase
    {
        public override ItemDef ItemDef => instance.ItemDef;
        public override Type objectiveType => typeof(NoDamageObjective);
        public override string titleText => "<style=cWorldEvent>the planet sent out a freakings emp .,,..</style>";
        public string descText = "skills disabled for {0} seconds,.,.";
        public static string tags => "noStack";

        public float timer;
        public float startingTime;
        
        public override void OnEnable()
        {
            startingTime = Run.instance.runRNG.RangeFloat(15, 45) - 0.5f;
            timer = startingTime + 2;
       
            if (body.skillLocator)
            {
                if (body.skillLocator.primary)
                    body.skillLocator.primary.SetSkillOverride(this, CharacterBody.CommonAssets.disabledSkill, GenericSkill.SkillOverridePriority.Contextual);

                if (body.skillLocator.secondary)
                    body.skillLocator.secondary.SetSkillOverride(this, CharacterBody.CommonAssets.disabledSkill, GenericSkill.SkillOverridePriority.Contextual);

                if (body.skillLocator.utility)
                    body.skillLocator.utility.SetSkillOverride(this, CharacterBody.CommonAssets.disabledSkill, GenericSkill.SkillOverridePriority.Contextual);

                if (body.skillLocator.special)
                    body.skillLocator.special.SetSkillOverride(this, CharacterBody.CommonAssets.disabledSkill, GenericSkill.SkillOverridePriority.Contextual);
            }
            
            internalDesc = string.Format(descText, startingTime.ToString("0"));
            base.OnEnable();
        }

        public override void OnDisable()
        {
            if (body.skillLocator)
            {
                if (body.skillLocator.primary)
                    body.skillLocator.primary.UnsetSkillOverride(this, CharacterBody.CommonAssets.disabledSkill, GenericSkill.SkillOverridePriority.Contextual);

                if (body.skillLocator.secondary)
                    body.skillLocator.secondary.UnsetSkillOverride(this, CharacterBody.CommonAssets.disabledSkill, GenericSkill.SkillOverridePriority.Contextual);

                if (body.skillLocator.utility)
                    body.skillLocator.utility.UnsetSkillOverride(this, CharacterBody.CommonAssets.disabledSkill, GenericSkill.SkillOverridePriority.Contextual);

                if (body.skillLocator.special)
                    body.skillLocator.special.UnsetSkillOverride(this, CharacterBody.CommonAssets.disabledSkill, GenericSkill.SkillOverridePriority.Contextual);
            }
            
            base.OnDisable();
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
    }
    
    public class NoDamageObjective : ObjectivePanelController.ObjectiveTracker
    {
        DisableSkillsBehaviorBase _disableSkillsQuest;
        private float localTimer;
        private string name;
        
        public override string GenerateString()
        {
            if (_disableSkillsQuest == null)
            {
                _disableSkillsQuest = (DisableSkillsBehaviorBase)sourceDescriptor.source;
            }

            localTimer = _disableSkillsQuest.timer;
            string text = string.Format(_disableSkillsQuest.descText, localTimer.ToString("0"));
            return text;
        }

        public override bool IsDirty()
        {
            return (_disableSkillsQuest == null || !Mathf.Approximately(localTimer, _disableSkillsQuest.timer));
        }
    }
}