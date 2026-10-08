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
    public float fallMultiplier = 3.5f;
    public float jumpCutMultiplier = 2f;

    [Header("Slide")]
    public float slideDuration = 0.5f;
    [Range(0.1f, 1f)] public float slideColliderYMultiplier = 0.45f;
    public float slideDownVelocity = 8f;
    //public int startingLane = 1;

    private float lastZPosition;
    [Header("Detekcja Śmierci przez Uderzenie")]
    [SerializeField] private float minExpectedSpeedRatio = 0.5f; // Jeśli prędkość spadnie poniżej 50% normy -> śmierć

    [Header("System Śmierci")]
    [SerializeField] private Collider mainCollider;
    [SerializeField] private Rigidbody mainRb;
    [SerializeField] private GameObject ragdollRoot; // Przeciągnij tu główną kość w Inspektorze

    private bool isDead = false;

    [Header("Jump reset")]
    public string platformTag = "Platform";

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

    private Rigidbody rb;
    private Collider playerCollider;
    private CapsuleCollider capsuleCollider;
    private Animator animator;

    private int currentLane;
    private float laneVelocity;
    private bool jumpRequested;
    private bool slideRequested;
    public bool isSliding;
    public bool canJump;
    public bool isGrounded;

    private readonly HashSet<int> touchingPlatformIds = new HashSet<int>();
    //private Coroutine slideRoutine;
    private float currentSlideTimer = 0f;
    private float baseCapsuleHeight;
    private Vector3 baseCapsuleCenter;
    //private float slideTimer = 0f;
    private int jumpTriggerHash;
    private int slideTriggerHash;
    private int turnLeftTriggerHash;
    private int turnRightTriggerHash;
    private int isGroundedBoolHash;
    private int isFallingBoolHash;
    private int jumpLockBoolHash;
    private int headCoverBoolHash;
    private bool jumpAnimationLock;

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
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            RequestLaneChange(1);

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            RequestLaneChange(-1);

        if (Input.GetKeyDown(KeyCode.Space))
            jumpRequested = true;

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            slideRequested = true;

        //Wyciete z FIXedUpdate
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        // 1. Sprawdzamy FAKTYCZNĄ prędkość z poprzedniej klatki ZANIM nadpiszemy nową
        float actualDeltaZ = rb.position.z - lastZPosition;
        float actualSpeedZ = actualDeltaZ / Time.fixedDeltaTime;

        // Jeżeli powinniśmy biec (moveSpeedZ > 0), ale faktyczny ruch drastycznie spadł (uderzenie w przeszkodę):
        // Dajemy warunek na np. mniej niż połowę prędkości docelowej
        Debug.Log("Actual Speed: " + actualSpeedZ + " Border:" + (moveSpeedZ * minExpectedSpeedRatio));
        if (actualSpeedZ < moveSpeedZ * minExpectedSpeedRatio && lastZPosition != 0f)
        {
            // Gracz wbił się w przeszkodę i fizyka go zablokowała!
            TriggerDeath();
            return;
        }
        
        // Zapamiętujemy aktualną pozycję do sprawdzenia w następnej klatce
        lastZPosition = rb.position.z;

        isGrounded = touchingPlatformIds.Count > 0;

        Vector3 velocity = rb.velocity;

        // 1. Zmiana toru
        float targetX = LaneToWorldX(currentLane);
        float nextX = Mathf.SmoothDamp(rb.position.x, targetX, ref laneVelocity, laneChangeSmoothTime, laneChangeMaxSpeed, Time.fixedDeltaTime);
        velocity.x = (nextX - rb.position.x) / Time.fixedDeltaTime;
        velocity.z = moveSpeedZ;

        // 2. SKOK (Priorytet)
        if (jumpRequested && canJump && isGrounded)
        {
            EndSlide(); // <--- TWARDY RESET WŚLIZGU PRZED SKOKIEM!

            velocity.y = jumpForce;
            canJump = false;
            isGrounded = false;
            jumpAnimationLock = true;

            if (useAnimator && animator != null)
                animator.SetTrigger(jumpTriggerHash);
        }
        jumpRequested = false;

        // 3. WŚLIZG (Tylko jeśli nie jesteśmy w trakcie)
        if (slideRequested && !isSliding)
        {
            StartSlide();
        }
        slideRequested = false;

        // Aktualizujemy prędkość zanim nałożymy modyfikatory grawitacji i wślizgu
        rb.velocity = velocity;

        // 4. Modyfikatory Grawitacji (Lepsze czucie skoku)
        if (rb.velocity.y < 0f)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0f && !Input.GetKey(KeyCode.Space))
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (jumpCutMultiplier - 1f) * Time.fixedDeltaTime;
        }

        // 5. OBSŁUGA CZASU WŚLIZGU I DOCISK DO ZIEMI
        if (isSliding)
        {
            currentSlideTimer -= Time.fixedDeltaTime; // Odliczamy czas

            if (currentSlideTimer <= 0f)
            {
                EndSlide(); // Koniec czasu - wstajemy
            }
            else
            {
                // Trwa wślizg - wymuszamy docisk do podłoża (zapobiega "lataniu" na rampach)
                Vector3 slideVelocity = rb.velocity;
                slideVelocity.y = Mathf.Min(slideVelocity.y, -Mathf.Abs(slideDownVelocity));
                rb.velocity = slideVelocity;
            }
        }

        UpdateAnimatorGroundedState();
    }

    private void TriggerDeath()
    {
        if (isDead) return;
        isDead = true;

        Vector3 ragdollMomentum = new Vector3(0f, mainRb.velocity.y, moveSpeedZ * 0.5f);

        // 1. Wyłączamy Animatora (żeby nie trzymał póz)
        if (animator != null)
            animator.enabled = false;

        // 2. Zabijamy główny collider i zamrażamy główne Rigidbody, żeby trup nie blokował świata
        if (mainCollider != null)
            mainCollider.enabled = false;

        if (mainRb != null)
        {
            mainRb.isKinematic = true;
            mainRb.detectCollisions = false;
        }

        // 3. Włączamy główną kość ragdolla (odpala całą zagnieżdżoną fizykę)
        if (ragdollRoot != null)
        {
            ragdollRoot.SetActive(true);

            // 5. PRZEKAZANIE ENERGII
            // Pobieramy wszystkie rigidbody szmacianki (po jej włączeniu!) 
            // i wstrzykujemy im naszą energię wejścia.
            Rigidbody[] bones = ragdollRoot.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody bone in bones)
            {
                bone.velocity = ragdollMomentum;
            }
        }

        // 4. Wyłączamy ten skrypt (zapobiega to wywoływaniu Update, FixedUpdate i sterowaniu)
        this.enabled = false;
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

    //public void HeadCover()
    //{
    //    if (useAnimator && animator != null)
    //    {
    //        animator.SetTrigger(headCoverBoolHash);
    //    }
    //}

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

    private void OnCollisionEnter(Collision collision)
    {
        if (IsPlatformCollision(collision))
        {
            touchingPlatformIds.Add(collision.collider.GetInstanceID());
            canJump = true;
            isGrounded = true;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (IsPlatformCollision(collision))
        {
            canJump = true;
            isGrounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!IsPlatformCollision(collision))
            return;

        touchingPlatformIds.Remove(collision.collider.GetInstanceID());
        if (touchingPlatformIds.Count == 0)
        {
            canJump = false;
            isGrounded = false;
        }
    }

    private bool IsPlatformCollision(Collision collision)
    {
        return collision.gameObject.CompareTag(platformTag);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(glassTriggerTag))
        {
            // Używamy boola zamiast triggera, żeby animacja trwała dopóki gracz jest w strefie
            if (animator != null)
                animator.SetTrigger(headCoverBoolHash);
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
}