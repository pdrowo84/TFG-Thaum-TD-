using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel de diálogo de Thatha con efecto typewriter.
/// 
/// SETUP en Unity:
/// 1. Crea un Panel UI en tu Canvas llamado "TutorialPanel"
/// 2. Dentro pon: Image de Thatha (ThathaImage), TextMeshProUGUI (DialogueText), Button (ContinueButton)
/// 3. Asigna las referencias en el Inspector de este script
/// 4. Asigna este componente al TutorialManager
/// </summary>
public class TutorialDialogueUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject TutorialPanel;
    public TextMeshProUGUI DialogueText;
    public Button ContinueButton;
    public TextMeshProUGUI ContinueButtonText;

    [Header("Sprite de Thatha")]
    public Image ThathaImage;
    // Opcional: si quieres que Thatha tenga expresiones distintas
    // public Sprite ThathaDefault;
    // public Sprite ThathaSurprised;

    [Header("Configuración typewriter")]
    [Tooltip("Caracteres por segundo")]
    public float TypewriterSpeed = 40f;

    // Estado
    private Coroutine typewriterCoroutine;
    private Action onContinueCallback;
    private bool isTyping = false;
    private bool isFree = true; // true si no hay diálogo activo

    // ─── Unity ───────────────────────────────────────────────────────
    private void Start()
    {
        if (TutorialPanel != null)
            TutorialPanel.SetActive(false);

        if (ContinueButton != null)
            ContinueButton.onClick.AddListener(OnContinuePressed);

        if (ContinueButton != null)
            ContinueButton.gameObject.SetActive(false);
    }

    // ─── API pública ─────────────────────────────────────────────────

    /// <summary>
    /// Muestra un diálogo de Thatha. Si onContinue es null, no aparece el botón
    /// y el panel queda abierto hasta que otro Show() lo reemplace o se llame a Hide().
    /// Si onContinue tiene valor, aparece el botón "Continuar".
    /// </summary>
    public void Show(string text, Action onContinue)
    {
        isFree = false;
        onContinueCallback = onContinue;

        if (TutorialPanel != null)
            TutorialPanel.SetActive(true);

        // Mostrar botón solo si hay callback
        if (ContinueButton != null)
            ContinueButton.gameObject.SetActive(onContinue != null);

        // Parar typewriter anterior si lo hubiera
        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        typewriterCoroutine = StartCoroutine(TypewriterRoutine(text));
    }

    /// <summary>
    /// Solo muestra el diálogo si no hay uno activo ya (para comentarios de enemigos
    /// que no deben interrumpir pasos críticos del tutorial).
    /// </summary>
    public void ShowIfFree(string text)
    {
        if (!isFree) return;
        Show(text, null);
        // Auto-ocultar después de unos segundos si no hay botón
        StartCoroutine(AutoHideAfter(5f));
    }

    public void Hide()
    {
        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        if (TutorialPanel != null)
            TutorialPanel.SetActive(false);

        isFree = true;
        isTyping = false;
    }

    // ─── Botón continuar ─────────────────────────────────────────────
    private void OnContinuePressed()
    {
        if (isTyping)
        {
            // Primer clic: completa el texto al instante
            SkipTypewriter();
            return;
        }

        // Segundo clic: ejecuta el callback y cierra
        Hide();
        Action callback = onContinueCallback;
        onContinueCallback = null;
        callback?.Invoke();
    }

    // ─── Typewriter ──────────────────────────────────────────────────
    private IEnumerator TypewriterRoutine(string fullText)
    {
        isTyping = true;
        DialogueText.text = "";

        float delay = 1f / TypewriterSpeed;

        foreach (char c in fullText)
        {
            DialogueText.text += c;
            yield return new WaitForSecondsRealtime(delay); // RealTime para que funcione aunque el juego esté pausado
        }

        isTyping = false;

        // Si no hay callback (diálogo informativo sin botón), el panel queda visible
        // ShowIfFree lanzará AutoHide aparte
    }

    private void SkipTypewriter()
    {
        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        // Necesitamos saber el texto completo — lo recuperamos del último Show
        // Para esto guardamos el fullText
        if (DialogueText != null && _lastFullText != null)
            DialogueText.text = _lastFullText;

        isTyping = false;
    }

    // Guardamos el texto completo para el skip
    private string _lastFullText;

    // Override de Show para guardar el texto
    private IEnumerator TypewriterRoutineInternal(string fullText)
    {
        _lastFullText = fullText;
        yield return TypewriterRoutine(fullText);
    }

    private IEnumerator AutoHideAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        // Solo ocultar si sigue siendo el mismo diálogo libre (no fue reemplazado)
        if (isFree == false && onContinueCallback == null)
            Hide();
    }
}
