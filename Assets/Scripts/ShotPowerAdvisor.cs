using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShotPowerAdvisor : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BallLauncher launcher;
    [SerializeField] private ShotPhysicsProfile profile;
    [SerializeField] private Transform shotOrigin;
    [SerializeField] private Transform hoopTarget; // center of rim, slightly below rim plane
    [SerializeField] private Camera cam;           // same camera used by BallLauncher (optional)

    [Header("Physics sampling (optional)")]
    [SerializeField] private bool tryPhysicsSampling = false;
    [SerializeField, Min(8)] private int samples = 40;
    [SerializeField, Min(0.005f)] private float simDt = 0.01f;
    [SerializeField, Min(0.5f)] private float simMaxTime = 4.0f;

    [Header("Runtime-calibrated bands (recommended)")]
    [SerializeField] private bool useRuntimeCalibrated = true;
    [SerializeField, Min(0.001f)] private float simDtCal = 0.005f;
    [SerializeField, Min(0.5f)] private float simMaxTimeCal = 5f;

    [Header("Band geometry (meters)")]
    [SerializeField] private float hoopRadius = 0.225f;     // ~0.45 m diameter
    [SerializeField] private float ballRadius = 0.12f;      // approx basketball
    [SerializeField] private float entryYOffset = -0.02f;   // aim slightly below rim center
    [SerializeField] private float perfectHorizSlack = 0.10f;
    [SerializeField] private float makeHorizSlack = 0.22f;
    [SerializeField] private float backboardExtra = 0.32f;  // extra beyond make (upper band)

    [Header("Analytic fallback (no drag)")]
    [SerializeField] private bool useAnalyticFallback = true;
    [SerializeField] private float angleMinDeg = 35f;
    [SerializeField] private float angleMaxDeg = 60f;
    [SerializeField, Min(3)] private int angleSamples = 50;

    // Computed ranges (0..1 of max impulse)
    public struct Range01 { public float min; public float max; public bool valid; }
    private Range01 _perfectRange, _makeRange, _backbdRange;
    public Range01 PerfectRange => _perfectRange;
    public Range01 MakeRange => _makeRange;
    public Range01 BackbdRange => _backbdRange;

    // ------------------------------------------------------------------

    public void Recompute()
    {
        _perfectRange = new Range01 { min = 1f, max = 0f, valid = false };
        _makeRange = new Range01 { min = 1f, max = 0f, valid = false };
        _backbdRange = new Range01 { min = 1f, max = 0f, valid = false };

        if (!launcher || !profile || !shotOrigin || !hoopTarget) return;

        bool done = false;

        if (useRuntimeCalibrated)
        {
            ComputeRuntimeCalibratedRanges();
            done = _perfectRange.valid || _makeRange.valid || _backbdRange.valid;
        }

        if (!done && tryPhysicsSampling)
        {
            done = TrySamplingRanges();
        }

        if (!done && useAnalyticFallback)
        {
            ComputeAnalyticRanges();
        }
    }

    // ------------------------------------------------------------------
    // RUNTIME-CALIBRATED BANDS (matches BallLauncher physics)
    // ------------------------------------------------------------------

    private void ComputeRuntimeCalibratedRanges()
    {
        Camera usedCam = cam ? cam : Camera.main;
        if (!usedCam) return;

        // Launch direction like BallLauncher, with zero lateral
        Vector3 dir =
              usedCam.transform.up * Mathf.Max(0.01f, profile.verticalInfluence)
            + usedCam.transform.forward * Mathf.Max(0.01f, profile.forwardBias);
        if (dir.sqrMagnitude < 1e-8f) return;
        dir.Normalize();

        Vector3 p0 = shotOrigin.position;
        Vector3 pc = hoopTarget.position + new Vector3(0f, entryYOffset, 0f);

        // Baseline in XZ for signed error (short vs beyond)
        Vector3 baseXZ = new Vector3(pc.x, p0.y, pc.z) - p0;
        float D = baseXZ.magnitude;
        if (D < 0.05f) return;
        Vector3 uxz = baseXZ / D;

        float gMul = Mathf.Max(0.01f, profile.gravityMultiplier);
        float drag = Mathf.Max(0f, profile.airDragWhileFlying);
        float maxImp = Mathf.Max(0.01f, profile.maxImpulse);

        float SignedErrorAtRim(float impulse)
        {
            Vector3 p = p0;
            Vector3 v = dir * impulse;

            float prevY = p.y;
            Vector3 prevP = p;

            float t = 0f;
            while (t < simMaxTimeCal)
            {
                // integrate velocity and position (same idea as BallLauncher)
                Vector3 vNext = v + Physics.gravity * gMul * simDtCal;
                vNext *= 1f / (1f + drag * simDtCal);
                Vector3 pNext = p + vNext * simDtCal;

                // check crossing rim plane while descending
                if (prevY > pc.y && pNext.y <= pc.y && vNext.y < 0f)
                {
                    float alpha = Mathf.InverseLerp(prevY, pNext.y, pc.y);
                    Vector3 hit = Vector3.Lerp(prevP, pNext, alpha);
                    Vector3 xzHit = new Vector3(hit.x, p0.y, hit.z);
                    Vector3 xzHoop = new Vector3(pc.x, p0.y, pc.z);

                    // signed along baseline: >0 beyond, <0 short
                    float s = Vector3.Dot((xzHit - xzHoop), uxz);
                    return s;
                }

                prevY = p.y; prevP = p;
                p = pNext; v = vNext;
                t += simDtCal;
            }
            // did not cross rim plane -> treat as short
            return -D;
        }

        // binary search to find center impulse
        float lo = 0f, hi = maxImp;
        if (SignedErrorAtRim(hi) <= 0f) return; // even max is short -> invalid layout for current maxImpulse

        float centerImpulse = hi;
        for (int i = 0; i < 18; i++)
        {
            float mid = 0.5f * (lo + hi);
            float e = SignedErrorAtRim(mid);
            if (e < 0f) lo = mid; else { hi = mid; centerImpulse = mid; }
            if (Mathf.Abs(hi - lo) <= 0.001f * maxImp) break;
        }

        // numerical slope d(error)/d(impulse)
        float eps = Mathf.Max(0.25f, 0.01f * maxImp);
        float eA = SignedErrorAtRim(Mathf.Max(0f, centerImpulse - eps));
        float eB = SignedErrorAtRim(Mathf.Min(maxImp, centerImpulse + eps));
        float slope = Mathf.Max(1e-4f, Mathf.Abs((eB - eA) / (2f * eps))); // meters per impulse unit

        // convert horizontal slack (meters) to impulse deltas
        float perfectDelta = Mathf.Max(0.005f, (hoopRadius - ballRadius) + perfectHorizSlack);
        float makeDelta = Mathf.Max(perfectDelta, hoopRadius + makeHorizSlack);
        float backMin = hoopRadius + backboardExtra;
        float backMax = backMin + 0.25f;

        float dImpPerfect = perfectDelta / slope;
        float dImpMake = makeDelta / slope;
        float dImpBackMin = backMin / slope;
        float dImpBackMax = backMax / slope;

        // impulse bands
        float pMinImp = Mathf.Max(0f, centerImpulse - dImpPerfect);
        float pMaxImp = Mathf.Min(maxImp, centerImpulse + dImpPerfect);

        float mMinImp = Mathf.Max(0f, centerImpulse - dImpMake);
        float mMaxImp = Mathf.Min(maxImp, centerImpulse + dImpMake);

        float bMinImp = Mathf.Clamp(centerImpulse + dImpBackMin, 0f, maxImp);
        float bMaxImp = Mathf.Clamp(centerImpulse + dImpBackMax, 0f, maxImp);

        // map to 0..1
        _perfectRange = new Range01 { min = pMinImp / maxImp, max = pMaxImp / maxImp, valid = pMaxImp > pMinImp };
        _makeRange = new Range01 { min = mMinImp / maxImp, max = mMaxImp / maxImp, valid = mMaxImp > mMinImp };
        _backbdRange = new Range01 { min = bMinImp / maxImp, max = bMaxImp / maxImp, valid = bMaxImp > bMinImp };

        // ensure UI-visible widths
        EnsureMinWidth(ref _perfectRange, 0.06f);
        EnsureMinWidth(ref _makeRange, 0.10f);
        EnsureMinWidth(ref _backbdRange, 0.08f);

        // make includes perfect (for UI you can clip overlaps)
        if (_perfectRange.valid && _makeRange.valid)
        {
            _makeRange.min = Mathf.Min(_makeRange.min, _perfectRange.min);
            _makeRange.max = Mathf.Max(_makeRange.max, _perfectRange.max);
        }
    }

    private void EnsureMinWidth(ref Range01 r, float minPctWidth)
    {
        if (!r.valid) return;
        float w = r.max - r.min;
        if (w < minPctWidth)
        {
            float c = 0.5f * (r.min + r.max);
            float h = 0.5f * minPctWidth;
            r.min = Mathf.Clamp01(c - h);
            r.max = Mathf.Clamp01(c + h);
        }
    }

    // ------------------------------------------------------------------
    // SIMPLE ANALYTIC FALLBACK (no drag) - kept as backup
    // ------------------------------------------------------------------

    private void ComputeAnalyticRanges()
    {
        Vector3 p0 = shotOrigin.position;
        Vector3 pc = hoopTarget.position + new Vector3(0f, entryYOffset, 0f);

        Vector3 toHoopXZ = new Vector3(pc.x, p0.y, pc.z) - p0;
        float D = toHoopXZ.magnitude;
        float dY = pc.y - p0.y;
        if (D < 0.05f) return;

        float g = Mathf.Max(0.01f, Physics.gravity.magnitude) * Mathf.Max(0.01f, profile.gravityMultiplier);
        float maxImp = Mathf.Max(0.01f, profile.maxImpulse);

        bool TrySpeedFor(float deg, float dist, out float v)
        {
            v = 0f;
            float rad = deg * Mathf.Deg2Rad;
            float tan = Mathf.Tan(rad);
            float cos2 = Mathf.Cos(rad);
            cos2 *= cos2;

            float denom = (dist * tan - dY);
            if (denom <= 1e-6f || cos2 <= 1e-6f) return false;

            float v2 = (g * dist * dist) / (2f * cos2 * denom);
            if (v2 <= 1e-6f) return false;
            v = Mathf.Sqrt(v2);
            return true;
        }

        float bestV = float.PositiveInfinity;
        bool found = false;
        for (int i = 0; i < angleSamples; i++)
        {
            float t = angleSamples <= 1 ? 0f : (float)i / (angleSamples - 1);
            float ang = Mathf.Lerp(angleMinDeg, angleMaxDeg, t);
            if (TrySpeedFor(ang, D, out float v) && v < bestV)
            {
                bestV = v;
                found = true;
            }
        }
        if (!found) return;

        float perfectDelta = Mathf.Max(0.005f, (hoopRadius - ballRadius) + perfectHorizSlack);
        float makeDelta = Mathf.Max(perfectDelta, hoopRadius + makeHorizSlack);
        float backMinDelta = hoopRadius + backboardExtra;
        float backMaxDelta = Mathf.Max(backMinDelta + 0.1f, makeDelta * 1.8f);

        bool SolveBand(float delta, out float vMin, out float vMax)
        {
            vMin = vMax = 0f;
            float Dlow = Mathf.Max(0.01f, D - delta);
            float Dhigh = D + delta;

            bool okL = TrySpeedFor(angleMinDeg + (angleMaxDeg - angleMinDeg) * 0.5f, Dlow, out float vL);
            bool okH = TrySpeedFor(angleMinDeg + (angleMaxDeg - angleMinDeg) * 0.5f, Dhigh, out float vH);
            if (!okL && !okH) return false;

            if (okL && okH) { vMin = Mathf.Min(vL, vH); vMax = Mathf.Max(vL, vH); return true; }
            if (okL) { vMin = vL; vMax = vL * 1.02f; return true; }
            vMin = vH * 0.98f; vMax = vH; return true;
        }

        if (SolveBand(perfectDelta, out float vPmin, out float vPmax))
            _perfectRange = new Range01 { min = Mathf.Clamp01(vPmin / maxImp), max = Mathf.Clamp01(vPmax / maxImp), valid = vPmax > vPmin };

        if (SolveBand(makeDelta, out float vMmin, out float vMmax))
        {
            _makeRange = new Range01 { min = Mathf.Clamp01(vMmin / maxImp), max = Mathf.Clamp01(vMmax / maxImp), valid = vMmax > vMmin };
            if (_perfectRange.valid)
            {
                _makeRange.min = Mathf.Min(_makeRange.min, _perfectRange.min);
                _makeRange.max = Mathf.Max(_makeRange.max, _perfectRange.max);
            }
        }

        bool ok1 = TrySpeedFor(angleMinDeg + (angleMaxDeg - angleMinDeg) * 0.5f, D + backMinDelta, out float vB1);
        bool ok2 = TrySpeedFor(angleMinDeg + (angleMaxDeg - angleMinDeg) * 0.5f, D + backMaxDelta, out float vB2);
        if (ok1 && ok2)
        {
            float vBmin = Mathf.Min(vB1, vB2);
            float vBmax = Mathf.Max(vB1, vB2);
            _backbdRange = new Range01 { min = Mathf.Clamp01(vBmin / maxImp), max = Mathf.Clamp01(vBmax / maxImp), valid = vBmax > vBmin };
        }

        EnsureMinWidth(ref _perfectRange, 0.06f);
        EnsureMinWidth(ref _makeRange, 0.10f);
        EnsureMinWidth(ref _backbdRange, 0.08f);
    }

    // ------------------------------------------------------------------
    // PLACEHOLDER FOR COLLIDER-BASED SAMPLING (optional)
    // ------------------------------------------------------------------

    private enum Outcome { Miss, Swish, Make, Backboard } // not used here

    private bool TrySamplingRanges()
    {
        // If you want to implement collider-based sampling, do it here.
        // Returning false so Recompute() falls back to calibrated or analytic modes.
        return false;
    }
}
