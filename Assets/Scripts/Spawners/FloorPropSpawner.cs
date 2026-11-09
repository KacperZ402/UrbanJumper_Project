using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FloorPropSpawner : PropSpawnerBase
{
    [Header("Możliwe typy przestrzeni (checklista)")]
    public List<SurfaceType> allowedSurfaceTypes;

    [Header("PropSet dla każdego typu przestrzeni")]
    public List<SurfacePropSet> surfacePropSets;

    [Header("Propy uniwersalne (np. doniczki)")]
    public List<GameObject> universalProps;
    public int maxUniversalProps = 2;

    [Header("Ilość propów do wygenerowania")]
    public int minProps = 3;
    public int maxProps = 6;

    [Header("Siatka")]
    public float cellSize = 1.0f;
    [SerializeField] private int maxPlacementAttempts = 30;

    [Header("Ręcznie przypisane blokery spawnu")]
    public List<BoxCollider> spawnBlockers;

    public event System.Action<SurfaceType> OnSurfaceChosen;
    public SurfaceType chosenType { get; private set; } // nazwa bez zmian, gdyby coś z niej korzystało

    private struct Footprint
    {
        public Vector2Int size;
        public bool alwaysSpawn;
    }

    // Cache danych z prefabów (assetów) – nie trzyma obiektów ze sceny, więc static jest tu bezpieczny
    private static readonly Dictionary<GameObject, Footprint> footprintCache = new Dictionary<GameObject, Footprint>();

    private BoxCollider area;
    private bool[,] grid; // true = zajęte albo zablokowane
    private int gridSizeX;
    private int gridSizeZ;
    private float step;
    private Vector3 gridOrigin;
    private readonly List<Bounds> blockerBounds = new List<Bounds>();

    private void Awake()
    {
        area = GetComponent<BoxCollider>();
    }

    protected override void BuildPlan(List<SpawnRequest> plan)
    {
        if (allowedSurfaceTypes == null || allowedSurfaceTypes.Count == 0)
        {
            Debug.LogWarning("Brak dozwolonych typów przestrzeni.", this);
            return;
        }

        chosenType = allowedSurfaceTypes[Random.Range(0, allowedSurfaceTypes.Count)];
        OnSurfaceChosen?.Invoke(chosenType);

        SurfacePropSet set = FindSet(chosenType);
        if (set == null || set.propGroups == null || set.propGroups.Count == 0)
        {
            Debug.LogWarning($"Brak propów dla typu przestrzeni: {chosenType}", this);
            return;
        }

        InitGrid();

        int total = Random.Range(minProps, maxProps + 1);
        int[] distribution = GetGroupDistribution(chosenType, set.propGroups.Count, total);

        for (int g = 0; g < set.propGroups.Count; g++)
        {
            PropGroup group = set.propGroups[g];
            if (group == null || group.props == null || group.props.Count == 0) continue;

            for (int i = 0; i < distribution[g]; i++)
            {
                GameObject prefab = group.props[Random.Range(0, group.props.Count)];
                if (prefab == null) continue;

                bool isMainProp = chosenType == SurfaceType.MeetingRoom && g == 0 && i == 0;
                TryPlace(prefab, isMainProp, plan);
            }
        }

        if (universalProps != null && universalProps.Count > 0)
        {
            int extra = Random.Range(0, maxUniversalProps + 1);
            for (int i = 0; i < extra; i++)
            {
                GameObject prefab = universalProps[Random.Range(0, universalProps.Count)];
                if (prefab != null) TryPlace(prefab, false, plan);
            }
        }
    }

    private SurfacePropSet FindSet(SurfaceType type)
    {
        if (surfacePropSets == null) return null;

        foreach (SurfacePropSet s in surfacePropSets)
            if (s != null && s.surfaceType == type) return s;

        return null;
    }

    // ---------- Rozmieszczanie ----------

    private void TryPlace(GameObject prefab, bool isMainProp, List<SpawnRequest> plan)
    {
        Footprint fp = GetFootprint(prefab);
        bool force = fp.alwaysSpawn || isMainProp;
        Vector2Int size = fp.size;

        int angle = 0;
        if (!force)
        {
            angle = 90 * Random.Range(0, 4);
            if (angle == 90 || angle == 270)
                size = new Vector2Int(size.y, size.x);
        }

        int maxX = gridSizeX - size.x;
        int maxZ = gridSizeZ - size.y;
        if (maxX < 0 || maxZ < 0)
        {
            Debug.LogWarning($"Prefab {prefab.name} jest za duży na grid.", this);
            return;
        }

        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            int x = Random.Range(0, maxX + 1);
            int z = Random.Range(0, maxZ + 1);
            if (!force && !CanOccupy(x, z, size)) continue;

            // rotacja względem podłogi, żeby zgadzała się z siatką także przy obróconym segmencie
            Quaternion rotation = transform.rotation * Quaternion.Euler(0f, angle, 0f);
            plan.Add(new SpawnRequest(prefab, GridToWorld(x, z, size), rotation));
            MarkOccupied(x, z, size);
            return;
        }
    }

    private static Footprint GetFootprint(GameObject prefab)
    {
        if (footprintCache.TryGetValue(prefab, out Footprint fp))
            return fp;

        //if (prefab.TryGetComponent(out PropFootprint custom))
        //    fp = new Footprint { size = custom.size, alwaysSpawn = custom.alwaysSpawn };
        else
            fp = GetFootprintByName(prefab.name.ToLowerInvariant());

        footprintCache[prefab] = fp;
        return fp;
    }

    // Stare reguły po nazwie – działają, dopóki prefab nie ma komponentu PropFootprint
    private static Footprint GetFootprintByName(string name)
    {
        Footprint fp = new Footprint
        {
            size = new Vector2Int(3, 3),
            alwaysSpawn = name.Contains("meetingroomtable")
        };

        if (name.Contains("conftable") || name.Contains("meetingroom"))
            fp.size = new Vector2Int(6, 15);
        else if (name.Contains("pot"))
            fp.size = new Vector2Int(1, 1);

        return fp;
    }

    private static int[] GetGroupDistribution(SurfaceType type, int groupCount, int total)
    {
        int[] result = new int[groupCount];
        if (groupCount == 0 || total == 0) return result;

        // OpenSpace/Cafe: główna grupa dostaje ~80%; MeetingRoom/Reception: 1 główny prop
        bool mainHeavy = type == SurfaceType.OpenSpace || type == SurfaceType.Cafe;
        result[0] = mainHeavy ? Mathf.RoundToInt(total * 0.8f) : 1;

        int rem = Mathf.Max(0, total - result[0]);
        int assigned = result[0];
        for (int i = 1; i < groupCount; i++)
        {
            result[i] = rem / (groupCount - 1);
            assigned += result[i];
        }

        // reszta z dzielenia – po 1 do kolejnych grup
        int leftover = total - assigned;
        for (int i = 1; i < groupCount && leftover > 0; i++, leftover--)
            result[i]++;

        return result;
    }

    // ---------- Siatka ----------

    private void InitGrid()
    {
        step = Mathf.Max(0.01f, cellSize);
        Vector3 areaSize = Vector3.Scale(area.size, transform.lossyScale);

        gridSizeX = Mathf.Max(0, Mathf.CeilToInt(areaSize.x / step));
        gridSizeZ = Mathf.Max(0, Mathf.CeilToInt(areaSize.z / step));

        if (grid == null || grid.GetLength(0) != gridSizeX || grid.GetLength(1) != gridSizeZ)
            grid = new bool[gridSizeX, gridSizeZ];
        else
            System.Array.Clear(grid, 0, grid.Length);

        // lewy-dolny róg siatki w świecie (skala liczona raz, nie podwójnie)
        gridOrigin = transform.TransformPoint(area.center) - transform.rotation * (areaSize * 0.5f);

        // blokery liczone z transformu i od razu wpisane do siatki
        blockerBounds.Clear();
        if (spawnBlockers != null)
        {
            foreach (BoxCollider col in spawnBlockers)
                if (col != null) blockerBounds.Add(GetWorldBounds(col));
        }

        if (blockerBounds.Count == 0) return;

        Vector3 cellBox = new Vector3(step, 1f, step);
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                Bounds cell = new Bounds(GridToWorld(x, z, Vector2Int.one), cellBox);
                foreach (Bounds b in blockerBounds)
                {
                    if (b.Intersects(cell))
                    {
                        grid[x, z] = true;
                        break;
                    }
                }
            }
        }
    }

    private bool CanOccupy(int startX, int startZ, Vector2Int size)
    {
        for (int x = startX; x < startX + size.x; x++)
            for (int z = startZ; z < startZ + size.y; z++)
                if (x >= gridSizeX || z >= gridSizeZ || grid[x, z]) return false;

        return true;
    }

    private void MarkOccupied(int startX, int startZ, Vector2Int size)
    {
        for (int x = startX; x < startX + size.x && x < gridSizeX; x++)
            for (int z = startZ; z < startZ + size.y && z < gridSizeZ; z++)
                grid[x, z] = true;
    }

    private Vector3 GridToWorld(int x, int z, Vector2Int size)
    {
        Vector3 localOffset = new Vector3((x + size.x * 0.5f) * step, 0f, (z + size.y * 0.5f) * step);
        return gridOrigin + transform.rotation * localOffset;
    }
}