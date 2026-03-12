using UnityEngine;

/// <summary>
/// Maneja la selección de torres mediante clicks en el Game View
/// </summary>
public class TowerSelection : MonoBehaviour
{
    [Header("Selection Settings")]
    public LayerMask TowerLayer; // Layer de las torres (configura "Tower" layer)
    public Color OutlineColor = Color.black;
    [Range(0.01f, 0.3f)]
    public float OutlineWidth = 0.05f; // Grosor del outline (5% más grande)

    private Camera mainCamera;
    private TowerBehaviour selectedTower;
    private TowerOutline currentOutline;

    // Referencia al UI Manager
    private TowerUIManager uiManager;

    void Start()
    {
        mainCamera = Camera.main;
        uiManager = FindObjectOfType<TowerUIManager>();

        if (mainCamera == null)
        {
            Debug.LogError("TowerSelection: No se encontró la cámara principal!");
        }

        if (uiManager == null)
        {
            Debug.LogWarning("TowerSelection: No se encontró TowerUIManager en la escena.");
        }
    }

    void Update()
    {
        // Detectar click izquierdo
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectTower();
        }

        // Desseleccionar con click derecho o ESC
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            DeselectTower();
        }
    }

    private void TrySelectTower()
    {
        // Evitar seleccionar si estamos sobre UI
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Raycast hacia todas las capas primero
        if (Physics.Raycast(ray, out hit, 1000f))
        {
            // Buscar el componente TowerBehaviour en el objeto golpeado o en sus padres
            TowerBehaviour tower = hit.collider.GetComponentInParent<TowerBehaviour>();

            if (tower != null)
            {
                // Verificar que está en la capa correcta
                if (((1 << tower.gameObject.layer) & TowerLayer) != 0)
                {
                    SelectTower(tower);
                    return;
                }
            }
        }

        // Click en vacío, desseleccionar
        DeselectTower();
    }

    private void SelectTower(TowerBehaviour tower)
    {
        // Si ya hay una torre seleccionada diferente, desseleccionarla primero
        if (selectedTower != null && selectedTower != tower)
        {
            DeselectTower();
        }

        // Si clickeamos la misma torre, no hacer nada
        if (selectedTower == tower)
            return;

        selectedTower = tower;

        // Añadir outline visual (el color se actualizará desde TowerUIManager)
        currentOutline = tower.gameObject.GetComponent<TowerOutline>();
        if (currentOutline == null)
        {
            currentOutline = tower.gameObject.AddComponent<TowerOutline>();
        }
        currentOutline.EnableOutline(OutlineColor, OutlineWidth);

        // Notificar al UI Manager (esto actualizará el color del outline)
        if (uiManager != null)
        {
            uiManager.ShowTowerInfo(tower);
        }

        Debug.Log($"TowerSelection: Torre seleccionada - {tower.name}");
    }

    /// <summary>
    /// Actualiza el color del outline de la torre actualmente seleccionada
    /// (llamado desde TowerUIManager)
    /// </summary>
    public void UpdateOutlineColor(Color newColor)
    {
        if (currentOutline != null)
        {
            currentOutline.UpdateColor(newColor);
            Debug.Log($"TowerSelection: Color del outline actualizado.");
        }
    }

    public void DeselectTower()
    {
        if (selectedTower != null)
        {
            // Remover outline
            if (currentOutline != null)
            {
                currentOutline.DisableOutline();
                // Destruir el componente para limpiar completamente
                Destroy(currentOutline);
            }

            selectedTower = null;
            currentOutline = null;

            // Ocultar UI
            if (uiManager != null)
            {
                uiManager.HideTowerInfo();
            }

            Debug.Log("TowerSelection: Torre desseleccionada");
        }
    }

    public TowerBehaviour GetSelectedTower()
    {
        return selectedTower;
    }

    void OnDrawGizmos()
    {
        // Visualizar el rayo del mouse en el editor (debug)
        if (mainCamera != null && Application.isPlaying)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(ray.origin, ray.direction * 100f);
        }
    }
}