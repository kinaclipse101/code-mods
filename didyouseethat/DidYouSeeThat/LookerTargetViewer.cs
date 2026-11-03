using System.Collections.Generic;
using HG;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace DidYouSeeThat;

public class LookerTargetViewer : SniperTargetViewer
{
    public void Update()
    {
        List<HurtBox> list = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
        if ((bool)hud && (bool)hud.targetMaster)
        {
            TeamIndex teamIndex = hud.targetMaster.teamIndex;
            IReadOnlyList<HurtBox> readOnlySniperTargetsList = HurtBox.readOnlySniperTargetsList;
            int i = 0;
            for (int count = readOnlySniperTargetsList.Count; i < count; i++)
            {
                HurtBox hurtBox = readOnlySniperTargetsList[i];
                if ((bool)hurtBox.healthComponent && hurtBox.healthComponent.alive && hurtBox.healthComponent.body != hud.targetMaster.GetBody())
                {
                    list.Add(hurtBox);
                }
            }
        }
        SetDisplayedTargets(list);
        list = CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(list);
    }
    
    public void OnDestroy()
    {
        Time.timeScale = 1;
    }
}