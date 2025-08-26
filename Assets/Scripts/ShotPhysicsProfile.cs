using UnityEngine;

[CreateAssetMenu(fileName = "ShotPhysicsProfile", menuName = "Basket/ShotPhysicsProfile")]
public sealed class ShotPhysicsProfile : ScriptableObject
{
    [Header("Force Mapping")]
    [Range(0f, 15f)] public float impulsePerCm = 6.5f;
    [Range(0f, 0.1f)] public float impulsePerCmPerSec = 0.03f;
    [Range(0f, 120f)] public float maxImpulse = 90f;

    [Header("Direction Weights")]
    [Range(0f, 1f)] public float horizontalInfluence = 0.40f;
    [Range(0f, 2f)] public float verticalInfluence = 1.3f;
    [Range(0f, 2f)] public float forwardBias = 0.65f;

    [Header("Flight Tuning")]
    [Range(0f, 5f)] public float gravityMultiplier = 3.2f;
    [Range(0f, 2f)] public float airDragWhileFlying = 0.08f;

    [Header("Spin at Launch")]
    public bool applySpin = true;
    [Range(0f, 1f)] public float backspinPerImpulse = 0.25f;
    [Range(0f, 1f)] public float sidespinPerImpulse = 0.12f;
    [Range(0f, 100f)] public float maxAngularVelocity = 45f;

    [Header("Scene Scale")]
    [Tooltip("How many real meters per 1 Unity unit. If your hoop is at Y=30 instead of 3.05, set ~10.")]
    [Range(0.1f, 20f)] public float sceneScale = 1f;
}
