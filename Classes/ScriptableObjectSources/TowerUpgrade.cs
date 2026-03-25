
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

    // Estado resultante que representará la mejora aplicada en la torre
    public TowerBehaviour.UpgradeState ResultingState;
}