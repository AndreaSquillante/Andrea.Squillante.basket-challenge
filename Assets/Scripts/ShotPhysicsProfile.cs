using UnityEngine;

[CreateAssetMenu(menuName = "Basketball/Shot Physics Profile")]
public sealed class ShotPhysicsProfile : ScriptableObject
{
    [Header("Force Mapping")]
    [Range(0f, 15f)] public float impulsePerCm = 6.5f;         // forza di base per cm
    [Range(0f, 0.1f)] public float impulsePerCmPerSec = 0.03f; // più veloce lo swipe, più spinta
    [Range(0f, 60f)] public float maxImpulse = 40f;            // limita la forza totale

    [Header("Direction Weights")]
    [Range(0f, 1f)] public float horizontalInfluence = 0.40f;  // non troppo sensibile lateralmente
    [Range(0f, 2f)] public float verticalInfluence = 1.3f;     // parabola accentuata
    [Range(0f, 2f)] public float forwardBias = 0.65f;          // direzione frontale prevalente

    [Header("Flight Tuning")]
    [Range(0f, 5f)] public float gravityMultiplier = 2.8f;     // caduta “pesante”, simile a Basketball Stars
    [Range(0f, 2f)] public float airDragWhileFlying = 0.35f;   // rallenta leggermente l’arco

    [Header("Spin at Launch")]
    public bool applySpin = true;
    [Range(0f, 1f)] public float backspinPerImpulse = 0.25f;   // backspin medio (effetto realistico)
    [Range(0f, 1f)] public float sidespinPerImpulse = 0.12f;   // sidespin più leggero
    [Range(0f, 100f)] public float maxAngularVelocity = 45f;   // limita la velocità di spin
}
