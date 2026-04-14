using System.Collections.Generic;
using UnityEngine;
using static ElementDamageType;

/// <summary>
/// Área temporal de rastro usada por Luneth.
/// - Aplica un efecto de velocidad a enemigos que entren en el trigger (una vez por enemigo).
/// - Se asegura de tener Rigidbody(isKinematic) para que OnTriggerEnter funcione aunque los enemigos no tengan Rigidbody.
/// - Si enemyLayer == 0 acepta todas las layers (útil en pruebas).
/// - Nuevo: permite ignorar a un creador/propietario (Owner) para que no se aplique el buff al que generó el trail.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class TrailArea : MonoBehaviour
{
    [Tooltip("Duración en segundos del área")]
    public float duration = 2.0f;

    [Tooltip("Multiplicador de velocidad que se aplicará a los enemigos (ej. 1.2 = +20%)")]
    public float speedMultiplier = 1.2f;

    [Tooltip("Nombre del efecto (para evitar duplicados y refrescar)")]
    public string effectName = "LunethSpeed";

    [Tooltip("Layer mask para filtrar colliders (poner la Layer de tus enemigos). Si es 0 se aceptan todas las layers.")]
    public LayerMask enemyLayer;

    [Tooltip("Transform del creador/propietario del trail. Si se asigna, los enemigos cuyo root sea igual a Owner serán ignorados.")]
    public Transform Owner;

    private SphereCollider col;
    // Evita volver a aplicar el efecto al mismo Enemy varias veces desde este área
    private HashSet<Enemy> appliedEnemies = new HashSet<Enemy>();

    void Awake()
    {
        col = GetComponent<SphereCollider>();
        if (col == null) col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;

        // Aseguramos que exista un Rigidbody kinematic para que OnTriggerEnter se invoque correctamente.
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnEnable()
    {
        // Auto-destruir tras duración
        CancelInvoke(nameof(DestroySelf));
        Invoke(nameof(DestroySelf), Mathf.Max(0.01f, duration));
    }

    void OnDisable()
    {
        CancelInvoke();
        appliedEnemies.Clear();
    }

    private void DestroySelf()
    {
        // Limpia referencias y destruye
        appliedEnemies.Clear();
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Si enemyLayer está a 0 -> aceptar todas las layers
        if (enemyLayer.value != 0 && (enemyLayer.value & (1 << other.gameObject.layer)) == 0)
            return;

        Enemy enemy = ResolveEnemyFromCollider(other);
        if (enemy == null || enemy.IsDead) return;

        // Si Owner está asignado y el enemigo pertenece al mismo root que Owner, ignorar.
        if (Owner != null)
        {
            Transform enemyRoot = enemy.transform.root;
            if (enemyRoot == Owner) return;
        }

        // Si ya aplicamos a este enemigo en esta área, evitamos reenqueuar
        if (appliedEnemies.Contains(enemy)) return;

        appliedEnemies.Add(enemy);

        // Crear efecto: duration igual a la vida del área para simplicidad
        float expireTime = Mathf.Max(0.01f, duration);
        Effect speedEffect = new Effect(effectName, 0f, 0f, expireTime, ElementType.Ninguno, speedMultiplier);

        GameLoopManager.EnqueueEffectToApply(new ApplyEffectData(enemy, speedEffect));

#if UNITY_EDITOR
        Debug.Log($"TrailArea: aplicando efecto '{effectName}' a {enemy.name}");
#endif
    }

    // Intentar resolver Enemy de forma robusta usando el mapeo de EntitySummoner (más fiable)
    private Enemy ResolveEnemyFromCollider(Collider c)
    {
        if (c == null) return null;

        // Preferir lookup por EntitySummoner si existe
        if (EntitySummoner.EnemyTransformPairs != null)
        {
            Transform root = c.transform.root;
            if (root != null && EntitySummoner.EnemyTransformPairs.TryGetValue(root, out var e))
                return e;
        }

        // Fallback: buscar componente en padres
        return c.GetComponentInParent<Enemy>();
    }
}