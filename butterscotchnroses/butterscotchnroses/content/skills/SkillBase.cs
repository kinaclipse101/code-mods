// stolens from pseudo .,., https://github.com/pseudopulse/ChaoticSkills/blob/main/ChaoticSkills/Content/SkillBase.cs ,..,,. 
using System;
using System.Collections.Generic;
using System.Linq;
using BNR;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace butterscotchnroses.skills;

public abstract class SkillBase<T> : SkillBase where T : SkillBase<T>
{
    public static T Instance { get; private set; }

    public SkillBase()
    {
        if (Instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting SkillBase was instantiated twice");
        Instance = this as T;
    }
}
public abstract class SkillBase {
    public abstract SerializableEntityStateType ActivationState { get; }
    public abstract float Cooldown { get; }
    public virtual int StockToConsume { get; } = 1;
    public abstract string Machine { get; }
    public abstract int MaxStock { get; }
    public abstract string LangToken { get; }
    public abstract string Name { get; }
    public virtual string Survivor { get; } = null;
    public virtual SkillSlot Slot { get; } = SkillSlot.None;
    public abstract string Description { get; }
    public virtual bool IsCombat { get; } = true;
    public virtual bool Agile { get; } = false;
    public virtual bool DelayCooldown { get; } = false;
    public virtual List<string> Keywords { get; } = new();
    public abstract Sprite SkillIcon { get; }
    public virtual UnlockableDef Unlock { get; } = null;
    public virtual bool AutoApply { get; } = true;
    public virtual bool MustKeyPress { get; } = false;
    public virtual bool AgileAddKeyword { get; } = true;
    public virtual bool SprintCancelable { get; } = true;
    public virtual bool ResetStockOnOverride { get; } = false;
    public virtual bool Passive { get; } = false;
    public virtual bool Configurable { get; } = true;
    public virtual bool ForceOff { get; } = false;
    public virtual InterruptPriority Priority { get; } = InterruptPriority.Skill;
    public virtual int StockToRecharge { get; } = 1;
    /*public virtual bool MiscSelectable { get; } = false;
    public virtual string MiscSelectableName { get; } = null;*/
    public SkillDef SkillDef;
    public static EventHandler PostCreationEvent;
    public void Init() {
        if (ForceOff) {
            return;
        }

        if (Configurable && !BNR.butterscotchnroses.instance.Config.Bind("Skills", Name, true, "Enable this skill?").Value) {
            return;
        }

        SkillDef = GetSkillDef();
        SkillDef.skillNameToken = "SKILL_" + LangToken.ToUpper() + "_NAME";
        SkillDef.skillDescriptionToken = "SKILL_" + LangToken.ToUpper() + "_DESC";
        SkillDef.skillName = LangToken.ToUpper();
        SkillDef.baseRechargeInterval = Cooldown;
        SkillDef.baseMaxStock = MaxStock;
        SkillDef.activationState = ActivationState;
        SkillDef.cancelSprintingOnActivation = Passive ? false : !Agile;
        SkillDef.icon = SkillIcon;
        SkillDef.interruptPriority = Priority;
        if (!SprintCancelable) {
            SkillDef.canceledFromSprinting = false;
        }
        else {
            SkillDef.canceledFromSprinting = !Agile;
        }
        SkillDef.isCombatSkill = IsCombat;
        SkillDef.activationStateMachineName = Machine;
        SkillDef.beginSkillCooldownOnSkillEnd = DelayCooldown;
        SkillDef.stockToConsume = StockToConsume;
        SkillDef.requiredStock = Passive ? 321 : 1;
        SkillDef.mustKeyPress = MustKeyPress;
        SkillDef.fullRestockOnAssign = ResetStockOnOverride;
        SkillDef.rechargeStock = StockToRecharge;
        (SkillDef as ScriptableObject).name = LangToken.ToUpper();
        List<string> newKeywords = Keywords;

        if (Agile && AgileAddKeyword) {
            newKeywords.Add(skills.Keywords.Agile);
        }
        
        if (newKeywords.Count >= 1) {
            SkillDef.keywordTokens = newKeywords.ToArray();
        }

        if (AutoApply && Passive) {
            GameObject survivor = Addressables.LoadAssetAsync<GameObject>(Survivor).WaitForCompletion();
            bool wasPassiveReal = false;

            foreach (GenericSkill skill in survivor.GetComponents<GenericSkill>()) {
                if (skill.skillName != null && skill.skillName.ToLower().Contains("passive") || (skill.skillFamily as ScriptableObject).name != null && (skill.skillFamily as ScriptableObject).name.ToLower().Contains("passive")) {
                    SkillFamily family = skill.skillFamily;

                    Array.Resize(ref family.variants, family.variants.Length + 1);
            
                    family.variants[family.variants.Length - 1] = new SkillFamily.Variant {
                        skillDef = SkillDef,
                        unlockableDef = Unlock,
                        viewableNode = new ViewablesCatalog.Node(SkillDef.skillNameToken, false, null)
                    };
                    wasPassiveReal = true;
                    break;
                }
            }

            if (!wasPassiveReal) {
                GenericSkill skill = survivor.AddComponent<GenericSkill>();
                SkillLocator locator = survivor.GetComponent<SkillLocator>();
                SkillFamily family = ScriptableObject.CreateInstance<SkillFamily>();
                skill.skillName = survivor.name + "Passive";
                (family as ScriptableObject).name = survivor.name + "Passive";
                family.variants = new SkillFamily.Variant[2];
                
                family.variants[1] = new SkillFamily.Variant {
                    skillDef = SkillDef,
                    unlockableDef = Unlock,
                    viewableNode = new ViewablesCatalog.Node(SkillDef.skillNameToken, false, null)
                };

                SkillDef oldPassive = ScriptableObject.CreateInstance<SkillDef>();
                oldPassive.skillNameToken = locator.passiveSkill.skillNameToken;
                oldPassive.skillDescriptionToken = locator.passiveSkill.skillDescriptionToken;
                oldPassive.activationStateMachineName = "TheAmongUs";
                oldPassive.activationState = new SerializableEntityStateType(typeof(Idle));
                oldPassive.icon = locator.passiveSkill.icon;
                
                ContentAddition.AddSkillDef(oldPassive);

                locator.passiveSkill.enabled = false;
                skill.hideInCharacterSelect = true;

                family.variants[0] = new SkillFamily.Variant {
                    skillDef = oldPassive,
                    viewableNode = new ViewablesCatalog.Node(oldPassive.skillNameToken, false, null)
                };

                skill._skillFamily = family;
            }
        }
        else if (AutoApply) {
            GameObject survivor = Addressables.LoadAssetAsync<GameObject>(Survivor).WaitForCompletion();
            SkillLocator skillLocator = survivor.GetComponent<SkillLocator>();
            SkillFamily family = null;

            switch (Slot) {
                case SkillSlot.Primary:
                    family = skillLocator.primary.skillFamily;
                    break;
                case SkillSlot.Secondary:
                    family = skillLocator.secondary.skillFamily;
                    break;
                case SkillSlot.Utility:
                    family = skillLocator.utility.skillFamily;
                    break;
                case SkillSlot.Special:
                    family = skillLocator.special.skillFamily;
                    break;
                default:
                    break;
            }

            if (family != null) {
                Array.Resize(ref family.variants, family.variants.Length + 1);
                
                family.variants[^1] = new SkillFamily.Variant {
                    skillDef = SkillDef,
                    unlockableDef = Unlock,
                    viewableNode = new ViewablesCatalog.Node(SkillDef.skillNameToken, false, null)
                };
            }
        }

        /*if (MiscSelectable && MiscSelectableName != null) {
            GameObject surv = Addressables.LoadAssetAsync<GameObject>(Survivor).WaitForCompletion();
            GenericSkill skill = surv.AddComponent<GenericSkill>();
            skill.hideFlags = HideFlags.DontSave;
            skill.hideInCharacterSelect = true;
            skill.skillName = Selectables.Prefix + MiscSelectableName;
            SkillFamily family = ScriptableObject.CreateInstance<SkillFamily>();
            (family as ScriptableObject).name = surv.name + "Misc";
            family.variants = null;

            skill._skillFamily = family;

            SkillDef.skillName = Selectables.Prefix + MiscSelectableName;
        }*/

        LanguageAPI.Add(SkillDef.skillNameToken, Name);
        string tempDesc = Description;
        if (Agile && AgileAddKeyword) {
            tempDesc = "<style=cIsUtility>Agile.</style> " + tempDesc;
        }
        LanguageAPI.Add(SkillDef.skillDescriptionToken, tempDesc);

        ContentAddition.AddSkillDef(SkillDef);
        PostCreation();
    }

    public virtual SkillDef GetSkillDef() {
        return ScriptableObject.CreateInstance<SkillDef>();
    }

    public virtual void PostCreation() {
        PostCreationEvent?.Invoke(this, new());
    }

    public EntityStateMachine AddEntityStateMachine<T>(GameObject obj, string name) where T : EntityState {
        EntityStateMachine stateMachine = obj.AddComponent<EntityStateMachine>();
        stateMachine.customName = name;
        stateMachine.initialStateType = new SerializableEntityStateType(typeof(T));
        stateMachine.mainStateType = new SerializableEntityStateType(typeof(T));
        
        List<EntityStateMachine> stateMachines = obj.GetComponents<EntityStateMachine>().ToList();
        obj.GetComponent<NetworkStateMachine>().stateMachines = stateMachines.ToArray();

        return stateMachine;
    }

    public GameObject GetSurvivor() {
        return Addressables.LoadAssetAsync<GameObject>(Survivor).WaitForCompletion();
    }
}

public static class Keywords {
    public static string Poison =  "KEYWORD_POISON";
    public static string Regenerative = "KEYWORD_RAPID_REGEN";
    public static string Agile = "KEYWORD_AGILE";
    public static string HealthCost = "KEYWORD_PERCENT_HP";
    public static string Disperse = "KEYWORD_SONIC_BOOM";
    public static string Weak = "KEYWORD_WEAK";
    public static string Heavy = "KEYWORD_HEAVY";
    public static string Freeze = "KEYWORD_FREEZING";
    public static string Stun = "KEYWORD_STUNNING";
    public static string Expose = "KEYWORD_EXPOSE";
    public static string Shock = "KEYWORD_SHOCKING";
    public static string Slayer = "KEYWORD_SLAYER";
    public static string Hemorrhage = "KEYWORD_SUPERBLEED";
    public static string Ignite = "KEYWORD_IGNITE";
    public static string Weakpoint = "KEYWORD_WEAKPOINT";
    public static string ActiveReload = "KEYWORD_ACTIVERELOAD";
    public static string VoidCorruption = "KEYWORD_VOIDCORRUPTION";
}