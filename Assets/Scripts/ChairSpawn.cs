using UnityEngine;
using System.Collections.Generic;

public class ChairSpawner : MonoBehaviour
{
    [Header("Lista prefabów krzese³")]
    public List<GameObject> chairPrefabs;

    [Header("Warunki kolizji")]
    public LayerMask collisionMask;
    public Vector3 checkBoxSize = new Vector3(0.5f, 0.5f, 0.5f);

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

        if (!IsPositionFree(spawnPos))
        {
            Debug.LogWarning($"Pozycja zajêta, nie mo¿na zespawnowaæ krzes³a przy {gameObject.name}");
            return;
        }

        GameObject prefab = chairPrefabs[Random.Range(0, chairPrefabs.Count)];
        Quaternion rotation = Quaternion.Euler(0f, 90f * Random.Range(0, 4), 0f);

        Instantiate(prefab, spawnPos, rotation, transform);
    }

    bool IsPositionFree(Vector3 point)
    {
        return !Physics.CheckBox(point, checkBoxSize / 2f, Quaternion.identity, collisionMask);
    }
}
