using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public enum SurfaceType
{
    OpenSpace,
    Reception,
    MeetingRoom,
    Cafe
}

[System.Serializable]
public class PropGroup
{
    public string groupName;
    public List<GameObject> props;
}

[System.Serializable]
public class SurfacePropSet
{
    public SurfaceType surfaceType;
    public List<PropGroup> propGroups;
}


[RequireComponent(typeof(BoxCollider))]
public class FloorPropSpawner : MonoBehaviour
{
    private const int MaxFloorSpawnersStartedPerFrame = 1;
    private static readonly Queue<FloorPropSpawner> PendingFloorSpawners = new Queue<FloorPropSpawner>();
    private static bool isProcessingFloorQueue;

    [Header("Mo¿liwe typy przestrzeni (checklista)")]
    public List<SurfaceType> allowedSurfaceTypes;

    [Header("PropSet dla ka¿dego typu przestrzeni")]
    public List<SurfacePropSet> surfacePropSets;

    [Header("Propy uniwersalne (np. doniczki)")]
    public List<GameObject> universalProps;

    [Header("Iloœæ propów do wygenerowania")]
    public int minProps = 3;
    public int maxProps = 6;

    [Header("Rozmiar jednej kratki (grid cell)")]
    public float cellSize = 1.0f;

    [Header("Ręcznie przypisane blokery spawnu")]
    public List<BoxCollider> spawnBlockers;

    [Header("Batch spawn")]
    public bool useBatchedSpawn = true;
    public int maxPropsPerFrame = 20;

    private bool[,] grid;
    private Vector3 gridOrigin;
    private List<Bounds> blockerBounds = new List<Bounds>();

    // readonly pola inicjalizowane w InitGrid()
    private int gridSizeX;
    private int gridSizeZ;
    private Coroutine spawnRoutine;
    private bool isQueuedForStart;

    // Mapowanie nazw prefabów na rozmiary, unikamy magicznych liczb
    private readonly Dictionary<string, Vector2Int> prefabSizes = new Dictionary<string, Vector2Int>()
    {
        {"conftable", new Vector2Int(6, 15)},
        {"meetingroom", new Vector2Int(6, 15)},
        {"pot", new Vector2Int(1, 1)},
        // default 3x3 dla innych
    };

    public event System.Action<SurfaceType> OnSurfaceChosen;
    void Start()
    {
        SpawnWorkQueue.Enqueue(this, QueueStart);
    }

    void OnDisable()
    {
        isQueuedForStart = false;
    }

    void QueueStart()
    {
        if (isQueuedForStart)
            return;

        isQueuedForStart = true;
        PendingFloorSpawners.Enqueue(this);

        if (!isProcessingFloorQueue)
        {
            if (SingleObjectPool.Instance != null)
                SingleObjectPool.Instance.StartCoroutine(ProcessFloorQueue());
            else
                StartCoroutine(ProcessFloorQueue());
        }
    }

    static IEnumerator ProcessFloorQueue()
    {
        isProcessingFloorQueue = true;

        while (PendingFloorSpawners.Count > 0)
        {
            int started = 0;

            while (started < MaxFloorSpawnersStartedPerFrame && PendingFloorSpawners.Count > 0)
            {
                FloorPropSpawner spawner = PendingFloorSpawners.Dequeue();
                if (spawner == null)
                    continue;

                spawner.isQueuedForStart = false;

                if (!spawner.isActiveAndEnabled)
                    continue;

                spawner.GenerateProps();
                started++;
            }

            if (PendingFloorSpawners.Count > 0)
                yield return null;
        }

        isProcessingFloorQueue = false;
    }

    public SurfaceType chosenType { get; private set; }

    void GenerateProps()
    {
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        spawnRoutine = StartCoroutine(GeneratePropsRoutine());
    }

    IEnumerator GeneratePropsRoutine()
    {
        if (allowedSurfaceTypes == null || allowedSurfaceTypes.Count == 0)
        {
            Debug.LogWarning("Brak dozwolonych typów przestrzeni.");
            spawnRoutine = null;
            yield break;
        }

        chosenType = allowedSurfaceTypes[Random.Range(0, allowedSurfaceTypes.Count)];
        OnSurfaceChosen?.Invoke(chosenType);
        SurfacePropSet selectedSet = surfacePropSets.Find(s => s.surfaceType == chosenType);

        if (selectedSet == null || selectedSet.propGroups.Count == 0)
        {
            Debug.LogWarning($"Brak propów dla typu przestrzeni: {chosenType}");
            spawnRoutine = null;
            yield break;
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

        InitGrid();

        int totalProps = Random.Range(minProps, maxProps + 1);
        int[] groupDistribution = GetGroupDistribution(chosenType, selectedSet.propGroups.Count, totalProps);
        int spawnedThisFrame = 0;

        for (int g = 0; g < selectedSet.propGroups.Count; g++)
        {
            PropGroup group = selectedSet.propGroups[g];
            if (group.props == null || group.props.Count == 0) continue;

            for (int i = 0; i < groupDistribution[g]; i++)
            {
                GameObject prefab = GetRandomProp(group.props);
                if (prefab == null) continue;

                bool isMainProp = (chosenType == SurfaceType.MeetingRoom && g == 0 && i == 0);
                TryPlaceProp(prefab, isMainProp);
                spawnedThisFrame++;

                if (useBatchedSpawn && spawnedThisFrame >= Mathf.Max(1, maxPropsPerFrame))
                {
                    spawnedThisFrame = 0;
                    yield return null;
                }
            }
        }

        // Uniwersalne propy
        int extraUniversal = Random.Range(0, 3);
        for (int i = 0; i < extraUniversal; i++)
        {
            GameObject uProp = GetRandomProp(universalProps);
            if (uProp != null)
            {
                TryPlaceProp(uProp, false);
                spawnedThisFrame++;

                if (useBatchedSpawn && spawnedThisFrame >= Mathf.Max(1, maxPropsPerFrame))
                {
                    spawnedThisFrame = 0;
                    yield return null;
                }
            }
        }

        spawnRoutine = null;
    }
    void InitGrid()
    {
        BoxCollider area = GetComponent<BoxCollider>();
        Vector3 areaSize = Vector3.Scale(area.size, transform.lossyScale);

        gridSizeX = Mathf.CeilToInt(areaSize.x / cellSize);
        gridSizeZ = Mathf.CeilToInt(areaSize.z / cellSize);
        grid = new bool[gridSizeX, gridSizeZ];

        // Ustawiamy dolny-lewy róg siatki wzglêdem œwiata
        Vector3 localOrigin = area.center - areaSize * 0.5f;
        gridOrigin = transform.TransformPoint(localOrigin);
    }

    // Wyra¿enie lambda dla losowego wyboru propa
    GameObject GetRandomProp(List<GameObject> list) =>
        list != null && list.Count > 0 ? list[Random.Range(0, list.Count)] : null;
    int[] GetGroupDistribution(SurfaceType type, int groupCount, int total)
    {
        int[] result = new int[groupCount];
        if (groupCount == 0 || total == 0) return result;

        if (type == SurfaceType.OpenSpace || type == SurfaceType.Cafe)
        {
            // G³ówna grupa dostaje 80% propów
            int mainCount = Mathf.RoundToInt(total * 0.8f);
            result[0] = mainCount;
            int rem = total - mainCount;

            // Równo dzielimy pozosta³e propy pomiêdzy pozosta³e grupy
            for (int i = 1; i < groupCount; i++)
                result[i] = rem / (groupCount - 1);

            // Dla reszty dodajemy po 1 do pierwszych grup, jeœli zosta³y jakieœ z zaokr¹gleñ
            int assigned = mainCount;
            for (int i = 1; i < groupCount; i++)
                assigned += result[i];

            int leftover = total - assigned;
            for (int i = 1; i < groupCount && leftover > 0; i++, leftover--)
                result[i]++;
        }
        else
        {
            // MeetingRoom, Reception - 1 g³ówny prop, reszta równo podzielona
            result[0] = 1;
            int rem = Mathf.Max(0, total - 1);
            for (int i = 1; i < groupCount; i++)
                result[i] = rem / (groupCount - 1);

            int assigned = 1;
            for (int i = 1; i < groupCount; i++)
                assigned += result[i];

            int leftover = total - assigned;
            for (int i = 1; i < groupCount && leftover > 0; i++, leftover--)
                result[i]++;
        }

        return result;
    }

    void TryPlaceProp(GameObject prefab, bool isMainProp)
    {
        bool forceSpawn = prefab.name.ToLower().Contains("meetingroomtable") || isMainProp;
        PlaceProp(prefab, forceSpawn);
    }
    void PlaceProp(GameObject prefab, bool forceSpawn)
    {
        Vector2Int size = GetColliderBasedSize(prefab);

        int angle = 0;
        Quaternion rotation = Quaternion.identity;

        if (!forceSpawn)
        {
            angle = 90 * Random.Range(0, 4);
            rotation = Quaternion.Euler(0, angle, 0);
            if (angle == 90 || angle == 270)
                size = new Vector2Int(size.y, size.x);
        }

        int maxX = gridSizeX - size.x;
        int maxZ = gridSizeZ - size.y;

        if (maxX < 0 || maxZ < 0)
        {
            Debug.LogWarning($"Prefab {prefab.name} jest za du¿y na grid.");
            return;
        }

        for (int attempts = 0; attempts < 30; attempts++)
        {
            int x = Random.Range(0, maxX + 1);
            int z = Random.Range(0, maxZ + 1);

            if (forceSpawn || CanOccupy(x, z, size))
            {
                Vector3 worldPos = GridToWorld(x, z, size);

                SingleObjectPool.Instance.Get(prefab, worldPos, rotation, transform);

                MarkOccupied(x, z, size);
                break;
            }
        }
    }
    bool CanOccupy(int startX, int startZ, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                int gx = startX + x;
                int gz = startZ + z;

                if (gx >= gridSizeX || gz >= gridSizeZ || grid[gx, gz])
                    return false;

                Vector3 worldPos = GridToWorld(gx, gz, Vector2Int.one);
                Bounds cellBounds = new Bounds(worldPos, new Vector3(cellSize, 1f, cellSize));

                foreach (Bounds b in blockerBounds)
                {
                    if (b.Intersects(cellBounds))
                        return false;
                }
            }
        }

        return true;
    }

    void MarkOccupied(int startX, int startZ, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                int gx = startX + x;
                int gz = startZ + z;
                if (gx < gridSizeX && gz < gridSizeZ)
                {
                    grid[gx, gz] = true;
                }
            }
        }
    }

    Vector3 GridToWorld(int x, int z, Vector2Int size)
    {
        Vector3 localOffset = new Vector3((x + size.x / 2f) * cellSize, 0f, (z + size.y / 2f) * cellSize);
        return gridOrigin + transform.rotation * localOffset;
    }

    Vector2Int GetColliderBasedSize(GameObject prefab)
    {
        string key = prefab.name.ToLower();

        foreach (var pair in prefabSizes)
        {
            if (key.Contains(pair.Key))
                return pair.Value;
        }
        return new Vector2Int(3, 3);
    }
}