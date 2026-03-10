using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using static ElementDamageType;



public class FireTriggerManager : MonoBehaviour
{
    [SerializeField] private FlameThrowerDamage BaseClass;


    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[FireTrigger] Algo ha entrado: {other.name}, tag: {other.tag}");

        if (other.transform.root.CompareTag("Enemy"))
        {
            Debug.Log($"[FireTrigger] {other.name} entra en el área de fuego.");

            var tower = GetComponentInParent<TowerBehaviour>();
            if (tower == null)
            {
                Debug.LogError("FireTriggerManager: No se encontró TowerBehaviour en este objeto ni en sus padres.");
                return;
            }
            ElementType damageType = tower.DamageElement;

            Effect FlameEffect = new Effect("Fire", BaseClass.FireRate, BaseClass.Damage, 5f, damageType);
            ApplyEffectData EffectData = new ApplyEffectData(EntitySummoner.EnemyTransformPairs[other.transform.parent], FlameEffect);
            GameLoopManager.EnqueueEffectToApply(EffectData);
        }
    }
}
