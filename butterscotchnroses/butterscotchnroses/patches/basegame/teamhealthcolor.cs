using System.Collections.Generic;
using BNR.patches;
using BepInEx.Configuration;
using On.RoR2.UI;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

using TeamCatalog = On.RoR2.TeamCatalog;
using TeamComponent = IL.RoR2.TeamComponent;

namespace BNR;

public class teamhealthcolor : PatchBase<teamhealthcolor>
{
    public override void Init()
    {
        applyHooks();
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            Addressables.LoadAssetAsync<RoR2.UI.HealthBarStyle>("RoR2/Base/Common/CombatHealthBar.asset").Completed += handle =>
            {
                defaultHealthColor = handle.Result.trailingOverHealthBarStyle.baseColor;
            };
            
            Addressables.LoadAssetAsync<RoR2.UI.HealthBarStyle>("RoR2/Base/Common/HUDHealthBar.asset").Completed += handle =>
            {
                playerHealthColor = handle.Result.trailingOverHealthBarStyle.baseColor;
            };
            
            RoR2.RoR2Application.onLoad += OnLoad;
            On.RoR2.UI.HealthBar.UpdateBarInfos += HealthBarOnUpdateBarInfos;
        }
        else
        {
            RoR2.RoR2Application.onLoad -= OnLoad;
            On.RoR2.UI.HealthBar.UpdateBarInfos -= HealthBarOnUpdateBarInfos;
        }
    }

    private void OnLoad()
    {
        foreach (TeamDef teamDef in RoR2.TeamCatalog.teamDefs)
        {
            Log.Debug($"adding team {teamDef}");
            
            UnityEngine.Color healthColor = defaultHealthColor;
            if (RoR2.TeamCatalog.GetTeamDef(TeamIndex.Player) == teamDef)
            {
                healthColor = playerHealthColor;
            }
            
            ConfigEntry<bool> teamEnabled = butterscotchnroses.instance.Config.Bind("BNR - teamhealthcolor",
                $"enable team {teamDef.nameToken.Replace("TEAM_", "").Replace("_NAME", "").ToLower()}",
                true,
                "");
            Utils.CheckboxConfig(teamEnabled, true);
            
            ConfigEntry<Color> teamDefColor = butterscotchnroses.instance.Config.Bind("BNR - teamhealthcolor",
                $"health bar color for team {teamDef.nameToken.Replace("TEAM_", "").Replace("_NAME", "").ToLower()}",
                healthColor,
                "");
            ModSettingsManager.AddOption(new ColorOption(teamDefColor));

            if (teamEnabled.Value)
            {
                teamDefColors.Add(teamDef, teamDefColor);
            }
        }
    }

    public static Color defaultHealthColor;
    public static Color playerHealthColor;
    public static Dictionary<TeamDef, ConfigEntry<Color>> teamDefColors = [];
    
    private void HealthBarOnUpdateBarInfos(HealthBar.orig_UpdateBarInfos orig, RoR2.UI.HealthBar self)
    {
        //stolen from neb neb ,.., probably should use an item like they did too ,., .
        orig(self);
            
        HealthComponent healthComponent = self._source;
        if (!healthComponent) return;
        if (self.barInfoCollection.trailingOverHealthbarInfo.color != defaultHealthColor && self.barInfoCollection.trailingOverHealthbarInfo.color != playerHealthColor) return;
        
        TeamDef teamDef = RoR2.TeamCatalog.GetTeamDef(healthComponent.body?.teamComponent?.teamIndex ?? TeamIndex.None);
        if (teamDef != null && teamDefColors.TryGetValue(teamDef, out ConfigEntry<Color> teamDefColor))
        {
            self.barInfoCollection.trailingOverHealthbarInfo.color = teamDefColor.Value;
        }
        //Log.Debug($"self.barInfoCollection.trailingOverHealthbarInfo.color {self.barInfoCollection.trailingOverHealthbarInfo.color}");
        //self.barInfoCollection.trailingOverHealthbarInfo.color = new Color32(100, 200, 255, 255);
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - teamhealthcolor",
            "enable patches for teamhealthcolor",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) => { applyHooks(); };
    }

    private ConfigEntry<bool> enabled;
}