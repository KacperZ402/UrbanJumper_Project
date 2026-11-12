using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Sterowanie swipe'ami na telefonie. W edytorze i na PC można "swipe'ować" myszką (przeciągnięcie z wciśniętym LPM).
public class SwipeInput : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;

    [Tooltip("Minimalna długość swipe'a jako część krótszego boku ekranu (0.06 = 6%)")]
    [SerializeField, Range(0.01f, 0.3f)] private float minSwipeDistance = 0.06f;

    [Tooltip("Kilka swipe'ów bez odrywania palca (np. dwa pasy jednym ruchem)")]
    [SerializeField] private bool multipleSwipesPerTouch = false;

    [Tooltip("Ignoruj dotyk, który zaczyna się na UI (np. przycisk pauzy)")]
    [SerializeField] private bool ignoreTouchesOverUI = true;

    private const int NoFinger = -1;
    private const int MouseId = -2;

    private int trackedId = NoFinger;
    private Vector2 startPos;
    private bool swipeConsumed;

    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    private void Awake()
    {
        if (player == null)
            player = GetComponent<PlayerMovement>();

        if (player == null)
            Debug.LogError("[SwipeInput] Przypisz PlayerMovement w Inspectorze.", this);
    }

    private void Update()
    {
        if (player == null) return;

        if (Input.touchCount > 0)
            HandleTouches();
#if UNITY_EDITOR || UNITY_STANDALONE
        else
            HandleMouse();
#endif
    }

    private void HandleTouches()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (trackedId == NoFinger && !IsOverUI(touch.position))
                        Begin(touch.fingerId, touch.position);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (touch.fingerId == trackedId)
                        Track(touch.position);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (touch.fingerId == trackedId)
                    {
                        Track(touch.position); // szybki "flick" potrafi zmieścić się w jednej klatce
                        trackedId = NoFinger;
                    }
                    break;
            }
        }
    }

#if UNITY_EDITOR || UNITY_STANDALONE
    private void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (trackedId == NoFinger && !IsOverUI(Input.mousePosition))
                Begin(MouseId, Input.mousePosition);
        }
        else if (Input.GetMouseButton(0))
        {
            if (trackedId == MouseId)
                Track(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && trackedId == MouseId)
        {
            Track(Input.mousePosition);
            trackedId = NoFinger;
        }
    }
#endif

    private void Begin(int id, Vector2 position)
    {
        trackedId = id;
        startPos = position;
        swipeConsumed = false;
    }

    private void Track(Vector2 position)
    {
        if (swipeConsumed) return;

        Vector2 delta = position - startPos;
        float threshold = minSwipeDistance * Mathf.Min(Screen.width, Screen.height);
        if (delta.sqrMagnitude < threshold * threshold) return;

        Fire(delta);

        if (multipleSwipesPerTouch)
            startPos = position;  // kolejny swipe liczony od tego miejsca
        else
            swipeConsumed = true; // jeden swipe na jedno dotknięcie
    }

    private void Fire(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            if (delta.x > 0f) player.MoveRight();
            else player.MoveLeft();
        }
        else
        {
            if (delta.y > 0f) player.Jump();
            else player.Slide();
        }
    }

    private bool IsOverUI(Vector2 screenPos)
    {
        if (!ignoreTouchesOverUI) return false;

        EventSystem es = EventSystem.current;
        if (es == null) return false;

        PointerEventData data = new PointerEventData(es) { position = screenPos };
        uiHits.Clear();
        es.RaycastAll(data, uiHits);
        return uiHits.Count > 0;
    }
}