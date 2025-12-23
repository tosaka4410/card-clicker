// Assets/Scripts/ShopSystem.cs
using System.Collections.Generic;
using UnityEngine;

public class ShopSystem : MonoBehaviour
{
    public List<CardData> cardPool = new();

    public int itemCount = 3;
    public int rerollCost = 50;
    public int baseCardCost = 100;

    private readonly List<ShopItem> currentItems = new();

    public IReadOnlyList<ShopItem> CurrentItems => currentItems;

    public void GenerateLineup()
    {
        currentItems.Clear();
        if (cardPool.Count == 0) return;

        for (int i = 0; i < itemCount; i++)
        {
            var card = cardPool[Random.Range(0, cardPool.Count)];
            currentItems.Add(new ShopItem { card = card, cost = baseCardCost });
        }
    }

    public void Reroll() => GenerateLineup();
}
