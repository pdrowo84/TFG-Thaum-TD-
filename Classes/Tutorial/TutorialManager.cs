using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla el flujo completo del tutorial de Thaum TD.
/// Escucha eventos del juego y avanza los pasos según condiciones.
/// 
/// SETUP: Coloca este script en un GameObject vacío llamado "TutorialManager" en tu escena de juego.
/// Asígnale la referencia a TutorialDialogueUI desde el Inspector.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Referencias")]
    public TutorialDialogueUI DialogueUI;

    // ─── Estado interno ──────────────────────────────────────────────
    private int currentStep = 0;
    private bool waitingForAction = false;
    private bool tutorialActive = true;

    // Seguimiento de qué enemigos ya han sido comentados
    private HashSet<int> commentedEnemyIDs = new HashSet<int>();

    // ─── IDs de enemigos (ajusta según tus prefabs) ──────────────────
    // Estos son los IDs que tienes configurados en tus EnemySummonData assets.
    // Revisa tus assets y ajusta si alguno no coincide.
    private const int ID_ACOLITO  = 1;
    private const int ID_FUGAZ    = 2;
    private const int ID_CASCARON = 3;
    private const int ID_MOTA     = 4;
    private const int ID_VELADO   = 5;
    private const int ID_IMPURO   = 9;
    private const int ID_SOLKAR   = 13;
    private const int ID_LUNETH = 14;

    // ─── Pasos del tutorial ──────────────────────────────────────────
    private enum TutorialStep
    {
        // Ronda 1
        Intro,                  // Contexto inicial de Thatha
        PlaceWindTower,         // Pide colocar Arcano Zéfiro
        ExplainUpgrade,         // Explica que las torres se pueden mejorar
        ExplainLives,           // "Si se te pasa alguno no importa mientras las defensas no lleguen a 0"
        ExplainQKey,            // "Con Q deseleccionas la previsualización"

        // Ronda 2
        Wave2Upgrades,          // Mejoras exclusivas explicadas en profundidad
        Wave2AfterUpgrade,      // Paso que se activa tras hacer la primera mejora en ronda 2

        // Enemigos por tipo (se activan cuando aparecen por primera vez)
        // Se gestionan dinámicamente en OnEnemySpawned, no como pasos lineales

        // Bosses
        SolkarIntro,
        LunethIntro,

        Done
    }

    private TutorialStep step => (TutorialStep)currentStep;

    // ─── Unity ───────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Esperar un frame para que todo esté inicializado
        StartCoroutine(StartTutorialAfterFrame());
    }

    private IEnumerator StartTutorialAfterFrame()
    {
        yield return null;
        ShowStep(TutorialStep.Intro);
    }

    // ─── Mostrar un paso ─────────────────────────────────────────────
    private void ShowStep(TutorialStep targetStep)
    {
        currentStep = (int)targetStep;
        waitingForAction = false;

        switch (targetStep)
        {
            case TutorialStep.Intro:
                GameLoopManager.PauseGame();   // <-- pausa
                DialogueUI.Show(
                    "¡Eh! Soy Thatha. Los acólitos avanzan hacia nuestras defensas y tú eres quien las organiza. ¡Manos a la obra!",
                    onContinue: () => ShowStep(TutorialStep.PlaceWindTower)
                );
                break;

            case TutorialStep.PlaceWindTower:
                GameLoopManager.ResumeGame();
                waitingForAction = true;
                DialogueUI.Show(
                    "Selecciona el Arcano Zéfiro del panel y haz clic en el terreno para colocarlo.",
                    onContinue: null
                );
                break;

            case TutorialStep.Wave2Upgrades:
                GameLoopManager.PauseGame();
                DialogueUI.Show(
                    "Ahora ya tienes algo de oro. Selecciona una torre y mejórala. " +
                    "Cada torre tiene dos ramas exclusivas, ¡elige bien porque no hay vuelta atrás!",
                    onContinue: () => { GameLoopManager.ResumeGame(); waitingForAction = true; }
                );
                break;

            case TutorialStep.ExplainLives:
                DialogueUI.Show(
                    "Si algún enemigo pasa, pierdes vida en las defensas. Si llegan a cero, perdemos. ¡No dejes que ocurra!",
                    onContinue: () => ShowStep(TutorialStep.ExplainQKey)
                );
                break;

            case TutorialStep.ExplainQKey:
                DialogueUI.Show(
                    "Ah, y pulsa Q para cancelar la previsualización de una torre.",
                    onContinue: () => GameLoopManager.ResumeGame()
                );
                break;

            case TutorialStep.SolkarIntro:
                DialogueUI.Show(
                    "¡Eso es Solkar! Es un boss. Tiene inmunidad al Fuego, así que el Arcano Brasas no le afectará. " +
                    "Usa Viento o Tierra. Y cuidado con su estela...",
                    onContinue: () => GameLoopManager.ResumeGame()
                );
                break;

            case TutorialStep.LunethIntro:
                DialogueUI.Show(
                    "Y ahora Luneth. Inmunidad al Agua, el Arcano Mareal es inútil contra él. " +
                    "Coordina bien tus Arcanos, ¡esto es lo último!",
                    onContinue: () => GameLoopManager.ResumeGame()
                );
                break;
        }
    }
        

    // ─── Hooks llamados desde otros scripts ──────────────────────────

    /// <summary>
    /// Llamar desde TowerPlacing.cs cuando se coloca una torre con éxito.
    /// </summary>
    public void OnTowerPlaced()
    {
        if (!tutorialActive) return;

        if (step == TutorialStep.PlaceWindTower && waitingForAction)
        {
            waitingForAction = false;
            ShowStep(TutorialStep.ExplainLives); // antes iba a ExplainUpgrade
        }
    }

    /// <summary>
    /// Llamar desde TowerUpgradeUI.cs después de TryPurchaseUpgrade con éxito.
    /// </summary>
    public void OnTowerUpgraded()
    {
        if (!tutorialActive) return;

        if (step == TutorialStep.ExplainUpgrade && waitingForAction)
        {
            waitingForAction = false;
            ShowStep(TutorialStep.ExplainLives);
        }
        else if (step == TutorialStep.Wave2Upgrades && waitingForAction)
        {
            waitingForAction = false;
            ShowStep(TutorialStep.Wave2AfterUpgrade);
        }
    }

    /// <summary>
    /// Llamar desde GameLoopManager.cs en StartNextWave(), pasando el número de oleada (1-based, antes de incrementar).
    /// </summary>
    public void OnWaveStarted(int waveNumber)
    {
        
        
    }

    /// <summary>
    /// Llamar desde EntitySummoner.cs justo después de instanciar un enemigo, pasando su ID.
    /// </summary>
    public void OnEnemySpawned(int enemyID)
    {
        if (!tutorialActive) return;
        if (commentedEnemyIDs.Contains(enemyID)) return;

        commentedEnemyIDs.Add(enemyID);

        // Bosses: paso dedicado con más peso
        if (enemyID == ID_SOLKAR)
        {
            GameLoopManager.PauseGame();
            ShowStep(TutorialStep.SolkarIntro);
            return;
        }
        if (enemyID == ID_LUNETH)
        {
            GameLoopManager.PauseGame();
            ShowStep(TutorialStep.LunethIntro);
            return;
        }

        // Enemigos normales
        string dialogue = GetEnemyCommentary(enemyID);
        if (dialogue == null) return;

        GameLoopManager.PauseGame();
        DialogueUI.Show(dialogue, onContinue: () => GameLoopManager.ResumeGame());
    }

    // ─── Comentarios de enemigos ─────────────────────────────────────
    private string GetEnemyCommentary(int id)
    {
        switch (id)
        {
            case ID_ACOLITO:
                return null; // Los acólitos se presentan en el intro general, sin comentario aquí

            case ID_FUGAZ:
                return "¿Ves esos que van disparados? Son los Fugaces. Rápidos como el rayo y difíciles de enfocar. " +
                       "Una torre de Viento bien colocada puede ralentizarlos, pero no te confíes.";

            case ID_CASCARON:
                return "Mira esas cosas grandes y lentas que se acercan. Son los Cascarones. " +
                       "Tienen mucha armadura, así que necesitarás daño sostenido para bajarlos. " +
                       "El Arcano Pétreo hace bien su trabajo con ellos.";

            case ID_MOTA:
                return "¡Wow, wow! ¿Ves esas cositas tan rápidas y pequeñas de ahí? Les llamamos Motas. " +
                       "Son enanas pero van en grupos y se hacen bola. Si se te cuelan unas pocas, te hacen daño de verdad.";

            case ID_VELADO:
                return "Hmm... ¿notas algo raro? Los Velados tienen una resistencia especial al daño elemental. " +
                       "Prueba a combinar tipos de torres para encontrar su punto débil.";

            case ID_IMPURO:
                return "Esos de ahí son los Impuros. Lo que los hace peligrosos es que te dañan más las defensas " +
                       "si consiguen llegar al final. Prioriza eliminarlos antes que a los demás.";

            case ID_SOLKAR:
            case ID_LUNETH:
                return null; // Los bosses tienen su propio paso dedicado en ShowStep

            default:
                return null;
        }
    }

    // ─── Utilidades ──────────────────────────────────────────────────
    private IEnumerator DelayedStep(TutorialStep targetStep, float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowStep(targetStep);
    }

    /// <summary>
    /// Para los bosses, llamar directamente con su ID desde OnEnemySpawned si prefieres
    /// que el TutorialManager los gestione como pasos formales.
    /// Esto se llama automáticamente desde OnEnemySpawned.
    /// </summary>
    public void TriggerBossIntro(int bossID)
    {
        if (!tutorialActive) return;
        if (bossID == ID_SOLKAR && !commentedEnemyIDs.Contains(ID_SOLKAR))
        {
            commentedEnemyIDs.Add(ID_SOLKAR);
            ShowStep(TutorialStep.SolkarIntro);
        }
        else if (bossID == ID_LUNETH && !commentedEnemyIDs.Contains(ID_LUNETH))
        {
            commentedEnemyIDs.Add(ID_LUNETH);
            ShowStep(TutorialStep.LunethIntro);
        }
    }

    private bool upgradeHintGiven = false;

    public void OnMoneyChanged(int currentMoney)
    {
        if (!tutorialActive) return;
        if (upgradeHintGiven) return;
        if (step != TutorialStep.ExplainQKey) return; // solo después de los pasos de ronda 1

        if (currentMoney >= 100)
        {
            upgradeHintGiven = true;
            ShowStep(TutorialStep.Wave2Upgrades);
        }
    }
}
