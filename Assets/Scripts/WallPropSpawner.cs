using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    private class WallCellPlan
    {
        public List<Vector2Int> standingCells;
        public List<Vector2Int> hangingCells;
    }

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

    [Header("Ręcznie przypisane blokery spawnu")]
    public List<BoxCollider> spawnBlockers;

    [Header("Batch spawn")]
    public bool useBatchedSpawn = true;
    public int maxOpsPerFrame = 20;

    private Coroutine spawnRoutine;
    private bool hasPendingSurface;
    private SurfaceType pendingSurface;

    void OnEnable()
    {
        if (hasPendingSurface)
        {
            hasPendingSurface = false;
            HandleSurfaceChosen(pendingSurface);
        }
    }

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
        if (!isActiveAndEnabled)
        {
            pendingSurface = type;
            hasPendingSurface = true;
            return;
        }

        Debug.Log($"[WallPropSpawner] Otrzymano typ przez event: {type}");

        WallSurfacePropSet set = wallSurfacePropSets.Find(s => s.surfaceType == type);
        if (set == null)
        {
            Debug.LogWarning($"Brak danych propów œciennych dla: {type}");
            return;
        }

        blockerBounds.Clear();
        if (spawnBlockers != null)
        {
            foreach (var col in spawnBlockers)
            {
                if (col != null)
                {
                    blockerBounds.Add(col.bounds);
                }
            }
        }

        WallSurfacePropSet targetSet = set;
        SpawnWorkQueue.Enqueue(this, () => StartSpawnRoutine(targetSet));
    }

    void StartSpawnRoutine(WallSurfacePropSet set)
    {
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        spawnRoutine = StartCoroutine(SpawnWallPropsRoutine(set));
    }

    IEnumerator SpawnWallPropsRoutine(WallSurfacePropSet set)
    {
        if (set == null)
        {
            spawnRoutine = null;
            yield break;
        }

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
        int opsThisFrame = 0;

        int seed = Random.Range(int.MinValue, int.MaxValue);
        Task<WallCellPlan> planTask = Task.Run(() => BuildCellPlan(xCells, zCells, seed));
        while (!planTask.IsCompleted)
            yield return null;

        WallCellPlan plan = (!planTask.IsFaulted && !planTask.IsCanceled) ? planTask.Result : null;
        if (plan == null)
        {
            spawnRoutine = null;
            yield break;
        }

        // --- FAZA 1: Spawnowanie "standing" i "standingWithHangingAllowed"
        List<GameObject> baseProps = new List<GameObject>();
        baseProps.AddRange(standing);
        baseProps.AddRange(standingWithHanging);

        int standingChecks = 0;
        foreach (Vector2Int cellIndex in plan.standingCells)
        {
            if (spawned >= maxStandingProps || standingChecks >= 100)
                break;

            int x = cellIndex.x;
            int z = cellIndex.y;
            standingChecks++;

            if (occupiedCells.Contains(cellIndex))
            {
                opsThisFrame++;

                if (useBatchedSpawn && opsThisFrame >= Mathf.Max(1, maxOpsPerFrame))
                {
                    opsThisFrame = 0;
                    yield return null;
                }
                continue;
            }

            Vector3 worldSpawnPos = GetWorldPosition(area, areaSize, x, z);
            Bounds cellBounds = new Bounds(worldSpawnPos, new Vector3(cellWidth, 1f, cellLength));

            if (IsBlocked(cellBounds))
            {
                opsThisFrame++;

                if (useBatchedSpawn && opsThisFrame >= Mathf.Max(1, maxOpsPerFrame))
                {
                    opsThisFrame = 0;
                    yield return null;
                }
                continue;
            }

            GameObject prefab = baseProps[Random.Range(0, baseProps.Count)];
            SingleObjectPool.Instance.Get(prefab, worldSpawnPos, transform.rotation, transform);


            occupiedCells.Add(cellIndex);
            if (standingWithHanging.Contains(prefab))
            {
                hangingAllowedCells.Add(cellIndex); // Zaznacz, e mona nad tym zawiesiæ
            }

            spawned++;
            opsThisFrame++;

            if (useBatchedSpawn && opsThisFrame >= Mathf.Max(1, maxOpsPerFrame))
            {
                opsThisFrame = 0;
                yield return null;
            }
        }

        // --- FAZA 2: Spawnowanie HangingProps
        HashSet<Vector2Int> hangingOccupiedCells = new HashSet<Vector2Int>();

        int hangingSpawned = 0;
        int hangingChecks = 0;
        foreach (Vector2Int cellIndex in plan.hangingCells)
        {
            if (hangingSpawned >= maxHangingProps || hangingChecks >= 100)
                break;

            int x = cellIndex.x;
            int z = cellIndex.y;
            hangingChecks++;

            bool isEmpty = !occupiedCells.Contains(cellIndex) && !hangingOccupiedCells.Contains(cellIndex);
            bool isAboveAllowed = hangingAllowedCells.Contains(cellIndex) && !hangingOccupiedCells.Contains(cellIndex);

            if (!isEmpty && !isAboveAllowed)
            {
                opsThisFrame++;

                if (useBatchedSpawn && opsThisFrame >= Mathf.Max(1, maxOpsPerFrame))
                {
                    opsThisFrame = 0;
                    yield return null;
                }
                continue;
            }

            Vector3 baseWorldPos = GetWorldPosition(area, areaSize, x, z);
            Vector3 hangingPos = baseWorldPos + Vector3.up * 1.5f;

            Bounds hangingBounds = new Bounds(hangingPos, new Vector3(cellWidth, 1f, cellLength));
            if (IsBlocked(hangingBounds))
            {
                opsThisFrame++;

                if (useBatchedSpawn && opsThisFrame >= Mathf.Max(1, maxOpsPerFrame))
                {
                    opsThisFrame = 0;
                    yield return null;
                }
                continue;
            }

            GameObject hangingPrefab = hanging[Random.Range(0, hanging.Count)];
            SingleObjectPool.Instance.Get(hangingPrefab, hangingPos, transform.rotation, transform);


            hangingOccupiedCells.Add(cellIndex); // Zaznacz, e ta komórka ma hanging propa
            hangingSpawned++;
            opsThisFrame++;

            if (useBatchedSpawn && opsThisFrame >= Mathf.Max(1, maxOpsPerFrame))
            {
                opsThisFrame = 0;
                yield return null;
            }
        }

        spawnRoutine = null;
    }

    private static WallCellPlan BuildCellPlan(int xCells, int zCells, int seed)
    {
        WallCellPlan plan = new WallCellPlan
        {
            standingCells = new List<Vector2Int>(xCells * zCells),
            hangingCells = new List<Vector2Int>(xCells * zCells)
        };

        for (int x = 0; x < xCells; x++)
        {
            for (int z = 0; z < zCells; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);
                plan.standingCells.Add(cell);
                plan.hangingCells.Add(cell);
            }
        }

        System.Random random = new System.Random(seed);
        Shuffle(plan.standingCells, random);
        Shuffle(plan.hangingCells, random);

        return plan;
    }

    private static void Shuffle<T>(List<T> list, System.Random random)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            T tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
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