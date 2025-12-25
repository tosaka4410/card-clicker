using System;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeOptionView : MonoBehaviour
{
    [Header("Parts")]
    [SerializeField] private Button selectButton;     // この選択肢を選ぶボタン（親でもOK）
    [SerializeField] private CardView cardView;       // ★元カード表示（あなたの既存CardView）
    [SerializeField] private Text upgradeTitleText;
    [SerializeField] private Text upgradeDescText;

    public void Bind(UpgradeChoice choice, Action onClick)
    {
        // 1) 元カードを表示
        // Deckモードにするとクリック不可＆見た目が安定しやすい
        if (cardView != null)
            cardView.Bind(choice.targetCard, CardDisplayMode.Upgrade, onClick: _ => onClick?.Invoke(), null, $"[{choice.upgrade.title}]を追加");

        // 2) アップグレード情報
        if (upgradeTitleText != null) upgradeTitleText.text = choice.upgrade.title;

        if (upgradeDescText != null)
        {
            // Target行は入れても入れなくてもOK（カードが見えてるので省略可）
            upgradeDescText.text = choice.upgrade.description;
        }

        // 3) 選択
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onClick?.Invoke());
        }
    }
}
