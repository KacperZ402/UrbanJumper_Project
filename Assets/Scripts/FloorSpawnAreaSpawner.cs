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
    public Vector3 checkBoxSize = new Vector3(0.5f, 0.5f, 0.5f);

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

        // Losuj typ przestrzeni
        SurfaceType chosenType = allowedSurfaceTypes[Random.Range(0, allowedSurfaceTypes.Count)];
        SurfacePropSet selectedSet = surfacePropSets.Find(s => s.surfaceType == chosenType);

        if (selectedSet == null)
        {
            Debug.LogWarning("Brak konfiguracji dla wybranego typu przestrzeni.");
            return;
        }

        int propCount = Random.Range(minProps, maxProps + 1);
        int tries = 0;
        int spawned = 0;

        BoxCollider area = GetComponent<BoxCollider>();
        if (area == null)
        {
            Debug.LogError("Brak BoxCollidera na obiekcie SpawnArea.");
            return;
        }

        while (spawned < propCount && tries < propCount * 10)
        {
            tries++;

            Vector3 randomPoint = GetRandomPointInBox(area);

            if (Physics.CheckBox(randomPoint, checkBoxSize / 2f, Quaternion.identity, collisionMask))
                continue;

            // Wybierz losow¹ grupê, a z niej losowy prop
            PropGroup group = selectedSet.propGroups[Random.Range(0, selectedSet.propGroups.Count)];
            GameObject prefab = group.props[Random.Range(0, group.props.Count)];

            Instantiate(prefab, randomPoint, Quaternion.identity, transform);
            spawned++;
        }

        // Mo¿emy dorzuciæ kilka uniwersalnych propów:
        if (universalProps.Count > 0)
        {
            int extraUniversal = Random.Range(0, 3); // np. 0–2 doniczki
            for (int i = 0; i < extraUniversal; i++)
            {
                Vector3 point = GetRandomPointInBox(area);
                if (!Physics.CheckBox(point, checkBoxSize / 2f, Quaternion.identity, collisionMask))
                {
                    GameObject uProp = universalProps[Random.Range(0, universalProps.Count)];
                    Instantiate(uProp, point, Quaternion.identity, transform);
                }
            }
        }
    }

    Vector3 GetRandomPointInBox(BoxCollider box)
    {
        Vector3 center = box.center + transform.position;
        Vector3 size = box.size;

        float x = Random.Range(-size.x / 2f, size.x / 2f);
        float y = Random.Range(-size.y / 2f, size.y / 2f);
        float z = Random.Range(-size.z / 2f, size.z / 2f);

        return center + transform.rotation * new Vector3(x, y, z);
    }
}
