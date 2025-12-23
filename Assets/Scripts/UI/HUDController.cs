using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private Text timeText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text goalText;
    [SerializeField] private Text diffText;

    public void Render(float timeLeft, int score, int goal)
    {
        timeText.text = $"Time: {timeLeft:0.0}s";
        scoreText.text = $"Score: {score}";
        goalText.text = $"Goal: {goal}";
        diffText.text = $"Diff: {goal - score}";
    }
}
