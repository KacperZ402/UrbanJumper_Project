using System.Collections.Generic;
using UnityEngine;

public class PotPlantSpawner : MonoBehaviour
{
    [Header("Lista mo¿liwych kwiatków do zrespienia")]
    public List<GameObject> flowerPrefabs;

    private GameObject spawnedFlower;

    void OnEnable()
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

        spawnedFlower = SingleObjectPool.Instance.Get(prefab, transform.position, Quaternion.identity, transform);

        spawnedFlower.transform.rotation = Quaternion.identity;
        spawnedFlower.SetActive(true);
    }
}