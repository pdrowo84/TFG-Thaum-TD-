using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class EntitySummoner : MonoBehaviour
{
    public static List<Enemy> EnemiesInGame;
    public static Dictionary<int, GameObject> EnemyPrefabs;
    public static Dictionary<int, Queue<Enemy>> EnemyObjectPools;

    void Start()
    {
        EnemyPrefabs = new Dictionary<int, GameObject>();
        EnemyObjectPools = new Dictionary<int, Queue<Enemy>>();
        EnemiesInGame = new List<Enemy>();

        EnemySummonData[] Enemies = Resources.LoadAll<EnemySummonData>("Enemies");
        Debug.Log(Enemies[0].name);
    }
}
