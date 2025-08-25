using UnityEngine;
using UnityEngine.UI;

public sealed class FireballController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField, Min(1)] private int shotsToActivate = 3;
    [SerializeField, Min(0.5f)] private float durationSeconds = 8f;

    [Header("UI (optional)")]
    [SerializeField] private Image chargeFill;   // fills [0..1] while NOT active
    [SerializeField] private Image activeFill;   // drains [1..0] while active
    [SerializeField] private GameObject activeFx;// flame/VFX toggle (optional)

    public bool IsActive { get; private set; }
    public int Streak { get; private set; }
    public float Charge01 { get; private set; }  // [0..1] while not active
    public float Remaining { get; private set; }
    public int Multiplier => IsActive ? 2 : 1;

    public event System.Action OnActivated;
    public event System.Action OnDeactivated;

    public void NotifyMake()
    {
        if (IsActive) return; // while active we don't build more charge
        Streak++;
        Charge01 = Mathf.Clamp01((float)Streak / Mathf.Max(1, shotsToActivate));
        UpdateUI();
        if (Streak >= shotsToActivate) Activate();
    }

    public void NotifyMiss()
    {
        if (IsActive) Deactivate();
        Streak = 0;
        Charge01 = 0f;
        UpdateUI();
    }

    private void Update()
    {
        if (!IsActive) return;
        Remaining -= Time.deltaTime;
        if (Remaining <= 0f)
        {
            Deactivate();
            Streak = 0;
            Charge01 = 0f;
        }
        UpdateUI();
    }

    public void ResetAll()
    {
        Streak = 0;
        Charge01 = 0f;
        Deactivate();
        UpdateUI();
    }

    private void Activate()
    {
        IsActive = true;
        Remaining = durationSeconds;
        if (activeFx) activeFx.SetActive(true);
        OnActivated?.Invoke();
        UpdateUI();
    }

    private void Deactivate()
    {
        IsActive = false;
        Remaining = 0f;
        if (activeFx) activeFx.SetActive(false);
        OnDeactivated?.Invoke();
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (chargeFill)
        {
            chargeFill.enabled = !IsActive;
            chargeFill.fillAmount = IsActive ? 0f : Charge01;
        }
        if (activeFill)
        {
            activeFill.enabled = IsActive;
            activeFill.fillAmount = (IsActive && durationSeconds > 0f)
                ? Mathf.Clamp01(Remaining / durationSeconds)
                : 0f;
        }
    }
}
