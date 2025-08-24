using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class PerfectSnapGizmo : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ShotPowerAdvisor advisor;      // must have PerfectRange computed
    [SerializeField] private BallLauncher launcher;         // provides Cam and ShotOrigin
    [SerializeField] private ShotPhysicsProfile profile;    // optional; fallback to launcher.PhysicsProfile

    [Header("Draw")]
    [SerializeField] private bool drawCenter = true;
    [SerializeField] private bool drawEdges = true;
    [SerializeField] private bool recomputeOnGizmo = true;  // auto recompute ranges each frame (Editor)
    [SerializeField, Min(0f)] private float maxWorldLength = 8f;
    [SerializeField] private float headSize = 0.25f;

    [Header("Colors")]
    [SerializeField] private Color centerColor = new Color(0.2f, 1f, 0.2f, 1f);
    [SerializeField] private Color edgeColor = new Color(0.6f, 1f, 0.6f, 1f);

    private ShotPhysicsProfile ActiveProfile =>
        profile != null ? profile : (launcher != null ? launcher.PhysicsProfile : null);

    private void OnDrawGizmos()
    {
        if (!advisor || !launcher) return;
        var prof = ActiveProfile;
        if (prof == null) return;

        // make sure we have fresh ranges in editor if desired
        if (!Application.isPlaying && recomputeOnGizmo)
            advisor.Recompute();

        var r = advisor.PerfectRange;
        if (!r.valid) return;

        // compute canonical launch direction (no lateral, same math base as BallLauncher/Profile)
        Camera cam = launcher.Cam ? launcher.Cam : Camera.main;
        if (!cam) return;

        Vector3 dir =
              cam.transform.up * Mathf.Max(0.01f, prof.verticalInfluence)
            + cam.transform.forward * Mathf.Max(0.01f, prof.forwardBias);
        if (dir.sqrMagnitude < 1e-8f) return;
        dir.Normalize();

        Transform origin = launcher.ShotOrigin ? launcher.ShotOrigin : launcher.transform;
        Vector3 p0 = origin.position;

        float maxImp = Mathf.Max(0.01f, prof.maxImpulse);

        if (drawCenter)
        {
            float centerPct = 0.5f * (r.min + r.max);
            DrawArrow(p0, dir, centerPct, maxImp, centerColor);
        }

        if (drawEdges)
        {
            DrawArrow(p0, dir, r.min, maxImp, edgeColor);
            DrawArrow(p0, dir, r.max, maxImp, edgeColor);
        }
    }

    private void DrawArrow(Vector3 p0, Vector3 dir, float pct, float maxImpulse, Color c)
    {
        pct = Mathf.Clamp01(pct);
        float len = maxWorldLength * pct;

        Vector3 p1 = p0 + dir * len;

        Gizmos.color = c;
        Gizmos.DrawLine(p0, p1);

        // arrow head
        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
        Vector3 baseHead = p1 - dir * headSize;
        Gizmos.DrawLine(p1, baseHead + (right + dir) * (headSize * 0.5f));
        Gizmos.DrawLine(p1, baseHead + (-right + dir) * (headSize * 0.5f));
    }
}
