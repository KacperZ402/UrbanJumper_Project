using UnityEngine;

public class ObstacleSpawnSystem : MonoBehaviour
{
    [Header("Ustawienia Torów")]
    public Transform startAnchor; // Pusty obiekt na początku (X:0, Y:0, Z:0 lokalnie)
    public float segmentLength = 50f; // Jak długa jest ta platforma w osi Z
    public float laneWidth = 10f;    // Odstęp między torami

    // Szybki sposób na pobranie pozycji konkretnego toru na początku platformy
    public Vector3 GetLaneStartPoint(int laneIndex)
    {
        // laneIndex: -1 (lewy), 0 (środek), 1 (prawy)
        float xOffset = laneIndex * laneWidth;
        return startAnchor.TransformPoint(new Vector3(xOffset, 0, 0));
    }

    // Szybki sposób na pobranie pozycji konkretnego toru na końcu platformy
    public Vector3 GetLaneEndPoint(int laneIndex)
    {
        float xOffset = laneIndex * laneWidth;
        // Dodajemy długość w osi Z
        return startAnchor.TransformPoint(new Vector3(xOffset, 0, segmentLength));
    }

    // Rysowanie pomocniczych linii w Editorze (Gizmos)
    private void OnDrawGizmos()
    {
        if (startAnchor == null) return;

        Gizmos.color = Color.cyan;
        for (int i = -1; i <= 1; i++)
        {
            Vector3 start = GetLaneStartPoint(i);
            Vector3 end = GetLaneEndPoint(i);
            
            // Rysujemy linie torów
            Gizmos.DrawLine(start, end);
            // Rysujemy punkty końcowe
            Gizmos.DrawWireSphere(end, 0.5f);
        }
    }
}