// Assets/Scripts/UI/ProtoUI.cs
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProtoUI : MonoBehaviour
{
    [Header("Top Bar")]
    public TMP_Text timerText;
    public TMP_Text scoreText;
    public TMP_Text dpsText;

    [Header("Hand")]
    public Transform handRoot;
    public Button cardButtonPrefab;

    [Header("Facilities")]
    public Transform facilityRoot;
    public TMP_Text facilityRowPrefab;

    [Header("Modal")]
    public GameObject modalPanel;
    public Button addBtn;
    public Button removeBtn;
    public Button upgradeBtn;

    [Header("Choice")]
    public GameObject choicePanel;
    public TMP_Text choiceTitle;
    public Transform choiceRoot;
    public Button choiceButtonPrefab;

    [Header("Run End")]
    public GameObject runEndPanel;
    public TMP_Text runEndText;
    public Button restartBtn;

    private ProtoGameManager gm;
    private readonly List<Button> handButtons = new();
    private readonly List<TMP_Text> facilityRows = new();
    private readonly List<Button> choiceButtons = new();

    public void Bind(ProtoGameManager manager)
    {
        gm = manager;

        // modal buttons
        addBtn.onClick.RemoveAllListeners();
        removeBtn.onClick.RemoveAllListeners();
        upgradeBtn.onClick.RemoveAllListeners();

        addBtn.onClick.AddListener(() => gm.ChooseAddCard());
        removeBtn.onClick.AddListener(() => gm.ChooseRemoveCard());
        upgradeBtn.onClick.AddListener(() => gm.ChooseUpgradeCard());

        restartBtn.onClick.RemoveAllListeners();
        restartBtn.onClick.AddListener(() => gm.StartRun());
    }

    public void RefreshAll()
    {
        RefreshTopBar(0);
        RefreshHand();
        RefreshFacilities();
    }

    public void RefreshTopBar(double perSec)
    {
        var st = gm.GetState();
        timerText.text = $"Time: {Mathf.CeilToInt(st.timeLeft)}";
        scoreText.text = $"Score: {st.score:0}";
        dpsText.text = $"PerSec: {perSec:0.0}";
    }

    public void RefreshHand()
    {
        var hand = gm.GetHand();

        // ensure 5 buttons
        while (handButtons.Count < gm.handLimit)
        {
            var b = Instantiate(cardButtonPrefab, handRoot);
            int idx = handButtons.Count;
            b.onClick.AddListener(() => gm.PlayCard(idx));
            handButtons.Add(b);
        }

        for (int i = 0; i < handButtons.Count; i++)
        {
            var b = handButtons[i];
            var label = b.GetComponentInChildren<TMP_Text>();
            if (i < hand.Count && hand[i] != null)
            {
                b.gameObject.SetActive(true);
                label.text = hand[i].displayName;
                b.interactable = true;
            }
            else
            {
                b.gameObject.SetActive(i < gm.handLimit);
                label.text = "-";
                b.interactable = false;
            }
        }
    }

    public void RefreshFacilities()
    {
        var facs = gm.GetFacilities();

        while (facilityRows.Count < facs.Count)
        {
            var row = Instantiate(facilityRowPrefab, facilityRoot);
            facilityRows.Add(row);
        }

        for (int i = 0; i < facilityRows.Count; i++)
        {
            if (i < facs.Count && facs[i].def != null)
            {
                facilityRows[i].gameObject.SetActive(true);
                facilityRows[i].text = $"{facs[i].def.displayName} Lv{facs[i].level}  +{facs[i].PerSec:0.0}/s";
            }
            else
            {
                facilityRows[i].gameObject.SetActive(false);
            }
        }
    }

    public void ShowManageModal()
    {
        modalPanel.SetActive(true);
        choicePanel.SetActive(false);
        gm.SetPaused(true);
    }

    public void HideModal()
    {
        modalPanel.SetActive(false);
        choicePanel.SetActive(false);
        gm.SetPaused(false);
    }

    private void ShowChoice(string title, List<CardDef> options, Action<CardDef> onPick)
    {
        choiceTitle.text = title;
        choicePanel.SetActive(true);

        // clear old
        foreach (var b in choiceButtons) Destroy(b.gameObject);
        choiceButtons.Clear();

        foreach (var c in options)
        {
            var b = Instantiate(choiceButtonPrefab, choiceRoot);
            b.GetComponentInChildren<TMP_Text>().text = c.displayName;
            b.onClick.AddListener(() => onPick(c));
            choiceButtons.Add(b);
        }
    }

    public void ShowAddChoice(List<CardDef> options, Action<CardDef> onPick)
        => ShowChoice("Add a Card", options, onPick);

    public void ShowRemoveChoice(List<CardDef> options, Action<CardDef> onPick)
        => ShowChoice("Remove a Card", options, onPick);

    public void ShowUpgradeChoice(List<CardDef> options, Action<CardDef> onPick)
        => ShowChoice("Upgrade a Card (+1 repeat)", options, onPick);

    public void ShowRunEnd(double score)
    {
        runEndPanel.SetActive(true);
        runEndText.text = $"Run End\nScore: {score:0}";
    }
}
