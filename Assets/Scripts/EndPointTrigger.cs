using UnityEngine;

public class EndPointTrigger : MonoBehaviour
{
    public string playerTag = "Player";
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;

        triggered = true;

        PlatformManager manager = FindObjectOfType<PlatformManager>();
        if (manager != null)
        {
            manager.SpawnNext(transform.parent);
            // parent = endPoint, bo trigger jest jego dzieckiem
        }
    }
}
