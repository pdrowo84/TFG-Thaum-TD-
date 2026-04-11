using System.Collections;
using System.Reflection;
using UnityEngine;

namespace GameFeel
{
    public class CutInManager : MonoBehaviour
    {
        public static CutInManager Instance { get; private set; }

        [Tooltip("Prefab del panel cut-in (prefab UI que contiene HeroAbilityCutIn en su raíz)")]
        [SerializeField] private GameObject cutInPrefab;

        [Tooltip("Parent UI donde instanciar (Canvas). Si es null se buscará un Canvas activo.")]
        [SerializeField] private Transform uiParent;

        [Tooltip("Tiempo por defecto tras el Play para destruir la instancia (segundos)")]
        [SerializeField] private float destroyDelay = 2f;

        [Header("Comprobaciones previas al CutIn")]
        [Tooltip("Si asignas aquí el componente del héroe (script que implementa IHeroAbility o HeroeTornado), se usará directamente.")]
        [SerializeField] private MonoBehaviour heroAbilityComponent;

        // Nombres heredados (se mantienen para compatibilidad con setups anteriores)
        [Tooltip("Método (bool) en heroAbilityComponent que indica si la habilidad está disponible. Si está vacío, se usará la interfaz o la búsqueda automática.")]
        [SerializeField] private string availabilityMethodName = "";
        [Tooltip("Método (bool) en heroAbilityComponent que indica si el héroe está colocado. Si está vacío se usará la búsqueda automática.")]
        [SerializeField] private string placementMethodName = "";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ShowCutIn(bool blockPointers = false, float destroyAfter = -1f)
        {
            if (cutInPrefab == null)
            {
                Debug.LogWarning("CutInManager.ShowCutIn: cutInPrefab no asignado.");
                return;
            }

            if (uiParent == null)
            {
                var canvas = FindObjectOfType<Canvas>();
                if (canvas != null) uiParent = canvas.transform;
            }

            GameObject inst = Instantiate(cutInPrefab, uiParent);
            inst.transform.SetAsLastSibling();

            var cutIn = inst.GetComponent<HeroAbilityCutIn>();
            if (cutIn == null) cutIn = inst.GetComponentInChildren<HeroAbilityCutIn>(true);

            if (cutIn != null) cutIn.Play();
            else Debug.LogWarning("CutInManager: prefab no contiene HeroAbilityCutIn.");

            float delay = destroyAfter > 0f ? destroyAfter : destroyDelay;
            if (delay > 0f) StartCoroutine(DestroyAfter(inst, delay));
        }

        // Método seguro: solo muestra el cut-in si existe un héroe colocado y su habilidad está lista.
        public void ShowCutInDefault()
        {
            // 1) Si se ha asignado explícitamente el componente del héroe, probar ese primero
            if (heroAbilityComponent != null)
            {
                // Intentar cast directo a HeroeTornado
                var tornadoComp = heroAbilityComponent as HeroeTornado;
                if (tornadoComp != null)
                {
                    if (!tornadoComp.IsPlaced())
                    {
                        Debug.Log("[CutInManager] ShowCutInDefault: héroe Tornado no colocado (heroAbilityComponent). Ignorando.");
                        return;
                    }
                    if (!tornadoComp.IsAbilityAvailable())
                    {
                        Debug.Log("[CutInManager] ShowCutInDefault: habilidad Tornado no disponible (heroAbilityComponent). Ignorando.");
                        return;
                    }
                }
                else
                {
                    // Intentar métodos legacy por nombre (si el componente usa otros nombres)
                    if (!string.IsNullOrEmpty(placementMethodName))
                    {
                        var miPlace = heroAbilityComponent.GetType().GetMethod(placementMethodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (miPlace != null)
                        {
                            var res = miPlace.Invoke(heroAbilityComponent, null);
                            if (res is bool placed && !placed)
                            {
                                Debug.Log("[CutInManager] ShowCutInDefault: héroe no colocado (legacy). Ignorando.");
                                return;
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(availabilityMethodName))
                    {
                        var miAvail = heroAbilityComponent.GetType().GetMethod(availabilityMethodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (miAvail != null)
                        {
                            var res = miAvail.Invoke(heroAbilityComponent, null);
                            if (res is bool avail && !avail)
                            {
                                Debug.Log("[CutInManager] ShowCutInDefault: habilidad no disponible (legacy). Ignorando.");
                                return;
                            }
                        }
                    }
                }
            }
            else
            {
                // 2) Si no hay componente asignado, buscar automáticamente una instancia de HeroeTornado en la escena
                var tornadoHero = FindObjectOfType<HeroeTornado>();
                if (tornadoHero != null)
                {
                    if (!tornadoHero.IsPlaced())
                    {
                        Debug.Log("[CutInManager] ShowCutInDefault: héroe Tornado no colocado. Ignorando.");
                        return;
                    }
                    if (!tornadoHero.IsAbilityAvailable())
                    {
                        Debug.Log("[CutInManager] ShowCutInDefault: habilidad Tornado no disponible. Ignorando.");
                        return;
                    }
                }
                else
                {
                    // No hay héroe Tornado en escena: bloquear por seguridad
                    Debug.Log("[CutInManager] ShowCutInDefault: no se encontró HeroeTornado en escena. Ignorando.");
                    return;
                }
            }

            // Si pasamos las comprobaciones, mostrar el cut-in
            Debug.Log("[CutInManager] ShowCutInDefault: comprobaciones OK. Mostrando cut-in.");
            ShowCutIn(blockPointers: false, destroyAfter: 1.5f);
        }

        private IEnumerator DestroyAfter(GameObject go, float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (go != null) Destroy(go);
        }
    }
}