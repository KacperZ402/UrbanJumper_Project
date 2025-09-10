using UnityEngine;

public class EndPointTrigger : MonoBehaviour
{
    public PlatformManager manager;
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;

        triggered = true;

        if (manager != null)
        {
            manager.SpawnNext(transform.parent); // spawn segmentu na pozycji rodzica
        }

        Destroy(gameObject); // usuwamy trigger po u¿yciu
    }
}