using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseAndNavigationMenu : MonoBehaviour
{
    public static bool IsGamePaused { get; private set; } = false;

    [Header("Główne kontenery UI")]
    [Tooltip("Panel z licznikami monet, dystansu i przyciskiem pauzy")]
    [SerializeField] private GameObject gameplayHudPanel;

    [Tooltip("Cały kontener menu pauzy (włączany po kliknięciu pauzy)")]
    [SerializeField] private GameObject pauseRootContainer;

    [Tooltip("Domyślny panel z przyciskami: Wznów, Sklep, Skiny, Opcje")]
    [SerializeField] private GameObject mainButtonsPanel;

    [Header("Sceny")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private GameObject currentActiveSubPanel;

    private void Awake()
    {
        // Resetujemy stan przy starcie sceny
        IsGamePaused = false;
        Time.timeScale = 1f;

        if (pauseRootContainer != null)
            pauseRootContainer.SetActive(true);
        if(mainButtonsPanel != null)
            mainButtonsPanel.SetActive(false);

        if (gameplayHudPanel != null)
            gameplayHudPanel.SetActive(true);
    }

    private void Update()
    {
        // Obsługa systemowego przycisku "Wstecz" na Androidzie (oraz klawisza ESC na PC)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!IsGamePaused)
            {
                PauseGame();
            }
            else if (currentActiveSubPanel != null)
            {
                // Jeśli jesteśmy w pod-menu (np. Sklep), cofamy się do głównej listy pauzy
                ShowMainButtonsPanel();
            }
            else
            {
                // Jeśli jesteśmy na głównej liście pauzy, wznawiamy grę
                ResumeGame();
            }
        }
    }

    public void PauseGame()
    {
        if (IsGamePaused) return;
        IsGamePaused = true;

        Time.timeScale = 0f;

        // Chowamy HUD z przyciskiem pauzy i licznikami
        if (gameplayHudPanel != null)
            gameplayHudPanel.SetActive(false);

        // Pokazujemy kontener menu pauzy
        if (pauseRootContainer != null)
            pauseRootContainer.SetActive(false);

        if(mainButtonsPanel != null)
            mainButtonsPanel.SetActive(true);

        ShowMainButtonsPanel();
    }

    public void ResumeGame()
    {
        if (!IsGamePaused) return;
        IsGamePaused = false;

        if (currentActiveSubPanel != null)
        {
            currentActiveSubPanel.SetActive(false);
            currentActiveSubPanel = null;
        }
        if(mainButtonsPanel != null)
            mainButtonsPanel.SetActive(false);

        if (pauseRootContainer != null)
            pauseRootContainer.SetActive(true);

        // Przywracamy HUD gry
        if (gameplayHudPanel != null)
            gameplayHudPanel.SetActive(true);


        Time.timeScale = 1f;
    }

    // Uniwersalne otwieranie zakładek (Sklep, Skiny, Opcje itp.)
    public void OpenSubPanel(GameObject targetPanel)
    {
        if (targetPanel == null) return;

        if (mainButtonsPanel != null)
            mainButtonsPanel.SetActive(false);

        if (currentActiveSubPanel != null)
            currentActiveSubPanel.SetActive(false);

        targetPanel.SetActive(true);
        currentActiveSubPanel = targetPanel;
    }

    // Powrót z zakładki (np. ze Sklepu) do głównych przycisków pauzy
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

    public void RestartGame()
    {
        Time.timeScale = 1f;
        IsGamePaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        IsGamePaused = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}