using UnityEngine;

/// <summary>
/// Habilidad de Solkar (boss):
/// - Al llegar a un nuevo NodeIndex incrementa su vida y escala progresivamente.
/// - Permite override del RootPart para anclar pivote de spawn/escalado.
/// - Escala anclando al RootPartOverrideObject para que el "cuerpo" crezca desde ese punto.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class SolkarAbility : MonoBehaviour
{
    [Tooltip("Porcentaje de aumento de vida por nodo (ej. 0.02 = +2%)")]
    [Range(0f, 0.2f)]
    public float healthIncreasePerNode = 0.02f;

    [Tooltip("Porcentaje de aumento de escala por nodo (ej. 0.02 = +2%)")]
    [Range(0f, 0.2f)]
    public float scaleIncreasePerNode = 0.02f;

    [Header("Spawn / RootPart")]
    [Tooltip("Si se asigna, este GameObject se usará como RootPart del Enemy (drag en el prefab)")]
    public GameObject RootPartOverrideObject;

    private Enemy enemy;
    private int lastNodeIndex;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        if (enemy == null)
            Debug.LogError("SolkarAbility: falta componente Enemy en el GameObject.");
    }

    void OnEnable()
    {
        // Asegurar enemy referenciado (puede inicializarse después en pooling)
        if (enemy == null) enemy = GetComponent<Enemy>();
        if (enemy == null) return;

        // Aplicar RootPart override cuando la instancia se active (funciona bien con pooling)
        if (RootPartOverrideObject != null)
        {
            enemy.RootPart = RootPartOverrideObject.transform;
        }

        // Reiniciar seguimiento de nodo a la posición actual del enemy
        lastNodeIndex = enemy.NodeIndex;
    }

    void Update()
    {
        if (enemy == null) return;

        // Detectar avance de nodo
        if (enemy.NodeIndex > lastNodeIndex)
        {
            int nodesAdvanced = enemy.NodeIndex - lastNodeIndex;
            ApplyGrowth(nodesAdvanced);
            lastNodeIndex = enemy.NodeIndex;
        }
    }

    private void ApplyGrowth(int steps)
    {
        if (steps <= 0) return;

        // Aplicar multiplicador acumulativo por cada paso
        float healthFactor = Mathf.Pow(1f + healthIncreasePerNode, steps);
        float scaleFactor = Mathf.Pow(1f + scaleIncreasePerNode, steps);

        // Aumentar MaxHealth y Health proporcionalmente
        enemy.MaxHealth *= healthFactor;
        enemy.Health *= healthFactor; // mantiene ratio actual de vida

        // Escalado: si hay RootPartOverrideObject lo usamos como pivote para mantenerlo fijo en mundo.
        if (RootPartOverrideObject != null)
        {
            Vector3 pivotBefore = RootPartOverrideObject.transform.position;

            // Escalar el transform raíz (aumenta todo el cuerpo visual y colliders)
            transform.localScale *= scaleFactor;

            // Tras escalar, el world position del pivot puede cambiar; compensamos moviendo el root
            Vector3 pivotAfter = RootPartOverrideObject.transform.position;
            Vector3 compensate = pivotBefore - pivotAfter;
            transform.position += compensate;
        }
        else
        {
            // Sin override, escala desde el pivot del root
            transform.localScale *= scaleFactor;
        }

        Debug.Log($"Solkar: nodo alcanzado. +{(healthFactor - 1f) * 100f:F2}% vida, +{(scaleFactor - 1f) * 100f:F2}% escala.");
    }
}