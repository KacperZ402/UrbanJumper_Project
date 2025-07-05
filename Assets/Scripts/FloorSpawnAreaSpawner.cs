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

    [Header("Rozmiar jednej kratki (grid cell)")]
    public float cellSize = 1.0f;

    private bool[,] grid;
    private Vector3 gridOrigin;
    private int gridSizeX;
    private int gridSizeZ;

    void Start()
    {
        GenerateProps();
    }

    void GenerateProps()
    {
        if (allowedSurfaceTypes == null || allowedSurfaceTypes.Count == 0)
        {
            Debug.LogWarning("Brak dozwolonych typów przestrzeni.");
            return;
        }

        SurfaceType chosenType = allowedSurfaceTypes[Random.Range(0, allowedSurfaceTypes.Count)];
        SurfacePropSet selectedSet = surfacePropSets.Find(s => s.surfaceType == chosenType);

        if (selectedSet == null || selectedSet.propGroups.Count == 0)
        {
            Debug.LogWarning($"Brak propów dla typu przestrzeni: {chosenType}");
            return;
        }

        InitGrid();

        int totalProps = Random.Range(minProps, maxProps + 1);
        int[] groupDistribution = GetGroupDistribution(chosenType, selectedSet.propGroups.Count, totalProps);

        for (int g = 0; g < selectedSet.propGroups.Count; g++)
        {
            PropGroup group = selectedSet.propGroups[g];
            if (group.props == null || group.props.Count == 0) continue;

            for (int i = 0; i < groupDistribution[g]; i++)
            {
                GameObject prefab = GetRandomProp(group.props);
                if (prefab == null) continue;

                bool isMainProp = (chosenType == SurfaceType.MeetingRoom && g == 0 && i == 0);
                TryPlaceProp(prefab, isMainProp);
            }
        }

        // Uniwersalne propy
        int extraUniversal = Random.Range(0, 3);
        for (int i = 0; i < extraUniversal; i++)
        {
            GameObject uProp = GetRandomProp(universalProps);
            if (uProp != null)
                TryPlaceProp(uProp, false);
        }
    }

    void InitGrid()
    {
        BoxCollider area = GetComponent<BoxCollider>();
        Vector3 areaSize = Vector3.Scale(area.size, transform.lossyScale);
        gridSizeX = Mathf.CeilToInt(areaSize.x / cellSize);
        gridSizeZ = Mathf.CeilToInt(areaSize.z / cellSize);
        grid = new bool[gridSizeX, gridSizeZ];
        gridOrigin = transform.position + transform.rotation * (area.center - area.size * 0.5f);
    }

    GameObject GetRandomProp(List<GameObject> list) =>
        list != null && list.Count > 0 ? list[Random.Range(0, list.Count)] : null;

    int[] GetGroupDistribution(SurfaceType type, int groupCount, int total)
    {
        int[] result = new int[groupCount];
        if (groupCount == 0 || total == 0) return result;

        if (type == SurfaceType.OpenSpace || type == SurfaceType.Cafe)
        {
            result[0] = Mathf.RoundToInt(total * 0.8f);
            int rem = total - result[0];
            for (int i = 1; i < groupCount; i++)
                result[i] = rem / (groupCount - 1);
        }
        else
        {
            result[0] = 1;
            int rem = Mathf.Max(0, total - 1);
            for (int i = 1; i < groupCount; i++)
                result[i] = rem / (groupCount - 1);
        }

        return result;
    }

    void TryPlaceProp(GameObject prefab, bool isMainProp)
    {
        bool forceSpawn = prefab.name.Contains("MeetingRoomTable");

        if (isMainProp || forceSpawn)
        {
            PlaceForcedProp(prefab, forceSpawn);
        }
        else
        {
            PlaceRandomly(prefab);
        }
    }

    void PlaceForcedProp(GameObject prefab, bool forceSpawn)
    {
        Quaternion rotation = Quaternion.identity;
        Vector2Int size = GetColliderBasedSize(prefab, 0, cellSize);
        int startX = 0, startZ = 0;

        Vector3 worldPos = GridToWorld(startX, startZ, size);
        GameObject obj = Instantiate(prefab, worldPos, rotation, transform);
        obj.SetActive(true);

        if (!forceSpawn)
            MarkOccupied(startX, startZ, size);
        else
            Debug.LogWarning($"[Spawner] Zespawnowano {prefab.name} BEZ walidacji siatki.");
    }

    void PlaceRandomly(GameObject prefab)
    {
        for (int attempts = 0; attempts < 30; attempts++)
        {
            int angle = 90 * Random.Range(0, 4);
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            Vector2Int size = GetColliderBasedSize(prefab, angle, cellSize);

            int maxX = gridSizeX - size.x;
            int maxZ = gridSizeZ - size.y;

            if (maxX <= 0 || maxZ <= 0)
                return;

            int x = Random.Range(0, maxX);
            int z = Random.Range(0, maxZ);

            if (CanOccupy(x, z, size))
            {
                Vector3 worldPos = GridToWorld(x, z, size);
                GameObject obj = Instantiate(prefab, worldPos, rotation, transform);
                obj.SetActive(true);
                MarkOccupied(x, z, size);
                break;
            }
        }
    }

    bool CanOccupy(int startX, int startZ, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                if (grid[startX + x, startZ + z])
                    return false;
            }
        }
        return true;
    }

    void MarkOccupied(int startX, int startZ, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                grid[startX + x, startZ + z] = true;
            }
        }
    }

    Vector3 GridToWorld(int x, int z, Vector2Int size)
    {
        Vector3 offset = new Vector3((x + size.x / 2f) * cellSize, 0f, (z + size.y / 2f) * cellSize);
        return gridOrigin + transform.rotation * offset;
    }

    Vector2Int GetColliderBasedSize(GameObject prefab, int angle, float cellSize)
    {
        BoxCollider[] colliders = prefab.GetComponentsInChildren<BoxCollider>();
        if (colliders == null || colliders.Length == 0)
        {
            Debug.LogWarning($"Prefab {prefab.name} nie ma ¿adnych BoxColliderów – pomijam.");
            return Vector2Int.one;
        }

        Bounds combined = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++)
            combined.Encapsulate(colliders[i].bounds);

        Vector3 size = combined.size;

        float sizeX = (angle == 90 || angle == 270) ? size.z : size.x;
        float sizeZ = (angle == 90 || angle == 270) ? size.x : size.z;

        int gridX = Mathf.CeilToInt(sizeX / cellSize);
        int gridZ = Mathf.CeilToInt(sizeZ / cellSize);

        return new Vector2Int(gridX, gridZ);
    }
}
