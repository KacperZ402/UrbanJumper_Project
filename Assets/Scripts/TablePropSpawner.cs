using System.Collections.Generic;
using UnityEngine;

public class TablePropSpawner : MonoBehaviour
{
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

    private List<Vector3> spawnedPositions = new List<Vector3>();

    private void Start()
    {
        SpawnStaticProps();
        SpawnRandomProps();
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

            if (point != null && prefab != null)
            {
                // Tylko sprawdzamy czy ktoœ ju¿ siê nie zrespi³ dok³adnie tutaj
                if (spawnedPositions.Contains(point.position))
                    continue;

                GameObject obj = Instantiate(prefab, point.position, point.rotation);
                obj.transform.parent = this.transform;

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
                    GameObject obj = Instantiate(prefab, candidatePos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
                    obj.transform.parent = this.transform;
                    spawnedPositions.Add(candidatePos);
                    spawned = true;
                }
                attempts++;
            }
        }
    }

    private List<GameObject> availablePrefabs = new List<GameObject>();

    private GameObject GetRandomProp()
    {
        if (allowDuplicateProps)
        {
            return randomProps[Random.Range(0, randomProps.Count)];
        }
        else
        {
            if (availablePrefabs.Count == 0)
            {
                // Skopiuj oryginaln¹ listê losowych propów do u¿ycia bez duplikatów
                availablePrefabs = new List<GameObject>(randomProps);
            }

            int index = Random.Range(0, availablePrefabs.Count);
            GameObject chosen = availablePrefabs[index];
            availablePrefabs.RemoveAt(index); // Usuwamy, ¿eby nie wypad³ drugi raz
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
        float radius = Mathf.Min(ownCollider.bounds.extents.x, ownCollider.bounds.extents.z) - 1f; // margines
        Vector2 point2D = Random.insideUnitCircle * radius;
        float y = ownCollider.bounds.max.y;
        return new Vector3(center.x + point2D.x, y, center.z + point2D.y);
    }

    Vector3 GetRandomPointInBoxes()
    {
        if (!allowSpawnOnFullSurface || spawnAreas.Count == 0)
        {
            return GetRandomPointOnOwnCollider();
        }

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