using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BasketShotDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider netTrigger; // Trigger in zona rete
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private BackboardBonus backboardBonus;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    // in BasketShotDetector
    [SerializeField] private ScoreFlyerSpawner flyerSpawner;
    [SerializeField] private Transform flayerSpawnPos; // optional: for world position

    public enum ShotResult
    {
        Perfect,
        BackboardBasket,
        NonPerfect,
        NoBasket
    }

    private void Awake()
    {
        if (netTrigger == null)
            Debug.LogError("[BasketShotDetector] Missing netTrigger reference!");
    }

    
    private void OnTriggerEnter(Collider other)
    {
        var rb = other.attachedRigidbody; if (rb == null) return;
        var tracker = rb.GetComponent<BallShotTracker>(); if (tracker == null) return;
        var surface = rb.GetComponent<BallSurfaceResponse>(); if (surface == null) return;

        ShotResult result =
            tracker.TouchedBackboard ? ShotResult.BackboardBasket :
            (tracker.TouchedRim ? ShotResult.NonPerfect : ShotResult.Perfect);

        int bonus = 0;
        if (result == ShotResult.BackboardBasket && backboardBonus != null)
            bonus = backboardBonus.ClaimBonus();

        int awarded = scoreManager ? scoreManager.RegisterShot(result, bonus) : 0;

        if (awarded != 0 && flyerSpawner != null)
        {
            Vector3 pos = flayerSpawnPos ? flayerSpawnPos.position : other.transform.position;
            flyerSpawner.SpawnAtWorld(pos, awarded);
        }

        surface.NotifyBasketScored();
        tracker.ResetShotFlags();
    }



    public void RegisterMiss()
    {
        if (debugLogs) Debug.Log("Basket result: NoBasket");
       
        scoreManager?.RegisterShot(ShotResult.NoBasket);
    }


}
