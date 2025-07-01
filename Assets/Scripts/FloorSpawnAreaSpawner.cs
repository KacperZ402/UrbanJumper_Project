using UnityEngine;
using System.Collections.Generic;

public enum SurfaceType
{
    OpenSpace,
    Reception,
    MeetingRoom,
    Cafe
}

[System.Serializable]
public class PropGroup
{
    public string groupName;
    public List<GameObject> props;
}

[System.Serializable]
public class SurfacePropSet
{
    public SurfaceType surfaceType;
    public List<PropGroup> propGroups;
}

[RequireComponent(typeof(BoxCollider))]
public class FloorSpawnAreaSpawner : MonoBehaviour
{
    [Header("Mo¿liwe typy przestrzeni (checklista)")]
    public List<SurfaceType> allowedSurfaceTypes;

    [Header("PropSet dla ka¿dego typu przestrzeni")]
    public List<SurfacePropSet> surfacePropSets;

    [Header("Propy uniwersalne (np. doniczki)")]
    public List<GameObject> universalProps;

    [Header("Iloœæ propów do wygenerowania")]
    public int minProps = 3;
    public int maxProps = 6;

    [Header("Parametry grida")]
    [Range(0.5f, 5f)] public float gridCellSize = 1.5f;

    private void Start()
    {
        GenerateProps();
    }

    void GenerateProps()
    {
        if (allowedSurfaceTypes == null || allowedSurfaceTypes.Count == 0) return;

        SurfaceType chosenType = allowedSurfaceTypes[Random.Range(0, allowedSurfaceTypes.Count)];
        SurfacePropSet selectedSet = surfacePropSets.Find(s => s.surfaceType == chosenType);

        if (selectedSet == null || selectedSet.propGroups.Count == 0) return;

        BoxCollider area = GetComponent<BoxCollider>();
        Vector3 areaCenter = transform.TransformPoint(area.center);
        Vector3 areaSize = Vector3.Scale(area.size, transform.lossyScale);

        int countX = Mathf.FloorToInt(areaSize.x / gridCellSize);
        int countZ = Mathf.FloorToInt(areaSize.z / gridCellSize);

        Vector3 startCorner = areaCenter - new Vector3(areaSize.x, 0, areaSize.z) * 0.5f + new Vector3(gridCellSize, 0, gridCellSize) * 0.5f;

        int totalProps = Random.Range(minProps, maxProps + 1);

        List<Vector2Int> availableCells = new List<Vector2Int>();
        for (int x = 0; x < countX; x++)
        {
            for (int z = 0; z < countZ; z++)
            {
                availableCells.Add(new Vector2Int(x, z));
            }
        }

        for (int i = 0; i < totalProps && availableCells.Count > 0; i++)
        {
            int index = Random.Range(0, availableCells.Count);
            Vector2Int cell = availableCells[index];
            availableCells.RemoveAt(index);

            Vector3 spawnPos = startCorner + new Vector3(cell.x * gridCellSize, 0, cell.y * gridCellSize);
            Quaternion rotation = Quaternion.Euler(0, 90 * Random.Range(0, 4), 0);

            PropGroup group = selectedSet.propGroups[Random.Range(0, selectedSet.propGroups.Count)];
            GameObject prefab = group.props[Random.Range(0, group.props.Count)];

            Instantiate(prefab, spawnPos, rotation, transform);
        }
    }
}
