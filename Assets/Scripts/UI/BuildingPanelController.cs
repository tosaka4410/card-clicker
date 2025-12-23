using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildingPanelController : MonoBehaviour
{
    [SerializeField] private Transform buildPanel;
    [SerializeField] private Button buildButtonPrefab;

    private readonly List<Button> buttons = new();

    public void BuildButtons(IReadOnlyList<BuildingDef> defs, Func<BuildingDef, bool> onTryBuild)
    {
        for (int i = buildPanel.childCount - 1; i >= 0; i--)
            Destroy(buildPanel.GetChild(i).gameObject);

        buttons.Clear();

        foreach (var def in defs)
        {
            var btn = Instantiate(buildButtonPrefab, buildPanel);
            btn.onClick.AddListener(() => onTryBuild?.Invoke(def));
            buttons.Add(btn);
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
        for (int i = 0; i < defs.Count && i < buttons.Count; i++)
        {
            var def = defs[i];
            var btn = buttons[i];

            int cost = getCost(def);
            int n = getActiveCount(def);

            btn.GetComponentInChildren<Text>().text =
                $"{def.id}  Cost:{cost}  +{def.scorePerSec}/s  x{n}";

            btn.interactable = (getScore() >= cost) && !isEnded();
        }
    }
}
