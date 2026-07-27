using UnityEngine;

public class OfficeDuoController : MonoBehaviour
{
    [Header("Animators")]
    [SerializeField] private Animator pullWorker;
    [SerializeField] private Animator pushWorker;

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 3f;

    [Header("Collision Detection")]
    [SerializeField] private Vector3 boxSize = new Vector3(2f, 2f, 0.5f);
    [SerializeField] private Vector3 boxOffset = new Vector3(0f, 1f, -1.5f);
    [SerializeField] private float vehicleLength = 3f;

    // Zmienne stanu 
    private bool _isMoving;
    private int _currentDirection = 1;

    void OnEnable()
    {
        bool randomState = Random.value > 0.5f;
        SetState(randomState);
    }

    public void SetState(bool moving)
    {
        _isMoving = moving;

        if (pullWorker != null) pullWorker.SetBool("isMoving", _isMoving);
        if (pushWorker != null) pushWorker.SetBool("isMoving", _isMoving);
    }

    void Update()
    {
        if (_isMoving)
        {
            ApplyMovement();
        }
    }

    private void ApplyMovement()
    {
        Vector3 moveDirection = Vector3.forward * _currentDirection;
        float moveDistance = movementSpeed * Time.deltaTime;
        float totalCastDistance = vehicleLength + moveDistance;

        Vector3 startPosition = transform.TransformPoint(boxOffset);

        bool isBlocked = Physics.BoxCast(
            startPosition,
            boxSize / 2f,
            transform.TransformDirection(moveDirection),
            out RaycastHit hit,
            transform.rotation,
            totalCastDistance
        );

        if (isBlocked)
        {
            Debug.Log("Zatrzymano przed: " + hit.collider.name);
            return;
        }

        transform.Translate(moveDirection * moveDistance);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 moveDirection = Vector3.forward * _currentDirection;

        Vector3 startPosition = transform.TransformPoint(boxOffset);

        Gizmos.matrix = Matrix4x4.TRS(startPosition, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxSize);

        Vector3 localMoveDir = transform.InverseTransformDirection(transform.TransformDirection(moveDirection));
        Gizmos.DrawWireCube(localMoveDir * vehicleLength, boxSize);
    }
}