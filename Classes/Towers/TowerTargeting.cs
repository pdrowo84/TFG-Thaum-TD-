using System.Collections.Generic;
using UnityEngine;

public class TowerTargeting : MonoBehaviour
{
    public enum TargetType
    {
        First,    // Primer enemigo en el camino (más avanzado)
        Last,     // Último enemigo en el camino (más atrasado)
        Close,    // Enemigo más cercano a la torre
        Strong,   // Enemigo con más vida
        Weak      // Enemigo con menos vida
    }

    // **NUEVO: Enum para filtro por elemento**
    public enum ElementFilter
    {
        Any,      // Cualquier enemigo (sin filtro)
        Fire,     // Solo enemigos de fuego
        Water,    // Solo enemigos de agua
        Wind,     // Solo enemigos de viento
        Rock      // Solo enemigos de roca/tierra
    }

    /// <summary>
    /// Obtiene el objetivo según el modo de targeting y filtro de elemento
    /// </summary>
    public static Enemy GetTarget(TowerBehaviour tower, TargetType targetType)
    {
        if (EntitySummoner.EnemiesInGame == null || EntitySummoner.EnemiesInGame.Count == 0)
            return null;

        // Filtrar enemigos dentro del rango
        List<Enemy> enemiesInRange = new List<Enemy>();

        foreach (Enemy enemy in EntitySummoner.EnemiesInGame)
        {
            if (enemy == null || enemy.IsDead) continue;

            float distance = Vector3.Distance(tower.transform.position, enemy.transform.position);
            if (distance <= tower.Range)
            {
                enemiesInRange.Add(enemy);
            }
        }

        if (enemiesInRange.Count == 0)
            return null;

        // **NUEVO: Aplicar filtro de elemento**
        List<Enemy> filteredEnemies = ApplyElementFilter(enemiesInRange, tower.ElementPriorityFilter);

        // Si no hay enemigos del elemento filtrado, buscar cualquier enemigo
        if (filteredEnemies.Count == 0)
        {
            filteredEnemies = enemiesInRange;
        }

        // Seleccionar objetivo según el modo
        switch (targetType)
        {
            case TargetType.First:
                return GetFirstEnemy(filteredEnemies);

            case TargetType.Last:
                return GetLastEnemy(filteredEnemies);

            case TargetType.Close:
                return GetClosestEnemy(tower.transform.position, filteredEnemies);

            case TargetType.Strong:
                return GetStrongestEnemy(filteredEnemies);

            case TargetType.Weak:
                return GetWeakestEnemy(filteredEnemies);

            default:
                return GetFirstEnemy(filteredEnemies);
        }
    }

    /// <summary>
    /// Filtra enemigos por elemento
    /// </summary>
    private static List<Enemy> ApplyElementFilter(List<Enemy> enemies, ElementFilter filter)
    {
        if (filter == ElementFilter.Any)
            return enemies;

        List<Enemy> filtered = new List<Enemy>();
        ElementDamageType.ElementType targetElement = ConvertFilterToElementType(filter);

        foreach (Enemy enemy in enemies)
        {
            // Si el enemigo tiene el campo EnemyElementType, usarlo
            if (enemy.EnemyElementType == targetElement)
            {
                filtered.Add(enemy);
            }
        }

        return filtered;
    }

    /// <summary>
    /// Convierte el filtro a ElementType
    /// </summary>
    private static ElementDamageType.ElementType ConvertFilterToElementType(ElementFilter filter)
    {
        switch (filter)
        {
            case ElementFilter.Fire:
                return ElementDamageType.ElementType.Fuego;
            case ElementFilter.Water:
                return ElementDamageType.ElementType.Agua;
            case ElementFilter.Wind:
                return ElementDamageType.ElementType.Viento;
            case ElementFilter.Rock:
                return ElementDamageType.ElementType.Roca;
            default:
                return ElementDamageType.ElementType.Ninguno;
        }
    }

    /// <summary>
    /// Primer enemigo (más avanzado en el camino)
    /// </summary>
    private static Enemy GetFirstEnemy(List<Enemy> enemies)
    {
        Enemy firstEnemy = null;
        int maxNodeIndex = -1;

        foreach (Enemy enemy in enemies)
        {
            if (enemy.NodeIndex > maxNodeIndex)
            {
                maxNodeIndex = enemy.NodeIndex;
                firstEnemy = enemy;
            }
        }

        return firstEnemy;
    }

    /// <summary>
    /// Último enemigo (más atrasado en el camino)
    /// </summary>
    private static Enemy GetLastEnemy(List<Enemy> enemies)
    {
        Enemy lastEnemy = null;
        int minNodeIndex = int.MaxValue;

        foreach (Enemy enemy in enemies)
        {
            if (enemy.NodeIndex < minNodeIndex)
            {
                minNodeIndex = enemy.NodeIndex;
                lastEnemy = enemy;
            }
        }

        return lastEnemy;
    }

    /// <summary>
    /// Enemigo más cercano a la torre
    /// </summary>
    private static Enemy GetClosestEnemy(Vector3 towerPosition, List<Enemy> enemies)
    {
        Enemy closestEnemy = null;
        float minDistance = float.MaxValue;

        foreach (Enemy enemy in enemies)
        {
            float distance = Vector3.Distance(towerPosition, enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }

    /// <summary>
    /// Enemigo con más vida (más resistente)
    /// </summary>
    private static Enemy GetStrongestEnemy(List<Enemy> enemies)
    {
        Enemy strongestEnemy = null;
        float maxHealth = 0f;

        foreach (Enemy enemy in enemies)
        {
            if (enemy.Health > maxHealth)
            {
                maxHealth = enemy.Health;
                strongestEnemy = enemy;
            }
        }

        return strongestEnemy;
    }

    /// <summary>
    /// Enemigo con menos vida (más débil)
    /// </summary>
    private static Enemy GetWeakestEnemy(List<Enemy> enemies)
    {
        Enemy weakestEnemy = null;
        float minHealth = float.MaxValue;

        foreach (Enemy enemy in enemies)
        {
            if (enemy.Health < minHealth)
            {
                minHealth = enemy.Health;
                weakestEnemy = enemy;
            }
        }

        return weakestEnemy;
    }
}