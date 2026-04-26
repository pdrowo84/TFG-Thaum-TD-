using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Gestión del panel de upgrades.
/// Muestra 1 botón por rama; al comprar la mejora se sustituye por el siguiente nivel
/// y se bloquea la rama opuesta cuando procede.
/// </summary>
public class TowerUpgradeUI : MonoBehaviour
{
    public RectTransform BranchAContainer;
    public RectTransform BranchBContainer;
    public Button UpgradeButtonPrefab; // Prefab: Button con TextMeshPro child

    [Header("Upgrade Button Feedback")]
    [SerializeField] private bool addUIButtonFeedback = false;
    [SerializeField] private bool enableUpgradeButtonWobble = false;
    [SerializeField] private bool enableUpgradeButtonClickPunch = false;

    // Nuevo: TMP Sprite Asset para icono de moneda usado en los textos de coste de las mejoras
    [Header("Price Icon (TextMeshPro Sprite Asset)")]
    [Tooltip("Sprite asset de TextMeshPro que contiene el icono de moneda (Create -> TextMeshPro -> Sprite Asset).")]
    public TMP_SpriteAsset CoinSpriteAssetTMP;
    [Tooltip("Nombre del sprite dentro del TMP Sprite Asset (si se deja vacío se usará el índice).")]
    public string CoinSpriteName = "";
    [Tooltip("Índice del sprite dentro del TMP Sprite Asset (usado si CoinSpriteName está vacío).")]
    public int CoinSpriteIndex = 0;

    private TowerBehaviour currentTower;
    private PlayerStats playerStats;

    private Button branchAButton;
    private Button branchBButton;

    // CanvasGroups para atenuar / bloquear interacción de ramas
    private CanvasGroup branchACanvasGroup;
    private CanvasGroup branchBCanvasGroup;

    // Inicialización perezosa para evitar race conditions cuando el panel se activa y ShowForTower
    // se llama antes de que Unity ejecute Start() en este componente.
    private bool isInitialized = false;

    void Start()
    {
        // Ejecutar la inicialización normal y mantener el panel oculto por defecto.
        EnsureInitialized();
        Hide();
    }

    private void EnsureCanvasGroup(ref CanvasGroup cg, RectTransform container)
    {
        if (container == null) return;
        cg = container.GetComponent<CanvasGroup>();
        if (cg == null) cg = container.gameObject.AddComponent<CanvasGroup>();
    }

    // Inicialización segura para ejecutar desde Start o desde ShowForTower si Start no ha corrido.
    private void EnsureInitialized()
    {
        if (isInitialized) return;
        isInitialized = true;

        // Localizar PlayerStats (puede no existir en Start)
        playerStats = playerStats ?? FindObjectOfType<PlayerStats>();

        // Auto-asignar contenedores si no están asignados y existen hijos con esos nombres
        if (BranchAContainer == null)
        {
            Transform t = transform.Find("BranchAContainer");
            if (t != null) BranchAContainer = t as RectTransform;
        }

        if (BranchBContainer == null)
        {
            Transform t = transform.Find("BranchBContainer");
            if (t != null) BranchBContainer = t as RectTransform;
        }

        // Asegurar CanvasGroup en contenedores (para bloquear/atenuar ramas)
        EnsureCanvasGroup(ref branchACanvasGroup, BranchAContainer);
        EnsureCanvasGroup(ref branchBCanvasGroup, BranchBContainer);

        // Logs de validación para depuración rápida
        if (BranchAContainer == null)
            Debug.LogWarning("TowerUpgradeUI: BranchAContainer no asignado (asigna el RectTransform en el Inspector).");
        else
            Debug.Log($"TowerUpgradeUI: BranchAContainer asignado -> {BranchAContainer.name}");

        if (BranchBContainer == null)
            Debug.LogWarning("TowerUpgradeUI: BranchBContainer no asignado (asigna el RectTransform en el Inspector).");
        else
            Debug.Log($"TowerUpgradeUI: BranchBContainer asignado -> {BranchBContainer.name}");

        if (UpgradeButtonPrefab == null)
            Debug.LogError("TowerUpgradeUI: UpgradeButtonPrefab no asignado (arrastra el prefab desde Assets).");
        else
            Debug.Log($"TowerUpgradeUI: UpgradeButtonPrefab asignado -> {UpgradeButtonPrefab.name}");
    }

    public void ShowForTower(TowerBehaviour tower)
    {
        if (tower == null) return;

        // Asegurar inicialización aunque Start() no haya corrido aún
        EnsureInitialized();

        // Reintentar localizar PlayerStats por si Start se ejecutó antes de su creación
        playerStats = playerStats ?? FindObjectOfType<PlayerStats>();

        currentTower = tower;
        ClearButtons();

        if (tower.UpgradePath == null)
        {
            Debug.LogWarning("TowerUpgradeUI: UpgradePath no asignado en la torre.");
            gameObject.SetActive(false);
            return;
        }

        // Asegurar que el GameObject del panel y su Canvas padre estén activos antes de crear controles.
        // Esto evita problemas donde la UI no mide correctamente elementos hijos si el canvas estaba inactivo.
        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        var parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && !parentCanvas.gameObject.activeInHierarchy)
            parentCanvas.gameObject.SetActive(true);

        // Asegurar que los contenedores estén activos para que LayoutGroup los mida correctamente
        if (BranchAContainer != null) BranchAContainer.gameObject.SetActive(true);
        if (BranchBContainer != null) BranchBContainer.gameObject.SetActive(true);

        // Forzar actualización del Canvas/Layouts antes de instanciar botones
        Canvas.ForceUpdateCanvases();
        if (parentCanvas != null)
        {
            var parentRect = parentCanvas.GetComponent<RectTransform>();
            if (parentRect != null) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }

        // Crear un solo botón por rama (ahora en contexto de Canvas activo)
        CreateBranchButton(tower, tower.UpgradePath.BranchA, BranchAContainer, isBranchA: true);
        CreateBranchButton(tower, tower.UpgradePath.BranchB, BranchBContainer, isBranchA: false);

        // Forzar actualización tras creación y aplicar estados
        Canvas.ForceUpdateCanvases();
        RebuildAndRefreshImmediate();
    }

    // Actualiza layout y estado inmediatamente (sin esperar frames)
    private void RebuildAndRefreshImmediate()
    {
        Canvas.ForceUpdateCanvases();

        if (BranchAContainer != null) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(BranchAContainer);
        if (BranchBContainer != null) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(BranchBContainer);

        // Asegurar CanvasGroup de nuevo (por si se añadieron/desasignaron dinámicamente)
        EnsureCanvasGroup(ref branchACanvasGroup, BranchAContainer);
        EnsureCanvasGroup(ref branchBCanvasGroup, BranchBContainer);

        UpdateBranchLocking();
        RefreshButtonsInteractable();

        // Diagnóstico detallado: estado de raycast/interactividad de cada botón y padres
        LogButtonRaycastState(branchAButton, "A");
        LogButtonRaycastState(branchBButton, "B");

        // Diagnóstico global: EventSystem y GraphicRaycaster en Canvas padre
        var es = FindObjectOfType<EventSystem>();
        Debug.Log($"TowerUpgradeUI: EventSystem presente = {es != null}");
        var parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            var gr = parentCanvas.GetComponent<GraphicRaycaster>();
            Debug.Log($"TowerUpgradeUI: Canvas padre '{parentCanvas.name}' GraphicRaycaster = {gr != null}");
        }
        else
        {
            Debug.Log("TowerUpgradeUI: No se encontró Canvas padre para TowerUpgradeUI (botones deben estar dentro de un Canvas).");
        }
    }

    // Espera un frame para dejar que el Canvas y Layout se estabilicen, luego refresca el estado
    // Mantengo la coroutine para compatibilidad si en algún flujo se desea esperar un frame.
    private IEnumerator RebuildAndRefresh()
    {
        yield return null; // espera un frame

        RebuildAndRefreshImmediate();
    }

    private void CreateBranchButton(TowerBehaviour tower, TowerUpgrade[] branchUpgrades, RectTransform parent, bool isBranchA)
    {
        // Protección si parent o prefab no existen
        if (parent == null)
        {
            Debug.LogWarning("TowerUpgradeUI: Parent del contenedor es null. Asegúrate de asignar BranchAContainer/BranchBContainer.");
            return;
        }
        if (UpgradeButtonPrefab == null)
        {
            Debug.LogWarning("TowerUpgradeUI: UpgradeButtonPrefab no asignado. No se puede crear botones.");
            return;
        }

        // Determinar siguiente índice disponible en la rama según el estado actual
        int nextIndex = GetNextUpgradeIndex(tower, isBranchA);

        // Instanciar y asegurar transform correcto en canvas
        Button btn = Instantiate(UpgradeButtonPrefab, parent);
        btn.transform.SetParent(parent, false);
        btn.transform.localScale = Vector3.one;
        btn.gameObject.SetActive(true);

        // Gamefeel opcional para botones de upgrade (normalmente sin wobble por escala grande)
        if (addUIButtonFeedback)
        {
            var feedback = btn.GetComponent<GameFeel.UIButtonFeedback>();
            if (feedback == null) feedback = btn.gameObject.AddComponent<GameFeel.UIButtonFeedback>();
            feedback.SetHoverWobbleEnabled(enableUpgradeButtonWobble);
            feedback.SetClickPunchEnabled(enableUpgradeButtonClickPunch);
        }

        TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (label == null)
        {
            Debug.LogWarning("TowerUpgradeUI: El prefab de upgrade no tiene TextMeshProUGUI hijo.");
        }
        // No modificar wrap / alineación / overflow del TMP: respetar el prefab al 100%.
        // Solo se cambia .text y (si aplica) .spriteAsset más abajo.

        btn.onClick.RemoveAllListeners();

        if (nextIndex < 0 || branchUpgrades == null || nextIndex >= branchUpgrades.Length || branchUpgrades[nextIndex] == null)
        {
            // Rama completa o no definida
            if (label != null) label.text = "Max\n-";
            btn.interactable = false;

            // Añadir/actualizar componente de datos con null para claridad
            var emptyData = btn.gameObject.GetComponent<UpgradeButtonData>();
            if (emptyData == null) emptyData = btn.gameObject.AddComponent<UpgradeButtonData>();
            emptyData.Upgrade = null;

            Debug.Log($"TowerUpgradeUI: Botón {(isBranchA ? "A" : "B")} creado como MAX en {parent.name}");
        }
        else
        {
            var upgrade = branchUpgrades[nextIndex];

            // Si el ResultingState no está asignado, inferir y avisar (asigna el asset en editor)
            if (upgrade.ResultingState == TowerBehaviour.UpgradeState.None)
            {
                var inferred = isBranchA
                    ? (nextIndex == 0 ? TowerBehaviour.UpgradeState.A1 : TowerBehaviour.UpgradeState.A2)
                    : (nextIndex == 0 ? TowerBehaviour.UpgradeState.B1 : TowerBehaviour.UpgradeState.B2);

                Debug.LogWarning($"TowerUpgradeUI: El upgrade '{upgrade.UpgradeName}' tiene ResultingState=None. Se inferirá '{inferred}'. Es recomendable asignarlo en el asset.");

                // Asignar para permitir pruebas; en editor marcamos el asset como sucio para guardar cambios
                upgrade.ResultingState = inferred;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(upgrade);
#endif
            }

            // Usar TMP sprite inline si está asignado, fallback a "$"
            if (label != null)
            {
                if (CoinSpriteAssetTMP != null)
                {
                    label.spriteAsset = CoinSpriteAssetTMP;
                    string spriteTag = !string.IsNullOrEmpty(CoinSpriteName)
                        ? $"<sprite name=\"{CoinSpriteName}\">"
                        : $"<sprite index={CoinSpriteIndex}>";

                    label.text = $"{upgrade.UpgradeName}\n{upgrade.Cost}\n{spriteTag}";
                }
                else
                {
                    // Tres líneas: nombre | precio | símbolo (igual que con sprite).
                    label.text = $"{upgrade.UpgradeName}\n{upgrade.Cost}\n$";
                }
            }

            // Guardar referencia directa al upgrade en el botón (evita parseos ambiguos)
            var data = btn.gameObject.GetComponent<UpgradeButtonData>();
            if (data == null) data = btn.gameObject.AddComponent<UpgradeButtonData>();
            data.Upgrade = upgrade;

            btn.onClick.AddListener(() => TryPurchaseUpgrade(upgrade));
            Debug.Log($"TowerUpgradeUI: Instanciado botón {(isBranchA ? "A" : "B")} -> '{label?.text}' en '{parent.name}'");
        }

        if (isBranchA)
            branchAButton = btn;
        else
            branchBButton = btn;
    }

    /// <summary>
    /// Devuelve el índice del siguiente upgrade en la rama o -1 si no hay más.
    /// A: 0->A1,1->A2 ; B: 0->B1,1->B2
    /// </summary>
    private int GetNextUpgradeIndex(TowerBehaviour tower, bool isBranchA)
    {
        if (tower == null) return -1;

        switch (tower.CurrentUpgradeState)
        {
            case TowerBehaviour.UpgradeState.None:
                return 0; // primer nivel de ambas ramas disponible
            case TowerBehaviour.UpgradeState.A1:
                return isBranchA ? 1 : -1; // A can move to A2; B locked
            case TowerBehaviour.UpgradeState.A2:
                return -1; // A done
            case TowerBehaviour.UpgradeState.B1:
                return isBranchA ? -1 : 1; // B can move to B2; A locked
            case TowerBehaviour.UpgradeState.B2:
                return -1; // B done
            default:
                return -1;
        }
    }

    private void TryPurchaseUpgrade(TowerUpgrade upgrade)
    {
        if (currentTower == null || upgrade == null) return;

        if (!currentTower.CanApplyUpgrade(upgrade))
        {
            Debug.LogWarning("TowerUpgradeUI: No se puede aplicar esta mejora en el estado actual.");
            return;
        }

        if (playerStats == null)
        {
            Debug.LogError("TowerUpgradeUI: No se encontró PlayerStats en la escena.");
            return;
        }

        if (playerStats.GetMoney() < upgrade.Cost)
        {
            Debug.Log("TowerUpgradeUI: No tienes suficiente dinero para comprar esta mejora.");
            return;
        }

        // Cobrar y aplicar
        playerStats.AddMoney(-upgrade.Cost);
        currentTower.ApplyUpgrade(upgrade);
        TutorialManager.Instance?.OnTowerUpgraded();

        // Refrescar UI principal y de upgrades
        var uiManager = FindObjectOfType<TowerUIManager>();
        uiManager?.ShowTowerInfo(currentTower);

        ShowForTower(currentTower); // reconstruye botones según nuevo estado
    }

    /// <summary>
    /// Actualiza la interactividad de los botones (coincide con el estado y dinero)
    /// </summary>
    private void RefreshButtonsInteractable()
    {
        if (currentTower == null)
        {
            Debug.Log("TowerUpgradeUI: currentTower null en RefreshButtonsInteractable.");
            return;
        }

        // Mostrar estado actual de la torre para depuración
        Debug.Log($"TowerUpgradeUI: currentTower.CurrentUpgradeState = {currentTower.CurrentUpgradeState}");

        // Reintentar localizar PlayerStats si no existía en Start
        playerStats = playerStats ?? FindObjectOfType<PlayerStats>();
        int playerMoney = playerStats != null ? playerStats.GetMoney() : -1;
        bool ignoreMoneyCheck = playerStats == null;

        // Branch A
        if (branchAButton != null)
        {
            var upgrade = GetButtonUpgrade(branchAButton);
            bool hasUpgrade = upgrade != null;
            bool canApply = hasUpgrade && currentTower.CanApplyUpgrade(upgrade);
            bool hasMoney = hasUpgrade && (ignoreMoneyCheck || playerMoney >= upgrade.Cost);
            bool otherBranchLocked = currentTower.CurrentUpgradeState == TowerBehaviour.UpgradeState.B1
                                     || currentTower.CurrentUpgradeState == TowerBehaviour.UpgradeState.B2;

            bool can = hasUpgrade && canApply && hasMoney && !otherBranchLocked;

            branchAButton.interactable = can;

            Debug.Log($"TowerUpgradeUI: BranchA -> hasUpgrade={hasUpgrade}, upgradeResultState={(upgrade != null ? upgrade.ResultingState.ToString() : "-")}, canApply={canApply}, playerMoney={playerMoney}, cost={(upgrade != null ? upgrade.Cost.ToString() : "-")}, hasMoney={hasMoney}, otherBranchLocked={otherBranchLocked} => interactable={can}");
        }

        // Branch B
        if (branchBButton != null)
        {
            var upgrade = GetButtonUpgrade(branchBButton);
            bool hasUpgrade = upgrade != null;
            bool canApply = hasUpgrade && currentTower.CanApplyUpgrade(upgrade);
            bool hasMoney = hasUpgrade && (ignoreMoneyCheck || playerMoney >= upgrade.Cost);
            bool otherBranchLocked = currentTower.CurrentUpgradeState == TowerBehaviour.UpgradeState.A1
                                     || currentTower.CurrentUpgradeState == TowerBehaviour.UpgradeState.A2;

            bool can = hasUpgrade && canApply && hasMoney && !otherBranchLocked;

            branchBButton.interactable = can;

            Debug.Log($"TowerUpgradeUI: BranchB -> hasUpgrade={hasUpgrade}, upgradeResultState={(upgrade != null ? upgrade.ResultingState.ToString() : "-")}, canApply={canApply}, playerMoney={playerMoney}, cost={(upgrade != null ? upgrade.Cost.ToString() : "-")}, hasMoney={hasMoney}, otherBranchLocked={otherBranchLocked} => interactable={can}");
        }
    }

    // Extrae el upgrade a partir del botón: usa el componente UpgradeButtonData como referencia directa
    private TowerUpgrade GetButtonUpgrade(Button btn)
    {
        if (btn == null) return null;

        var data = btn.gameObject.GetComponent<UpgradeButtonData>();
        if (data != null && data.Upgrade != null) return data.Upgrade;

        // Fallback al método anterior por compatibilidad (parseo del texto)
        TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (label == null) return null;

        string[] lines = label.text.Split('\n');
        if (lines.Length < 2) return null;
        string costStr = lines[1].Trim();

        // El costStr puede contener etiquetas <sprite ...>. Eliminarlas.
        costStr = RemoveTmpSpriteTags(costStr);

        // Eliminar '$' y cualquier carácter no numérico
        string digits = new string(costStr.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digits)) return null;
        int cost;
        if (!int.TryParse(digits, out cost)) return null;

        if (currentTower == null || currentTower.UpgradePath == null) return null;

        foreach (var u in currentTower.UpgradePath.BranchA)
            if (u != null && u.Cost == cost) return u;
        foreach (var u in currentTower.UpgradePath.BranchB)
            if (u != null && u.Cost == cost) return u;

        return null;
    }

    // Elimina etiquetas <sprite ...> presentes en un texto TMP
    private string RemoveTmpSpriteTags(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        int start = s.IndexOf("<sprite");
        while (start >= 0)
        {
            int end = s.IndexOf('>', start);
            if (end < 0) break;
            s = s.Remove(start, end - start + 1);
            start = s.IndexOf("<sprite");
        }
        return s;
    }

    private void ClearButtons()
    {
        if (branchAButton != null)
        {
            Destroy(branchAButton.gameObject);
            branchAButton = null;
        }

        if (branchBButton != null)
        {
            Destroy(branchBButton.gameObject);
            branchBButton = null;
        }
    }

    private void UpdateBranchLocking()
    {
        if (currentTower == null) return;

        bool lockA = currentTower.CurrentUpgradeState == TowerBehaviour.UpgradeState.B1 || currentTower.CurrentUpgradeState == TowerBehaviour.UpgradeState.B2;
        bool lockB = currentTower.CurrentUpgradeState == TowerBehaviour.UpgradeState.A1 || currentTower.CurrentUpgradeState == TowerBehaviour.UpgradeState.A2;

        if (branchACanvasGroup != null)
        {
            branchACanvasGroup.interactable = !lockA;
            branchACanvasGroup.alpha = lockA ? 0.5f : 1f;
        }

        if (branchBCanvasGroup != null)
        {
            branchBCanvasGroup.interactable = !lockB;
            branchBCanvasGroup.alpha = lockB ? 0.5f : 1f;
        }
    }

    private void LogButtonRaycastState(Button btn, string id)
    {
        if (btn == null)
        {
            Debug.Log($"TowerUpgradeUI: Button {id} is null");
            return;
        }
        var img = btn.GetComponent<Image>();
        var rc = btn.GetComponent<RectTransform>();
        Debug.Log($"TowerUpgradeUI: Button {id} interactable={btn.interactable}, image={(img!=null)}, rect={rc?.rect.size}");
    }

    // Resto del código omitido por brevedad si existiera... (mantengo públicas las funciones public/)
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}