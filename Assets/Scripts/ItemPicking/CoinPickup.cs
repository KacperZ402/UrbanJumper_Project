using UnityEngine;

public class CoinPickup : PickupItem
{
    [Header("Ustawienia Monety")]
    [SerializeField] private int coinValue = 1;

    protected override void OnCollect(GameObject player)
    {
        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.AddCoin(coinValue);
        }
    }
}