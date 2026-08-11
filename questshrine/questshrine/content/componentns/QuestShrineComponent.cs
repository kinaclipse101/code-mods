using System;
using System.Linq;
using System.Reflection;
using questshrine.bases;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace questshrine.content.componentns;

public class QuestShrineComponent : NetworkBehaviour
{
    private static readonly GameObject shrineUseEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/ShrineUseEffect.prefab").WaitForCompletion();
    public PurchaseInteraction purchaseInteraction;

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
            Log.Warning("on purchase server called on client .,.");
            return;
        }

        EffectManager.SpawnEffect(shrineUseEffect, new EffectData()
        {
            origin = gameObject.transform.position,
            rotation = Quaternion.identity,
            scale = 3f,
            color = new Color32(103, 77, 170, 255)
        }, true);

        ActivateQuest(context);
        
        purchaseInteraction.SetAvailable(false);
    }

    public void ActivateQuest(CostTypeDef.PayCostContext context)
    {
        WeightedSelection<QuestBase> weightedSelection = new WeightedSelection<QuestBase>();
        foreach (QuestBase questBase in questshrine.instance.questComponents)
        {
            float weight = 10;
            
            int questCount = context.activatorMaster.gameObject.GetComponents(questBase.Behavior).Length;
            for (int i = 0; i < questCount; i++)
            {
                weight /= 2;
            }

            if (questBase.Tags != null)
            {
                foreach (string tagType in questBase.Tags)
                {
                    switch (tagType)
                    {
                        case "noStack":
                            if (questCount > 0)
                            {
                                weight = 0;
                            }
                            break;
                        case "requireScrapper":
                            GameObject scrapper = GameObject.Find("Scrapper(Clone)");
                            if (!scrapper)
                            {
                                weight = 0;
                            }
                            break;
                    }
                }
            }
            
            weightedSelection.AddChoice(questBase, weight);
        }

        QuestBase selectedQuest = weightedSelection.Evaluate(Run.instance.runRNG.nextNormalizedFloat);
        //context.activatorMaster.gameObject.AddComponent(selectedQuest.Behavior);
        RpcActivateQuest(context.activatorMaster.gameObject, questshrine.instance.questComponents.IndexOf(selectedQuest));
    }

    [ClientRpc]
    public void RpcActivateQuest(GameObject master, int questIndex)
    {
        Log.Debug("ran clientrpc !");
        master.gameObject.AddComponent(questshrine.instance.questComponents[questIndex].Behavior);
    }
}