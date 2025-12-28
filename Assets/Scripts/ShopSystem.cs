// Assets/Scripts/ShopSystem.cs
using System.Collections.Generic;
using UnityEngine;

public class ShopSystem : MonoBehaviour
{
    public List<CardData> cardPool = new();

    public int itemCount = 3;

    [Header("Shop Open Cost")]
    public int baseOpenCost = 80; // ★開く基本コスト
    public float openCostRate = 1.25f; // ★開くたびに上がる倍率（固定なら 1.0）
    public int totalOpenCount = 0; // ★累計で何回開いたか（ラン中持ち越し）

    private readonly List<ShopItem> currentItems = new();
    public IReadOnlyList<ShopItem> CurrentItems => currentItems;

    public int GetOpenCost()
    {
        float raw = baseOpenCost * Mathf.Pow(openCostRate, totalOpenCount);
        return Mathf.CeilToInt(raw);
    }

    public void NotifyOpened()
    {
        totalOpenCount++;
    }

    public void ResetRun()
    {
        totalOpenCount = 0;
        currentItems.Clear();
    }

public void GenerateLineup()
{
    currentItems.Clear();

    if (cardPool == null || cardPool.Count == 0)
        return;

    var candidates = new List<CardData>(cardPool);
    int count = Mathf.Min(itemCount, candidates.Count);

    for (int i = 0; i < count; i++)
    {
        int index = Random.Range(0, candidates.Count);
        var card = candidates[index];

        currentItems.Add(new ShopItem
        {
            card = card
        });

        candidates.RemoveAt(index); // ★重複防止
    }
}

}
