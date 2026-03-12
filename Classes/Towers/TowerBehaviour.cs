using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class TowerBehaviour : MonoBehaviour
{
    public ElementDamageType.ElementType DamageElement;

    public LayerMask EnemiesLayer;

    public Enemy Target;
    public Transform TowerPivot;

    public bool HasHeroBuff = false;

    public float Damage;
    public float FireRate;
    public float Range;
    public int SummonCost;

    // Sistema de venta
    [HideInInspector]
    public int TotalInvestedCost;
    [Tooltip("Porcentaje del coste que se devuelve al vender (0-100)")]
    public float SellRefundPercentage = 70f;

    // Sistema de targeting
    [Header("Targeting Settings")]
    [Tooltip("Modo de apuntado de la torre")]
    public TowerTargeting.TargetType TargetingMode = TowerTargeting.TargetType.First;

    // **NUEVO: Filtro por elemento**
    [Tooltip("Filtro de elemento prioritario (None = cualquier enemigo)")]
    public TowerTargeting.ElementFilter ElementPriorityFilter = TowerTargeting.ElementFilter.Any;

    private float Delay;

    private IDamageMethod CurrentDamageMethodClass;

    void Start()
    {
        // Inicializar el coste total invertido con el coste de colocación
        if (TotalInvestedCost == 0)
        {
            TotalInvestedCost = SummonCost;
        }

        CurrentDamageMethodClass = GetComponent<IDamageMethod>();

        if (CurrentDamageMethodClass == null)
        {
            Debug.LogError("TOWERS: No damage class attached to given tower!");
        }

        else
        {
            CurrentDamageMethodClass.Init(Damage, FireRate);
        }

        Delay = 1 / FireRate;
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

    /// <summary>
    /// Calcula el dinero que se devuelve al vender esta torre
    /// </summary>
    public int GetSellValue()
    {
        return Mathf.RoundToInt(TotalInvestedCost * (SellRefundPercentage / 100f));
    }

    /// <summary>
    /// Añade coste al total invertido (por ejemplo, al comprar upgrades)
    /// </summary>
    public void AddInvestedCost(int cost)
    {
        TotalInvestedCost += cost;
    }

    /// <summary>
    /// Cambia el modo de targeting de la torre
    /// </summary>
    public void SetTargetingMode(TowerTargeting.TargetType newMode)
    {
        TargetingMode = newMode;
        Debug.Log($"TowerBehaviour: Modo de targeting cambiado a {newMode}");
    }

    /// <summary>
    /// Cambia el filtro de elemento prioritario
    /// </summary>
    public void SetElementFilter(TowerTargeting.ElementFilter newFilter)
    {
        ElementPriorityFilter = newFilter;
        Debug.Log($"TowerBehaviour: Filtro de elemento cambiado a {newFilter}");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, Range);
    }
}