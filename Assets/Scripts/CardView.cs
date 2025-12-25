using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public enum CardDisplayMode
{
    Hand, // クリックでPlay
    Shop, // Cost表示、クリックでBuy
    Deck, // 表示のみ
}

public class CardView : MonoBehaviour
{
    [Header("Core")]
    [SerializeField]
    private Text titleText;

    [SerializeField]
    private Text bodyText;

    [SerializeField]
    private Button button;

    [Header("Optional Footer")]
    [SerializeField]
    private GameObject footerRoot; // まとめてON/OFF

    [SerializeField]
    private Text costText;

    [SerializeField]
    private Text tagText; // SOLDなど

    [SerializeField]
    private GameObject soldOverlay; // 任意

    [Header("Art")]
    [SerializeField]
    private Image cardImage; // Mask配下のCardImage(Image)

    private CardData boundCard;

    [SerializeField]
    private HoverScaleAnimator hoverAnimator;

    public void Bind(
        CardData card,
        CardDisplayMode mode,
        Action<CardData> onClick = null,
        int? cost = null,
        bool sold = false,
        string tag = null
    )
    {
        boundCard = card;

        titleText.text = card != null ? card.cardName : "(null)";
        bodyText.text = BuildBody(card);

        // Footer
        bool showFooter = (mode == CardDisplayMode.Shop);
        if (footerRoot != null)
            footerRoot.SetActive(showFooter);

        if (showFooter)
        {
            if (costText != null)
                costText.text = cost.HasValue ? $"Cost: {cost.Value}" : "";
            if (tagText != null)
                tagText.text = string.IsNullOrEmpty(tag) ? "" : tag;
        }

        if (soldOverlay != null)
            soldOverlay.SetActive(sold);

        if (cardImage != null)
        {
            cardImage.sprite = card != null ? card.art : null;
            // 画像がないカードを見た目で分かるようにするなら
            cardImage.enabled = (card != null && card.art != null);
        }

        // Button
        button.onClick.RemoveAllListeners();

        if (mode == CardDisplayMode.Deck)
        {
            button.interactable = false;
            return;
        }

        // Hand/Shop
        button.interactable = !sold;
        button.onClick.AddListener(() => onClick?.Invoke(boundCard));

        // Hover Animation
        if (hoverAnimator != null)
        {
            hoverAnimator.SetEnabled(mode == CardDisplayMode.Hand || mode == CardDisplayMode.Shop);
        }
    }

    public void SetSold(bool sold)
    {
        if (button != null)
            button.interactable = !sold;
        if (soldOverlay != null)
            soldOverlay.SetActive(sold);
        if (tagText != null)
            tagText.text = sold ? "SOLD" : "";
    }

    private string BuildBody(CardData card)
    {
        if (card == null)
            return "";

        var sb = new StringBuilder();
        // if (!string.IsNullOrEmpty(card.description))
        //     sb.AppendLine(card.description);

        if (card.effects != null && card.effects.Count > 0)
        {
            foreach (var e in card.effects)
                if (e != null)
                    sb.AppendLine($"- {e.name}");
        }
        return sb.ToString();
    }
}
