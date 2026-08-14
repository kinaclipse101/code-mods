using System.Collections.Generic;
using System.Linq;
using BNR.patches;
using BepInEx.Configuration;
using EntityStates;
using RoR2;
using ConCommandArgs = RoR2.ConCommandArgs;
using TeleporterInteraction = On.RoR2.TeleporterInteraction;

namespace BNR;

public class whodiddamage : PatchBase<whodiddamage>
{
    public override void Init()
    {
        applyHooks();
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            RoR2.TeleporterInteraction.onTeleporterBeginChargingGlobal += StartTracking;
            BossGroup.onBossGroupDefeatedServer += PrintDamage;
            RoR2.Stage.onStageStartGlobal += StageOnonStageStartGlobal;
        }
        else
        {

        }
    }

    //catch strays .,
    private void StageOnonStageStartGlobal(Stage stage)
    {
        totalDamages = [];
        GlobalEventManager.onServerDamageDealt -= GlobalEventManagerOnonServerDamageDealt;
    }

    private void PrintDamage(BossGroup bossGroup)
    {
        List<KeyValuePair<CharacterMaster, DamageCredit>> damageOrdered = totalDamages.ToList();
        damageOrdered.Sort((kvp, kvp2) => (kvp2.Value.damage + kvp2.Value.minionDamage).CompareTo(kvp.Value.damage + kvp.Value.minionDamage));
        foreach (KeyValuePair<CharacterMaster, DamageCredit> kvp in damageOrdered)
        {
            string name = kvp.Key.GetBody()?.baseNameToken;

            if (name != null)
            {
                name = Language.GetString(name);
            }
            
            if (kvp.Key.playerCharacterMasterController)
            {
                name = kvp.Key.playerCharacterMasterController.GetDisplayName();
            }

            if (name == null)
            {
                Log.Warning($"gave up trying to get name for master {kvp.Key.name}");
                continue;
            }
            
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage() { baseToken = $"<color=#e5eefc><style=cIsUtility>{name}</style> dealt <style=cIsDamage>{(kvp.Value.damage + kvp.Value.minionDamage):0} damage</style>!" + ((kvp.Value.minionDamage != 0) ? $" <style=cStack>({kvp.Value.damage:0} self, {kvp.Value.minionDamage:0} minion)</style>" : "" ) + "</color>"});
            Log.Debug($"{name} - {kvp.Value.damage:0}- {kvp.Value.minionDamage:0}");
        }
        
        GlobalEventManager.onServerDamageDealt -= GlobalEventManagerOnonServerDamageDealt;
    }

    private void StartTracking(RoR2.TeleporterInteraction teleporterInteraction)
    {
        totalDamages = [];
        GlobalEventManager.onServerDamageDealt += GlobalEventManagerOnonServerDamageDealt;
    }

    private class DamageCredit(float damage, float minionDamage, CharacterMaster master)
    {
        public float damage = damage;
        public float minionDamage = minionDamage;
        public CharacterMaster master = master;
    }

    private Dictionary<CharacterMaster, DamageCredit> totalDamages = [];
    private void GlobalEventManagerOnonServerDamageDealt(DamageReport damageReport)
    {
        if (!damageReport.attackerMaster)
            return;
        if (!damageReport.victimIsBoss)
            return;
        
        //attackerOwner logic
        if (damageReport.attackerOwnerMaster)
        {
            if (totalDamages.TryGetValue(damageReport.attackerOwnerMaster, out DamageCredit damageCreditMinion))
            {
                damageCreditMinion.minionDamage += damageReport.damageDealt;
            }
            else
            {
                totalDamages.Add(damageReport.attackerOwnerMaster, new DamageCredit(0, damageReport.damageDealt, damageReport.attackerOwnerMaster));
            }

            return;
        }

        if (totalDamages.TryGetValue(damageReport.attackerMaster, out DamageCredit damageCredit))
        {
            damageCredit.damage += damageReport.damageDealt;
        }
        else
        {
            totalDamages.Add(damageReport.attackerMaster, new DamageCredit(damageReport.damageDealt, 0, damageReport.attackerOwnerMaster));
        }
        
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - whodiddamage",
            "enable patches for whodiddamage",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) => { applyHooks(); };
    }

    private ConfigEntry<bool> enabled;
}