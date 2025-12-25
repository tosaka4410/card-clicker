using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private Text timeText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text goalText;
    [SerializeField] private Text scorePerSecText;

    [Header("Relics")]
    [SerializeField] private Text relicText; // 複数行表示用

    public void Render(
        float timeLeft,
        int score,
        int goal, 
        int scorePerSec,
        IReadOnlyDictionary<RelicData, int> relicCounts
    )
    {
        timeText.text = $"{timeLeft:0.0}s";
        scoreText.text = $"{score}";
        goalText.text = $"{goal}";
        scorePerSecText.text = $"{scorePerSec}/s";

        if (relicText != null)
        {
            var sb = new StringBuilder();

            if (relicCounts == null || relicCounts.Count == 0)
            {
                sb.AppendLine("-");
            }
            else
            {
                foreach (var kv in relicCounts)
                {
                    if (kv.Key == null) continue;
                    sb.AppendLine($"- {kv.Key.relicName} x{kv.Value}");
                }
            }

            relicText.text = sb.ToString();
        }
    }
}
