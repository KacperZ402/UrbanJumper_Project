using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WallPropGroup
{
    public string groupName;
    public List<GameObject> props;
}

[System.Serializable]
public class WallSurfacePropSet
{
    public SurfaceType surfaceType;
    public List<WallPropGroup> propGroups;
}

[RequireComponent(typeof(BoxCollider))]
public class WallFloorPropSpawner : MonoBehaviour
{
    [Header("Referencja do FloorSpawnera")]
    public FloorSpawnAreaSpawner floorSpawnerRef;

    [Header("Zestawy propów do œcian")]
    public List<WallSurfacePropSet> wallSurfacePropSets;

    [Header("Maksymalna iloœæ propów przy œcianie")]
    public int maxWallProps = 4;

    void Start()
    {
        if (floorSpawnerRef == null)
        {
            Debug.LogError("Brakuje referencji do FloorSpawnAreaSpawner.");
            return;
        }

        SurfaceType surfaceType = floorSpawnerRef.chosenType;

        WallSurfacePropSet set = wallSurfacePropSets.Find(s => s.surfaceType == surfaceType);
        if (set == null || set.propGroups.Count == 0)
        {
            Debug.LogWarning($"Brak danych propów œciennych dla: {surfaceType}");
            return;
        }

        // Zak³adamy, ¿e ostatnia grupa to propsy przyœcienne (opcjonalnie mo¿na to zmieniæ)
        List<GameObject> wallProps = set.propGroups[set.propGroups.Count - 1].props;
        SpawnWallProps(wallProps);
    }

    void SpawnWallProps(List<GameObject> wallPrefabs)
    {
        if (wallPrefabs == null || wallPrefabs.Count == 0) return;

        BoxCollider area = GetComponent<BoxCollider>();
        if (area == null)
        {
            Debug.LogError("Brakuje BoxCollidera.");
            return;
        }

        Vector3 areaSize = Vector3.Scale(area.size, transform.lossyScale);

        int count = Mathf.Min(maxWallProps, wallPrefabs.Count);
        float spacing = areaSize.x / (count + 1); // Rozstaw propów

        for (int i = 1; i <= count; i++)
        {
            GameObject prefab = wallPrefabs[Random.Range(0, wallPrefabs.Count)];
            if (prefab == null) continue;

            // Pozycja przy jednej z d³u¿szych krawêdzi boxa
            Vector3 localOffset = new Vector3(-areaSize.x / 2f + spacing * i, 0, -areaSize.z / 2f + 0.5f);
            Vector3 spawnPos = transform.TransformPoint(area.center + localOffset);

            GameObject spawned = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
            spawned.SetActive(true);
        }
    }
}