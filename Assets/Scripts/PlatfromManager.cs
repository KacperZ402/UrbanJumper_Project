using UnityEngine;

public class PlatformManager : MonoBehaviour
{
    [Header("Prefaby segmentów")]
    public GameObject[] regularPlatforms;
    public GameObject endPlatformPrefab;

    [Header("Zakres liczby segmentów")]
    public int minSegments = 10;
    public int maxSegments = 20;

    private int lastIndex = -1;   // do kontroli ¿eby nie powtarzaæ tego samego segmentu
    private int spawnedCount = 0;
    private int targetCount = 0;

    private void Start()
    {
        StartNewCycle();
    }

    /// <summary>
    /// Wywo³ywane przez trigger endpointa.
    /// </summary>
    public void SpawnNext(Transform endPoint)
    {
        if (spawnedCount >= targetCount)
        {
            // Postaw koñcow¹ platformê
            Instantiate(endPlatformPrefab, endPoint.position, endPoint.rotation);
            Debug.Log("Postawiono EndPlatform – cykl zakoñczony!");

            // Nowy cykl
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

    /// <summary>
    /// Rozpoczyna now¹ rundê spawnowania.
    /// </summary>
    private void StartNewCycle()
    {
        spawnedCount = 0;
        lastIndex = -1;
        targetCount = Random.Range(minSegments, maxSegments + 1);

        Debug.Log($"Nowy cykl: do wygenerowania {targetCount} segmentów.");
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