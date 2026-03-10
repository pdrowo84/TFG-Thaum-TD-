using UnityEngine;
using UnityEngine.EventSystems;

public class TowerPlacing : MonoBehaviour
{
    [SerializeField] private LayerMask PlacementCheckMask;
    [SerializeField] private LayerMask PlacementCollideMask;

    [SerializeField] private Camera PlayerCamera;
    [SerializeField] private PlayerStats PlayerStatisctics;

    private GameObject CurrentPlacingTower;



    void Start()
    {

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
                // Evita colocar la torre si el ratón está sobre el UI
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

                        // Reactiva el daño y los colliders de daño
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
                        CurrentPlacingTower = null;
                    }
                }
            }
        }
    }

    public void SetTowerToPlace(GameObject tower)
    {
        // Si ya hay una torre en previsualización, elimínala
        if (CurrentPlacingTower != null)
        {
            Destroy(CurrentPlacingTower);
            CurrentPlacingTower = null;
        }

        // Bloquea la colocación si ya hay una torre héroe
        if (tower.GetComponent<HeroTower>() && HeroTower.HeroPlaced)
        {
            Debug.LogWarning("¡Solo puedes colocar una torre héroe por partida!");
            return;
        }

        int TowerSummonCost = tower.GetComponent<TowerBehaviour>().SummonCost;

        if (PlayerStatisctics.GetMoney() >= TowerSummonCost)
        {
            CurrentPlacingTower = Instantiate(tower, Vector3.zero, Quaternion.identity);

            // Desactiva el daño y los colliders de daño en todos los hijos
            var flameThrower = CurrentPlacingTower.GetComponent<FlameThrowerDamage>();
            if (flameThrower != null)
            {
                flameThrower.enabled = false;
            }

            // Desactiva todos los colliders en hijos que estén en modo trigger (usualmente los de daño)
            var colliders = CurrentPlacingTower.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                if (col.isTrigger)
                    col.enabled = false;
            }
        }
        else
        {
            Debug.Log("No tienes suficiente dinero para colocar esta torre.");
        }
    }

    public class HeroTower : TowerBehaviour
    {
        public static bool HeroPlaced = false;

        void Awake()
        {
            if (HeroPlaced)
            {
                Debug.LogWarning("¡Ya hay una torre héroe en la partida!");
                Destroy(gameObject);
                return;
            }
            HeroPlaced = true;
        }

        void OnDestroy()
        {
            HeroPlaced = false;
        }
    }
}
