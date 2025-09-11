using UnityEngine;

public class EndPointTrigger : MonoBehaviour
{
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;

        PlatformManager manager = FindObjectOfType<PlatformManager>();
        if (manager != null)
        {
            manager.OnTriggerActivated(transform.parent);
            // parent = endPoint
        }
    }
}