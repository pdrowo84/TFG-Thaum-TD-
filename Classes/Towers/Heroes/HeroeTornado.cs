using UnityEngine;
using UnityEngine.UI;

public class HeroeTornado : MonoBehaviour
{
    [Header("Passive Buff")]
    public float BuffRadius = 5f;
    public float DamageBuff = 1.3f; // multiplicador de da�o
    public ElementDamageType.ElementType RequiredElement = ElementDamageType.ElementType.Viento;

    [Header("Active Ability")]
    public float AbilityCooldown = 30f;
    public GameObject TornadoPrefab;
    public GameObject TornadoSpawnPoint;
    public float TornadoDuration = 5f;
    public float TornadoDamageMultiplier = 3f;

    [Header("GameFeel (Cut-in)")]
    [SerializeField] private GameFeel.HeroAbilityCutIn abilityCutIn;

    [Header("Visualization")]
    public bool ShowPassiveRadius = true;
    public Color PassiveRadiusColor = new Color(0, 1, 1, 0.5f);
    public float PassiveLineWidth = 0.2f;

    private float cooldownTimer = 0f;
    private bool isAbilityReady = true;

    private Button abilityButton;
    private GameObject activeTornado;
    private LineRenderer passiveRadiusRenderer;

    void Start()
    {
        if (abilityCutIn == null)
        {
#if UNITY_2020_1_OR_NEWER
            abilityCutIn = FindObjectOfType<GameFeel.HeroAbilityCutIn>(true);
#else
            abilityCutIn = FindObjectOfType<GameFeel.HeroAbilityCutIn>();
#endif
        }

        if (TornadoSpawnPoint == null)
        {
            TornadoSpawnPoint = GameObject.Find("TornadoSpawnPoint");
            if (TornadoSpawnPoint != null) Debug.Log("HeroeTornado: TornadoSpawnPoint encontrado.");
            else Debug.LogError("HeroeTornado: No se encontr� 'TornadoSpawnPoint' en la escena.");
        }

        GameObject buttonObj = GameObject.Find("TornadoAbilityButton");
        if (buttonObj != null)
        {
            abilityButton = buttonObj.GetComponent<Button>();
            if (abilityButton != null)
            {
                abilityButton.onClick.AddListener(ActivateAbility);
                Debug.Log("HeroeTornado: Bot�n encontrado y listener a�adido.");
            }
        }

        CreatePassiveRadiusVisualization();
        UpdateButtonState();
    }

    void Update()
    {
        ApplyPassiveBuff();
        UpdateAbilityCooldown();
        UpdatePassiveRadiusVisualization();
        UpdateButtonState();
    }

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

    public void ActivateAbility()
    {
        if (!isAbilityReady) return;

        if (abilityCutIn != null) abilityCutIn.Play();

        if (TornadoSpawnPoint == null)
            TornadoSpawnPoint = GameObject.Find("TornadoSpawnPoint");

        if (TornadoSpawnPoint == null || TornadoPrefab == null)
        {
            Debug.LogError("HeroeTornado: TornadoSpawnPoint o TornadoPrefab no est�n asignados!");
            return;
        }

        Vector3 spawnPos = TornadoSpawnPoint.transform.position;
        GameObject tornado = Instantiate(TornadoPrefab, spawnPos, Quaternion.identity);
        activeTornado = tornado;

        if (tornado.transform.localScale == Vector3.zero) tornado.transform.localScale = Vector3.one * 2f;

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

    private void UpdateButtonState()
    {
        if (abilityButton != null)
        {
            bool placed = false;
            var tb = GetComponent<TowerBehaviour>();
            if (tb != null && GameLoopManager.TowersInGame != null)
            {
                placed = GameLoopManager.TowersInGame.Contains(tb);
            }

            abilityButton.interactable = placed && isAbilityReady;

            Text buttonText = abilityButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                if (isAbilityReady && placed) buttonText.text = "Tornado";
                else
                {
                    if (!isAbilityReady) buttonText.text = $"{Mathf.CeilToInt(cooldownTimer)}s";
                    else buttonText.text = "Tornado";
                }
            }
        }
    }

    void OnDestroy()
    {
        if (passiveRadiusRenderer != null) Destroy(passiveRadiusRenderer.gameObject);

        if (abilityButton != null)
        {
            abilityButton.interactable = false;
            abilityButton.onClick.RemoveListener(ActivateAbility);
        }
    }

    public bool IsPlaced()
    {
        var tb = GetComponent<TowerBehaviour>();
        return tb != null && GameLoopManager.TowersInGame != null && GameLoopManager.TowersInGame.Contains(tb);
    }

    public bool IsAbilityAvailable()
    {
        return isAbilityReady;
    }
}