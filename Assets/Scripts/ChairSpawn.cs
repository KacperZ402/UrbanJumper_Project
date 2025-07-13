using UnityEngine;
using System.Collections.Generic;

public class ChairSpawner : MonoBehaviour
{
    [Header("Lista prefabów krzese³")]
    public List<GameObject> chairPrefabs;

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

        GameObject spawned = Instantiate(prefab, spawnPosition, spawnRotation, transform);
        spawned.SetActive(true);
    }
}
