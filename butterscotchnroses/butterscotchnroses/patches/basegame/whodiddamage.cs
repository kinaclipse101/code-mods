using System.Collections.Generic;
using System.Linq;
using BNR.patches;
using BepInEx.Configuration;
using BNR.items;
using EntityStates;
using RoR2;
using RoR2.UI;
using SS2.Orbs;
using UnityEngine;
using UnityEngine.UI;
using ConCommandArgs = RoR2.ConCommandArgs;
using TeleporterInteraction = On.RoR2.TeleporterInteraction;

namespace BNR;

public class whodiddamage : PatchBase<whodiddamage>
{
    public static Texture CrownIcon;
    public override void Init()
    {
        applyHooks();
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            //RoR2.TeleporterInteraction.onTeleporterBeginChargingGlobal += StartTracking;
            BossGroup.onBossGroupStartServer += StartTracking;
            BossGroup.onBossGroupDefeatedServer += PrintDamage;
            RoR2.Stage.onStageStartGlobal += StageOnonStageStartGlobal;
            On.RoR2.UI.AllyCardController.InfoOverride += AllyCardControllerOnInfoOverride;
            CrownIcon = butterscotchnroses.bnrBundle.LoadAsset<Texture>("texCrownIcon");
        }
        else
        {

        }
    }
    
    private static List<GameObject> crownImages = [];

    public static Color GetHex(string hex)
    {
        if (!hex.StartsWith("#"))
        {
            hex = "#" + hex;
        }
        
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
    
    private void AllyCardControllerOnInfoOverride(On.RoR2.UI.AllyCardController.orig_InfoOverride orig, RoR2.UI.AllyCardController self)
    {
        orig(self);

        if (!useCrown.Value) return;
        if (self.cachedSourceMaster.inventory.GetItemCountEffective(Crown.instance.ItemDef) <= 0) return;
        if (self.portraitIconImage.transform.Find("crown")) return;
            
        GameObject crownObj = new GameObject("crown");
        crownObj.transform.parent = self.portraitIconImage.transform;
        crownObj.transform.localPosition = Vector3.zero;
                
        RawImage rawImage = crownObj.AddComponent<RawImage>();
        rawImage.texture = CrownIcon;//LocalUserManager.GetFirstLocalUser().userProfile.portraitTexture;
        if (crownColorBasedOffBody.Value)
        {
            rawImage.color = self.sourceMaster.bodyPrefab.GetComponent<CharacterBody>().bodyColor;
        }
        if (crownColorOverrides.Value != "")
        {
            string? steamID = self.sourceMaster?.playerCharacterMasterController?.networkUser?.id.steamId.ToSteamID();
            if (steamID != null)
            {
                string[] values = steamID.Split(',');
                for (int i = 0; i < values.Length; i += 2)
                {
                    values[i] = values[i].Trim();
                    if (values[i] == steamID)
                    {
                        rawImage.color = GetHex(values[i + 1]);
                    }
                }
            }
        }
        
        RectTransform rectTransform = crownObj.GetComponent<RectTransform>();
        rectTransform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        rectTransform.pivot = new Vector2(0.5f, -0.5f);
            
        crownImages.Add(crownObj);
    }

    //catch strays .,
    private void StageOnonStageStartGlobal(Stage stage)
    {
        bossGroupsToDamages = [];
        GlobalEventManager.onServerDamageDealt -= GlobalEventManagerOnonServerDamageDealt;
    }
    
    private Dictionary<BossGroup, Dictionary<CharacterMaster, DamageCredit>> bossGroupsToDamages = [];
    private void PrintDamage(BossGroup bossGroup)
    {
        if (bossGroupsToDamages.TryGetValue(bossGroup, out Dictionary<CharacterMaster, DamageCredit> bossGroupsToDamage))
        {
            List<KeyValuePair<CharacterMaster, DamageCredit>> damageOrdered = bossGroupsToDamage.ToList();
            damageOrdered.Sort((kvp, kvp2) => (kvp2.Value.damage + kvp2.Value.minionDamage).CompareTo(kvp.Value.damage + kvp.Value.minionDamage));

            foreach (CharacterMaster master in CharacterMaster.readOnlyInstancesList)
            {
                master.inventory.RemoveItemPermanent(Crown.instance.ItemDef, 999);
            }

            foreach (GameObject crownObj in crownImages)
            {
                Object.Destroy(crownObj);
            }
            
            damageOrdered[0].Key.inventory.GiveItemPermanent(Crown.instance.ItemDef);
            foreach (HUD hud in HUD.instancesList)
            {
                foreach (AllyCardController cardController in hud.allyCardManager.cardAllocator.elements)
                {
                    cardController.InfoOverride();
                }
            }
            
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
        }
        
        GlobalEventManager.onServerDamageDealt -= GlobalEventManagerOnonServerDamageDealt;
    }

    private void StartTracking(BossGroup bossGroup)
    {
        //totalDamages = [];
        bossGroupsToDamages.Add(bossGroup, new Dictionary<CharacterMaster, DamageCredit>());
        GlobalEventManager.onServerDamageDealt += GlobalEventManagerOnonServerDamageDealt;
    }

    private class DamageCredit(float damage, float minionDamage, CharacterMaster master)
    {
        public float damage = damage;
        public float minionDamage = minionDamage;
        public CharacterMaster master = master;
    }

    //private Dictionary<CharacterMaster, DamageCredit> totalDamages = [];
    private void GlobalEventManagerOnonServerDamageDealt(DamageReport damageReport)
    {
        if (!damageReport.attackerMaster)
            return;
        if (!damageReport.victimIsBoss)
            return;

        bool exit = true;
        KeyValuePair<BossGroup, Dictionary<CharacterMaster, DamageCredit>> saved = default;
        foreach (KeyValuePair<BossGroup, Dictionary<CharacterMaster, DamageCredit>> kvp in bossGroupsToDamages)
        {
            if (!kvp.Key.combatSquad.membersList.Contains(damageReport.victimMaster)) continue;
            
            exit = false;
            saved = kvp;
            break;
        }
        if(exit)
            return;

        //attackerOwner logic
        if (damageReport.attackerOwnerMaster)
        {
            if (saved.Value.TryGetValue(damageReport.attackerOwnerMaster, out DamageCredit damageCreditMinion))
            {
                damageCreditMinion.minionDamage += damageReport.damageDealt;
            }
            else
            {
                saved.Value.Add(damageReport.attackerOwnerMaster, new DamageCredit(0, damageReport.damageDealt, damageReport.attackerOwnerMaster));
            }

            return;
        }

        if (saved.Value.TryGetValue(damageReport.attackerMaster, out DamageCredit damageCredit))
        {
            damageCredit.damage += damageReport.damageDealt;
        }
        else
        {
            saved.Value.Add(damageReport.attackerMaster, new DamageCredit(damageReport.damageDealt, 0, damageReport.attackerOwnerMaster));
        }
        
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - whodiddamage",
            "enable patches for whodiddamage",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        
        useCrown = config.Bind("BNR - whodiddamage",
            "use crown icon",
            true,
            "adds a crown icon to the highest damage dealer on the scoreboard that persists going into the next stage");
        Utils.CheckboxConfig(useCrown);
        
        crownColorBasedOffBody = config.Bind("BNR - whodiddamage",
            "base crown icon color off body color",
            true,
            "makes the crown use body color ,.,.");
        Utils.CheckboxConfig(crownColorBasedOffBody);
        
        crownColorOverrides = config.Bind("BNR - whodiddamage",
            "crown color overrides",
            "",
            "crown color overrides for specific players steamids, formatted \"STEAM_0:1:174533492,#F3D2F7\"");
        Utils.StringConfig(crownColorOverrides);
    }

    private ConfigEntry<bool> enabled;
    private ConfigEntry<bool> useCrown;
    private ConfigEntry<bool> crownColorBasedOffBody;
    private ConfigEntry<string> crownColorOverrides;
}