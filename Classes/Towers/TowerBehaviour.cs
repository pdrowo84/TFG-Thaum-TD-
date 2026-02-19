using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class TowerBehaviour : MonoBehaviour
{
    public ElementDamageType.ElementType DamageElement;

    public LayerMask EnemiesLayer;

    public Enemy Target;
    public Transform TowerPivot;

    public float Damage;
    public float FireRate;
    public float Range;
    public int SummonCost = 100;
    private float Delay;

    private IDamageMethod CurrentDamageMethodClass;

    void Start()
    {
        CurrentDamageMethodClass = GetComponent<IDamageMethod>();

        if(CurrentDamageMethodClass == null)
        {
            Debug.LogError("TOWERS: No damage class attached to given tower!");
        }

        else
        {
            CurrentDamageMethodClass.Init(Damage, FireRate);
        }

        Delay = 1/ FireRate;
    }

    public void Tick()
    {
        CurrentDamageMethodClass.DamageTick(Target);

        if (Target != null)
        {
            Vector3 targetPos = Target.RootPart != null ? Target.RootPart.position : Target.transform.position;
            TowerPivot.transform.rotation = Quaternion.LookRotation(targetPos - TowerPivot.position);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, Range);
    }


}
