using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntitySummoner : MonoBehaviour
{
    public static List<Enemy> EnemiesInGame;
    public static List<Transform> EnemiesIsGameTransform;

    public static Dictionary<Transform, Enemy> EnemyTransformPairs;
    public static Dictionary<int, GameObject> EnemyPrefabs;
    public static Dictionary<int, Queue<Enemy>> EnemyObjectPools;

    private static bool IsInitialized;

    // Asegura que los campos estáticos se reinicien al cargar la escena/play mode.
    // Evita que valores estáticos persistentes (por ejemplo con Domain Reload desactivado)
    // impidan la recarga correcta de prefabs/colas.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        IsInitialized = false;
        EnemyTransformPairs = null;
        EnemyPrefabs = null;
        EnemyObjectPools = null;
        EnemiesInGame = null;
        EnemiesIsGameTransform = null;
    }

    public static void Init()
    {
        // Si ya está marcado como inicializado y hay prefabs cargados, no hacer nada.
        if (IsInitialized && EnemyPrefabs != null && EnemyPrefabs.Count > 0)
            return;

        // (Re)crea todas las estructuras - esto cubre tanto la primera inicialización
        // como casos donde la inicialización anterior quedó en estado inconsistente.
        EnemyTransformPairs = new Dictionary<Transform, Enemy>();
        EnemyPrefabs = new Dictionary<int, GameObject>();
        EnemyObjectPools = new Dictionary<int, Queue<Enemy>>();
        EnemiesInGame = new List<Enemy>();
        EnemiesIsGameTransform = new List<Transform>();

        EnemySummonData[] Enemies = Resources.LoadAll<EnemySummonData>("Enemies");

        foreach (EnemySummonData Enemy in Enemies)
        {
            Debug.Log($"Cargando EnemyID: {Enemy.EnemyID}, Prefab: {Enemy.EnemyPrefab}");
            if (!EnemyPrefabs.ContainsKey(Enemy.EnemyID))
                EnemyPrefabs.Add(Enemy.EnemyID, Enemy.EnemyPrefab);

            if (!EnemyObjectPools.ContainsKey(Enemy.EnemyID))
                EnemyObjectPools.Add(Enemy.EnemyID, new Queue<Enemy>());
        }

        IsInitialized = true;
    }

    // Forzar reinicialización (opcional) y opcionalmente destruir objetos en pool
    public static void ForceReinit(bool destroyPooledObjects = true)
    {
        if (EnemyObjectPools != null && destroyPooledObjects)
        {
            foreach (var pool in EnemyObjectPools.Values)
            {
                while (pool.Count > 0)
                {
                    Enemy e = pool.Dequeue();
                    if (e != null)
                        Object.Destroy(e.gameObject);
                }
            }
        }

        IsInitialized = false;
        Init();
    }

    public static Enemy SummonEnemy(int EnemyID)
    {
        if (EnemyPrefabs == null || !EnemyPrefabs.ContainsKey(EnemyID))
        {
            Debug.LogError($"ENTITYSUMMONER: ENEMY WITH ID OF {EnemyID} DOES NOT EXIST OR PREFABS NO ESTÁN CARGADOS!");
            return null;
        }

        Enemy SummonedEnemy = null;

        if (EnemyPrefabs[EnemyID] == null)
        {
            Debug.LogError($"El prefab para EnemyID {EnemyID} es NULL. Revisa el asset EnemySummonData correspondiente.");
        }

        Queue<Enemy> ReferencedQueue = EnemyObjectPools[EnemyID];

        if (ReferencedQueue != null && ReferencedQueue.Count > 0)
        {
            // Dequeue enemy and initialize 
            SummonedEnemy = ReferencedQueue.Dequeue();
            if (SummonedEnemy != null)
            {
                SummonedEnemy.Init();
                SummonedEnemy.gameObject.SetActive(true);
            }
            else
            {
                // fallback: instantiate if pooled entry estaba null
                GameObject NewEnemy = Instantiate(EnemyPrefabs[EnemyID], GameLoopManager.NodePositions[0], Quaternion.identity);
                SummonedEnemy = NewEnemy.GetComponent<Enemy>();
                SummonedEnemy.Init();
            }
        }
        else
        {
            // Instantiate new instance of enemy and initialize
            GameObject NewEnemy = Instantiate(EnemyPrefabs[EnemyID], GameLoopManager.NodePositions[0], Quaternion.identity);
            SummonedEnemy = NewEnemy.GetComponent<Enemy>();
            SummonedEnemy.Init();
        }

        if (SummonedEnemy != null)
        {
            if (!EnemiesInGame.Contains(SummonedEnemy)) EnemiesInGame.Add(SummonedEnemy);
            if (!EnemiesIsGameTransform.Contains(SummonedEnemy.transform)) EnemiesIsGameTransform.Add(SummonedEnemy.transform);
            if (!EnemyTransformPairs.ContainsKey(SummonedEnemy.transform)) EnemyTransformPairs.Add(SummonedEnemy.transform, SummonedEnemy);

            SummonedEnemy.ID = EnemyID;
        }

        return SummonedEnemy;
    }

    public static void RemoveEnemy(Enemy EnemyToRemove)
    {
        if (EnemyToRemove == null) return;

        // Limpiar efectos y restaurar velocidad antes de enpoolar
        try
        {
            if (EnemyToRemove.ActiveEffects != null)
                EnemyToRemove.ActiveEffects.Clear();
        }
        catch { /* ignore safety */ }

        // Restaurar velocidad a la base original por si quedó alterada
        try
        {
            EnemyToRemove.Speed = EnemyToRemove.BaseSpeed;
        }
        catch { /* ignore safety */ }

        if (EnemyObjectPools != null && EnemyObjectPools.ContainsKey(EnemyToRemove.ID))
        {
            EnemyObjectPools[EnemyToRemove.ID].Enqueue(EnemyToRemove);
        }
        else
        {
            // Si no hay pool disponible, destruye el objeto para evitar referencias zombis.
            Object.Destroy(EnemyToRemove.gameObject);
            return;
        }

        EnemyToRemove.gameObject.SetActive(false);

        if (EnemyTransformPairs != null)
            EnemyTransformPairs.Remove(EnemyToRemove.transform);

        if (EnemiesIsGameTransform != null)
            EnemiesIsGameTransform.Remove(EnemyToRemove.transform);

        if (EnemiesInGame != null)
            EnemiesInGame.Remove(EnemyToRemove);
    }

}