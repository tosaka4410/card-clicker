using UnityEngine;

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
