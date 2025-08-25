using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public sealed class AIShooterController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BallLauncher launcher;
    [SerializeField] private ShotPowerAdvisor advisor;
    [SerializeField] private BackboardBonus backboardBonus;
    [SerializeField] private AIDifficultyProfile difficulty;

    [Header("Run")]
    [SerializeField] private bool obeyGameFlow = true; // play only in Gameplay
    [SerializeField] private bool autoStart = true;    // used only if not obeying flow

    private Coroutine _loop;

    private void OnEnable()
    {
        if (obeyGameFlow) BasicFlowManager.OnStateChanged += HandleFlow;
    }

    private void OnDisable()
    {
        if (obeyGameFlow) BasicFlowManager.OnStateChanged -= HandleFlow;
        StopAI();
    }

    private void Start()
    {
        if (obeyGameFlow)
        {
            var fm = BasicFlowManager.Instance;
            HandleFlow(fm ? fm.Current : BasicFlowManager.State.MainMenu);
        }
        else if (autoStart)
        {
            StartAI();
        }
    }

    private void HandleFlow(BasicFlowManager.State s)
    {
        if (s == BasicFlowManager.State.Gameplay) StartAI();
        else StopAI();
    }

    public void StartAI()
    {
        if (_loop != null) return;
        _loop = StartCoroutine(AILoop());
    }

    public void StopAI()
    {
        if (_loop == null) return;
        StopCoroutine(_loop);
        _loop = null;
    }

    private bool IsGameplay()
    {
        var fm = BasicFlowManager.Instance;
        return fm && fm.Current == BasicFlowManager.State.Gameplay;
    }

    private IEnumerator AILoop()
    {
        if (difficulty == null || launcher == null) yield break;
        if (difficulty.initialDelay > 0f) yield return new WaitForSeconds(difficulty.initialDelay);

        while (true)
        {
            // gate on flow
            if (obeyGameFlow && !IsGameplay()) { yield return null; continue; }

            float wait = Random.Range(difficulty.minShotInterval, difficulty.maxShotInterval);
            float t = 0f;
            while (t < wait)
            {
                if (obeyGameFlow && !IsGameplay()) { yield return null; goto ContinueLoop; }
                t += Time.deltaTime;
                yield return null;
            }

            // wait until the ball is ready
            while (!launcher.IsReadyForShot)
            {
                if (obeyGameFlow && !IsGameplay()) { yield return null; goto ContinueLoop; }
                yield return null;
            }

            // choose outcome and shoot
            Outcome o = ChooseOutcome();
            float impulsePct = PickImpulsePct(o);
            float lateral01 = PickLateral01(o);
            launcher.LaunchAI(impulsePct, lateral01);

        ContinueLoop:
            yield return null;
        }
    }

    private enum Outcome { Perfect, Make, Backboard, Miss }

    private Outcome ChooseOutcome()
    {
        float wP = difficulty.weightPerfect;
        float wM = difficulty.weightMake;
        float wB = difficulty.weightBackboard;
        float wX = difficulty.weightMiss;

        if (difficulty.adaptToBackboardBonus && backboardBonus != null)
        {
            // requires BackboardBonus.IsActive { get; }
            var prop = backboardBonus.GetType().GetProperty("IsActive");
            if (prop != null && prop.PropertyType == typeof(bool))
            {
                bool active = (bool)prop.GetValue(backboardBonus, null);
                if (active) wB *= Mathf.Max(1f, difficulty.backboardWeightBoost);
            }
        }

        float sum = Mathf.Max(0.0001f, wP + wM + wB + wX);
        float r = Random.value * sum;

        if ((r -= wP) <= 0f) return Outcome.Perfect;
        if ((r -= wM) <= 0f) return Outcome.Make;
        if ((r -= wB) <= 0f) return Outcome.Backboard;
        return Outcome.Miss;
    }

    private float PickImpulsePct(Outcome o)
    {
        if (advisor == null)
        {
            float basePct = 0.6f;
            return Mathf.Clamp01(basePct + Random.Range(-difficulty.powerJitterPct, difficulty.powerJitterPct));
        }

        advisor.Recompute();
        var p = advisor.PerfectRange;
        var m = advisor.MakeRange;
        var b = advisor.BackbdRange;

        float pct;
        switch (o)
        {
            case Outcome.Perfect:
                if (p.valid)
                {
                    float center = 0.5f * (p.min + p.max);
                    pct = difficulty.snapInsidePerfect ? center : Random.Range(p.min, p.max);
                }
                else if (m.valid) pct = Mathf.Clamp01(0.5f * (m.min + m.max));
                else pct = 0.6f;
                break;

            case Outcome.Make:
                if (m.valid)
                {
                    float min = m.min, max = m.max;
                    if (p.valid)
                    {
                        float leftW = Mathf.Max(0f, p.min - m.min);
                        float rightW = Mathf.Max(0f, m.max - p.max);
                        if (leftW > rightW && leftW > 0.02f) { min = m.min; max = p.min; }
                        else if (rightW > 0.02f) { min = p.max; max = m.max; }
                    }
                    pct = Random.Range(min, max);
                }
                else if (p.valid) pct = Mathf.Clamp01(0.5f * (p.min + p.max) + 0.05f);
                else pct = 0.6f;
                break;

            case Outcome.Backboard:
                if (b.valid) pct = Random.Range(b.min, b.max);
                else if (m.valid) pct = Mathf.Clamp01(m.max + 0.05f);
                else pct = 0.7f;
                break;

            default: // Miss
                float below = m.valid ? Mathf.Clamp01(m.min - 0.08f) : 0.35f;
                float above = (b.valid ? Mathf.Clamp01(b.max + 0.08f) :
                               (m.valid ? Mathf.Clamp01(m.max + 0.12f) : 0.85f));
                pct = (Random.value < 0.5f) ? below : above;
                break;
        }

        pct += Random.Range(-difficulty.powerJitterPct, difficulty.powerJitterPct);
        return Mathf.Clamp01(pct);
    }

    private float PickLateral01(Outcome o)
    {
        float deg = Random.Range(-difficulty.lateralNoiseDeg, difficulty.lateralNoiseDeg);

        float maxYaw = 12f;
        var f = typeof(BallLauncher).GetField("maxYawFromSwipeDeg",
                 System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (f != null) maxYaw = (float)f.GetValue(launcher);

        return Mathf.Clamp(deg / Mathf.Max(1f, maxYaw), -1f, 1f);
    }
}
