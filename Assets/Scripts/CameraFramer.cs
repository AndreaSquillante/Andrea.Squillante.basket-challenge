using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class CameraFramer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BallLauncher launcher;     // provides ShotOrigin and event
    [SerializeField] private Transform hoop;            // empty above the rim

    [Header("Behind Shooter Settings")]
    [SerializeField, Min(0f)] private float distanceBehindShooter = 120f;
    [SerializeField] private float sideOffset = 0f;
    [SerializeField, Min(0f)] private float height = 100f;
    [SerializeField] private float lookHoopHeightOffset = -7f; // allow negative

    [Header("Smoothing (Play Mode only)")]
    [SerializeField] private float moveLerp = 8f;
    [SerializeField] private float rotateLerp = 12f;

    [Header("Collision Avoidance")]
    [SerializeField] private bool avoidClipping = false;
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float probeRadius = 0.2f;

    [Header("Editor")]
    [SerializeField] private bool autoFindLauncherInEditor = true;
    [SerializeField] private float minBaseline = 0.75f; // safety for degenerate layouts

    private Transform _shotOrigin;
    private Vector3 _targetPos;
    private Vector3 _targetLook;
    private bool _subscribed;

    private void OnEnable()
    {
        TryBindLauncher();
        CacheShotOrigin();
        Reframe(instant: true);
        EnsureDecentFOV();
    }

    private void OnDisable()
    {
        UnsubscribeLauncher();
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        TryBindLauncher();
        CacheShotOrigin();
        Reframe(instant: true);
        EnsureDecentFOV();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            CacheShotOrigin();
            Reframe(instant: true);
        }
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying) return;

        transform.position = Vector3.Lerp(transform.position, _targetPos, moveLerp * Time.deltaTime);

        var desiredRot = Quaternion.LookRotation((_targetLook - transform.position).normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotateLerp * Time.deltaTime);
    }

    private void TryBindLauncher()
    {
        if (launcher == null && autoFindLauncherInEditor)
            launcher = FindObjectOfType<BallLauncher>();

        if (launcher != null && !_subscribed)
        {
            launcher.OnShotOriginChanged += HandleShotOriginChanged;
            _subscribed = true;
        }
    }

    private void UnsubscribeLauncher()
    {
        if (_subscribed && launcher != null)
        {
            launcher.OnShotOriginChanged -= HandleShotOriginChanged;
            _subscribed = false;
        }
    }

    private void HandleShotOriginChanged(Transform newOrigin)
    {
        _shotOrigin = newOrigin;
        if (Application.isPlaying) Reframe(instant: false);
        else Reframe(instant: true);
    }

    private void CacheShotOrigin()
    {
        if (launcher != null) _shotOrigin = launcher.ShotOrigin;
    }

    private void Reframe(bool instant)
    {
        if (_shotOrigin == null || hoop == null) return;

        Vector3 pShooter = _shotOrigin.position;
        Vector3 pHoop = hoop.position;

        Vector3 toHoop = (pHoop - pShooter);
        float len = toHoop.magnitude;
        if (len < minBaseline) toHoop = (len > 1e-4f ? toHoop / len : Vector3.forward);
        else toHoop /= len;

        Vector3 lateral = Vector3.Cross(Vector3.up, toHoop).normalized;

        Vector3 desired = pShooter
                - toHoop * Mathf.Max(distanceBehindShooter, 0f)
                + lateral * sideOffset
                + Vector3.up * height;

        Vector3 look = pHoop + Vector3.up * lookHoopHeightOffset;

        if (avoidClipping)
            desired = ResolveClipping(desired, look);

        if (!Application.isPlaying || instant)
        {
            transform.position = desired;
            transform.rotation = Quaternion.LookRotation((look - desired).normalized, Vector3.up);
        }

        _targetPos = desired;
        _targetLook = look;
    }

    private Vector3 ResolveClipping(Vector3 desired, Vector3 lookTarget)
    {
        Vector3 dir = desired - lookTarget;
        float dist = dir.magnitude;
        if (dist <= 0.001f) return desired;
        dir /= dist;

        if (Physics.SphereCast(lookTarget, probeRadius, dir, out var hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
            return hit.point - dir * 0.05f;

        return desired;
    }

    private void EnsureDecentFOV()
    {
        var cam = GetComponent<Camera>();
        if (cam && cam.fieldOfView < 40f) cam.fieldOfView = 60f;
    }

    public void ForceReframeImmediate()
    {
        CacheShotOrigin();
        Reframe(true);
    }
}
