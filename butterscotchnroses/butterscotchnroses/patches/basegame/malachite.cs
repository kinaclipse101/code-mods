using static BNR.butterscotchnroses;
using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace BNR;

public class malachite : PatchBase<malachite>
{
    public static GameObject urchinMaster;
    public override void Init()
    {     
        applyHooks();
        urchinMaster = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ElitePoison.UrchinTurretMaster_prefab).WaitForCompletion();
    }
        
    private void applyHooks()
    {
        if (enabled.Value && clientSide.Value)
        {
            On.RoR2.CharacterBody.OnBuffFirstStackGained += CharacterBodyOnOnBuffFirstStackGained;
        }
        else
        {
            On.RoR2.CharacterBody.OnBuffFirstStackGained -= CharacterBodyOnOnBuffFirstStackGained;
        }
    }

    private void CharacterBodyOnOnBuffFirstStackGained(On.RoR2.CharacterBody.orig_OnBuffFirstStackGained orig, CharacterBody self, BuffDef buffDef)
    {
        orig(self, buffDef);

        if (buffDef != RoR2Content.Buffs.AffixPoison) return;
                
        EvilUrchinSpawner spawner = self.gameObject.AddComponent<EvilUrchinSpawner>();
        spawner._characterBody = self;
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - malachite",
            "enable patches for malachite (have them naturally spawn urchins ,.,.",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) =>
        {
            applyHooks();
        };
        
        timeBetweenSpawns = config.Bind("BNR - malachite",
            "time between urchin spawns .,,.",
            12f,
            "byeah.,,");
        Utils.SliderConfig(1, 30, timeBetweenSpawns);
    }

    private ConfigEntry<bool> enabled;
    public static ConfigEntry<float> timeBetweenSpawns;
}

public class EvilUrchinSpawner : MonoBehaviour
{
    public CharacterBody _characterBody;
    private float urchinSpawnTimer;

    private void OnEnable()
    {
        Log.Debug("added urchin spawner !!");
    }

    private void FixedUpdate()
    {
        urchinSpawnTimer += Time.fixedDeltaTime;

        if (!(urchinSpawnTimer > malachite.timeBetweenSpawns.Value)) return;
            
        urchinSpawnTimer = 0f;
        Log.Debug("spawning urgin !!");

        if (!_characterBody || !_characterBody.HasBuff(RoR2Content.Buffs.AffixPoison))
        {
            Log.Debug("uh oh,,");
            Destroy(this);
            return;
        }
        
        GameObject instantiatedMaster = Instantiate(malachite.urchinMaster, new Vector3(transform.position.x, transform.position.y + 5, transform.position.z), Quaternion.LookRotation(Vector3.up));
        CharacterMaster master = instantiatedMaster.GetComponent<CharacterMaster>();
        if (!master) return;
        
        master.teamIndex = _characterBody.teamComponent.teamIndex;
        NetworkServer.Spawn(instantiatedMaster);
        master.SpawnBodyHere();
    }
}
