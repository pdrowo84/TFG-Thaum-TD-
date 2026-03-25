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

    // Filtro por elemento
    [Tooltip("Filtro de elemento prioritario (Any = cualquier enemigo)")]
    public TowerTargeting.ElementFilter ElementPriorityFilter = TowerTargeting.ElementFilter.Any;

    // ---------------- Upgrades ----------------
    [Header("Upgrades")]
    public TowerUpgradePath UpgradePath; // Asignar ScriptableObject con las dos ramas
    public enum UpgradeState { None = 0, A1 = 1, A2 = 2, B1 = 3, B2 = 4 }
    [HideInInspector] public UpgradeState CurrentUpgradeState = UpgradeState.None;
    // ------------------------------------------

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

    /// <summary>
    /// Verifica si se puede aplicar la mejora (según el estado actual y la mejora solicitada)
    /// </summary>
    public bool CanApplyUpgrade(TowerUpgrade upgrade)
    {
        if (upgrade == null) return false;

        switch (upgrade.ResultingState)
        {
            case UpgradeState.A1:
                return CurrentUpgradeState == UpgradeState.None;
            case UpgradeState.A2:
                return CurrentUpgradeState == UpgradeState.A1;
            case UpgradeState.B1:
                return CurrentUpgradeState == UpgradeState.None;
            case UpgradeState.B2:
                return CurrentUpgradeState == UpgradeState.B1;
            default:
                return false;
        }
    }

    /// <summary>
    /// Aplica la mejora a la torre (no gestiona el pago; asume que ya se descontó)
    /// </summary>
    public void ApplyUpgrade(TowerUpgrade upgrade)
    {
        if (upgrade == null) return;
        if (!CanApplyUpgrade(upgrade))
        {
            Debug.LogWarning("TowerBehaviour: No se puede aplicar esta mejora en el estado actual.");
            return;
        }

        // Aplicar modificadores
        Damage *= upgrade.DamageMultiplier;
        FireRate *= upgrade.FireRateMultiplier;
        Range += upgrade.RangeBonus;

        // Asegurar que el método de daño use los nuevos valores
        CurrentDamageMethodClass?.Init(Damage, FireRate);

        // Actualizar coste invertido
        AddInvestedCost(upgrade.Cost);

        // Actualizar estado
        CurrentUpgradeState = upgrade.ResultingState;

        Debug.Log($"TowerBehaviour: Upgrade aplicado: {upgrade.UpgradeName}. Nuevo daño {Damage:F1}, cadencia {FireRate:F2}, rango {Range:F1}");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, Range);
    }
}