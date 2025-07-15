using System.Collections.Generic;
using UnityEngine;

public class PotPlantSpawner : MonoBehaviour
{
    [Header("Lista mo¿liwych kwiatków do zrespienia")]
    public List<GameObject> flowerPrefabs;

    private GameObject spawnedFlower;

    void Start()
    {
        SpawnFlower();
    }

    public void SpawnFlower()
    {
        GameObject prefab = flowerPrefabs[Random.Range(0, flowerPrefabs.Count)];
        spawnedFlower = Instantiate(prefab, transform.position, Quaternion.identity, transform);
        spawnedFlower.transform.rotation = Quaternion.identity;
        spawnedFlower.SetActive(true);
    }
}