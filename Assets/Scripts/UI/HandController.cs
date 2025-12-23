using System;
using System.Collections.Generic;
using UnityEngine;

public class HandController : MonoBehaviour
{
    [SerializeField] private Transform handPanel;
    [SerializeField] private CardView cardViewPrefab;

    public void Render(IReadOnlyList<CardData> hand, Action<CardData> onPlay)
    {
        for (int i = handPanel.childCount - 1; i >= 0; i--)
            Destroy(handPanel.GetChild(i).gameObject);

        foreach (var card in hand)
        {
            var v = Instantiate(cardViewPrefab, handPanel);
            v.Bind(card, () => onPlay?.Invoke(card));
        }
    }
}
