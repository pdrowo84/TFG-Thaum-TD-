using UnityEngine;

[CreateAssetMenu(fileName = "NewWaveData", menuName = "TowerDefense/WaveData")]
public class WaveData : ScriptableObject
{
    public EnemyWaveEntry[] EnemiesToSpawn;
    public float SpawnInterval = 1f; // Tiempo entre spawns

    // Modo de spawn de la ola: secuencial (por entradas), intercalado (round-robin) o aleatorio (baraja todos)
    public SpawnMode Mode = SpawnMode.Sequential;

    public enum SpawnMode
    {
        Sequential = 0,   // comportamiento actual: spawnea todos de la primera entrada, luego los de la segunda...
        Interleaved = 1,  // round-robin: 1 de la entrada A, 1 de la B, 1 de la C, volver a A...
        Randomized = 2    // crea una lista con todos los enemigos y la baraja antes de spawnear
    }
}

[System.Serializable]
public struct EnemyWaveEntry
{
    public int EnemyID;   // El ID que usas en tu sistema de invocación
    public int Count;     // Cuántos de este tipo
}