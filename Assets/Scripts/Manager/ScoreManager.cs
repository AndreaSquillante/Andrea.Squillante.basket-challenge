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

    public void RegisterShot(BasketShotDetector.ShotResult result, int bonusPoints = 0)
    {
        int points = 0;

        switch (result)
        {
            case BasketShotDetector.ShotResult.Perfect:
                points = 3;
                break;

            case BasketShotDetector.ShotResult.NonPerfect:
            case BasketShotDetector.ShotResult.BackboardBasket:
                points = 2;
                break;

            case BasketShotDetector.ShotResult.NoBasket:
                points = 0;
                break;
        }

        points += bonusPoints;

        _score += points;
        UpdateUI();
    }

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
