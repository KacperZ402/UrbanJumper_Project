using UnityEngine;

public class PoolableObject : MonoBehaviour
{
    public GameObject prefab;
    public int preloadCount = 1;

    public void Init(GameObject prefab)
    {
        this.prefab = prefab;
    }

    public void ReturnToPool()
    {
        SingleObjectPool.Instance.Return(prefab, gameObject);
    }
}
