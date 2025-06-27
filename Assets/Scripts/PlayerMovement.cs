using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Ruch i skok")]
    public float moveSpeedZ = 5f;               // Sta³a prêdkoœæ do przodu
    public float jumpForce = 7f;                // Si³a skoku
    public float laneDistance = 5f;             // Odleg³oœæ miêdzy torami (X)
    public float laneChangeSpeed = 15f;         // Szybkoœæ przesuwania miêdzy torami

    [Header("Modyfikatory skoku")]
    public float fallMultiplier = 3.5f;         // Przyspieszone opadanie
    public float jumpRiseMultiplier = 2f;       // Dodatkowy multiplier gdy spacja nie jest przytrzymana

    [Header("Game Over")]
    private Vector3 lastPosition;
    public float gameOverVelocityThreshold = 1f; // Minimalna prêdkoœæ w osi Z, by uznaæ, ¿e gracz siê porusza
    public float gameOverTimeLimit = 0.01f;           // Czas, przez który prêdkoœæ musi byæ poni¿ej threshold, by zakoñczyæ grê

    private int currentLane = 1;  // 0 = lewy, 1 = œrodek, 2 = prawy
    private float targetX;        // Docelowa pozycja X dla aktualnego toru
    private Rigidbody rb;
    private bool isGrounded = true;


    // Zmienne do monitorowania ruchu w osi Z
    private float stopTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentLane = 1;  // Zak³adamy, ¿e zaczynamy na œrodku
        targetX = transform.position.x;
        lastPosition = transform.position;
    }

    void Update()
    {
        // Obs³uga zmiany torów i skoku – input w Update
        if (Input.GetKeyDown(KeyCode.RightArrow) && currentLane > 0)
        {
            int newLane = currentLane - 1;
            if (CanChangeLane(newLane))
            {
                currentLane = newLane;
                targetX = (currentLane - 1) * laneDistance; // Przy laneDistance = 5 => -5, 0, 5
            }
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) && currentLane < 2)
        {
            int newLane = currentLane + 1;
            if (CanChangeLane(newLane))
            {
                currentLane = newLane;
                targetX = (currentLane - 1) * laneDistance;
            }
        }

        // Skakanie
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            // Zerujemy pionow¹ prêdkoœæ dla spójnego skoku
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }

        // G³adkie przesuwanie miêdzy torami na osi X – ruch niezale¿ny od osi Z
        float newX = Mathf.MoveTowards(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }

    void FixedUpdate()
    {
        // Utrzymanie sta³ej prêdkoœci w osi Z
        Vector3 currentVelocity = rb.velocity;
        currentVelocity.z = moveSpeedZ;
        rb.velocity = new Vector3(currentVelocity.x, currentVelocity.y, currentVelocity.z);

        // Modyfikacja grawitacji – dynamiczny skok
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0 && !Input.GetKey(KeyCode.Space))
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (jumpRiseMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    // Sprawdza, czy w docelowym torze (nowej pozycji X) nie znajduje siê inny obiekt.
    private bool CanChangeLane(int targetLane)
    {
        float targetXPos = (targetLane - 1) * laneDistance;
        Vector3 targetPos = new Vector3(targetXPos, transform.position.y, transform.position.z);
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            // U¿ywamy rozmiaru kolidera jako obszaru do sprawdzenia,
            // zmniejszonego nieco o margines (0.9) by unikn¹æ fa³szywych wykryæ.
            Vector3 halfExtents = col.bounds.extents * 0.9f;
            Collider[] hits = Physics.OverlapBox(targetPos, halfExtents, transform.rotation);
            foreach (Collider hit in hits)
            {
                if (hit.gameObject != gameObject)
                {

                    return false;  // Znaleziono inny obiekt – nie mo¿na zmieniæ toru
                }
            }
        }
        return true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Sprawdzenie czy gracz dotkn¹³ pod³o¿a (do skakania)
        foreach (ContactPoint contact in collision.contacts)
        {
            if (Vector3.Dot(contact.normal, Vector3.up) > 0.5f)
            {
                isGrounded = true;
                break;
            }
        }

        // Sprawdzenie kolizji z przeszkod¹
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            GameOver();
        }
    }
    private void GameOver()
    {
        Debug.Log("Game Over!");
        this.enabled = false;

        // Mo¿esz te¿ dodaæ np. wywo³anie UI, sceny itd.
        // SceneManager.LoadScene("GameOverScene");
    }
}
