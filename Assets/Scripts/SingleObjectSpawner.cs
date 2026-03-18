using UnityEngine;
using System.Collections.Generic;

public class SingleObjectSpawner : MonoBehaviour
{
    private const string SpawnBlockerTag = "SpawnBlocker";
    private static readonly Collider[] BlockerHitsBuffer = new Collider[16];

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
            SpawnWorkQueue.Enqueue(this, TrySpawnNow);
            return;
        }

        TrySpawnNow();
    }

    private void TrySpawnNow()
    {

        if (SingleObjectPool.Instance == null)
        {
            // Pool jeszcze się nie zainicjalizował — spróbuj ponownie przez globalną kolejkę.
            SpawnWorkQueue.Enqueue(this, TrySpawnNow);
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