using UnityEngine;
using UnityEngine.UI;
using System.Text;

public class DeckCardItemView : MonoBehaviour
{
    [Header("Assign in Inspector (Legacy Text)")]
    public Text nameText;
    public Text effectsText;

    public void Bind(CardData card)
    {
        if (nameText == null || effectsText == null)
        {
            Debug.LogError("[DeckCardItemView] nameText/effectsText is not assigned");
            return;
        }

        nameText.text = card.cardName;

        var sb = new StringBuilder();
        for (int i = 0; i < card.effects.Count; i++)
        {
            var e = card.effects[i];
            if (e != null) sb.Append("- ").Append(e.name).Append('\n');
        }
        effectsText.text = sb.ToString();
    }
}
