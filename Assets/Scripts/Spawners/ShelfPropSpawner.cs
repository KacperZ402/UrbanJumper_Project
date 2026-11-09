using System.Collections.Generic;
using UnityEngine;

public class ShelfPropSpawner : PropSpawnerBase
{
    public enum PropType { Books, Boxes, Organizers }

    [System.Serializable]
    public class ShelfPropGroup
    {
        public PropType type;
        public List<GameObject> prefabs;
        public bool spawnMultiple;
    }

    [Header("Spawn Zones (BoxColliders)")]
    public List<BoxCollider> spawnZones;

    [Header("Prop Groups")]
    public List<ShelfPropGroup> propGroups;

    [Header("Spawn Settings")]
    public float spacing = 0.3f;
    public int maxPropsPerZone = 5;

    protected override void BuildPlan(List<SpawnRequest> plan)
    {
        if (spawnZones == null || propGroups == null || propGroups.Count == 0) return;

        float safeSpacing = Mathf.Max(0.01f, spacing);

        foreach (BoxCollider zone in spawnZones)
        {
            if (zone == null) continue;

            ShelfPropGroup group = propGroups[Random.Range(0, propGroups.Count)];
            if (group == null || group.prefabs == null || group.prefabs.Count == 0) continue;

            Bounds b = GetWorldBounds(zone);
            Quaternion rotation = zone.transform.rotation;

            if (!group.spawnMultiple)
            {
                Vector3 pos = new Vector3(b.center.x, b.max.y, b.center.z);
                plan.Add(new SpawnRequest(RandomPrefab(group), pos, rotation));
                continue;
            }

            int maxAllowed = Mathf.Min(Mathf.FloorToInt(b.size.z / safeSpacing), maxPropsPerZone);
            if (maxAllowed <= 0) continue;

            int count = Random.Range(0, maxAllowed + 1);
            float startZ = b.center.z - (count - 1) * safeSpacing * 0.5f;

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = new Vector3(b.center.x, b.max.y, startZ + i * safeSpacing);
                plan.Add(new SpawnRequest(RandomPrefab(group), pos, rotation));
            }
        }
    }

    private static GameObject RandomPrefab(ShelfPropGroup group)
    {
        return group.prefabs[Random.Range(0, group.prefabs.Count)];
    }
}