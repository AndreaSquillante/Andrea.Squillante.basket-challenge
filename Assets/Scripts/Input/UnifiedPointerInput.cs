using UnityEngine;

public sealed class UnifiedPointerInput : MonoBehaviour, IPointerInput
{
    [SerializeField] private bool useFirstTouchOnly = true;

    public bool IsDragging { get; private set; }
    public Vector2 StartScreenPos { get; private set; }
    public Vector2 CurrentScreenPos { get; private set; }
    public float DragDuration { get; private set; }

    public event System.Action<Vector2> OnDragStart;
    public event System.Action<Vector2> OnDrag;
    public event System.Action<Vector2, Vector2, float> OnDragEnd;

    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouse();
#else
        HandleTouch();
#endif
    }

    private void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            IsDragging = true;
            DragDuration = 0f;
            StartScreenPos = CurrentScreenPos = Input.mousePosition;
            OnDragStart?.Invoke(StartScreenPos);
        }
        else if (IsDragging && Input.GetMouseButton(0))
        {
            DragDuration += Time.deltaTime;
            CurrentScreenPos = Input.mousePosition;
            OnDrag?.Invoke(CurrentScreenPos);
        }
        else if (IsDragging && Input.GetMouseButtonUp(0))
        {
            IsDragging = false;
            var end = (Vector2)Input.mousePosition;
            OnDragEnd?.Invoke(StartScreenPos, end, Mathf.Max(0.0001f, DragDuration));
        }
    }

    private void HandleTouch()
    {
        if (Input.touchCount == 0) return;

        int idx = 0;
        if (!useFirstTouchOnly)
        {
            // choose the first Began/Ended touch if you want to support multi-finger later
            for (int i = 0; i < Input.touchCount; i++) { if (Input.touches[i].phase == TouchPhase.Began) { idx = i; break; } }
        }

        Touch t = Input.touches[idx];
        switch (t.phase)
        {
            case TouchPhase.Began:
                IsDragging = true;
                DragDuration = 0f;
                StartScreenPos = CurrentScreenPos = t.position;
                OnDragStart?.Invoke(StartScreenPos);
                break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if (!IsDragging) break;
                DragDuration += Time.deltaTime;
                CurrentScreenPos = t.position;
                OnDrag?.Invoke(CurrentScreenPos);
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (!IsDragging) break;
                IsDragging = false;
                OnDragEnd?.Invoke(StartScreenPos, t.position, Mathf.Max(0.0001f, DragDuration));
                break;
        }
    }
}
