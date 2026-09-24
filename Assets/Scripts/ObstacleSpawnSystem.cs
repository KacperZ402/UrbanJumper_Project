using UnityEngine;
using System.Collections.Generic;
public class SegmentObstacleSpawner : MonoBehaviour
{
    public enum ObstacleType { None, Jump, Slide, Wall }

    [Header("Prefaby Przeszkód")]
    public GameObject[] jumpObstaclePrefab;
    public GameObject[] slideObstaclePrefab;
    public GameObject[] wallObstaclePrefab;

    [Header("Prefaby Monet")]
    public GameObject[] coinPrefabs;
    [Range(0f, 1f)] public float coinInRowChance = 0.4f;       // Szansa na monetę w rzędzie
    [Range(0f, 1f)] public float coinBetweenRowsChance = 0.6f; // Szansa na monetę pomiędzy rzędami

    [Header("Wysokości Monet")]

    public float coinDefaultHeight = 3.0f; // Dla pustego pola i pomiędzy rzędami
    public float coinJumpHeight = 10f;
    public float coinSlideHeight = 1f;  // Pod przeszkodą do wślizgu

    [Header("Ustawienia Torów i Platformy")]
    public Transform startAnchor;     // Pusty obiekt na początku (X:0, Y:0, Z:0 lokalnie)
    public float segmentLength = 50f; // Długość platformy w osi Z
    public float laneWidth = 10f;     // Odstęp między torami
    public bool[] LaneEnabled = new bool[3] { true, true, true };  // Czy lewy tor jest aktywny


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
            // 1. Spawnowanie właściwego rzędu z przeszkodami i monetami
            SpawnRow(currentLocalZ);

            // 2. Spawnowanie pojedynczej monety POMIĘDZY rzędami
            float midRowZ = currentLocalZ + (distanceBetweenRows * 0.5f);
            if (midRowZ < segmentLength - startOffsetZ)
            {
                SpawnBetweenRowsCoin(midRowZ);
            }

            currentLocalZ += distanceBetweenRows;
        }
    }

    private void SpawnRow(float localZ)
    {

        List<int> availableLanes = new List<int>();
        for (int i = -1; i <= 1; i++)
        {
            if (LaneEnabled[i + 1] && Mathf.Abs(i - currentSafeLane) <= 1)
            {
                availableLanes.Add(i);
            }
        }

        if (availableLanes.Count == 0)
        {
           
            return; // Przerywamy spawnowanie tego rzędu, żeby nie zawiesić gry
        }
        currentSafeLane = availableLanes[Random.Range(0, availableLanes.Count)];

        // 2. Losujemy przeszkodę na bezpieczny tor (0 = None, 1 = Jump, 2 = Slide)
        ObstacleType safeObstacle = (ObstacleType)Random.Range(0, 3);

        // 3. Wypełniamy wszystkie 3 tory
        for (int lane = -1; lane <= 1; lane++)
        {
            if (!LaneEnabled[lane + 1])
            {
                continue;
            }
            // Liczymy pozycję lokalną względem startAnchor
            float xOffset = lane * laneWidth;
            Vector3 localPosition = new Vector3(xOffset, 0, localZ);

            // Konwersja na pozycję globalną (uwzględnia obrót całego segmentu)
            Vector3 worldPosition = startAnchor.TransformPoint(localPosition);

            ObstacleType chosenType;

            if (lane == currentSafeLane)
            {
                chosenType = safeObstacle;
            }
            else
            {
                chosenType = (ObstacleType)Random.Range(0, 4); // Tu może pojawić się Wall
            }

            // Spawnowanie przeszkody (jeśli nie None)
            SpawnObstacle(chosenType, worldPosition);

            // Spawnowanie monety w rzędzie (tylko None, Jump, Slide)
            if (chosenType != ObstacleType.Wall && Random.value <= coinInRowChance)
            {
                SpawnCoinAt(chosenType, worldPosition);
            }
        }
    }

    private void SpawnBetweenRowsCoin(float localZ)
    {
        if (coinPrefabs == null || coinPrefabs.Length == 0) return;
        if (Random.value > coinBetweenRowsChance) return;

        // Monetę pomiędzy rzędami stawiamy na torze bezpiecznym
        float xOffset = currentSafeLane * laneWidth;
        Vector3 localPosition = new Vector3(xOffset, 0, localZ);
        Vector3 worldPosition = startAnchor.TransformPoint(localPosition);

        SpawnCoinAt(ObstacleType.None, worldPosition);
    }

    private void SpawnCoinAt(ObstacleType obstacleUnderneath, Vector3 baseGroundPosition)
    {
        if (coinPrefabs == null || coinPrefabs.Length == 0) return;

        float targetHeight = obstacleUnderneath switch
        {
            ObstacleType.Jump => coinJumpHeight,
            ObstacleType.Slide => coinSlideHeight,
            _ => coinDefaultHeight
        };

        Vector3 spawnPos = baseGroundPosition + (startAnchor.up * targetHeight);
        GameObject coinPrefab = coinPrefabs[Random.Range(0, coinPrefabs.Length)];
        Quaternion defaultRotation = coinPrefab.transform.rotation;

        SingleObjectPool.Instance.Get(coinPrefab, spawnPos, defaultRotation, transform);
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
            SingleObjectPool.Instance.Get(prefabToSpawn, position, Quaternion.identity, transform);
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