using System;
using System.Collections.Generic;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace questshrine.bases;

public abstract class QuestBehaviorBase : MonoBehaviour
{
    public abstract QuestBase QuestBase { get; }
    public abstract Type ObjectiveType { get; }
    public static int notificationEnum = 1225;
    public string QuestDescInternal;
    public CharacterBody body;
    
    private void Awake()
    {
        body = GetComponent<CharacterBody>();
    }

    public virtual void OnEnable()
    {
        ObjectivePanelController.collectObjectiveSources += OnCollectObjectiveSources;
            
        CharacterMasterNotificationQueue notificationQueueForMaster = CharacterMasterNotificationQueue.GetNotificationQueueForMaster(body.master);
        var info = new CharacterMasterNotificationQueue.NotificationInfo(QuestBase, new CharacterMasterNotificationQueue.TransformationInfo((CharacterMasterNotificationQueue.TransformationType)notificationEnum, null), new CharacterMasterNotificationQueue.CustomOverrideInfo()
        {
            titleText = QuestBase.QuestTitle,
            descriptionText = QuestDescInternal,
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
            objectiveType = ObjectiveType,
            source = this
        };

        objectiveSourcesList.Add(newObjective);
    }
}