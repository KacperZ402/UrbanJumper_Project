using System.Collections.Generic;
using UnityEngine;

public class ShelfPropSpawner : MonoBehaviour
{
    public enum PropType
    {
        Books,
        Boxes,
        Organizers
    }

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

    private void Start()
    {
        SpawnProps();
    }
    private void SpawnProps()
    {
        foreach (BoxCollider zone in spawnZones)
        {
            if (propGroups.Count == 0 || zone == null)
                continue;

            // Wylosuj grupê
            PropGroup selectedGroup = propGroups[Random.Range(0, propGroups.Count)];
            if (selectedGroup.prefabs.Count == 0)
                continue;

            Vector3 center = zone.bounds.center;
            Quaternion rotation = zone.transform.rotation;

            if (!selectedGroup.spawnMultiple)
            {
                // Jeden prop na œrodku
                GameObject prefab = selectedGroup.prefabs[Random.Range(0, selectedGroup.prefabs.Count)];
                Vector3 spawnPos = new Vector3(center.x, zone.bounds.max.y, center.z);
                GameObject obj = Instantiate(prefab, spawnPos, rotation);
                obj.transform.parent = this.transform;
            }
            else
            {
                // Wiele propów wzd³u¿ osi Z, ale w losowej iloœci
                float zoneLength = zone.bounds.size.z;
                int possibleCount = Mathf.FloorToInt(zoneLength / spacing);
                int maxAllowed = Mathf.Min(possibleCount, maxPropsPerZone);
                if (maxAllowed <= 0) continue;

                // Wylosuj ile propów ma siê zrespiæ (od 0 do maxAllowed)
                int spawnCount = Random.Range(0, maxAllowed + 1);
                if (spawnCount == 0) continue;

                float totalSpacing = (spawnCount - 1) * spacing;
                float startZ = center.z - totalSpacing / 2f;

                for (int i = 0; i < spawnCount; i++)
                {
                    GameObject prefab = selectedGroup.prefabs[Random.Range(0, selectedGroup.prefabs.Count)];
                    Vector3 spawnPos = new Vector3(center.x, zone.bounds.max.y, startZ + i * spacing);
                    GameObject obj = Instantiate(prefab, spawnPos, rotation);
                    obj.transform.parent = this.transform;
                }
            }
        }
    }
}