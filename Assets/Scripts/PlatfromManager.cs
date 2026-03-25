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

    [Header("Debug")]
    public bool debugLogs = true;

    private int lastIndex = -1;   // kontrola żeby nie powtarzać tego samego segmentu
    private int spawnedCount = 0;
    private int targetCount = 0;
    private bool cycleStarted = false; // żeby pierwsze wejście w trigger odróżniało start

    private void Start()
    {
        PlatformManager[] managers = FindObjectsOfType<PlatformManager>();
        if (managers.Length > 1)
            Debug.LogWarning($"[PlatformManager#{GetInstanceID()}] Wykryto {managers.Length} instancji PlatformManager w scenie!");

        Log($"Start | instanceId={GetInstanceID()} | initial cycleStarted={cycleStarted} | spawnedCount={spawnedCount} | targetCount={targetCount}");
        StartNewCycle();
    }

    /// <summary>
    /// Wywoływane przez trigger endpointa.
    /// </summary>
    public void OnTriggerActivated(Transform endPoint)
    {
        Log($"OnTriggerActivated | instanceId={GetInstanceID()} | cycleStarted={cycleStarted} | spawnedCount={spawnedCount} | targetCount={targetCount} | endPoint={(endPoint != null ? endPoint.name : "null")}");

        if (!cycleStarted)
        {
            // pierwszy trigger w tym cyklu – spawn sekwencji początkowej
            Log($"START initial sequence | initialSegments={initialSegments}");
            SpawnInitialSequence(endPoint);
            cycleStarted = true;
            Log($"END initial sequence | spawnedCount={spawnedCount} | targetCount={targetCount}");
            return;
        }

        Log("SpawnNext requested by trigger");
        SpawnNext(endPoint);
    }

    private void SpawnNext(Transform endPoint)
    {
        Log($"SpawnNext ENTER | spawnedCount={spawnedCount} | targetCount={targetCount}");

        if (spawnedCount >= targetCount)
        {
            // Postaw końcową platformę
            Instantiate(endPlatformPrefab, endPoint.position, endPoint.rotation);
            Debug.Log("Postawiono EndPlatform – cykl zakończony!");
            Log("EndPlatform spawned -> StartNewCycle()");

            StartNewCycle();
            return;
        }

        int newIndex = GetRandomIndexDifferentFromLast();
        GameObject segment = Instantiate(
            regularPlatforms[newIndex],
            endPoint.position,
            endPoint.rotation
        );
        Log($"Spawned segment | index={newIndex} | name={segment.name} | at={endPoint.position}");

        lastIndex = newIndex;
        spawnedCount++;
        Log($"SpawnNext EXIT | spawnedCount={spawnedCount} | targetCount={targetCount}");
    }

    private void StartNewCycle()
    {
        spawnedCount = 0;
        lastIndex = -1;
        targetCount = Random.Range(minSegments, maxSegments + 1);
        cycleStarted = false;

        Debug.Log($"Nowy cykl: do wygenerowania {targetCount} segmentów.");
        Log($"StartNewCycle | min={minSegments} | max={maxSegments} | target={targetCount}");
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
            else Log($"Initial[{i}] WARNING: brak endPoint w {segment.name}");

            // ogarniamy trigger
            EndPointTrigger trigger = segment.GetComponentInChildren<EndPointTrigger>();
            if (trigger != null && disableTriggersInInitial && i < initialSegments - 1)
            {
                trigger.gameObject.SetActive(false); // wyłączamy triggery we wcześniejszych segmentach
            }

            Log($"Initial[{i}] | index={newIndex} | segment={segment.name} | triggerActive={(trigger != null && trigger.gameObject.activeSelf)} | spawnedCount={spawnedCount}");
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

    private void Log(string message)
    {
        if (!debugLogs)
            return;

        Debug.Log($"[PlatformManager#{GetInstanceID()}] {message}");
    }
}