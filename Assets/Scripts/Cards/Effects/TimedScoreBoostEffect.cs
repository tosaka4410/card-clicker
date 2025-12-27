using UnityEngine;

[CreateAssetMenu(menuName = "Card/Effects/TimedScoreBoost")]
public class TimedScoreBoostEffect : CardEffect
{
    public float multiplier = 2f;
    public float duration = 5f;

    public override void Apply(GameContext ctx)
    {
        Debug.Log($"[Effect] Score x{multiplier} for {duration}s");
        ctx.AddTempScoreMultiplier(multiplier, duration);
    }
}
