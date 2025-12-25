using UnityEngine;

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