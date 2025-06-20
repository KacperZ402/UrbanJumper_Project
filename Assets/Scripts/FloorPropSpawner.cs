using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public enum SpaceType
{
    Office,
    MeetingRoom,
    Reception,
    Cafe
}

[System.Serializable]
public class SpacePropSet
{
    public SpaceType spaceType;
    public List<GameObject> prefabs;
}


public class FloorPropSpawner : MonoBehaviour
{
    [Header("Typy przestrzeni, w których mo¿e dzia³aæ ten spawner")]
    public List<SpaceType> allowedSpaceTypes;

    [Header("Aktualnie wybrany typ przestrzeni (ustawiany z zewn¹trz)")]
    public SpaceType currentSpaceType;

    [Header("Prefaby propów do danego typu przestrzeni")]
    public List<SpacePropSet> propSets;

    [Header("Iloœæ propów (losowa)")]
    public int minProps = 3;
    public int maxProps = 7;

    private void Start()
    {
        if (!allowedSpaceTypes.Contains(currentSpaceType)) return;

        var set = propSets.Find(s => s.spaceType == currentSpaceType);
        if (set == null || set.prefabs.Count == 0) return;

        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        int spawnCount = Random.Range(minProps, maxProps + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject prefab = set.prefabs[Random.Range(0, set.prefabs.Count)];
            Vector3 spawnPos = GetRandomPointInBox(box);
            Instantiate(prefab, spawnPos, Quaternion.identity, transform);
        }
    }

    private Vector3 GetRandomPointInBox(BoxCollider box)
    {
        Vector3 center = box.transform.position + box.center;
        Vector3 size = Vector3.Scale(box.size, box.transform.lossyScale);

        float x = Random.Range(-size.x / 2, size.x / 2);
        float y = Random.Range(-size.y / 2, size.y / 2);
        float z = Random.Range(-size.z / 2, size.z / 2);

        return center + new Vector3(x, y, z);
    }
}
