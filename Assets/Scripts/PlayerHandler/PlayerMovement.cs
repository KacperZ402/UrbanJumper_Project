using UnityEngine;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour
{
    [Header("Ruch")]
    public float moveSpeedZ = 50f;
    [SerializeField] private float maxMoveSpeedZ = 100f;
    [SerializeField] private float speedStepDistanceZ = 1000f; // 200 pkt * 5
    [SerializeField] private float speedIncreasePerStep = 5.0f;
    public float laneDistance = 3f;
    public float laneChangeSmoothTime = 0.08f;
    public float laneChangeMaxSpeed = 25f;
    [SerializeField] private float laneSpeedIncrease = 1.5f;
    private float startMoveSpeedZ;
    private float nextSpeedMilestoneZ;
    private bool isMaxSpeedReached = false;
    private Vector3 lastPlatformPosition;

    [Header("Skalowanie Animacji Biegu")]
    [SerializeField] private float maxRunAnimMultiplier = 1.35f;


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
    [SerializeField] private float slideDownIncrease = 2.0f;

    [Header("Obrót przy zmianie pasa")]
    [SerializeField] private Transform visualRoot;            // model postaci (puste = obiekt z Animatorem)
    [SerializeField] private float laneTurnAngle = 30f;       // maks. skręt w stopniach
    [SerializeField] private float laneTurnSharpness = 15f;   // jak szybko model dochodzi do kąta

    private float currentTurnYaw;
    private Quaternion visualBaseRotation;

    private float lastZPosition;
    [Header("Detekcja Śmierci przez Uderzenie")]

    [Header("System Śmierci")]
    [SerializeField] private Collider mainCollider;
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
    private readonly int runSpeedMultHash = Animator.StringToHash("RunSpeedMultiplier");
    public bool followCameraY { get; private set; } = false;
    public float lockedFloorY { get; private set; }

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

        if (visualRoot == null && animator != null)
            visualRoot = animator.transform;

        if (visualRoot != null)
            visualBaseRotation = visualRoot.localRotation;
        lockedFloorY = transform.position.y;
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
        startMoveSpeedZ = moveSpeedZ;
        nextSpeedMilestoneZ = transform.position.z + speedStepDistanceZ;

        if (useAnimator && animator != null)
        {
            animator.SetFloat(runSpeedMultHash, 1f);
        }
    }

    private void Update()
    {
        if (PauseAndNavigationMenu.IsGamePaused) return;

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
        lastZPosition = rb.position.z;

        UpdateSpeedByDistance();
        //Debug.Log(moveSpeedZ);

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
        if (slideRequested)
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
    private void LateUpdate()
    {
        if (visualRoot == null) return;

        // pełny skręt przez pierwszą połowę zmiany pasa, potem płynny powrót do prosta
        float offset = LaneToWorldX(currentLane) - transform.position.x;
        float halfLane = Mathf.Max(0.01f, laneDistance * 0.5f);
        float targetYaw = Mathf.Clamp(offset / halfLane, -1f, 1f) * laneTurnAngle;

        float t = 1f - Mathf.Exp(-laneTurnSharpness * Time.deltaTime);
        currentTurnYaw = Mathf.Lerp(currentTurnYaw, targetYaw, t);

        visualRoot.localRotation = Quaternion.Euler(0f, currentTurnYaw, 0f) * visualBaseRotation;
    }

    private void UpdateSpeedByDistance()
    {
        if (isMaxSpeedReached) return;

        if (rb.position.z >= nextSpeedMilestoneZ)
        {
            nextSpeedMilestoneZ += speedStepDistanceZ;
            moveSpeedZ = Mathf.Min(moveSpeedZ + speedIncreasePerStep, maxMoveSpeedZ);

            // 2. Podbijamy bezpośrednio maxSpeed zmiany pasa
            laneChangeMaxSpeed += laneSpeedIncrease;

            // 3. Podbijamy siłę docisku wślizgu do ziemi
            slideDownVelocity += slideDownIncrease;

            // Aktualizujemy animację TYLKO raz na 200 punktów!
            if (useAnimator && animator != null)
            {
                // Obliczamy na jakim procencie między startową a maksymalną prędkością jesteśmy (0.0 -> 1.0)
                float t = (moveSpeedZ - startMoveSpeedZ) / (maxMoveSpeedZ - startMoveSpeedZ);

                // Płynnie skalujemy mnożnik nóg od 1.0 do np. 1.35

                float newAnimMult = Mathf.Lerp(1f, maxRunAnimMultiplier, t);
                animator.SetFloat(runSpeedMultHash, newAnimMult);
                Debug.Log(newAnimMult);
            }

            if (moveSpeedZ >= maxMoveSpeedZ)
            {
                isMaxSpeedReached = true;
            }
        }
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

    public Vector3 RespawnPoint => new Vector3(LaneToWorldX(1), lastPlatformPosition.y, lastPlatformPosition.z);

    // Wołane przez PlayerLife przy wskrzeszeniu (już po teleportacji gracza)
    public void ResetForRevive()
    {
        laneVelocity = 0f;
        currentLane = 1;
        EndSlide();
        jumpAvailable = false;
        isGrounded = false;
        jumpAnimationLock = false;
        jumpBufferCounter = 0f;

        riseMultiplier = 3.5f;
        fallMultiplier = 3f;
        isBetweenBuildings = false;
        followCameraY = false;
        lockedFloorY = transform.position.y;
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
            followCameraY = true;

        }
        if (other.CompareTag(seqTriggerTag))
        {
            // Zapisujemy pozycję samego Triggera jako punkt odrodzenia
            lastPlatformPosition = other.transform.position;
            lockedFloorY = other.transform.position.y;
            followCameraY = false;
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