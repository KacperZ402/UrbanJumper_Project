using UnityEngine;

public class endPlaftormTrigger : MonoBehaviour
{
    public GameObject startPlatformPrefab;

    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;

        Transform spawnPoint = transform.parent.Find("endPoint");
        spawnPoint.position += new Vector3(0f, 0f, 150f);

        if (spawnPoint != null)
        {
            Instantiate(startPlatformPrefab, spawnPoint.position, Quaternion.identity);
        }
        else
        {
            Debug.LogError("Brak endPoint w EndPlatform!");
        }
    }
}
