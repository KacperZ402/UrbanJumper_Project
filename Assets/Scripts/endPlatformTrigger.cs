using System.Collections;
using UnityEngine;

public class endPlaftormTrigger : MonoBehaviour
{
    public GameObject startPlatformPrefab;
    [SerializeField] private int floorSpawnersPerStep = 1;

    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;

        SpawnWorkQueue.Enqueue(this, SpawnNextStartPlatform);
    }

    private void SpawnNextStartPlatform()
    {
        if (startPlatformPrefab == null)
        {
            Debug.LogError("Brak startPlatformPrefab w endPlaftormTrigger!");
            return;
        }

        if (transform.parent == null)
        {
            Debug.LogError("Brak parenta dla endPlaftormTrigger!");
            return;
        }

        Transform spawnPoint = transform.parent.Find("endPoint");
        if (spawnPoint == null)
        {
            Debug.LogError("Brak endPoint w EndPlatform!");
            return;
        }

        Vector3 spawnPosition = spawnPoint.position + new Vector3(0f, 0f, 150f);

        GameObject platform = Instantiate(startPlatformPrefab, spawnPosition, Quaternion.identity);

        FloorPropSpawner[] floorSpawners = platform.GetComponentsInChildren<FloorPropSpawner>(true);
        WallPropSpawner[] wallSpawners = platform.GetComponentsInChildren<WallPropSpawner>(true);

        for (int i = 0; i < wallSpawners.Length; i++)
        {
            if (wallSpawners[i] != null)
                wallSpawners[i].enabled = false;
        }

        for (int i = 0; i < floorSpawners.Length; i++)
        {
            if (floorSpawners[i] != null)
                floorSpawners[i].enabled = false;
        }

        StartCoroutine(EnableSpawnersQueued(wallSpawners, floorSpawners));
    }

    private IEnumerator EnableSpawnersQueued(WallPropSpawner[] wallSpawners, FloorPropSpawner[] floorSpawners)
    {
        int step = Mathf.Max(1, floorSpawnersPerStep);

        if (wallSpawners != null)
        {
            for (int i = 0; i < wallSpawners.Length; i++)
            {
                WallPropSpawner spawner = wallSpawners[i];
                if (spawner != null)
                {
                    SpawnWorkQueue.Enqueue(this, () =>
                    {
                        if (spawner != null)
                            spawner.enabled = true;
                    });
                }

                if ((i + 1) % step == 0)
                    yield return null;
            }
        }

        if (floorSpawners == null || floorSpawners.Length == 0)
            yield break;

        for (int i = 0; i < floorSpawners.Length; i++)
        {
            FloorPropSpawner spawner = floorSpawners[i];
            if (spawner != null)
            {
                SpawnWorkQueue.Enqueue(this, () =>
                {
                    if (spawner != null)
                        spawner.enabled = true;
                });
            }

            if ((i + 1) % step == 0)
                yield return null;
        }
    }
}