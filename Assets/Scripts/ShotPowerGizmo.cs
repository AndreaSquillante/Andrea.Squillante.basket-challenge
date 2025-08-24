using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class ShotPowerGizmo : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ShotPowerAdvisor advisor;
    [SerializeField] private BallLauncher launcher;
    [SerializeField] private ShotPhysicsProfile profile; // optional; fallback to launcher.PhysicsProfile
    [SerializeField] private Transform shotOrigin;
    [SerializeField] private Transform hoopTarget;

    [Header("Draw Toggles")]
    [SerializeField] private bool drawPerfect = true;
    [SerializeField] private bool drawMake = true;
    [SerializeField] private bool drawBackboard = true;
    [SerializeField] private bool drawPerfectEdges = false;
    [SerializeField] private bool drawHoopRadius = true;

    [Header("Colors")]
    [SerializeField] private Color perfectColor = new Color(0.2f, 1f, 0.2f, 1f);
    [SerializeField] private Color perfectEdgeColor = new Color(0.6f, 1f, 0.6f, 1f);
    [SerializeField] private Color makeColor = new Color(1f, 0.92f, 0.2f, 1f);
    [SerializeField] private Color backboardColor = new Color(0.6f, 0.6f, 1f, 1f);
    [SerializeField] private Color hoopColor = new Color(1f, 0.4f, 0.2f, 1f);

    [Header("Integration Settings")]
    [SerializeField, Min(8)] private int steps = 120;
    [SerializeField, Min(0.001f)] private float dt = 0.01f;

    private ShotPhysicsProfile ActiveProfile => profile != null ? profile : (launcher != null ? launcher.PhysicsProfile : null);

    private void OnDrawGizmos()
    {
        if (!advisor || !shotOrigin || !hoopTarget) return;
        var p = ActiveProfile;
        if (p == null) return;

        // draw hoop radius at target (for reference)
        if (drawHoopRadius)
        {
            Gizmos.color = hoopColor;
            float r = Mathf.Max(0.02f, advisor != null ? GetAdvisorHoopRadiusSafe() : 0.225f);
            Gizmos.DrawWireSphere(hoopTarget.position, r);
        }

        // compute launch direction used by advisor analytic (horizontal toward hoop + vertical influence)
        Vector3 dir = ComputeAnalyticDirection(shotOrigin.position, hoopTarget.position, p);
        if (dir.sqrMagnitude < 1e-6f) return;

        float maxImp = Mathf.Max(0.01f, p.maxImpulse);

        // draw PERFECT
        if (drawPerfect && advisor.PerfectRange.valid)
        {
            float centerPct = 0.5f * (advisor.PerfectRange.min + advisor.PerfectRange.max);
            float imp = centerPct * maxImp;
            DrawArc(shotOrigin.position, dir, imp, p, perfectColor);

            if (drawPerfectEdges)
            {
                float impMin = advisor.PerfectRange.min * maxImp;
                float impMax = advisor.PerfectRange.max * maxImp;
                DrawArc(shotOrigin.position, dir, impMin, p, perfectEdgeColor);
                DrawArc(shotOrigin.position, dir, impMax, p, perfectEdgeColor);
            }
        }

        // draw MAKE
        if (drawMake && advisor.MakeRange.valid)
        {
            float centerPct = 0.5f * (advisor.MakeRange.min + advisor.MakeRange.max);
            float imp = centerPct * maxImp;
            DrawArc(shotOrigin.position, dir, imp, p, makeColor);
        }

        // draw BACKBOARD
        if (drawBackboard && advisor.BackbdRange.valid)
        {
            float centerPct = 0.5f * (advisor.BackbdRange.min + advisor.BackbdRange.max);
            float imp = centerPct * maxImp;
            DrawArc(shotOrigin.position, dir, imp, p, backboardColor);
        }
    }

    private Vector3 ComputeAnalyticDirection(Vector3 p0, Vector3 hoop, ShotPhysicsProfile prof)
    {
        // Horizontal toward hoop in XZ
        Vector3 toHoopXZ = new Vector3(hoop.x, p0.y, hoop.z) - p0;
        float D = toHoopXZ.magnitude;
        if (D < 1e-4f) return Vector3.zero;
        Vector3 uxz = toHoopXZ / Mathf.Max(1e-4f, D);

        // Same approach as analytic fallback: horizontal + vertical
        float vInf = Mathf.Max(0.01f, prof.verticalInfluence);
        Vector3 dir = (uxz * 1f + Vector3.up * vInf).normalized;
        return dir;
    }

    private void DrawArc(Vector3 origin, Vector3 dir, float impulse, ShotPhysicsProfile prof, Color c)
    {
        Vector3 p = origin;
        Vector3 v = dir * impulse;
        float gMul = Mathf.Max(0.01f, prof.gravityMultiplier);
        float drag = Mathf.Max(0f, prof.airDragWhileFlying);

        Gizmos.color = c;
        for (int i = 0; i < steps; i++)
        {
            // integrate
            Vector3 vNext = v + Physics.gravity * gMul * dt;
            // approximate linear drag similar to Unity's drag
            vNext *= 1f / (1f + drag * dt);
            Vector3 pNext = p + vNext * dt;

            Gizmos.DrawLine(p, pNext);

            p = pNext;
            v = vNext;
        }
    }

    private float GetAdvisorHoopRadiusSafe()
    {
        // If you exposed hoopRadius as public property, use it; otherwise return a sensible default.
        // Keeping a default here to avoid reflection or tight coupling.
        return 0.225f;
    }
}
