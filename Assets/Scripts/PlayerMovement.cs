using UnityEngine;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour
{
    [Header("Ruch")]
    public float moveSpeedZ = 10f;
    public float laneDistance = 3f;
    public float laneChangeSmoothTime = 0.08f;
    public float laneChangeMaxSpeed = 25f;

    [Header("Skok")]
    public float jumpForce = 8f;
    public float riseMultiplier = 2.5f;
    public float fallMultiplier = 3.5f;
    [Header("Lot między budynkami")]
    public float floatyRiseMultiplier = 1.1f; // Powolne wznoszenie
    public float floatyFallMultiplier = 1.5f; // Powolne opadanie
    public bool isBetweenBuildings { get; private set; }

    [Header("Detekcja Podłoża")]
    [SerializeField] private Transform groundCheck; // Tu podepniesz ten swój punkcik pod graczem
    [SerializeField] private float groundDistance = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.35f;

    private float jumpBufferCounter;

    [Header("Slide")]
    public float slideDuration = 0.5f;
    [Range(0.1f, 1f)] public float slideColliderYMultiplier = 0.45f;
    public float slideDownVelocity = 8f;

    private float lastZPosition;
    [Header("Detekcja Śmierci przez Uderzenie")]
    [SerializeField] private float minExpectedSpeedRatio = 0.5f; // Jeśli prędkość spadnie poniżej 50% normy -> śmierć

    [Header("System Śmierci")]
    [SerializeField] private Collider mainCollider;
    [SerializeField] private Rigidbody mainRb;
    [SerializeField] private GameObject ragdollRoot; // Przeciągnij tu główną kość w Inspektorze
    private bool isDead = false;
    private bool isInvincible = false;

    [Header("Wskrzeszenie (Revive)")]
    [SerializeField] private LayerMask obstacleLayer; // Ustawisz tu warstwę "Obstacle"
    [SerializeField] private float clearObstacleRadius = 15f;
    [SerializeField] private float reviveHeightOffset = 5f; // Zwiększ to, żeby spadał z wyższego pułapu
    [SerializeField] private float invincibilityDuration = 2f;
    private Vector3 lastPlatformPosition; // Trzyma pivot platformy
    [SerializeField] private Renderer[] playerRenderers; // Wrzuć tu wszystkie meshe postaci
    [SerializeField] private float blinkInterval = 0.15f; // Szybkość migania (w sekundach)
    [SerializeField]private float fastBlinkInterval = 0.1f;
    private Collider[] groundColliders = new Collider[5]; // Pamięć na optymalny detektor podłoża

    [Header("Animator")]
    public bool useAnimator = true;
    public string jumpTriggerName = "Jump";
    public string slideTriggerName = "Slide";
    public string turnLeftTriggerName = "TurnLeft";
    public string turnRightTriggerName = "TurnRight";
    public string isGroundedBoolName = "IsGrounded";
    public string jumpStateTag = "Jump"; // Ustaw tag "Jump" na stanie animacji skoku
    public string isFallingBoolName = "IsFalling";
    public string jumpLockBoolName = "JumpLock";
    public string HeadCoverBoolName = "HeadCover";
    private string glassTriggerTag = "GlassTrigger";
    private string seqTriggerTag = "SegTrigger";

    private Rigidbody rb;
    private Collider playerCollider;
    private CapsuleCollider capsuleCollider;
    private Animator animator;

    private int currentLane;
    private float laneVelocity;
    private bool slideRequested;
    public bool jumpAvailable;
    public bool isSliding;
    public bool isGrounded;

    private float currentSlideTimer = 0f;
    private float baseCapsuleHeight;
    private Vector3 baseCapsuleCenter;
    private int jumpTriggerHash;
    private int slideTriggerHash;
    private int turnLeftTriggerHash;
    private int turnRightTriggerHash;
    private int isGroundedBoolHash;
    private int isFallingBoolHash;
    private int jumpLockBoolHash;
    private int headCoverBoolHash;
    private bool jumpAnimationLock;

    private float actualDeltaZ;
    private float actualSpeedZ;

    private float targetX;
    private float nextX;
    private Collider currentFloorCollider;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        animator = GetComponentInChildren<Animator>();

        if (rb != null)
            rb.constraints |= RigidbodyConstraints.FreezeRotation;

        if (capsuleCollider != null)
        {
            baseCapsuleHeight = capsuleCollider.height;
            baseCapsuleCenter = capsuleCollider.center;
        }

        currentLane = Mathf.Clamp(1, 0, 2);

        jumpTriggerHash = Animator.StringToHash(jumpTriggerName);
        slideTriggerHash = Animator.StringToHash(slideTriggerName);
        turnLeftTriggerHash = Animator.StringToHash(turnLeftTriggerName);
        turnRightTriggerHash = Animator.StringToHash(turnRightTriggerName);
        isGroundedBoolHash = Animator.StringToHash(isGroundedBoolName);
        isFallingBoolHash = Animator.StringToHash(isFallingBoolName);
        jumpLockBoolHash = Animator.StringToHash(jumpLockBoolName);
        headCoverBoolHash = Animator.StringToHash(HeadCoverBoolName);
        
    }

    private void Start()
    {
        SetLanePositionImmediately(currentLane);
        UpdateAnimatorGroundedState();
    }

    private void Update()
    {

        if (isDead && Input.GetKeyDown(KeyCode.R))
        {
            Revive();
            return;
        }

        // Jeśli jest martwy, blokujemy całą resztę sterowania
        if (isDead) return;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            RequestLaneChange(1);

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            RequestLaneChange(-1);

        if (Input.GetKeyDown(KeyCode.Space))
            jumpBufferCounter = jumpBufferTime;

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            slideRequested = true;

        //Wyciete z FIXedUpdate
    }

    private void FixedUpdate()
    {
        // 1. Zbijanie bufora skoku
        if (jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.fixedDeltaTime;
        }
        // 2. Detekcja zderzenia (śmierć)
        actualDeltaZ = rb.position.z - lastZPosition;
        actualSpeedZ = actualDeltaZ / Time.fixedDeltaTime;
        if (!isInvincible &&actualSpeedZ < moveSpeedZ * minExpectedSpeedRatio && lastZPosition != 0f)
        {
            TriggerDeath();
            return;
        }
        lastZPosition = rb.position.z;

        // 3.detekcja podłoża na podstawie punkcika pod graczem
        isGrounded = false;
            int hitCount = Physics.OverlapSphereNonAlloc(groundCheck.position, groundDistance, groundColliders);
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = groundColliders[i];
                if (col != mainCollider && !col.isTrigger)
                {
                    isGrounded = true;
                    jumpAvailable = true;
                    
                    // Zapisuje platformę TYLKO gdy wejdziemy na NOWY kawałek podłogi
                    if (!isBetweenBuildings && col != currentFloorCollider)
                    {
                        lastPlatformPosition = col.transform.position;
                        currentFloorCollider = col; // Zapamiętuje ten kawałek, żeby go nie nadpisywać co klatkę
                    }
                    break;
                }
            }

        Vector3 velocity = rb.velocity;

        // 4. Zmiana toru
        targetX = LaneToWorldX(currentLane);
        nextX = Mathf.SmoothDamp(rb.position.x, targetX, ref laneVelocity, laneChangeSmoothTime, laneChangeMaxSpeed, Time.fixedDeltaTime);
        velocity.x = (nextX - rb.position.x) / Time.fixedDeltaTime;
        velocity.z = moveSpeedZ;

        // 5. skok (na podstawie bufora)
        if (jumpBufferCounter > 0f && jumpAvailable)
        {
            EndSlide(); // Przerywamy wślizg jeśli był aktywny

            velocity.y = 0f; // Zerujemy pęd przed wybiciem w powietrzu
            velocity.y = jumpForce;

            jumpBufferCounter = 0f; // Zużyto bufor
            jumpAvailable = false;  // Zużyto prawo do skoku
            isGrounded = false;
            jumpAnimationLock = true;

            if (useAnimator && animator != null)
                animator.SetTrigger(jumpTriggerHash);
        }

        // 6. WŚLIZG
        if (slideRequested && !isSliding)
        {
            StartSlide();
            velocity.y = -Mathf.Abs(slideDownVelocity);
        }
        slideRequested = false;

        // Aktualizujemy prędkość zanim nałożymy modyfikatory grawitacji i wślizgu
        rb.velocity = velocity;

        // 7. Modyfikatory Grawitacji
        if (rb.velocity.y <= 0f)
        {
            // Spadanie 
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else
        {
            // WZNOSZENIE (wciśnięta spacja) - to jest ten Twój szybki skok
            rb.velocity += Vector3.up * Physics.gravity.y * (riseMultiplier - 1f) * Time.fixedDeltaTime;
        }

        // 8. OBSŁUGA CZASU WŚLIZGU I DOCISK DO ZIEMI
        if (isSliding)
        {
            currentSlideTimer -= Time.fixedDeltaTime;
            if (currentSlideTimer <= 0f)
            {
                EndSlide();
            }
        }

        UpdateAnimatorGroundedState();
    }

    private void TriggerDeath()
    {
        if (isDead || isInvincible) return;
        isDead = true;

        Vector3 ragdollMomentum = new Vector3(0f, mainRb.velocity.y, moveSpeedZ * 0.5f);

        // Wyłączamy Animatora (żeby nie trzymał póz)
        if (animator != null)
            animator.enabled = false;

        // Zabijamy główny collider i zamrażamy główne Rigidbody
        if (mainCollider != null)
            mainCollider.enabled = false;

        if (mainRb != null)
        {
            mainRb.isKinematic = true;
            //mainRb.detectCollisions = false;
        }

        //  Włączamy główną kość ragdolla
        if (ragdollRoot != null)
        {
            ragdollRoot.SetActive(true);

            Rigidbody[] bones = ragdollRoot.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody bone in bones)
            {
                bone.velocity = ragdollMomentum;
            }
        }
        riseMultiplier = 3.5f;
        fallMultiplier = 3f;

        // Wyłączamy ten skrypt (zapobiega to wywoływaniu Update)
        //this.enabled = false;
    }

    public void Revive()
    {
        if (!isDead) return;

        // 1. TWARDY RESET PASA I PĘDU
        laneVelocity = 0f; // Zabija resztki pędu z poprzedniego pasa!
        currentLane = 1;

        // Składamy pozycję na środek obecnej platformy
        Vector3 revivePosition = lastPlatformPosition;
        revivePosition.x = LaneToWorldX(currentLane);
        revivePosition.y += reviveHeightOffset;

        // 2. TWARDA TELEPORTACJA FIZYKI (nie samego Transform!)
        if (mainRb != null)
        {
            mainRb.isKinematic = true; // Usypiamy fizykę, żeby nami nie szarpała
            transform.position = revivePosition;
            mainRb.position = revivePosition; // Przesuwamy właściwe ciało
            mainRb.velocity = Vector3.zero;
        }

        // 3. Czyszczenie strefy zrzutu
        ClearObstaclesAround(revivePosition);

        // 4. Sprzątanie trupa i pobudka fizyki
        if (ragdollRoot != null)
            ragdollRoot.SetActive(false);

        if (mainRb != null)
        {
            mainRb.isKinematic = false; // Budzimy fizykę w nowym miejscu
            mainRb.detectCollisions = true;
        }

        if (mainCollider != null)
            mainCollider.enabled = true;

        if (animator != null)
            animator.enabled = true;

        // 5. Reset flag systemowych
        isDead = false;
        EndSlide();
        jumpAvailable = false;
        isGrounded = false;
        jumpAnimationLock = false;
        jumpBufferCounter = 0f;

        riseMultiplier = 3.5f;
        fallMultiplier = 3f;
        isBetweenBuildings = false;

        // 6. Zabezpieczenie przed natychmiastowym "death-loopem"
        lastZPosition = revivePosition.z;

        //this.enabled = true;
        StartCoroutine(InvincibilityRoutine());

    }
    private void ClearObstaclesAround(Vector3 centerPosition)
    {
        // Tu nie trzeba NonAlloc, bo wywołujemy to tylko raz po obejrzeniu reklamy
        Collider[] obstacles = Physics.OverlapSphere(centerPosition, clearObstacleRadius, obstacleLayer);
        foreach (Collider obs in obstacles)
        {
            obs.gameObject.SetActive(false); // Wyłącza (nie niszczy!), żeby pooling działał
        }
    }

    private System.Collections.IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        
        int playerLayer = LayerMask.NameToLayer("Player");
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");

        // Wyłączamy kolizję między Player a Obstacle
        if (playerLayer != -1 && obstacleLayer != -1)
        {
            Physics.IgnoreLayerCollision(playerLayer, obstacleLayer, true);
        }

        float timer = 0f;
        bool isVisible = true;
        float fastBlinkThreshold = invincibilityDuration * 0.7f;

        WaitForSeconds normalWait = new WaitForSeconds(blinkInterval);
        WaitForSeconds fastWait = new WaitForSeconds(fastBlinkInterval);

        // Pętla migania postaci
        while (timer < invincibilityDuration)
        {
            isVisible = !isVisible;
            
            foreach (Renderer r in playerRenderers)
            {
                if (r != null) r.enabled = isVisible;
            }
            Debug.Log(timer);
            // Sprawdzamy, czy weszliśmy w ostatnie 30% czasu
            if (timer >= fastBlinkThreshold)
            {
                yield return fastWait;
                timer += fastBlinkInterval;
            }
            else
            {
                yield return normalWait;
                timer += blinkInterval;
            }
        }

        // Upewniamy się, że model jest widoczny po zakończeniu
        foreach (Renderer r in playerRenderers)
        {
            if (r != null) r.enabled = true;
        }
        
        // Przywracamy kolizję z powrotem
        if (playerLayer != -1 && obstacleLayer != -1)
        {
            Physics.IgnoreLayerCollision(playerLayer, obstacleLayer, false);
        }
        
        isInvincible = false;
    }

    private void StartSlide()
    {
        isSliding = true;
        currentSlideTimer = slideDuration; // Zaczynamy odliczanie

        if (useAnimator && animator != null)
            animator.SetTrigger(slideTriggerHash);

        // Błyskawiczna zmiana collidera
        if (capsuleCollider != null)
        {
            float newHeight = Mathf.Max(0.1f, baseCapsuleHeight * slideColliderYMultiplier);
            capsuleCollider.height = newHeight;

            Vector3 newCenter = baseCapsuleCenter;
            newCenter.y = baseCapsuleCenter.y * slideColliderYMultiplier;
            capsuleCollider.center = newCenter;
        }
    }

    public void EndSlide()
    {
        if (!isSliding) return; // Zapobiega podwójnemu resetowaniu

        isSliding = false;
        currentSlideTimer = 0f;

        // Błyskawiczny powrót collidera
        if (capsuleCollider != null)
        {
            capsuleCollider.height = baseCapsuleHeight;
            capsuleCollider.center = baseCapsuleCenter;
        }
    }

    private void RequestLaneChange(int direction)
    {
        int targetLane = Mathf.Clamp(currentLane + direction, 0, 2);
        if (targetLane == currentLane)
            return;

        if (useAnimator && animator != null)
        {
            if (direction > 0)
                animator.SetTrigger(turnRightTriggerHash);
            else
                animator.SetTrigger(turnLeftTriggerHash);
        }

        currentLane = targetLane;
    }


    private void UpdateAnimatorGroundedState()
    {
        if (!useAnimator || animator == null)
            return;

        if (jumpAnimationLock)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            bool inJumpState = state.IsTag(jumpStateTag);

            if (inJumpState)
            {
                if (state.normalizedTime >= 0.98f && !animator.IsInTransition(0))
                    jumpAnimationLock = false;
            }
            else
            {
                jumpAnimationLock = false;
            }
        }

        bool animatorGrounded = isGrounded || jumpAnimationLock;
        bool animatorFalling = !isGrounded && !jumpAnimationLock;

        animator.SetBool(isGroundedBoolHash, animatorGrounded);
        animator.SetBool(isFallingBoolHash, animatorFalling);
        animator.SetBool(jumpLockBoolHash, jumpAnimationLock);
    }

    private float LaneToWorldX(int lane)
    {
        return (lane - 1) * laneDistance;
    }

    private void SetLanePositionImmediately(int lane)
    {
        Vector3 pos = transform.position;
        pos.x = LaneToWorldX(lane);
        transform.position = pos;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(glassTriggerTag))
        {
            // Używamy boola zamiast triggera, żeby animacja trwała dopóki gracz jest w strefie
            if (animator != null)
                animator.SetBool(headCoverBoolHash, true);
            isBetweenBuildings = !isBetweenBuildings;
            if (isBetweenBuildings)
            {
                riseMultiplier = floatyRiseMultiplier;
                fallMultiplier = floatyFallMultiplier;
            }
            else
            {
                riseMultiplier = 3.5f;
                fallMultiplier = 3f;
            }

        }
        if (other.CompareTag(seqTriggerTag))
        {
            // Zapisujemy pozycję samego Triggera jako punkt odrodzenia
            lastPlatformPosition = other.transform.position;
        }

    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(glassTriggerTag))
        {
            // Gracz rozbił szybę i przez nią przeszedł – opuszczamy ręce
            if (animator != null)
                animator.SetBool(headCoverBoolHash, false);
        }
    }
    //private void OnDrawGizmosSelected()
    //{
    //    if (groundCheck != null)
    //    {
    //        Gizmos.color = Color.green;
    //        Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
    //    }
    //}
}