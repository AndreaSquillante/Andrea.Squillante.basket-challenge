using UnityEngine;

public sealed class ScoreFlyerSpawner : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private Canvas worldCanvas;  // World Space
    [SerializeField] private ScoreFlyer flyerPrefab;

    public void SpawnAtWorld(Vector3 worldPos, int points)
    {
        if (!worldCanvas || !flyerPrefab) return;
        if (!cam) cam = Camera.main;

        ScoreFlyer f = Instantiate(flyerPrefab, worldPos, Quaternion.identity, worldCanvas.transform);
        f.Setup(points, cam);
    }
}
