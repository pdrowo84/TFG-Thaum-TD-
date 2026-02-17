using UnityEngine;
using System.Collections;
using TMPro;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private GameObject GameOverPanel; // Asigna el panel en el inspector
    [SerializeField] private GameObject GameplayUIPanel; // (Opcional) Panel principal de la UI del juego
    [SerializeField] private TextMeshProUGUI MoneyDisplayText;
    [SerializeField] private TextMeshProUGUI LifeDisplayText; // Añade esto en el inspector
    [SerializeField] private int StartingMoney;
    [SerializeField] private int StartingLife = 20; // Vida inicial

    private int CurrentMoney;
    private int CurrentLife;

    private void Start()
    {
        CurrentMoney = StartingMoney;
        CurrentLife = StartingLife;
        MoneyDisplayText.SetText($"$ {StartingMoney}");
        LifeDisplayText.SetText($"<3 {StartingLife}");
    }

    public void AddMoney(int MoneyToAdd)
    {
        CurrentMoney += MoneyToAdd;
        MoneyDisplayText.SetText($"$ {CurrentMoney}");
    }

    public int GetMoney()
    {
        return CurrentMoney;
    }

    public void LoseLife(int amount)
    {
        CurrentLife -= amount;
        if (CurrentLife < 0) CurrentLife = 0;
        LifeDisplayText.SetText($"<3 {CurrentLife}");
        if (CurrentLife == 0)
        {
            Debug.Log("¡Has perdido!");
            GameLoopManager.PauseGame();

            // Muestra el panel de Game Over
            if (GameOverPanel != null)
                GameOverPanel.SetActive(true);

            // Desactiva la UI de juego (botones, paneles, etc.)
            if (GameplayUIPanel != null)
                GameplayUIPanel.SetActive(false);
        }
    }

    public int GetLife()
    {
        return CurrentLife;
    }
}
