using UnityEngine;
using System.Collections.Generic;

public class SingleObjectPool : MonoBehaviour
{
    public static SingleObjectPool Instance;

    private Dictionary<string, Queue<GameObject>> pools = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
            Debug.Log("Pobranio obiekt: " + prefab.name);
        }
        else
        {
            obj = Instantiate(prefab, position, rotation, parent);
            Debug.Log("Stworzono obiekt: " + prefab.name);
            PoolableObject po = obj.GetComponent<PoolableObject>();
            if (po != null) po.Init(prefab);
        }

        return obj;
    }

    public void Return(GameObject prefab, GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(this.transform);

        string key = prefab.name;

        if (!pools.ContainsKey(key))
            pools[key] = new Queue<GameObject>();

        pools[key].Enqueue(obj);
    }
}