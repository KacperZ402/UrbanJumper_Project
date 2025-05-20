using UnityEngine;

public class GlassBreaking : MonoBehaviour
{
    [Header("Prefab i dŸwiêk")]
    public GameObject replacementPrefab;   // Prefab rozbitej szyby
    public AudioClip breakSound;           // DŸwiêk t³uczenia szk³a

    private bool hasBroken = false;

    private void OnTriggerEnter(Collider collision)
    {
        if (hasBroken) return; // zapobiega wielokrotnemu wywo³aniu

        if (collision.CompareTag("Player"))
        {
            hasBroken = true;

            // Odtwórz dŸwiêk
            if (breakSound != null)
                AudioSource.PlayClipAtPoint(breakSound, transform.position);

            // Podmieñ szybê na inny prefab
            if (replacementPrefab != null)
            {
                Instantiate(replacementPrefab, transform.position, transform.rotation);

            }
            else
            {
                Debug.LogWarning("Brak przypisanego replacementPrefab!");
            }

            // Zniszcz obecn¹ szybê
            Destroy(gameObject);
        }
    }
}
