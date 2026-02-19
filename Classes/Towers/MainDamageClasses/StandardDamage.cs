using UnityEngine;
using static ElementDamageType;


public interface IDamageMethod
{
    public void DamageTick(Enemy Target);
    public void Init(float Damage, float FireRate);
}
public class StandardDamage : MonoBehaviour, IDamageMethod
{
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
            if (Delay > 0f)
            {
                Delay -= Time.deltaTime;
                return;
            }

            ElementType damageType = GetComponent<TowerBehaviour>().DamageElement;

            GameLoopManager.EnqueueDamageData(new EnemyDamageData(Target, Damage, Target.DamageResistance, damageType));
            Delay = 1f / FireRate;
        }
        
    }

}
