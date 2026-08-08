using System;
using System.Collections.Generic;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace questshrine.bases;

public abstract class QuestBehaviorBase : MonoBehaviour
{
    public CharacterBody body;
    public abstract ItemDef ItemDef { get; }
    public virtual bool gaveReward { get; set; }
    public abstract Type objectiveType { get; }
    public abstract string titleText { get; }
    public string internalDesc;

    private void Awake()
    {
        body = GetComponent<CharacterBody>();
    }

    public virtual void OnEnable()
    {
        ObjectivePanelController.collectObjectiveSources += OnCollectObjectiveSources;
            
        CharacterMasterNotificationQueue notificationQueueForMaster = CharacterMasterNotificationQueue.GetNotificationQueueForMaster(body.master);
        var info = new CharacterMasterNotificationQueue.NotificationInfo(ItemDef, null, new CharacterMasterNotificationQueue.CustomOverrideInfo()
        {
            titleText = titleText,
            descriptionText = internalDesc,
            iconColor = new Color(1f, 1f, 1f, 1f)
        }, showExtra: false);

        if (notificationQueueForMaster.notifications.Count != 0)
        {
            notificationQueueForMaster.notifications.Add(new CharacterMasterNotificationQueue.TimedNotificationInfo
            {
                notification = info,
                startTime = Run.instance.fixedTime,
                duration = 3f
            });
        }
        else
        {
            notificationQueueForMaster.PushNotification(info, 3f);
        }
    }

    public virtual void OnDisable()
    {
        ObjectivePanelController.collectObjectiveSources -= OnCollectObjectiveSources;
    }

    public virtual void OnCollectObjectiveSources(CharacterMaster master, List<ObjectivePanelController.ObjectiveSourceDescriptor> objectiveSourcesList)
    {
        var newObjective = new ObjectivePanelController.ObjectiveSourceDescriptor
        {
            master = master,
            objectiveType = objectiveType,
            source = this
        };

        objectiveSourcesList.Add(newObjective);
    }
}