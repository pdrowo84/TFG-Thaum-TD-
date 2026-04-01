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


    // Nueva: penetración de armadura (0 = ninguna, 0.5 = 50% de la resistencia ignorada, 1 = ignora toda la resistencia)
    [Tooltip("Porcentaje de penetración de armadura (0-1). Ej: 0.5 = ignora 50% de la resistencia del enemigo)")]
    [Range(0f, 1f)]
    public float ArmorPenetration = 0f;

    // Nueva: número de rebotes del láser que tiene la torre (0 = sin rebote)
    [Tooltip("Número de rebotes del láser (0 = sin rebote)")]
    [Range(0, 5)]
    public int LaserBounceCount = 0;

    // --- Valores específicos para el lanzamisiles ---
    [Header("Missile Settings")]
    [Tooltip("Radio de explosión para el lanzamisiles")]
    public float MissileExplosionRadius = 1f;

    [Tooltip("Porcentaje de ralentización aplicado por el lanzamisiles (0-1). Ej: 0.2 = -20% velocidad")]
    [Range(0f, 1f)]
    public float MissileSlowAmount = 0f;

    [Tooltip("Duración de la ralentización aplicada por el lanzamisiles (segundos)")]
    public float MissileSlowDuration = 0f;
    // ----------------------------------------------

    // --- Valores específicos para la lanzallamas ---
    [Header("Flamethrower Settings")]
    [Tooltip("Duración por defecto (s) del efecto de quemadura aplicado por la lanzallamas")]
    public float FlameBurnDuration = 5f;
    // ----------------------------------------------

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
    /// Cambia el modo de targeting de la torre (no gestiona el pago; asume que ya se descontó)
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

        // Aplicar bonus de penetración de armadura (se suma y se clampa entre 0 y 1)
        ArmorPenetration += upgrade.ArmorPenetrationBonus;
        ArmorPenetration = Mathf.Clamp(ArmorPenetration, 0f, 1f);

        // Aplicar bonus de rebotes para el láser (rama B)
        LaserBounceCount += upgrade.BounceCountBonus;
        LaserBounceCount = Mathf.Clamp(LaserBounceCount, 0, 5);

        // Aplicar mejoras específicas para el lanzamisiles
        MissileExplosionRadius += upgrade.ExplosionRadiusBonus;
        MissileExplosionRadius = Mathf.Max(0f, MissileExplosionRadius);

        MissileSlowAmount += upgrade.SlowAmountBonus;
        MissileSlowAmount = Mathf.Clamp01(MissileSlowAmount);

        MissileSlowDuration += upgrade.SlowDurationBonus;
        if (MissileSlowDuration < 0f) MissileSlowDuration = 0f;

        // Aplicar mejoras específicas para la lanzallamas (duración de quemadura)
        FlameBurnDuration += upgrade.BurnDurationBonus;
        if (FlameBurnDuration < 0f) FlameBurnDuration = 0f;

        // Asegurar que el método de daño use los nuevos valores
        CurrentDamageMethodClass?.Init(Damage, FireRate);

        // Actualizar coste invertido
        AddInvestedCost(upgrade.Cost);

        // Actualizar estado
        CurrentUpgradeState = upgrade.ResultingState;

        Debug.Log($"TowerBehaviour: Upgrade aplicado: {upgrade.UpgradeName}. Nuevo daño {Damage:F1}, cadencia {FireRate:F2}, rango {Range:F1}, penetración {ArmorPenetration:F2}, rebotes láser {LaserBounceCount}, missileRadius {MissileExplosionRadius:F2}, slow {MissileSlowAmount:F2}@{MissileSlowDuration:F1}s, burnDuration {FlameBurnDuration:F1}s");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, Range);
    }
}