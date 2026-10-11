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
    private bool isBetweenBuildings = false;

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

        if (actualSpeedZ < moveSpeedZ * minExpectedSpeedRatio && lastZPosition != 0f)
        {
            TriggerDeath();
            return;
        }
        lastZPosition = rb.position.z;

        // 3.detekcja podłoża na podstawie punkcika pod graczem
        isGrounded = false;
        if (groundCheck != null)
        {
            Collider[] colliders = Physics.OverlapSphere(groundCheck.position, groundDistance);
            foreach (Collider col in colliders)
            {
                // Ignoruje główny collider gracza i triggery (np. monety)
                if (col != mainCollider)
                {
                    isGrounded = true;
                    jumpAvailable = true; // Dotknięcie ładuje skok
                    break;
                }
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
        if (isDead) return;
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
        isBetweenBuildings = false;

        // Wyłączamy ten skrypt (zapobiega to wywoływaniu Update)
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

        }

    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(glassTriggerTag))
        {
            // Gracz rozbił szybę i przez nią przeszedł – opuszczamy ręce
            if (animator != null)
                animator.SetBool(headCoverBoolHash, false);
            isBetweenBuildings = !isBetweenBuildings;
            Debug.Log("isBetweenBuildings: " + isBetweenBuildings);
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