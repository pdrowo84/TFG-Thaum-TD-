using UnityEngine;

[CreateAssetMenu(fileName = "NewTowerUpgrade", menuName = "TowerDefense/Upgrade")]
public class TowerUpgrade : ScriptableObject
{
    public string UpgradeName;
    [TextArea] public string Description;
    public Sprite Icon;

    public int Cost = 100;

    // Multiplicadores y bonos (1 = sin cambio)
    public float DamageMultiplier = 1f;
    public float FireRateMultiplier = 1f;
    public float RangeBonus = 0f;

    // Nueva: bonus de penetración de armadura que aporta la mejora (0 = ninguno, 1 = +100%)
    [Tooltip("Porcentaje adicional de penetración de armadura que aporta la mejora (0-1). Ej: 0.2 = +20%")]
    [Range(0f, 1f)]
    public float ArmorPenetrationBonus = 0f;

    // Nueva: número de rebotes que aporta la mejora (para la rama B)
    [Tooltip("Número adicional de rebotes que aporta la mejora (0 = ninguno)")]
    [Range(0, 5)]
    public int BounceCountBonus = 0;

    // Nueva: efectos para lanzamisiles (rama A y B)
    [Tooltip("Cantidad de ralentización aplicada por la mejora (0-1). Ej: 0.2 = -20% velocidad")]
    [Range(0f, 1f)]
    public float SlowAmountBonus = 0f;

    [Tooltip("Duración en segundos de la ralentización aplicada por la mejora")]
    public float SlowDurationBonus = 0f;

    [Tooltip("Aumento del radio de explosión que aporta la mejora (unidades del mundo, suma al radio base)")]
    public float ExplosionRadiusBonus = 0f;

    // Nueva: aumento de duración de la quemadura (rama B de la lanzallamas)
    [Tooltip("Aumento en segundos de la duración del efecto de quemadura que aporta la mejora")]
    public float BurnDurationBonus = 0f;

    // Estado resultante que representará la mejora aplicada en la torre
    public TowerBehaviour.UpgradeState ResultingState;
}