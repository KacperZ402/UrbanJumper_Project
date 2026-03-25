using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class TablePropSpawner : MonoBehaviour
{
    private struct AreaData
    {
        public float minX;
        public float maxX;
        public float minZ;
        public float maxZ;
        public float y;
    }

    private struct CandidateData
    {
        public Vector3 position;
        public float rotationY;
    }

    public enum TableType { CaffeTable, Desk, Reception, Conference }

    [Header("General Settings")]
    public TableType tableType;
    public bool allowDuplicateProps = true;
    public bool allowSpawnOnFullSurface = true;
    public float minDistanceBetweenProps = 0.5f;
    public int maxAttemptsPerProp = 100;
    public int propCount = 10;

    [Header("Prop Lists")]
    public List<GameObject> randomProps;
    public List<Transform> staticPropPoints;
    public List<GameObject> staticProps;

    [Header("Spawn Areas")]
    public List<BoxCollider> spawnAreas;
    public BoxCollider ownCollider;

    [Header("Pooling Settings")]
    public string keepLayerName = "Keep";

    [Header("Batch spawn")]
    public bool useBatchedSpawn = true;
    public int maxOpsPerFrame = 20;

    private int keepLayer;

    private List<Vector3> spawnedPositions = new List<Vector3>();
    private List<GameObject> availablePrefabs = new List<GameObject>();
    private Coroutine spawnRoutine;

    private void Awake()
    {
        keepLayer = LayerMask.NameToLayer(keepLayerName);
    }

    private void OnEnable()
    {
        //Najpierw czyœcimy stare obiekty
        ReturnSpawnedChildren();

        //Reset pozycji
        spawnedPositions.Clear();

        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        //Spawn na nowo
        SpawnWorkQueue.Enqueue(this, StartSpawn);
    }

    private void StartSpawn()
    {
        if (useBatchedSpawn)
            spawnRoutine = StartCoroutine(SpawnAllRoutine());
        else
        {
            SpawnStaticProps();
            SpawnRandomProps();
        }
    }

    private IEnumerator SpawnAllRoutine()
    {
        yield return StartCoroutine(SpawnStaticPropsRoutine());
        yield return StartCoroutine(SpawnRandomPropsRoutine());
        spawnRoutine = null;
    }

    private void ReturnSpawnedChildren()
    {
        // Zapisujemy dzieci w tablicy, ¿eby nie iterowaæ na ¿ywo
        Transform[] children = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            children[i] = transform.GetChild(i);

        foreach (Transform child in children)
        {
            if (child.gameObject.layer == keepLayer)
                continue;

            PoolableObject po = child.GetComponent<PoolableObject>();
            if (po != null)
                po.ReturnToPool();
            else
                Destroy(child.gameObject); // fallback, gdyby coœ nie mia³o PoolableObject
        }
    }

    private IEnumerator SpawnStaticPropsRoutine()
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < Mathf.Min(staticPropPoints.Count, staticProps.Count); i++)
            indices.Add(i);

        Shuffle(indices);

        int opsThisFrame = 0;

        foreach (int i in indices)
        {
            Transform point = staticPropPoints[i];
            GameObject prefab = staticProps[i];

            if (point != null && prefab != null && !spawnedPositions.Contains(point.position))
            {
                GameObject obj = SingleObjectPool.Instance.Get(prefab, point.position, point.rotation, this.transform);

                PoolableObject po = obj.GetComponent<PoolableObject>();
                if (po != null && po.prefab == null)
                    po.Init(prefab);

                spawnedPositions.Add(point.position);
            }

            opsThisFrame++;
            if (opsThisFrame >= Mathf.Max(1, maxOpsPerFrame))
            {
                opsThisFrame = 0;
                yield return null;
            }
        }
    }

    private IEnumerator SpawnRandomPropsRoutine()
    {
        List<CandidateData> candidates = null;
        int seed = Random.Range(int.MinValue, int.MaxValue);

        AreaData ownArea = default;
        bool hasOwnArea = false;
        if (ownCollider != null)
        {
            Bounds b = ownCollider.bounds;
            ownArea = new AreaData { minX = b.min.x, maxX = b.max.x, minZ = b.min.z, maxZ = b.max.z, y = b.max.y };
            hasOwnArea = true;
        }

        Vector3 circleCenter = hasOwnArea ? ownCollider.bounds.center : transform.position;
        float circleRadius = hasOwnArea ? Mathf.Max(0f, Mathf.Min(ownCollider.bounds.extents.x, ownCollider.bounds.extents.z) - 1f) : 0f;

        List<AreaData> areas = new List<AreaData>();
        if (spawnAreas != null)
        {
            foreach (BoxCollider area in spawnAreas)
            {
                if (area == null) continue;
                Bounds b = area.bounds;
                areas.Add(new AreaData { minX = b.min.x, maxX = b.max.x, minZ = b.min.z, maxZ = b.max.z, y = b.max.y });
            }
        }

        Task<List<CandidateData>> planTask = Task.Run(() => BuildTableCandidates(
            tableType,
            propCount,
            maxAttemptsPerProp,
            allowSpawnOnFullSurface,
            minDistanceBetweenProps,
            hasOwnArea,
            ownArea,
            areas,
            circleCenter,
            circleRadius,
            seed));

        while (!planTask.IsCompleted)
            yield return null;

        if (!planTask.IsFaulted && !planTask.IsCanceled)
            candidates = planTask.Result;

        if (candidates == null)
            candidates = new List<CandidateData>();

        int opsThisFrame = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            CandidateData data = candidates[i];
            GameObject prefab = GetRandomProp();
            GameObject obj = SingleObjectPool.Instance.Get(
                prefab,
                data.position,
                Quaternion.Euler(0, data.rotationY, 0),
                this.transform
            );

            PoolableObject po = obj.GetComponent<PoolableObject>();
            if (po != null && po.prefab == null)
                po.Init(prefab);

            spawnedPositions.Add(data.position);

            opsThisFrame++;
            if (opsThisFrame >= Mathf.Max(1, maxOpsPerFrame))
            {
                opsThisFrame = 0;
                yield return null;
            }
        }
    }

    private static List<CandidateData> BuildTableCandidates(
        TableType tableType,
        int propCount,
        int maxAttemptsPerProp,
        bool allowSpawnOnFullSurface,
        float minDistanceBetweenProps,
        bool hasOwnArea,
        AreaData ownArea,
        List<AreaData> areas,
        Vector3 circleCenter,
        float circleRadius,
        int seed)
    {
        List<CandidateData> result = new List<CandidateData>(Mathf.Max(0, propCount));
        System.Random random = new System.Random(seed);

        for (int i = 0; i < propCount; i++)
        {
            int attempts = 0;
            bool spawned = false;

            while (attempts < maxAttemptsPerProp && !spawned)
            {
                Vector3 pos = BuildRandomPosition(tableType, allowSpawnOnFullSurface, hasOwnArea, ownArea, areas, circleCenter, circleRadius, random);

                if (IsPositionValidForPlan(result, pos, minDistanceBetweenProps))
                {
                    result.Add(new CandidateData
                    {
                        position = pos,
                        rotationY = (float)(random.NextDouble() * 360.0)
                    });

                    spawned = true;
                }

                attempts++;
            }
        }

        return result;
    }

    private static Vector3 BuildRandomPosition(
        TableType tableType,
        bool allowSpawnOnFullSurface,
        bool hasOwnArea,
        AreaData ownArea,
        List<AreaData> areas,
        Vector3 circleCenter,
        float circleRadius,
        System.Random random)
    {
        switch (tableType)
        {
            case TableType.CaffeTable:
                {
                    if (circleRadius <= 0f)
                        return circleCenter;

                    double angle = random.NextDouble() * System.Math.PI * 2.0;
                    double radius = System.Math.Sqrt(random.NextDouble()) * circleRadius;
                    float x = circleCenter.x + (float)(System.Math.Cos(angle) * radius);
                    float z = circleCenter.z + (float)(System.Math.Sin(angle) * radius);
                    float y = hasOwnArea ? ownArea.y : circleCenter.y;
                    return new Vector3(x, y, z);
                }

            case TableType.Desk:
            case TableType.Reception:
            case TableType.Conference:
            default:
                {
                    AreaData area;
                    if (allowSpawnOnFullSurface && areas != null && areas.Count > 0)
                    {
                        area = areas[random.Next(areas.Count)];
                    }
                    else if (hasOwnArea)
                    {
                        area = ownArea;
                    }
                    else
                    {
                        return circleCenter;
                    }

                    float x = Lerp(area.minX, area.maxX, (float)random.NextDouble());
                    float z = Lerp(area.minZ, area.maxZ, (float)random.NextDouble());
                    return new Vector3(x, area.y, z);
                }
        }
    }

    private static bool IsPositionValidForPlan(List<CandidateData> existing, Vector3 candidate, float minDistance)
    {
        for (int i = 0; i < existing.Count; i++)
        {
            Vector3 pos = existing[i].position;
            float dx = candidate.x - pos.x;
            float dz = candidate.z - pos.z;
            float distSq = dx * dx + dz * dz;
            if (distSq < minDistance * minDistance)
                return false;
        }

        return true;
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    void SpawnStaticProps()
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < Mathf.Min(staticPropPoints.Count, staticProps.Count); i++)
            indices.Add(i);

        Shuffle(indices);

        foreach (int i in indices)
        {
            Transform point = staticPropPoints[i];
            GameObject prefab = staticProps[i];

            if (point != null && prefab != null && !spawnedPositions.Contains(point.position))
            {
                GameObject obj = SingleObjectPool.Instance.Get(prefab, point.position, point.rotation, this.transform);

                PoolableObject po = obj.GetComponent<PoolableObject>();
                if (po != null && po.prefab == null)
                    po.Init(prefab);

                spawnedPositions.Add(point.position);
            }
        }
    }

    void SpawnRandomProps()
    {
        for (int i = 0; i < propCount; i++)
        {
            int attempts = 0;
            bool spawned = false;

            while (attempts < maxAttemptsPerProp && !spawned)
            {
                Vector3 candidatePos = GetRandomSpawnPosition();
                if (IsPositionValid(candidatePos))
                {
                    GameObject prefab = GetRandomProp();
                    GameObject obj = SingleObjectPool.Instance.Get(
                        prefab,
                        candidatePos,
                        Quaternion.Euler(0, Random.Range(0f, 360f), 0),
                        this.transform
                    );

                    PoolableObject po = obj.GetComponent<PoolableObject>();
                    if (po != null && po.prefab == null)
                        po.Init(prefab);

                    spawnedPositions.Add(candidatePos);
                    spawned = true;
                }
                attempts++;
            }
        }
    }

    private GameObject GetRandomProp()
    {
        if (allowDuplicateProps)
        {
            return randomProps[Random.Range(0, randomProps.Count)];
        }
        else
        {
            if (availablePrefabs.Count == 0)
                availablePrefabs = new List<GameObject>(randomProps);

            int index = Random.Range(0, availablePrefabs.Count);
            GameObject chosen = availablePrefabs[index];
            availablePrefabs.RemoveAt(index);
            return chosen;
        }
    }

    Vector3 GetRandomSpawnPosition()
    {
        switch (tableType)
        {
            case TableType.CaffeTable:
                return GetRandomPointInCircle();
            case TableType.Desk:
            case TableType.Reception:
            case TableType.Conference:
                return GetRandomPointInBoxes();
            default:
                return transform.position;
        }
    }

    Vector3 GetRandomPointInCircle()
    {
        Vector3 center = ownCollider.bounds.center;
        float radius = Mathf.Min(ownCollider.bounds.extents.x, ownCollider.bounds.extents.z) - 1f;
        Vector2 point2D = Random.insideUnitCircle * radius;
        float y = ownCollider.bounds.max.y;
        return new Vector3(center.x + point2D.x, y, center.z + point2D.y);
    }

    Vector3 GetRandomPointInBoxes()
    {
        if (!allowSpawnOnFullSurface || spawnAreas.Count == 0)
            return GetRandomPointOnOwnCollider();

        BoxCollider area = spawnAreas[Random.Range(0, spawnAreas.Count)];
        Bounds bounds = area.bounds;

        float x = Random.Range(bounds.min.x, bounds.max.x);
        float z = Random.Range(bounds.min.z, bounds.max.z);
        float y = bounds.max.y;

        return new Vector3(x, y, z);
    }

    Vector3 GetRandomPointOnOwnCollider()
    {
        if (ownCollider == null) return transform.position;
        Bounds bounds = ownCollider.bounds;

        float x = Random.Range(bounds.min.x, bounds.max.x);
        float z = Random.Range(bounds.min.z, bounds.max.z);
        float y = bounds.max.y;

        return new Vector3(x, y, z);
    }

    bool IsPositionValid(Vector3 candidate)
    {
        foreach (Vector3 pos in spawnedPositions)
        {
            float dist = Vector2.Distance(new Vector2(candidate.x, candidate.z), new Vector2(pos.x, pos.z));
            if (dist < minDistanceBetweenProps)
                return false;
        }
        return true;
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }
}