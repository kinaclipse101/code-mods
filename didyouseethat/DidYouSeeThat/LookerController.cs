using RoR2;
using RoR2.HudOverlay;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace DidYouSeeThat;

public class LookerController : NetworkBehaviour
{
    private OverlayController overlayController;
    private float secondsTimer;
    private float timeout = -1;
    
    public void OnEnable()
    {
        Log.Debug("OnEnable");
        RoR2.Run.onRunDestroyGlobal += _ => Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    public void Update()
    {
        if (!NetworkServer.active) return;

        if (!Mathf.Approximately(timeout, -1) && timeout < Time.unscaledTime)
        {
            timeout = -1;
            Time.timeScale = 1;

            //RemoveOverlay();
            RpcRemoveOverlay();
        }
    }

    public void FixedUpdate()
    {
        if (!NetworkServer.active) return;
        
        secondsTimer += Time.fixedDeltaTime;
        if (secondsTimer < 1) return;
        secondsTimer = 0;

        if (Mathf.Approximately(timeout, -1) && Run.instance && Random.Range(0f, 100f) < DidYouSeeThat.instance.lookerChance.Value)
        {
            //ActivateLooker();
            RpcActivateLooker();
        }
    }

    public void ActivateLooker()
    {
        timeout = Time.unscaledTime + 3;
        overlayController = HudOverlayManager.AddOverlay(LocalUserManager.GetFirstLocalUser().cachedBodyObject, new OverlayCreationParams
        {
            prefab = DidYouSeeThat.instance.lookerOverlayPrefab,
            childLocatorEntry = "ScopeContainer"
        });
    }
    
    [ClientRpc]
    public void RpcActivateLooker()
    {
        Log.Debug("RpcActivateLooker");
        ActivateLooker();
    }

    public void RemoveOverlay()
    {
        Time.timeScale = 1;
        HudOverlayManager.RemoveOverlay(overlayController);
    }
    
    [ClientRpc]
    public void RpcRemoveOverlay()
    {
        Log.Debug("RpcRemoveOverlay");
        RemoveOverlay();
    }
}