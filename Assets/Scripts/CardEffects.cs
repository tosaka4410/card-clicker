// Assets/Scripts/CardEffects.cs
using UnityEngine;

public abstract class CardEffect : ScriptableObject
{
    public abstract void Apply(GameContext ctx);
}

[CreateAssetMenu(menuName = "Card/Effects/AddScore")]
public class AddScoreEffect : CardEffect
{
    public int amount = 50;
    public override void Apply(GameContext ctx) => ctx.AddScore(amount);
}

[CreateAssetMenu(menuName = "Card/Effects/Draw")]
public class DrawEffect : CardEffect
{
    public int amount = 1;
    public override void Apply(GameContext ctx) => ctx.Draw(amount);
}
