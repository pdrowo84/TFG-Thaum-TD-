using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using static ElementDamageType;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Type")]
    public ElementDamageType.ElementType EnemyElementType = ElementDamageType.ElementType.None;

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
    public bool IsDead = false;

    // Guardamos la velocidad base para recomponerla cuando los efectos expiren
    public float BaseSpeed;

    // Nueva: inmunidad a ralentizaciones
    [HideInInspector]
    public bool IsSlowImmune = false;

    // Guardar la velocidad original del prefab al despertar (solo se ejecuta una vez por instancia)
    private void Awake()
    {
        // En Awake tomamos la velocidad configurada en el prefab como BaseSpeed
        BaseSpeed = Speed;
    }

    public void Init()
    {
        ActiveEffects = new List<Effect>();

        IsDead = false;
        Health = MaxHealth;
        transform.position = GameLoopManager.NodePositions[0];
        NodeIndex = 0;

        BaseSpeed = BaseSpeed <= 0f ? Speed : BaseSpeed;

        // Restaurar velocidad/estado base cuando se (re)inicializa desde pool
        Speed = BaseSpeed;

        // Asegurar que no arrastre inmunidades por defecto
        IsSlowImmune = false;
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

        for (int i = 0; i < ActiveEffects.Count; i++)
        {
            if (ActiveEffects[i].ExpireTime > 0f)
            {
                if (ActiveEffects[i].DamageDelay > 0f)
                {
                    ActiveEffects[i].DamageDelay -= Time.deltaTime;
                }
                else
                {
                    GameLoopManager.EnqueueDamageData(new EnemyDamageData(this, ActiveEffects[i].Damage, 1f, ActiveEffects[i].DamageElement, 0f));
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

        // Recalcular velocidad actual como BaseSpeed * producto de SpeedMultiplier de efectos activos
        float speedMul = 1f;
        if (ActiveEffects != null && ActiveEffects.Count > 0)
        {
            foreach (var eff in ActiveEffects)
            {
                // sólo efectos con SpeedMultiplier != 1 influyen
                if (eff.SpeedMultiplier != 1f)
                    speedMul *= eff.SpeedMultiplier;
            }
        }

        Speed = BaseSpeed * speedMul;
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