using UnityEngine;
using UnityEngine.WSA;

public sealed class BasicFlowManager : MonoBehaviour
{
    public static BasicFlowManager Instance { get; private set; }

    public enum State { MainMenu, Gameplay, Reward }
    public State Current { get; private set; }

    public static event System.Action<State> OnStateChanged;

    [SerializeField] private UIFlow ui;

    [Header("Game Refs")]
    [SerializeField] private GameTimer timer;
    [SerializeField] private ScoreManager playerScore;
    [SerializeField] private ScoreManager aiScore;          
    [SerializeField] private BallLauncher playerLauncher;
    [SerializeField] private BallLauncher aiLauncher;       
    [SerializeField] private BackboardBonus backboardBonus;
    [SerializeField] private ShootingPositionsManager positionsManagerPlayer;
    [SerializeField] private ShootingPositionsManager positionsManagerAI;
    [SerializeField] private FireballController playerFireball;
    [SerializeField] private FireballController aiFireball;
    private bool _hasSetOnce;

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
        positionsManagerPlayer?.ResetCycle();
        positionsManagerAI?.ResetCycle();
        var pStart = positionsManagerPlayer.GetCurrentPosition() ?? positionsManagerPlayer.GetNextPosition();
        playerLauncher.SetShotOrigin(pStart);
        playerLauncher.ForceStopAndHold();
        var aStart = positionsManagerAI.GetCurrentPosition() ?? positionsManagerAI.GetNextPosition();
        aiLauncher.SetShotOrigin(aStart);
        aiLauncher.ForceStopAndHold();
        playerLauncher?.GetComponent<BallSurfaceResponse>()?.HardResetForNewMatch();
        aiLauncher?.GetComponent<BallSurfaceResponse>()?.HardResetForNewMatch();
        playerFireball?.ResetAll();
        aiFireball?.ResetAll();
        timer?.StopTimer();
        timer?.SetMatchDuration(120f);
        Time.timeScale = 1f;

        playerScore?.ResetScore();
        aiScore?.ResetScore();

        playerLauncher?.ForceStopAndHold();
        aiLauncher?.ForceStopAndHold();

        if (backboardBonus != null)
        {
            backboardBonus.ResetBonus();
            backboardBonus.TrySpawnBonus();
        }

        timer?.StartTimer();
    }

    public void EndGameplayToReward()
    {
        timer?.StopTimer();

        int player = playerScore ? playerScore.CurrentScore : 0;
        int ai = aiScore ? aiScore.CurrentScore : 0;

        SetState(State.Reward);

        ui?.ShowReward(player, ai);
    }

    public void PlayAgain() => StartGame();

    public void BackToMain()
    {
        timer?.StopTimer();
        Time.timeScale = 1f;
        SetState(State.MainMenu);
    }

    private void SetState(State s)
    {
        if (_hasSetOnce && Current == s) return;

        Current = s;

        switch (s)
        {
            case State.MainMenu: ui?.ShowMainMenu(); break;
            case State.Gameplay: ui?.ShowGameplay(); break;
            case State.Reward: ui?.ShowReward(); break;
        }

        OnStateChanged?.Invoke(s);
        _hasSetOnce = true;
    }
}
