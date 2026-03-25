using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgradePath", menuName = "TowerDefense/UpgradePath")]
public class TowerUpgradePath : ScriptableObject
{
    [Header("Rama A (Alcance)")]
    public TowerUpgrade[] BranchA = new TowerUpgrade[2];

    [Header("Rama B (Velocidad de ataque)")]
    public TowerUpgrade[] BranchB = new TowerUpgrade[2];
}