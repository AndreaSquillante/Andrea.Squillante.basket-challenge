using UnityEngine;

public sealed class PowerBarZonesBinder : MonoBehaviour
{
    [SerializeField] private BallLauncher launcher;
    [SerializeField] private PowerBarZonesUI zonesUI;

    private void OnEnable()
    {
        if (launcher != null)
            launcher.OnShotOriginChanged += HandleShotOriginChanged;
    }

    private void OnDisable()
    {
        if (launcher != null)
            launcher.OnShotOriginChanged -= HandleShotOriginChanged;
    }

    private void Start()
    {
        // 1° calcolo all'avvio (quando la UI ha dimensioni valide)
        zonesUI?.RefreshZones();
    }

    private void HandleShotOriginChanged(Transform t)
    {
        zonesUI?.RefreshZones();
    }
}
