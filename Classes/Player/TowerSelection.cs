using UnityEngine;

/// <summary>
/// Maneja la selecci�n de torres mediante clicks en el Game View
/// </summary>
public class TowerSelection : MonoBehaviour
{
    [Header("Selection Settings")]
    public LayerMask TowerLayer; // Layer de las torres (configura "Tower" layer)
    public Color OutlineColor = Color.black;
    [Range(0.01f, 0.3f)]
    public float OutlineWidth = 0.05f; // Grosor del outline (5% m�s grande)

    private Camera mainCamera;
    private TowerBehaviour selectedTower;
    private TowerOutline currentOutline;
    private TowerPlacing towerPlacing;

    // Referencia al UI Manager
    private TowerUIManager uiManager;

    void Start()
    {
        mainCamera = Camera.main;
        uiManager = FindObjectOfType<TowerUIManager>();
        towerPlacing = FindObjectOfType<TowerPlacing>();

        if (mainCamera == null)
        {
            Debug.LogError("TowerSelection: No se encontr� la c�mara principal!");
        }

        if (uiManager == null)
        {
            Debug.LogWarning("TowerSelection: No se encontr� TowerUIManager en la escena.");
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
        // Mientras se est� previsualizando/colocando una torre, no permitir selecci�n de torres colocadas.
        if (towerPlacing != null && towerPlacing.IsPlacingTower())
            return;

        // Evitar seleccionar si estamos sobre UI
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Usamos RaycastAll y seleccionamos el primer collider v�lido (por distancia).
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);
        if (hits == null || hits.Length == 0)
        {
            DeselectTower();
            return;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        bool found = false;
        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];

            // Intentar resolver la torre desde el collider alcanzado
            TowerBehaviour tower = hit.collider.GetComponentInParent<TowerBehaviour>();
            if (tower != null)
            {
                // Verificar que est� en la capa permitida
                if (((1 << tower.gameObject.layer) & TowerLayer) != 0)
                {
                    // Evitar seleccionar torres en previsualizaci�n/no colocadas a�n.
                    // Solo permitimos seleccionar torres registradas en el loop de juego.
                    if (GameLoopManager.TowersInGame == null || !GameLoopManager.TowersInGame.Contains(tower))
                        continue;

                    SelectTower(tower);
                    found = true;
                    break;
                }
            }
            // Si no es torre, seguimos buscando en los siguientes hits (esto evita bloqueos por objetos intermedios)
        }

        if (!found)
        {
#if UNITY_EDITOR
            // Depuraci�n: lista de los primeros 6 objetos que recibe el rayo
            string info = "TowerSelection: Hits (no torre v�lida encontrada): ";
            int count = Mathf.Min(hits.Length, 6);
            for (int j = 0; j < count; j++)
            {
                info += $"[{j}] {hits[j].collider.gameObject.name}(layer={hits[j].collider.gameObject.layer}) ";
            }
            Debug.Log(info);
#endif
            DeselectTower();
        }
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

        // A�adir outline visual (el color se actualizar� desde TowerUIManager)
        currentOutline = tower.gameObject.GetComponent<TowerOutline>();
        if (currentOutline == null)
        {
            currentOutline = tower.gameObject.AddComponent<TowerOutline>();
        }
        currentOutline.EnableOutline(OutlineColor, OutlineWidth);

        // Notificar al UI Manager (esto actualizar� el color del outline)
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