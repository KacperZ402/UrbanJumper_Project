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
    public int minProps = 6;
    public int maxProps = 12;

    [Header("Kolizja")]
    public LayerMask collisionMask;
    public Vector3 checkBoxSize = new Vector3(0.5f, 0.5f, 0.5f);

    [Header("Dystans krzese³ od biurek")]
    public float chairDistanceFromDesk = 2.5f;

    private void Start()
    {
        GenerateProps();
    }

    void GenerateProps()
    {
        if (allowedSurfaceTypes.Count == 0) return;

        SurfaceType chosenType = allowedSurfaceTypes[Random.Range(0, allowedSurfaceTypes.Count)];
        SurfacePropSet selectedSet = surfacePropSets.Find(s => s.surfaceType == chosenType);

        if (selectedSet == null || selectedSet.propGroups.Count == 0) return;

        BoxCollider area = GetComponent<BoxCollider>();
        if (area == null) return;

        int totalProps = Random.Range(minProps, maxProps + 1);

        List<GameObject> spawnedProps = new List<GameObject>();
        int[] groupCounts = GetGroupDistribution(chosenType, totalProps);

        for (int g = 0; g < groupCounts.Length; g++)
        {
            if (g >= selectedSet.propGroups.Count) break;

            PropGroup group = selectedSet.propGroups[g];
            for (int i = 0; i < groupCounts[g]; i++)
            {
                Vector3 spawnPos = Vector3.zero;

                // Punkt zale¿ny od innego propa
                if (chosenType == SurfaceType.OpenSpace && g == 1 && spawnedProps.Count > i)
                {
                    Vector3 candidate = GetOppositePoint(spawnedProps[i].transform.position, 2f); // wiêkszy dystans
                    if (IsPositionValid(candidate))
                        spawnPos = candidate;
                }
                else if ((chosenType == SurfaceType.Cafe || chosenType == SurfaceType.MeetingRoom) && g == 1 && spawnedProps.Count > 0)
                {
                    int attempts = 0;
                    while (attempts < 10)
                    {
                        Vector3 candidate = GetAroundPoint(spawnedProps[0].transform.position, 1.5f); // wiêkszy promieñ
                        if (IsPositionValid(candidate))
                        {
                            spawnPos = candidate;
                            break;
                        }
                        attempts++;
                    }
                }

                // Fallback: losowa pozycja
                if (spawnPos == Vector3.zero)
                    spawnPos = GetRandomValidPoint(area);

                if (spawnPos == Vector3.zero) continue;

                GameObject prefab = group.props[Random.Range(0, group.props.Count)];
                Quaternion rot = Quaternion.Euler(0, 90 * Random.Range(0, 4), 0);
                GameObject spawned = Instantiate(prefab, spawnPos, rot, transform);

                if (g == 0) spawnedProps.Add(spawned);
            }
        }


        // Uniwersalne propy
        for (int i = 0; i < Random.Range(0, 3); i++)
        {
            Vector3 point = GetRandomValidPoint(area);
            if (point == Vector3.zero) continue;
            GameObject uProp = universalProps[Random.Range(0, universalProps.Count)];
            Instantiate(uProp, point, Quaternion.Euler(0, 90 * Random.Range(0, 4), 0), transform);
        }
    }

    int[] GetGroupDistribution(SurfaceType type, int total)
    {
        int[] groupCounts = new int[4];

        switch (type)
        {
            case SurfaceType.OpenSpace:
                groupCounts[0] = Mathf.RoundToInt(total * 0.35f);
                groupCounts[1] = Mathf.RoundToInt(total * 0.35f);
                groupCounts[2] = (total - groupCounts[0] - groupCounts[1]) / 2;
                groupCounts[3] = total - groupCounts[0] - groupCounts[1] - groupCounts[2];
                break;

            case SurfaceType.Cafe:
                groupCounts[0] = Mathf.RoundToInt(total * 0.25f);
                groupCounts[1] = total - groupCounts[0];
                break;

            case SurfaceType.Reception:
                groupCounts[0] = 1;
                groupCounts[1] = 1;
                break;

            case SurfaceType.MeetingRoom:
                groupCounts[0] = 1;
                groupCounts[1] = Mathf.RoundToInt(total * 0.9f);
                groupCounts[2] = total - groupCounts[0] - groupCounts[1];
                break;
        }

        return groupCounts;
    }

    Vector3 GetRandomValidPoint(BoxCollider box)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 point = GetRandomPointInBox(box);
            if (!Physics.CheckBox(point, checkBoxSize / 2f, Quaternion.identity, collisionMask))
                return point;
        }
        return Vector3.zero;
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

    Vector3 GetOppositePoint(Vector3 reference, float distance)
    {
        Vector3 dir = (reference - transform.position).normalized;
        return reference + (-dir * distance);
    }

    Vector3 GetAroundPoint(Vector3 center, float radius)
    {
        float angle = Random.Range(0f, 360f);
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
        return center + offset;
    }
    bool IsPositionValid(Vector3 point)
    {
        return !Physics.CheckBox(point, checkBoxSize / 2f, Quaternion.identity, collisionMask);
    }
}