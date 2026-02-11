using System.Collections.Generic;
using System.Collections;
using UnityEngine;


public class EntitySummoner : MonoBehaviour
{
    public static List<Enemy> EnemiesInGame;
    public static List<Transform> EnemiesIsGameTransform;

    public static Dictionary<Transform, Enemy> EnemyTransformPairs;
    public static Dictionary<int, GameObject> EnemyPrefabs;
    public static Dictionary<int, Queue<Enemy>> EnemyObjectPools;

    private static bool IsInitialized;

    public static void Init()
    {
        if (!IsInitialized) 
        {
            EnemyTransformPairs = new Dictionary<Transform, Enemy>();
            EnemyPrefabs = new Dictionary<int, GameObject>();
            EnemyObjectPools = new Dictionary<int, Queue<Enemy>>();
            EnemiesInGame = new List<Enemy>();
            EnemiesIsGameTransform = new List<Transform>();

            EnemySummonData[] Enemies = Resources.LoadAll<EnemySummonData>("Enemies");

            foreach (EnemySummonData Enemy in Enemies)
            {
                EnemyPrefabs.Add(Enemy.EnemyID, Enemy.EnemyPrefab);
                EnemyObjectPools.Add(Enemy.EnemyID, new Queue<Enemy>());
            }

            IsInitialized = true;
        }

        else
        {
            Debug.Log("ENTITY SUMMONER:  THIS CLASS IS ALREADY INITIALIZED");
        }

    }
    public static Enemy SummonEnemy (int EnemyID)
    {
        Enemy SummonedEnemy = null;

        if (EnemyPrefabs.ContainsKey(EnemyID))
        {
            Queue<Enemy> ReferencedQueue = EnemyObjectPools[EnemyID];

            if(ReferencedQueue.Count > 0)
            {
                //Dequeue enemy and initialize 
                SummonedEnemy = ReferencedQueue.Dequeue();
                SummonedEnemy.Init();

                SummonedEnemy.gameObject.SetActive(true);
            }
            else
            {
                //Intantiate new intance of enemy and initialize
                GameObject NewEnemy = Instantiate(EnemyPrefabs[EnemyID], GameLoopManager.NodePositions[0], Quaternion.identity);
                SummonedEnemy = NewEnemy.GetComponent<Enemy>();
                SummonedEnemy.Init();
            }
        }
        else
        {
            Debug.Log($"ENTITYSUMMONER: ENEMY WITH ID OF {EnemyID} DOES NOT EXIST!");
            return null;
        }

        if(!EnemiesInGame.Contains(SummonedEnemy)) EnemiesInGame.Add(SummonedEnemy);
        if(!EnemiesIsGameTransform.Contains(SummonedEnemy.transform)) EnemiesIsGameTransform.Add(SummonedEnemy.transform);
        if(!EnemyTransformPairs.ContainsKey(SummonedEnemy.transform)) EnemyTransformPairs.Add(SummonedEnemy.transform, SummonedEnemy);

        SummonedEnemy.ID = EnemyID;
        return SummonedEnemy;
    }
    
    public static void RemoveEnemy(Enemy EnemyToRemove)
    {
        EnemyObjectPools[EnemyToRemove.ID].Enqueue(EnemyToRemove);
        EnemyToRemove.gameObject.SetActive(false);
        EnemyTransformPairs.Remove(EnemyToRemove.transform);
        EnemiesIsGameTransform.Remove(EnemyToRemove.transform);
        EnemiesInGame.Remove(EnemyToRemove);
    }

}
