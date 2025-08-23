using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShotPowerAdvisor : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BallLauncher launcher;
    [SerializeField] private ShotPhysicsProfile profile;
    [SerializeField] private Transform shotOrigin;     // usually launcher.ShotOrigin
    [SerializeField] private Transform hoopTarget;     // empty at hoop center
    [SerializeField] private Collider rimCollider;
    [SerializeField] private Collider backboardCollider;
    [SerializeField] private Collider netTrigger;      // thin trigger under rim

    [Header("Sampling")]
    [SerializeField, Min(8)] private int samples = 40;
    [SerializeField, Min(0.005f)] private float simDt = 0.01f;
    [SerializeField, Min(0.5f)] private float simMaxTime = 4.0f;

    [Header("Tolerances")]
    [SerializeField] private float swishRadius = 0.18f;    // hoop center tolerance (m)
    [SerializeField] private float entryYSlack = 0.25f;     // slack for entry

    // --- Internal ranges (mutables)
    private Range01 _perfectRange;
    private Range01 _makeRange;
    private Range01 _backbdRange;

    // --- Public read-only properties
    public Range01 PerfectRange => _perfectRange;
    public Range01 MakeRange => _makeRange;
    public Range01 BackbdRange => _backbdRange;

    public struct Range01 { public float min; public float max; public bool valid; }

    public void Recompute()
    {
        _perfectRange = new Range01 { min = 1f, max = 0f, valid = false };
        _makeRange = new Range01 { min = 1f, max = 0f, valid = false };
        _backbdRange = new Range01 { min = 1f, max = 0f, valid = false };

        if (!launcher || !profile || !shotOrigin || !hoopTarget) return;

        // canonical direction (like swipe straight up)
        Vector3 dir = (launcher.Cam.transform.up * profile.verticalInfluence
                      + launcher.Cam.transform.forward * profile.forwardBias).normalized;

        for (int i = 0; i < samples; i++)
        {
            float t = (samples <= 1) ? 0f : (float)i / (samples - 1);
            float impulse = Mathf.Lerp(0f, profile.maxImpulse, t);

            Outcome o = Simulate(dir, impulse);
            switch (o)
            {
                case Outcome.Swish: Extend(ref _perfectRange, t); break;
                case Outcome.Make: Extend(ref _makeRange, t); break;
                case Outcome.Backboard: Extend(ref _backbdRange, t); break;
            }
        }

        ClampRange(ref _perfectRange);
        ClampRange(ref _makeRange);
        ClampRange(ref _backbdRange);
    }

    private enum Outcome { Miss, Swish, Make, Backboard }

    private void Extend(ref Range01 r, float t)
    {
        if (!r.valid) { r.min = r.max = t; r.valid = true; }
        else { r.min = Mathf.Min(r.min, t); r.max = Mathf.Max(r.max, t); }
    }

    private void ClampRange(ref Range01 r)
    {
        if (!r.valid) return;
        r.min = Mathf.Clamp01(r.min);
        r.max = Mathf.Clamp01(r.max);
        if (r.max < r.min) r.valid = false;
    }

    private Outcome Simulate(Vector3 dir, float impulse)
    {
        Vector3 p = shotOrigin.position;
        Vector3 v = dir * impulse;

        float t = 0f;
        float drag = profile.airDragWhileFlying;

        while (t < simMaxTime)
        {
            v += Physics.gravity * profile.gravityMultiplier * simDt;
            v *= 1f / (1f + drag * simDt);

            Vector3 pNext = p + v * simDt;

            if (backboardCollider && SegmentHits(backboardCollider, p, pNext)) return Outcome.Backboard;

            bool hitRim = rimCollider && SegmentHits(rimCollider, p, pNext);
            bool throughNet = netTrigger && SegmentHits(netTrigger, p, pNext);

            if (throughNet)
            {
                bool nearCenter = Vector3.Distance(pNext, hoopTarget.position) <= swishRadius;
                bool descending = v.y < 0f;
                if (nearCenter && descending && !hitRim) return Outcome.Swish;
                return Outcome.Make;
            }
            if (hitRim) return Outcome.Make;

            p = pNext;
            t += simDt;
        }
        return Outcome.Miss;
    }

    private static bool SegmentHits(Collider col, Vector3 a, Vector3 b)
    {
        Vector3 dir = b - a;
        float dist = dir.magnitude;
        if (dist <= 1e-5f) return false;
        dir /= dist;
        return col.Raycast(new Ray(a, dir), out var hit, dist);
    }
}
