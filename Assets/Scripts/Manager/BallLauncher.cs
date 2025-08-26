using UnityEngine;
using UnityEngine.Profiling;

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
    [SerializeField] private ShotPowerAdvisor powerAdvisor;
 
    [Header("Physics Profile")]
    [SerializeField] private ShotPhysicsProfile physicsProfile;
    public ShotPhysicsProfile PhysicsProfile => physicsProfile;

    [Header("Swipe Rules")]
    [SerializeField] private float minSwipeCm = 1.0f;
    [SerializeField] private float maxAngleFromUpDeg = 40f;
    [SerializeField] private float cooldown = 0.25f;
    [SerializeField] private bool holdAtOriginOnStart = true;
    [SerializeField, Range(1f, 3f)] private float fallMultiplier = 1.4f; // extra gravity on descent
    [SerializeField, Range(0f, 2f)] private float apexDeadzoneVy = 0.5f; // no extra near apex
    [Header("Arcade Arc")]
    [SerializeField] private Transform hoopTarget;            // rim center (empty)
    [SerializeField] private bool useArcadeArc = true;        // fixed arc like Basketball Stars
    [SerializeField, Range(20f, 70f)] private float targetLaunchAngleDeg = 48f;
    [SerializeField, Range(0f, 25f)] private float maxYawFromSwipeDeg = 12f;
    // BallLauncher fields
    [SerializeField] private bool autoAngleByDistance = true;
    [SerializeField] private float minAngleDeg = 34f; // far shots
    [SerializeField] private float maxAngleDeg = 46f; // close shots
    [SerializeField] private float minDist = 4f;      // meters/units near
    [SerializeField] private float maxDist = 14f;     // meters/units far

    [Header("Perfect Snap")]
    [SerializeField] private bool snapToPerfect = true;
    [SerializeField, Range(0f, 0.05f)] private float perfectSnapPad = 0.02f; // tolerance beyond band
    [SerializeField, Range(0f, 1f)] private float maxLateralInPerfect = 0.20f;

    [Header("Debug")]
    [SerializeField] private bool liveTune = true; // updates rb.maxAngularVelocity at runtime

    [Header("Control")]
    [SerializeField] private bool listenToPlayerInput = true;  // Player ball: true, AI ball: false
    [SerializeField] private bool autoFindInput = true;
    // State
    private Rigidbody _rb;
    private float _dpi;
    private float _nextAllowed;
    private bool _subscribed;
    private LaunchState _state = LaunchState.Holding;

    // Events
    public System.Action OnLaunched;
    public event System.Action<Transform> OnShotOriginChanged;

    // Exposed read-only
    public Transform ShotOrigin => shotOrigin;
    public float MaxImpulse => physicsProfile ? physicsProfile.maxImpulse : 0f;
    public float GravityMultiplier => physicsProfile ? physicsProfile.gravityMultiplier : 1f;
    public Camera Cam { get => cam; set => cam = value; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _dpi = Mathf.Clamp(Screen.dpi, 100f, 400f);

        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (physicsProfile)
            _rb.maxAngularVelocity = physicsProfile.maxAngularVelocity;
    }

    private void Start()
    {
        if (listenToPlayerInput)
        {
            if (!input && autoFindInput) input = FindObjectOfType<UnifiedPointerInput>();
            SubscribeInput();
        }
        else
        {
            UnsubscribeInput(); 
        }

        if (holdAtOriginOnStart) HoldAtOrigin();
        OnShotOriginChanged?.Invoke(shotOrigin);
   
        zonesUI?.RefreshZones();                    
    }

    private void OnEnable() { if (listenToPlayerInput) SubscribeInput(); }
    private void OnDisable() { UnsubscribeInput(); }

    private void SubscribeInput()
    {
        if (!listenToPlayerInput) return;
        if (_subscribed || input == null) return;
        input.OnDragStart += HandleStart;
        input.OnDragEnd += HandleEnd;
        _subscribed = true;
    }

    private void UnsubscribeInput()
    {
        if (!_subscribed || input == null) { _subscribed = false; return; }
        input.OnDragStart -= HandleStart;
        input.OnDragEnd -= HandleEnd;
        _subscribed = false;
    }

    private void Update()
    {
        if (liveTune && physicsProfile)
            _rb.maxAngularVelocity = physicsProfile.maxAngularVelocity;
    }

    private void FixedUpdate()
    {
        if (_state != LaunchState.Flying || physicsProfile == null) return;
        float effMul = physicsProfile.gravityMultiplier * physicsProfile.sceneScale;
        Vector3 addAcc = Physics.gravity * Mathf.Max(0f, effMul - 1f);

        float vy = _rb.velocity.y;
        if (vy < -apexDeadzoneVy)
        {
            float extra = Mathf.Max(0f, fallMultiplier - 1f);
            addAcc += Physics.gravity * extra;
        }
        if (addAcc.sqrMagnitude > 0f) _rb.AddForce(addAcc, ForceMode.Acceleration);
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
        if (!physicsProfile) { _state = LaunchState.Holding; return; }

        Vector2 delta = end - start;

        float cm = ScreenPixelsToCm(delta.magnitude);
        if (cm < minSwipeCm) { _state = LaunchState.Holding; return; }

        float angle = Vector2.Angle(Vector2.up, delta.normalized);
        if (angle > maxAngleFromUpDeg) { _state = LaunchState.Holding; return; }

        // lateral [-1..1] from swipe X; vertical is ignored in arcade mode
        float lateral = Mathf.Clamp(delta.x / (Screen.width * 0.5f), -1f, 1f);
        float vertical = Mathf.Clamp(delta.y / (Screen.height * 0.5f), -1f, 1f);

        // direction
        Vector3 dir = useArcadeArc
            ? BuildArcadeDirection(lateral)
            : (
                cam.transform.right * (lateral * physicsProfile.horizontalInfluence) +
                cam.transform.up * (vertical * physicsProfile.verticalInfluence) +
                cam.transform.forward * physicsProfile.forwardBias
              ).normalized;

        // impulse
        duration = Mathf.Max(0.02f, duration);
        float speedCmPerSec = cm / duration;
        float impulse = Mathf.Min(
            physicsProfile.maxImpulse,
            cm * physicsProfile.impulsePerCm + speedCmPerSec * physicsProfile.impulsePerCmPerSec
        );

        // snap-to-perfect (optional)
        if (snapToPerfect && powerAdvisor != null)
        {
            powerAdvisor.Recompute(); // ensure fresh bands
            var r = powerAdvisor.PerfectRange; // advisor should be recomputed by binders when origin changes
            if (r.valid)
            {
                float pct = impulse / physicsProfile.maxImpulse;
                float pMin = Mathf.Max(0f, r.min - perfectSnapPad);
                float pMax = Mathf.Min(1f, r.max + perfectSnapPad);

                if (pct >= pMin && pct <= pMax)
                {
                    float centerPct = 0.5f * (r.min + r.max);
                    impulse = Mathf.Clamp(centerPct * physicsProfile.maxImpulse, 0f, physicsProfile.maxImpulse);
                    lateral = Mathf.Clamp(lateral, -maxLateralInPerfect, maxLateralInPerfect);
                    if (useArcadeArc) dir = BuildArcadeDirection(lateral); // re-apply yaw clamp
                }
            }
        }

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
            Vector3 backspinAxis = cam ? cam.transform.right : Vector3.right;
            Vector3 sidespinAxis = cam ? cam.transform.up : Vector3.up;

            float backspinImpulse = physicsProfile.backspinPerImpulse * impulse;
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
        if (backboardBonus != null)
        {
            // These must exist on your BackboardBonus (public).
            backboardBonus.ResetBonus();
            backboardBonus.TrySpawnBonus();
        }
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

    // Builds a consistent arcade arc that aims toward the hoop with a fixed elevation and limited yaw from swipe.
    private Vector3 BuildArcadeDirection(float lateral01)
    {
        Transform origin = shotOrigin ? shotOrigin : transform;
        Vector3 p0 = origin.position;
        Vector3 pc = hoopTarget ? hoopTarget.position : (cam ? cam.transform.position + cam.transform.forward * 10f : p0 + Vector3.forward);
        Vector3 toHoopXZ = new Vector3(pc.x, p0.y, pc.z) - p0;
        float D = toHoopXZ.magnitude;
        Vector3 hDir = (D > 1e-4f) ? toHoopXZ / D : Vector3.forward;

        float angDeg = targetLaunchAngleDeg;
        if (autoAngleByDistance)
        {
            float t = Mathf.InverseLerp(minDist, maxDist, D);
            angDeg = Mathf.Lerp(maxAngleDeg, minAngleDeg, t); // near -> high, far -> low
        }

        float ang = Mathf.Deg2Rad * Mathf.Clamp(angDeg, 20f, 70f);
        Vector3 dir = (hDir * Mathf.Cos(ang) + Vector3.up * Mathf.Sin(ang)).normalized;

        float yaw = Mathf.Clamp(lateral01, -1f, 1f) * maxYawFromSwipeDeg;
        dir = Quaternion.AngleAxis(yaw, Vector3.up) * dir;
        return dir;
    }

    // Public read-only: can AI shoot now?
    public bool IsReadyForShot => _state == LaunchState.Holding;

    // Public launch for AI (impulsePct in [0..1], lateral01 in [-1..1])
    public bool LaunchAI(float impulsePct, float lateral01)
    {
        if (!_rb || physicsProfile == null) return false;
        if (Time.time < _nextAllowed) return false;
        if (_state != LaunchState.Holding) return false;

        // Build direction (use same policy as player)
        Vector3 dir = useArcadeArc
            ? BuildArcadeDirection(lateral01)
            : (
                cam.transform.right * (lateral01 * physicsProfile.horizontalInfluence) +
                cam.transform.up * (1f * physicsProfile.verticalInfluence) +
                cam.transform.forward * physicsProfile.forwardBias
              ).normalized;

        float impulse = Mathf.Clamp01(impulsePct) * physicsProfile.maxImpulse;

        ReleaseAndLaunch(dir, impulse, lateral01);
        _nextAllowed = Time.time + cooldown;
        _state = LaunchState.Cooldown;
        return true;
    }

}
