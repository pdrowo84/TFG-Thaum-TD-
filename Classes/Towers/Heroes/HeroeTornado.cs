using UnityEngine;
using UnityEngine.UI;

public class HeroeTornado : MonoBehaviour
{
    [Header("Passive Buff")]
    public float BuffRadius = 5f;
    public float DamageBuff = 2f; // multiplicador de daño (2 = x2)
    public ElementDamageType.ElementType RequiredElement = ElementDamageType.ElementType.Wind;

    [Header("Active Ability")]
    public float AbilityCooldown = 30f; // Tiempo de enfriamiento en segundos
    public GameObject TornadoPrefab; // Prefab del tornado (cubo blanco por ahora)
    public GameObject TornadoSpawnPoint; // GameObject en el centro del mapa (asignar manualmente o se busca automáticamente)
    public float TornadoDuration = 5f; // Duración de la habilidad activa
    public float TornadoDamageMultiplier = 3f; // Multiplicador del daño del héroe

    [Header("Visualization")]
    public bool ShowPassiveRadius = true; // Mostrar radio de buff pasivo
    public Color PassiveRadiusColor = new Color(0, 1, 1, 0.5f); // Cyan transparente
    public float PassiveLineWidth = 0.2f; // Grosor de la línea

    private float cooldownTimer = 0f;
    private bool isAbilityReady = true;

    // Referencia para encontrar el botón por código
    private Button abilityButton;

    // Referencia al tornado activo para visualización
    private GameObject activeTornado;

    // LineRenderer para el radio pasivo
    private LineRenderer passiveRadiusRenderer;

    void Start()
    {
        // Si no está asignado manualmente, buscar por nombre
        if (TornadoSpawnPoint == null)
        {
            TornadoSpawnPoint = GameObject.Find("TornadoSpawnPoint");
            if (TornadoSpawnPoint != null)
            {
                Debug.Log("HeroeTornado: TornadoSpawnPoint encontrado.");
            }
            else
            {
                Debug.LogError("HeroeTornado: No se encontró 'TornadoSpawnPoint' en la escena.");
            }
        }

        // Buscar el botón por nombre
        GameObject buttonObj = GameObject.Find("TornadoAbilityButton");
        if (buttonObj != null)
        {
            abilityButton = buttonObj.GetComponent<Button>();
            if (abilityButton != null)
            {
                abilityButton.onClick.RemoveAllListeners();
                abilityButton.onClick.AddListener(ActivateAbility);
                Debug.Log("HeroeTornado: Botón encontrado y listener añadido.");
            }
        }

        // Crear visualización del radio pasivo
        CreatePassiveRadiusVisualization();

        UpdateButtonState();
    }

    void Update()
    {
        ApplyPassiveBuff();
        UpdateAbilityCooldown();
        UpdatePassiveRadiusVisualization();
    }

    // Crea el LineRenderer para el radio del buff pasivo
    private void CreatePassiveRadiusVisualization()
    {
        if (!ShowPassiveRadius) return;

        GameObject circleObject = new GameObject("PassiveBuffRadiusVisual");
        circleObject.transform.SetParent(transform);
        circleObject.transform.localPosition = Vector3.zero;

        passiveRadiusRenderer = circleObject.AddComponent<LineRenderer>();
        passiveRadiusRenderer.material = new Material(Shader.Find("Sprites/Default"));
        passiveRadiusRenderer.startColor = PassiveRadiusColor;
        passiveRadiusRenderer.endColor = PassiveRadiusColor;
        passiveRadiusRenderer.startWidth = PassiveLineWidth;
        passiveRadiusRenderer.endWidth = PassiveLineWidth;
        passiveRadiusRenderer.loop = true;
        passiveRadiusRenderer.useWorldSpace = false;

        int segments = 64;
        passiveRadiusRenderer.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * BuffRadius;
            float z = Mathf.Sin(angle) * BuffRadius;
            passiveRadiusRenderer.SetPosition(i, new Vector3(x, 0.1f, z));
        }
    }

    // Actualiza el efecto visual del radio pasivo (pulso)
    private void UpdatePassiveRadiusVisualization()
    {
        if (passiveRadiusRenderer != null)
        {
            float pulse = 0.3f + Mathf.Sin(Time.time * 2f) * 0.15f;
            Color color = new Color(PassiveRadiusColor.r, PassiveRadiusColor.g, PassiveRadiusColor.b, pulse);
            passiveRadiusRenderer.startColor = color;
            passiveRadiusRenderer.endColor = color;
        }
    }

    // Aplica el buff pasivo a torres cercanas con elemento viento
    private void ApplyPassiveBuff()
    {
        TowerBehaviour myTower = GetComponent<TowerBehaviour>();
        if (myTower == null || GameLoopManager.TowersInGame == null) return;

        foreach (var tower in GameLoopManager.TowersInGame)
        {
            if (tower == null || tower == myTower) continue;

            bool inRange = Vector3.Distance(transform.position, tower.transform.position) < BuffRadius;
            bool hasRequiredElement = tower.DamageElement == RequiredElement;

            if (inRange && hasRequiredElement)
            {
                if (!tower.HasHeroBuff)
                {
                    tower.Damage *= DamageBuff;
                    tower.HasHeroBuff = true;
                }
            }
            else
            {
                if (tower.HasHeroBuff)
                {
                    tower.Damage /= DamageBuff;
                    tower.HasHeroBuff = false;
                }
            }
        }
    }

    // Actualiza el temporizador de enfriamiento
    private void UpdateAbilityCooldown()
    {
        if (!isAbilityReady)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isAbilityReady = true;
                cooldownTimer = 0f;
            }
            UpdateButtonState();
        }
    }

    // Activa la habilidad especial
    public void ActivateAbility()
    {
        if (!isAbilityReady) return;

        if (TornadoSpawnPoint == null)
        {
            TornadoSpawnPoint = GameObject.Find("TornadoSpawnPoint");
        }

        if (TornadoSpawnPoint == null || TornadoPrefab == null)
        {
            Debug.LogError("HeroeTornado: TornadoSpawnPoint o TornadoPrefab no están asignados!");
            return;
        }

        Vector3 spawnPos = TornadoSpawnPoint.transform.position;
        GameObject tornado = Instantiate(TornadoPrefab, spawnPos, Quaternion.identity);
        activeTornado = tornado;

        if (tornado.transform.localScale == Vector3.zero)
        {
            tornado.transform.localScale = Vector3.one * 2f;
        }

        TornadoAbility tornadoScript = tornado.GetComponent<TornadoAbility>();
        if (tornadoScript != null)
        {
            TowerBehaviour myTower = GetComponent<TowerBehaviour>();
            if (myTower != null)
            {
                float damage = myTower.Damage * TornadoDamageMultiplier;
                tornadoScript.Init(damage, TornadoDuration);
            }
        }

        Destroy(tornado, TornadoDuration);
        Invoke(nameof(ClearTornadoReference), TornadoDuration);

        isAbilityReady = false;
        cooldownTimer = AbilityCooldown;
        UpdateButtonState();
    }

    private void ClearTornadoReference()
    {
        activeTornado = null;
    }

    // Actualiza el estado visual del botón
    private void UpdateButtonState()
    {
        if (abilityButton != null)
        {
            abilityButton.interactable = isAbilityReady;

            Text buttonText = abilityButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                if (isAbilityReady)
                {
                    buttonText.text = "Tornado";
                }
                else
                {
                    buttonText.text = $"{Mathf.CeilToInt(cooldownTimer)}s";
                }
            }
        }
    }

    // Visualización en el Editor (Gizmos)
    void OnDrawGizmos()
    {
        if (ShowPassiveRadius)
        {
            Gizmos.color = new Color(PassiveRadiusColor.r, PassiveRadiusColor.g, PassiveRadiusColor.b, 0.3f);
            Gizmos.DrawWireSphere(transform.position, BuffRadius);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (ShowPassiveRadius)
        {
            Gizmos.color = PassiveRadiusColor;
            Gizmos.DrawWireSphere(transform.position, BuffRadius);
        }
    }

    void OnDestroy()
    {
        if (passiveRadiusRenderer != null)
        {
            Destroy(passiveRadiusRenderer.gameObject);
        }
    }
}