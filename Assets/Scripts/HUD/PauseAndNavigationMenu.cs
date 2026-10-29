using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class PauseAndNavigationMenu : MonoBehaviour
{
    public static bool IsGamePaused { get; private set; } = false;

    [Header("Główne kontenery UI")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameplayHudPanel;
    [SerializeField] private GameObject pauseRootContainer;
    [SerializeField] private GameObject mainButtonsPanel;

    [Header("Ekran Śmierci (Death Screen)")]
    [SerializeField] private GameObject gameOverPanel;
    [Tooltip("Przycisk oglądania reklamy - wyłączymy go, jeśli gracz zużył już Revive w tym biegu")]
    [SerializeField] private GameObject watchAdButton;

    [Header("Co ma się stać po obejrzeniu reklamy (Revive)")]
    [Tooltip("Podepnij tu np. metodę RevivePlayer() ze skryptu gracza oraz ClearZoneAhead z TrackObstacleCleaner")]
    public UnityEvent onPlayerRevived;

    private GameObject currentActiveSubPanel;
    private bool hasUsedRevive = false;

    private void Awake()
    {
        IsGamePaused = false;
        hasUsedRevive = false;
        Time.timeScale = 1f;

        if (pauseRootContainer != null)
            pauseRootContainer.SetActive(true);

        if (mainButtonsPanel != null)
            mainButtonsPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentActiveSubPanel != null)
            {
                ShowMainButtonsPanel();
                return;
            }

            bool isRunning = gameplayHudPanel != null && gameplayHudPanel.activeSelf;

            if (!IsGamePaused && isRunning)
            {
                PauseGame();
            }
            else if (IsGamePaused)
            {
                ResumeGame();
            }
        }
    }

    // ---> WYWOŁYWANE PRZY ŚMIERCI GRACZA <---
    public void TriggerGameOver()
    {
        IsGamePaused = false;

        if (gameplayHudPanel != null)
            gameplayHudPanel.SetActive(false);

        if (pauseRootContainer != null)
            pauseRootContainer.SetActive(false);

        if (mainButtonsPanel != null)
            mainButtonsPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        // Pokazujemy przycisk reklamy tylko wtedy, gdy gracz jeszcze się nie wskrzesił w tym biegu
        if (watchAdButton != null)
            watchAdButton.SetActive(!hasUsedRevive);
    }

    // ---> PODPINASZ POD PRZYCISK "OBEJRZYJ REKLAMĘ" <---
    public void OnWatchAdForReviveClicked()
    {
        if (hasUsedRevive) return;

        // TUTAJ w przyszłości wstawisz wywołanie SDK reklam (np. Unity Ads / AdMob):
        // AdsManager.Instance.ShowRewardedAd(CompleteReviveAfterAd);

        // Na razie (dopóki nie masz podpiętego SDK reklam) odpalamy wskrzeszenie od razu:
        CompleteReviveAfterAd();
    }

    private void CompleteReviveAfterAd()
    {
        hasUsedRevive = true;

        // Chowamy Death Screen i przywracamy HUD oraz przycisk pauzy
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameplayHudPanel != null)
            gameplayHudPanel.SetActive(true);

        if (pauseRootContainer != null)
            pauseRootContainer.SetActive(true);

        // Odpalamy wszystko, co podpiąłeś w Inspektorze (czyszczenie toru + włączenie ruchu gracza)
        onPlayerRevived?.Invoke();
    }

    public void PauseGame()
    {
        if (IsGamePaused) return;
        IsGamePaused = true;

        Time.timeScale = 0f;

        if (gameplayHudPanel != null)
            gameplayHudPanel.SetActive(false);

        if (pauseRootContainer != null)
            pauseRootContainer.SetActive(false);

        if (mainButtonsPanel != null)
            mainButtonsPanel.SetActive(true);
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

        if (mainButtonsPanel != null)
            mainButtonsPanel.SetActive(false);

        if (pauseRootContainer != null)
            pauseRootContainer.SetActive(true);

        if (gameplayHudPanel != null)
            gameplayHudPanel.SetActive(true);

        Time.timeScale = 1f;
    }

    public void OpenSubPanel(GameObject targetPanel)
    {
        if (targetPanel == null) return;

        if (IsGamePaused)
        {
            if (mainButtonsPanel != null)
                mainButtonsPanel.SetActive(false);
        }
        else
        {
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);
        }

        if (currentActiveSubPanel != null)
            currentActiveSubPanel.SetActive(false);

        targetPanel.SetActive(true);
        currentActiveSubPanel = targetPanel;
    }

    public void ShowMainButtonsPanel()
    {
        if (currentActiveSubPanel != null)
        {
            currentActiveSubPanel.SetActive(false);
            currentActiveSubPanel = null;
        }

        if (IsGamePaused)
        {
            if (mainButtonsPanel != null)
                mainButtonsPanel.SetActive(true);
        }
        else
        {
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(true);
        }
    }

    // ---> PODPINASZ POD PRZYCISK "RESTART" (W Pauzie i w Death Screenie) <---
    public void RestartGame()
    {
        Time.timeScale = 1f;
        IsGamePaused = false;
        MenuCameraTransition.IsRestarting = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ---> PODPINASZ POD PRZYCISK "MAIN MENU" (W Pauzie i w Death Screenie) <---
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        IsGamePaused = false;
        MenuCameraTransition.IsRestarting = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}