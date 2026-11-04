using System;
using RoR2;
using UnityEngine;

namespace MithrixSlop;

public class PillarLockCommunicator : MonoBehaviour
{
    private HoldoutZoneController holdoutZoneController;

    private void OnEnable()
    {
        holdoutZoneController = gameObject.GetComponent<HoldoutZoneController>();
        holdoutZoneController.baseChargeDuration = 30f;

        if (!TeleporterInteraction.instance.gameObject.TryGetComponent(out LockTeleporterUntilPillarCompletion teleLock))
        {
            teleLock = TeleporterInteraction.instance.gameObject.AddComponent<LockTeleporterUntilPillarCompletion>();
        }
        teleLock.AddHoldout(holdoutZoneController);
    }
}