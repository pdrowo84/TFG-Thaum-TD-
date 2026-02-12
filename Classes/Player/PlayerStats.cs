using UnityEngine;
using System.Collections;
using TMPro;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI MoneyDisplayText;
    [SerializeField] private int StartingMoney;
    private int CurrentMoney;
    private void Start()
    {
        CurrentMoney = StartingMoney;
        MoneyDisplayText.SetText($"$ {StartingMoney}");
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

     

}
