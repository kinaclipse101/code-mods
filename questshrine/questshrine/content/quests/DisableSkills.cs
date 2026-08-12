//NETWORKING TODO
//uses Run.instance.runRNG, send r2api networking to clients  
using System;
using BepInEx.Configuration;
using questshrine.bases;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace questshrine.content.quests;

public class DisableSkills : QuestBase<DisableSkills>
{ 
    public override string QuestName => "Disable Skills";
    public override string QuestTitle => "<style=cWorldEvent>the planet sent out a freakings emp .,,..</style>";
    public override string QuestDesc => "skills disabled for {0} seconds,.,.";
    public override Sprite QuestIcon => questshrine.bundle.LoadAsset<Sprite>("noskills");
    public override string[] Tags => ["noStack"];
    public override Type Behavior => typeof(DisableSkillsBehaviorBase);

    private static ConfigEntry<int> minTimer;
    private static ConfigEntry<int> maxTimer;
    
    public override void CreateConfig(ConfigFile config)
    {
        minTimer = Utils.SliderConfig(ConfigHelper("min timer value", 10, "minimum timer value for skills being disabled .,,."));
        maxTimer = Utils.SliderConfig(ConfigHelper("max timer value", 25, "max timer value for skills being disabled .,,."));
    }

    public class DisableSkillsBehaviorBase : QuestBehaviorBase
    {
        public override QuestBase QuestBase => instance;
        public override Type ObjectiveType => typeof(NoDamageObjective);

        public float timer;
        public float startingTime;
        
        public override void StartQuest()
        {
            if (!NetworkServer.active)
            {
                base.StartQuest();
                return;
            }
            
            startingTime = Run.instance.runRNG.RangeFloat(minTimer.Value, maxTimer.Value) - 0.5f;
            timer = startingTime + 2;

            DisableSkills(body);
            charMaster.onBodyStart += DisableSkills;// fuck your dios <3 ,.,.
            
            QuestDescInternal = string.Format(instance.QuestDesc, startingTime.ToString("0"));
            base.StartQuest();
        }

        public void DisableSkills(CharacterBody characterBody)
        {
            if (!characterBody.skillLocator) return;
            
            if (characterBody.skillLocator.primary)
                characterBody.skillLocator.primary.SetSkillOverride(this, CharacterBody.CommonAssets.disabledSkill, GenericSkill.SkillOverridePriority.Contextual);

            if (characterBody.skillLocator.secondary)
                characterBody.skillLocator.secondary.SetSkillOverride(this, CharacterBody.CommonAssets.disabledSkill, GenericSkill.SkillOverridePriority.Contextual);

            if (characterBody.skillLocator.utility)
                characterBody.skillLocator.utility.SetSkillOverride(this, CharacterBody.CommonAssets.disabledSkill, GenericSkill.SkillOverridePriority.Contextual);

            if (characterBody.skillLocator.special)
                characterBody.skillLocator.special.SetSkillOverride(this, CharacterBody.CommonAssets.disabledSkill, GenericSkill.SkillOverridePriority.Contextual);
        }

        public override void RpcOnDisable()
        {
            charMaster.onBodyStart -= DisableSkills;

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
            
            base.RpcOnDisable();
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
    }

    private class NoDamageObjective : ObjectivePanelController.ObjectiveTracker
    {
        private DisableSkillsBehaviorBase _disableSkillsQuest;
        private float localTimer;
        
        public override string GenerateString()
        {
            if (_disableSkillsQuest == null)
            {
                _disableSkillsQuest = (DisableSkillsBehaviorBase)sourceDescriptor.source;
            }

            localTimer = _disableSkillsQuest.timer;
            string text = string.Format(instance.QuestDesc, localTimer.ToString("0"));
            return text;
        }

        public override bool IsDirty()
        {
            return (_disableSkillsQuest == null || !Mathf.Approximately(localTimer, _disableSkillsQuest.timer));
        }
    }
}