using UnityEngine;

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