using UnityEngine;

public class SegmentReturnTrigger : MonoBehaviour
{
    [Header("Gracz")]
    public string playerTag = "Player";

    [Header("Layery, które maj¹ zostaæ w scenie")]
    public string keepLayerName = "Keep";

    private int keepLayer;

    private void Awake()
    {
        keepLayer = LayerMask.NameToLayer(keepLayerName);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // Segment = rodzic triggera
        Transform segmentTransform = transform.parent;
        if (segmentTransform != null)
        {
            // 1. Zwróæ wszystkie dzieci
            ReturnChildProps(segmentTransform);
            // 2. Zniszcz Segment
            Destroy(transform.parent.gameObject);
        }
    }

    private void ReturnChildProps(Transform parent)
    {
        Transform[] children = new Transform[parent.childCount];
        for (int i = 0; i < parent.childCount; i++)
            children[i] = parent.GetChild(i);

        foreach (Transform child in children)
        {
            if (child.gameObject.layer != keepLayer)
            {
                PoolableObject po = child.GetComponent<PoolableObject>();
                if (po != null)
                {
                    po.ReturnToPool();
                }
            }

            // Rekurencyjnie zwracamy dzieci dziecka
            if (child.childCount > 0)
            {
                ReturnChildProps(child);
            }
        }
    }
}