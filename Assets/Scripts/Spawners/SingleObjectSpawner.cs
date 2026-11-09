using System.Collections.Generic;
using UnityEngine;

// Spawnuje jeden losowy prop (np. doniczka, kosz) w swojej pozycji – z szansą i sprawdzeniem blokerów
public class SingleObjectSpawner : PropSpawnerBase
{
    [Header("Lista prefabów")]
    public List<GameObject> prefabs;

    [Header("Szansa na spawn (0–1)")]
    [Range(0f, 1f)]
    public float spawnChance = 1f;

    [Header("Spawn blockery")]
    public bool ignoreSpawnBlockers = false;
    public string spawnBlockerLayerName = "SpawnBlocker";
    [SerializeField] private float blockerCheckRadius = 0.1f;

    private int blockerMask;

    private void Awake()
    {
        int layer = LayerMask.NameToLayer(spawnBlockerLayerName);
        blockerMask = layer >= 0 ? 1 << layer : 0;
    }

    protected override void BuildPlan(List<SpawnRequest> plan)
    {
        if (Random.value > spawnChance) return;
        if (!ignoreSpawnBlockers && IsBlocked()) return;

        GameObject prefab = PickPrefab();
        if (prefab == null) return;

        plan.Add(new SpawnRequest(prefab, transform.position, transform.rotation));
    }

    private bool IsBlocked()
    {
        if (blockerMask == 0) return false;

        // jedno zapytanie tylko po warstwie blokerów – inne collidery nie "zasłonią" blokera
        return Physics.CheckSphere(transform.position, blockerCheckRadius, blockerMask, QueryTriggerInteraction.Collide);
    }

    private GameObject PickPrefab()
    {
        if (prefabs == null || prefabs.Count == 0)
        {
            Debug.LogWarning($"[SingleObjectSpawner] Brak prefabów przy {name}", this);
            return null;
        }

        // kilka prób, gdyby lista miała puste wpisy
        for (int attempt = 0; attempt < prefabs.Count; attempt++)
        {
            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            if (prefab != null) return prefab;
        }

        Debug.LogWarning($"[SingleObjectSpawner] Wszystkie wpisy prefabów są puste przy {name}", this);
        return null;
    }
}