using UnityEngine;
using UnityEngine.EventSystems;

public class TowerPlacing : MonoBehaviour
{
    [SerializeField] private LayerMask PlacementCheckMask;
    [SerializeField] private LayerMask PlacementCollideMask;
    [SerializeField] private Camera PlayerCamera;

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
                        GameLoopManager.TowersInGame.Add(CurrentPlacingTower.GetComponent<TowerBehaviour>());

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
        // Instancia la nueva torre
        CurrentPlacingTower = Instantiate(tower, Vector3.zero, Quaternion.identity);
    }


}
