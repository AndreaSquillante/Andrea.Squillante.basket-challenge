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

    [Header("Fireball (per team)")]
    [SerializeField] private FireballController playerFireball;
    [SerializeField] private FireballController aiFireball;

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

        var sm = (team == BallOwner.Team.AI) ? aiScore : playerScore;
        var fb = (team == BallOwner.Team.AI) ? aiFireball : playerFireball;
        int mult = (fb != null && fb.IsActive) ? 2 : 1;

        if (sm != null)
        {
            // add base with multiplier
            sm.RegisterShot(result, mult);

            // optional backboard bonus (also multiplied)
            int bonus = 0;
            if (result == ShotResult.BackboardBasket && backboardBonus != null)
            {
                bonus = backboardBonus.ClaimBonus();
                if (bonus > 0) sm.AddBonusPoints(bonus, mult);
            }

            // flyer shows total awarded this event
            int totalAdded = basePts * mult + bonus * mult;
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

        // fireball: increase streak / possibly activate
        fb?.NotifyMake();

        if (debugLogs) Debug.Log($"[BasketShotDetector] {team} -> {result} (x{mult})");

        surface.NotifyBasketScored();
        tracker.ResetShotFlags();
    }

    // called from BallSurfaceResponse on first ground touch after a miss
    public void RegisterMiss(BallOwner.Team team)
    {
        var sm = (team == BallOwner.Team.AI) ? aiScore : playerScore;
        sm?.RegisterShot(ShotResult.NoBasket); // adds 0 but keeps UI consistent

        var fb = (team == BallOwner.Team.AI) ? aiFireball : playerFireball;
        fb?.NotifyMiss();

        if (debugLogs) Debug.Log($"[BasketShotDetector] Miss for {team} (fireball reset)");
    }
}
