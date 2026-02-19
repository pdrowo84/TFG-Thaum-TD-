using UnityEngine;
using static ElementDamageType;

public class StandardBullet : MonoBehaviour
{
    private Enemy target;
    private float speed = 20f;
    private float damage;
    private ElementType damageType;

    public void Init(Enemy target, float damage, ElementType damageType)
    {
        this.target = target;
        this.damage = damage;
        this.damageType = damageType;
    }

    void Update()
    {
        if (target == null || target.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPos = target.RootPart != null ? target.RootPart.position : target.transform.position;
        Vector3 dir = targetPos - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

        if (dir.magnitude <= distanceThisFrame)
        {
            HitTarget();
            return;
        }

        transform.Translate(dir.normalized * distanceThisFrame, Space.World);
        transform.LookAt(targetPos);
    }

    void HitTarget()
    {
        // Encola el daño para que lo procese GameLoopManager (respetando resistencias/inmunidades)
        GameLoopManager.EnqueueDamageData(new EnemyDamageData(target, damage, target.DamageResistance, damageType));
        Destroy(gameObject);
    }
}
