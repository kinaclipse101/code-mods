//NETWORKING TODO
//Run.instance.runRNG isnt available on clients; send a r2api networking packet to them .,
using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using questshrine.bases;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace questshrine.content.quests;

public class KillEnemies : QuestBase<KillEnemies>
{ 
    public override string QuestName => "Kill Enemies";
    public override string QuestTitle => "<style=cWorldEvent>the planet's malice grows .,,..</style>";
    public override string QuestDesc => "defeat {0} {1},.,.";
    public override Sprite QuestIcon => questshrine.bundle.LoadAsset<Sprite>("killenemies");
    public override bool useListeners => true;
    public override Type Behavior => typeof(KillEnemiesBehaviorBase);

    public override void CreateConfig(ConfigFile config)
    {
    }

    public class KillEnemiesBehaviorBase : QuestBehaviorBase
    {
        public override QuestBase QuestBase => instance;
        public override Type ObjectiveType => typeof(KillEnemiesObjective);

        public BodyIndex targetIndex;
        public int killAmount;
        
        public override void OnEnable()
        {
            KilledOtherServer += OnKilledOtherServer;

            ClassicStageInfo classicStageInfo = GameObject.Find("SceneInfo").GetComponent<ClassicStageInfo>();
            List<WeightedSelection<DirectorCard>.ChoiceInfo> availableChoices = [];
            List<CharacterMaster> availableMasters = [];
            foreach (WeightedSelection<DirectorCard>.ChoiceInfo choice in classicStageInfo.monsterSelection.choices)
            {
                if (choice.value?.minimumStageCompletions > Run.instance.stageClearCount) continue;
                if (choice.value?.spawnCard?.prefab?.TryGetComponent(out CharacterMaster master) != true) continue;
                
                if (master.bodyPrefab.GetComponent<CharacterBody>().isChampion)
                {
                    Log.Debug($"{master.name} is champion");
                    continue;
                }

                if (choice.value.cost > 100)
                {
                    Log.Debug($"card cost {choice.value.cost} greater than 100 skipping {choice.value.spawnCard.name}");
                    continue;
                }
                    
                /*Log.Debug(master.name);
                Log.Debug(choice.value.spawnCard.prefab);
                Log.Debug(choice.value.cost);
                Log.Debug(choice.weight);*/
                
                availableChoices.Add(choice);
                availableMasters.Add(master);
            }

            WeightedSelection<CharacterMaster> weightedSelection = new WeightedSelection<CharacterMaster>();
            for (int i = 0; i < availableChoices.Count; i++)
            {
                weightedSelection.AddChoice(availableMasters[i], availableChoices[i].value.cost);
            }
            CharacterMaster chosenMaster = weightedSelection.Evaluate(Run.instance.runRNG.nextNormalizedFloat);

            targetIndex = BodyCatalog.FindBodyIndex(chosenMaster.bodyPrefab);
            killAmount = (int)((50f/availableChoices[availableMasters.IndexOf(chosenMaster)].value.cost) * Run.instance.runRNG.RangeFloat(1, 2));
            if (killAmount <= 3)
                killAmount += 2;
            
            QuestDescInternal = string.Format(instance.QuestDesc, killAmount, Language.GetString(BodyCatalog.GetBodyPrefab(targetIndex).GetComponent<CharacterBody>().baseNameToken) + (killAmount > 1 ? "s" : ""));
            base.OnEnable();
        }

        public void OnKilledOtherServer(DamageReport damageReport)
        {
            if (damageReport.victimBodyIndex != targetIndex) return;
            
            killAmount--;
            if (killAmount != 0) return;
            
            instance.GiveReward(body);
            Destroy(this);
        }

        public override void OnDisable()
        {
            KilledOtherServer += OnKilledOtherServer;
            base.OnDisable();
        }
    }

    private class KillEnemiesObjective : ObjectivePanelController.ObjectiveTracker
    {
        private KillEnemiesBehaviorBase _killEnemiesBehaviorBase;
        private int localKillAmount;
        private string name;
        
        public override string GenerateString()
        {
            if (_killEnemiesBehaviorBase == null)
            {
                _killEnemiesBehaviorBase = (KillEnemiesBehaviorBase)sourceDescriptor.source;
                name = Language.GetString(BodyCatalog.GetBodyPrefab(_killEnemiesBehaviorBase.targetIndex).GetComponent<CharacterBody>().baseNameToken);
            }

            localKillAmount = _killEnemiesBehaviorBase.killAmount;
            string text = $"kill {_killEnemiesBehaviorBase.killAmount} {name}{(localKillAmount > 1 ? "s" : "")}";

            return text;
        }

        public override bool IsDirty()
        {
            return (_killEnemiesBehaviorBase == null || localKillAmount != _killEnemiesBehaviorBase.killAmount);
        }
    }
}