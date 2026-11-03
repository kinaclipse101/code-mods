using BNR.patches;
using BepInEx.Configuration;
using RoR2;
using R2API;
using Skillsmas.Skills.Merc;

namespace BNR;

public class skillsmas : ModdedPatchBase<skillsmas>
{
	public override string chainLoaderKey => "com.themysticsword.skillsmas";

	public override void Init()
    {     
        if(!enabled.Value) return; 
        
        GlobalEventManager.onCharacterDeathServer += GlobalEventManagerOnonCharacterDeathGlobal;
    }

	private static void GlobalEventManagerOnonCharacterDeathGlobal(DamageReport damageReport)
    {
        if (damageReport.damageInfo != null && damageReport.damageInfo.HasModdedDamageType(Zandatsu.zandatsuDamageType))
        {
            if (damageReport.attackerBody)
            {   
                damageReport.attackerBody.skillLocator.special.ApplyAmmoPack();
            }
        }
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("Mods - skillsmas",
            "enable patches for skillsmas",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        
        zandatsuRechargeSkill = config.Bind("Mods - skillsmas",
            "have zandatsu recharge skill on kill !!",
            true,
            "like !! if it kills something refresh cooldown !!");
        Utils.CheckboxConfig(zandatsuRechargeSkill);
    }

    private static ConfigEntry<bool> zandatsuRechargeSkill;
    private ConfigEntry<bool> enabled;
}