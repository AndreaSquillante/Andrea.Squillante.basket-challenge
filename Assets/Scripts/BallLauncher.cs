using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class BallLauncher : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform shotOrigin;
    [SerializeField] private UnifiedPointerInput input;
    [SerializeField] private TrailRenderer trail;

    [Header("Swipe Rules")]
    [SerializeField] private float minSwipeCm = 1.0f;
    [SerializeField] private float maxAngleFromUpDeg = 35f;

    [Header("Force Mapping")]
    [Tooltip("Impulse gained per swipe centimeter")]
    [SerializeField] private float impulsePerCm = 3.6f;               
    [Tooltip("Extra impulse gained per swipe speed (cm/s)")]
    [SerializeField] private float impulsePerCmPerSec = 0.012f;       
    [SerializeField] private float maxImpulse = 26f;                   

    [Header("Direction Weights")]
    [SerializeField] private float horizontalInfluence = 0.45f;
    [SerializeField] private float verticalInfluence = 1.15f;          
    [SerializeField] private float forwardBias = 0.62f;                

    [Header("Flight Tuning")]
    [SerializeField] private float gravityMultiplier = 1.35f;         
    [SerializeField] private float airDragWhileFlying = 0.02f;         
    [SerializeField] private float cooldown = 0.2f;
    [SerializeField] private bool holdAtOriginOnStart = true;

    private Rigidbody _rb;
    private float _nextAllowed;
    private float _dpi;
    private bool _flying;

    public System.Action OnLaunched;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _dpi = Mathf.Clamp(Screen.dpi, 100f, 400f);
    }

    private void Start()
    {
        if (holdAtOriginOnStart) HoldAtOrigin();
        if (!input) input = FindObjectOfType<UnifiedPointerInput>();
        // rigidbody sane defaults
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void OnEnable()
    {
        if (input)
        {
            input.OnDragStart += HandleStart;
            input.OnDragEnd += HandleEnd;
        }
    }

    private void OnDisable()
    {
        if (input)
        {
            input.OnDragStart -= HandleStart;
            input.OnDragEnd -= HandleEnd;
        }
    }

    private void FixedUpdate()
    {
        if (!_flying) return;

        // Custom gravity multiplier for a snappier arc
        if (gravityMultiplier > 1.001f)
            _rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);
    }

    private void HandleStart(Vector2 pos)
    {
        if (Time.time < _nextAllowed) return;
        SnapToOrigin();
        SetTrail(false);
        _flying = false;
        _rb.drag = 0f; 
    }

    private void HandleEnd(Vector2 start, Vector2 end, float duration)
    {
        if (Time.time < _nextAllowed) return;

        Vector2 delta = end - start;
        float cm = PixelsToCm(delta.magnitude);
        if (cm < minSwipeCm) return;

        float angle = Vector2.Angle(Vector2.up, delta.normalized);
        if (angle > maxAngleFromUpDeg) return;

        
        Vector3 dir =
              cam.transform.right * (Mathf.Clamp(delta.x / (Screen.width * 0.5f), -1f, 1f) * horizontalInfluence)
            + cam.transform.up * (Mathf.Clamp(delta.y / (Screen.height * 0.5f), -1f, 1f) * verticalInfluence)
            + cam.transform.forward * forwardBias;
        dir = dir.normalized;

       
        duration = Mathf.Max(0.02f, duration);
        float speedCmPerSec = cm / duration; 
        float impulse = cm * impulsePerCm + speedCmPerSec * impulsePerCmPerSec;
        impulse = Mathf.Min(maxImpulse, impulse);

        ReleaseAndLaunch(dir * impulse);
        _nextAllowed = Time.time + cooldown;
    }

    private float PixelsToCm(float px) => (px / _dpi) * 2.54f;

    private void HoldAtOrigin()
    {
        SnapToOrigin();
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = true;
        _rb.useGravity = false;
        SetTrail(false);
    }

    private void SnapToOrigin()
    {
        if (!shotOrigin) { Debug.LogWarning("[SwipeUpLauncher] ShotOrigin not set"); return; }
        transform.SetPositionAndRotation(shotOrigin.position, shotOrigin.rotation);
    }

    private void ReleaseAndLaunch(Vector3 impulse)
    {
        _rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.drag = airDragWhileFlying;   
        _rb.AddForce(impulse, ForceMode.Impulse);
        _flying = true;
        SetTrail(true);
        OnLaunched?.Invoke();
    }

    public void PrepareNextShot()
    {
        _flying = false;
        HoldAtOrigin();
    }

    private void SetTrail(bool on) { if (trail) trail.emitting = on; }
}
