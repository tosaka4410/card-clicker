using UnityEngine;

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
