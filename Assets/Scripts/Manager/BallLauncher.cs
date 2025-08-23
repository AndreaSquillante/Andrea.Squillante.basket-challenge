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
    [SerializeField] private PowerBarZonesUI zonesUI;
    [SerializeField] private BackboardBonus backboardBonus;

    [Header("Physics Profile")]
    [SerializeField] private ShotPhysicsProfile physicsProfile;
    public ShotPhysicsProfile PhysicsProfile => physicsProfile;

    [Header("Swipe Rules")]
    [SerializeField] private float minSwipeCm = 1.0f;
    [SerializeField] private float maxAngleFromUpDeg = 40f;
    [SerializeField] private float cooldown = 0.25f;
    [SerializeField] private bool holdAtOriginOnStart = true;

    private Rigidbody _rb;
    private float _dpi;
    private float _nextAllowed;
    private bool _subscribed;
    private LaunchState _state = LaunchState.Holding;

    public System.Action OnLaunched;
    public event System.Action<Transform> OnShotOriginChanged;
    public Transform ShotOrigin => shotOrigin;

    // Expose read-only profile values
    public float MaxImpulse => physicsProfile.maxImpulse;
    public float GravityMultiplier => physicsProfile.gravityMultiplier;

    public Camera Cam { get => cam; set => cam = value; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _dpi = Mathf.Clamp(Screen.dpi, 100f, 400f);
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.maxAngularVelocity = physicsProfile.maxAngularVelocity;
    }

    private void Start()
    {
        if (!input) input = FindObjectOfType<UnifiedPointerInput>();
        if (holdAtOriginOnStart) HoldAtOrigin();
        SubscribeInput();
        OnShotOriginChanged?.Invoke(shotOrigin);
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
        if (_state == LaunchState.Flying && physicsProfile.gravityMultiplier > 1.001f)
            _rb.AddForce(Physics.gravity * (physicsProfile.gravityMultiplier - 1f), ForceMode.Acceleration);
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
        float cm = ScreenPixelsToCm(delta.magnitude);
        if (cm < minSwipeCm) { _state = LaunchState.Holding; return; }

        float angle = Vector2.Angle(Vector2.up, delta.normalized);
        if (angle > maxAngleFromUpDeg) { _state = LaunchState.Holding; return; }

        float lateral = Mathf.Clamp(delta.x / (Screen.width * 0.5f), -1f, 1f);
        float vertical = Mathf.Clamp(delta.y / (Screen.height * 0.5f), -1f, 1f);

        Vector3 dir =
              Cam.transform.right * (lateral * physicsProfile.horizontalInfluence)
            + Cam.transform.up * (vertical * physicsProfile.verticalInfluence)
            + Cam.transform.forward * physicsProfile.forwardBias;
        dir = dir.normalized;

        duration = Mathf.Max(0.02f, duration);
        float speedCmPerSec = cm / duration;
        float impulse = Mathf.Min(physicsProfile.maxImpulse, cm * physicsProfile.impulsePerCm + speedCmPerSec * physicsProfile.impulsePerCmPerSec);

        ReleaseAndLaunch(dir, impulse, lateral);
        _nextAllowed = Time.time + cooldown;
        _state = LaunchState.Cooldown;
    }

    public float ScreenPixelsToCm(float px) => (px / _dpi) * 2.54f;

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
        _rb.drag = physicsProfile.airDragWhileFlying;
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        _rb.AddForce(dir * impulse, ForceMode.Impulse);

        if (physicsProfile.applySpin)
        {
            Vector3 backspinAxis = Cam.transform.right;
            float backspinImpulse = physicsProfile.backspinPerImpulse * impulse;

            Vector3 sidespinAxis = Cam.transform.up;
            float sidespinImpulse = physicsProfile.sidespinPerImpulse * impulse * lateral01;

            _rb.AddTorque(-backspinAxis * backspinImpulse, ForceMode.Impulse);
            _rb.AddTorque(-sidespinAxis * sidespinImpulse, ForceMode.Impulse);
        }

        SetTrail(true);
        _state = LaunchState.Flying;
        OnLaunched?.Invoke();
    }

    public void SetShotOrigin(Transform newOrigin)
    {
        shotOrigin = newOrigin;
        OnShotOriginChanged?.Invoke(shotOrigin);
        zonesUI?.RefreshZones();
    }

    public void PrepareNextShot()
    {
        backboardBonus.ResetBonus();
        backboardBonus.TrySpawnBonus();
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
