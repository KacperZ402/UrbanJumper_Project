using UnityEngine;
using System.Collections.Generic;

public class SingleObjectSpawner : MonoBehaviour
{
    [Header("Lista prefabów")]
    public List<GameObject> prefabs;

    [Header("Szansa na respawn (0–1)")]
    [Range(0f, 1f)]
    public float spawnChance = 1f;

    [Header("Ignoruj SpawnBlockery")]
    public bool ignoreSpawnBlockers = false;

    [Header("Layer, które nie są czyszczone")]
    public string keepLayerName = "Keep";

    private int keepLayer;

    private void Awake()
    {
        keepLayer = LayerMask.NameToLayer(keepLayerName);
    }

    private void OnEnable()
    {
        // Najpierw wyczyść poprzednie dzieci
        ClearChildren();

        // Losowa szansa
        if (Random.value > spawnChance) return;

        // Sprawdzenie blockerów (jeśli nie ignorujemy)
        if (!ignoreSpawnBlockers && IsBlocked()) return;

        // Spawn nowego obiektu
        SpawnObject();
    }

    private void ClearChildren()
    {
        // kopiujemy listę, żeby nie leciało "na żywo" po hierarchii
        List<Transform> children = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
            children.Add(transform.GetChild(i));

        foreach (Transform child in children)
        {
            if (child.gameObject.layer != keepLayer)
            {
                PoolableObject po = child.GetComponent<PoolableObject>();
                if (po != null)
                {
                    po.ReturnToPool();
                }
                else
                {
                    Destroy(child.gameObject); // fallback gdyby coś nie było poolowane
                }
            }
        }
    }

    private bool IsBlocked()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.1f);

        foreach (var hit in hits)
        {
            if (hit.GetComponent<SpawnBlocker>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private void SpawnObject()
    {
        if (prefabs == null || prefabs.Count == 0)
        {
            Debug.LogWarning($"[SingleObjectSpawner] Brak prefabów przy {gameObject.name}");
            return;
        }

        GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];

        // pobieramy obiekt z puli i ustawiamy jako dziecko spawnera
        GameObject spawned = SingleObjectPool.Instance.Get(
            prefab,
            transform.position,
            transform.rotation,
            this.transform // <<< WAŻNE – dziecko spawnera
        );

        // resetujemy skalę do tej z prefab’a (ważne np. dla budynków)
        spawned.transform.localScale = prefab.transform.localScale;

        spawned.SetActive(true);
    }
}