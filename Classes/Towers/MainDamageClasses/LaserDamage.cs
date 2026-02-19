using UnityEngine;
using static ElementDamageType;

public class LaserDamage : MonoBehaviour, IDamageMethod
{
    [SerializeField] private Transform LaserPivot;
    [SerializeField] private LineRenderer LaserRenderer;

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
            LaserRenderer.SetPosition(0, LaserPivot.position);
            LaserRenderer.SetPosition(1, Target.RootPart.position);

            if (Delay > 0f)
            {
                Delay -= Time.deltaTime;
                return;
            }

            ElementType damageType = GetComponent<TowerBehaviour>().DamageElement;

            GameLoopManager.EnqueueDamageData(new EnemyDamageData(Target, Damage, Target.DamageResistance, damageType));
            Delay = 1f / FireRate;
            return;
        }

        LaserRenderer.enabled = false;

    }
}

    

