using System.Collections.Generic;
using UnityEngine;

public class PotPlantSpawner : MonoBehaviour
{
    [Header("Lista mo¿liwych kwiatków do zrespienia")]
    public List<GameObject> flowerPrefabs;

    void Start()
    {
        SpawnFlower();
    }

    public void SpawnFlower()
    {
        if (flowerPrefabs == null || flowerPrefabs.Count == 0)
        {
            Debug.LogWarning("Brak prefabów w flowerPrefabs!");
            return;
        }

        GameObject prefab = flowerPrefabs[Random.Range(0, flowerPrefabs.Count)];

        GameObject flower = SingleObjectPool.Instance.Get(prefab, transform.position, Quaternion.identity, transform);
        if (flower != null)
            flower.transform.rotation = Quaternion.identity;
    }
}