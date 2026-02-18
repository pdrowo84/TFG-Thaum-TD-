using UnityEngine;

[CreateAssetMenu(fileName = "NewWaveData", menuName = "TowerDefense/WaveData")]
public class WaveData : ScriptableObject
{
    public EnemyWaveEntry[] EnemiesToSpawn;
    public float SpawnInterval = 1f; // Tiempo entre spawns

}

[System.Serializable]
public struct EnemyWaveEntry
{
    public int EnemyID;   // El ID que usas en tu sistema de invocación
    public int Count;     // Cuántos de este tipo
}