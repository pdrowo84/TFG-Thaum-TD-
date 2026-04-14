using UnityEngine;
using static ElementDamageType;

public class StandardBullet : MonoBehaviour
{
    private Enemy target;
    private float speed = 20f;
    private float damage;
    private ElementType damageType;
    private float penetration; // nueva: penetraci�n de armadura de la torre que dispar�

    [Tooltip("Si se asigna, solo este transform visual será orientado hacia el objetivo. Si es null, se usará el transform root.")]
    [SerializeField] private Transform visual;

    public void Init(Enemy target, float damage, ElementType damageType, float penetration)
    {
        this.target = target;
        this.damage = damage;
        this.damageType = damageType;
        this.penetration = Mathf.Clamp01(penetration);
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

        // Orienta solo el visual (si existe) o el root (comportamiento previo)
        Transform rotTarget = visual != null ? visual : transform;
        Vector3 lookSourcePos = rotTarget.position;
        Vector3 toTarget = (targetPos - lookSourcePos);
        if (toTarget.sqrMagnitude > 0.000001f)
        {
            rotTarget.rotation = Quaternion.LookRotation(toTarget.normalized);
        }
    }

    void HitTarget()
    {
        // Encola el da�o para que lo procese GameLoopManager (respetando resistencias/inmunidades)
        GameLoopManager.EnqueueDamageData(new EnemyDamageData(target, damage, target.DamageResistance, damageType, penetration));
        Destroy(gameObject);
    }
}