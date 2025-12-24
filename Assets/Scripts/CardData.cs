// Assets/Scripts/CardData.cs
using System.Collections.Generic;
using UnityEngine;
public enum CardKind
{
    Cow, Dog
}
[CreateAssetMenu(menuName = "Card/CardData")]
public class CardData : ScriptableObject
{
    public string cardName = "Strike";

    [TextArea]
    public string description;
    public List<CardEffect> effects = new List<CardEffect>();

    [Header("Kind")]
    public CardKind kind; 

    [Header("Visual")]
    public Sprite art;
}
