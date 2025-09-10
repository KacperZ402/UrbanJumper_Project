using UnityEngine;
using System.Collections.Generic;

public class SingleObjectSpawner : MonoBehaviour
{
    [Header("Lista prefabów")]
    public List<GameObject> prefabs;

    [Header("Szansa na respawn (0–1)")]
    [Range(0f, 1f)]
    public float spawnChance = 1f;

    [Header("Ignoruj SpawnBlockery")]
    public bool ignoreSpawnBlockers = false;

    void OnEnable()
    {
        // Losowa szansa
        if (Random.value > spawnChance) return;

        // Sprawdzenie blockerów (jeśli nie ignorujemy)
        if (!ignoreSpawnBlockers && IsBlocked()) return;

        SpawnObject();
    }

    bool IsBlocked()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.1f);

        foreach (var hit in hits)
        {
            if (hit.GetComponent<SpawnBlocker>() != null)
            {
                return true;
            }
        }

        return false;
    }

    void SpawnObject()
    {
        if (prefabs == null || prefabs.Count == 0)
        {
            Debug.LogWarning($"[SingleObjectSpawner] Brak prefabów przy {gameObject.name}");
            return;
        }

        GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
        Quaternion spawnRotation = transform.rotation;
        Vector3 spawnPosition = transform.position;
        Transform parent = transform.parent;

        GameObject spawned = SingleObjectPool.Instance.Get(prefab, spawnPosition, spawnRotation, parent);
        spawned.SetActive(true);
    }
}