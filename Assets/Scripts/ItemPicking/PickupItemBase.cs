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
    [SerializeField] private float collectDuration = 0.25f; // Jak szybko znika (ułamki sekund)
    [SerializeField] private float flyUpDistance = 1.5f;     // Ile leci w górę po zebraniu
    [SerializeField] private AudioClip collectSound;        // Opcjonalny dźwięk
    [SerializeField][Range(0f, 1f)] private float soundVolume = 0.8f;

    [Header("Detekcja Gracza")]
    [SerializeField] private string playerTag = "Player";

    private Vector3 basePosition;
    private Vector3 initialScale;
    private Collider itemCollider;
    private bool isCollected = false;

    protected virtual void Awake()
    {
        itemCollider = GetComponent<Collider>();
        itemCollider.isTrigger = true;
        initialScale = transform.localScale;
    }

    protected virtual void OnEnable()
    {
        basePosition = transform.position;
        transform.localScale = initialScale; // Reset skali po wyjęciu z Object Poola
        isCollected = false;

        if (itemCollider != null)
            itemCollider.enabled = true;
    }

    protected virtual void Update()
    {
        if (isCollected) return; // Zatrzymujemy standardowy ruch po zebraniu

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

        if (other.CompareTag(playerTag))
        {
            isCollected = true;

            // Wyłączamy collider natychmiast, żeby nie zaliczyć kolizji 2 razy
            if (itemCollider != null)
                itemCollider.enabled = false;

            // Dźwięk niezależny od zniszczenia obiektu (odpali się w miejscu podniesienia)
            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position, soundVolume);
            }

            // Logika podklasy (punkty, HP)
            OnCollect(other.gameObject);

            // Odpalamy animację zniknięcia
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

            // Szybki obrót w trakcie zbierania dla lepszego efektu
            transform.Rotate(Vector3.up, rotationSpeed * 5f * Time.deltaTime, Space.World);

            // Ruch w górę z wygładzeniem (EaseOut)
            transform.position = Vector3.Lerp(startPos, targetPos, Mathf.Sin(t * Mathf.PI * 0.5f));

            // Zmniejszanie do zera (Fade out bez ruszania materiału)
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            yield return null;
        }

        CollectAndDispose();
    }

    protected abstract void OnCollect(GameObject player);

    protected virtual void CollectAndDispose()
    {
        PoolableObject po = GetComponent<PoolableObject>();
        if (po != null)
        {
            // Zakładam, że PoolableObject ma referencję do oryginału lub metodę ReturnToPool:
            SingleObjectPool.Instance.Return(po.gameObject, gameObject);
            // Jeśli Twój PoolableObject sam tego nie woła, wystarczy:
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}