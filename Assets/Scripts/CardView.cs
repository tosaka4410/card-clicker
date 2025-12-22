// Assets/Scripts/CardView.cs
using UnityEngine;
using UnityEngine.UI;
using System.Text;

public class CardView : MonoBehaviour
{
    public Text titleText;
    public Text bodyText;
    public Button button;

    public void Bind(CardData data, System.Action onClick)
    {
        titleText.text = data.cardName;

        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(data.description)) sb.AppendLine(data.description);
        sb.AppendLine("Effects:");
        foreach (var e in data.effects)
            sb.AppendLine($"- {e.name}");
        bodyText.text = sb.ToString();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => {
            Debug.Log($"Card '{data.cardName}' clicked.");
            onClick?.Invoke();
            });
    }
}
