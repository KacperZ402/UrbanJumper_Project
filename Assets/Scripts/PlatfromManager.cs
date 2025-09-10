using UnityEngine;

public class PlatformManager : MonoBehaviour
{
    [Header("Prefaby segmentów")]
    public GameObject[] regularPlatforms;
    public GameObject endPlatformPrefab;

    [Header("Zakres liczby segmentów")]
    public int minSegments = 10;
    public int maxSegments = 20;

    [Header("Ile segmentów spawnujemy od razu na starcie")]
    public int initialSpawnCount = 3;

    private int lastIndex = -1;   // żeby nie powtarzać segmentu
    private int spawnedCount = 0;
    private int targetCount = 0;

    private void Start()
    {
        StartNewCycle();
    }

    private void StartNewCycle()
    {
        spawnedCount = 0;
        lastIndex = -1;
        targetCount = Random.Range(minSegments, maxSegments + 1);
        Debug.Log($"Nowy cykl: do wygenerowania {targetCount} segmentów.");

        // Spawn pierwszych kilku segmentów od razu
        Transform spawnPoint = transform; // startowa pozycja (np. start platformy)
        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnSegmentAt(spawnPoint);
            spawnPoint = GetSegmentEndPoint(); // ustawiamy kolejny spawnPoint
        }
    }

    /// <summary>
    /// Wywoływane przez trigger segmentu
    /// </summary>
    public void SpawnNext(Transform triggerParentEndPoint)
    {
        if (spawnedCount >= targetCount)
        {
            // Spawn EndPlatform
            Instantiate(endPlatformPrefab, triggerParentEndPoint.position, triggerParentEndPoint.rotation);
            Debug.Log("Postawiono EndPlatform – cykl zakończony!");
            StartNewCycle();
            return;
        }

        SpawnSegmentAt(triggerParentEndPoint);
    }

    private void SpawnSegmentAt(Transform spawnPoint)
    {
        int newIndex = GetRandomIndexDifferentFromLast();
        GameObject segment = Instantiate(
            regularPlatforms[newIndex],
            spawnPoint.position,
            spawnPoint.rotation
        );

        lastIndex = newIndex;
        spawnedCount++;

        // Znajdź trigger w segmencie i przypisz mu PlatformManager
        EndPointTrigger trigger = segment.GetComponentInChildren<EndPointTrigger>();
        if (trigger != null)
        {
            trigger.manager = this;
        }
        else
        {
            Debug.LogWarning("Brak EndPointTrigger w segmencie!");
        }
    }

    private Transform GetSegmentEndPoint()
    {
        // Pobiera endpoint ostatnio zrespionego segmentu
        GameObject lastSegment = GameObject.FindGameObjectsWithTag("Segment")?[GameObject.FindGameObjectsWithTag("Segment").Length - 1];
        if (lastSegment != null)
        {
            Transform endPoint = lastSegment.transform.Find("endPoint");
            if (endPoint != null) return endPoint;
        }
        return transform; // fallback na start
    }

    private int GetRandomIndexDifferentFromLast()
    {
        if (regularPlatforms.Length <= 1) return 0;

        int newIndex;
        do
        {
            newIndex = Random.Range(0, regularPlatforms.Length);
        } while (newIndex == lastIndex);

        return newIndex;
    }
}