using UnityEngine;
using System.Collections.Generic;

public class TornadoAbility : MonoBehaviour
{
    [Header("Tornado Settings")]
    public float DetectionRadius = 5f; // Radio de detección de enemigos
    public float SpiralSpeed = 3f; // Velocidad lineal del movimiento en espiral
    public float AngularSpeed = 90f; // Velocidad angular (grados por segundo)
    public float DamageTickRate = 0.2f; // Cada cuánto aplica daño (en segundos)

    [Header("Slow effect (aplica al contacto)")]
    [Tooltip("Multiplicador de velocidad aplicado a enemigos (0.5 = 50% speed)")]
    public float SlowMultiplier = 0.6f;
    [Tooltip("Duración del slow en segundos")]
    public float SlowDuration = 3f;
    [Tooltip("Nombre del efecto de slow (para evitar duplicados por nombre)")]
    public string SlowEffectName = "TornadoSlow";

    [Header("Visualization")]
    public bool ShowDetectionRadius = true; // Mostrar radio de detección
    public Color DetectionRadiusColor = new Color(0, 1, 0, 0.5f); // Verde transparente
    public float LineWidth = 0.2f; // Grosor de la línea del círculo

    private float damage;
    private float duration;
    private float currentAngle = 0f;
    private float currentRadius = 0f;
    private Vector3 centerPosition;
    private float damageTickTimer = 0f;

    // Rastreo de enemigos actualmente en contacto
    private HashSet<Enemy> enemiesInContact = new HashSet<Enemy>();

    // LineRenderer para visualización en Game View
    private LineRenderer circleRenderer;

    public void Init(float damage, float duration)
    {
        this.damage = damage;
        this.duration = duration;
        this.centerPosition = transform.position;
        this.damageTickTimer = DamageTickRate;

        Debug.Log($"TornadoAbility: Inicializado con {damage} de daño, duración {duration}s, centro en {centerPosition}");

        // Crear visualización del círculo
        CreateRadiusVisualization();
    }

    void Update()
    {
        MoveSpiralOutward();
        DetectAndDamageEnemies();
        UpdateRadiusVisualization();
    }

    // Mueve el tornado en espiral hacia afuera desde el centro
    private void MoveSpiralOutward()
    {
        currentAngle += AngularSpeed * Time.deltaTime;
        currentRadius += SpiralSpeed * Time.deltaTime;

        float angleInRadians = currentAngle * Mathf.Deg2Rad;
        float x = centerPosition.x + currentRadius * Mathf.Cos(angleInRadians);
        float z = centerPosition.z + currentRadius * Mathf.Sin(angleInRadians);

        transform.position = new Vector3(x, centerPosition.y, z);
    }

    // Detecta enemigos en el radio y aplica daño continuo + slow al contacto
    private void DetectAndDamageEnemies()
    {
        if (EntitySummoner.EnemiesInGame == null) return;

        damageTickTimer -= Time.deltaTime;

        if (damageTickTimer <= 0f)
        {
            enemiesInContact.Clear();

            foreach (Enemy enemy in EntitySummoner.EnemiesInGame)
            {
                if (enemy == null || enemy.IsDead) continue;

                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance <= DetectionRadius)
                {
                    enemiesInContact.Add(enemy);

                    // Aplica daño elemental de viento
                    GameLoopManager.EnqueueDamageData(
                        new EnemyDamageData(enemy, damage, enemy.DamageResistance, ElementDamageType.ElementType.Viento)
                    );

                    // Aplica/refresh del slow: efecto sin daño, solo SpeedMultiplier y duración.
                    // El GameLoopManager evitará duplicados y refrescará la duración si ya existe.
                    var slowEffect = new Effect(SlowEffectName, 0f, 0f, SlowDuration, ElementDamageType.ElementType.Ninguno, SlowMultiplier);
                    GameLoopManager.EnqueueEffectToApply(new ApplyEffectData(enemy, slowEffect));
                }
            }

            damageTickTimer = DamageTickRate;
        }
    }

    // Crea el LineRenderer para el círculo de detección
    private void CreateRadiusVisualization()
    {
        if (!ShowDetectionRadius) return;

        GameObject circleObject = new GameObject("TornadoRadiusVisual");
        circleObject.transform.SetParent(transform);
        circleObject.transform.localPosition = Vector3.zero;

        circleRenderer = circleObject.AddComponent<LineRenderer>();
        circleRenderer.material = new Material(Shader.Find("Sprites/Default"));
        circleRenderer.startColor = DetectionRadiusColor;
        circleRenderer.endColor = DetectionRadiusColor;
        circleRenderer.startWidth = LineWidth;
        circleRenderer.endWidth = LineWidth;
        circleRenderer.loop = true;
        circleRenderer.useWorldSpace = false;

        int segments = 16;
        circleRenderer.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * DetectionRadius;
            float z = Mathf.Sin(angle) * DetectionRadius;
            circleRenderer.SetPosition(i, new Vector3(x, 0.1f, z));
        }
    }

    // Actualiza la posición del círculo (sigue al tornado)
    private void UpdateRadiusVisualization()
    {
        if (circleRenderer != null)
        {
            float pulse = 0.5f + Mathf.Sin(Time.time * 3f) * 0.2f;
            circleRenderer.startColor = new Color(DetectionRadiusColor.r, DetectionRadiusColor.g, DetectionRadiusColor.b, pulse);
            circleRenderer.endColor = new Color(DetectionRadiusColor.r, DetectionRadiusColor.g, DetectionRadiusColor.b, pulse);
        }
    }

    // Visualización en el Editor (Gizmos)
    void OnDrawGizmos()
    {
        if (!ShowDetectionRadius) return;

        Gizmos.color = new Color(DetectionRadiusColor.r, DetectionRadiusColor.g, DetectionRadiusColor.b, 0.5f);
        Gizmos.DrawWireSphere(transform.position, DetectionRadius);

        DrawCircle(transform.position, DetectionRadius, DetectionRadiusColor);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(centerPosition, transform.position);
        }
    }

    private void DrawCircle(Vector3 center, float radius, Color color)
    {
        int segments = 32;
        float angle = 0f;
        Vector3 lastPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);

        for (int i = 1; i <= segments; i++)
        {
            angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);

            Gizmos.color = color;
            Gizmos.DrawLine(lastPoint, newPoint);

            lastPoint = newPoint;
        }
    }

    void OnDestroy()
    {
        if (circleRenderer != null)
        {
            Destroy(circleRenderer.gameObject);
        }
    }
}