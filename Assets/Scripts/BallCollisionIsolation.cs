using UnityEngine;

public sealed class BallCollisionIsolation : MonoBehaviour
{
    [SerializeField] private Transform playerBallRoot;
    [SerializeField] private Transform aiBallRoot;
    [SerializeField] private bool includeTriggers = true;

    private void OnEnable()
    {
        if (!playerBallRoot || !aiBallRoot) return;

        var colsA = playerBallRoot.GetComponentsInChildren<Collider>(true);
        var colsB = aiBallRoot.GetComponentsInChildren<Collider>(true);

        foreach (var a in colsA)
            foreach (var b in colsB)
            {
                if (!a || !b) continue;
                if (!includeTriggers && (a.isTrigger || b.isTrigger)) continue;
                Physics.IgnoreCollision(a, b, true);
            }
    }
}
