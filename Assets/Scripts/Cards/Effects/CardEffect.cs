// Assets/Scripts/CardEffects.cs
using UnityEngine;

public abstract class CardEffect : ScriptableObject
{
    public abstract void Apply(GameContext ctx);
}

