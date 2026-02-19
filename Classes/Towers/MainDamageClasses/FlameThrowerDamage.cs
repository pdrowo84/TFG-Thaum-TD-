using UnityEngine;
using static ElementDamageType;

public class FlameThrowerDamage : MonoBehaviour, IDamageMethod
{
    [SerializeField] private Collider FireTrigger;
    [SerializeField] private ParticleSystem FireEffect;

    [HideInInspector] public float Damage;
    [HideInInspector] public float FireRate;

    public void Init(float Damage, float FireRate)
    {
        this.Damage = Damage;
        this.FireRate = FireRate;
    }

    public void DamageTick(Enemy Target)
    {
        FireTrigger.enabled = Target != null;

        if (Target)
        {
            if (!FireEffect.isPlaying) FireEffect.Play();

            var tower = GetComponent<TowerBehaviour>();
            if (tower == null) return;
            ElementType damageType = tower.DamageElement;

            // Encola el daño para que GameLoopManager lo procese con resistencias/inmunidades
            GameLoopManager.EnqueueDamageData(
                new EnemyDamageData(Target, Damage * Time.deltaTime, Target.DamageResistance, damageType)
            );
            return;
        }

        FireEffect.Stop();
    }
}