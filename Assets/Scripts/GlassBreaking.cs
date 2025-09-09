using UnityEngine;

public class GlassBreaking : MonoBehaviour
{
    [Header("Prefab i dźwięk")]
    public GameObject replacementPrefab;   // Prefab odłamków szkła
    public AudioClip breakSound;           // Dźwięk tłuczenia szkła

    private bool hasBroken = false;

    private void OnTriggerEnter(Collider collision)
    {
        if (hasBroken) return;

        if (collision.CompareTag("Player"))
        {
            hasBroken = true;

            // Odtwórz dźwięk
            if (breakSound != null)
                AudioSource.PlayClipAtPoint(breakSound, transform.position);

            // Podmień szybę na odłamki, przypisując do tego samego rodzica
            if (replacementPrefab != null)
            {
                GameObject shards = SingleObjectPool.Instance.Get(
                    replacementPrefab,
                    transform.position,
                    transform.rotation,
                    transform.parent // <- tutaj przypisujemy do rodzica
                );
            }
            PoolableObject po = GetComponent<PoolableObject>();
            if (po != null)
                po.ReturnToPool();
            else
                gameObject.SetActive(false);
        }
    }
}
