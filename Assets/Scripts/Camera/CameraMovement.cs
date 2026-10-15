using UnityEngine;

public class RunnerCamera : MonoBehaviour
{
    [Header("Cel")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Ustawienia osi Y (Góra / Dół)")]
    [SerializeField] private float ySmoothSpeed = 8f; // Szybkość płynnego podążania w pionie

    private Vector3 initialOffset;
    private float lockedX;
    private float lockedY;
    private bool wasBetweenBuildings = false;

    private void Start()
    {
        if (playerTransform == null)
        {
            Debug.LogError("Nie przypisano gracza do kamery!");
            enabled = false;
            return;
        }

        // Zapamiętujemy startowy dystans kamery względem gracza (ustawiony ręcznie na scenie)
        initialOffset = transform.position - playerTransform.position;

        // Blokujemy startowe osie X oraz Y
        lockedX = transform.position.x;
        lockedY = transform.position.y;
    }

    private void LateUpdate()
    {
        if (playerTransform == null || playerMovement == null) return;

        bool isFreeY = playerMovement.isBetweenBuildings;

        // Moment WYJŚCIA z drugiego GlassTriggera (powrót na twardy dach)
        if (wasBetweenBuildings && !isFreeY)
        {
            // Zamrażamy kamerę na wysokości NOWEGO budynku + nasz domyślny offset wysokości
            lockedY = playerTransform.position.y + initialOffset.y;
        }

        wasBetweenBuildings = isFreeY;

        // 1. Oś Z: Zawsze podąża na sztywno za graczem
        float targetZ = playerTransform.position.z + initialOffset.z;

        // 2. Oś X: Całkowicie zamrożona na środku toru
        float targetX = lockedX;

        // 3. Oś Y: Zależna od tego, czy lecimy między budynkami
        float currentTargetY;
        if (isFreeY)
        {
            // Odmrożona: Kamera podąża za wysokością gracza z zachowaniem startowego odstępu
            currentTargetY = playerTransform.position.y + initialOffset.y;
        }
        else
        {
            // Zamrożona: Kamera trzyma stały pułap dachu (ignoruje zwykłe podskoki)
            currentTargetY = lockedY;
        }

        // Płynne dociąganie osi Y (żeby przy wpadnięciu w szybę kamera nie szarpała ekranem)
        float finalY = Mathf.Lerp(transform.position.y, currentTargetY, ySmoothSpeed * Time.deltaTime);

        // Aplikujemy finalną pozycję (X = stały, Y = zamrożony lub płynny, Z = 1:1 za graczem)
        transform.position = new Vector3(targetX, finalY, targetZ);
    }

    // Wywołaj to pod koniec metody Revive() w graczu, jeśli odradzasz się na innej wysokości!
    public void SnapToPlayerAfterRevive()
    {
        lockedY = playerTransform.position.y + initialOffset.y;
        transform.position = new Vector3(lockedX, lockedY, playerTransform.position.z + initialOffset.z);
    }
}