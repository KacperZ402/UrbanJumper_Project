using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RotateOnPlayerTrigger : MonoBehaviour
{
    //Skrypt do obrotu obiektu o okreœlony k¹t po wejœciu gracza w trigger. Opcjonalnie mo¿na ustawiæ, aby obrót nastêpowa³ tylko raz.
    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool oneTimeOnly = true;

    [Header("Rotation")]
    [SerializeField] private float yDegrees = -90f;

    private bool triggered;

    private void Reset()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered && oneTimeOnly)
            return;

        if (!other.CompareTag(playerTag))
            return;

        transform.Rotate(0f, yDegrees, 0f, Space.Self);

        if (oneTimeOnly)
            triggered = true;
    }
}