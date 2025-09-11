using UnityEngine;
using System.Collections.Generic;

public class SingleObjectPool : MonoBehaviour
{
    public static SingleObjectPool Instance;

    [Header("Prefaby do preloadu")]
    public List<PoolableObject> preloadPrefabs = new();

    private Dictionary<string, Queue<GameObject>> pools = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Preload na podstawie listy
        foreach (var config in preloadPrefabs)
        {
            if (config.prefab == null) continue;
            Preload(config.prefab, config.preloadCount);
        }
    }

    private void Preload(GameObject prefab, int count)
    {
        string key = prefab.name;

        if (!pools.ContainsKey(key))
            pools[key] = new Queue<GameObject>();

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            obj.transform.SetParent(transform);

            PoolableObject po = obj.GetComponent<PoolableObject>();
            if (po != null) po.Init(prefab);

            pools[key].Enqueue(obj);
        }

        Debug.Log($"Preload: {prefab.name} x{count}");
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        string key = prefab.name;

        if (!pools.ContainsKey(key))
            pools[key] = new Queue<GameObject>();

        GameObject obj;
        if (pools[key].Count > 0)
        {
            obj = pools[key].Dequeue();
            obj.transform.SetPositionAndRotation(position, rotation);
            if (parent != null) obj.transform.SetParent(parent);
            obj.SetActive(true);
            Debug.Log("Pobrano: "+ prefab.name + "Pozosta³o" + pools[key].Count);
        }
        else
        {
            obj = Instantiate(prefab, position, rotation, parent);
            PoolableObject po = obj.GetComponent<PoolableObject>();
            if (po != null) po.Init(prefab);
            Debug.Log($"[Pooling] Brak w puli, stworzono nowy: {prefab.name}");
        }

        return obj;
    }

    public void Return(GameObject prefab, GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(transform);

        string key = prefab.name;

        if (!pools.ContainsKey(key))
            pools[key] = new Queue<GameObject>();

        pools[key].Enqueue(obj);
    }
}