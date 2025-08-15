using UnityEngine;

public sealed class BasketShotDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider netTrigger; // Trigger in zona rete

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    public enum ShotResult
    {
        Perfect,
        BackboardBasket,
        NonPerfect,
        NoBasket
    }

    public System.Action<ShotResult> OnShotResult;

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

        // Decide il tipo di canestro
        ShotResult result;
        if (tracker.TouchedBackboard)
            result = ShotResult.BackboardBasket;
        else if (tracker.TouchedRim)
            result = ShotResult.NonPerfect;
        else
            result = ShotResult.Perfect;

        if (debugLogs) Debug.Log($"Basket result: {result}");

        OnShotResult?.Invoke(result);

        // Notifica la palla che ha segnato
        if (surface != null)
            surface.NotifyBasketScored();

        // Reset per prossimo tiro
        tracker.ResetShotFlags();
    }

    public void RegisterMiss()
    {
        if (debugLogs) Debug.Log("Basket result: NoBasket");
        OnShotResult?.Invoke(ShotResult.NoBasket);
    }
}
