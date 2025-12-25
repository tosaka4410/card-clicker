using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildingPanelController : MonoBehaviour
{
    [SerializeField] private Transform buildPanel;
    [SerializeField] private BuildingButtonView buildButtonPrefab; // ★ Button -> View に変更

    private readonly List<BuildingButtonView> views = new();

    public void BuildButtons(IReadOnlyList<BuildingDef> defs, Func<BuildingDef, bool> onTryBuild)
    {
        for (int i = buildPanel.childCount - 1; i >= 0; i--)
            Destroy(buildPanel.GetChild(i).gameObject);

        views.Clear();

        foreach (var def in defs)
        {
            var view = Instantiate(buildButtonPrefab, buildPanel);

            // 念のためnullチェック
            if (view.BuyButton != null)
            {
                view.BuyButton.onClick.RemoveAllListeners();
                view.BuyButton.onClick.AddListener(() => onTryBuild?.Invoke(def));
            }

            views.Add(view);
        }
    }

    public void UpdateLabels(
        IReadOnlyList<BuildingDef> defs,
        Func<BuildingDef, int> getCost,
        Func<BuildingDef, int> getActiveCount,
        Func<int> getScore,
        Func<bool> isEnded
    )
    {
        for (int i = 0; i < defs.Count && i < views.Count; i++)
        {
            var def = defs[i];
            var view = views[i];

            int cost = getCost(def);
            int n = getActiveCount(def);

            // 表示（画像の要素に分割）
            string name = $"{def.id} x{n}";
            string dps  = $"{def.scorePerSec:0.##}/s";
            string costStr = $"{cost}";

            view.SetTexts(name, dps, costStr);

            // 押せる条件
            bool interactable = (getScore() >= cost) && !isEnded();
            view.SetInteractable(interactable);
        }
    }
}
