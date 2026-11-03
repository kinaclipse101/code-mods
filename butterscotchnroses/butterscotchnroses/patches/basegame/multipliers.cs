using System;
using BNR.patches;
using BepInEx.Configuration;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace BNR;

public class multipliers : PatchBase<multipliers>
{
    public override void Init()
    {
        applyHooks();
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            On.RoR2.CharacterMaster.GiveMoney_uint_bool += CharacterMasterOnGiveMoney_uint;
            On.RoR2.TeamManager.GiveTeamExperience += TeamManagerOnGiveTeamExperience;
            IL.RoR2.SceneDirector.PopulateScene += PopulateSceneIL;
        }
        else
        {
            On.RoR2.CharacterMaster.GiveMoney_uint_bool -= CharacterMasterOnGiveMoney_uint;
            On.RoR2.TeamManager.GiveTeamExperience -= TeamManagerOnGiveTeamExperience;
            IL.RoR2.SceneDirector.PopulateScene -= PopulateSceneIL;
        }
    }

    private void PopulateSceneIL(ILContext il)
    {
        var cursor = new ILCursor(il);

        /* // interactableCredit -= directorCard.cost;
           IL_0129: ldarg.0
           IL_012a: ldarg.0
           IL_012b: call instance int32 RoR2.SceneDirector::get_interactableCredit()
           IL_0130: ldloc.s 4
           IL_0132: callvirt instance int32 RoR2.DirectorCard::get_cost()
           IL_0137: sub
         */
        
        if (!cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<SceneDirector>("get_interactableCredit"),
                x => x.MatchLdloc(out _),
                x => x.MatchCallOrCallvirt<DirectorCard>("get_cost"),
                x => x.MatchSub()))
        {
            Log.Error("failed il 1");
            return;
        }

        cursor.Index--;
        cursor.EmitDelegate<Func<int, int>>((cardCost) =>
        {
            if(Mathf.Approximately(interactableMultiplier.Value, 1))
                return cardCost;
            
            int cost = (int)(cardCost * interactableMultiplier.Value);
            return cost > 0 ? cost : interactableMultiplierMin.Value;
        });
    }

    private void TeamManagerOnGiveTeamExperience(On.RoR2.TeamManager.orig_GiveTeamExperience orig, RoR2.TeamManager self, TeamIndex teamindex, ulong experience)
    {
        if (teamindex != TeamIndex.Player)
        {
            orig(self, teamindex, experience);
            return;
        }
        
        orig(self, teamindex, (ulong)(experience * xpMultiplier.Value));
    }

    private void CharacterMasterOnGiveMoney_uint(On.RoR2.CharacterMaster.orig_GiveMoney_uint_bool orig, RoR2.CharacterMaster self, uint amount, bool bypassStatManager)
    {
        orig(self, (uint)(amount * moneyMultiplier.Value), bypassStatManager);
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - multipliers",
            "enable patches for multipliers",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) => { applyHooks(); };
        
        moneyMultiplier = config.Bind("BNR - multipliers",
            "money multiplier",
            1f,
            "amount to multiply incoming money by ,..,");
        Utils.SliderConfig(0, 1, moneyMultiplier);
        
        xpMultiplier = config.Bind("BNR - multipliers",
            "xp multiplier",
            1f,
            "amount to multiply incoming xp by ,..,");
        Utils.SliderConfig(0, 1, xpMultiplier);
        
        interactableMultiplier = config.Bind("BNR - multipliers",
            "interactable multiplier",
            1f,
            "amount to multiply interactable director cost by ,.. (higher number = less interactable ,.,,");
        Utils.SliderConfig(0f, 1f, interactableMultiplier);
        
        interactableMultiplierMin = config.Bind("BNR - multipliers",
            "interactable multiplier min",
            1,
            "minimum value for interactables to cost .,.,. if they all cost 0 causes game to infinite loop,,.,");
        Utils.SliderConfig(0, 1, interactableMultiplierMin);
    }

    private ConfigEntry<float> moneyMultiplier;
    private ConfigEntry<float> xpMultiplier;
    private ConfigEntry<float> interactableMultiplier;
    private ConfigEntry<int> interactableMultiplierMin;
    private ConfigEntry<bool> enabled;
}