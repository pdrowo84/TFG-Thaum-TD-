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

            MissileSystem.transform.rotation = GetComponent<TowerBehaviour>().TowerPivot.rotation;
            MissileSystemMain.startRotationX = TowerHead.forward.x;
            MissileSystemMain.startRotationY = TowerHead.forward.y;
            MissileSystemMain.startRotationZ = TowerHead.forward.z;

            MissileSystem.Play();
            Delay = 1f / FireRate;
        }

    }

}

