using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class PickupItem : MonoBehaviour
{
    [Header("Wizualia (Unoszenie)")]
    [SerializeField] private float bobbingAmplitude = 0.25f;
    [SerializeField] private float bobbingFrequency = 1.5f;
    [SerializeField] private float rotationSpeed = 45f;

    [Header("Animacja Podniesienia")]
    [SerializeField] private float collectDuration = 0.25f;
    [SerializeField] private float flyUpDistance = 1.5f;
    [SerializeField] private AudioClip collectSound;
    [SerializeField][Range(0f, 1f)] private float soundVolume = 0.8f;

    private Vector3 basePosition;
    private Vector3 initialScale;
    private Collider itemCollider;
    private PoolableObject poolableObject; // Cache'ujemy raz w Awake!

    private bool isCollected = false;
    private bool basePositionInitialized = false;
    private int playerLayer;

    protected virtual void Awake()
    {
        itemCollider = GetComponent<Collider>();
        itemCollider.isTrigger = true;

        // Pobieramy raz na starcie zamiast przy każdym zebraniu
        poolableObject = GetComponent<PoolableObject>();

        initialScale = transform.localScale;
        playerLayer = LayerMask.NameToLayer("Player");
    }

    protected virtual void OnEnable()
    {
        transform.localScale = initialScale;
        isCollected = false;
        basePositionInitialized = false; // Czekamy 1 klatkę, aż Spawner ustawi pozycję!

        if (itemCollider != null)
            itemCollider.enabled = true;
    }

    protected virtual void Update()
    {
        if (isCollected) return;

        // Zapisujemy basePosition dopiero w pierwszym Update (po tym, jak Pool ustawi już transform.position)
        if (!basePositionInitialized)
        {
            basePosition = transform.position;
            basePositionInitialized = true;
        }

        // Standardowe unoszenie
        float newY = basePosition.y + Mathf.Sin(Time.time * bobbingFrequency) * bobbingAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Standardowy obrót
        if (rotationSpeed > 0f)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        // Porównanie warstwy (int == int) jest szybsze niż CompareTag, 
        // ale zostawiam CompareTag jako fallback, gdybyś nie ustawił warstwy:
        if (other.gameObject.layer == playerLayer || other.CompareTag("Player"))
        {
            isCollected = true;

            if (itemCollider != null)
                itemCollider.enabled = false;

            // Dźwięk przez GameHUD (PlayOneShot zamiast kosztownego PlayClipAtPoint!)
            if (collectSound != null && GameHUD.Instance != null)
            {
                GameHUD.Instance.PlaySound(collectSound, soundVolume);
            }

            OnCollect(other.gameObject);
            StartCoroutine(CollectAnimationRoutine());
        }
    }

    private IEnumerator CollectAnimationRoutine()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * flyUpDistance;
        Vector3 startScale = transform.localScale;

        float elapsed = 0f;

        while (elapsed < collectDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / collectDuration;

            transform.Rotate(Vector3.up, rotationSpeed * 5f * Time.deltaTime, Space.World);
            transform.position = Vector3.Lerp(startPos, targetPos, Mathf.Sin(t * Mathf.PI * 0.5f));
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            yield return null;
        }

        CollectAndDispose();
    }

    protected abstract void OnCollect(GameObject player);

    protected virtual void CollectAndDispose()
    {
        if (poolableObject != null && SingleObjectPool.Instance != null)
        {
            SingleObjectPool.Instance.Return(poolableObject.gameObject, gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}