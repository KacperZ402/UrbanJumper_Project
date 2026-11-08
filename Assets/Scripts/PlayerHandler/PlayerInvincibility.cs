using System.Collections;
using UnityEngine;

// Nieśmiertelność: miganie postaci + wyłączona kolizja z przeszkodami.
// Używana po Revive, później np. przez powerup tarczy.
public class PlayerInvincibility : MonoBehaviour
{
    [SerializeField] private Renderer[] playerRenderers; // Wrzuć tu wszystkie meshe postaci
    [SerializeField] private float blinkInterval = 0.15f; // Szybkość migania (w sekundach)
    [SerializeField] private float fastBlinkInterval = 0.1f;
    [SerializeField, Range(0f, 1f)] private float fastBlinkFrom = 0.7f; // od której części czasu szybsze miganie

    public bool IsActive { get; private set; }

    private Coroutine routine;
    private int playerLayer;
    private int obstacleLayer;

    private void Awake()
    {
        playerLayer = LayerMask.NameToLayer("Player");
        obstacleLayer = LayerMask.NameToLayer("Obstacle");
    }

    public void Activate(float duration)
    {
        if (routine != null) StopCoroutine(routine);
        IsActive = true;
        routine = StartCoroutine(InvincibilityRoutine(duration));
    }

    private IEnumerator InvincibilityRoutine(float duration)
    {
        SetObstacleCollision(false);

        float timer = 0f;
        bool isVisible = true;
        float fastBlinkThreshold = duration * fastBlinkFrom;

        WaitForSeconds normalWait = new WaitForSeconds(blinkInterval);
        WaitForSeconds fastWait = new WaitForSeconds(fastBlinkInterval);

        while (timer < duration)
        {
            isVisible = !isVisible;
            SetVisible(isVisible);

            if (timer >= fastBlinkThreshold)
            {
                yield return fastWait;
                timer += fastBlinkInterval;
            }
            else
            {
                yield return normalWait;
                timer += blinkInterval;
            }
        }

        Finish();
    }

    private void Finish()
    {
        SetVisible(true);
        SetObstacleCollision(true);
        IsActive = false;
        routine = null;
    }

    private void OnDisable()
    {
        // Gdyby obiekt zniknął w trakcie (restart, wyjście do menu) – przywracamy kolizję
        if (!IsActive) return;
        if (routine != null) StopCoroutine(routine);
        Finish();
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer r in playerRenderers)
            if (r != null) r.enabled = visible;
    }

    private void SetObstacleCollision(bool enabled)
    {
        if (playerLayer != -1 && obstacleLayer != -1)
            Physics.IgnoreLayerCollision(playerLayer, obstacleLayer, !enabled);
    }
}