using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class LaunchDirectionPreview : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private UnifiedPointerInput input;
    [SerializeField] private BallLauncher launcher;
    [SerializeField] private ShotPowerAdvisor advisor; // optional (for zone colors)
    [SerializeField] private LineRenderer line;        // raw preview (world-space)
    [SerializeField] private LineRenderer snappedLine; // snapped-to-perfect preview (world-space, optional)

    [Header("Visual")]
    [SerializeField, Min(0f)] private float maxWorldLength = 8f;
    [SerializeField] private float headSize = 0.25f;
    [SerializeField] private bool hideWhenIdle = true;

    [Header("Colors")]
    [SerializeField] private Color baseColor = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField] private Color perfectColor = new Color(0.2f, 1f, 0.2f, 0.95f);
    [SerializeField] private Color makeColor = new Color(1f, 0.92f, 0.2f, 0.95f);
    [SerializeField] private Color backboardColor = new Color(0.6f, 0.6f, 1f, 0.95f);
    [SerializeField] private Color snappedColor = new Color(0.2f, 1f, 0.2f, 1f);

    [Header("Snap-to-Perfect Preview")]
    [SerializeField] private bool previewSnapToPerfect = true;
    [SerializeField, Range(0f, 0.05f)] private float previewSnapPad = 0.02f; // extra tolerance
    [SerializeField] private bool clampLateralInPerfect = true;
    [SerializeField, Range(0f, 1f)] private float maxLateralInPerfect = 0.20f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = false;
    [SerializeField] private Color gizmoColor = new Color(1f, 1f, 1f, 0.6f);

    private Vector2 _start;
    private Vector2 _curr;
    private bool _aiming;

    private float _rawPct;          // 0..1
    private Vector3 _rawDirWS;      // normalized
    private float _snapPct;         // 0..1
    private Vector3 _snapDirWS;     // normalized
    private bool _hasSnapPreview;

    private void Reset()
    {
        line = GetComponent<LineRenderer>();
    }

    private void OnEnable()
    {
        if (!input) input = FindObjectOfType<UnifiedPointerInput>();
        Subscribe();
        SetupLine(line);
        SetupLine(snappedLine);
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (input == null) return;
        input.OnDragStart += HandleStart;
        input.OnDrag += HandleDrag;
        input.OnDragEnd += HandleEnd;
    }

    private void Unsubscribe()
    {
        if (input == null) return;
        input.OnDragStart -= HandleStart;
        input.OnDrag -= HandleDrag;
        input.OnDragEnd -= HandleEnd;
    }

    private static void SetupLine(LineRenderer lr)
    {
        if (!lr) return;
        lr.useWorldSpace = true;
        lr.positionCount = 4; // line + simple arrow head
        lr.enabled = true;    // visibility controlled below
    }

    private void HandleStart(Vector2 s)
    {
        _aiming = true;
        _start = s;
        _curr = s;
        Recompute();
        UpdateLines();
    }

    private void HandleDrag(Vector2 c)
    {
        if (!_aiming) return;
        _curr = c;
        Recompute();
        UpdateLines();
    }

    private void HandleEnd(Vector2 s, Vector2 e, float d)
    {
        _aiming = false;
        UpdateLines();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            Recompute();
            UpdateLines();
        }
    }

    private void Recompute()
    {
        _rawPct = 0f;
        _snapPct = 0f;
        _rawDirWS = Vector3.forward;
        _snapDirWS = _rawDirWS;
        _hasSnapPreview = false;

        if (!launcher || launcher.PhysicsProfile == null) return;

        Vector2 delta = _aiming ? (_curr - _start) : new Vector2(0f, 200f);
        float sqrMag = delta.sqrMagnitude;
        if (sqrMag < 1e-6f)
        {
            _rawPct = 0f;
            _rawDirWS = launcher.transform.forward;
            return;
        }

        var profile = launcher.PhysicsProfile;

        float cm = launcher.ScreenPixelsToCm(Mathf.Sqrt(sqrMag));
        float durSec = Mathf.Max(0.02f, _aiming ? Mathf.Max(0.02f, input.DragDuration) : 0.2f);
        float speedCmPerSec = cm / durSec;
        float impulse = Mathf.Min(profile.maxImpulse, cm * profile.impulsePerCm + speedCmPerSec * profile.impulsePerCmPerSec);
        _rawPct = Mathf.Clamp01(impulse / profile.maxImpulse);

        float lateral = Mathf.Clamp(delta.x / (Screen.width * 0.5f), -1f, 1f);
        float vertical = Mathf.Clamp(delta.y / (Screen.height * 0.5f), -1f, 1f);

        Camera cam = launcher.Cam ? launcher.Cam : Camera.main;

        Vector3 dir =
              cam.transform.right * (lateral * profile.horizontalInfluence)
            + cam.transform.up * (vertical * profile.verticalInfluence)
            + cam.transform.forward * profile.forwardBias;
        _rawDirWS = dir.sqrMagnitude > 1e-8f ? dir.normalized : cam.transform.forward;

        // Snap-to-perfect preview
        _snapPct = _rawPct;
        _snapDirWS = _rawDirWS;
        _hasSnapPreview = false;

        if (previewSnapToPerfect && advisor != null)
        {
            var r = advisor.PerfectRange;
            if (r.valid)
            {
                float pMin = Mathf.Max(0f, r.min - previewSnapPad);
                float pMax = Mathf.Min(1f, r.max + previewSnapPad);

                if (_rawPct >= pMin && _rawPct <= pMax)
                {
                    float centerPct = 0.5f * (r.min + r.max);
                    _snapPct = Mathf.Clamp01(centerPct);

                    float latAdj = lateral;
                    if (clampLateralInPerfect)
                        latAdj = Mathf.Clamp(lateral, -maxLateralInPerfect, maxLateralInPerfect);

                    Vector3 dirSnap =
                          cam.transform.right * (latAdj * profile.horizontalInfluence)
                        + cam.transform.up * (vertical * profile.verticalInfluence)
                        + cam.transform.forward * profile.forwardBias;

                    _snapDirWS = dirSnap.sqrMagnitude > 1e-8f ? dirSnap.normalized : _rawDirWS;
                    _hasSnapPreview = true;
                }
            }
        }
    }

    private void UpdateLines()
    {
        Transform origin = launcher && launcher.ShotOrigin ? launcher.ShotOrigin : (launcher ? launcher.transform : transform);
        Vector3 p0 = origin.position;

        bool show = _aiming || !Application.isPlaying;
        if (line)
        {
            line.enabled = show && (!hideWhenIdle || _aiming);
            if (line.enabled)
            {
                float len = maxWorldLength * _rawPct;
                Vector3 p1 = p0 + _rawDirWS * len;

                Color c = baseColor;
                if (advisor != null)
                {
                    var rP = advisor.PerfectRange;
                    var rM = advisor.MakeRange;
                    var rB = advisor.BackbdRange;
                    if (rP.valid && _rawPct >= rP.min && _rawPct <= rP.max) c = perfectColor;
                    else if (rB.valid && _rawPct >= rB.min && _rawPct <= rB.max) c = backboardColor;
                    else if (rM.valid && _rawPct >= rM.min && _rawPct <= rM.max) c = makeColor;
                }

                line.startColor = c;
                line.endColor = c;
                SetArrow(line, p0, p1, _rawDirWS);
            }
        }

        if (snappedLine)
        {
            snappedLine.enabled = show && _hasSnapPreview && (!hideWhenIdle || _aiming);
            if (snappedLine.enabled)
            {
                float len = maxWorldLength * _snapPct;
                Vector3 p1 = p0 + _snapDirWS * len;
                snappedLine.startColor = snappedColor;
                snappedLine.endColor = snappedColor;
                SetArrow(snappedLine, p0, p1, _snapDirWS);
            }
        }
    }

    private void SetArrow(LineRenderer lr, Vector3 p0, Vector3 p1, Vector3 dir)
    {
        lr.positionCount = 4;
        lr.SetPosition(0, p0);
        lr.SetPosition(1, p1);

        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
        Vector3 headBase = p1 - dir * headSize;
        Vector3 h1 = headBase + (right + dir) * (headSize * 0.5f);
        Vector3 h2 = headBase + (-right + dir) * (headSize * 0.5f);
        lr.SetPosition(2, h1);
        lr.SetPosition(3, h2);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || launcher == null) return;
        Transform origin = launcher.ShotOrigin != null ? launcher.ShotOrigin : launcher.transform;
        Vector3 p0 = origin.position;

        float len = maxWorldLength * _rawPct;
        Vector3 p1 = p0 + _rawDirWS * len;

        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(p0, p1);
    }
}
