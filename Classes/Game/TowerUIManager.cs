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
    public TextMeshProUGUI SellButtonText; // Texto del botón de venta (muestra el valor)

    [Header("Element Colors")]
    [Tooltip("Color para torres sin elemento")]
    public Color NoneColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);
    [Tooltip("Color para torres de elemento Fuego")]
    public Color FireColor = new Color(1f, 0.3f, 0f, 0.8f);
    [Tooltip("Color para torres de elemento Agua")]
    public Color WaterColor = new Color(0f, 0.5f, 1f, 0.8f);
    [Tooltip("Color para torres de elemento Viento")]
    public Color WindColor = new Color(0.6f, 1f, 0.6f, 0.8f);
    [Tooltip("Color para torres de elemento Tierra")]
    public Color EarthColor = new Color(0.6f, 0.4f, 0.2f, 0.8f);
    [Tooltip("Color para torres de elemento Rayo")]
    public Color LightningColor = new Color(1f, 1f, 0f, 0.8f);
    [Tooltip("Color para torres de elemento Hielo")]
    public Color IceColor = new Color(0.5f, 0.8f, 1f, 0.8f);

    [Header("Outline Colors (más intensos que el panel)")]
    public Color FireOutlineColor = new Color(1f, 0.2f, 0f, 1f);
    public Color WaterOutlineColor = new Color(0f, 0.4f, 1f, 1f);
    public Color WindOutlineColor = new Color(0.4f, 1f, 0.4f, 1f);
    public Color EarthOutlineColor = new Color(0.5f, 0.3f, 0.1f, 1f);
    public Color LightningOutlineColor = new Color(1f, 1f, 0f, 1f);
    public Color IceOutlineColor = new Color(0.3f, 0.7f, 1f, 1f);
    public Color NoneOutlineColor = Color.black;

    [Header("Sell Settings")]
    [Tooltip("Mostrar confirmación antes de vender")]
    public bool ShowSellConfirmation = true;
    public GameObject SellConfirmationPanel; // Panel de confirmación (opcional)
    public TextMeshProUGUI SellConfirmationText; // Texto de confirmación

    private TowerBehaviour currentTower;
    private TowerSelection towerSelection;
    private PlayerStats playerStats;

    void Start()
    {
        // Buscar referencias
        towerSelection = FindObjectOfType<TowerSelection>();
        playerStats = FindObjectOfType<PlayerStats>();

        // Ocultar panel al inicio
        if (TowerInfoPanel != null)
        {
            TowerInfoPanel.SetActive(false);
        }

        // Ocultar panel de confirmación
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

        // Configurar botón de venta
        if (SellButton != null)
        {
            SellButton.onClick.AddListener(OnSellButtonClicked);
        }

        if (TargetingDropdown != null)
        {
            TargetingDropdown.onValueChanged.AddListener(OnTargetingChanged);
            PopulateTargetingDropdown();
        }
    }

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
        "Último",    // Last
        "Cercano",   // Close
        "Fuerte",    // Strong
        "Débil"      // Weak
    };

        TargetingDropdown.AddOptions(options);
    }

    // (Se muestra únicamente la parte modificada: ShowTowerInfo + nueva coroutine)
    public void ShowTowerInfo(TowerBehaviour tower)
    {
        if (TowerInfoPanel == null) return;

        currentTower = tower;
        TowerInfoPanel.SetActive(true);

        // Obtener colores según el elemento
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
            TowerStatsText.text = $"Daño: {tower.Damage:F1}\n" +
                                   $"Cadencia: {tower.FireRate:F2}/s\n" +
                                   $"Rango: {tower.Range:F1}";
        }

        if (TowerElementText != null)
        {
            TowerElementText.text = $"Elemento: {tower.DamageElement}";
        }

        // Actualizar botón de venta
        UpdateSellButton(tower);

        // Actualizar dropdown de targeting**
        UpdateTargetingDropdown(tower);

        // Actualizar dropdown de filtro de elemento**
        UpdateElementFilterDropdown(tower);

        // Mostrar UI de upgrades (o ocultar panel si no hay UI)
        if (UpgradeUI != null)
        {
            // Ejecutar ShowForTower en el siguiente frame para asegurar que
            // el GameObject/Canvas del panel ya esté completamente activo y layout estabilizado.
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

        // Convertir el enum a índice del dropdown
        int dropdownIndex = (int)tower.TargetingMode;
        TargetingDropdown.SetValueWithoutNotify(dropdownIndex);
    }

    private void UpdateSellButton(TowerBehaviour tower)
    {
        if (SellButton == null) return;

        int sellValue = tower.GetSellValue();

        // Actualizar texto del botón
        if (SellButtonText != null)
        {
            SellButtonText.text = $"Vender ({sellValue}$)";
        }
        else
        {
            // Si no hay texto específico, usar el del botón
            TextMeshProUGUI buttonText = SellButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = $"Vender ({sellValue}$)";
            }
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
            // Si no hay panel de confirmación, vender directamente
            SellTower();
            return;
        }

        int sellValue = currentTower.GetSellValue();

        // Mostrar panel de confirmación
        SellConfirmationPanel.SetActive(true);

        if (SellConfirmationText != null)
        {
            SellConfirmationText.text = $"¿Vender {currentTower.name.Replace("(Clone)", "").Trim()} por {sellValue}$?";
        }
    }

    /// <summary>
    /// Confirmar venta (llamado desde el botón "Sí" del panel de confirmación)
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
    /// Cancelar venta (llamado desde el botón "No" del panel de confirmación)
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
        switch (element)
        {
            case ElementDamageType.ElementType.Fire:
                return FireColor;
            case ElementDamageType.ElementType.Water:
                return WaterColor;
            case ElementDamageType.ElementType.Wind:
                return WindColor;
            case ElementDamageType.ElementType.Rock:
                return EarthColor;
            case ElementDamageType.ElementType.None:
            default:
                return NoneColor;
        }
    }

    private Color GetOutlineColorForElement(ElementDamageType.ElementType element)
    {
        switch (element)
        {
            case ElementDamageType.ElementType.Fire:
                return FireOutlineColor;
            case ElementDamageType.ElementType.Water:
                return WaterOutlineColor;
            case ElementDamageType.ElementType.Wind:
                return WindOutlineColor;
            case ElementDamageType.ElementType.Rock:
                return EarthOutlineColor;
            case ElementDamageType.ElementType.None:
            default:
                return NoneOutlineColor;
        }
    }

    // Métodos para Fase 3 y 4 (implementaremos después)
    /// <summary>
    /// Callback cuando se cambia el dropdown de targeting
    /// </summary>
    public void OnTargetingChanged(int index)
    {
        if (currentTower == null) return;

        // Convertir índice del dropdown a enum
        TowerTargeting.TargetType newMode = (TowerTargeting.TargetType)index;
        currentTower.SetTargetingMode(newMode);

        Debug.Log($"TowerUIManager: Targeting cambiado a {newMode}");
    }
    public void OnUpgradeButtonClicked(int upgradeIndex) { }
}