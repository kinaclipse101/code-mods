using System;
using System.Collections.Generic;
using RoR2;
using RoR2.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace questshrine.bases;

public abstract class QuestBehaviorBase : MonoBehaviour
{
    public abstract QuestBase QuestBase { get; }
    public abstract Type ObjectiveType { get; }
    public static int notificationEnum = 1225;
    public string QuestDescInternal;
    public CharacterBody body => charMaster.GetBody();
    public CharacterMaster charMaster;
    public QuestInterfaceListener listener;

    private int startingStage;
    
    private void Awake()
    {
        startingStage = RoR2.Run.instance.stageClearCount;
        charMaster = GetComponent<CharacterMaster>();
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
        
        charMaster.onBodyStart += AddListenersNewStageCheck;
        AddListenersNewStageCheck(body);
    }
    
    private void AddListenersNewStageCheck(CharacterBody body)
    {
        if (startingStage != RoR2.Run.instance.stageClearCount)
        {
            Destroy(this);
            return;
        }
        
        if (!QuestBase.useListeners) return;
        
        if (body.gameObject.TryGetComponent(out listener))
        {
            listener.questListeners.Add(this);
            return;
        }
        
        listener = body.gameObject.AddComponent<QuestInterfaceListener>();
        listener.questListeners.Add(this);
    }

    protected Action<DamageReport> KilledOtherServer;
    protected Action<DamageReport> TakeDamageServer;

    public class QuestInterfaceListener : MonoBehaviour, IOnKilledOtherServerReceiver, IOnTakeDamageServerReceiver
    {
        public List<QuestBehaviorBase> questListeners = [];

        public void OnEnable()
        {
            HG.ArrayUtils.ArrayAppend(ref gameObject.GetComponent<CharacterBody>().healthComponent.onTakeDamageReceivers, this);
        }

        public void OnKilledOtherServer(DamageReport damageReport)
        {
            foreach (QuestBehaviorBase questBehaviorBase in questListeners)
            {
                questBehaviorBase?.KilledOtherServer(damageReport);
            }
        }

        public void OnTakeDamageServer(DamageReport damageReport)
        {
            foreach (QuestBehaviorBase questBehaviorBase in questListeners)
            {
                questBehaviorBase?.TakeDamageServer(damageReport);
            }
        }
    }

    public virtual void OnDisable()
    {
        ObjectivePanelController.collectObjectiveSources -= OnCollectObjectiveSources;
        if (QuestBase.useListeners)
        {
            listener.questListeners.Remove(this);
            if (listener.questListeners.Count == 0)
            {
                Destroy(listener);
            }
        }
        charMaster.onBodyStart -= AddListenersNewStageCheck;
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