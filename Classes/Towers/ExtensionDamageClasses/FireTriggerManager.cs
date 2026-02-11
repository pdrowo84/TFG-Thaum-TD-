using UnityEngine;
using System.Collections.Generic;
using System.Collections;



public class FireTriggerManager : MonoBehaviour
{
    [SerializeField] private FlameThrowerDamage BaseClass;

    
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[FireTrigger] Algo ha entrado: {other.name}, tag: {other.tag}");

        // Comprobar el tag en el padre
        if (other.transform.root.CompareTag("Enemy"))
        {
            Debug.Log($"[FireTrigger] {other.name} entra en el área de fuego.");

            Effect FlameEffect = new Effect("Fire", BaseClass.FireRate, BaseClass.Damage, 5f);
            ApplyEffectData EffectData = new ApplyEffectData(EntitySummoner.EnemyTransformPairs[other.transform.parent], FlameEffect);
            GameLoopManager.EnqueueEffectToApply(EffectData);
        }
    }
}
