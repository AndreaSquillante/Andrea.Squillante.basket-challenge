using UnityEngine;
using System.Reflection;

[RequireComponent(typeof(Collider))]
public sealed class BasketShotDetector : MonoBehaviour
{
    [Header("Score targets")]
    [SerializeField] private ScoreManager playerScore;
    [SerializeField] private ScoreManager aiScore;

    [Header("Flyer spawn")]
    [SerializeField] private ScoreFlyerSpawner playerFlyer;  // world-space spawner for Player
    [SerializeField] private ScoreFlyerSpawner aiFlyer;      // world-space spawner for AI
    [SerializeField] private Transform flyerAnchor;          // e.g. rim center; if null, uses transform

    [Header("Optional bonus")]
    [SerializeField] private BackboardBonus backboardBonus;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    public enum ShotResult { Perfect, BackboardBasket, NonPerfect, NoBasket }

    private void Reset()
    {
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = true; // this Mono must be on the net trigger
    }

    private void OnTriggerEnter(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (!rb) return;

        var tracker = rb.GetComponent<BallShotTracker>();
        var surface = rb.GetComponent<BallSurfaceResponse>();
        var owner = rb.GetComponent<BallOwner>(); // Team Player/AI

        if (!tracker || !surface)
        {
            if (debugLogs) Debug.LogWarning("[BasketShotDetector] Missing BallShotTracker or BallSurfaceResponse on ball.");
            return;
        }
        var team = (owner != null) ? owner.TeamId : BallOwner.Team.Player;

        // Classify
        ShotResult result =
            tracker.TouchedBackboard ? ShotResult.BackboardBasket :
            tracker.TouchedRim ? ShotResult.NonPerfect :
                                       ShotResult.Perfect;

        // Base points
        int basePts = result switch
        {
            ShotResult.Perfect => 3,
            ShotResult.NonPerfect => 2,
            ShotResult.BackboardBasket => 2,
            _ => 0
        };

        // Route to correct scoreboard
        var sm = (team == BallOwner.Team.AI) ? aiScore : playerScore;
        if (sm != null)
        {
            sm.RegisterShot(result);

            // Backboard bonus: add directly to scoreboard
            int bonus = 0;
            if (result == ShotResult.BackboardBasket && backboardBonus != null)
            {
                bonus = backboardBonus.ClaimBonus();
                if (bonus > 0)
                    sm.AddBonusPoints(bonus); // <-- direct call
            }

            // Spawn flyer with total awarded points (base + bonus)
            int totalAdded = basePts + bonus;
            if (totalAdded > 0)
            {
                var sp = (team == BallOwner.Team.AI) ? aiFlyer : playerFlyer;
                if (sp != null)
                {
                    Vector3 pos = flyerAnchor ? flyerAnchor.position : transform.position;
                    sp.SpawnAtWorld(pos, totalAdded);
                }
            }
        }
        else if (debugLogs)
        {
            Debug.LogWarning("[BasketShotDetector] No ScoreManager assigned for team " + team);
        }

        if (debugLogs) Debug.Log($"[BasketShotDetector] {team} -> {result} (+{basePts})");

        surface.NotifyBasketScored();
        tracker.ResetShotFlags();
    }

    // Miss from ground (call from BallSurfaceResponse when first ground touch after a shot)
    public void RegisterMiss(BallOwner.Team team)
    {
        var sm = (team == BallOwner.Team.AI) ? aiScore : playerScore;
        sm?.RegisterShot(ShotResult.NoBasket);
        if (debugLogs) Debug.Log($"[BasketShotDetector] Miss for {team}");
    }
}
