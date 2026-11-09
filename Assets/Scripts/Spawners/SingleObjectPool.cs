using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-90)] // Instance gotowy przed Awake/OnEnable innych skryptów
public class SingleObjectPool : MonoBehaviour
{
    public static SingleObjectPool Instance { get; private set; }

    [Header("Prefaby do preloadu")]
    public List<PoolableObject> preloadPrefabs = new();

    // Klucz = sam prefab (bez alokacji stringa jak przy prefab.name)
    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new();

    // Obiekty le¿¹ce aktualnie w puli – ochrona przed podwójnym Return
    private readonly HashSet<GameObject> inPool = new();

    // Nieaktywny kontener – obiekty w nim nie odpalaj¹ Awake/OnEnable
    private Transform storage;

    private void Awake()
    {
        Instance = this; // pool z nowej sceny przejmuje pa³eczkê

        storage = new GameObject("Storage").transform;
        storage.SetParent(transform, false);
        storage.gameObject.SetActive(false);

        foreach (PoolableObject config in preloadPrefabs)
        {
            if (config == null) continue;
            Preload(config.gameObject, config.preloadCount);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Preload(GameObject prefab, int count)
    {
        Queue<GameObject> queue = GetQueue(prefab);

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab, storage);
            obj.SetActive(false);
            InitPoolable(obj, prefab);

            inPool.Add(obj);
            queue.Enqueue(obj);
        }
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null)
        {
            Debug.LogError("[SingleObjectPool] Get() z pustym prefabem.");
            return null;
        }

        Queue<GameObject> queue = GetQueue(prefab);

        while (queue.Count > 0)
        {
            GameObject obj = queue.Dequeue();
            inPool.Remove(obj);
            if (obj == null) continue; // ktoœ zniszczy³ obiekt le¿¹cy w puli

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.transform.SetParent(parent, true); // null = korzeñ sceny
            obj.SetActive(true);
            return obj;
        }

        // Debug.Log($"[SingleObjectPool] Brak w puli, tworzê: {prefab.name}");
        GameObject created = Instantiate(prefab, position, rotation, parent);
        InitPoolable(created, prefab);
        return created;
    }

    public void Return(GameObject prefab, GameObject obj)
    {
        if (obj == null) return;

        if (prefab == null)
        {
            Debug.LogWarning($"[SingleObjectPool] {obj.name} nie ma prefaba – niszczê zamiast oddaæ.", obj);
            Destroy(obj);
            return;
        }

        if (!inPool.Add(obj))
        {
            Debug.LogWarning($"[SingleObjectPool] {obj.name} oddany do puli drugi raz – pomijam.", obj);
            return;
        }

        obj.SetActive(false);
        obj.transform.SetParent(storage, true);
        GetQueue(prefab).Enqueue(obj);
    }

    private Queue<GameObject> GetQueue(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            pools.Add(prefab, queue);
        }
        return queue;
    }

    private static void InitPoolable(GameObject obj, GameObject prefab)
    {
        PoolableObject po = obj.GetComponent<PoolableObject>();
        if (po != null) po.Init(prefab);
    }
}