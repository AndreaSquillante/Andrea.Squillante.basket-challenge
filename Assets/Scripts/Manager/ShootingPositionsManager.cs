using UnityEngine;

public sealed class ShootingPositionsManager : MonoBehaviour
{
    [Header("Positions Setup")]
    [SerializeField] private Transform[] shootingPositions;
    [SerializeField] private bool randomOrder = true;

    [Header("Reset options")]
    [SerializeField] private bool pickOnReset = true; // pick a starting spot when resetting
    [SerializeField] private int startIndex = 0;      // used if !randomOrder

    private int _currentIndex = -1;

    public int Count => shootingPositions != null ? shootingPositions.Length : 0;
    public int CurrentIndex => _currentIndex;

    /// <summary>Returns the current shooting position (or null if none selected yet).</summary>
    public Transform GetCurrentPosition()
    {
        if (shootingPositions == null || shootingPositions.Length == 0) return null;
        if (_currentIndex < 0 || _currentIndex >= shootingPositions.Length) return null;
        return shootingPositions[_currentIndex];
    }

    /// <summary>Advance to next position and return it (random or sequential).</summary>
    public Transform GetNextPosition()
    {
        if (shootingPositions == null || shootingPositions.Length == 0)
        {
            Debug.LogError("[ShootingPositionsManager] No shooting positions assigned!");
            return null;
        }

        if (shootingPositions.Length == 1)
        {
            _currentIndex = 0;
            return shootingPositions[0];
        }

        if (randomOrder)
        {
            // pick a different random index than the current one
            int newIndex = _currentIndex;
            // small guard loop to avoid immediate repetition
            for (int i = 0; i < 8 && newIndex == _currentIndex; i++)
                newIndex = Random.Range(0, shootingPositions.Length);
            _currentIndex = newIndex;
        }
        else
        {
            if (_currentIndex < 0) _currentIndex = Mathf.Clamp(startIndex, 0, shootingPositions.Length - 1);
            else _currentIndex = (_currentIndex + 1) % shootingPositions.Length;
        }

        return shootingPositions[_currentIndex];
    }

    /// <summary>Reset the cycle. If pickOnReset = true, selects a starting spot immediately.</summary>
    public void ResetCycle()
    {
        _currentIndex = -1;

        if (shootingPositions == null || shootingPositions.Length == 0) return;

        if (pickOnReset)
        {
            if (randomOrder)
            {
                _currentIndex = Random.Range(0, shootingPositions.Length);
            }
            else
            {
                _currentIndex = Mathf.Clamp(startIndex, 0, shootingPositions.Length - 1);
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (shootingPositions == null) return;

        for (int i = 0; i < shootingPositions.Length; i++)
        {
            Transform pos = shootingPositions[i];
            if (pos != null)
            {
                Gizmos.color = (i == _currentIndex) ? Color.green : Color.yellow;
                Gizmos.DrawSphere(pos.position, 0.25f);
                Gizmos.DrawLine(pos.position, pos.position + Vector3.up * 2);
            }
        }
    }
#endif
}
