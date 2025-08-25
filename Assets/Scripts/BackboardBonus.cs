using UnityEngine;

public sealed class BackboardBonus : MonoBehaviour
{
    public enum BonusRarity { None, Common, Rare, VeryRare }

    [Header("Spawn Chances (0-1)")]
    [Range(0f, 1f)] public float chanceCommon = 0.25f;
    [Range(0f, 1f)] public float chanceRare = 0.1f;
    [Range(0f, 1f)] public float chanceVeryRare = 0.05f;

    [Header("Points")]
    public int pointsCommon = 4;
    public int pointsRare = 6;
    public int pointsVeryRare = 8;

    [Header("Visuals")]
    [SerializeField] private GameObject markerVisual;
    [SerializeField] private Material commonMaterial;
    [SerializeField] private Material rareMaterial;
    [SerializeField] private Material veryRareMaterial;

    private BonusRarity _activeRarity = BonusRarity.None;
    private Renderer _markerRenderer;
    public bool IsActive => _activeRarity != BonusRarity.None;
    private void Start()
    {
        TrySpawnBonus();    
    }
    private void Awake()
    {
        if (markerVisual != null)
            _markerRenderer = markerVisual.GetComponent<Renderer>();
    }

    /// <summary>
    /// Called externally at each new shot (e.g. from BallSurfaceResponse when preparing next shot).
    /// </summary>
    public void TrySpawnBonus()
    {
        _activeRarity = RollRarity();

        if (_activeRarity == BonusRarity.None)
        {
            if (markerVisual) markerVisual.SetActive(false);
        }
        else
        {
            if (markerVisual) markerVisual.SetActive(true);
            ApplyVisual(_activeRarity);
        }
    }

    /// <summary>
    /// Called when a miss happens to clear the marker.
    /// </summary>
    public void ResetBonus()
    {
        _activeRarity = BonusRarity.None;
        if (markerVisual) markerVisual.SetActive(false);
    }

    private BonusRarity RollRarity()
    {
        float roll = Random.value;

        if (roll < chanceVeryRare) return BonusRarity.VeryRare;
        if (roll < chanceVeryRare + chanceRare) return BonusRarity.Rare;
        if (roll < chanceVeryRare + chanceRare + chanceCommon) return BonusRarity.Common;
        return BonusRarity.None;
    }

    private void ApplyVisual(BonusRarity rarity)
    {
        if (_markerRenderer == null) return;

        switch (rarity)
        {
            case BonusRarity.Common: _markerRenderer.sharedMaterial = commonMaterial; break;
            case BonusRarity.Rare: _markerRenderer.sharedMaterial = rareMaterial; break;
            case BonusRarity.VeryRare: _markerRenderer.sharedMaterial = veryRareMaterial; break;
        }
    }

    public int ClaimBonus()
    {
        int bonus = 0;

        switch (_activeRarity)
        {
            case BonusRarity.Common: bonus = pointsCommon; break;
            case BonusRarity.Rare: bonus = pointsRare; break;
            case BonusRarity.VeryRare: bonus = pointsVeryRare; break;
        }

        ResetBonus(); // reset always after claim
        return bonus;
    }
}
