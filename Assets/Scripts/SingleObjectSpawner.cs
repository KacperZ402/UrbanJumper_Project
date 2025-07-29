using UnityEngine;
using System.Collections.Generic;

public class SingleObjectSpawner : MonoBehaviour
{
    [Header("Lista prefabów")]
    public List<GameObject> prefabs;

    [Header("Szansa na respawn (0–1)")]
    [Range(0f, 1f)]
    public float spawnChance = 1f;

    void Start()
    {
        if (Random.value > spawnChance)
        {
            return;
        }
        SpawnObject();
        Destroy(transform.gameObject);
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

        // Ustaw rodzica na tego samego, co obiekt z tym skryptem
        Transform parent = transform.parent;

        GameObject spawned = Instantiate(prefab, spawnPosition, spawnRotation, parent);
        spawned.SetActive(true);
    }
}