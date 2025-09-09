using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WallSurfacePropSet
{
    public SurfaceType surfaceType;

    [Header("Propy stoj¹ce przy œcianie")]
    public List<GameObject> standingProps;

    [Header("Propy wisz¹ce")]
    public List<GameObject> hangingProps;

    [Header("Propy stoj¹ce z mo¿liwoœci¹ zawieszenia nad nimi")]
    public List<GameObject> standingWithHangingAllowed;
}

[RequireComponent(typeof(BoxCollider))]
public class WallPropSpawner : MonoBehaviour
{
    private List<Bounds> blockerBounds = new List<Bounds>();

    [Header("Referencja do FloorSpawnera")]
    public FloorPropSpawner floorSpawnerRef;

    [Header("Zestawy propów do œcian")]
    public List<WallSurfacePropSet> wallSurfacePropSets;

    [Header("Maksymalna iloœæ propów przy œcianie")]
    public int maxStandingProps = 4;
    public int maxHangingProps = 2;

    [Header("Ustawienia siatki")]
    public float cellWidth = 1f;
    public float cellLength = 1f;

    private List<Vector3> occupiedPositions = new List<Vector3>();

    void Awake()
    {
        if (floorSpawnerRef == null)
        {
            floorSpawnerRef = GetComponentInParent<FloorPropSpawner>();
        }

        if (floorSpawnerRef != null)
        {
            floorSpawnerRef.OnSurfaceChosen += HandleSurfaceChosen;
        }
        else
        {
            Debug.LogWarning("Brakuje referencji do FloorPropSpawner.");
        }
    }

    void HandleSurfaceChosen(SurfaceType type)
    {
        Debug.Log($"[WallPropSpawner] Otrzymano typ przez event: {type}");

        WallSurfacePropSet set = wallSurfacePropSets.Find(s => s.surfaceType == type);
        if (set == null)
        {
            Debug.LogWarning($"Brak danych propów œciennych dla: {type}");
            return;
        }

        blockerBounds.Clear();
        SpawnBlocker[] blockers = FindObjectsOfType<SpawnBlocker>();
        foreach (var blocker in blockers)
        {
            BoxCollider col = blocker.GetComponent<BoxCollider>();
            if (col != null)
            {
                blockerBounds.Add(col.bounds);
            }
        }

        // £¹czymy wszystkie typy propów w jedn¹ listê
        List<GameObject> allProps = new List<GameObject>();
        allProps.AddRange(set.standingProps);
        allProps.AddRange(set.hangingProps);
        allProps.AddRange(set.standingWithHangingAllowed);

        SpawnWallProps(set); ;
    }

    void SpawnWallProps(WallSurfacePropSet set)
    {
        if (set == null) return;

        // Podzia³ na grupy prefabów
        List<GameObject> standing = set.standingProps;
        List<GameObject> hanging = set.hangingProps;
        List<GameObject> standingWithHanging = set.standingWithHangingAllowed;

        BoxCollider area = GetComponent<BoxCollider>();
        Vector3 areaSize = Vector3.Scale(area.size, transform.lossyScale);
        int xCells = Mathf.FloorToInt(areaSize.x / cellWidth);
        int zCells = Mathf.FloorToInt(areaSize.z / cellLength);

        HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
        HashSet<Vector2Int> hangingAllowedCells = new HashSet<Vector2Int>();

        int spawned = 0;
        int attempts = 0;

        // --- FAZA 1: Spawnowanie "standing" i "standingWithHangingAllowed"
        List<GameObject> baseProps = new List<GameObject>();
        baseProps.AddRange(standing);
        baseProps.AddRange(standingWithHanging);

        while (spawned < maxStandingProps && attempts < 100)
        {
            int x = Random.Range(0, xCells);
            int z = Random.Range(0, zCells);
            Vector2Int cellIndex = new Vector2Int(x, z);

            if (occupiedCells.Contains(cellIndex))
            {
                attempts++;
                continue;
            }

            Vector3 worldSpawnPos = GetWorldPosition(area, areaSize, x, z);
            Bounds cellBounds = new Bounds(worldSpawnPos, new Vector3(cellWidth, 1f, cellLength));

            if (IsBlocked(cellBounds))
            {
                attempts++;
                continue;
            }

            GameObject prefab = baseProps[Random.Range(0, baseProps.Count)];
            GameObject obj = SingleObjectPool.Instance.Get(prefab, worldSpawnPos, transform.rotation, transform);


            occupiedCells.Add(cellIndex);
            if (standingWithHanging.Contains(prefab))
            {
                hangingAllowedCells.Add(cellIndex); // Zaznacz, ¿e mo¿na nad tym zawiesiæ
            }

            spawned++;
            attempts++;
        }

        // --- FAZA 2: Spawnowanie HangingProps
        HashSet<Vector2Int> hangingOccupiedCells = new HashSet<Vector2Int>();

        int hangingAttempts = 0;
        int hangingSpawned = 0;
        while (hangingSpawned < maxHangingProps && hangingAttempts < 100)
        {
            int x = Random.Range(0, xCells);
            int z = Random.Range(0, zCells);
            Vector2Int cellIndex = new Vector2Int(x, z);

            bool isEmpty = !occupiedCells.Contains(cellIndex) && !hangingOccupiedCells.Contains(cellIndex);
            bool isAboveAllowed = hangingAllowedCells.Contains(cellIndex) && !hangingOccupiedCells.Contains(cellIndex);

            if (!isEmpty && !isAboveAllowed)
            {
                hangingAttempts++;
                continue;
            }

            Vector3 baseWorldPos = GetWorldPosition(area, areaSize, x, z);
            Vector3 hangingPos = baseWorldPos + Vector3.up * 1.5f;

            Bounds hangingBounds = new Bounds(hangingPos, new Vector3(cellWidth, 1f, cellLength));
            if (IsBlocked(hangingBounds))
            {
                hangingAttempts++;
                continue;
            }

            GameObject hangingPrefab = hanging[Random.Range(0, hanging.Count)];
            GameObject obj = SingleObjectPool.Instance.Get(hangingPrefab, hangingPos, transform.rotation, transform);


            hangingOccupiedCells.Add(cellIndex); // Zaznacz, ¿e ta komórka ma hanging propa
            hangingSpawned++;
            hangingAttempts++;
        }
    }
    Vector3 GetWorldPosition(BoxCollider area, Vector3 areaSize, int x, int z)
    {
        float xPos = -areaSize.x / 2f + (x + 0.5f) * cellWidth;
        float zPos = -areaSize.z / 2f + (z + 0.5f) * cellLength;
        Vector3 localSpawnPos = new Vector3(xPos, 0f, zPos);
        return transform.TransformPoint(area.center + localSpawnPos);
    }
    bool IsBlocked(Bounds testBounds)
    {
        foreach (Bounds b in blockerBounds)
        {
            if (b.Intersects(testBounds)) return true;
        }
        return false;
    }
}