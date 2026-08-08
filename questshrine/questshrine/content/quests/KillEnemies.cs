using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using questshrine.bases;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace questshrine.content.quests;

public class KillEnemies : QuestItemBase<KillEnemies>
{
    public override string ItemName => "Kill Enemies";
    public override string ItemLangTokenName => "KILLENEMIES";
    public override Type ComponentType => typeof(KillEnemiesBehaviorBase);
    public override Sprite ItemIcon => questshrine.bundle.LoadAsset<Sprite>("killenemies");

    public override void CreateConfig(ConfigFile config)
    {
    }

    public class KillEnemiesBehaviorBase : QuestBehaviorBase, IOnKilledOtherServerReceiver
    {
        public override ItemDef ItemDef => instance.ItemDef;
        public override Type objectiveType => typeof(KillEnemiesObjective);
        public override string titleText => "<style=cWorldEvent>the planet's malice grows .,,..</style>";
        public string descText => "defeat {0} {1},.,.";

        public BodyIndex targetIndex;
        public int killAmount;
        
        public override void OnEnable()
        {
            ClassicStageInfo classicStageInfo = GameObject.Find("SceneInfo").GetComponent<ClassicStageInfo>();
            List<WeightedSelection<DirectorCard>.ChoiceInfo> availableChoices = [];
            List<CharacterMaster> availableMasters = [];
            foreach (WeightedSelection<DirectorCard>.ChoiceInfo choice in classicStageInfo.monsterSelection.choices)
            {
                if (choice.value.minimumStageCompletions > Run.instance.stageClearCount) continue;
                if (!choice.value.spawnCard.prefab.TryGetComponent(out CharacterMaster master)) continue;
                
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
                    
                Log.Debug(master.name);
                Log.Debug(choice.value.spawnCard.prefab);
                Log.Debug(choice.value.cost);
                Log.Debug(choice.weight);
                
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
            
            internalDesc = string.Format(descText, killAmount, Language.GetString(BodyCatalog.GetBodyPrefab(targetIndex).GetComponent<CharacterBody>().baseNameToken) + (killAmount > 1 ? "s" : ""));
            base.OnEnable();
        }

        public void OnKilledOtherServer(DamageReport damageReport)
        {
            if (damageReport.victimBodyIndex != targetIndex) return;
            
            killAmount--;
            if (killAmount != 0) return;
            
            GiveReward(body);
            gaveReward = true;
            Destroy(this);
        }
    }
    
    public class KillEnemiesObjective : ObjectivePanelController.ObjectiveTracker
    {
        KillEnemiesBehaviorBase _killEnemiesBehaviorBase;
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