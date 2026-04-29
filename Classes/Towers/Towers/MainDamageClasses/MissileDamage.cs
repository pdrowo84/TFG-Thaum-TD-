using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MissileDamage : MonoBehaviour, IDamageMethod
{
    public LayerMask EnemiesLayer;
    [SerializeField] private ParticleSystem MissileSystem;
    [SerializeField] private Transform TowerHead;

    private ParticleSystem.MainModule MissileSystemMain;
    public float Damage;
    private float FireRate;
    private float Delay;
    public void Init(float Damage, float FireRate)
    {
        MissileSystemMain = MissileSystem.main;

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

            // Apuntar directamente al enemigo en los 3 ejes
            Vector3 targetPos = Target.RootPart != null ? Target.RootPart.position : Target.transform.position;
            Vector3 direccion = (targetPos - TowerHead.position).normalized;
            if (direccion != Vector3.zero)
                MissileSystem.transform.rotation = Quaternion.LookRotation(direccion);

            MissileSystem.Play();
            Delay = 1f / FireRate;
        }
    }

}

