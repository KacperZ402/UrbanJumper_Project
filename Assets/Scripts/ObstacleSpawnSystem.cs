using UnityEngine;

public class SegmentObstacleSpawner : MonoBehaviour
{
    public enum ObstacleType { None, Jump, Slide, Wall }

    [Header("Prefaby Przeszkód")]
    public GameObject[] jumpObstaclePrefab;
    public GameObject[] slideObstaclePrefab;
    public GameObject[] wallObstaclePrefab;

    [Header("Ustawienia Torów i Platformy")]
    public Transform startAnchor;     // Pusty obiekt na początku (X:0, Y:0, Z:0 lokalnie)
    public float segmentLength = 50f; // Długość platformy w osi Z
    public float laneWidth = 10f;     // Odstęp między torami

    [Header("Ustawienia Spawnu")]
    public float distanceBetweenRows = 10f; // Co ile metrów w osi Z stawiać rząd
    public float startOffsetZ = 5f;         // Margines od krawędzi segmentu

    // Statyczna zmienna – współdzielona przez WSZYSTKIE instancje tego skryptu.
    // Dzięki temu, gdy kończy się Segment A i zaczyna Segment B, 
    // wirtualny gracz płynnie przechodzi między nimi bez teleportacji.
    private static int currentSafeLane = 0;

    private void Start()
    {
        // Generujemy przeszkody od razu, gdy segment pojawia się na scenie
        GenerateObstacles();
    }

    public void GenerateObstacles()
    {
        if (startAnchor == null)
        {
            Debug.LogError("Brak przypisanego startAnchor w segmencie!");
            return;
        }

        float currentLocalZ = startOffsetZ;

        // Idziemy wzdłuż segmentu i stawiamy rzędy
        while (currentLocalZ < segmentLength - startOffsetZ)
        {
            SpawnRow(currentLocalZ);
            currentLocalZ += distanceBetweenRows;
        }
    }

    private void SpawnRow(float localZ)
    {
        // 1. Zmieniamy tor ratunkowy o max 1 (w zakresie od -1 do 1)
        int laneChange = Random.Range(-1, 2);
        currentSafeLane = Mathf.Clamp(currentSafeLane + laneChange, -1, 1);

        // 2. Losujemy przeszkodę na bezpieczny tor (0 = None, 1 = Jump, 2 = Slide)
        ObstacleType safeObstacle = (ObstacleType)Random.Range(0, 3);

        // 3. Wypełniamy wszystkie 3 tory
        for (int lane = -1; lane <= 1; lane++)
        {
            // Liczymy pozycję lokalną względem startAnchor
            float xOffset = lane * laneWidth;
            Vector3 localPosition = new Vector3(xOffset, 0, localZ);

            // Konwersja na pozycję globalną (uwzględnia obrót całego segmentu)
            Vector3 worldPosition = startAnchor.TransformPoint(localPosition);

            if (lane == currentSafeLane)
            {
                SpawnObstacle(safeObstacle, worldPosition);
            }
            else
            {
                // Pozostałe tory dostają losowe przeszkody, włącznie ze ścianami
                ObstacleType randomObstacle = (ObstacleType)Random.Range(0, 4);
                SpawnObstacle(randomObstacle, worldPosition);
            }
        }
    }

    private void SpawnObstacle(ObstacleType type, Vector3 position)
    {
        if (type == ObstacleType.None) return;

        GameObject prefabToSpawn = type switch
        {
            ObstacleType.Jump => jumpObstaclePrefab[Random.Range(0, jumpObstaclePrefab.Length)],
            ObstacleType.Slide => slideObstaclePrefab[Random.Range(0, slideObstaclePrefab.Length)],
            ObstacleType.Wall => wallObstaclePrefab[Random.Range(0, wallObstaclePrefab.Length)],
            _ => null
        };

        if (prefabToSpawn != null)
        {
            // Tworzymy przeszkodę i od razu podpinamy ją pod ten segment (transform)
            // Jak usuniesz segment, przeszkody znikną razem z nim.
            Instantiate(prefabToSpawn, position, Quaternion.identity, transform);
        }
    }

    private void OnDrawGizmos()
    {
        if (startAnchor == null) return;

        Gizmos.color = Color.cyan;
        for (int i = -1; i <= 1; i++)
        {
            float xOffset = i * laneWidth;
            Vector3 start = startAnchor.TransformPoint(new Vector3(xOffset, 0, 0));
            Vector3 end = startAnchor.TransformPoint(new Vector3(xOffset, 0, segmentLength));

            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(end, 0.5f);
        }
    }
}