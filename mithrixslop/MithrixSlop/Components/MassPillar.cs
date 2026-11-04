using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MithrixSlop;

public class MassPillar : MonoBehaviour
{
    private void Start()
    {
        OnDisableEvent onDisableEvent = GetComponent<OnDisableEvent>();
        onDisableEvent.action.AddListener(SpawnPillar);
    }

    private void SpawnPillar()
    {
        if (!NetworkServer.active) return;
        
        EffectData effectData = new EffectData
        {
            origin = transform.position,
        };
        EffectManager.SpawnEffect(MithrixSlop.impactEffectPrefab, effectData, transmit: true);
        
        uint damage = 35 + 7 * TeamManager.instance.GetTeamLevel(TeamIndex.Monster);
        BlastAttack blastAttack = new BlastAttack
        {
            inflictor = base.gameObject,
            baseDamage = damage,
            baseForce = 400,
            attackerFiltering = AttackerFiltering.Default,
            crit = false,
            falloffModel = BlastAttack.FalloffModel.Linear,
            attacker = MithrixSlop.braindeadmithrix5000unitsunderthemap?.gameObject,
            bonusForce = Vector3.zero,
            damageColorIndex = DamageColorIndex.Default,
            position = transform.position,
            procChainMask = default,
            procCoefficient = 1f,
            teamIndex = TeamIndex.Monster,
            radius = 40f,
        };
        blastAttack.Fire();
        
        GameObject pillar = Instantiate(MithrixSlop.massPilarModified, transform.position, Quaternion.identity);
        NetworkServer.Spawn(pillar);

        List<PlayerCharacterMasterController> alivePlayers = [];
        foreach (PlayerCharacterMasterController pc in PlayerCharacterMasterController.instances)
        {
            if(pc.body)
                alivePlayers.Add(pc);
        }
        BrotherUltUtils.EvaluatePattern(MithrixSlop.pizzaFissure, new BrotherUltUtils.BrotherUltContext
        {
            owner = MithrixSlop.braindeadmithrix5000unitsunderthemap?.gameObject,
            target = alivePlayers[Random.Range(0, alivePlayers.Count)].body.gameObject,
            originOverride = transform.position,
            useOriginOverride = true
        });  
    }
}