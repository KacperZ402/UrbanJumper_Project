using UnityEngine;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    [Header("Referencje")]
    [SerializeField] private Transform playerTransform;

    [Header("Elementy UI (TextMeshPro)")]
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private float distanceDivider = 5f;

    private AudioSource audioSource;

    private int sessionCoins = 0;
    private int currentDistanceMeters = 0;
    private int highScoreMeters = 0;

    private float startZ;
    private float maxReachedZ;

    private const string HighScoreKey = "HighScoreMeters";

    private void Awake()
    {
        // Konfiguracja Singletona
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Pobieramy AudioSource raz na starcie (do odtwarzania dźwięków znajdźiek bez alokacji)
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        if (playerTransform != null)
        {
            startZ = playerTransform.position.z;
            maxReachedZ = startZ;
        }
        else
        {
            Debug.LogError("Brak przypisanego PlayerTransform w GameHUD!");
        }

        // Wczytanie rekordu z pamięci urządzenia
        highScoreMeters = PlayerPrefs.GetInt(HighScoreKey, 0);

        // Inicjalizacja UI na starcie (SetText nie generuje śmieci dla Garbage Collectora)
        UpdateCoinsUI();
        if (distanceText != null) distanceText.SetText("{0}", 0);
        if (highScoreText != null) highScoreText.SetText("BEST: {0}", highScoreMeters);
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Sprawdzamy, czy gracz pobił swój najdalszy punkt w tej sesji
        // Dzięki temu po Revive() i cofnięciu na wcześniejszą platformę wynik nie spadnie
        if (playerTransform.position.z > maxReachedZ)
        {
            maxReachedZ = playerTransform.position.z;
            int newMeters = Mathf.FloorToInt((maxReachedZ - startZ)/ distanceDivider);

            // Odświeżamy Canvas TYLKO wtedy, gdy zmieniła się pełna liczba metrów
            if (newMeters > currentDistanceMeters)
            {
                currentDistanceMeters = newMeters;

                if (distanceText != null)
                    distanceText.SetText("{0}", currentDistanceMeters);

                // Aktualizacja rekordu na żywo podczas biegu
                if (currentDistanceMeters > highScoreMeters)
                {
                    highScoreMeters = currentDistanceMeters;
                    if (highScoreText != null)
                        highScoreText.SetText("BEST: {0}", highScoreMeters);
                }
            }
        }
    }

    public void AddCoin(int amount = 1)
    {
        sessionCoins += amount;
        UpdateCoinsUI();
    }

    private void UpdateCoinsUI()
    {
        if (coinsText != null)
            coinsText.SetText("{0}", sessionCoins);
    }

    // Metoda wywoływana przez PickupItem zamiast kosztownego PlayClipAtPoint
    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    // Wywołaj tę metodę w TriggerDeath() w PlayerMovement
    public void SaveHighScoreOnDeath()
    {
        int savedBest = PlayerPrefs.GetInt(HighScoreKey, 0);
        if (currentDistanceMeters > savedBest)
        {
            PlayerPrefs.SetInt(HighScoreKey, currentDistanceMeters);
            PlayerPrefs.Save();
        }
    }

    // Opcjonalne gettery, jeśli będziesz potrzebować tych danych na ekranie Game Over
    public int GetSessionCoins() => sessionCoins;
    public int GetCurrentDistance() => currentDistanceMeters;
}