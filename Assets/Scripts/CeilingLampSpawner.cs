using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class CeilingLampSpawner : MonoBehaviour
{
    [Header("Lista prefabów lamp (jeden typ wybierany losowo)")]
    public List<GameObject> lampPrefabs;

    [Header("Rozmiar siatki")]
    [Tooltip("Szerokoœæ i g³êbokoœæ jednej kratki w metrach (X = szerokoœæ, Y = g³êbokoœæ)")]
    public Vector2 cellSize = new Vector2(2f, 2f);  // <-- kontrolowane z inspektora

    private void Start()
    {
        SpawnCeilingLamps();
    }

    void SpawnCeilingLamps()
    {
        if (lampPrefabs == null || lampPrefabs.Count == 0)
        {
            Debug.LogWarning("Brak prefabów lamp.");
            return;
        }

        GameObject chosenLamp = lampPrefabs[Random.Range(0, lampPrefabs.Count)];

        BoxCollider area = GetComponent<BoxCollider>();
        Vector3 areaSize = Vector3.Scale(area.size, transform.lossyScale);
        Vector3 center = transform.TransformPoint(area.center);

        float startX = center.x - areaSize.x / 2f;
        float startZ = center.z - areaSize.z / 2f;
        float y = center.y + areaSize.y / 2f; // sufit

        int gridCountX = Mathf.FloorToInt(areaSize.x / cellSize.x);
        int gridCountZ = Mathf.FloorToInt(areaSize.z / cellSize.y);

        for (int x = 0; x < gridCountX; x++)
        {
            for (int z = 0; z < gridCountZ; z++)
            {
                float posX = startX + (x + 0.5f) * cellSize.x;
                float posZ = startZ + (z + 0.5f) * cellSize.y;

                Vector3 spawnPos = new Vector3(posX, y, posZ);

                GameObject lamp = Instantiate(chosenLamp, spawnPos, chosenLamp.transform.rotation, transform);
                lamp.transform.localScale = Vector3.one;
                lamp.SetActive(true);
            }
        }
    }
}
