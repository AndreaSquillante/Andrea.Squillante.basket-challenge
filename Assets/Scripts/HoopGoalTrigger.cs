using UnityEngine;
using UnityEngine.WSA;

[RequireComponent(typeof(Collider))]
public sealed class HoopGoalTrigger : MonoBehaviour
{
    [SerializeField] private bool requireDownwardVelocity = true;
    [SerializeField, Tooltip("Minimum downward speed to accept a goal (if requireDownwardVelocity = true)")]
    private float minDownwardSpeed = 0.1f;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        gameObject.tag = "NetTrigger";
    }

    private void OnTriggerEnter(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (!rb) return;

        // Must be the basketball
        if (!rb.TryGetComponent<BallIdentifier>(out _)) return;

        //ensure ball is going down when it crosses the net
        if (requireDownwardVelocity && rb.velocity.y > -minDownwardSpeed) return;

        Debug.Log("Got it!");
    }

}
