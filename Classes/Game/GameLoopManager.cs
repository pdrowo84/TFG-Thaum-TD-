using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
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

    [Header("UI")]
    [Tooltip("Texto que mostrará la oleada actual (ej. 3/20)")]
    public TextMeshProUGUI WaveText;

    [Header("Enemies")]
    [Tooltip("Velocidad angular en grados/seg que usan los enemigos para rotar hacia su dirección de movimiento")]
    public float EnemyRotationSpeed = 720f;

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

        // Inicializar texto de oleadas
        UpdateWaveText();

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

            if (EnemyIDsToSummon.Count > 0)
            {
                for (int i = 0; i < EnemyIDsToSummon.Count; i++)
                {
                    EntitySummoner.SummonEnemy(EnemyIDsToSummon.Dequeue());
                }
            }

            //Spawn Towers

            //Move Enemies

            NativeArray<Vector3> NodesToUse = new NativeArray<Vector3>(NodePositions, Allocator.TempJob);
            NativeArray<int> NodeIndicies = new NativeArray<int>(EntitySummoner.EnemiesInGame.Count, Allocator.TempJob);
            NativeArray<float> EnemySpeeds = new NativeArray<float>(EntitySummoner.EnemiesInGame.Count, Allocator.TempJob);

            // Limpia los nulls de la lista antes de usarla
            EntitySummoner.EnemiesIsGameTransform.RemoveAll(t => t == null);
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
                DeltaTime = Time.deltaTime,
                RotationSpeed = EnemyRotationSpeed
            };

            JobHandle MoveJobHandle = MoveJob.Schedule(EnemyAccess);
            MoveJobHandle.Complete();

            for (int i = 0; i < EntitySummoner.EnemiesInGame.Count; i++)
            {
                EntitySummoner.EnemiesInGame[i].NodeIndex = NodeIndicies[i];

                if (EntitySummoner.EnemiesInGame[i].NodeIndex >= NodePositions.Length)
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

            foreach (TowerBehaviour tower in TowersInGame)
            {
                tower.Target = TowerTargeting.GetTarget(tower, tower.TargetingMode);
                tower.Tick();
            }

            //Apply Effects
            if (EffectsQueue.Count > 0)
            {
                for (int i = 0; i < EffectsQueue.Count; i++)
                {
                    ApplyEffectData CurrentDamageData = EffectsQueue.Dequeue();

                    // Evitar aplicar efectos a enemigos ya muertos o nulos (por pooling)
                    if (CurrentDamageData.EnemyToAffect == null || CurrentDamageData.EnemyToAffect.IsDead)
                    {
                        Debug.Log($"GameLoopManager: Saltando efecto '{CurrentDamageData.EffectToApply.EffectName}' para enemigo nulo/muerto.");
                        continue;
                    }

                    // Si el efecto afecta la velocidad y el enemigo es inmune a ralentizaciones, ignorar.
                    if (CurrentDamageData.EffectToApply != null && CurrentDamageData.EffectToApply.SpeedMultiplier != 1f)
                    {
                        if (CurrentDamageData.EnemyToAffect.IsSlowImmune)
                        {
                            Debug.Log($"GameLoopManager: {CurrentDamageData.EnemyToAffect.name} es inmune a ralentizaciones. Ignorando efecto '{CurrentDamageData.EffectToApply.EffectName}'.");
                            continue;
                        }
                    }

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
            foreach (Enemy CurrentEnemy in EntitySummoner.EnemiesInGame)
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

                    // Ignorar entradas para enemigos nulos o ya muertos (por pooling)
                    if (CurrentDamageData.TargetedEnemy == null || CurrentDamageData.TargetedEnemy.IsDead)
                    {
                        Debug.Log("GameLoopManager: Saltando daño en cola para enemigo nulo/muerto.");
                        continue;
                    }

                    float multiplier = 1f;
                    if (CurrentDamageData.DamageElement != ElementType.None)
                        multiplier = CurrentDamageData.TargetedEnemy.GetElementalMultiplier(CurrentDamageData.DamageElement);

                    // Aplica el daño con el multiplicador elemental y la penetración
                    float effectiveResistance = Mathf.Max(0.1f, CurrentDamageData.Resistance * (1f - CurrentDamageData.Penetration));
                    CurrentDamageData.TargetedEnemy.Health -= (CurrentDamageData.TotalDamage * multiplier) / effectiveResistance;

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

        // Actualizar UI después de intentar arrancar la siguiente ola
        UpdateWaveText();
    }

    private IEnumerator SpawnWave(WaveData wave)
    {
        waveInProgress = true;

        // Actualizar indicador de oleada cuando la corrutina comienza (currentWave ya fue incrementado por StartNextWave)
        UpdateWaveText();

        if (wave == null || wave.EnemiesToSpawn == null || wave.EnemiesToSpawn.Length == 0)
        {
            waveInProgress = false;
            yield break;
        }

        switch (wave.Mode)
        {
            case WaveData.SpawnMode.Sequential:
                // Comportamiento original: por cada entrada spawnear su Count seguido
                foreach (var enemyInfo in wave.EnemiesToSpawn)
                {
                    for (int i = 0; i < enemyInfo.Count; i++)
                    {
                        EnqueuedEnemyIDToSummon(enemyInfo.EnemyID);
                        yield return new WaitForSeconds(wave.SpawnInterval);
                    }
                }
                break;

            case WaveData.SpawnMode.Interleaved:
                // Round-robin: spawnea 1 de cada entrada por iteración hasta agotar todas
                int entries = wave.EnemiesToSpawn.Length;
                int[] remaining = new int[entries];
                for (int e = 0; e < entries; e++) remaining[e] = Mathf.Max(0, wave.EnemiesToSpawn[e].Count);

                bool anyLeft = true;
                while (anyLeft)
                {
                    anyLeft = false;
                    for (int e = 0; e < entries; e++)
                    {
                        if (remaining[e] > 0)
                        {
                            EnqueuedEnemyIDToSummon(wave.EnemiesToSpawn[e].EnemyID);
                            remaining[e]--;
                            yield return new WaitForSeconds(wave.SpawnInterval);
                            anyLeft = true;
                        }
                    }
                }
                break;

            case WaveData.SpawnMode.Randomized:
                // Barajar la lista completa y spawnear en orden aleatorio
                var pool = new System.Collections.Generic.List<int>();
                foreach (var enemyInfo in wave.EnemiesToSpawn)
                {
                    for (int i = 0; i < enemyInfo.Count; i++)
                        pool.Add(enemyInfo.EnemyID);
                }

                // Fisher-Yates shuffle
                for (int i = pool.Count - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    int tmp = pool[i];
                    pool[i] = pool[j];
                    pool[j] = tmp;
                }

                for (int i = 0; i < pool.Count; i++)
                {
                    EnqueuedEnemyIDToSummon(pool[i]);
                    yield return new WaitForSeconds(wave.SpawnInterval);
                }
                break;

            default:
                // Fallback seguro al comportamiento secuencial
                foreach (var enemyInfo in wave.EnemiesToSpawn)
                {
                    for (int i = 0; i < enemyInfo.Count; i++)
                    {
                        EnqueuedEnemyIDToSummon(enemyInfo.EnemyID);
                        yield return new WaitForSeconds(wave.SpawnInterval);
                    }
                }
                break;
        }

        waveInProgress = false;

        // Actualizar indicador al terminar la oleada
        UpdateWaveText();
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
        // Detener cualquier loop activo en instancias existentes
        foreach (var manager in Object.FindObjectsOfType<GameLoopManager>())
        {
            manager.LoopShouldEnd = true;
            manager.StopAllCoroutines();
        }

        // Limpiar colas estáticas para evitar aplicar daño/efectos pendientes tras el reload
        if (EffectsQueue != null) EffectsQueue.Clear();
        if (DamageData != null) DamageData.Clear();
        if (EnemyIDsToSummon != null) EnemyIDsToSummon.Clear();
        if (EnemiesToRemove != null) EnemiesToRemove.Clear();

        // Limpiar y destruir objetos pool antiguos para evitar estados persistentes (e.g. enemigos dañados)
        EntitySummoner.ForceReinit(destroyPooledObjects: true);

        // Limpia listas públicas/estáticas referenciadas por el GameLoop
        if (EntitySummoner.EnemiesInGame != null) EntitySummoner.EnemiesInGame.Clear();
        if (EntitySummoner.EnemiesIsGameTransform != null) EntitySummoner.EnemiesIsGameTransform.Clear();
        if (TowersInGame != null) TowersInGame.Clear();

        // Destruye todas las torres activas en la escena actual por si quedan instancias huérfanas
        foreach (var tower in Object.FindObjectsOfType<TowerBehaviour>())
        {
            if (tower != null)
                Object.Destroy(tower.gameObject);
        }

        // Resetear el estado del héroe
        TowerPlacing.ResetHeroPlacement();

        // Asegura escala de tiempo normalizada
        Time.timeScale = 1f;

        // Recarga la escena (estado ya limpiado)
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

    // Actualiza el TextMeshProUGUI que muestra la oleada actual
    private void UpdateWaveText()
    {
        if (WaveText == null || Waves == null)
            return;

        int total = Waves.Count;

        // Cuando no ha empezado ninguna ola currentWave == 0 y waveInProgress == false -> mostrar 0/total
        // Cuando está en progreso o después de iniciar una ola, currentWave contiene el número de la ola en curso (1-based)
        int displayWave = Mathf.Clamp(currentWave, 0, total);

        // Si no hay oleadas configuradas, mostrar 0/0 por seguridad
        WaveText.text = $"Oleada {displayWave}/{total}";
    }


}


public class Effect
{
    // Añadido SpeedMultiplier para soporte de ralentizaciones (1 = sin cambio, 0.8 = 20% más lento)
    public Effect(string effectName, float damageRate, float damage, float expireTime, ElementType damageElement, float speedMultiplier = 1f)
    {

        ExpireTime = expireTime;
        EffectName = effectName;
        DamageRate = damageRate;
        Damage = damage;
        DamageElement = damageElement;
        SpeedMultiplier = speedMultiplier;

    }

    public string EffectName;
    public ElementType DamageElement;

    public float Damage;
    public float DamageRate;
    public float DamageDelay;

    public float ExpireTime;

    public float SpeedMultiplier = 1f;
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
    public EnemyDamageData(Enemy target, float damage, float resistance, ElementType damageElement, float penetration = 0f)
    {
        DamageElement = damageElement;
        TargetedEnemy = target;
        TotalDamage = damage;
        Resistance = resistance;
        Penetration = Mathf.Clamp01(penetration);
    }

    public ElementType DamageElement;
    public Enemy TargetedEnemy;
    public float TotalDamage;
    public float Resistance;
    public float Penetration; // 0..1 porcentaje de resistencia ignorada
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

    // Nueva: velocidad angular (grados/seg) para rotación suave hacia la dirección de movimiento
    public float RotationSpeed;

    public void Execute(int index, TransformAccess transform)
    {
        if (NodeIndex[index] >= NodePositions.Length)
            return;

        Vector3 PositionToMove = NodePositions[NodeIndex[index]];
        Vector3 currentPos = transform.position;

        // Dirección hacia el objetivo
        Vector3 dir = PositionToMove - currentPos;
        float distToTarget = dir.magnitude;

        // Mover hacia el objetivo respetando la velocidad
        Vector3 newPos = Vector3.MoveTowards(currentPos, PositionToMove, EnemySpeeds[index] * DeltaTime);
        transform.position = newPos;

        // Rotar suavemente hacia la dirección de movimiento si hay separación
        Vector3 moveDir = newPos - currentPos;
        if (moveDir.sqrMagnitude > 0.000001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized);
            // Rota hasta RotationSpeed * DeltaTime grados en este frame
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, RotationSpeed * DeltaTime);
        }

        // Si hemos alcanzado la posición objetivo, avanzamos al siguiente nodo
        if (newPos == PositionToMove)
        {
            NodeIndex[index]++;
        }

    }
}