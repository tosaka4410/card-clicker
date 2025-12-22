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

    public override void Apply(GameContext ctx)
    {
        int v = amount * Mathf.Max(1, ctx.Multiplier);
        Debug.Log($"[Effect] AddScore {amount} x{ctx.Multiplier} => {v}");
        ctx.AddScore(v);
    }
}

[CreateAssetMenu(menuName = "Card/Effects/Draw")]
public class DrawEffect : CardEffect
{
    public int amount = 1;

    public override void Apply(GameContext ctx)
    {
        int v = amount * Mathf.Max(1, ctx.Multiplier);
        Debug.Log($"[Effect] Draw {amount} x{ctx.Multiplier} => {v}");
        ctx.Draw(v);
    }
}

[CreateAssetMenu(menuName = "Card/Effects/DoubleAndExhaust")]
public class DoubleAndExhaustEffect : CardEffect
{
    public int multiplyBy = 2;

    public override void Apply(GameContext ctx)
    {
        ctx.Multiplier *= Mathf.Max(1, multiplyBy);
        ctx.ExhaustThisCard = true;
        Debug.Log($"[Effect] DoubleAndExhaust => Multiplier now x{ctx.Multiplier}, Exhaust=true");
    }
}

[CreateAssetMenu(menuName = "Card/Effects/AllInAddTime")]
public class AllInAddTimeEffect : CardEffect
{
    public float addSeconds = 3f;

    public override void Apply(GameContext ctx)
    {
        int lost = ctx.ConsumeAllScore();


        if (lost < 0)
        {
            Debug.Log("[Effect] AllInAddTime: no score to consume");
            return;
        }

        float t = addSeconds * Mathf.Max(1, ctx.Multiplier);

        Debug.Log($"[Effect] AllInAddTime: lost={lost}, +{t}s");

        ctx.AddTime(t);
    }
}
