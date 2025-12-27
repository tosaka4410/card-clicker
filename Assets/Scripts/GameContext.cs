public class GameContext
{
    private readonly GameManager gm;

    public GameContext(GameManager gm)
    {
        this.gm = gm;
    }

    // 既存
    public int Multiplier { get; set; } = 1;
    public bool ExhaustThisCard { get; set; } = false;

    // ★追加：一時スコア倍率
    public float TempScoreMultiplier { get; set; } = 1f;

    public void AddScore(int amount) => gm.AddScore(amount, ScoreSource.Card);

    public bool TryPayScore(int amount) => gm.TryPayScore(amount);

    public void Draw(int amount) => gm.DrawCards(amount);

    public void AddTime(float seconds) => gm.AddTime(seconds);

    public int ConsumeAllScore() => gm.ConsumeAllScore();

    public void AddTempScoreMultiplier(float multiplier, float duration)
    {
        gm.AddTempScoreMultiplier(multiplier, duration);
    }
}
