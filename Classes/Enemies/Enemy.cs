using System.Collections.Generic;
using System.Collections;
using UnityEngine;

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

    public void Init()
    {
        ActiveEffects = new List<Effect>();

        Health = MaxHealth;
        transform.position = GameLoopManager.NodePositions[0];
        NodeIndex = 0;
        Speed = 4f;
    }


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
                    GameLoopManager.EnqueueDamageData(new EnemyDamageData(this, ActiveEffects[i].Damage, 1f));
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
}
