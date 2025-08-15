using UnityEngine;
using TMPro;

public sealed class ScoreManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;

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

        _score += points;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = _score.ToString();
    }
}
