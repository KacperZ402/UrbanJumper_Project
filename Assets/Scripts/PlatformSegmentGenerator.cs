using UnityEngine;

public class PlatformSegmentGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] regularPlatforms;
    public GameObject endPlatformPrefab;

    [Header("Range of Platform Quantity")]
    public int minPlatforms = 15;
    public int maxPlatforms = 30;
    
    private int lastIndex;
    private Transform currentSpawnPoint;

    public void Start()
    {
        GenerateSegment();
    }
    public void GenerateSegment()
    {
        currentSpawnPoint = GetEndPoint(this.gameObject);

        int platformCount = Random.Range(minPlatforms, maxPlatforms + 1);
        for (int i = 0; i < platformCount; i++)
        {
            int newIndex = GetRandomIndexDifferentFromLast();
            GameObject segment = Instantiate(regularPlatforms[newIndex], currentSpawnPoint.position, currentSpawnPoint.rotation);
            lastIndex = newIndex;
            currentSpawnPoint = GetEndPoint(segment);
            lastIndex = newIndex;
        }

        // Na koñcu generujemy endPlatform
        GameObject endPlatform = Instantiate(endPlatformPrefab, currentSpawnPoint.position, Quaternion.Euler(0, 180, 0));
    }

    GameObject GetRandomPlatform()
    {
        return regularPlatforms[Random.Range(0, regularPlatforms.Length)];
    }

    Transform GetEndPoint(GameObject platform)
    {
        Transform endPoint = platform.transform.Find("endPoint");
        if (endPoint == null)
        {
            Debug.LogError($"Brak endPoint w {platform.name}");
        }
        return endPoint;
    }
    int GetRandomIndexDifferentFromLast()
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