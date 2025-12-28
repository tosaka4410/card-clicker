using System;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeButtonController : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Text costText; // 任意
    [SerializeField] private Text usesText; // 任意

    private Func<int> getCost;
    private Func<long> getScore;
    private Func<bool> canUse;
    private Action onClick;
    private Func<int> getUses; // 任意（表示用）

    public void Init(
        Func<int> getCost,
        Func<long> getScore,
        Func<bool> canUse,
        Action onClick,
        Func<int> getUses = null
    )
    {
        this.getCost = getCost;
        this.getScore = getScore;
        this.canUse = canUse;
        this.onClick = onClick;
        this.getUses = getUses;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => this.onClick?.Invoke());
        }
    }

    void Update()
    {
        if (button == null) return;

        int cost = getCost?.Invoke() ?? 0;
        long score = getScore?.Invoke() ?? 0;
        bool ok = (canUse?.Invoke() ?? true) && score >= cost;

        button.interactable = ok;

        if (costText != null) costText.text = $"{cost}";
        if (usesText != null && getUses != null) usesText.text = $"{getUses()}";
    }
}
