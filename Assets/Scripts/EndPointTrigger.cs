using UnityEngine;

public class EndPointTrigger : MonoBehaviour
{
    [SerializeField] private bool debugLogs = true;

    private PlatformManager platformManager;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;

        if (platformManager == null)
            platformManager = FindObjectOfType<PlatformManager>();

        if (debugLogs)
        {
            string managerInfo = platformManager == null
                ? "null"
                : $"{platformManager.name}#{platformManager.GetInstanceID()}";
            Debug.Log($"[EndPointTrigger:{name}] Triggered | parent={(transform.parent != null ? transform.parent.name : "null")} | manager={managerInfo}");
        }

        if (platformManager != null)
        {
            platformManager.OnTriggerActivated(transform.parent);
            // parent = endPoint
        }
        else if (debugLogs)
        {
            Debug.LogWarning($"[EndPointTrigger:{name}] Nie znaleziono PlatformManager na scenie.");
        }
    }
}