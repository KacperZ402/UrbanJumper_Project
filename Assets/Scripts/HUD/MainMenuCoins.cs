using UnityEngine;
using TMPro;

// Wyświetla łączną liczbę monet z zapisu (portfel gracza).
// Podepnij na obiekt z TextMeshPro w menu głównym albo na dowolny obiekt i przypisz tekst w Inspectorze.
public class MainMenuCoins : MonoBehaviour
{
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private string format = "{0}"; // np. "Monety: {0}"
    [SerializeField] private string highScoreFormat = "HighScore: {0}";

    private bool subscribed;
    private void OnEnable()
    {
        TrySubscribe();
        Refresh();
    }

    private void Start()
    {
        // Na wypadek, gdyby OnEnable odpalił się przed Awake() GameSavera
        TrySubscribe();
        Refresh();
    }

    private void OnDisable()
    {
        if (subscribed && GameSaver.Instance != null)
            GameSaver.Instance.OnDataChanged -= Refresh;
        subscribed = false;
    }

    private void TrySubscribe()
    {
        if (subscribed || GameSaver.Instance == null) return;
        GameSaver.Instance.OnDataChanged += Refresh;
        subscribed = true;
    }

    private void Refresh()
    {
        if (GameSaver.Instance == null) return;
        var d = GameSaver.Instance.Data;

        if (coinsText != null)
            coinsText.SetText(format, d.coins);

        if (highScoreText != null)
            highScoreText.SetText(highScoreFormat, d.highScore);
    }
}