using System.Collections.Generic;
using UnityEngine;

public class TablePropSpawner : PropSpawnerBase
{
    public enum TableType { CaffeTable, Desk, Reception, Conference }

    [Header("General Settings")]
    public TableType tableType;
    public bool allowDuplicateProps = true;
    public bool allowSpawnOnFullSurface = true;
    public float minDistanceBetweenProps = 0.1f;
    public int maxAttemptsPerProp = 100;
    public int propCount = 10;

    [Header("Prop Lists")]
    public List<GameObject> randomProps;
    public List<Transform> staticPropPoints;
    public List<GameObject> staticProps;

    [Header("Spawn Areas")]
    public List<BoxCollider> spawnAreas;
    public BoxCollider ownCollider;
    [Tooltip("Margines od krawêdzi okr¹g³ego stolika (CaffeTable)")]
    public float circleEdgeMargin = 1f;

    // pozycje zajête w bie¿¹cym planie (statyczne + losowe)
    private readonly List<Vector3> placed = new List<Vector3>();
    // powierzchnie do losowania (bez pustych wpisów)
    private readonly List<BoxCollider> validAreas = new List<BoxCollider>();
    // pula bez powtórzeñ (gdy allowDuplicateProps = false)
    private readonly List<GameObject> availablePrefabs = new List<GameObject>();

    protected override void BuildPlan(List<SpawnRequest> plan)
    {
        placed.Clear();
        availablePrefabs.Clear();
        CollectAreas();

        AddStaticProps(plan);
        AddRandomProps(plan);
    }

    private void CollectAreas()
    {
        validAreas.Clear();

        if (allowSpawnOnFullSurface && spawnAreas != null)
        {
            foreach (BoxCollider area in spawnAreas)
                if (area != null) validAreas.Add(area);
        }

        if (validAreas.Count == 0 && ownCollider != null)
            validAreas.Add(ownCollider);
    }

    private void AddStaticProps(List<SpawnRequest> plan)
    {
        if (staticPropPoints == null || staticProps == null) return;

        int count = Mathf.Min(staticPropPoints.Count, staticProps.Count);
        for (int i = 0; i < count; i++)
        {
            Transform point = staticPropPoints[i];
            GameObject prefab = staticProps[i];
            if (point == null || prefab == null || placed.Contains(point.position)) continue;

            plan.Add(new SpawnRequest(prefab, point.position, point.rotation));
            placed.Add(point.position);
        }
    }

    private void AddRandomProps(List<SpawnRequest> plan)
    {
        if (randomProps == null || randomProps.Count == 0) return;

        for (int i = 0; i < propCount; i++)
        {
            for (int attempt = 0; attempt < maxAttemptsPerProp; attempt++)
            {
                // brak jakiejkolwiek powierzchni – nie ma sensu próbowaæ dalej
                if (!TryGetRandomPosition(out Vector3 pos)) return;
                if (!IsFarEnough(pos)) continue;

                GameObject prefab = GetRandomProp();
                if (prefab != null)
                    plan.Add(new SpawnRequest(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)));

                placed.Add(pos);
                break;
            }
        }
    }

    private bool TryGetRandomPosition(out Vector3 pos)
    {
        pos = default;

        if (tableType == TableType.CaffeTable)
        {
            if (ownCollider == null) return false;

            Transform t = ownCollider.transform;
            Vector3 size = Vector3.Scale(ownCollider.size, t.lossyScale);
            float half = Mathf.Min(Mathf.Abs(size.x), Mathf.Abs(size.z)) * 0.5f;

            // margines nie mo¿e zjeœæ wiêcej ni¿ po³owy promienia – inaczej wszystko l¹duje w œrodku
            float radius = half - Mathf.Min(circleEdgeMargin, half * 0.5f);

            Vector3 top = t.TransformPoint(ownCollider.center + Vector3.up * ownCollider.size.y * 0.5f);
            Vector2 p = Random.insideUnitCircle * radius;
            pos = new Vector3(top.x + p.x, top.y, top.z + p.y);
            return true;
        }

        // Desk / Reception / Conference – losowy punkt na górnej œciance strefy
        if (validAreas.Count == 0) return false;

        pos = RandomPointOnTop(validAreas[Random.Range(0, validAreas.Count)]);
        return true;
    }

    private bool IsFarEnough(Vector3 candidate)
    {
        float minSq = minDistanceBetweenProps * minDistanceBetweenProps;

        for (int i = 0; i < placed.Count; i++)
        {
            float dx = candidate.x - placed[i].x;
            float dz = candidate.z - placed[i].z;
            if (dx * dx + dz * dz < minSq) return false;
        }
        return true;
    }

    private GameObject GetRandomProp()
    {
        if (allowDuplicateProps)
            return randomProps[Random.Range(0, randomProps.Count)];

        if (availablePrefabs.Count == 0)
            availablePrefabs.AddRange(randomProps);

        int index = Random.Range(0, availablePrefabs.Count);
        GameObject chosen = availablePrefabs[index];
        availablePrefabs.RemoveAt(index);
        return chosen;
    }
}