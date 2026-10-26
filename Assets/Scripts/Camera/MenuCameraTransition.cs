using UnityEngine;
using System.Collections;

public class MenuCameraTransition : MonoBehaviour
{
    [Header("Obiekty i Skrypty")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private Transform cameraTransform;
    [Tooltip("Twój główny skrypt śledzenia na kamerze (zostanie włączony po animacji)")]
    [SerializeField] private MonoBehaviour mainCameraFollowScript;
    [SerializeField] private EndPointTrigger firstTrigger;

    [Header("Ustawienia Przejścia Kamery")]
    [Tooltip("Pusty obiekt ustawiony w miejscu, z którego kamera patrzy na drzwi w Menu")]
    [SerializeField] private Transform menuViewPoint;
    [SerializeField] private float transitionDuration = 0.75f;

    [Header("Panele UI")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameplayHudPanel;

    // Zapamiętane ustawienia kamery względem gracza (z ustawienia w scenie)
    private Vector3 targetOffsetFromPlayer;
    private Quaternion targetGameplayRotation;

    private readonly WaitForEndOfFrame waitForLateUpdate = new WaitForEndOfFrame();

    private void Awake()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        // 1. Zanim cokolwiek ruszymy, zapisujemy jak kamera powinna stać względem gracza w trakcie gry
        if (playerObject != null)
        {
            targetOffsetFromPlayer = cameraTransform.position - playerObject.transform.position;
            targetGameplayRotation = cameraTransform.rotation;

            // Wyłączamy gracza na czas pobytu w Menu Głównym
            playerObject.SetActive(false);
        }

        // 2. Wyłączamy główny skrypt podążania kamery
        if (mainCameraFollowScript != null)
            mainCameraFollowScript.enabled = false;

        // 3. Przestawiamy kamerę na widok Menu (patrzy na drzwi)
        if (menuViewPoint != null)
        {
            cameraTransform.SetPositionAndRotation(menuViewPoint.position, menuViewPoint.rotation);
        }
        if (firstTrigger != null)
        {
            firstTrigger.TriggerManually();
        }

        // 4. Ustawiamy UI pod Menu Główne i blokujemy pauzę
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (gameplayHudPanel != null) gameplayHudPanel.SetActive(false);
    }

    // ---> TĘ METODĘ PODPINASZ POD PRZYCISK "PLAY" <---
    public void StartGame()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        // Włączamy gracza - od razu wybiega zza drzwi swoim normalnym ruchem
        if (playerObject != null)
            playerObject.SetActive(true);

        StartCoroutine(TransitionCameraRoutine());
    }

    private IEnumerator TransitionCameraRoutine()
    {
        Transform playerTr = playerObject.transform;

        Vector3 startCamPos = cameraTransform.position;
        Quaternion startCamRot = cameraTransform.rotation;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            // Czekamy na koniec klatki (jak LateUpdate), żeby gracz wykonał już swój ruch
            yield return waitForLateUpdate;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);

            // SmoothStep daje miękki start i miękkie wyhamowanie kamery za plecami
            float smoothT = t * t * (3f - 2f * t);

            // 2. Dynamiczny punkt docelowy (bieżąca pozycja uciekającego gracza + offset kamery)
            Vector3 currentTargetCamPos = playerTr.position + targetOffsetFromPlayer;

            // 3. Płynny lot i obrót kamery za plecy gracza
            cameraTransform.position = Vector3.Lerp(startCamPos, currentTargetCamPos, smoothT);
            cameraTransform.rotation = Quaternion.Slerp(startCamRot, targetGameplayRotation, smoothT);
        }

        // Dopięcie idealnej pozycji na koniec i przekazanie pałeczki głównemu skryptowi kamery
        cameraTransform.SetPositionAndRotation(playerTr.position + targetOffsetFromPlayer, targetGameplayRotation);

        if (mainCameraFollowScript != null)
            mainCameraFollowScript.enabled = true;

        if (gameplayHudPanel != null)
            gameplayHudPanel.SetActive(true);
    }
}