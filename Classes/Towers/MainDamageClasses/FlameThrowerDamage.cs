using UnityEngine;

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
        FireTrigger.enabled  = Target != null;

        if (Target)
        {
            if (!FireEffect.isPlaying) FireEffect.Play();
            Target.Health -= Damage * Time.deltaTime;
            return;
        }

        FireEffect.Stop();

    }
}
