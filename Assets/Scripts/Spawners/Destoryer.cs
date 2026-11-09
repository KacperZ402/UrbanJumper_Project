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
            ReturnDirectChildren(segmentTransform);
            Destroy(segmentTransform.gameObject);
        }
    }

    private void ReturnDirectChildren(Transform parent)
    {
        int childCount = parent.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.gameObject.layer != keepLayer)
            {
                PoolableObject po = child.GetComponent<PoolableObject>();
                if (po != null)
                {
                    po.ReturnToPool();
                }
            }
        }
    }
}
