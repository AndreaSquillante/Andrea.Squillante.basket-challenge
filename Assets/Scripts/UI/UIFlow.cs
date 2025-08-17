using UnityEngine;
using TMPro;

public sealed class UIFlow : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject rewardPanel;

    [Header("Labels")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text rewardText;

    public void ShowMainMenu()
    {
        SetActive(mainMenuPanel);
        if (titleText) titleText.text = "Main Menu";
    }

    public void ShowGameplay()
    {
        SetActive(gameplayPanel);
        if (titleText) titleText.text = "Gameplay";
    }

    public void ShowReward(int finalScore = 0)
    {
        SetActive(rewardPanel);
        if (titleText) titleText.text = "Match Over!";
        if (rewardText) rewardText.text = $"Final Score: {finalScore}";
    }

    private void SetActive(GameObject active)
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (gameplayPanel) gameplayPanel.SetActive(false);
        if (rewardPanel) rewardPanel.SetActive(false);
        if (active) active.SetActive(true);
    }

    // Buttons
    public void OnStartButton() => BasicFlowManager.Instance?.StartGame();
    public void OnFinishButton() => BasicFlowManager.Instance?.EndGameplayToReward();
    public void OnPlayAgainButton() => BasicFlowManager.Instance?.PlayAgain();
    public void OnBackToMainButton() => BasicFlowManager.Instance?.BackToMain();
}
