
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using static ElementDamageType;

public class MissileCollisionManager : MonoBehaviour
{
    [SerializeField] private MissileDamage BaseClass;
    [SerializeField] private ParticleSystem ExplosionSystem;
    [SerializeField] private ParticleSystem MissileSystem;
    [SerializeField] private float ExplosionRadius = 1f;

    private List<ParticleCollisionEvent> MissileCollisions;
    private void Start()
    {
        MissileCollisions = new List<ParticleCollisionEvent>();
    }

    private void OnParticleCollision(GameObject other)
    {
        MissileSystem.GetCollisionEvents(other, MissileCollisions);


        for (int collisionevent = 0; collisionevent < MissileCollisions.Count; collisionevent++) 
        {
            ExplosionSystem.transform.position = MissileCollisions[collisionevent].intersection;
            ExplosionSystem.Play();

            Collider[] EnemiesInRadius = Physics.OverlapSphere(MissileCollisions[collisionevent].intersection, ExplosionRadius, BaseClass.EnemiesLayer);

            for(int i = 0; i < EnemiesInRadius.Length; i++)
            {
                ElementType damageType = GetComponentInParent<TowerBehaviour>().DamageElement;


                Enemy EnemyToDamage = EntitySummoner.EnemyTransformPairs[EnemiesInRadius[i].transform.parent];
                if (!EnemyToDamage.IsDead)
                {
                    EnemyDamageData DamageToApply = new EnemyDamageData(EnemyToDamage, BaseClass.Damage, EnemyToDamage.DamageResistance, damageType);
                    GameLoopManager.EnqueueDamageData(DamageToApply);
                }
            }

        }

    }
}
