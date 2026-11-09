using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class WallPropSpawner : PropSpawnerBase
{
    [Header("Referencja do FloorSpawnera")]
    public FloorPropSpawner floorSpawnerRef;

    [Header("Zestawy propów do ścian")]
    public List<WallSurfacePropSet> wallSurfacePropSets;

    [Header("Maksymalna ilość propów przy ścianie")]
    public int maxStandingProps = 4;
    public int maxHangingProps = 2;

    [Header("Ustawienia siatki")]
    public float cellWidth = 1f;
    public float cellLength = 1f;
    [SerializeField] private float hangingHeight = 1.5f;
    [SerializeField] private int maxCellChecks = 100;

    [Header("Ręcznie przypisane blokery spawnu")]
    public List<BoxCollider> spawnBlockers;

    // Ściana nie startuje sama – czeka na typ przestrzeni z podłogi
    protected override bool AutoSpawnOnEnable => false;

    private BoxCollider area;
    private SurfaceType currentSurface;
    private bool hasSurface;
    private bool pendingSpawn;

    private readonly List<Bounds> blockerBounds = new List<Bounds>();
    private readonly List<Vector2Int> cells = new List<Vector2Int>();
    private readonly HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> hangingAllowed = new HashSet<Vector2Int>();
    private readonly List<GameObject> baseProps = new List<GameObject>();

    private void Awake()
    {
        area = GetComponent<BoxCollider>();

        if (floorSpawnerRef == null)
            floorSpawnerRef = GetComponentInParent<FloorPropSpawner>();

        if (floorSpawnerRef != null)
            floorSpawnerRef.OnSurfaceChosen += HandleSurfaceChosen;
    }

    private void OnDestroy()
    {
        if (floorSpawnerRef != null)
            floorSpawnerRef.OnSurfaceChosen -= HandleSurfaceChosen;
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        // podłoga wybrała typ, kiedy ściana była wyłączona – nadrabiamy
        if (pendingSpawn)
            SpawnWorkQueue.Enqueue(this, Spawn);
    }

    private void HandleSurfaceChosen(SurfaceType type)
    {
        currentSurface = type;
        hasSurface = true;
        pendingSpawn = true;

        if (isActiveAndEnabled)
            SpawnWorkQueue.Enqueue(this, Spawn);
    }

    protected override void BuildPlan(List<SpawnRequest> plan)
    {
        pendingSpawn = false;
        if (!hasSurface) return;

        WallSurfacePropSet set = FindSet(currentSurface);
        if (set == null)
        {
            Debug.LogWarning($"Brak danych propów ściennych dla: {currentSurface}", this);
            return;
        }

        CollectBlockers();

        Vector3 areaSize = Vector3.Scale(area.size, transform.lossyScale);
        int xCells = Mathf.FloorToInt(areaSize.x / Mathf.Max(0.01f, cellWidth));
        int zCells = Mathf.FloorToInt(areaSize.z / Mathf.Max(0.01f, cellLength));

        occupied.Clear();
        hangingAllowed.Clear();

        // FAZA 1: propy stojące (w tym te, nad którymi można coś powiesić)
        baseProps.Clear();
        if (set.standingProps != null) baseProps.AddRange(set.standingProps);
        if (set.standingWithHangingAllowed != null) baseProps.AddRange(set.standingWithHangingAllowed);

        if (baseProps.Count > 0)
        {
            FillShuffledCells(xCells, zCells);
            int spawned = 0;
            int checks = Mathf.Min(cells.Count, maxCellChecks);

            for (int i = 0; i < checks && spawned < maxStandingProps; i++)
            {
                Vector2Int cell = cells[i];
                Vector3 pos = CellToWorld(areaSize, cell);
                if (IsBlocked(pos)) continue;

                GameObject prefab = baseProps[Random.Range(0, baseProps.Count)];
                if (prefab == null) continue;

                plan.Add(new SpawnRequest(prefab, pos, transform.rotation));
                occupied.Add(cell);

                if (set.standingWithHangingAllowed != null && set.standingWithHangingAllowed.Contains(prefab))
                    hangingAllowed.Add(cell);

                spawned++;
            }
        }

        // FAZA 2: propy wiszące – w pustych komórkach albo nad propami, które na to pozwalają
        List<GameObject> hanging = set.hangingProps;
        if (hanging != null && hanging.Count > 0)
        {
            FillShuffledCells(xCells, zCells);
            int spawned = 0;
            int checks = Mathf.Min(cells.Count, maxCellChecks);

            for (int i = 0; i < checks && spawned < maxHangingProps; i++)
            {
                Vector2Int cell = cells[i];
                if (occupied.Contains(cell) && !hangingAllowed.Contains(cell)) continue;

                Vector3 pos = CellToWorld(areaSize, cell) + Vector3.up * hangingHeight;
                if (IsBlocked(pos)) continue;

                GameObject prefab = hanging[Random.Range(0, hanging.Count)];
                if (prefab == null) continue;

                plan.Add(new SpawnRequest(prefab, pos, transform.rotation));
                spawned++;
            }
        }
    }

    private WallSurfacePropSet FindSet(SurfaceType type)
    {
        if (wallSurfacePropSets == null) return null;

        foreach (WallSurfacePropSet s in wallSurfacePropSets)
            if (s != null && s.surfaceType == type) return s;

        return null;
    }

    private void CollectBlockers()
    {
        blockerBounds.Clear();
        if (spawnBlockers == null) return;

        foreach (BoxCollider col in spawnBlockers)
            if (col != null) blockerBounds.Add(GetWorldBounds(col));
    }

    private bool IsBlocked(Vector3 cellPos)
    {
        Bounds cell = new Bounds(cellPos, new Vector3(cellWidth, 1f, cellLength));
        foreach (Bounds b in blockerBounds)
            if (b.Intersects(cell)) return true;

        return false;
    }

    private void FillShuffledCells(int xCells, int zCells)
    {
        cells.Clear();
        for (int x = 0; x < xCells; x++)
            for (int z = 0; z < zCells; z++)
                cells.Add(new Vector2Int(x, z));

        for (int i = cells.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector2Int tmp = cells[i];
            cells[i] = cells[j];
            cells[j] = tmp;
        }
    }

    // Środek komórki w świecie (skala liczona raz, nie podwójnie)
    private Vector3 CellToWorld(Vector3 areaSize, Vector2Int cell)
    {
        float x = -areaSize.x * 0.5f + (cell.x + 0.5f) * cellWidth;
        float z = -areaSize.z * 0.5f + (cell.y + 0.5f) * cellLength;
        return transform.TransformPoint(area.center) + transform.rotation * new Vector3(x, 0f, z);
    }
}