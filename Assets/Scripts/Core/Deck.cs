// Assets/Scripts/Core/Deck.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class Deck<T>
{
    private readonly List<T> drawPile = new();
    private readonly List<T> discardPile = new();
    private readonly System.Random rng = new();

    public int DrawCount => drawPile.Count;
    public int DiscardCount => discardPile.Count;

    public Deck(IEnumerable<T> initial)
    {
        drawPile.AddRange(initial);
        Shuffle(drawPile);
    }

    public void AddToDiscard(T item) => discardPile.Add(item);

    public void AddToDraw(T item)
    {
        drawPile.Add(item);
        Shuffle(drawPile);
    }

    public T Draw()
    {
        if (drawPile.Count == 0) Reshuffle();
        if (drawPile.Count == 0) return default; // empty
        var top = drawPile[^1];
        drawPile.RemoveAt(drawPile.Count - 1);
        return top;
    }

    public void Reshuffle()
    {
        if (discardPile.Count == 0) return;
        drawPile.AddRange(discardPile);
        discardPile.Clear();
        Shuffle(drawPile);
    }

    private void Shuffle(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public List<T> SnapshotAllCards()
    {
        var all = new List<T>();
        all.AddRange(drawPile);
        all.AddRange(discardPile);
        return all;
    }
}
