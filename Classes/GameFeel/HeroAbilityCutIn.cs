using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace GameFeel
{
    /// <summary>
    /// Cut-in estilo Persona: el panel no se desliza, sino que "se abre" escalando
    /// en altura (Y) o en anchura (X). El pivot del RectTransform define desde d�nde crece
    /// (ej. centro 0.5,0.5 = se abre hacia arriba y abajo a la vez).
    /// </summary>
    public class HeroAbilityCutIn : MonoBehaviour
    {
        public enum OpenAxis
        {
            Vertical,   // escala Y: de casi 0 ? tama�o final
            Horizontal  // escala X: de casi 0 ? tama�o final
        }

        [Header("Apertura (estilo Persona)")]
        [SerializeField] private RectTransform cutInRoot;
        [Tooltip("Vertical = se abre en altura; Horizontal = se abre en anchura.")]
        [SerializeField] private OpenAxis openAxis = OpenAxis.Vertical;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Timing")]
        [SerializeField] private float enterDuration = 0.14f;
        [SerializeField] private float holdDuration = 0.28f;
        [SerializeField] private float exitDuration = 0.16f;

        [Header("Escala m�nima (evita 0 exacto en layout)")]
        [SerializeField] private float collapsedEpsilon = 0.001f;

        [Header("Optional Slash / imagen")]
        [SerializeField] private Image slashImage;
        [SerializeField] private float slashAlpha = 1f;

        [Header("SFX")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip cutInClip;
        [SerializeField] private float cutInClipVolume = 0.8f;

        [Header("Activation hook")]
        [Tooltip("Coloca aqu� la funci�n que activa la habilidad del h�roe (p.ej. YourHeroScript.ActivateAbility)")]
        public UnityEvent OnActivate;

        private Coroutine routine;
        private Vector3 fullLocalScale;
        private bool fullScaleCached;

        private void Awake()
        {
            if (cutInRoot == null)
                cutInRoot = transform as RectTransform;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        /// <summary>
        /// M�todo p�blico para usar desde un Button.OnClick:
        /// invoca la l�gica de la habilidad (OnActivate) y muestra el cut-in.
        /// </summary>
        public void ActivateAndPlay()
        {
            // Ejecutar la l�gica de la habilidad (conectada desde el Inspector)
            OnActivate?.Invoke();

            // Mostrar el cut-in visual
            Play();
        }

        public void Play()
        {
            Debug.Log("[HeroAbilityCutIn] Play() called. cutInRoot=" + (cutInRoot != null ? cutInRoot.name : "null"));
            if (cutInRoot == null) return;

            CacheFullScaleIfNeeded();

            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(PlayRoutine());
        }

        private void CacheFullScaleIfNeeded()
        {
            if (fullScaleCached) return;

            fullLocalScale = cutInRoot.localScale;
            if (fullLocalScale.x < 1e-4f) fullLocalScale.x = 1f;
            if (fullLocalScale.y < 1e-4f) fullLocalScale.y = 1f;
            if (fullLocalScale.z < 1e-4f) fullLocalScale.z = 1f;

            fullScaleCached = true;
        }

        private Vector3 CollapsedScale()
        {
            float e = Mathf.Max(1e-5f, collapsedEpsilon);
            if (openAxis == OpenAxis.Vertical)
                return new Vector3(fullLocalScale.x, e, fullLocalScale.z);
            return new Vector3(e, fullLocalScale.y, fullLocalScale.z);
        }

        private IEnumerator PlayRoutine()
        {
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            if (slashImage != null)
            {
                var c = slashImage.color;
                slashImage.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(slashAlpha));
            }

            if (audioSource != null && cutInClip != null)
                audioSource.PlayOneShot(cutInClip, Mathf.Clamp01(cutInClipVolume));

            Vector3 from = CollapsedScale();
            cutInRoot.localScale = from;

            yield return ScaleRoutine(from, fullLocalScale, Mathf.Max(0.01f, enterDuration));

            if (holdDuration > 0f)
                yield return new WaitForSecondsRealtime(holdDuration);

            yield return ScaleRoutine(fullLocalScale, from, Mathf.Max(0.01f, exitDuration));

            if (canvasGroup != null) canvasGroup.alpha = 0f;
            routine = null;
        }

        private IEnumerator ScaleRoutine(Vector3 from, Vector3 to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                float k = t / duration;
                // ease-out cubico al abrir, ease-in al cerrar (misma curva, inversa visualmente ok)
                float ease = 1f - Mathf.Pow(1f - k, 3f);
                cutInRoot.localScale = Vector3.LerpUnclamped(from, to, ease);
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            cutInRoot.localScale = to;
        }
    }
}
