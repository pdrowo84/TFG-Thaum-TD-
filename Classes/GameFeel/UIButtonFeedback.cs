using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameFeel
{
    [DisallowMultipleComponent]
    public class UIButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Hover Wobble")]
        [SerializeField] private RectTransform target;
        [SerializeField] private float wobbleDuration = 0.25f;
        [SerializeField] private float wobbleRotationDegrees = 6f;
        [SerializeField] private float wobbleScale = 1.06f;
        [SerializeField] private float wobbleFrequency = 14f;

        [Header("Click Punch")]
        [SerializeField] private float clickPunchScale = 1.08f;
        [SerializeField] private float clickPunchDuration = 0.12f;

        [Header("SFX")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private float hoverClipVolume = 0.6f;
        [SerializeField] private AudioClip clickClip;
        [SerializeField] private float clickClipVolume = 0.75f;

        private Coroutine hoverRoutine;
        private Coroutine clickRoutine;
        private Vector3 baseScale;
        private Quaternion baseRotation;
        private bool hasBase;
        private bool isHovered;

        private void Awake()
        {
            if (target == null)
                target = transform as RectTransform;

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            CacheBaseIfNeeded();

            if (hoverRoutine != null) StopCoroutine(hoverRoutine);
            hoverRoutine = StartCoroutine(HoverWobble());

            if (audioSource != null && hoverClip != null)
                audioSource.PlayOneShot(hoverClip, Mathf.Clamp01(hoverClipVolume));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            CacheBaseIfNeeded();

            if (hoverRoutine != null) StopCoroutine(hoverRoutine);
            hoverRoutine = StartCoroutine(ReturnToBase(0.10f));
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            CacheBaseIfNeeded();

            if (clickRoutine != null) StopCoroutine(clickRoutine);
            clickRoutine = StartCoroutine(ClickPunch());

            if (audioSource != null && clickClip != null)
                audioSource.PlayOneShot(clickClip, Mathf.Clamp01(clickClipVolume));
        }

        private void CacheBaseIfNeeded()
        {
            if (hasBase || target == null) return;
            baseScale = target.localScale;
            baseRotation = target.localRotation;
            hasBase = true;
        }

        private IEnumerator HoverWobble()
        {
            float dur = Mathf.Max(0.01f, wobbleDuration);
            float rot = Mathf.Max(0f, wobbleRotationDegrees);
            float freq = Mathf.Max(0.01f, wobbleFrequency);
            float scaleMul = Mathf.Max(0.01f, wobbleScale);

            float t = 0f;
            while (t < dur && isHovered)
            {
                float k = t / dur;
                float ease = 1f - Mathf.Pow(1f - k, 3f);

                float s = Mathf.Sin(Time.unscaledTime * freq) * rot;
                target.localRotation = baseRotation * Quaternion.Euler(0f, 0f, s * ease);
                target.localScale = Vector3.Lerp(baseScale, baseScale * scaleMul, ease);

                t += Time.unscaledDeltaTime;
                yield return null;
            }

            while (isHovered)
            {
                float s = Mathf.Sin(Time.unscaledTime * freq) * rot;
                target.localRotation = baseRotation * Quaternion.Euler(0f, 0f, s);
                target.localScale = Vector3.Lerp(target.localScale, baseScale * scaleMul, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 30f));
                yield return null;
            }
        }

        private IEnumerator ClickPunch()
        {
            float dur = Mathf.Max(0.01f, clickPunchDuration);
            float mul = Mathf.Max(0.01f, clickPunchScale);

            float half = dur * 0.5f;
            float t = 0f;

            while (t < half)
            {
                float k = t / half;
                float ease = 1f - Mathf.Pow(1f - k, 3f);
                target.localScale = Vector3.Lerp(target.localScale, baseScale * mul, ease);
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                float k = t / half;
                float ease = 1f - Mathf.Pow(1f - k, 3f);
                Vector3 targetScale = isHovered ? baseScale * Mathf.Max(0.01f, wobbleScale) : baseScale;
                target.localScale = Vector3.Lerp(target.localScale, targetScale, ease);
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            clickRoutine = null;
        }

        private IEnumerator ReturnToBase(float duration)
        {
            float dur = Mathf.Max(0.01f, duration);
            float t = 0f;

            Quaternion startRot = target.localRotation;
            Vector3 startScale = target.localScale;

            while (t < dur)
            {
                float k = t / dur;
                float ease = 1f - Mathf.Pow(1f - k, 3f);
                target.localRotation = Quaternion.Slerp(startRot, baseRotation, ease);
                target.localScale = Vector3.Lerp(startScale, baseScale, ease);
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            target.localRotation = baseRotation;
            target.localScale = baseScale;
            hoverRoutine = null;
        }
    }
}

