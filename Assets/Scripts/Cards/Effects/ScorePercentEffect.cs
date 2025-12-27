using UnityEngine;

[CreateAssetMenu(menuName = "Card/Effects/AddScorePercentOfCurrent")]
public class AddScorePercentOfCurrentEffect : CardEffect
{
    [Range(0f, 1f)]
    public float percent = 0.30f;

    public override void Apply(GameContext ctx)
    {
        int cur = ctx.GetScore();
        int add = Mathf.FloorToInt(cur * percent);

        Debug.Log($"[Effect] AddScorePercent cur={cur} percent={percent} add={add}");

        if (add > 0)
            ctx.AddScore(add);
    }
}
