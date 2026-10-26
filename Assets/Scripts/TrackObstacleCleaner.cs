using System.Collections;
using UnityEngine;

public class TrackObstacleCleaner : MonoBehaviour
{
    [Header("Punkt odniesienia")]
    [Tooltip("Obiekt gracza, od którego mierzymy czyszczenie w przód")]
    [SerializeField] private Transform originTransform;

    [Header("Parametry strefy czyszczenia")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float defaultClearDistanceZ = 100f;
    [SerializeField] private float clearWidthX = 15f;
    [SerializeField] private float clearHeightY = 6f;

    // Stały bufor w pamięci - zero alokacji dla Garbage Collectora
    private readonly Collider[] hitBuffer = new Collider[64];
    public void Start()
    {
        RequestClearOnStart();
    }

    // Metoda wywoływana np. w momencie kliknięcia PLAY
    public void RequestClearOnStart()
    {
        StartCoroutine(ClearAfterFramesRoutine());
    }

    private IEnumerator ClearAfterFramesRoutine()
    {
        // Czekamy chwilę, aż spawner i silnik fizyczny zbudują początkowe obiekty na scenie
        yield return new WaitForFixedUpdate();
        yield return null;

        ClearZoneAhead(defaultClearDistanceZ);
    }

    public void ClearZoneAhead(float distanceZ)
    {
        if (originTransform == null) return;

        // Wymuszamy synchronizację fizyki
        Physics.SyncTransforms();

        Vector3 startPos = originTransform.position;

        // Środek pudełka przesunięty o połowę dystansu do przodu
        Vector3 boxCenter = new Vector3(
            startPos.x,
            startPos.y + (clearHeightY * 0.5f),
            startPos.z + (distanceZ * 0.5f)
        );

        // Wymiary od środka do krawędzi (Half Extents)
        Vector3 boxHalfExtents = new Vector3(
            clearWidthX * 0.5f,
            clearHeightY * 0.5f,
            distanceZ * 0.5f
        );

        int hitCount = Physics.OverlapBoxNonAlloc(
            boxCenter,
            boxHalfExtents,
            hitBuffer,
            Quaternion.identity,
            obstacleLayer,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < hitCount; i++)
        {
            GameObject hitObj = hitBuffer[i].gameObject;

            // Zwrócenie do puli / wyłączenie przeszkody
            hitObj.SetActive(false);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (originTransform == null) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
        Vector3 startPos = originTransform.position;
        Vector3 center = new Vector3(startPos.x, startPos.y + (clearHeightY * 0.5f), startPos.z + (defaultClearDistanceZ * 0.5f));
        Vector3 size = new Vector3(clearWidthX, clearHeightY, defaultClearDistanceZ);

        Gizmos.DrawCube(center, size);
    }
#endif
}