using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TowerPlacing : MonoBehaviour
{
    [SerializeField] private LayerMask PlacementCheckMask;
    [SerializeField] private LayerMask PlacementCollideMask;

    [SerializeField] private Camera PlayerCamera;
    [SerializeField] private PlayerStats PlayerStatisctics;

    private GameObject CurrentPlacingTower;

    // Tracking de h�roes colocados
    public static bool HeroPlaced = false;
    private static Button heroPlacementButton;

    // Referencia al TowerSelection para notificar cuando se coloca una torre
    private TowerSelection towerSelection;

    void Start()
    {
        // Resetear el estado del h�roe al iniciar (por si viene de un reset)
        HeroPlaced = false;
        heroPlacementButton = null;

        // Buscar TowerSelection
        towerSelection = FindObjectOfType<TowerSelection>();
    }

    void Update()
    {
        if (CurrentPlacingTower != null)
        {
            Ray camray = PlayerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit HitInfo;
            bool hit = Physics.Raycast(camray, out HitInfo, 100f, PlacementCollideMask);

            if (hit)
            {
                CurrentPlacingTower.transform.position = HitInfo.point;
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                Destroy(CurrentPlacingTower);
                CurrentPlacingTower = null;
                return;
            }

            // Solo intenta colocar si el raycast ha detectado algo
            if (Input.GetMouseButtonDown(0) && hit && HitInfo.collider != null)
            {
                // Evita colocar la torre si el rat�n est� sobre el UI
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                if (!HitInfo.collider.gameObject.CompareTag("NoPlace"))
                {
                    BoxCollider TowerCollider = CurrentPlacingTower.GetComponent<BoxCollider>();
                    TowerCollider.isTrigger = true;

                    Vector3 BoxCenter = CurrentPlacingTower.gameObject.transform.position + TowerCollider.center;
                    Vector3 HalfExtents = TowerCollider.size / 2;
                    if (!Physics.CheckBox(BoxCenter, HalfExtents, Quaternion.identity, PlacementCheckMask, QueryTriggerInteraction.Ignore))
                    {
                        TowerBehaviour CurrentToweBehaviour = CurrentPlacingTower.GetComponent<TowerBehaviour>();
                        GameLoopManager.TowersInGame.Add(CurrentToweBehaviour);

                        PlayerStatisctics.AddMoney(-CurrentToweBehaviour.SummonCost);

                        // Verificar si es un h�roe y marcar como colocado
                        HeroeTornado heroComponent = CurrentPlacingTower.GetComponent<HeroeTornado>();
                        if (heroComponent != null)
                        {
                            HeroPlaced = true;
                            DisableHeroPlacementButton();
                            Debug.Log("TowerPlacing: H�roe colocado. Bot�n desactivado.");
                        }

                        // Reactiva el da�o y los colliders de da�o
                        var flameThrower = CurrentPlacingTower.GetComponent<FlameThrowerDamage>();
                        if (flameThrower != null)
                        {
                            flameThrower.enabled = true;
                        }
                        var colliders = CurrentPlacingTower.GetComponentsInChildren<Collider>(true);
                        foreach (var col in colliders)
                        {
                            if (col.isTrigger)
                                col.enabled = true;
                        }

                        TowerCollider.isTrigger = false;
                        if (towerSelection != null) { towerSelection.DeselectTower(); }
                        TutorialManager.Instance?.OnTowerPlaced();    // Llamada a la funci�n OnTowerPlaced del TutorialManager
                        CurrentPlacingTower = null;
                    }
                }
            }
        }
    }

    public bool IsPlacingTower()
    {
        return CurrentPlacingTower != null;
    }

    public void SetTowerToPlace(GameObject tower)
    {
        // **NUEVO: Deseleccionar torre actual antes de empezar a colocar una nueva**
        if (towerSelection != null)
        {
            towerSelection.DeselectTower();
        }

        // Si ya hay una torre en previsualizaci�n, elim�nala
        if (CurrentPlacingTower != null)
        {
            Destroy(CurrentPlacingTower);
            CurrentPlacingTower = null;
        }

        // Verificar si es un h�roe y si ya se coloc� uno
        HeroeTornado heroComponent = tower.GetComponent<HeroeTornado>();
        if (heroComponent != null && HeroPlaced)
        {
            Debug.LogWarning("TowerPlacing: �Solo puedes colocar un h�roe por partida!");
            return;
        }

        int TowerSummonCost = tower.GetComponent<TowerBehaviour>().SummonCost;

        if (PlayerStatisctics.GetMoney() >= TowerSummonCost)
        {
            CurrentPlacingTower = Instantiate(tower, Vector3.zero, Quaternion.identity);

            // Desactiva el da�o y los colliders de da�o en todos los hijos
            var flameThrower = CurrentPlacingTower.GetComponent<FlameThrowerDamage>();
            if (flameThrower != null)
            {
                flameThrower.enabled = false;
            }

            // Desactiva todos los colliders en hijos que est�n en modo trigger (usualmente los de da�o)
            var colliders = CurrentPlacingTower.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                if (col.isTrigger)
                    col.enabled = false;
            }
        }
        else
        {
            Debug.Log("TowerPlacing: No tienes suficiente dinero para colocar esta torre.");
        }
    }

    // M�todo para registrar el bot�n del h�roe (llamado desde el bot�n o desde c�digo)
    public static void RegisterHeroButton(Button button)
    {
        heroPlacementButton = button;

        // Si el h�roe ya est� colocado, desactivar el bot�n inmediatamente
        if (HeroPlaced && heroPlacementButton != null)
        {
            heroPlacementButton.interactable = false;
            UpdateButtonVisuals(heroPlacementButton, false);
        }
    }

    // Desactiva el bot�n de colocaci�n del h�roe
    private static void DisableHeroPlacementButton()
    {
        if (heroPlacementButton != null)
        {
            heroPlacementButton.interactable = false;
            UpdateButtonVisuals(heroPlacementButton, false);
        }
    }

    // Actualiza los visuales del bot�n (texto, color, etc.)
    private static void UpdateButtonVisuals(Button button, bool isAvailable)
    {
        Text buttonText = button.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            if (isAvailable)
            {
                buttonText.text = "H�roe";
            }
            else
            {
                buttonText.text = "Colocado";
            }
        }

        // Opcional: cambiar el color del bot�n
        ColorBlock colors = button.colors;
        if (!isAvailable)
        {
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Gris semi-transparente
        }
        button.colors = colors;
    }

    // M�todo para resetear el estado (llamar desde GameLoopManager.ResetGame)
    public static void ResetHeroPlacement()
    {
        HeroPlaced = false;
        if (heroPlacementButton != null)
        {
            heroPlacementButton.interactable = true;
            UpdateButtonVisuals(heroPlacementButton, true);
        }
        Debug.Log("TowerPlacing: Estado del h�roe reseteado.");
    }
}