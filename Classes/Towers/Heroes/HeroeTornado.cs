using UnityEngine;

public class HeroeTornado : MonoBehaviour
{
    public float BuffRadius = 5f;
    public float DamageBuff = 2f; // multiplicador de daño (2 = x2)

    // Elemento requerido para recibir el buff (configurable desde el Inspector)
    public ElementDamageType.ElementType RequiredElement = ElementDamageType.ElementType.Wind;

    void Update()
    {
        TowerBehaviour myTower = GetComponent<TowerBehaviour>();
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
}