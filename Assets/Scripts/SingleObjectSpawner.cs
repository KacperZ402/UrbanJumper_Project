using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class SingleObjectSpawner : MonoBehaviour
{
    private const string SpawnBlockerTag = "SpawnBlocker";
    private const int MaxSpawnersPerFrame = 40;
    private const float MaxQueueTimePerFrameMs = 1.5f;
    private static readonly Collider[] BlockerHitsBuffer = new Collider[16];

    private static readonly Queue<SingleObjectSpawner> PendingSpawners = new Queue<SingleObjectSpawner>();
    private static bool isProcessingQueue;

    [Header("Lista prefabów")]
    public List<GameObject> prefabs;

    [Header("Szansa na respawn (0–1)")]
    [Range(0f, 1f)]
    public float spawnChance = 1f;

    [Header("Ignoruj SpawnBlockery")]
    public bool ignoreSpawnBlockers = false;

    [Header("Layer, które nie są czyszczone")]
    public string keepLayerName = "Keep";

    [Header("Batch spawn")]
    public bool useBatchedSpawn = true;

    private int keepLayer;
    private bool isQueuedForSpawn;

    private void Awake()
    {
        keepLayer = LayerMask.NameToLayer(keepLayerName);
    }

    private void OnEnable()
    {
        // Najpierw wyczyść poprzednie dzieci
        ClearChildren();

        if (useBatchedSpawn)
        {
            SpawnWorkQueue.Enqueue(this, QueueSpawn);
            return;
        }

        TrySpawnNow();
    }

    private void OnDisable()
    {
        isQueuedForSpawn = false;
    }

    private void QueueSpawn()
    {
        if (isQueuedForSpawn)
            return;

        isQueuedForSpawn = true;
        PendingSpawners.Enqueue(this);

        if (!isProcessingQueue)
        {
            if (SingleObjectPool.Instance != null)
                SingleObjectPool.Instance.StartCoroutine(ProcessSpawnQueue());
            else
                StartCoroutine(ProcessSpawnQueue());
        }
    }

    private static IEnumerator ProcessSpawnQueue()
    {
        isProcessingQueue = true;

        while (PendingSpawners.Count > 0)
        {
            float frameStart = Time.realtimeSinceStartup;
            int processedThisFrame = 0;

            while (processedThisFrame < MaxSpawnersPerFrame && PendingSpawners.Count > 0)
            {
                float elapsedMs = (Time.realtimeSinceStartup - frameStart) * 1000f;
                if (elapsedMs >= MaxQueueTimePerFrameMs)
                    break;

                SingleObjectSpawner spawner = PendingSpawners.Dequeue();
                if (spawner == null)
                    continue;

                spawner.isQueuedForSpawn = false;

                if (!spawner.isActiveAndEnabled)
                    continue;

                spawner.TrySpawnNow();
                processedThisFrame++;
            }

            if (PendingSpawners.Count > 0)
                yield return null;
        }

        isProcessingQueue = false;
    }

    private void TrySpawnNow()
    {

        if (SingleObjectPool.Instance == null)
        {
            // Pool jeszcze się nie zainicjalizował — spróbuj ponownie w kolejnych klatkach.
            if (!isQueuedForSpawn)
            {
                isQueuedForSpawn = true;
                PendingSpawners.Enqueue(this);
            }
            return;
        }

        // Losowa szansa
        if (Random.value > spawnChance) return;

        // Sprawdzenie blockerów (jeśli nie ignorujemy)
        if (!ignoreSpawnBlockers && IsBlocked()) return;

        // Spawn nowego obiektu
        SpawnObject();
    }

    private void ClearChildren()
    {
        // kopiujemy listę, żeby nie leciało "na żywo" po hierarchii
        List<Transform> children = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
            children.Add(transform.GetChild(i));

        foreach (Transform child in children)
        {
            if (child.gameObject.layer != keepLayer)
            {
                if (child.TryGetComponent<PoolableObject>(out var po))
                {
                    po.ReturnToPool();
                }
                else
                {
                    Destroy(child.gameObject); // fallback gdyby coś nie było poolowane
                }
            }
        }
    }

    private bool IsBlocked()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, 0.1f, BlockerHitsBuffer);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = BlockerHitsBuffer[i];
            if (hit.CompareTag(SpawnBlockerTag))
            {
                return true;
            }
        }

        return false;
    }

    private void SpawnObject()
    {
        if (prefabs == null || prefabs.Count == 0)
        {
            Debug.LogWarning($"[SingleObjectSpawner] Brak prefabów przy {gameObject.name}");
            return;
        }

        GameObject prefab = null;
        int attempts = prefabs.Count;
        while (attempts-- > 0 && prefab == null)
            prefab = prefabs[Random.Range(0, prefabs.Count)];

        if (prefab == null)
        {
            Debug.LogWarning($"[SingleObjectSpawner] Wszystkie wpisy prefabów są null przy {gameObject.name}");
            return;
        }

        // pobieramy obiekt z puli i ustawiamy jako dziecko spawnera
        GameObject spawned = SingleObjectPool.Instance.Get(
            prefab,
            transform.position,
            transform.rotation,
            this.transform // <<< WAŻNE – dziecko spawnera
        );

        // resetujemy skalę do tej z prefab’a (ważne np. dla budynków)
        if (spawned != null)
            spawned.transform.localScale = prefab.transform.localScale;
    }
}