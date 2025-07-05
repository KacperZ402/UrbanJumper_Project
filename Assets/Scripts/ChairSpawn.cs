using UnityEngine;
using System.Collections.Generic;

public class ChairSpawner : MonoBehaviour
{
    [Header("Lista prefabów krzese³")]
    public List<GameObject> chairPrefabs;

    [Header("Maska kolizji – np. Default, Environment itp.")]
    public LayerMask collisionMask;

    void Start()
    {
        SpawnChair();
    }

    void SpawnChair()
    {
        if (chairPrefabs == null || chairPrefabs.Count == 0)
        {
            Debug.LogWarning($"[ChairSpawner] Brak prefabów krzese³ przy {gameObject.name}");
            return;
        }

        GameObject prefab = chairPrefabs[Random.Range(0, chairPrefabs.Count)];
        Quaternion spawnRotation = transform.rotation;      //  U¿yj dok³adnej rotacji pustego GameObjectu
        Vector3 spawnPosition = transform.position;

        if (!CanSpawnWithoutCollision(prefab, spawnPosition, spawnRotation))
        {
            Debug.LogWarning($"[ChairSpawner] Kolizja – nie mo¿na zespawnowaæ krzes³a przy {gameObject.name}");
            return;
        }

        GameObject spawned = Instantiate(prefab, spawnPosition, spawnRotation, transform);
        spawned.SetActive(true);
    }

    bool CanSpawnWithoutCollision(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject temp = Instantiate(prefab, position, rotation);
        temp.SetActive(false);

        BoxCollider box = temp.GetComponentInChildren<BoxCollider>();
        if (box == null)
        {
            Debug.LogWarning($"[ChairSpawner] Brak BoxCollidera w prefabie: {prefab.name}");
            Destroy(temp);
            return true;
        }

        Vector3 center = box.bounds.center;
        Vector3 halfExtents = box.bounds.extents;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation, collisionMask);

        foreach (var hit in hits)
        {
            if (!hit.isTrigger && !hit.transform.IsChildOf(temp.transform))
            {
                Destroy(temp);
                return false;
            }
        }

        Destroy(temp);
        return true;
    }
}