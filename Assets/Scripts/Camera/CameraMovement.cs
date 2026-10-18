using UnityEngine;

public class RunnerCamera : MonoBehaviour
{
    [Header("Cel")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Płynność w osi Y")]
    [SerializeField] private float ySmoothSpeed = 10f;

    private float yDistance; // Święta odległość w osi Y (kamera <-> stopa gracza)
    private float zDistance; // Stała odległość w osi Z
    private float lockedX;   // Zamrożona oś X
    private float targetY;

    private void Start()
    {
        if (playerTransform == null || playerMovement == null)
        {
            Debug.LogError("Brak referencji do gracza w RunnerCamera!");
            enabled = false;
            return;
        }

        // 1. RAZ na starcie zapisujemy odległość w osi Y,Z oraz X
        yDistance = transform.position.y - playerTransform.position.y;
        zDistance = transform.position.z - playerTransform.position.z;
        lockedX = transform.position.x;
    }

    private void LateUpdate()
    {
        if (playerTransform == null || playerMovement == null) return;
        if (playerMovement.followCameraY)
        {
            // Stan 1 (GlassTrigger): Kamera swobodnie podąża za graczem w pionie
            targetY = playerTransform.position.y + yDistance;
        }
        else
        {
            // Stan 2 (SegmentTrigger): Kamera trzyma sztywny dystans nad poziomem podłogi
            targetY = playerMovement.lockedFloorY + yDistance;
        }

        // Płynne przejście w osi Y (brak szarpnięć przy wejściu w triggery)
        float finalY = Mathf.Lerp(transform.position.y, targetY, ySmoothSpeed * Time.deltaTime);

        // Oś Z zawsze na sztywno za graczem
        float finalZ = playerTransform.position.z + zDistance;

        transform.position = new Vector3(lockedX, finalY, finalZ);
    }
}