using System;
using System.Collections.Generic;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace MithrixSlop;

public class LockTeleporterUntilPillarCompletion : MonoBehaviour
{
    public static LockTeleporterUntilPillarCompletion instance;
    
    private List<HoldoutZoneController> pillars = [];
    private GameObject lockObject;
    private HoldoutZoneController teleHoldout;
    private Material prevTeleMat;
    private GameObject loopHolder;
    private Color prevChargeColor;
    private static Material teleMat;
    private static readonly int RemapTex = Shader.PropertyToID("_RemapTex");
    private float stuckChargePercent = -1;

    private void OnEnable()
    {
        instance = this;
        teleHoldout = gameObject.GetComponent<HoldoutZoneController>();
    }

    private void OnPillarsChanged()
    {
        if (!NetworkServer.active) return;
        
        if (pillars.Count == 0)
        {
            TeleporterInteraction.instance.locked = false;
            
            if (lockObject)
            {
                NetworkServer.Destroy(lockObject);
            }
            
            ObjectivePanelController.collectObjectiveSources -= OnCollectObjectiveSources;
            
            if (TeleporterInteraction.instance.activationState == TeleporterInteraction.ActivationState.Charging)
            {
                var holdoutSphere = teleHoldout.radiusIndicator.gameObject.GetComponent<MeshRenderer>();
                holdoutSphere.material = prevTeleMat;
                
                TeleporterInteraction.instance.teleporterChargeIndicatorController.spriteChargingColor = prevChargeColor;
                stuckChargePercent = -1;
                
                if(loopHolder)
                    loopHolder.SetActive(true);
            }
        }
        else if (stuckChargePercent < 0)
        {
            TeleporterInteraction.instance.locked = true;
            
            if (!lockObject)
            {
                lockObject = Object.Instantiate(MithrixSlop.purchaseLockBlue, TeleporterInteraction.instance.gameObject.transform);
                NetworkServer.Spawn(lockObject);
            }
            
            ObjectivePanelController.collectObjectiveSources += OnCollectObjectiveSources;

            if (TeleporterInteraction.instance.activationState == TeleporterInteraction.ActivationState.Charging)
            {
                var holdoutSphere = teleHoldout.radiusIndicator.gameObject.GetComponent<MeshRenderer>();
                if (!teleMat)
                {
                    prevTeleMat = holdoutSphere.material;
                    teleMat = Instantiate(prevTeleMat);
                    teleMat.SetTexture(RemapTex, Addressables.LoadAssetAsync<Texture2D>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_ColorRamps.texRampLunarWispFire_png).WaitForCompletion());
                }
                holdoutSphere.material = teleMat;
                stuckChargePercent = teleHoldout.charge;
                prevChargeColor = TeleporterInteraction.instance.teleporterChargeIndicatorController.spriteChargingColor;
                TeleporterInteraction.instance.teleporterChargeIndicatorController.spriteChargingColor = new Color32(172, 244, 252, 255);
                loopHolder = TeleporterInteraction.instance.gameObject.transform.Find("TeleporterBaseMesh/BuiltInEffects/ChargingEffect/BetweenProngs/Loop")?.gameObject;
                if(loopHolder)
                    loopHolder.SetActive(false);
            }
        }
    }

    public void Update()
    {
        if (stuckChargePercent > 0)
        {
            teleHoldout.charge = stuckChargePercent;
        }
    }

    public void OnDestroy()
    {
        ObjectivePanelController.collectObjectiveSources -= OnCollectObjectiveSources;
    }

    public void AddHoldout(HoldoutZoneController holdout)
    {
        holdout.onCharged.AddListener(OnCharged);
        pillars.Add(holdout);
        OnPillarsChanged();
    }

    private void OnCharged(HoldoutZoneController holdout)
    {
        pillars.Remove(holdout);
        OnPillarsChanged();
    }

    private static void OnCollectObjectiveSources(CharacterMaster master, List<ObjectivePanelController.ObjectiveSourceDescriptor> objectiveSourcesList)
    {
        var newObjective = new ObjectivePanelController.ObjectiveSourceDescriptor
        {
            master = master,
            objectiveType = typeof(PrimalBirthrightObjectiveTracker),
            source = instance
        };

        objectiveSourcesList.Add(newObjective);
    }

    private class PrimalBirthrightObjectiveTracker : ObjectivePanelController.ObjectiveTracker
    {
        public override string GenerateString()
        {
            return instance.pillars.Count == 0 ? "Completed the <style=cIsLunar>Pillar(s)</style>!" : "Complete the <style=cIsLunar>Pillar(s)</style>!";
        }

        public override bool IsDirty()
        {
            return true;
        }
    }
}