using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameFeel
{
    public class DamageFeedback : MonoBehaviour
    {
        [Header("Camera Shake")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float shakeDuration = 0.12f;
        [SerializeField] private float shakeMagnitude = 0.12f;
        [SerializeField] private int shakeFrequency = 24;

        [Header("Screen Flash")]
        [SerializeField] private Image flashImage;
        [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 0.65f);
        [SerializeField] private float flashInDuration = 0.03f;
        [SerializeField] private float flashOutDuration = 0.15f;

        [Header("SFX")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip damageClip;
        [SerializeField] private float damageClipVolume = 0.8f;

        private Coroutine shakeRoutine;
        private Coroutine flashRoutine;
        private Vector3 cameraOriginalLocalPos;
        private bool hasCameraOriginal;

        private void Awake()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (flashImage != null)
            {
                var c = flashImage.color;
                flashImage.color = new Color(c.r, c.g, c.b, 0f);
                flashImage.enabled = true;
            }
        }

        public void Play()
        {
            PlayShake();
            PlayFlash();
            PlaySfx();
        }

        private void PlaySfx()
        {
            if (audioSource == null || damageClip == null) return;
            audioSource.PlayOneShot(damageClip, Mathf.Clamp01(damageClipVolume));
        }

        private void PlayShake()
        {
            if (cameraTransform == null) return;

            if (!hasCameraOriginal)
            {
                cameraOriginalLocalPos = cameraTransform.localPosition;
                hasCameraOriginal = true;
            }

            if (shakeRoutine != null) StopCoroutine(shakeRoutine);
            shakeRoutine = StartCoroutine(ShakeCoroutine());
        }

        private IEnumerator ShakeCoroutine()
        {
            float duration = Mathf.Max(0f, shakeDuration);
            float mag = Mathf.Max(0f, shakeMagnitude);
            int freq = Mathf.Clamp(shakeFrequency, 1, 120);

            float t = 0f;
            float step = 1f / freq;

            while (t < duration)
            {
                float strength = 1f - Mathf.Clamp01(t / duration);
                Vector2 r = Random.insideUnitCircle * mag * strength;
                cameraTransform.localPosition = cameraOriginalLocalPos + new Vector3(r.x, r.y, 0f);

                float wait = Mathf.Min(step, duration - t);
                t += wait;
                yield return new WaitForSecondsRealtime(wait);
            }

            cameraTransform.localPosition = cameraOriginalLocalPos;
            shakeRoutine = null;
        }

        private void PlayFlash()
        {
            if (flashImage == null) return;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashCoroutine());
        }

        private IEnumerator FlashCoroutine()
        {
            float inDur = Mathf.Max(0f, flashInDuration);
            float outDur = Mathf.Max(0f, flashOutDuration);

            Color baseColor = flashColor;

            if (inDur > 0f)
            {
                float t = 0f;
                while (t < inDur)
                {
                    float a = Mathf.Lerp(0f, baseColor.a, t / inDur);
                    flashImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            flashImage.color = baseColor;

            if (outDur > 0f)
            {
                float t = 0f;
                while (t < outDur)
                {
                    float a = Mathf.Lerp(baseColor.a, 0f, t / outDur);
                    flashImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            flashImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
            flashRoutine = null;
        }
    }
}

