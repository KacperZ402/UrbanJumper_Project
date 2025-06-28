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

    [Header("Kolizja")]
    public LayerMask collisionMask;

    private void Start()
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

        BoxCollider area = GetComponent<BoxCollider>();
        if (area == null)
        {
            Debug.LogError("Brak BoxCollidera na obiekcie SpawnArea.");
            return;
        }

        int totalProps = Random.Range(minProps, maxProps + 1);
        int[] groupDistribution = GetGroupDistribution(chosenType, totalProps);

        for (int g = 0; g < groupDistribution.Length; g++)
        {
            if (g >= selectedSet.propGroups.Count) break;

            PropGroup group = selectedSet.propGroups[g];
            int groupCount = groupDistribution[g];

            for (int i = 0; i < groupCount; i++)
            {
                int attempts = 0;
                while (attempts < 10)
                {
                    attempts++;

                    Vector3 randomPoint = GetRandomPointInBox(area);
                    Quaternion rotation = Quaternion.Euler(0f, 90f * Random.Range(0, 4), 0f);

                    GameObject prefab = group.props[Random.Range(0, group.props.Count)];
                    if (TrySpawnWithoutCollision(prefab, randomPoint, rotation, out GameObject spawned))
                    {
                        break;
                    }
                }
            }
        }

        // Uniwersalne propy
        int extraUniversal = Random.Range(0, 3);
        for (int i = 0; i < extraUniversal; i++)
        {
            int attempts = 0;
            while (attempts < 10)
            {
                attempts++;

                Vector3 point = GetRandomPointInBox(area);
                Quaternion rotation = Quaternion.Euler(0f, 90f * Random.Range(0, 4), 0f);

                GameObject uProp = universalProps[Random.Range(0, universalProps.Count)];
                if (TrySpawnWithoutCollision(uProp, point, rotation, out GameObject spawned))
                {
                    break;
                }
            }
        }
    }

    int[] GetGroupDistribution(SurfaceType type, int total)
    {
        int[] result = new int[4];

        switch (type)
        {
            case SurfaceType.OpenSpace:
            case SurfaceType.Cafe:
                result[0] = Mathf.RoundToInt(total * 0.8f);
                int remaining = total - result[0];
                for (int i = 1; i < result.Length; i++)
                    result[i] = remaining / (result.Length - 1);
                break;

            case SurfaceType.Reception:
            case SurfaceType.MeetingRoom:
                result[0] = 1;
                int rest = total - 1;
                for (int i = 1; i < result.Length; i++)
                    result[i] = rest / (result.Length - 1);
                break;
        }

        return result;
    }

    Vector3 GetRandomPointInBox(BoxCollider box)
    {
        Vector3 center = transform.TransformPoint(box.center);
        Vector3 size = Vector3.Scale(box.size, transform.lossyScale);

        float x = Random.Range(-size.x / 2f, size.x / 2f);
        float y = Random.Range(-size.y / 2f, size.y / 2f);
        float z = Random.Range(-size.z / 2f, size.z / 2f);

        return center + transform.rotation * new Vector3(x, y, z);
    }

    bool TrySpawnWithoutCollision(GameObject prefab, Vector3 position, Quaternion rotation, out GameObject spawnedObj)
    {
        spawnedObj = Instantiate(prefab, position, rotation, transform);
        spawnedObj.SetActive(false); // tymczasowo wy³¹czony do testów

        bool hasCollision = false;
        Collider[] spawnedColliders = spawnedObj.GetComponentsInChildren<Collider>();

        foreach (Collider spawnedCol in spawnedColliders)
        {
            if (!spawnedCol.enabled || spawnedCol.isTrigger)
                continue;

            Collider[] overlapped = Physics.OverlapBox(
                spawnedCol.bounds.center,
                spawnedCol.bounds.extents,
                spawnedCol.transform.rotation,
                collisionMask
            );

            foreach (Collider hit in overlapped)
            {
                if (hit.transform.IsChildOf(spawnedObj.transform) || !hit.enabled || hit.isTrigger)
                    continue;

                if (Physics.ComputePenetration(
                    spawnedCol, spawnedCol.transform.position, spawnedCol.transform.rotation,
                    hit, hit.transform.position, hit.transform.rotation,
                    out Vector3 dir, out float dist))
                {
                    hasCollision = true;
                    break;
                }
            }

            if (hasCollision)
                break;
        }

        if (hasCollision)
        {
            Destroy(spawnedObj);
            spawnedObj = null;
            return false;
        }

        spawnedObj.SetActive(true);
        return true;
    }
}