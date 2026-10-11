using RoR2.UI;
using UnityEngine;

namespace DidYouSeeThat;

public class SkeleRunner : MonoBehaviour
{
    private GameObject skeleton;
    private float timer;
    
    public void OnEnable()
    {
        skeleton = Instantiate(DidYouSeeThat.instance.runningPrefab, gameObject.transform);
    }

    public void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        if (timer < 1f) return;
        
        Instantiate(DidYouSeeThat.instance.grayscalePrefab, gameObject.transform);
        gameObject.GetComponent<SniperTargetViewer>().enabled = true;
        Destroy(skeleton);
        timer = -9999999999999;
        Time.timeScale = 0;
    }
}