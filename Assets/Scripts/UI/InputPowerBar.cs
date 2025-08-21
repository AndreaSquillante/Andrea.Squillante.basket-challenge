using UnityEngine;
using UnityEngine.UI;

public sealed class InputPowerBar : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private UnifiedPointerInput input;
    [SerializeField] private BallLauncher launcher;
    [SerializeField] private Image fill; // type = Filled Vertical

    [Header("Colors")]
    [SerializeField] private Color weakColor = Color.red;
    [SerializeField] private Color mediumColor = Color.yellow;
    [SerializeField] private Color strongColor = Color.green;

    [Header("Timing")]
    [SerializeField] private float holdDuration = 0.6f; // time bar stays visible after release
    [SerializeField] private float fadeSpeed = 4f;

    private float _value;
    private float _timer;
    private bool _active;
    private CanvasGroup _cg;

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
        _cg.alpha = 0f;
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

    private void Update()
    {
        if (!_active && _cg.alpha > 0f)
        {
            _cg.alpha = Mathf.MoveTowards(_cg.alpha, 0f, fadeSpeed * Time.deltaTime);
        }
    }

    private void HandleStart(Vector2 s)
    {
        _value = 0f;
        _cg.alpha = 1f;
        _active = true;
        Set(0f);
    }

    private void HandleDrag(Vector2 curr)
    {
        if (!launcher || !fill) return;
        if (launcher.PhysicsProfile == null) return;

        var profile = launcher.PhysicsProfile;

        Vector2 delta = curr - input.StartScreenPos;

        float cm = launcher.ScreenPixelsToCm(delta.magnitude);
        float durSec = Mathf.Max(0.02f, input.DragDuration);
        float speed = cm / durSec;

        // Calcolo dell’impulso usando i valori dal profilo
        float impulse = Mathf.Min(profile.maxImpulse,
            cm * profile.impulsePerCm + speed * profile.impulsePerCmPerSec);

        // Normalizzazione 0..1 per la barra
        _value = Mathf.Clamp01(impulse / profile.maxImpulse);

        Set(_value);
    }

    private void HandleEnd(Vector2 s, Vector2 e, float d)
    {
        _active = false;
        _timer = holdDuration; // keep frozen
        Invoke(nameof(HideBar), holdDuration);
    }

    private void Set(float v)
    {
        fill.fillAmount = v;

        // Color feedback
        if (v < 0.3f) fill.color = weakColor;
        else if (v < 0.7f) fill.color = mediumColor;
        else fill.color = strongColor;
    }

    private void HideBar()
    {
        // Let Update fade it out
    }
}
