using UnityEngine;
using TMPro;

public sealed class SwipeFeedbackUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private UnifiedPointerInput input;
    [SerializeField] private BallLauncher launcher;

    // Container that moves to touch start; put arrow and label inside it
    [SerializeField] private RectTransform container;
    [SerializeField] private RectTransform arrow;      // pivot (0.5, 0)
    [SerializeField] private TMP_Text powerLabel;      // optional text

    [Header("Tuning")]
    [SerializeField] private float maxArrowLength = 300f; // px
    [SerializeField] private float labelTipOffset = 24f;

    [Header("Direction Fix")]
    [SerializeField] private bool invertHorizontal = false;
    [SerializeField] private bool invertVertical = false;

    private Canvas _canvas;
    private RectTransform _canvasRect;
    private bool _active;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas) _canvasRect = _canvas.transform as RectTransform;
    }

    private void OnEnable()
    {
        if (!input) input = FindObjectOfType<UnifiedPointerInput>();
        if (input != null)
        {
            input.OnDragStart += HandleStart;
            input.OnDrag += HandleDrag;
            input.OnDragEnd += HandleEnd;
        }
        SetVisible(false);
    }

    private void OnDisable()
    {
        if (input != null)
        {
            input.OnDragStart -= HandleStart;
            input.OnDrag -= HandleDrag;
            input.OnDragEnd -= HandleEnd;
        }
    }

    private void HandleStart(Vector2 start)
    {
        _active = true;
        SetVisible(true);

        // move container to touch start (in canvas space)
        if (container && _canvasRect)
            container.anchoredPosition = ScreenToCanvas(start);

        UpdateVisual(start, start, 0f);
    }

    private void HandleDrag(Vector2 curr)
    {
        if (!_active) return;
        UpdateVisual(input.StartScreenPos, curr, input.DragDuration);
    }

    private void HandleEnd(Vector2 start, Vector2 end, float dur)
    {
        _active = false;
        SetVisible(false);
    }

    private void UpdateVisual(Vector2 start, Vector2 current, float dur)
    {
        if (!launcher || !arrow || !_canvasRect) return;
        var profile = launcher.PhysicsProfile;
        if (profile == null) return;

        Vector2 delta = current - start;
        if (invertHorizontal) delta.x = -delta.x;
        if (invertVertical) delta.y = -delta.y;

        float sqrMag = delta.sqrMagnitude;
        if (sqrMag < 1e-6f) return;

        // power like BallLauncher
        float cm = launcher.ScreenPixelsToCm(Mathf.Sqrt(sqrMag));
        float durSec = Mathf.Max(0.02f, dur);
        float speedCmPerSec = cm / durSec;
        float impulse = Mathf.Min(profile.maxImpulse,
            cm * profile.impulsePerCm + speedCmPerSec * profile.impulsePerCmPerSec);

        float pct = Mathf.Clamp01(impulse / profile.maxImpulse);
        float length = pct * maxArrowLength;

        // orient arrow in pure screen space
        float angle = Vector2.SignedAngle(Vector2.up, delta.normalized);

        // base at touch start
        if (container)
            container.anchoredPosition = ScreenToCanvas(start);

        // apply visuals
        arrow.sizeDelta = new Vector2(arrow.sizeDelta.x, length);
        arrow.localRotation = Quaternion.Euler(0f, 0f, -angle);

        if (powerLabel)
        {
            powerLabel.text = Mathf.RoundToInt(pct * 100f) + "%";
            // place label near arrow tip along its local up
            var tipLocal = Vector3.up * (length + labelTipOffset);
            powerLabel.rectTransform.localPosition = tipLocal;
            powerLabel.rectTransform.localRotation = Quaternion.identity; // keep upright
        }
    }

    private Vector2 ScreenToCanvas(Vector2 screenPos)
    {
        if (_canvas == null || _canvasRect == null) return screenPos;

        Camera cam = null;
        if (_canvas.renderMode == RenderMode.ScreenSpaceCamera || _canvas.renderMode == RenderMode.WorldSpace)
            cam = _canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, cam, out Vector2 local);
        return local;
    }

    private void SetVisible(bool v)
    {
        if (container) container.gameObject.SetActive(v);
        else
        {
            if (arrow) arrow.gameObject.SetActive(v);
            if (powerLabel) powerLabel.gameObject.SetActive(v);
        }
    }
}
