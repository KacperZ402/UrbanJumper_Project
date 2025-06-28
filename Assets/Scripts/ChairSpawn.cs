using UnityEngine;
using System.Collections.Generic;

public class ChairSpawner : MonoBehaviour
{
    [Header("Lista prefabów krzese³")]
    public List<GameObject> chairPrefabs;

    [Header("Obrót obiektu (ustawiany w Inspectorze)")]
    public Vector3 customRotation = Vector3.zero;

    [Header("Warunki kolizji")]
    public LayerMask collisionMask;

    void Start()
    {
        SpawnChair();
    }

    void SpawnChair()
    {
        if (chairPrefabs == null || chairPrefabs.Count == 0)
        {
            Debug.LogWarning($"Brak prefabów w ChairSpawner przy {gameObject.name}");
            return;
        }

        Vector3 spawnPos = transform.position;
        GameObject prefab = chairPrefabs[Random.Range(0, chairPrefabs.Count)];
        Quaternion rotation = Quaternion.Euler(customRotation);

        if (TrySpawnWithoutCollision(prefab, spawnPos, rotation, out GameObject spawned))
        {
            // Sukces
        }
        else
        {
            Debug.LogWarning($"Kolizja – nie mo¿na zespawnowaæ krzes³a przy {gameObject.name}");
        }
    }

    bool TrySpawnWithoutCollision(GameObject prefab, Vector3 position, Quaternion rotation, out GameObject spawnedObj)
    {
        spawnedObj = Instantiate(prefab, position, rotation, transform);
        spawnedObj.SetActive(false); // tymczasowe wy³¹czenie

        bool hasCollision = false;
        foreach (Collider col in spawnedObj.GetComponentsInChildren<Collider>())
        {
            if (!col.enabled || col.isTrigger) continue;

            Collider[] overlaps = Physics.OverlapBox(col.bounds.center, col.bounds.extents, col.transform.rotation, collisionMask);
            foreach (Collider hit in overlaps)
            {
                if (hit.gameObject != spawnedObj && !hit.isTrigger)
                {
                    hasCollision = true;
                    break;
                }
            }

            if (hasCollision) break;
        }

        if (hasCollision)
        {
            Destroy(spawnedObj);
            spawnedObj = null;
            return false;
        }

        spawnedObj.SetActive(true);
        return true;
    }
}