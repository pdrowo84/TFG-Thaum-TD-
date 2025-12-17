using UnityEngine;

public class TowerBehaviour : MonoBehaviour
{

    public LayerMask EnemiesLayer;

    public Enemy Target;
    public Transform TowerPivot;

    public float Damage;
    public float FireRate;
    public float Range;

    private float Delay;


    void Start()
    {
       Delay = 1/ FireRate;
    }

    public void Tick()
    {
        if(Target != null)
        {
            TowerPivot.transform.rotation = Quaternion.LookRotation(Target.transform.position - transform.position);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, Range);
    }


}
