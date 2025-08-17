using UnityEngine;

public sealed class BasicFlowManager : MonoBehaviour
{
    public static BasicFlowManager Instance { get; private set; }

    public enum State { MainMenu, Gameplay, Reward }
    public State Current { get; private set; }

    [SerializeField] private UIFlow ui;
    [SerializeField] private GameTimer timer;
    [SerializeField] private ScoreManager scoreManager;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        SetState(State.MainMenu);
    }

    public void StartGame()
    {
        SetState(State.Gameplay);
        scoreManager?.ResetScore();
        timer?.StartTimer();
    }

    public void EndGameplayToReward()
    {
        timer?.StopTimer();
        SetState(State.Reward);

        int finalScore = scoreManager != null ? scoreManager.CurrentScore : 0;
        ui?.ShowReward(finalScore);
    }

    public void PlayAgain() => StartGame();
    public void BackToMain() => SetState(State.MainMenu);

    private void SetState(State s)
    {
        Current = s;
        switch (s)
        {
            case State.MainMenu: ui?.ShowMainMenu(); break;
            case State.Gameplay: ui?.ShowGameplay(); break;
            case State.Reward: ui?.ShowReward(); break;
        }
    }
}
