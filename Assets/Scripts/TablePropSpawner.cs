using UnityEngine;
using System.Collections.Generic;

public enum TableType
{
    CaffeTable,
    Desk,
    Reception,
    ConferenceTable
}

public class PropSpawner : MonoBehaviour
{
    public TableType tableType;
    public bool useOwnBoxCollider = true;
    public bool allowRepeatingProps = false;

    public List<GameObject> randomProps;
    public List<GameObject> fixedProps;

    public List<Transform> fixedPropSpawnPoints; // Sta³e pozycje
    public List<BoxCollider> customSpawnAreas;   // Dla biurek, recepcji itp.

    public int maxSpawnAttempts = 10;

    private BoxCollider ownBoxCollider;

    void Start()
    {
        ownBoxCollider = GetComponent<BoxCollider>();
        SpawnFixedProps();
        SpawnRandomProps();
    }

    void SpawnFixedProps()
    {
        for (int i = 0; i < Mathf.Min(fixedProps.Count, fixedPropSpawnPoints.Count); i++)
        {
            var prefab = fixedProps[i];
            var point = fixedPropSpawnPoints[i];
            GameObject spawned = Instantiate(prefab, point.position, point.rotation, transform);
            spawned.transform.localRotation = point.localRotation;
        }
    }

    void SpawnRandomProps()
    {
        List<Vector3> occupiedPositions = new List<Vector3>();
        List<GameObject> usedPrefabs = new List<GameObject>();

        int spawnCount = randomProps.Count;

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject prefab = randomProps[i];

            if (!allowRepeatingProps && usedPrefabs.Contains(prefab)) continue;

            bool spawned = false;

            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                Vector3 spawnPos = Vector3.zero;

                switch (tableType)
                {
                    case TableType.CaffeTable:
                        spawnPos = GetRandomPointInCircle(ownBoxCollider);
                        break;

                    case TableType.Desk:
                    case TableType.Reception:
                    case TableType.ConferenceTable:
                        spawnPos = GetRandomPointInAreas();
                        break;
                }

                if (!IsOverlapping(spawnPos, prefab))
                {
                    Quaternion randomRot = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    GameObject spawnedObj = Instantiate(prefab, spawnPos, randomRot, transform);
                    occupiedPositions.Add(spawnPos);
                    usedPrefabs.Add(prefab);
                    spawned = true;
                    break;
                }
            }

            if (!spawned)
            {
                Debug.LogWarning($"Could not spawn prop '{prefab.name}' after {maxSpawnAttempts} attempts.");
            }
        }
    }

    Vector3 GetRandomPointInCircle(BoxCollider col)
    {
        Vector3 center = col.bounds.center;
        float radius = Mathf.Min(col.bounds.extents.x, col.bounds.extents.z) - 1f; // Margines 1 jednostki

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector2 rand = Random.insideUnitCircle * radius;
            Vector3 point = new Vector3(center.x + rand.x, center.y + col.bounds.extents.y, center.z + rand.y);

            if (Physics.Raycast(point + Vector3.up * 2, Vector3.down, out RaycastHit hit, 4f))
            {
                return hit.point;
            }
        }

        return center;
    }

    Vector3 GetRandomPointInAreas()
    {
        List<BoxCollider> areas = useOwnBoxCollider ? new List<BoxCollider> { ownBoxCollider } : customSpawnAreas;
        if (areas.Count == 0) return transform.position;

        BoxCollider area = areas[Random.Range(0, areas.Count)];

        Vector3 min = area.bounds.min;
        Vector3 max = area.bounds.max;

        Vector3 point = new Vector3(
            Random.Range(min.x, max.x),
            max.y,
            Random.Range(min.z, max.z)
        );

        return point;
    }

    bool IsOverlapping(Vector3 position, GameObject prefab)
    {
        float radius = 0.3f; // Szacowany promieñ obiektu
        Collider[] colliders = Physics.OverlapSphere(position, radius);

        foreach (var col in colliders)
        {
            if (col.gameObject.transform.IsChildOf(this.transform))
                return true;
        }

        return false;
    }
}