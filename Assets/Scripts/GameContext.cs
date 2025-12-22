// Assets/Scripts/GameContext.cs
public class GameContext
{
    private readonly GameManager gm;
    public GameContext(GameManager gm) { this.gm = gm; }

    public void AddScore(int amount) => gm.AddScore(amount);
    public void Draw(int amount) => gm.DrawCards(amount);
}
