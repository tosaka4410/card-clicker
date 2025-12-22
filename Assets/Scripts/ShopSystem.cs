// Assets/Scripts/ShopSystem.cs
using System.Collections.Generic;
using UnityEngine;

public class ShopSystem : MonoBehaviour
{
    [Header("Shop Pool")]
    public List<CardData> cardPool = new();

    [Header("Config")]
    public int itemCount = 3;
    public int rerollCost = 50;
    public int baseCardCost = 100;

    private List<ShopItem> currentItems = new();

    public List<ShopItem> GenerateLineup()
    {
        currentItems.Clear();

        for (int i = 0; i < itemCount; i++)
        {
            var card = cardPool[Random.Range(0, cardPool.Count)];
            currentItems.Add(new ShopItem
            {
                card = card,
                cost = baseCardCost
            });
        }

        return currentItems;
    }

    public List<ShopItem> Reroll()
    {
        return GenerateLineup();
    }
}
