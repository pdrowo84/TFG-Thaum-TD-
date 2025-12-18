using UnityEngine;
using System.Collections;   
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Jobs;
using Unity.Jobs;
using UnityEngine.UIElements;

public class GameLoopManager : MonoBehaviour 
{
    public static List<TowerBehaviour> TowersInGame;
    public static Vector3[] NodePositions;
    public static float[] NodeDistances;

    private static Queue<EnemyDamageData> DamageData;
    private static Queue<Enemy> EnemiesToRemove;
    private static Queue<int> EnemyIDsToSummon;

    public Transform NodeParent;
    public bool LoopShouldEnd;

    private void Start()
    {
        DamageData = new Queue<EnemyDamageData>();
        TowersInGame = new List<TowerBehaviour>();
        EnemyIDsToSummon = new Queue<int>();
        EnemiesToRemove = new Queue<Enemy>();
        EntitySummoner.Init();

        NodePositions = new Vector3[NodeParent.childCount];

        for(int i = 0; i < NodePositions.Length; i++)
        {
            NodePositions[i] = NodeParent.GetChild(i).position;
        }

        NodeDistances = new float[NodePositions.Length - 1];

        for (int i = 0; i < NodeDistances.Length; i++)
        {
            NodeDistances[i] = Vector3.Distance(NodePositions[i], NodePositions[i + 1]);
        }

        StartCoroutine(GameLoop());
        InvokeRepeating("SummonTest", 0f, 1f);
        
    }

    

    void SummonTest()
    {
        EnqueuedEnemyIDToSummon(1);
    }

    IEnumerator GameLoop()
    {

        if (EntitySummoner.EnemiesInGame == null)
            Debug.LogError("EnemiesInGame no está inicializado");
        if (EntitySummoner.EnemiesIsGameTransform == null)
            Debug.LogError("EnemiesIsGameTransform no está inicializado");

        while (LoopShouldEnd == false)
        {
          
            //Spawn Enemies

            if(EnemyIDsToSummon.Count > 0)
            {
                for(int i = 0;i < EnemyIDsToSummon.Count;i++)
                {
                    EntitySummoner.SummonEnemy(EnemyIDsToSummon.Dequeue());
                }
            }

            //Spawn Towers

            //Move Enemies

            NativeArray<Vector3> NodesToUse = new NativeArray<Vector3>(NodePositions, Allocator.TempJob);
            NativeArray<int> NodeIndicies = new NativeArray<int>(EntitySummoner.EnemiesInGame.Count, Allocator.TempJob);
            NativeArray<float> EnemySpeeds = new NativeArray<float>(EntitySummoner.EnemiesInGame.Count, Allocator.TempJob);
            TransformAccessArray EnemyAccess = new TransformAccessArray(EntitySummoner.EnemiesIsGameTransform.ToArray(), 2);

            for (int i = 0; i < EntitySummoner.EnemiesInGame.Count; i++) 
            {
                EnemySpeeds[i] = EntitySummoner.EnemiesInGame[i].Speed;
                NodeIndicies[i] = EntitySummoner.EnemiesInGame[i].NodeIndex;
            }

            MoveEnemyJob MoveJob = new MoveEnemyJob
            {
                NodePositions = NodesToUse,
                NodeIndex = NodeIndicies,
                EnemySpeeds = EnemySpeeds,
                DeltaTime = Time.deltaTime
            };

            JobHandle MoveJobHandle = MoveJob.Schedule(EnemyAccess);
            MoveJobHandle.Complete();

            for(int i = 0; i < EntitySummoner.EnemiesInGame.Count; i++)
            {
                EntitySummoner.EnemiesInGame[i].NodeIndex = NodeIndicies[i];

                if(EntitySummoner.EnemiesInGame[i].NodeIndex >= NodePositions.Length)
                {
                    EnqueuedEnemyToRemove(EntitySummoner.EnemiesInGame[i]);
                }
            }
            
            NodesToUse.Dispose();
            EnemySpeeds.Dispose();
            NodeIndicies.Dispose();
            EnemyAccess.Dispose();

            //Tick Towers

            foreach(TowerBehaviour tower in TowersInGame)
            {
                tower.Target =  TowerTargeting.GetTarget(tower, TowerTargeting.TargetType.First);
                tower.Tick();
            }

            //Apply Effects

            //Damage Enemies
            if (DamageData.Count > 0)
            {
                for (int i = 0; i < DamageData.Count; i++)
                {
                   EnemyDamageData CurrentDamageData = DamageData.Dequeue();
                    CurrentDamageData.TargetedEnemy.Health -= CurrentDamageData.TotalDamage / CurrentDamageData.Resistance;
                
                    if(CurrentDamageData.TargetedEnemy.Health <= 0f)
                    {
                        EnqueuedEnemyToRemove(CurrentDamageData.TargetedEnemy);
                    }

                }
            } 
            //Remove Enemies

            if (EnemiesToRemove.Count > 0)
            {
                for (int i = 0; i < EnemiesToRemove.Count; i++)
                {
                    EntitySummoner.RemoveEnemy(EnemiesToRemove.Dequeue());
                }
            }

            //Remove Towers

            yield return null;  

        }
    }


    public static void EnqueueDamageData(EnemyDamageData damageData)
    {
        DamageData.Enqueue(damageData);
    }
    public static void EnqueuedEnemyIDToSummon(int ID)
    {
        EnemyIDsToSummon.Enqueue(ID);
    }

    public static void EnqueuedEnemyToRemove(Enemy EnemyToRemove)
    {
        EnemiesToRemove.Enqueue(EnemyToRemove);
    }
}


public struct EnemyDamageData
{
    public EnemyDamageData(Enemy target, float damage, float resistance)
    {
        TargetedEnemy = target;
        TotalDamage = damage;
        Resistance = resistance;
    }

    public Enemy TargetedEnemy;
    public float TotalDamage;
    public float Resistance;
}
public struct MoveEnemyJob : IJobParallelForTransform
{
    [NativeDisableParallelForRestriction]
    public NativeArray<Vector3> NodePositions;

    [NativeDisableParallelForRestriction]
    public NativeArray<int> NodeIndex;

    [NativeDisableParallelForRestriction]
    public NativeArray<float> EnemySpeeds;

    public float DeltaTime;
    public void Execute(int index, TransformAccess transform)
    {
        if (NodeIndex[index] >= NodePositions.Length)
            return;

        Vector3 PositionToMove = NodePositions[NodeIndex[index]];
        transform.position = Vector3.MoveTowards(transform.position, PositionToMove, EnemySpeeds[index] * DeltaTime);
         
        if(transform.position == PositionToMove)
        {
            NodeIndex[index] ++;
        }

    }
}
