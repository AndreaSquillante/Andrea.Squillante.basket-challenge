using Unity.VisualScripting;
using UnityEngine;

public sealed class BasicFlowManager : MonoBehaviour
{
    public static BasicFlowManager Instance { get; private set; }

    public enum State { MainMenu, Gameplay, Reward }
    public State Current { get; private set; }

    [SerializeField] private UIFlow ui;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        SetState(State.MainMenu);
    }

    public void StartGame() => SetState(State.Gameplay);
    public void EndGameplayToReward() => SetState(State.Reward);
    public void PlayAgain() => SetState(State.Gameplay);
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
