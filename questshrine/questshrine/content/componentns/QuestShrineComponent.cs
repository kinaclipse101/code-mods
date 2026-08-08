using System;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace questshrine.content.componentns;

public class QuestShrineComponent : NetworkBehaviour
{
    public PurchaseInteraction purchaseInteraction;
    private GameObject shrineUseEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/ShrineUseEffect.prefab").WaitForCompletion();

    public void Start()
    {
        if (NetworkServer.active && Run.instance)
        {
            purchaseInteraction.SetAvailable(true);
        }

        purchaseInteraction.onDetailedPurchaseServer.AddListener(OnPurchase);
    }

    [Server]
    public void OnPurchase(CostTypeDef.PayCostContext context, CostTypeDef.PayCostResults payCostResults)
    {
        if (!NetworkServer.active)
        {
            Log.Warning("[Server] function 'BeebleMemorialManager::OnPurchase(RoR2.Interactor)' called on client");
            return;
        }

        EffectManager.SpawnEffect(shrineUseEffect, new EffectData()
        {
            origin = gameObject.transform.position,
            rotation = Quaternion.identity,
            scale = 3f,
            color = new Color32(103, 77, 170, 255)
        }, true);
        Chat.SendBroadcastChat(new Chat.SimpleChatMessage() { baseToken = "<style=cEvent><color=#674DAA>quest shrine activated .,.,.,</color></style>" });

        ActivateQuest(context);
    }

    public void ActivateQuest(CostTypeDef.PayCostContext context)
    {
        WeightedSelection<Type> weightedSelection = new WeightedSelection<Type>();
        foreach (Type itemTypeCombo in questshrine.instance.questComponents)
        {
            float weight = 10;
            
            int questCount = context.activatorBody.gameObject.GetComponents(itemTypeCombo).Length;
            for (int i = 0; i < questCount; i++)
            {
                weight /= 2;
            }
            
            weightedSelection.AddChoice(itemTypeCombo, weight);
        }
        
        Type component = weightedSelection.Evaluate(Run.instance.runRNG.nextNormalizedFloat);
        context.activatorBody.gameObject.AddComponent(component);
    }
}