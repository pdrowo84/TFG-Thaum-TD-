using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Maneja la UI que aparece al seleccionar una torre
/// </summary>
public class TowerUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject TowerInfoPanel;
    public Image PanelBackgroundImage;
    public TextMeshProUGUI TowerNameText;
    public TextMeshProUGUI TowerStatsText;
    public TextMeshProUGUI TowerElementText;

    public TowerUpgradeUI UpgradeUI;

    public TMP_Dropdown TargetingDropdown;
    public TMP_Dropdown ElementFilterDropdown; // **NUEVO: Dropdown de filtro por elemento**
    public GameObject UpgradePanel;
    public Button SellButton;
    public TextMeshProUGUI SellButtonText; // Texto del bot�n de venta (muestra el valor)

    [Header("Price Icon (opcional) - TextMeshPro Sprite Asset")]
    [Tooltip("Sprite asset de TextMeshPro que contiene el icono de moneda (crear con 'Create -> TextMeshPro -> Sprite Asset').")]
    public TMP_SpriteAsset CoinSpriteAssetTMP;
    [Tooltip("Nombre del sprite dentro del TMP Sprite Asset (si se deja vac�o se usar� el �ndice).")]
    public string CoinSpriteName = "";
    [Tooltip("�ndice del sprite dentro del TMP Sprite Asset (usado si CoinSpriteName est� vac�o).")]
    public int CoinSpriteIndex = 0;

    [Header("Price Icon (legacy, optional Images)")]
    [Tooltip("Sprite de la moneda (legacy).")]
    public Sprite CoinSprite;
    [Tooltip("Image que muestra el icono de la moneda junto al precio en el bot�n de venta (opcional).")]
    public Image SellButtonPriceIcon;
    [Tooltip("Image que muestra el icono de la moneda en el panel de confirmaci�n (opcional).")]
    public Image SellConfirmationPriceIcon;

    [Header("Element Colors (legacy defaults)")]
    [Tooltip("Color para torres sin elemento")]
    public Color NoneColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [Tooltip("Color para torres de elemento Fuego")]
    public Color FireColor = new Color(1f, 0.3f, 0f, 1f);
    [Tooltip("Color para torres de elemento Agua")]
    public Color WaterColor = new Color(0f, 0.5f, 1f, 1f);
    [Tooltip("Color para torres de elemento Viento")]
    public Color WindColor = new Color(0.6f, 1f, 0.6f, 1f);
    [Tooltip("Color para torres de elemento Roca")]
    public Color EarthColor = new Color(0.6f, 0.4f, 0.2f, 1f);


    [Header("Outline Colors (m�s intensos que el panel) - legacy defaults")]
    public Color FireOutlineColor = new Color(1f, 0.2f, 0f, 1f);
    public Color WaterOutlineColor = new Color(0f, 0.4f, 1f, 1f);
    public Color WindOutlineColor = new Color(0.4f, 1f, 0.4f, 1f);
    public Color EarthOutlineColor = new Color(0.5f, 0.3f, 0.1f, 1f);
    public Color NoneOutlineColor = Color.black;

    [Header("Editable Element / Panel Color Sets")]
    [Tooltip("Define aqu� los colores de panel y outline por elemento. Si est� vac�o se usan los valores legacy de arriba.")]
    public List<ElementColorSet> ElementColorSets = new List<ElementColorSet>();

    [Tooltip("Color por defecto del fondo del panel si no hay entrada para el elemento")]
    public Color PanelDefaultColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Tooltip("Color por defecto del outline si no hay entrada para el elemento")]
    public Color OutlineDefaultColor = Color.black;

    [System.Serializable]
    public class ElementColorSet
    {
        public ElementDamageType.ElementType Element = ElementDamageType.ElementType.Ninguno;
        public Color PanelColor = Color.gray;
        public Color OutlineColor = Color.black;
    }

    [Header("Sell Settings")]
    [Tooltip("Mostrar confirmaci�n antes de vender")]
    public bool ShowSellConfirmation = true;
    public GameObject SellConfirmationPanel; // Panel de confirmaci�n (opcional)
    public TextMeshProUGUI SellConfirmationText; // Texto de confirmaci�n

    private TowerBehaviour currentTower;
    private TowerSelection towerSelection;
    private PlayerStats playerStats;

    private void OnValidate()
    {
        // Si el dise�ador no ha rellenado ElementColorSets, inicializar con los valores legacy
        if (ElementColorSets == null || ElementColorSets.Count == 0)
        {
            ElementColorSets = new List<ElementColorSet>
            {
                new ElementColorSet { Element = ElementDamageType.ElementType.Ninguno, PanelColor = NoneColor, OutlineColor = NoneOutlineColor },
                new ElementColorSet { Element = ElementDamageType.ElementType.Fuego, PanelColor = FireColor, OutlineColor = FireOutlineColor },
                new ElementColorSet { Element = ElementDamageType.ElementType.Agua, PanelColor = WaterColor, OutlineColor = WaterOutlineColor },
                new ElementColorSet { Element = ElementDamageType.ElementType.Viento, PanelColor = WindColor, OutlineColor = WindOutlineColor },
                new ElementColorSet { Element = ElementDamageType.ElementType.Roca, PanelColor = EarthColor, OutlineColor = EarthOutlineColor }
            };

            // Ajustar defaults coherentes con legacy
            PanelDefaultColor = NoneColor;
            OutlineDefaultColor = NoneOutlineColor;
        }
    }

    void Start()
    {
        // Buscar referencias
        tower_selection_init();
        player_stats_init();

        // Ocultar panel al inicio
        if (TowerInfoPanel != null)
        {
            TowerInfoPanel.SetActive(false);
        }

        // Ocultar panel de confirmaci�n
        if (SellConfirmationPanel != null)
        {
            SellConfirmationPanel.SetActive(false);
        }

        if (ElementFilterDropdown != null)
        {
            ElementFilterDropdown.onValueChanged.AddListener(OnElementFilterChanged);
            PopulateElementFilterDropdown();
        }

        // Verificar que tengamos la referencia a la imagen de fondo
        if (PanelBackgroundImage == null && TowerInfoPanel != null)
        {
            PanelBackgroundImage = TowerInfoPanel.GetComponent<Image>();
        }

        // Configurar bot�n de venta
        if (SellButton != null)
        {
            SellButton.onClick.AddListener(OnSellButtonClicked);
        }

        if (TargetingDropdown != null)
        {
            TargetingDropdown.onValueChanged.AddListener(OnTargetingChanged);
            PopulateTargetingDropdown();
        }

        // Legacy: ocultar Image icons si est�n asignados (usaremos TMP sprite inline si se asigna)
        if (SellButtonPriceIcon != null)
        {
            SellButtonPriceIcon.gameObject.SetActive(false);
        }
        if (SellConfirmationPriceIcon != null)
        {
            SellConfirmationPriceIcon.gameObject.SetActive(false);
        }
    }

    // peque�os helpers para Start() claridad
    private void tower_selection_init() => towerSelection = FindObjectOfType<TowerSelection>();
    private void player_stats_init() => playerStats = FindObjectOfType<PlayerStats>();

    /// <summary>
    /// Poblar el dropdown de filtro de elemento
    /// </summary>
    private void PopulateElementFilterDropdown()
    {
        if (ElementFilterDropdown == null) return;

        ElementFilterDropdown.ClearOptions();

        List<string> options = new List<string>
    {
        "Cualquiera",  // Any
        "Fuego",       // Fire
        "Agua",        // Water
        "Viento",      // Wind
        "Roca"         // Rock
    };

        ElementFilterDropdown.AddOptions(options);
    }

    /// <summary>
    /// Poblar el dropdown con los modos de targeting
    /// </summary>
    private void PopulateTargetingDropdown()
    {
        if (TargetingDropdown == null) return;

        TargetingDropdown.ClearOptions();

        List<string> options = new List<string>
    {
        "Primero",   // First
        "Ultimo",    // Last
        "Cercano",   // Close
        "Fuerte",    // Strong
        "Debil"      // Weak
    };

        TargetingDropdown.AddOptions(options);
    }

    // (Se muestra �nicamente la parte modificada: ShowTowerInfo + nueva coroutine)
    public void ShowTowerInfo(TowerBehaviour tower)
    {
        if (TowerInfoPanel == null) return;

        currentTower = tower;
        TowerInfoPanel.SetActive(true);

        // Obtener colores seg�n el elemento
        Color panelColor = GetPanelColorForElement(tower.DamageElement);
        Color outlineColor = GetOutlineColorForElement(tower.DamageElement);

        // Aplicar color al panel
        if (PanelBackgroundImage != null)
        {
            PanelBackgroundImage.color = panelColor;
        }

        // Notificar a TowerSelection para cambiar el color del outline
        if (towerSelection != null)
        {
            towerSelection.UpdateOutlineColor(outlineColor);
        }

        // Actualizar textos
        if (TowerNameText != null)
        {
            TowerNameText.text = tower.name.Replace("(Clone)", "").Trim();
        }

        if (TowerStatsText != null)
        {
            TowerStatsText.text = $"Ataque: {tower.Damage:F1}\n" +
                                  $"Cadencia: {tower.FireRate:F2}/s\n" +
                                  $"Rango: {tower.Range:F1}\n" +
                                  $"Pen. Armadura: {tower.ArmorPenetration:F1}\n";
        }

        if (TowerElementText != null)
        {
            TowerElementText.text = $"Elemento: {tower.DamageElement}";
        }

        // Actualizar bot�n de venta
        UpdateSellButton(tower);

        // Actualizar dropdown de targeting**
        UpdateTargetingDropdown(tower);

        // Actualizar dropdown de filtro de elemento**
        UpdateElementFilterDropdown(tower);

        // Mostrar UI de upgrades (o ocultar panel si no hay UI)
        if (UpgradeUI != null)
        {
            // Ejecutar ShowForTower en el siguiente frame para asegurar que
            // el GameObject/Canvas del panel ya est� completamente activo y layout estabilizado.
            StopCoroutine("ShowUpgradeUINextFrame");
            StartCoroutine(ShowUpgradeUINextFrame(tower));
        }
        else if (UpgradePanel != null)
        {
            UpgradePanel.SetActive(false);
        }
    }

    private IEnumerator ShowUpgradeUINextFrame(TowerBehaviour tower)
    {
        // Esperar fin de frame permite a Unity activar objetos y recalcular layouts
        // antes de que el UpgradeUI instancie/forceje el layout.
        yield return null;

        if (UpgradeUI != null)
            UpgradeUI.ShowForTower(tower);
    }

    /// <summary>
    /// Actualiza el dropdown de filtro de elemento
    /// </summary>
    private void UpdateElementFilterDropdown(TowerBehaviour tower)
    {
        if (ElementFilterDropdown == null) return;

        int dropdownIndex = (int)tower.ElementPriorityFilter;
        ElementFilterDropdown.SetValueWithoutNotify(dropdownIndex);
    }

    /// <summary>
    /// Callback cuando se cambia el filtro de elemento
    /// </summary>
    public void OnElementFilterChanged(int index)
    {
        if (currentTower == null) return;

        TowerTargeting.ElementFilter newFilter = (TowerTargeting.ElementFilter)index;
        currentTower.SetElementFilter(newFilter);

        Debug.Log($"TowerUIManager: Filtro de elemento cambiado a {newFilter}");
    }

    private void UpdateTargetingDropdown(TowerBehaviour tower)
    {
        if (TargetingDropdown == null) return;

        // Convertir el enum a �ndice del dropdown
        int dropdownIndex = (int)tower.TargetingMode;
        TargetingDropdown.SetValueWithoutNotify(dropdownIndex);
    }

    private void UpdateSellButton(TowerBehaviour tower)
    {
        if (SellButton == null) return;

        int sellValue = tower.GetSellValue();

        if (SellButtonText != null)
        {
            SellButtonText.text = $"VENDER";
            if (SellButtonPriceIcon != null) SellButtonPriceIcon.gameObject.SetActive(false);
        }
    }

    public void OnSellButtonClicked()
    {
        if (currentTower == null) return;

        if (ShowSellConfirmation)
        {
            ShowSellConfirmationDialog();
        }
        else
        {
            SellTower();
        }
    }

    private void ShowSellConfirmationDialog()
    {
        if (SellConfirmationPanel == null)
        {
            // Si no hay panel de confirmaci�n, vender directamente
            SellTower();
            return;
        }

        int sellValue = currentTower.GetSellValue();

        // Mostrar panel de confirmaci�n
        SellConfirmationPanel.SetActive(true);

        if (SellConfirmationText != null)
        {
            if (CoinSpriteAssetTMP != null)
            {
                SellConfirmationText.spriteAsset = CoinSpriteAssetTMP;
                string spriteTag = !string.IsNullOrEmpty(CoinSpriteName)
                    ? $"<sprite name=\"{CoinSpriteName}\">"
                    : $"<sprite index={CoinSpriteIndex}>";

                SellConfirmationText.text = $"�Vender {currentTower.name.Replace("(Clone)", "").Trim()} por {sellValue} {spriteTag}?";
                if (SellConfirmationPriceIcon != null) SellConfirmationPriceIcon.gameObject.SetActive(false);
            }
            else
            {
                SellConfirmationText.text = $"�Vender {currentTower.name.Replace("(Clone)", "").Trim()} por {sellValue}$?";
                if (SellConfirmationPriceIcon != null) SellConfirmationPriceIcon.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Confirmar venta (llamado desde el bot�n "S�" del panel de confirmaci�n)
    /// </summary>
    public void ConfirmSell()
    {
        if (SellConfirmationPanel != null)
        {
            SellConfirmationPanel.SetActive(false);
        }

        SellTower();
    }

    /// <summary>
    /// Cancelar venta (llamado desde el bot�n "No" del panel de confirmaci�n)
    /// </summary>
    public void CancelSell()
    {
        if (SellConfirmationPanel != null)
        {
            SellConfirmationPanel.SetActive(false);
        }
    }

    private void SellTower()
    {
        if (currentTower == null) return;

        // Calcular el valor de venta
        int sellValue = currentTower.GetSellValue();

        // Devolver dinero al jugador
        if (playerStats != null)
        {
            playerStats.AddMoney(sellValue);
            Debug.Log($"TowerUIManager: Torre vendida por {sellValue}$");
        }

        // Remover de la lista de torres activas
        if (GameLoopManager.TowersInGame != null)
        {
            GameLoopManager.TowersInGame.Remove(currentTower);
        }

        // Guardar referencia antes de destruir
        GameObject towerObject = currentTower.gameObject;

        // Ocultar UI y deseleccionar
        HideTowerInfo();
        if (towerSelection != null)
        {
            towerSelection.DeselectTower();
        }

        // Destruir la torre
        Destroy(towerObject);

        Debug.Log("TowerUIManager: Torre destruida.");
    }

    // peque�a ayuda para evitar warnings por referencia nula en el dif
    private bool tower_selection_is_not_null() => towerSelection != null;

    public void HideTowerInfo()
    {
        if (TowerInfoPanel != null)
        {
            TowerInfoPanel.SetActive(false);
        }

        currentTower = null;
    }

    public TowerBehaviour GetCurrentTower()
    {
        return currentTower;
    }

    private Color GetPanelColorForElement(ElementDamageType.ElementType element)
    {
        if (ElementColorSets != null && ElementColorSets.Count > 0)
        {
            var set = ElementColorSets.Find(x => x.Element == element);
            if (set != null)
                return set.PanelColor;
        }

        // Fallback a legacy switch si no hay set definido
        switch (element)
        {
            case ElementDamageType.ElementType.Fuego:
                return FireColor;
            case ElementDamageType.ElementType.Agua:
                return WaterColor;
            case ElementDamageType.ElementType.Viento:
                return WindColor;
            case ElementDamageType.ElementType.Roca:
                return EarthColor;
            case ElementDamageType.ElementType.Ninguno:
            default:
                return PanelDefaultColor != default ? PanelDefaultColor : NoneColor;
        }
    }

    private Color GetOutlineColorForElement(ElementDamageType.ElementType element)
    {
        if (ElementColorSets != null && ElementColorSets.Count > 0)
        {
            var set = ElementColorSets.Find(x => x.Element == element);
            if (set != null)
                return set.OutlineColor;
        }

        // Fallback a legacy switch si no hay set definido
        switch (element)
        {
            case ElementDamageType.ElementType.Fuego:
                return FireOutlineColor;
            case ElementDamageType.ElementType.Agua:
                return WaterOutlineColor;
            case ElementDamageType.ElementType.Viento:
                return WindOutlineColor;
            case ElementDamageType.ElementType.Roca:
                return EarthOutlineColor;
            case ElementDamageType.ElementType.Ninguno:
            default:
                return OutlineDefaultColor != default ? OutlineDefaultColor : NoneOutlineColor;
        }
    }

    // M�todos para Fase 3 y 4 (implementaremos despu�s)
    /// <summary>
    /// Callback cuando se cambia el dropdown de targeting
    /// </summary>
    public void OnTargetingChanged(int index)
    {
        if (currentTower == null) return;

        // Convertir �ndice del dropdown a enum
        TowerTargeting.TargetType newMode = (TowerTargeting.TargetType)index;
        currentTower.SetTargetingMode(newMode);

        Debug.Log($"TowerUIManager: Targeting cambiado a {newMode}");
    }
    public void OnUpgradeButtonClicked(int upgradeIndex) { }
}