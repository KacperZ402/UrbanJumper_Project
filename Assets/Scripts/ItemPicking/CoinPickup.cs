using UnityEngine;

public class CoinPickup : PickupItem
{
    [SerializeField] private int scoreValue = 1;

    protected override void OnCollect(GameObject player)
    {
        Debug.Log($"Gracz zebrał monetę! Dodano {scoreValue} punktów.");
        // np. GameManager.Instance.AddScore(scoreValue);
    }
}