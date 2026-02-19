using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using static ElementDamageType;

public class Enemy : MonoBehaviour
{
    public int NodeIndex;

    public List<Effect> ActiveEffects;

    public Transform RootPart;
    public float DamageResistance = 1f;
    public float MaxHealth;
    public float Health;
    public float Speed;
    public int ID;
    public int LifeDamage;
    public int MoneyReward;

    public void Init()
    {
        ActiveEffects = new List<Effect>();

        Health = MaxHealth;
        transform.position = GameLoopManager.NodePositions[0];
        NodeIndex = 0;
       
    }

    [System.Serializable]
    public class ElementResistance
    {
        public ElementType Element;
        [Range(0, 1)] public float Resistance; // 0 = sin resistencia, 1 = inmune
    }

    public List<ElementType> Immunities; // Elementos a los que es inmune
    public List<ElementResistance> Resistances; // Resistencias parciales

    public void Tick()
    {
        if (this == null) return; // Protección extra

        Debug.Log($"[Efecto] {name}: {ActiveEffects.Count} efectos activos.");

        for (int i = 0; i < ActiveEffects.Count; i++)
        {
            if (ActiveEffects[i].ExpireTime > 0f)
            {
                if (ActiveEffects[i].DamageDelay > 0f)
                {
                    ActiveEffects[i].DamageDelay -= Time.deltaTime;
                    Debug.Log($"[Efecto] {name}: Delay de '{ActiveEffects[i].EffectName}' reducido a {ActiveEffects[i].DamageDelay}");
                }
                else
                {
                    Debug.Log($"[Efecto] {name} recibe {ActiveEffects[i].Damage} de daño por '{ActiveEffects[i].EffectName}'. Vida actual: {Health}");
                    GameLoopManager.EnqueueDamageData(new EnemyDamageData(this, ActiveEffects[i].Damage, 1f, ActiveEffects[i].DamageElement));
                    ActiveEffects[i].DamageDelay = 1f / ActiveEffects[i].DamageRate;
                }

                ActiveEffects[i].ExpireTime -= Time.deltaTime;
            }
        }

        int eliminados = ActiveEffects.RemoveAll(x => x.ExpireTime <= 0f);
        if (eliminados > 0)
        {
            Debug.Log($"[Efecto] {name}: {eliminados} efecto(s) expirado(s) y eliminado(s).");
        }
    }
    public float GetElementalMultiplier(ElementType attackElement)
    {
        if (Immunities != null && Immunities.Contains(attackElement))
            return 0f;

        if (Resistances != null)
        {
            var resistance = Resistances.Find(r => r.Element == attackElement);
            if (resistance != null)
                return 1f - resistance.Resistance; // Ej: 0.5 resistencia = 0.5 daño recibido
        }

        return 1f; // Sin resistencia ni inmunidad
    }
}
