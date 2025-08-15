using UnityEngine;
using UnityEngine.Timeline;

public sealed class BallShotTracker : MonoBehaviour
{
    public bool TouchedRim { get; private set; }
    public bool TouchedBackboard { get; private set; }

    public void ResetShotFlags()
    {
        TouchedRim = false;
        TouchedBackboard = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.GetComponent<RimMarker>() != null)
            TouchedRim = true;

        if (collision.collider.GetComponent<BackboardMarker>() != null)
            TouchedBackboard = true;
    }
}
