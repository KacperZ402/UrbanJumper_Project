using UnityEngine;
using System.Collections.Generic;

public class SingleObjectSpawner : MonoBehaviour
{
    private static readonly Collider[] BlockerHitsBuffer = new Collider[16];

    [Header("Lista prefabów")]
    public List<GameObject> prefabs;

    [Header("Szansa na respawn (0–1)")]
    [Range(0f, 1f)]
    public float spawnChance = 1f;

    [Header("Ignoruj SpawnBlockery")]
    public bool ignoreSpawnBlockers = false;

    [Header("Spawn blocker layer")]
    public string spawnBlockerLayerName = "SpawnBlocker";

    [Header("Layer, które nie są czyszczone")]
    public string keepLayerName = "Keep";

    [Header("Batch spawn")]
    public bool useBatchedSpawn = true;

    private int keepLayer;
    private int spawnBlockerLayer;

    private void Awake()
    {
        keepLayer = LayerMask.NameToLayer(keepLayerName);
        spawnBlockerLayer = LayerMask.NameToLayer(spawnBlockerLayerName);
    }

    private void OnEnable()
    {
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
            SpawnWorkQueue.Enqueue(this, TrySpawnNow);
            return;
        }

        if (Random.value > spawnChance) return;

        if (!ignoreSpawnBlockers && IsBlocked()) return;

        SpawnObject();
    }

    private void ClearChildren()
    {
        List<Transform> children = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
            children.Add(transform.GetChild(i));

        foreach (Transform child in children)
        {
            if (child.gameObject.layer != keepLayer)
            {
                if (child.TryGetComponent<PoolableObject>(out var po))
                    po.ReturnToPool();
                else
                    Destroy(child.gameObject);
            }
        }
    }

    private bool IsBlocked()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, 0.1f, BlockerHitsBuffer);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = BlockerHitsBuffer[i];
            if (hit != null && hit.gameObject.layer == spawnBlockerLayer)
                return true;
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

        GameObject spawned = SingleObjectPool.Instance.Get(
            prefab,
            transform.position,
            transform.rotation,
            transform
        );

        if (spawned != null)
            spawned.transform.localScale = prefab.transform.localScale;
    }
}