using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using static ElementDamageType;
using static UnityEngine.EventSystems.EventTrigger;

public class MissileCollisionManager : MonoBehaviour
{
    [SerializeField] private MissileDamage BaseClass;
    [SerializeField] private ParticleSystem ExplosionSystem;
    [SerializeField] private ParticleSystem MissileSystem;
    [SerializeField] private float ExplosionRadius = 1f;

    private List<ParticleCollisionEvent> MissileCollisions;

    // Cache para escalado proporcional de las partículas
    private ParticleSystem.MainModule explosionMain;
    private ParticleSystem.ShapeModule explosionShape;
    private float baseExplosionRadius;
    private float baseStartSize = 1f;

    private void Start()
    {
        MissileCollisions = new List<ParticleCollisionEvent>();

        if (ExplosionSystem != null)
        {
            explosionMain = ExplosionSystem.main;
            explosionShape = ExplosionSystem.shape;

            // Guardar valores base para calcular el factor de escala más adelante
            baseExplosionRadius = ExplosionRadius > 0f ? ExplosionRadius : 1f;

            // Intentar leer un startSize base (si es curva se toma el valor constante)
            try
            {
                baseStartSize = explosionMain.startSize.constant;
            }
            catch
            {
                baseStartSize = 1f;
            }
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        MissileSystem.GetCollisionEvents(other, MissileCollisions);

        // Obtener parámetros desde TowerBehaviour si existe (permite upgrades)
        var tower = GetComponentInParent<TowerBehaviour>();
        float explosionRadius = ExplosionRadius;
        float slowAmount = 0f;
        float slowDuration = 0f;

        if (tower != null)
        {
            explosionRadius = tower.MissileExplosionRadius;
            slowAmount = tower.MissileSlowAmount;
            slowDuration = tower.MissileSlowDuration;
        }

        // Ajustar visualmente el sistema de partículas según el radio actual
        if (ExplosionSystem != null)
        {
            float safeBase = baseExplosionRadius <= 0f ? 1f : baseExplosionRadius;
            float scaleFactor = explosionRadius / safeBase;

            // Ajustar shape.radius (afecta la distribución de partículas)
            var shape = ExplosionSystem.shape;
            shape.radius = explosionRadius;
            // Ajustar startSize proporcionalmente
            var main = ExplosionSystem.main;
            main.startSize = new ParticleSystem.MinMaxCurve(baseStartSize * scaleFactor);

            // También escalar el transform para asegurar coincidencia visual si es necesario
            ExplosionSystem.transform.localScale = Vector3.one * Mathf.Max(0.001f, scaleFactor);
        }

        for (int collisionevent = 0; collisionevent < MissileCollisions.Count; collisionevent++)
        {
            ExplosionSystem.transform.position = MissileCollisions[collisionevent].intersection;
            ExplosionSystem.Play();

            Collider[] EnemiesInRadius = Physics.OverlapSphere(MissileCollisions[collisionevent].intersection, explosionRadius, BaseClass.EnemiesLayer);

            for (int i = 0; i < EnemiesInRadius.Length; i++)
            {
                ElementType damageType = GetComponentInParent<TowerBehaviour>().DamageElement;

                Transform enemyTransform = EnemiesInRadius[i].transform;

                Enemy EnemyToDamage = null;
                if (EntitySummoner.EnemyTransformPairs.ContainsKey(enemyTransform))
                    EnemyToDamage = EntitySummoner.EnemyTransformPairs[enemyTransform];
                else if (enemyTransform.parent != null && EntitySummoner.EnemyTransformPairs.ContainsKey(enemyTransform.parent))
                    EnemyToDamage = EntitySummoner.EnemyTransformPairs[enemyTransform.parent];

                if (EnemyToDamage != null && !EnemyToDamage.IsDead)
                {
                    EnemyDamageData DamageToApply = new EnemyDamageData(EnemyToDamage, BaseClass.Damage, EnemyToDamage.DamageResistance, damageType);
                    GameLoopManager.EnqueueDamageData(DamageToApply);

                    if (slowAmount > 0f && slowDuration > 0f)
                    {
                        float speedMultiplier = Mathf.Clamp01(1f - slowAmount);
                        Effect slowEffect = new Effect("MissileSlow", 0f, 0f, slowDuration, ElementType.Ninguno, speedMultiplier);
                        GameLoopManager.EnqueueEffectToApply(new ApplyEffectData(EnemyToDamage, slowEffect));
                    }
                }
            }
        }
    }
}