using UnityEngine;
using TMPro;

public sealed class GameTimer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float matchDuration = 120f; // 2 min default

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;

    private float _timeRemaining;
    private bool _running;

    public void StartTimer()
    {
        _timeRemaining = matchDuration;
        _running = true;
    }

    public void StopTimer()
    {
        _running = false;
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
