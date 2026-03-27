using UnityEngine;
using static ElementDamageType;



public class TornadoHeroeDamage : MonoBehaviour, IDamageMethod
{
    [SerializeField] private Transform FirePoint; // Punto de salida de la bala
    [SerializeField] private GameObject BulletPrefab; // Prefab de la bala

    private float Damage;
    private float FireRate;
    private float Delay;
    public void Init(float Damage, float FireRate)
    {

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

            // Instancia la bala y la dirige al enemigo
            if (BulletPrefab != null && FirePoint != null)
            {
                GameObject bullet = GameObject.Instantiate(BulletPrefab, FirePoint.position, Quaternion.identity);
                StandardBullet bulletScript = bullet.GetComponent<StandardBullet>();
                if (bulletScript != null)
                {
                    var tower = GetComponent<TowerBehaviour>();
                    if (tower != null)
                    {
                        // Pasar también la penetración de armadura como cuarto parámetro
                        bulletScript.Init(Target, tower.Damage, tower.DamageElement, tower.ArmorPenetration);
                    }
                    else
                    {
                        // Fallback: si no hay TowerBehaviour, pasar 0 de penetración
                        bulletScript.Init(Target, GetComponent<TowerBehaviour>().Damage, GetComponent<TowerBehaviour>().DamageElement, 0f);
                    }
                }
            }

            Delay = 1f / FireRate;
        }

    }
}