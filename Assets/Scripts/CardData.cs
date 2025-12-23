// Assets/Scripts/CardData.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card/CardData")]
public class CardData : ScriptableObject
{
    public string cardName = "Strike";

    [TextArea]
    public string description;
    public List<CardEffect> effects = new List<CardEffect>();

    [Header("Visual")]
    public Sprite art;
}
