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
        var rb = other.attachedRigidbody;
        if (rb == null) return;

        var tracker = rb.GetComponent<BallShotTracker>();
        if (tracker == null) return;

        var surface = rb.GetComponent<BallSurfaceResponse>();
        if (surface == null) return;

        ShotResult result;
        if (tracker.TouchedBackboard)
            result = ShotResult.BackboardBasket;
        else if (tracker.TouchedRim)
            result = ShotResult.NonPerfect;
        else
            result = ShotResult.Perfect;

        if (debugLogs) Debug.Log($"Basket result: {result}");

        int bonus = 0;
        if (result == ShotResult.BackboardBasket && backboardBonus != null)
            bonus = backboardBonus.ClaimBonus();

        scoreManager?.RegisterShot(result, bonus);

        surface.NotifyBasketScored();
        tracker.ResetShotFlags();
    }


    public void RegisterMiss()
    {
        if (debugLogs) Debug.Log("Basket result: NoBasket");
       
        scoreManager?.RegisterShot(ShotResult.NoBasket);
    }


}
