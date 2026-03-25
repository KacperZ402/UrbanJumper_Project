using UnityEngine;
using System.Collections;
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

    [Header("Tory")]
    public int startingLane = 1; // 0 = lewy, 1 = œrodek, 2 = prawy

    [Header("Jump reset")]
    public string platformTag = "Platform";

    [Header("Lane check")]
    public LayerMask laneBlockMask = ~0;
    public float laneCheckHeightScale = 0.6f;

    private Rigidbody rb;
    private Collider playerCollider;
    private CapsuleCollider capsuleCollider;

    private int currentLane;
    private float laneVelocity;
    private bool jumpRequested;
    private bool slideRequested;
    public bool isSliding;
    public bool canJump;
    public bool requireNewPlatformTouchAfterJump;

    private readonly HashSet<int> touchingPlatformIds = new HashSet<int>();
    private Coroutine slideRoutine;
    private float baseCapsuleHeight;
    private Vector3 baseCapsuleCenter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        if (rb != null)
            rb.constraints |= RigidbodyConstraints.FreezeRotation;

        if (capsuleCollider != null)
        {
            baseCapsuleHeight = capsuleCollider.height;
            baseCapsuleCenter = capsuleCollider.center;
        }

        currentLane = Mathf.Clamp(startingLane, 0, 2);
    }

    private void Start()
    {
        SetLanePositionImmediately(currentLane);
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
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        Vector3 velocity = rb.velocity;

        float targetX = LaneToWorldX(currentLane);
        float nextX = Mathf.SmoothDamp(rb.position.x, targetX, ref laneVelocity, laneChangeSmoothTime, laneChangeMaxSpeed, Time.fixedDeltaTime);
        velocity.x = (nextX - rb.position.x) / Time.fixedDeltaTime;

        velocity.z = moveSpeedZ;

        if (jumpRequested && canJump)
        {
            velocity.y = jumpForce;
            canJump = false;
            requireNewPlatformTouchAfterJump = true;
        }
        jumpRequested = false;

        if (slideRequested && !isSliding)
            StartSlide();
        slideRequested = false;

        rb.velocity = velocity;

        if (rb.velocity.y < 0f)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0f && !Input.GetKey(KeyCode.Space))
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (jumpCutMultiplier - 1f) * Time.fixedDeltaTime;
        }

        if (isSliding)
        {
            Vector3 slideVelocity = rb.velocity;
            slideVelocity.y = Mathf.Min(slideVelocity.y, -Mathf.Abs(slideDownVelocity));
            rb.velocity = slideVelocity;
        }
    }

    private void StartSlide()
    {
        if (slideRoutine != null)
            StopCoroutine(slideRoutine);

        slideRoutine = StartCoroutine(SlideRoutine());
    }

    private IEnumerator SlideRoutine()
    {
        isSliding = true;

        if (capsuleCollider != null)
        {
            float newHeight = Mathf.Max(0.1f, baseCapsuleHeight * slideColliderYMultiplier);
            capsuleCollider.height = newHeight;

            Vector3 newCenter = baseCapsuleCenter;
            newCenter.y = baseCapsuleCenter.y * slideColliderYMultiplier;
            capsuleCollider.center = newCenter;
        }

        yield return new WaitForSeconds(slideDuration);

        if (capsuleCollider != null)
        {
            capsuleCollider.height = baseCapsuleHeight;
            capsuleCollider.center = baseCapsuleCenter;
        }

        isSliding = false;
        slideRoutine = null;
    }

    private void RequestLaneChange(int direction)
    {
        int targetLane = Mathf.Clamp(currentLane + direction, 0, 2);
        if (targetLane == currentLane)
            return;

        if (!CanChangeLane(targetLane))
            return;

        currentLane = targetLane;
    }

    private bool CanChangeLane(int targetLane)
    {
        if (playerCollider == null)
            return true;

        Bounds bounds = playerCollider.bounds;
        Vector3 targetPos = new Vector3(LaneToWorldX(targetLane), bounds.center.y, bounds.center.z);

        Vector3 halfExtents = bounds.extents * 0.9f;
        halfExtents.y *= Mathf.Clamp01(laneCheckHeightScale);

        Collider[] hits = Physics.OverlapBox(targetPos, halfExtents, transform.rotation, laneBlockMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit != null && hit.gameObject != gameObject)
                return false;
        }

        return true;
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
            if (!requireNewPlatformTouchAfterJump)
                canJump = true;
            else
            {
                canJump = true;
                requireNewPlatformTouchAfterJump = false;
            }
        }

        if (collision.gameObject.CompareTag("Obstacle"))
            GameOver();
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!IsPlatformCollision(collision))
            return;

        touchingPlatformIds.Remove(collision.collider.GetInstanceID());
        if (touchingPlatformIds.Count == 0)
            canJump = false;
    }

    private bool IsPlatformCollision(Collision collision)
    {
        return collision.gameObject.CompareTag(platformTag);
    }

    private void GameOver()
    {
        Debug.Log("Game Over!");
        enabled = false;
    }
}