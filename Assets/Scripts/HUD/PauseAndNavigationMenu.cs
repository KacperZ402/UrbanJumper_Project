using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseAndNavigationMenu : MonoBehaviour
{
    [Header("Główne kontenery")]
    [Tooltip("Cały obiekt menu pauzy (włączany, gdy klikniesz pauzę)")]
    [SerializeField] private GameObject pauseRootContainer;

    [Tooltip("Domyślny panel z przyciskami: Wznów, Sklep, Skiny, Opcje, Menu Główne")]
    [SerializeField] private GameObject mainButtonsPanel;

    [Header("Sceny")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    // Pamięta, które okno (np. Sklep albo Skiny) jest aktualnie otwarte
    private GameObject currentActiveSubPanel;
    private bool isPaused = false;

    private void Start()
    {
        // Na starcie gry upewniamy się, że menu jest schowane, a czas płynie normalnie
        if (pauseRootContainer != null)
            pauseRootContainer.SetActive(false);

        Time.timeScale = 1f;
    }

    // 1. PAUZA / WZNOWIENIE GRY
    public void PauseGame()
    {
        if (isPaused) return;
        isPaused = true;

        // Zatrzymujemy fizykę i czas w grze
        Time.timeScale = 0f;

        if (pauseRootContainer != null)
            pauseRootContainer.SetActive(true);

        // Zawsze otwieramy główny widok z listą przycisków
        ShowMainButtonsPanel();
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;

        // chowamy ewentualnie otwarty pod-panel i całe menu
        if (currentActiveSubPanel != null)
            currentActiveSubPanel.SetActive(false);

        if (pauseRootContainer != null)
            pauseRootContainer.SetActive(false);

        // Przywracamy czas w grze
        Time.timeScale = 1f;
    }

    // 2. UNIWERSALNA NAWIGACJA (Sklep, Skiny, Opcje i wszystko, co dodasz w przyszłości!)
    // Tę metodę podpinasz pod przyciski: Sklep, Skiny, Opcje itp.
    public void OpenSubPanel(GameObject targetPanel)
    {
        if (targetPanel == null) return;

        // Chowamy główną listę przycisków
        if (mainButtonsPanel != null)
            mainButtonsPanel.SetActive(false);

        // Jeśli był otwarty jakiś inny pod-panel, chowamy go
        if (currentActiveSubPanel != null)
            currentActiveSubPanel.SetActive(false);

        // Otwieramy wybrany panel (np. Skiny) i zapamiętujemy go
        targetPanel.SetActive(true);
        currentActiveSubPanel = targetPanel;
    }

    // Tę metodę podpinasz pod przycisk "Wstecz / Powrót" wewnątrz Sklepu, Skinów czy Opcji
    public void ShowMainButtonsPanel()
    {
        if (currentActiveSubPanel != null)
        {
            currentActiveSubPanel.SetActive(false);
            currentActiveSubPanel = null;
        }

        if (mainButtonsPanel != null)
            mainButtonsPanel.SetActive(true);
    }

    // 3. WYJŚCIE DO MENU GŁÓWNEGO / RESTART
    public void LoadMainMenu()
    {
        // KRYTYCZNE: Przed zmianą sceny ZAWSZE musisz przywrócić Time.timeScale = 1f!
        // Inaczej po załadowaniu Menu Głównego cała gra nadal będzie zamrożona.
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}