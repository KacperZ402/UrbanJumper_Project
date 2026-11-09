using UnityEngine;

// Śmierć (wykrywanie po spadku prędkości + ragdoll) i wskrzeszanie gracza.
// Wrzuć na ten sam obiekt co PlayerMovement.
[DefaultExecutionOrder(-10)] // wykrywanie śmierci przed ruchem w każdym kroku fizyki – tak jak było
[RequireComponent(typeof(PlayerMovement), typeof(PlayerInvincibility))]
public class PlayerLife : MonoBehaviour
{
    [Header("Detekcja Śmierci przez Uderzenie")]
    [SerializeField] private float minExpectedSpeedRatio = 0.01f; // Poniżej 50% normy -> śmierć

    [Header("System Śmierci")]
    [SerializeField] private Collider mainCollider;
    [SerializeField] private Rigidbody mainRb;
    [SerializeField] private GameObject ragdollRoot; // Główna kość ragdolla
    [SerializeField] private PauseAndNavigationMenu navigationMenu;

    [Header("Wskrzeszenie (Revive)")]
    [SerializeField] private float reviveHeightOffset = 5f;
    [SerializeField] private float invincibilityDuration = 2f;

    public bool IsDead { get; private set; }
    public event System.Action OnDied;
    public event System.Action OnRevived;

    private PlayerMovement movement;
    private PlayerInvincibility invincibility;
    private Animator animator;
    private Rigidbody[] ragdollBones;
    private float lastZPosition;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        invincibility = GetComponent<PlayerInvincibility>();
        animator = GetComponentInChildren<Animator>();

        if (mainRb == null) mainRb = GetComponent<Rigidbody>();
        if (mainCollider == null) mainCollider = GetComponent<Collider>();
        if (ragdollRoot != null) ragdollBones = ragdollRoot.GetComponentsInChildren<Rigidbody>(true);
    }

    private void FixedUpdate()
    {
        if (IsDead) return;

        float actualSpeedZ = (mainRb.position.z - lastZPosition) / Time.fixedDeltaTime;
        Debug.Log("Speed: " + actualSpeedZ + "Treshold: " + (movement.moveSpeedZ * minExpectedSpeedRatio));
        if (!invincibility.IsActive && actualSpeedZ < movement.moveSpeedZ * minExpectedSpeedRatio && lastZPosition != 0f)
        {
            Die();
            return;
        }
        lastZPosition = mainRb.position.z;
    }

    public void Die()
    {
        if (IsDead || invincibility.IsActive) return;
        IsDead = true;

        // Wyłączenie skryptu ruchu = koniec inputu i FixedUpdate ruchu
        movement.enabled = false;

        Vector3 ragdollMomentum = new Vector3(0f, mainRb.velocity.y, movement.moveSpeedZ * 0.5f);

        if (animator != null) animator.enabled = false;
        if (mainCollider != null) mainCollider.enabled = false;
        mainRb.isKinematic = true;

        if (ragdollRoot != null)
        {
            ragdollRoot.SetActive(true);
            foreach (Rigidbody bone in ragdollBones)
                bone.velocity = ragdollMomentum;
        }

        if (GameHUD.Instance != null)
            GameHUD.Instance.SaveRunOnDeath();

        if (navigationMenu != null)
            navigationMenu.TriggerGameOver();

        OnDied?.Invoke();
    }

    public void Revive()
    {
        if (!IsDead) return;

        Vector3 revivePosition = movement.RespawnPoint;
        revivePosition.y += reviveHeightOffset;

        // Twarda teleportacja fizyki
        mainRb.isKinematic = true;
        transform.position = revivePosition;
        mainRb.position = revivePosition;
        mainRb.velocity = Vector3.zero;

        // Sprzątanie ragdolla i pobudka fizyki
        if (ragdollRoot != null) ragdollRoot.SetActive(false);
        mainRb.isKinematic = false;
        mainRb.detectCollisions = true;
        if (mainCollider != null) mainCollider.enabled = true;
        if (animator != null) animator.enabled = true;

        IsDead = false;

        // Reset stanu ruchu i powrót sterowania
        movement.ResetForRevive();
        movement.enabled = true;

        // Zabezpieczenie przed natychmiastowym "death-loopem"
        lastZPosition = revivePosition.z;
        invincibility.Activate(invincibilityDuration);

        OnRevived?.Invoke();
    }
}