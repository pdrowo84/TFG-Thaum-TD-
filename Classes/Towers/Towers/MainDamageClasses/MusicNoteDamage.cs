using UnityEngine;
using static ElementDamageType;

/// <summary>
/// Igual que StandardDamage, pero alterna entre varios prefabs de proyectil (ej. 4 notas distintas).
/// </summary>
public class MusicNoteDamage : MonoBehaviour, IDamageMethod
{
    [SerializeField] private Transform FirePoint;

    [Header("Projectiles (alternate)")]
    [Tooltip("Prefabs de bala. Se instancian en orden aleatorio (shuffle-bag) para evitar patrones repetitivos.")]
    [SerializeField] private GameObject[] BulletPrefabs;

    private float FireRate;
    private float Delay;
    private int[] shuffleBag;
    private int bagCursor = 0;

    private void EnsureShuffleBag()
    {
        if (BulletPrefabs == null || BulletPrefabs.Length == 0)
        {
            shuffleBag = null;
            bagCursor = 0;
            return;
        }

        // Crear o recrear bag si cambia el tamaño
        if (shuffleBag == null || shuffleBag.Length != BulletPrefabs.Length)
            shuffleBag = new int[BulletPrefabs.Length];

        // Rellenar 0..N-1 y barajar
        for (int i = 0; i < shuffleBag.Length; i++) shuffleBag[i] = i;
        for (int i = shuffleBag.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffleBag[i], shuffleBag[j]) = (shuffleBag[j], shuffleBag[i]);
        }

        bagCursor = 0;
    }

    public void Init(float Damage, float FireRate)
    {
        this.FireRate = FireRate;
        Delay = 1f / FireRate;
        EnsureShuffleBag();
    }

    public void DamageTick(Enemy Target)
    {
        if (!Target) return;

        if (Delay > 0f)
        {
            Delay -= Time.deltaTime;
            return;
        }

        if (FirePoint != null && BulletPrefabs != null && BulletPrefabs.Length > 0)
        {
            if (shuffleBag == null || shuffleBag.Length != BulletPrefabs.Length || bagCursor >= shuffleBag.Length)
                EnsureShuffleBag();

            int idx = shuffleBag != null && shuffleBag.Length > 0 ? shuffleBag[bagCursor] : 0;
            bagCursor++;

            GameObject prefabToUse = BulletPrefabs[Mathf.Clamp(idx, 0, BulletPrefabs.Length - 1)];

            if (prefabToUse == null)
            {
                Delay = 1f / FireRate;
                return;
            }

            GameObject bullet = GameObject.Instantiate(prefabToUse, FirePoint.position, Quaternion.identity);

            StandardBullet bulletScript = bullet.GetComponent<StandardBullet>();
            if (bulletScript != null)
            {
                var tower = GetComponent<TowerBehaviour>();
                float towerPen = tower != null ? tower.ArmorPenetration : 0f;
                bulletScript.Init(Target, tower.Damage, tower.DamageElement, towerPen);
            }
        }

        Delay = 1f / FireRate;
    }
}

