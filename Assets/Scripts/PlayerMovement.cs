using UnityEngine;

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

    [Header("Tory")]
    public int startingLane = 1; // 0 = lewy, 1 = œrodek, 2 = prawy

    [Header("Ground check")]
    public LayerMask groundMask = ~0;
    public float groundCheckOffset = 0.05f;
    public float groundCheckRadiusScale = 0.4f;

    [Header("Lane check")]
    public LayerMask laneBlockMask = ~0;
    public float laneCheckHeightScale = 0.6f;

    [Header("Animator")]
    public bool disableAnimatorRootMotion = true;
    public bool updateAnimator = true;
    public string speedParam = "Speed";
    public string groundedParam = "IsGrounded";
    public string verticalSpeedParam = "VerticalSpeed";

    private Rigidbody rb;
    private Collider playerCollider;
    private Animator animator;

    private int currentLane;
    private float laneVelocity;
    private bool jumpRequested;
    private bool isGrounded;

    private int speedHash;
    private int groundedHash;
    private int verticalSpeedHash;
    private bool hasSpeedParam;
    private bool hasGroundedParam;
    private bool hasVerticalParam;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        animator = GetComponentInChildren<Animator>();

        if (rb != null)
            rb.constraints |= RigidbodyConstraints.FreezeRotation;

        if (animator != null && disableAnimatorRootMotion)
            animator.applyRootMotion = false;

        currentLane = Mathf.Clamp(startingLane, 0, 2);
        CacheAnimatorParams();
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

        if (updateAnimator)
            UpdateAnimatorState();
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        isGrounded = CheckGrounded();

        Vector3 velocity = rb.velocity;

        float targetX = LaneToWorldX(currentLane);
        float nextX = Mathf.SmoothDamp(rb.position.x, targetX, ref laneVelocity, laneChangeSmoothTime, laneChangeMaxSpeed, Time.fixedDeltaTime);
        velocity.x = (nextX - rb.position.x) / Time.fixedDeltaTime;

        velocity.z = moveSpeedZ;

        if (jumpRequested && isGrounded)
        {
            velocity.y = jumpForce;
            isGrounded = false;
        }
        jumpRequested = false;

        rb.velocity = velocity;

        if (rb.velocity.y < 0f)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0f && !Input.GetKey(KeyCode.Space))
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (jumpCutMultiplier - 1f) * Time.fixedDeltaTime;
        }
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

    private bool CheckGrounded()
    {
        if (playerCollider == null)
            return false;

        Bounds bounds = playerCollider.bounds;
        Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + groundCheckOffset, bounds.center.z);
        float radius = Mathf.Max(0.05f, Mathf.Min(bounds.extents.x, bounds.extents.z) * groundCheckRadiusScale);

        return Physics.CheckSphere(origin, radius, groundMask, QueryTriggerInteraction.Ignore);
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

    private void CacheAnimatorParams()
    {
        if (animator == null)
            return;

        speedHash = Animator.StringToHash(speedParam);
        groundedHash = Animator.StringToHash(groundedParam);
        verticalSpeedHash = Animator.StringToHash(verticalSpeedParam);

        hasSpeedParam = HasAnimatorParameter(speedParam, AnimatorControllerParameterType.Float);
        hasGroundedParam = HasAnimatorParameter(groundedParam, AnimatorControllerParameterType.Bool);
        hasVerticalParam = HasAnimatorParameter(verticalSpeedParam, AnimatorControllerParameterType.Float);
    }

    private bool HasAnimatorParameter(string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrEmpty(paramName))
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == type && parameters[i].name == paramName)
                return true;
        }

        return false;
    }

    private void UpdateAnimatorState()
    {
        if (animator == null)
            return;

        if (hasSpeedParam)
            animator.SetFloat(speedHash, moveSpeedZ);

        if (hasGroundedParam)
            animator.SetBool(groundedHash, isGrounded);

        if (hasVerticalParam)
            animator.SetFloat(verticalSpeedHash, rb != null ? rb.velocity.y : 0f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
            GameOver();
    }

    private void GameOver()
    {
        Debug.Log("Game Over!");
        enabled = false;
    }
}
