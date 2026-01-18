using UnityEngine;

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
        if(CurrentPlacingTower != null)
        {
            Ray camray = PlayerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit HitInfo;
            if (Physics.Raycast(camray, out HitInfo, 100f, PlacementCollideMask))
            {
                CurrentPlacingTower.transform.position = HitInfo.point;
            }

            if(Input.GetKeyDown(KeyCode.Q))
            {
                Destroy(CurrentPlacingTower);
                CurrentPlacingTower = null;
                return;
            }

            if (Input.GetMouseButtonDown(0) && HitInfo.collider.gameObject != null)
            {
                if(!HitInfo.collider.gameObject.CompareTag("NoPlace"))
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
