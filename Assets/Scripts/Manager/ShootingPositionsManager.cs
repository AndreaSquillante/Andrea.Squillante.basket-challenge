using UnityEngine;

public sealed class ShootingPositionsManager : MonoBehaviour
{
    [Header("Positions Setup")]
    [SerializeField] private Transform[] shootingPositions;
    [SerializeField] private bool randomOrder = true;

    private int _currentIndex = -1;

    /// <summary>
    /// Restituisce la prossima posizione di tiro.
    /// </summary>
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
            int newIndex;
            do
            {
                newIndex = Random.Range(0, shootingPositions.Length);
            }
            while (newIndex == _currentIndex); // Evita ripetizione immediata

            _currentIndex = newIndex;
        }
        else
        {
            _currentIndex = (_currentIndex + 1) % shootingPositions.Length;
        }

        return shootingPositions[_currentIndex];
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
