using UnityEngine;
using TMPro;

public sealed class ScoreManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    public int CurrentScore => _score;

    private int _score;

    private void Awake()
    {
        UpdateUI();
    }

    public void RegisterShot(BasketShotDetector.ShotResult result)
    {
        int points = 0;
        switch (result)
        {
            case BasketShotDetector.ShotResult.Perfect: points = 3; break;
            case BasketShotDetector.ShotResult.NonPerfect: points = 2; break;
            case BasketShotDetector.ShotResult.BackboardBasket: points = 2; break;
            case BasketShotDetector.ShotResult.NoBasket: points = 0; break;
        }
        _score += points;
        UpdateUI();
    }

    // Optional helper to add raw bonus points (for backboard bonus etc.)
    public System.Action<int> TryAddRaw => AddRaw;
    private void AddRaw(int pts)
    {
        _score += Mathf.Max(0, pts);
        UpdateUI();
    }
    public void AddBonusPoints(int pts) { _score += Mathf.Max(0, pts); UpdateUI(); }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = _score.ToString();
    }

    public void ResetScore()
    {
        _score = 0;
        UpdateUI();
    }

}
