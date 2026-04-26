using UnityEngine;
using System.Collections;
using TMPro;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private GameObject BasicTowerPrefab;

    [SerializeField] private GameObject GameOverPanel; // Asigna el panel en el inspector
    [SerializeField] private GameObject GameplayUIPanel; // (Opcional) Panel principal de la UI del juego
    [SerializeField] private TextMeshProUGUI MoneyDisplayText; // Añade esto en el inspector
    [SerializeField] private TextMeshProUGUI LifeDisplayText; // Añade esto en el inspector

    [SerializeField] private GameObject[] TowerPrefabs;
    [SerializeField] private TextMeshProUGUI[] TowerCostTexts;

    [SerializeField] private int StartingMoney; // Dinero inicial
    [SerializeField] private int StartingLife; // Vida inicial

    [Header("Price Icon (opcional) - TextMeshPro Sprite Asset")]
    [SerializeField] private TMP_SpriteAsset CoinSpriteAssetTMP;
    [SerializeField] private string CoinSpriteName = "";
    [SerializeField] private int CoinSpriteIndex = 0;

    private int CurrentMoney;
    private int CurrentLife;
    
    private void Start()
    {
        CurrentMoney = StartingMoney;
        CurrentLife = StartingLife;
        MoneyDisplayText.SetText($"  {StartingMoney}");
        LifeDisplayText.SetText($"   {StartingLife}");

        // Mostrar el coste de cada torre
        for (int i = 0; i < TowerPrefabs.Length && i < TowerCostTexts.Length; i++)
        {
            var tower = TowerPrefabs[i].GetComponent<TowerBehaviour>();
            if (tower != null && TowerCostTexts[i] != null)
            {
                if (CoinSpriteAssetTMP != null) TowerCostTexts[i].spriteAsset = CoinSpriteAssetTMP;
                TowerCostTexts[i].SetText($"{FormatPrice(tower.SummonCost)}");
            }
        }
    }

    public void AddMoney(int MoneyToAdd)
    {
        CurrentMoney += MoneyToAdd;
        MoneyDisplayText.SetText($"  {CurrentMoney}");
        TutorialManager.Instance?.OnMoneyChanged(CurrentMoney);
    }

    public int GetMoney()
    {
        return CurrentMoney;
    }

    public void LoseLife(int amount)
    {
        CurrentLife -= amount;
        if (CurrentLife < 0) CurrentLife = 0;
        LifeDisplayText.SetText($"   {CurrentLife}");

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

    private string FormatPrice(int amount)
    {
        if (CoinSpriteAssetTMP != null)
        {
            string spriteTag = !string.IsNullOrEmpty(CoinSpriteName)
                ? $"<sprite name=\"{CoinSpriteName}\">"
                : $"<sprite index={CoinSpriteIndex}>";

            return $"{amount} {spriteTag}";
        }

        return $"{amount}$";
    }
}
