using UnityEngine;

/// <summary>
/// Habilidad de Luneth (boss):
/// - Deja un rastro de áreas temporales tras él que aplican un buff de velocidad
///   a los enemigos que pasen por encima.
/// - El rastro se crea cada cierta distancia o intervalo, configurable desde inspector.
/// - El TrailArea recibido tendrá asignado Owner = Luneth para que ella no reciba su propio buff.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class LunethAbility : MonoBehaviour
{
    [Tooltip("Prefab opcional de TrailArea. Si es null, se generará uno simple en runtime.")]
    public GameObject trailAreaPrefab;

    [Tooltip("Distancia mínima recorrida para tirar un segmento de rastro")]
    public float spawnDistance = 0.6f;

    [Tooltip("Intervalo de tiempo mínimo entre spawns (segundos)")]
    public float minSpawnInterval = 0.15f;

    [Header("Parámetros del área")]
    [Tooltip("Radio físico del área (colisión)")]
    public float trailRadius = 0.75f;
    [Tooltip("Duración en segundos del área")]
    public float trailDuration = 2.5f;
    [Tooltip("Multiplicador de velocidad (1.2 = +20%)")]
    public float speedMultiplier = 1.2f;

    [Header("Ajustes visuales")]
    [Tooltip("Offset de rotación (en grados) que se aplica al trail al instanciarlo. Usa (0,180,0) si aparece al revés.")]
    public Vector3 trailRotationOffsetEuler = Vector3.zero;

    [Header("Control de alcance")]
    [Tooltip("Desplaza el punto de spawn hacia atrás (en unidades) respecto a la posición del boss. Reduce alcance del trail en curvas.")]
    public float spawnBackwardOffset = 0.5f;

    private Vector3 lastSpawnPos;
    private float lastSpawnTime;

    void Start()
    {
        lastSpawnPos = transform.position;
        lastSpawnTime = -minSpawnInterval;
    }

    void Update()
    {
        // Spawn por distancia y por intervalo para estabilidad
        float dist = Vector3.Distance(transform.position, lastSpawnPos);
        if (dist >= spawnDistance && Time.time - lastSpawnTime >= minSpawnInterval)
        {
            // calcular punto ligeramente detrás del boss para acortar alcance en curvas
            Vector3 spawnPos = transform.position - transform.forward * spawnBackwardOffset;
            SpawnTrailAt(spawnPos);
            lastSpawnPos = transform.position;
            lastSpawnTime = Time.time;
        }
    }

    private void SpawnTrailAt(Vector3 position)
    {
        GameObject go;
        // base rotation: la rotación del boss
        Quaternion baseRot = transform.rotation;
        // aplicar offset configurable
        Quaternion rot = baseRot * Quaternion.Euler(trailRotationOffsetEuler);

        if (trailAreaPrefab != null)
        {
            // Instanciar con la rotación calculada
            go = Instantiate(trailAreaPrefab, position, rot);

            // Asegurar parámetros si el prefab tiene TrailArea
            var ta = go.GetComponent<TrailArea>();
            if (ta != null)
            {
                ta.duration = trailDuration;
                ta.speedMultiplier = speedMultiplier;

                // Asignar Owner para que Luneth no reciba su propio buff
                ta.Owner = this.transform;
            }

            // Ajustar radio si hay collider (área lógica)
            var sc = go.GetComponent<SphereCollider>();
            if (sc != null) sc.radius = trailRadius;

            // NOTA: no modificamos partículas aquí; controla la VFX desde el prefab del trail.
        }
        else
        {
            // Crear objeto runtime sencillo y rotarlo según el boss + offset
            go = new GameObject("LunethTrail");
            go.transform.position = position;
            go.transform.rotation = rot;

            var sc = go.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = trailRadius;

            var ta = go.AddComponent<TrailArea>();
            ta.duration = trailDuration;
            ta.speedMultiplier = speedMultiplier;
            ta.effectName = "LunethSpeed";

            // Asignar Owner para evitar afectar al creador
            ta.Owner = this.transform;
        }
    }
}