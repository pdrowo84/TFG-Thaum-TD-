using UnityEngine;
using System.Collections;   
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Jobs;
using Unity.Jobs;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static ElementDamageType;

public class GameLoopManager : MonoBehaviour 
{
    public List<WaveData> Waves;
    private int currentWave = 0;
    private bool waveInProgress = false;

    public static List<TowerBehaviour> TowersInGame;
    public static Vector3[] NodePositions;
    public static float[] NodeDistances;

    private static Queue<ApplyEffectData> EffectsQueue;
    private static Queue<EnemyDamageData> DamageData;
    private static Queue<Enemy> EnemiesToRemove;
    private static Queue<int> EnemyIDsToSummon;

    private PlayerStats PlayerStatistics;

    public Transform NodeParent;
    public bool LoopShouldEnd;

    public UnityEngine.UI.Toggle speedToggle;

    private void Start()
    {
        EntitySummoner.Init();

        Debug.Log(Time.timeScale);

        currentWave = 0;
        waveInProgress = false;

        PlayerStatistics = FindObjectOfType<PlayerStats>();
        EffectsQueue = new Queue<ApplyEffectData>();
        DamageData = new Queue<EnemyDamageData>();
        TowersInGame = new List<TowerBehaviour>();
        EnemyIDsToSummon = new Queue<int>();
        EnemiesToRemove = new Queue<Enemy>();

        NodePositions = new Vector3[NodeParent.childCount];

        if (speedToggle != null) speedToggle.onValueChanged.AddListener(ChangeSpeed);

        for (int i = 0; i < NodePositions.Length; i++)
        {
            NodePositions[i] = NodeParent.GetChild(i).position;
        }

        NodeDistances = new float[NodePositions.Length - 1];

        for (int i = 0; i < NodeDistances.Length; i++)
        {
            NodeDistances[i] = Vector3.Distance(NodePositions[i], NodePositions[i + 1]);
        }

        
        StartCoroutine(GameLoop());
      
    }
    private void Update()
    {
        // Si no hay oleada en curso y no quedan enemigos vivos, lanza la siguiente oleada
        if (!waveInProgress && EntitySummoner.EnemiesInGame != null && EntitySummoner.EnemiesInGame.Count == 0)
        {
            StartNextWave();
        }
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
                    if (PlayerStatistics != null)
                    {
                        int lifeDamage = EntitySummoner.EnemiesInGame[i].LifeDamage;
                        PlayerStatistics.LoseLife(lifeDamage);
                    }
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
            if (EffectsQueue.Count > 0)
            {
                for (int i = 0; i < EffectsQueue.Count; i++)
                {
                    ApplyEffectData CurrentDamageData = EffectsQueue.Dequeue();
                    Effect EffectDuplicate = CurrentDamageData.EnemyToAffect.ActiveEffects.Find(x => x.EffectName == CurrentDamageData.EffectToApply.EffectName);

                    if (EffectDuplicate == null)
                    {
                        CurrentDamageData.EnemyToAffect.ActiveEffects.Add(CurrentDamageData.EffectToApply);
                        Debug.Log($"[Efecto] Añadido efecto '{CurrentDamageData.EffectToApply.EffectName}' a {CurrentDamageData.EnemyToAffect.name}");
                    }
                
                    else
                    {
                        EffectDuplicate.ExpireTime = CurrentDamageData.EffectToApply.ExpireTime;
                        Debug.Log($"[Efecto] Refrescada duración de '{EffectDuplicate.EffectName}' en {CurrentDamageData.EnemyToAffect.name}");
                    }
                
                }
            }

            //Tick Enemies
            foreach(Enemy CurrentEnemy in EntitySummoner.EnemiesInGame)
            {
                if (CurrentEnemy != null)
                    CurrentEnemy.Tick();
            }

            //Damage Enemies
            if (DamageData.Count > 0)
            {
                for (int i = 0; i < DamageData.Count; i++)
                {
                    EnemyDamageData CurrentDamageData = DamageData.Dequeue();
                    
                    float multiplier = 1f;
                    if (CurrentDamageData.DamageElement != ElementType.None)
                        multiplier = CurrentDamageData.TargetedEnemy.GetElementalMultiplier(CurrentDamageData.DamageElement);

                    // Aplica el daño con el multiplicador elemental
                    CurrentDamageData.TargetedEnemy.Health -= (CurrentDamageData.TotalDamage * multiplier) / CurrentDamageData.Resistance;

                    if (CurrentDamageData.TargetedEnemy.Health <= 0f && !CurrentDamageData.TargetedEnemy.IsDead)
                    {
                        CurrentDamageData.TargetedEnemy.IsDead = true;
                        PlayerStatistics.AddMoney(CurrentDamageData.TargetedEnemy.MoneyReward);
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
    public void StartNextWave()
    {
        if (currentWave < Waves.Count)
        {
            StartCoroutine(SpawnWave(Waves[currentWave]));
            currentWave++;
        }
        else
        {
            Debug.Log("¡Todas las oleadas completadas!");
        }
    }

    private IEnumerator SpawnWave(WaveData wave)
    {
        waveInProgress = true;
        foreach (var enemyInfo in wave.EnemiesToSpawn)
        {
            for (int i = 0; i < enemyInfo.Count; i++)
            {
                EnqueuedEnemyIDToSummon(enemyInfo.EnemyID);
                yield return new WaitForSeconds(wave.SpawnInterval);
            }
        }
        waveInProgress = false;
    }
    public static void EnqueueEffectToApply(ApplyEffectData effectData)
    {
        EffectsQueue.Enqueue(effectData);
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
        if (!EnemiesToRemove.Contains(EnemyToRemove))
            EnemiesToRemove.Enqueue(EnemyToRemove);
    }

    public static void PauseGame()
    {
        Time.timeScale = 0f;
    }
    public static void ResumeGame()
    {
        Time.timeScale = 1f;
    }  
    
    public static void ResetGame()
    {
        // Limpia pools y listas para evitar referencias a objetos destruidos
        if (EntitySummoner.EnemiesInGame != null) EntitySummoner.EnemiesInGame.Clear();
        if (EntitySummoner.EnemiesIsGameTransform != null) EntitySummoner.EnemiesIsGameTransform.Clear();
        if (TowersInGame != null) TowersInGame.Clear(); 
        if (EntitySummoner.EnemyObjectPools != null)
        {
            foreach (var pool in EntitySummoner.EnemyObjectPools.Values)
                pool.Clear();
        }
        // Destruye todas las torres activas en la escena
        foreach (var tower in FindObjectsOfType<TowerBehaviour>())
        {
            Destroy(tower.gameObject);
        }
        Time.timeScale = 1f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    public void ChangeSpeed(bool isFast)
    {
        Time.timeScale = isFast ? 2f : 1f;
    }

    public void GoToMainMenu()
    {
        ResetGame();
        SceneManager.LoadScene(0);
    }
}


public class Effect
{
    public Effect(string effectName, float damageRate, float damage, float expireTime, ElementType damageElement)
    {
        
        ExpireTime = expireTime;
        EffectName = effectName;
        DamageRate = damageRate;
        Damage = damage;
        DamageElement = damageElement;
        
    }

    public string EffectName;
    public ElementType DamageElement;

    public float Damage;
    public float DamageRate;
    public float DamageDelay;

    public float ExpireTime;
}

public struct ApplyEffectData
{
    public ApplyEffectData(Enemy enemyToAffect, Effect effectToApply)
    {
        EnemyToAffect = enemyToAffect;
        EffectToApply = effectToApply;
    }

    public Enemy EnemyToAffect;
    public Effect EffectToApply;
}
public struct EnemyDamageData
{
    public EnemyDamageData(Enemy target, float damage, float resistance, ElementType damageElement)
    {
        DamageElement = damageElement;
        TargetedEnemy = target;
        TotalDamage = damage;
        Resistance = resistance;
    }

    public ElementType DamageElement;
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
