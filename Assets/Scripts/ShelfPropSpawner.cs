using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class ShelfPropSpawner : MonoBehaviour
{
    private struct ZonePlanData
    {
        public float centerX;
        public float centerZ;
        public float topY;
        public float zoneLength;
        public Quaternion rotation;
    }

    private struct ShelfSpawnInstruction
    {
        public int groupIndex;
        public int prefabIndex;
        public Vector3 position;
        public Quaternion rotation;
    }

    public enum PropType { Books, Boxes, Organizers }

    [System.Serializable]
    public class PropGroup
    {
        public PropType type;
        public List<GameObject> prefabs;
        public bool spawnMultiple;
    }

    [Header("Spawn Zones (BoxColliders)")]
    public List<BoxCollider> spawnZones;

    [Header("Prop Groups")]
    public List<PropGroup> propGroups;

    [Header("Spawn Settings")]
    public float spacing = 0.3f;
    public int maxPropsPerZone = 5;

    [Header("Pooling Settings")]
    public string keepLayerName = "Keep";

    [Header("Batch spawn")]
    public bool useBatchedSpawn = true;
    public int maxOpsPerFrame = 20;

    private int keepLayer;
    private Coroutine spawnRoutine;

    private void Awake()
    {
        keepLayer = LayerMask.NameToLayer(keepLayerName);
    }

    private void OnEnable()
    {
        //Najpierw oddajemy dzieci do puli
        ReturnSpawnedChildren();

        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        //Spawn na nowo
        SpawnWorkQueue.Enqueue(this, StartSpawn);
    }

    private void StartSpawn()
    {
        if (useBatchedSpawn)
            spawnRoutine = StartCoroutine(SpawnPropsRoutine());
        else
            SpawnProps();
    }

    private void ReturnSpawnedChildren()
    {
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
                Destroy(child.gameObject); // fallback, jeœli coœ nie ma PoolableObject
        }
    }

    public void SpawnProps()
    {
        foreach (BoxCollider zone in spawnZones)
        {
            if (propGroups.Count == 0 || zone == null)
                continue;

            PropGroup selectedGroup = propGroups[Random.Range(0, propGroups.Count)];
            if (selectedGroup.prefabs.Count == 0) continue;

            Vector3 center = zone.bounds.center;
            Quaternion rotation = zone.transform.rotation;

            if (!selectedGroup.spawnMultiple)
            {
                GameObject prefab = selectedGroup.prefabs[Random.Range(0, selectedGroup.prefabs.Count)];
                Vector3 spawnPos = new Vector3(center.x, zone.bounds.max.y, center.z);
                SingleObjectPool.Instance.Get(prefab, spawnPos, rotation, this.transform);
            }
            else
            {
                float zoneLength = zone.bounds.size.z;
                int possibleCount = Mathf.FloorToInt(zoneLength / spacing);
                int maxAllowed = Mathf.Min(possibleCount, maxPropsPerZone);
                if (maxAllowed <= 0) continue;

                int spawnCount = Random.Range(0, maxAllowed + 1);
                if (spawnCount == 0) continue;

                float totalSpacing = (spawnCount - 1) * spacing;
                float startZ = center.z - totalSpacing / 2f;

                for (int i = 0; i < spawnCount; i++)
                {
                    GameObject prefab = selectedGroup.prefabs[Random.Range(0, selectedGroup.prefabs.Count)];
                    Vector3 spawnPos = new Vector3(center.x, zone.bounds.max.y, startZ + i * spacing);
                    SingleObjectPool.Instance.Get(prefab, spawnPos, rotation, this.transform);
                }
            }
        }
    }

    private IEnumerator SpawnPropsRoutine()
    {
        List<ShelfSpawnInstruction> instructions = null;

        List<ZonePlanData> zones = new List<ZonePlanData>();
        if (spawnZones != null)
        {
            foreach (BoxCollider zone in spawnZones)
            {
                if (zone == null) continue;
                zones.Add(new ZonePlanData
                {
                    centerX = zone.bounds.center.x,
                    centerZ = zone.bounds.center.z,
                    topY = zone.bounds.max.y,
                    zoneLength = zone.bounds.size.z,
                    rotation = zone.transform.rotation
                });
            }
        }

        List<int> prefabCounts = new List<int>();
        List<bool> spawnMultiple = new List<bool>();
        if (propGroups != null)
        {
            for (int i = 0; i < propGroups.Count; i++)
            {
                PropGroup g = propGroups[i];
                prefabCounts.Add(g != null && g.prefabs != null ? g.prefabs.Count : 0);
                spawnMultiple.Add(g != null && g.spawnMultiple);
            }
        }

        int seed = Random.Range(int.MinValue, int.MaxValue);
        Task<List<ShelfSpawnInstruction>> planTask = Task.Run(() => BuildShelfPlan(zones, prefabCounts, spawnMultiple, spacing, maxPropsPerZone, seed));
        while (!planTask.IsCompleted)
            yield return null;

        if (!planTask.IsFaulted && !planTask.IsCanceled)
            instructions = planTask.Result;

        if (instructions == null)
            instructions = new List<ShelfSpawnInstruction>();

        int opsThisFrame = 0;

        foreach (ShelfSpawnInstruction instruction in instructions)
        {
            if (instruction.groupIndex < 0 || instruction.groupIndex >= propGroups.Count)
                continue;

            PropGroup group = propGroups[instruction.groupIndex];
            if (group == null || group.prefabs == null || instruction.prefabIndex < 0 || instruction.prefabIndex >= group.prefabs.Count)
                continue;

            GameObject prefab = group.prefabs[instruction.prefabIndex];
            SingleObjectPool.Instance.Get(prefab, instruction.position, instruction.rotation, this.transform);

            opsThisFrame++;

            if (opsThisFrame >= Mathf.Max(1, maxOpsPerFrame))
            {
                opsThisFrame = 0;
                yield return null;
            }
        }

        spawnRoutine = null;
    }

    private static List<ShelfSpawnInstruction> BuildShelfPlan(
        List<ZonePlanData> zones,
        List<int> prefabCounts,
        List<bool> spawnMultiple,
        float spacing,
        int maxPropsPerZone,
        int seed)
    {
        List<ShelfSpawnInstruction> result = new List<ShelfSpawnInstruction>();
        if (zones == null || prefabCounts == null || prefabCounts.Count == 0)
            return result;

        System.Random random = new System.Random(seed);

        for (int z = 0; z < zones.Count; z++)
        {
            ZonePlanData zone = zones[z];
            int groupIndex = random.Next(prefabCounts.Count);
            int prefCount = prefabCounts[groupIndex];
            if (prefCount <= 0)
                continue;

            if (!spawnMultiple[groupIndex])
            {
                result.Add(new ShelfSpawnInstruction
                {
                    groupIndex = groupIndex,
                    prefabIndex = random.Next(prefCount),
                    position = new Vector3(zone.centerX, zone.topY, zone.centerZ),
                    rotation = zone.rotation
                });
            }
            else
            {
                float safeSpacing = spacing <= 0f ? 0.1f : spacing;
                int possibleCount = Mathf.FloorToInt(zone.zoneLength / safeSpacing);
                int maxAllowed = Mathf.Min(possibleCount, maxPropsPerZone);
                if (maxAllowed <= 0)
                    continue;

                int spawnCount = random.Next(maxAllowed + 1);
                if (spawnCount <= 0)
                    continue;

                float totalSpacing = (spawnCount - 1) * safeSpacing;
                float startZ = zone.centerZ - totalSpacing / 2f;

                for (int i = 0; i < spawnCount; i++)
                {
                    result.Add(new ShelfSpawnInstruction
                    {
                        groupIndex = groupIndex,
                        prefabIndex = random.Next(prefCount),
                        position = new Vector3(zone.centerX, zone.topY, startZ + i * safeSpacing),
                        rotation = zone.rotation
                    });
                }
            }
        }

        return result;
    }
}