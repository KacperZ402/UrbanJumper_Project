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
    private List<Bounds> blockerBounds = new List<Bounds>();

    [Header("Referencja do FloorSpawnera")]
    public FloorSpawnAreaSpawner floorSpawnerRef;

    [Header("Zestawy propów do œcian")]
    public List<WallSurfacePropSet> wallSurfacePropSets;

    [Header("Maksymalna iloœæ propów przy œcianie")]
    public int maxWallProps = 6;

    [Header("Ustawienia siatki")]
    public float cellWidth = 1f;
    public float cellLength = 1f;

    private List<Vector3> occupiedPositions = new List<Vector3>();

    void Awake()
    {
        if (floorSpawnerRef == null)
        {
            floorSpawnerRef = GetComponentInParent<FloorSpawnAreaSpawner>();
        }

        if (floorSpawnerRef != null)
        {
            floorSpawnerRef.OnSurfaceChosen += HandleSurfaceChosen;
        }
        else
        {
            Debug.LogWarning("Brakuje referencji do FloorSpawnAreaSpawner.");
        }
    }

    void HandleSurfaceChosen(SurfaceType type)
    {
        Debug.Log($"[WallPropSpawner] Otrzymano typ przez event: {type}");

        WallSurfacePropSet set = wallSurfacePropSets.Find(s => s.surfaceType == type);
        if (set == null || set.propGroups.Count == 0)
        {
            Debug.LogWarning($"Brak danych propów œciennych dla: {type}");
            return;
        }

        List<GameObject> wallProps = new List<GameObject>();
        foreach (var group in set.propGroups)
        {
            wallProps.AddRange(group.props);
        }

        SpawnBlocker[] blockers = FindObjectsOfType<SpawnBlocker>();
        foreach (var blocker in blockers)
        {
            BoxCollider col = blocker.GetComponent<BoxCollider>();
            if (col != null)
            {
                blockerBounds.Add(col.bounds);
            }
        }

        SpawnWallProps(wallProps);
    }


    void SpawnWallProps(List<GameObject> wallPrefabs)
    {
        if (wallPrefabs == null || wallPrefabs.Count == 0) return;

        BoxCollider area = GetComponent<BoxCollider>();
        Vector3 areaSize = Vector3.Scale(area.size, transform.lossyScale);

        int xCells = Mathf.FloorToInt(areaSize.x / cellWidth);
        int zCells = Mathf.FloorToInt(areaSize.z / cellLength); // Dodaj w inspektorze `cellDepth` (np. 1f)

        HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();

        int spawned = 0;
        int attempts = 0;

        while (spawned < maxWallProps && attempts < 100)
        {
            int x = Random.Range(0, xCells);
            int z = Random.Range(0, zCells);
            Vector2Int cellIndex = new Vector2Int(x, z);

            if (occupiedCells.Contains(cellIndex))
            {
                attempts++;
                continue;
            }

            float xPos = -areaSize.x / 2f + (x + 0.5f) * cellWidth;
            float zPos = -areaSize.z / 2f + (z + 0.5f) * cellLength;

            Vector3 localSpawnPos = new Vector3(xPos, 0f, zPos);
            Vector3 worldSpawnPos = transform.TransformPoint(area.center + localSpawnPos);

            Bounds cellBounds = new Bounds(worldSpawnPos, new Vector3(cellWidth, 1f, cellLength));
            bool blocked = false;

            foreach (Bounds b in blockerBounds)
            {
                if (b.Intersects(cellBounds))
                {
                    blocked = true;
                    break;
                }
            }

            if (blocked || occupiedCells.Contains(cellIndex))
            {
                attempts++;
                continue;
            }


            GameObject prefab = wallPrefabs[Random.Range(0, wallPrefabs.Count)];
            GameObject spawnedObj = Instantiate(prefab, worldSpawnPos, transform.rotation, transform);
            spawnedObj.SetActive(true);

            occupiedCells.Add(cellIndex);
            spawned++;
            attempts++;
        }
    }
}