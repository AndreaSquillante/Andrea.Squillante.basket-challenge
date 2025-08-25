using UnityEngine;

public sealed class BallOwner : MonoBehaviour
{
    public enum Team { Player, AI }
    [SerializeField] private Team team = Team.Player;
    public Team TeamId => team;
}
