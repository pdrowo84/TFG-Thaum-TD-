using System.Collections.Generic;
using UnityEngine;
using static ElementDamageType;

public class LaserDamage : MonoBehaviour, IDamageMethod
{
    [SerializeField] private Transform LaserPivot;
    [SerializeField] private LineRenderer LaserRenderer;

    // Configurables para el rebote
    [Tooltip("Multiplicador de daño aplicado al enemigo rebotado")]
    [Range(0f, 1f)]
    [SerializeField] private float BounceDamageMultiplier = 0.6f;

    [Tooltip("Distancia máxima entre objetivo y siguiente para considerar el rebote (unidades del juego)")]
    [SerializeField] private float MaxBounceDistance = 2f;

    private float Damage;
    private float FireRate;
    private float Delay;

    public void Init(float Damage, float FireRate)
    {
        this.Damage = Damage;
        this.FireRate = FireRate;
        Delay = 1f / FireRate;
    }

    public void DamageTick(Enemy Target)
    {
        if (Target)
        {
            LaserRenderer.enabled = true;

            Vector3 pivotPos = LaserPivot.position;
            Vector3 targetPos = Target.RootPart != null ? Target.RootPart.position : Target.transform.position;

            var tower = GetComponent<TowerBehaviour>();
            ElementType damageType = tower != null ? tower.DamageElement : ElementType.None;
            float towerDamage = tower != null ? tower.Damage : Damage;
            float penetration = tower != null ? tower.ArmorPenetration : 0f;
            int bounceCount = tower != null ? tower.LaserBounceCount : 0;

            // Construir cadena de rebotes secuencial hacia atrás (cada salto toma el "inmediato por detrás")
            List<Enemy> chain = new List<Enemy>();
            if (bounceCount > 0 && EntitySummoner.EnemiesInGame != null)
            {
                Enemy current = Target;
                for (int i = 0; i < bounceCount; i++)
                {
                    Enemy next = null;
                    int bestIndex = int.MinValue;
                    float bestDist = float.MaxValue;

                    Vector3 currentPos = current.RootPart != null ? current.RootPart.position : current.transform.position;

                    for (int j = 0; j < EntitySummoner.EnemiesInGame.Count; j++)
                    {
                        var e = EntitySummoner.EnemiesInGame[j];
                        if (e == null || e.IsDead) continue;
                        if (e == current) continue;
                        if (chain.Contains(e)) continue;

                        // Solo candidatos que estén "por detrás" o en la misma posición de nodo
                        if (e.NodeIndex > current.NodeIndex) continue;

                        Vector3 ePos = e.RootPart != null ? e.RootPart.position : e.transform.position;
                        float distToCurrent = Vector3.Distance(ePos, currentPos);

                        // Preferir el mayor NodeIndex posible (más cercano por detrás). Si hay empate, elegir el más cercano físicamente.
                        if (e.NodeIndex > bestIndex)
                        {
                            bestIndex = e.NodeIndex;
                            bestDist = distToCurrent;
                            next = e;
                        }
                        else if (e.NodeIndex == bestIndex && distToCurrent < bestDist)
                        {
                            bestDist = distToCurrent;
                            next = e;
                        }
                    }

                    if (next == null) break;

                    // Verificación de distancia con el punto anterior para asegurar "justo detrás"
                    Vector3 nextPos = next.RootPart != null ? next.RootPart.position : next.transform.position;
                    Vector3 prevPos = current.RootPart != null ? current.RootPart.position : current.transform.position;
                    if (Vector3.Distance(nextPos, prevPos) > MaxBounceDistance)
                    {
                        break; // demasiado lejos, cortar cadena
                    }

                    chain.Add(next);
                    current = next;
                }
            }

            // Actualizar visual (LineRenderer) siempre, independientemente del retraso de disparo,
            // para evitar parpadeos: la parte visual es estable entre ticks de daño.
            if (chain.Count == 0)
            {
                LaserRenderer.positionCount = 2;
                LaserRenderer.SetPosition(0, pivotPos);
                LaserRenderer.SetPosition(1, targetPos);
            }
            else
            {
                LaserRenderer.positionCount = 2 + chain.Count;
                LaserRenderer.SetPosition(0, pivotPos);
                LaserRenderer.SetPosition(1, targetPos);
                for (int i = 0; i < chain.Count; i++)
                {
                    Vector3 pos = chain[i].RootPart != null ? chain[i].RootPart.position : chain[i].transform.position;
                    LaserRenderer.SetPosition(2 + i, pos);
                }
            }

            // Solo encolar daño cuando el temporizador llegue a 0 (lógica de disparo independiente de la visual)
            if (Delay > 0f)
            {
                Delay -= Time.deltaTime;
                return;
            }

            // Encolar daño para el objetivo principal (incluye penetración)
            GameLoopManager.EnqueueDamageData(new EnemyDamageData(Target, towerDamage, Target.DamageResistance, damageType, penetration));

            // Encolar daño reducido para cada rebote (si los hay)
            if (chain.Count > 0)
            {
                float bounceDamage = towerDamage * BounceDamageMultiplier;
                for (int i = 0; i < chain.Count; i++)
                {
                    GameLoopManager.EnqueueDamageData(new EnemyDamageData(chain[i], bounceDamage, chain[i].DamageResistance, damageType, penetration));
                }
            }

            Delay = 1f / FireRate;
            return;
        }

        // Sin objetivo: apagar láser y restablecer renderer a 2 posiciones por si acaso
        LaserRenderer.enabled = false;
        if (LaserRenderer != null)
            LaserRenderer.positionCount = 2;
    }
}