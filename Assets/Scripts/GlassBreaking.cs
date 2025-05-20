using UnityEngine;

public class GlassBreaking : MonoBehaviour
{
    [Header("Prefab i d�wi�k")]
    public GameObject replacementPrefab;   // Prefab rozbitej szyby
    public AudioClip breakSound;           // D�wi�k t�uczenia szk�a

    private bool hasBroken = false;

    private void OnTriggerEnter(Collider collision)
    {
        if (hasBroken) return; // zapobiega wielokrotnemu wywo�aniu

        if (collision.CompareTag("Player"))
        {
            hasBroken = true;

            // Odtw�rz d�wi�k
            if (breakSound != null)
                AudioSource.PlayClipAtPoint(breakSound, transform.position);

            // Podmie� szyb� na inny prefab
            if (replacementPrefab != null)
            {
                Instantiate(replacementPrefab, transform.position, transform.rotation);

            }
            else
            {
                Debug.LogWarning("Brak przypisanego replacementPrefab!");
            }

            // Zniszcz obecn� szyb�
            Destroy(gameObject);
        }
    }
}
