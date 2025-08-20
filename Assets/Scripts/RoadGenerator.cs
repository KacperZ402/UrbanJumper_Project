using System.Collections.Generic;
using UnityEngine;

public class RoadGenerator : MonoBehaviour
{
    [Header("Prefabs (0 = straight, reszta = crossings incl. T)")]
    public List<GameObject> roadPrefabs;

    [Header("Settings")]
    public int maxSegments = 50;

    void Start()
    {
        GenerateRoad(transform.position, 0, 1);
    }

    void GenerateRoad(Vector3 position, int count, int sinceLastCross)
    {
        if (count >= maxSegments) return;

        // Wybór prefabu
        GameObject prefab = ChoosePrefab(sinceLastCross);

        // Spawn
        GameObject obj = Instantiate(prefab, position, Quaternion.identity, transform);
        Transform endPoint = obj.transform.Find("EndPoint");

        // Jeśli T → koniec
        if (roadPrefabs.IndexOf(prefab) == roadPrefabs.Count - 1) return;

        // Ustaw licznik od skrzyżowania
        int nextSince = roadPrefabs.IndexOf(prefab) == 0 ? sinceLastCross + 1 : 0;

        // Spawn next
        if (endPoint != null)
        {
            GenerateRoad(endPoint.position, count + 1, nextSince);
        }
    }

    GameObject ChoosePrefab(int sinceLastCross)
    {
        // po skrzyżowaniu zawsze minimum jedna prosta
        if (sinceLastCross < 1) return roadPrefabs[0];

        return roadPrefabs[Random.Range(0, roadPrefabs.Count)];
    }
}