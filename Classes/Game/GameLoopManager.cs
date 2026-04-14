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
    private bool victoryShown = false;

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
    private static float DesiredTimeScale = 1f;
    private static bool IsPaused = false;

    [Header("UI")]
    [Tooltip("Texto que mostrar� la oleada actual (ej. 3/20)")]
    public TextMeshProUGUI WaveText;
    [Tooltip("Panel de victoria (se activa al completar todas las oleadas)")]
    public GameObject VictoryPanel;
    [Tooltip("Panel principal de la UI de juego (se ocultar� en victoria)")]
    public GameObject GameplayUIPanel;

    [Header("Enemies")]
    [Tooltip("Velocidad angular en grados/seg que usan los enemigos para rotar hacia su direcci�n de movimiento")]
    public float EnemyRotationSpeed = 720f;

    private void Start()
    {
        EntitySummoner.Init();

        Debug.Log(Time.timeScale);

        currentWave = 0;
        waveInProgress = false;
        victoryShown = false;

        PlayerStatistics = FindObjectOfType<PlayerStats>();
        EffectsQueue = new Queue<ApplyEffectData>();
        DamageData = new Queue<EnemyDamageData>();
        TowersInGame = new List<TowerBehaviour>();
        EnemyIDsToSummon = new Queue<int>();
        EnemiesToRemove = new Queue<Enemy>();

        NodePositions = new Vector3[NodeParent.childCount];

        if (speedToggle != null) speedToggle.onValueChanged.AddListener(ChangeSpeed);
        // Respetar el estado del toggle al iniciar la escena
        ChangeSpeed(speedToggle != null && speedToggle.isOn);

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
        // Si ya se mostr� la victoria, no arrancar m�s oleadas
        if (victoryShown) return;

        // Si no hay oleada en curso y no quedan enemigos vivos, lanza la siguiente oleada
        if (!waveInProgress && EntitySummoner.EnemiesInGame != null && EntitySummoner.EnemiesInGame.Count == 0)
        {
            StartNextWave();
        }
    }

    IEnumerator GameLoop()
    {

        if (EntitySummoner.EnemiesInGame == null)
            Debug.LogError("EnemiesInGame no est� inicializado");
        if (EntitySummoner.EnemiesIsGameTransform == null)
            Debug.LogError("EnemiesIsGameTransform no est� inicializado");

        while (LoopShouldEnd == false)
        {

            //Spawn Enemies

            if (EnemyIDsToSummon.Count > 0)
            {
                while (EnemyIDsToSummon.Count > 0)
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
                while (EffectsQueue.Count > 0)
                {
                    ApplyEffectData CurrentDamageData = EffectsQueue.Dequeue();

                    // Evitar aplicar efectos a enemigos ya muertos o nulos (por pooling)
                    if (CurrentDamageData.EnemyToAffect == null || CurrentDamageData.EnemyToAffect.IsDead)
                    {
                        Debug.Log($"GameLoopManager: Saltando efecto '{CurrentDamageData.EffectToApply.EffectName}' para enemigo nulo/muerto.");
                        continue;
                    }

                    // Inmune a ralentizaciones: ignorar solo multiplicadores < 1 (los buffs de velocidad usan > 1).
                    if (CurrentDamageData.EffectToApply != null && CurrentDamageData.EnemyToAffect.IsSlowImmune
                        && CurrentDamageData.EffectToApply.SpeedMultiplier < 1f)
                    {
                        Debug.Log($"GameLoopManager: {CurrentDamageData.EnemyToAffect.name} es inmune a ralentizaciones. Ignorando efecto '{CurrentDamageData.EffectToApply.EffectName}'.");
                        continue;
                    }

                    Effect EffectDuplicate = CurrentDamageData.EnemyToAffect.ActiveEffects.Find(x => x.EffectName == CurrentDamageData.EffectToApply.EffectName);

                    if (EffectDuplicate == null)
                    {
                        CurrentDamageData.EnemyToAffect.ActiveEffects.Add(CurrentDamageData.EffectToApply);
                        Debug.Log($"[Efecto] A�adido efecto '{CurrentDamageData.EffectToApply.EffectName}' a {CurrentDamageData.EnemyToAffect.name}");
                    }

                    else
                    {
                        EffectDuplicate.ExpireTime = CurrentDamageData.EffectToApply.ExpireTime;
                        Debug.Log($"[Efecto] Refrescada duraci�n de '{EffectDuplicate.EffectName}' en {CurrentDamageData.EnemyToAffect.name}");
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
                while (DamageData.Count > 0)
                {
                    EnemyDamageData CurrentDamageData = DamageData.Dequeue();

                    // Ignorar entradas para enemigos nulos o ya muertos (por pooling)
                    if (CurrentDamageData.TargetedEnemy == null || CurrentDamageData.TargetedEnemy.IsDead)
                    {
                        Debug.Log("GameLoopManager: Saltando da�o en cola para enemigo nulo/muerto.");
                        continue;
                    }

                    float multiplier = 1f;
                    if (CurrentDamageData.DamageElement != ElementType.Ninguno)
                        multiplier = CurrentDamageData.TargetedEnemy.GetElementalMultiplier(CurrentDamageData.DamageElement);

                    // Aplica el da�o con el multiplicador elemental y la penetraci�n
                    float effectiveResistance = Mathf.Max(0.1f, CurrentDamageData.Resistance * (1f - CurrentDamageData.Penetration));
                    CurrentDamageData.TargetedEnemy.Health -= (CurrentDamageData.TotalDamage * multiplier) / effectiveResistance;

                    if (CurrentDamageData.TargetedEnemy.Health <= 0f && !CurrentDamageData.TargetedEnemy.IsDead)
                    {
                        CurrentDamageData.TargetedEnemy.IsDead = true;
                        if (PlayerStatistics != null)
                            PlayerStatistics.AddMoney(CurrentDamageData.TargetedEnemy.MoneyReward);
                        EnqueuedEnemyToRemove(CurrentDamageData.TargetedEnemy);
                    }
                }
            }

            //Remove Enemies

            if (EnemiesToRemove.Count > 0)
            {
                while (EnemiesToRemove.Count > 0)
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
            Debug.Log("�Todas las oleadas completadas!");
            ShowVictory();
        }

        // Actualizar UI despu�s de intentar arrancar la siguiente ola
        UpdateWaveText();
    }

    private void ShowVictory()
    {
        if (victoryShown) return;
        victoryShown = true;

        // Pausar el juego y mostrar panel de victoria
        Time.timeScale = 0f;

        if (VictoryPanel != null)
            VictoryPanel.SetActive(true);

        if (GameplayUIPanel != null)
            GameplayUIPanel.SetActive(false);
    }

    // Bot�n UI: jugar de nuevo (reinicia escena/estado)
    public void VictoryPlayAgain()
    {
        // ResetGame normaliza timeScale y recarga escena
        ResetGame();
    }

    // Bot�n UI: salir del juego
    public void VictoryQuit()
    {
        Debug.Log("Saliendo del juego (victoria)");
        Application.Quit();
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
                // Round-robin: spawnea 1 de cada entrada por iteraci�n hasta agotar todas
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
        IsPaused = true;
        Time.timeScale = 0f;
    }
    public static void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = DesiredTimeScale;
    }

    public static void ResetGame()
    {
        // Detener loops activos antes de recargar
        foreach (var manager in Object.FindObjectsOfType<GameLoopManager>())
        {
            manager.LoopShouldEnd = true;
            manager.StopAllCoroutines();
        }

        // Limpiar colas estáticas pendientes
        if (EffectsQueue != null) EffectsQueue.Clear();
        if (DamageData != null) DamageData.Clear();
        if (EnemyIDsToSummon != null) EnemyIDsToSummon.Clear();
        if (EnemiesToRemove != null) EnemiesToRemove.Clear();

        // Limpiar y destruir objetos de pool para no arrastrar referencias inválidas
        EntitySummoner.ForceReinit(destroyPooledObjects: true);

        if (EntitySummoner.EnemiesInGame != null) EntitySummoner.EnemiesInGame.Clear();
        if (EntitySummoner.EnemiesIsGameTransform != null) EntitySummoner.EnemiesIsGameTransform.Clear();
        if (TowersInGame != null) TowersInGame.Clear();

        foreach (var tower in Object.FindObjectsOfType<TowerBehaviour>())
        {
            if (tower != null)
                Object.Destroy(tower.gameObject);
        }

        // Resetear estado del héroe entre runs
        TowerPlacing.ResetHeroPlacement();

        IsPaused = false;
        DesiredTimeScale = 1f;
        Time.timeScale = 1f;

        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    public void ChangeSpeed(bool isFast)
    {
        DesiredTimeScale = isFast ? 2f : 1f;

        // Si el juego est� pausado (o en un panel que pausa), no tocar el timescale actual.
        // Al reanudar, ResumeGame aplicar� DesiredTimeScale.
        if (!IsPaused)
            Time.timeScale = DesiredTimeScale;
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
        // Cuando est� en progreso o despu�s de iniciar una ola, currentWave contiene el n�mero de la ola en curso (1-based)
        int displayWave = Mathf.Clamp(currentWave, 0, total);

        // Si no hay oleadas configuradas, mostrar 0/0 por seguridad
        WaveText.text = $"Oleada {displayWave}/{total}";
    }


}


public class Effect
{
    // A�adido SpeedMultiplier para soporte de ralentizaciones (1 = sin cambio, 0.8 = 20% m�s lento)
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

    // Nueva: velocidad angular (grados/seg) para rotaci�n suave hacia la direcci�n de movimiento
    public float RotationSpeed;

    public void Execute(int index, TransformAccess transform)
    {
        if (NodeIndex[index] >= NodePositions.Length)
            return;

        Vector3 PositionToMove = NodePositions[NodeIndex[index]];
        Vector3 currentPos = transform.position;

        // Direcci�n hacia el objetivo
        Vector3 dir = PositionToMove - currentPos;
        float distToTarget = dir.magnitude;

        // Mover hacia el objetivo respetando la velocidad
        Vector3 newPos = Vector3.MoveTowards(currentPos, PositionToMove, EnemySpeeds[index] * DeltaTime);
        transform.position = newPos;

        // Rotar suavemente hacia la direcci�n de movimiento si hay separaci�n
        Vector3 moveDir = newPos - currentPos;
        if (moveDir.sqrMagnitude > 0.000001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized);
            // Rota hasta RotationSpeed * DeltaTime grados en este frame
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, RotationSpeed * DeltaTime);
        }

        // Si hemos alcanzado la posici�n objetivo, avanzamos al siguiente nodo
        if (newPos == PositionToMove)
        {
            NodeIndex[index]++;
        }

    }
}