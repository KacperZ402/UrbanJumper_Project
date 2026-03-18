using UnityEngine;

public class EndPointTrigger : MonoBehaviour
{
    [SerializeField] private PlatformManager platformManager;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;

        if (platformManager != null)
        {
            platformManager.OnTriggerActivated(transform.parent);
            // parent = endPoint
        }
    }
}