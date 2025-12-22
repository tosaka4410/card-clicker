// Assets/Scripts/DeckSystem.cs
using System.Collections.Generic;
using UnityEngine;

public class DeckSystem
{
    private readonly List<CardData> drawPile = new();
    private readonly List<CardData> discardPile = new();

    public void Init(IEnumerable<CardData> initialDeck)
    {
        drawPile.Clear();
        discardPile.Clear();
        drawPile.AddRange(initialDeck);
        Shuffle(drawPile);
    }

    public CardData DrawOne()
    {
        if (drawPile.Count == 0)
        {
            if (discardPile.Count == 0) return null;
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile);
        }
        var c = drawPile[0];
        drawPile.RemoveAt(0);
        return c;
    }

    public void Discard(CardData card) => discardPile.Add(card);

    private void Shuffle(List<CardData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
