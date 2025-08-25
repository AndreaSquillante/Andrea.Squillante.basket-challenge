using UnityEngine;
using TMPro;

public sealed class ScoreManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    public int CurrentScore => _score;

    private int _score;

    private void Awake() => UpdateUI();

    // now supports multiplier (default 1 keeps old calls working)
    public void RegisterShot(BasketShotDetector.ShotResult result, int multiplier = 1)
    {
        int points = 0;
        switch (result)
        {
            case BasketShotDetector.ShotResult.Perfect: points = 3; break;
            case BasketShotDetector.ShotResult.NonPerfect: points = 2; break;
            case BasketShotDetector.ShotResult.BackboardBasket: points = 2; break;
            case BasketShotDetector.ShotResult.NoBasket: points = 0; break;
        }
        _score += points * Mathf.Max(1, multiplier);
        UpdateUI();
    }

    // keep old signature
    public void AddBonusPoints(int pts) => AddBonusPoints(pts, 1);

    // new overload with multiplier
    public void AddBonusPoints(int pts, int multiplier)
    {
        _score += Mathf.Max(0, pts) * Mathf.Max(1, multiplier);
        UpdateUI();
    }

    // kept for compatibility with your earlier code
    public System.Action<int> TryAddRaw => AddRaw;
    private void AddRaw(int pts) { _score += Mathf.Max(0, pts); UpdateUI(); }

    private void UpdateUI()
    {
        if (scoreText) scoreText.text = _score.ToString();
    }

    public void ResetScore() { _score = 0; UpdateUI(); }
}
