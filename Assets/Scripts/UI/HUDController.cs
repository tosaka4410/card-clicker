using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField]
    private Text timeText;

    [SerializeField]
    private Text scoreText;

    [SerializeField]
    private Text goalText;

    [SerializeField]
    private Text scorePerSecText;

    [SerializeField]
    private Text deckText;

    [SerializeField]
    private Text discardText;

    [SerializeField]
    private Text multiplierText;

    [Header("Relics")]
    [SerializeField]
    private Text relicText; // 複数行表示用

    public void Render(
        float timeLeft,
        long score,
        int goal,
        long scorePerSec,
        IReadOnlyDictionary<RelicData, int> relicCounts,
        int deckCount,
        int discardCount,
        float tempMul
    )
    {
        timeText.text = $"{timeLeft:0.0}s";
        scoreText.text = $"{score}";
        goalText.text = $"{goal}";
        scorePerSecText.text = $"{scorePerSec}/s";
        if (deckText != null)
            deckText.text = $"{deckCount}";

        if (discardText != null)
            discardText.text = $"{discardCount}";
        if (multiplierText != null)
        {
            multiplierText.text = tempMul > 1.001f ? $"x{tempMul:0.##}" : "x1.0";
        }

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
                    if (kv.Key == null)
                        continue;
                    sb.AppendLine($"- {kv.Key.relicName} x{kv.Value}");
                }
            }

            relicText.text = sb.ToString();
        }
    }
}
