using UnityEngine;

public class PlatformManager : MonoBehaviour
{
    [Header("Prefaby segmentów")]
    public GameObject[] regularPlatforms;
    public GameObject endPlatformPrefab;

    [Header("Zakres liczby segmentów")]
    public int minSegments = 10;
    public int maxSegments = 20;

    [Header("Inicjalne ustawienia")]
    public int initialSegments = 3;              // ile segmentów na start
    public bool disableTriggersInInitial = true; // wyłączać triggery w początkowych segmentach?

    private int lastIndex = -1;   // kontrola żeby nie powtarzać tego samego segmentu
    private int spawnedCount = 0;
    private int targetCount = 0;
    private bool cycleStarted = false; // żeby pierwsze wejście w trigger odróżniało start

    private void Start()
    {
        StartNewCycle();
    }

    /// <summary>
    /// Wywoływane przez trigger endpointa.
    /// </summary>
    public void OnTriggerActivated(Transform endPoint)
    {
        if (!cycleStarted)
        {
            // pierwszy trigger w tym cyklu – spawn sekwencji początkowej
            SpawnInitialSequence(endPoint);
            cycleStarted = true;
            return;
        }

        SpawnNext(endPoint);
    }

    private void SpawnNext(Transform endPoint)
    {
        if (spawnedCount >= targetCount)
        {
            // Postaw końcową platformę
            Instantiate(endPlatformPrefab, endPoint.position, endPoint.rotation);
            Debug.Log("Postawiono EndPlatform – cykl zakończony!");

            StartNewCycle();
            return;
        }

        int newIndex = GetRandomIndexDifferentFromLast();
        GameObject segment = Instantiate(
            regularPlatforms[newIndex],
            endPoint.position,
            endPoint.rotation
        );

        lastIndex = newIndex;
        spawnedCount++;
    }

    private void StartNewCycle()
    {
        spawnedCount = 0;
        lastIndex = -1;
        targetCount = Random.Range(minSegments, maxSegments + 1);
        cycleStarted = false;

        Debug.Log($"Nowy cykl: do wygenerowania {targetCount} segmentów.");
    }

    private void SpawnInitialSequence(Transform spawnPoint)
    {
        Transform currentPoint = spawnPoint;

        for (int i = 0; i < initialSegments; i++)
        {
            int newIndex = GetRandomIndexDifferentFromLast();
            GameObject segment = Instantiate(
                regularPlatforms[newIndex],
                currentPoint.position,
                currentPoint.rotation
            );

            lastIndex = newIndex;
            spawnedCount++;

            // pobieramy endpoint
            Transform endPoint = segment.transform.Find("endPoint");
            if (endPoint != null) currentPoint = endPoint;

            // ogarniamy trigger
            EndPointTrigger trigger = segment.GetComponentInChildren<EndPointTrigger>();
            if (trigger != null && disableTriggersInInitial && i < initialSegments - 1)
            {
                trigger.gameObject.SetActive(false); // wyłączamy triggery we wcześniejszych segmentach
            }
        }
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