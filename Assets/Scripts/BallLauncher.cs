using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class BallLauncher : MonoBehaviour
{
    private enum LaunchState { Holding, Aiming, Flying, Cooldown }

    [Header("Refs")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform shotOrigin;
    [SerializeField] private UnifiedPointerInput input;
    [SerializeField] private TrailRenderer trail;

    [Header("Swipe Rules")]
    [SerializeField] private float minSwipeCm = 1.0f;
    [SerializeField] private float maxAngleFromUpDeg = 40f;

    [Header("Force Mapping")]
    [SerializeField] private float impulsePerCm = 4.5f;
    [SerializeField] private float impulsePerCmPerSec = 0.018f;
    [SerializeField] private float maxImpulse = 32f;

    [Header("Direction Weights")]
    [SerializeField] private float horizontalInfluence = 0.45f;
    [SerializeField] private float verticalInfluence = 1.2f;
    [SerializeField] private float forwardBias = 0.60f;

    [Header("Flight Tuning")]
    [SerializeField] private float gravityMultiplier = 3f;   
    [SerializeField] private float airDragWhileFlying = 0.0f;
    [SerializeField] private float cooldown = 0.25f;
    [SerializeField] private bool holdAtOriginOnStart = true;

    [Header("Spin at launch")]
    [SerializeField] private bool applySpin = true;
    [SerializeField, Tooltip("Torque impulse per unit of linear impulse for backspin")]
    private float backspinPerImpulse = 0.28f;
    [SerializeField, Tooltip("Torque impulse per unit of linear impulse for side spin (from horizontal swipe)")]
    private float sidespinPerImpulse = 0.15f;
    [SerializeField] private float maxAngularVelocity = 50f; 

    private Rigidbody _rb;
    private float _dpi;
    private float _nextAllowed;
    private bool _subscribed;
    private LaunchState _state = LaunchState.Holding;

    public System.Action OnLaunched;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _dpi = Mathf.Clamp(Screen.dpi, 100f, 400f);
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.maxAngularVelocity = maxAngularVelocity; 
    }

    private void Start()
    {
        if (!input) input = FindObjectOfType<UnifiedPointerInput>();
        if (holdAtOriginOnStart) HoldAtOrigin();
        SubscribeInput();
    }

    private void OnEnable() => SubscribeInput();
    private void OnDisable() => UnsubscribeInput();

    private void SubscribeInput()
    {
        if (_subscribed || input == null) return;
        input.OnDragStart += HandleStart;
        input.OnDragEnd += HandleEnd;
        _subscribed = true;
    }
    private void UnsubscribeInput()
    {
        if (!_subscribed || input == null) return;
        input.OnDragStart -= HandleStart;
        input.OnDragEnd -= HandleEnd;
        _subscribed = false;
    }

    private void FixedUpdate()
    {
        if (_state == LaunchState.Flying && gravityMultiplier > 1.001f)
            _rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);
    }

    private void HandleStart(Vector2 screenPos)
    {
        if (Time.time < _nextAllowed) return;
        if (_state == LaunchState.Cooldown) return;
        HoldAtOrigin();
        _state = LaunchState.Aiming;
    }

    private void HandleEnd(Vector2 start, Vector2 end, float duration)
    {
        if (Time.time < _nextAllowed) return;
        if (_state != LaunchState.Aiming) return;

        Vector2 delta = end - start;

        float cm = PixelsToCm(delta.magnitude);
        if (cm < minSwipeCm) { _state = LaunchState.Holding; return; }

        float angle = Vector2.Angle(Vector2.up, delta.normalized);
        if (angle > maxAngleFromUpDeg) { _state = LaunchState.Holding; return; }

        // lateral factor in [-1..1] for sidespin
        float lateral = Mathf.Clamp(delta.x / (Screen.width * 0.5f), -1f, 1f);
        float vertical = Mathf.Clamp(delta.y / (Screen.height * 0.5f), -1f, 1f);

        Vector3 dir =
              cam.transform.right * (lateral * horizontalInfluence)
            + cam.transform.up * (vertical * verticalInfluence)
            + cam.transform.forward * forwardBias;
        dir = dir.normalized;

        duration = Mathf.Max(0.02f, duration);
        float speedCmPerSec = cm / duration;
        float impulse = Mathf.Min(maxImpulse, cm * impulsePerCm + speedCmPerSec * impulsePerCmPerSec);

        ReleaseAndLaunch(dir, impulse, lateral);
        _nextAllowed = Time.time + cooldown;
        _state = LaunchState.Cooldown;
    }

    private float PixelsToCm(float px) => (px / _dpi) * 2.54f;

    private void HoldAtOrigin()
    {
        SnapToOrigin();
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.useGravity = false;
        _rb.isKinematic = true;
        _rb.drag = 0f;
        _rb.Sleep();
        ClearTrail();
        SetTrail(false);
    }

    private void SnapToOrigin()
    {
        if (!shotOrigin)
        {
            Debug.LogWarning("[BallLauncher] ShotOrigin not set; cannot hold.");
            return;
        }
        transform.SetPositionAndRotation(shotOrigin.position, shotOrigin.rotation);
    }

    private void ReleaseAndLaunch(Vector3 dir, float impulse, float lateral01)
    {
        _rb.WakeUp();
        _rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.drag = airDragWhileFlying;
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        _rb.AddForce(dir * impulse, ForceMode.Impulse);

        if (applySpin)
        {
            // Backspin around camera right (negative -> backspin)
            Vector3 backspinAxis = cam.transform.right;
            float backspinImpulse = backspinPerImpulse * impulse;
            // Sidespin around world up (or cam.up) from lateral swipe
            Vector3 sidespinAxis = cam.transform.up;
            float sidespinImpulse = sidespinPerImpulse * impulse * lateral01;

            _rb.AddTorque(-backspinAxis * backspinImpulse, ForceMode.Impulse);
            _rb.AddTorque(-sidespinAxis * sidespinImpulse, ForceMode.Impulse);
        }

        SetTrail(true);
        _state = LaunchState.Flying;
        OnLaunched?.Invoke();
    }

    public void PrepareNextShot()
    {
        HoldAtOrigin();
        _state = LaunchState.Holding;
    }

    public void ForceStopAndHold()
    {
        HoldAtOrigin();
        _state = LaunchState.Holding;
        _nextAllowed = 0f;
    }

    private void SetTrail(bool on) { if (trail) trail.emitting = on; }
    private void ClearTrail() { if (trail) trail.Clear(); }
}
