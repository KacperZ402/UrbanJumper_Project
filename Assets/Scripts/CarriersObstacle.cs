using UnityEngine;

public class OfficeDuoController : MonoBehaviour
{
    [Header("Animators")]
    [SerializeField] private Animator pullWorker; // Ten, który idzie przodem
    [SerializeField] private Animator pushWorker; // Ten, który pcha z tyłu

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 3f;

    // Zmienne stanu - gotowe do nadpisania przez Twój przyszły system
    private bool _isMoving;
    private int _currentDirection = 1; // Możesz tu potem dodać logikę kierunków

    /// <summary>
    /// Wywoływane przez Object Pooler przy spawnowaniu segmentu.
    /// </summary>
    void OnEnable()
    {
        // Tu na razie dajemy prosty template losowania, 
        // który potem zastąpimy Twoim pełnym systemem.
        bool randomState = Random.value > 0.5f;
        SetState(randomState);
    }

    /// <summary>
    /// Główna metoda sterująca stanem przeszkody. 
    /// Możesz ją wywołać z zewnętrznego skryptu (np. Managera Przeszkód).
    /// </summary>
    public void SetState(bool moving)
    {
        _isMoving = moving;

        // Synchronizacja obu animatorów
        if (pullWorker != null) pullWorker.SetBool("isMoving", _isMoving);
        if (pushWorker != null) pushWorker.SetBool("isMoving", _isMoving);
    }

    void Update()
    {
        if (_isMoving)
        {
            ApplyMovement();
        }
    }

    private void ApplyMovement()
    {
        // Ruch całego zestawu (Rodzic + Dzieci)
        // Używamy Space.World lub Space.Self w zależności od tego, jak rotujesz segmenty
        transform.Translate(Vector3.forward * _currentDirection * movementSpeed * Time.deltaTime);
    }

    // Opcjonalnie: Debugowanie w edytorze
    private void OnValidate()
    {
        // Pomaga szybko sprawdzić w edytorze, czy przypisałeś animatorów
        if (pullWorker == null || pushWorker == null)
        {
            Debug.LogWarning($"[OfficeDuo] Brakuje animatorów na obiekcie {gameObject.name}!");
        }
    }
}

//Ten skrypt i tak bedzie jeszcze eydtowany, tylko taki zamysł w jaki 
//sposób bedzie wybierany stan przeszkody