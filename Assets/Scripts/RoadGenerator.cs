using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class RoadGenerator : MonoBehaviour
{
    [Header("Prefabs (0 = straight, reszta = crossings incl. T)")]
    public List<GameObject> roadPrefabs;

    [Header("Settings")]
    public int maxSegments = 50;
    public bool useBatchedSpawn = true;
    public int maxSegmentsPerFrame = 5;

    [Header("Queue child SingleObjectSpawner")]
    public bool queueChildSingleSpawners = true;
    public int childSingleSpawnersPerFrame = 10;

    private Coroutine spawnRoutine;
    private Coroutine enableChildSpawnersRoutine;
    private readonly List<SingleObjectSpawner> pendingChildSpawners = new List<SingleObjectSpawner>();

    void OnEnable()
    {
        SpawnWorkQueue.Enqueue(this, StartRoadGeneration);
    }

    private void StartRoadGeneration()
    {
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        if (enableChildSpawnersRoutine != null)
            StopCoroutine(enableChildSpawnersRoutine);

        pendingChildSpawners.Clear();

        spawnRoutine = StartCoroutine(GenerateRoadRoutine());
    }

    private IEnumerator GenerateRoadRoutine()
    {
        while (SingleObjectPool.Instance == null)
            yield return null;

        Vector3 position = transform.position;
        int count = 0;
        int sinceLastCross = 0;
        int spawnedThisFrame = 0;

        while (count < maxSegments)
        {
            GameObject prefab = ChoosePrefab(sinceLastCross);
            if (prefab == null)
                break;

            GameObject obj = SingleObjectPool.Instance.Get(prefab, position, Quaternion.identity, transform);

            if (queueChildSingleSpawners)
                CollectAndDisableChildSingleSpawners(obj);

            count++;
            spawnedThisFrame++;

            int prefabIndex = roadPrefabs.IndexOf(prefab);
            if (prefabIndex == roadPrefabs.Count - 1)
                break;

            Transform endPoint = obj.transform.Find("EndPoint");
            if (endPoint == null)
                break;

            sinceLastCross = prefabIndex == 0 ? sinceLastCross + 1 : 0;
            position = endPoint.position;

            if (useBatchedSpawn && spawnedThisFrame >= Mathf.Max(1, maxSegmentsPerFrame))
            {
                spawnedThisFrame = 0;
                yield return null;
            }
        }

        if (queueChildSingleSpawners && pendingChildSpawners.Count > 0)
            enableChildSpawnersRoutine = StartCoroutine(EnableChildSingleSpawnersRoutine());

        spawnRoutine = null;
    }

    private void CollectAndDisableChildSingleSpawners(GameObject segment)
    {
        if (segment == null)
            return;

        SingleObjectSpawner[] spawners = segment.GetComponentsInChildren<SingleObjectSpawner>(true);
        for (int i = 0; i < spawners.Length; i++)
        {
            SingleObjectSpawner spawner = spawners[i];
            if (spawner == null)
                continue;

            spawner.enabled = false;
            pendingChildSpawners.Add(spawner);
        }
    }

    private IEnumerator EnableChildSingleSpawnersRoutine()
    {
        int step = Mathf.Max(1, childSingleSpawnersPerFrame);

        for (int i = 0; i < pendingChildSpawners.Count; i++)
        {
            SingleObjectSpawner spawner = pendingChildSpawners[i];

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

        pendingChildSpawners.Clear();
        enableChildSpawnersRoutine = null;
    }

    GameObject ChoosePrefab(int sinceLastCross)
    {
        // po skrzyżowaniu zawsze minimum jedna prosta
        if (sinceLastCross < 1) return roadPrefabs[0];

        return roadPrefabs[Random.Range(0, roadPrefabs.Count)];
    }
}