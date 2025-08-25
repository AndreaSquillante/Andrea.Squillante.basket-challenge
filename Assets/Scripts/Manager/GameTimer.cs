using UnityEngine;
using TMPro;

public sealed class GameTimer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float matchDuration = 120f; // 2 min 

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;

    private float _timeRemaining;
    private bool _running;

    public bool IsRunning => _running;
    public float RemainingSeconds => _timeRemaining;

    public void SetMatchDuration(float seconds)
    {
        matchDuration = Mathf.Max(1f, seconds);
    }

    public void StartTimer()
    {
        _timeRemaining = matchDuration;
        _running = true;
        UpdateUI(); // <--- mostra subito 02:00
    }

    public void StopTimer()
    {
        _running = false;
        _timeRemaining = 0f; UpdateUI();
    }

    private void Update()
    {
        if (!_running) return;

        _timeRemaining -= Time.deltaTime;
        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            _running = false;

            // Notifica fine partita
            BasicFlowManager.Instance?.EndGameplayToReward();
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (!timerText) return;

        int minutes = Mathf.FloorToInt(_timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(_timeRemaining % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
