using UnityEngine;

[CreateAssetMenu(fileName = "AIDifficultyProfile", menuName = "Basket/A.I. Difficulty", order = 0)]
public sealed class AIDifficultyProfile : ScriptableObject
{
    [Header("Tempo di tiro")]
    [Min(0.1f)] public float minShotInterval = 1.2f;
    [Min(0.1f)] public float maxShotInterval = 2.4f;
    public float initialDelay = 0.75f;

    [Header("Probabilità di esito (pesi relativi)")]
    [Min(0f)] public float weightPerfect = 0.55f;
    [Min(0f)] public float weightMake = 0.30f;
    [Min(0f)] public float weightBackboard = 0.10f;
    [Min(0f)] public float weightMiss = 0.05f;

    [Header("Rumore / errore")]
    [Range(0f, 0.25f)] public float powerJitterPct = 0.04f; // +/- % of bar
    [Range(0f, 20f)] public float lateralNoiseDeg = 6f;     // yaw noise in degrees

    [Header("Comportamento")]
    public bool snapInsidePerfect = true;  // if outcome=Perfect, force center of band
    public bool adaptToBackboardBonus = true; // optional: boost backboard weight when bonus active

    [Header("Bonus backboard (boost moltiplicativo)")]
    public float backboardWeightBoost = 2.0f; // applied if bonus active and adaptToBackboardBonus=true
}
