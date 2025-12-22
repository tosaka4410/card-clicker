public class GameContext
{
    private readonly GameManager gm;
    public GameContext(GameManager gm) { this.gm = gm; }

    // このカードの効果倍率（通常1）
    public int Multiplier { get; set; } = 1;

    // このカードはプレイ後に消滅するか
    public bool ExhaustThisCard { get; set; } = false;

    public void AddScore(int amount) => gm.AddScore(amount);
    public bool TryPayScore(int amount) => gm.TryPayScore(amount);

    public void Draw(int amount) => gm.DrawCards(amount);

    public void AddTime(float seconds) => gm.AddTime(seconds);
    public int ConsumeAllScore() => gm.ConsumeAllScore();

}
