// Assets/Scripts/GameManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Stage")]
    public float stageTime = 150f;
    public int goal = 800;

    [Header("Draw")]
    public float drawInterval = 1.2f;
    public int startingHand = 3;
    public int handLimit = 5;

    [Header("Data")]
    public List<CardData> startingDeck = new();
    public List<BuildingDef> buildingDefs = new();

    [Header("UI")]
    public Text timeText;
    public Text scoreText;
    public Text goalText;
    public Text diffText;
    public Transform handPanel;
    public CardView cardViewPrefab;
    public Transform buildPanel;
    public Button buildButtonPrefab;
    public GameObject resultModal;
    public Text resultText;
    public Button nextButton;
    public Button retryButton;

    private float timeLeft;
    private float drawTimer;
    private int score;
    private float autoScoreBuffer = 0f;

    private readonly DeckSystem deck = new();
    private readonly List<CardData> hand = new();
    private BuildingSystem buildings = new();

    private GameContext ctx;
    private bool ended;

    void Start()
    {
        ctx = new GameContext(this);
        StartStage();
    }

    void Update()
    {
        if (ended)
            return;

        timeLeft -= Time.deltaTime;
        drawTimer += Time.deltaTime;

        // 自動スコア
        float sps = buildings.GetTotalScorePerSec(buildingDefs);
        autoScoreBuffer += sps * Time.deltaTime;

        int add = Mathf.FloorToInt(autoScoreBuffer);
        if (add > 0)
        {
            autoScoreBuffer -= add;
            AddScore(add);
        }

        // 時間ドロー
        while (drawTimer >= drawInterval)
        {
            drawTimer -= drawInterval;
            DrawCards(1);
        }

        UpdateUITextsOnly();

        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            EndStage(score >= goal);
        }
    }

    void StartStage()
    {
        ended = false;
        resultModal.SetActive(false);

        timeLeft = stageTime;
        drawTimer = 0f;
        score = 0;

        deck.Init(startingDeck);
        hand.Clear();
        DrawCards(startingHand);

        SetupBuildButtons();
        UpdateUI();
    }

    void SetupBuildButtons()
    {
        foreach (Transform c in buildPanel)
            Destroy(c.gameObject);

        foreach (var def in buildingDefs)
        {
            var btn = Instantiate(buildButtonPrefab, buildPanel);
            btn.onClick.AddListener(() =>
            {
                if (ended)
                    return;
                buildings.TryBuild(def, TryPayScore);
                UpdateBuildButtonLabels();
            });
        }
        UpdateBuildButtonLabels();
    }

    void UpdateBuildButtonLabels()
    {
        for (int i = 0; i < buildingDefs.Count; i++)
        {
            var def = buildingDefs[i];
            var btn = buildPanel.GetChild(i).GetComponent<Button>();
            int cost = buildings.GetCost(def);
            int n = buildings.GetActiveCount(def.id);
            btn.GetComponentInChildren<Text>().text =
                $"{def.id}  Cost:{cost}  +{def.scorePerSec}/s  x{n}";
            btn.interactable = (score >= cost) && !ended;
        }
    }

    void UpdateUI()
    {
        timeText.text = $"Time: {timeLeft:0.0}s";
        scoreText.text = $"Score: {score}";
        goalText.text = $"Goal: {goal}";
        diffText.text = $"Diff: {goal - score}";
        UpdateHandUI();
        UpdateBuildButtonLabels();
    }

    void UpdateUITextsOnly()
    {
        timeText.text = $"Time: {timeLeft:0.0}s";
        scoreText.text = $"Score: {score}";
        goalText.text = $"Goal: {goal}";
        diffText.text = $"Diff: {goal - score}";
        UpdateBuildButtonLabels();
    }

    void UpdateHandUI()
    {
        foreach (Transform c in handPanel)
            Destroy(c.gameObject);

        foreach (var card in hand)
        {
            var v = Instantiate(cardViewPrefab, handPanel);
            v.Bind(card, () => PlayCard(card));
        }
    }

    public void AddScore(int amount)
    {
        score += Mathf.Max(0, amount);
    }

    public bool TryPayScore(int amount)
    {
        if (score < amount)
            return false;
        score -= amount;
        return true;
    }

    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (hand.Count >= handLimit)
                return;
            var c = deck.DrawOne();
            if (c == null)
                return;
            hand.Add(c);
        }
        UpdateHandUI();
    }

    void PlayCard(CardData card)
    {
        if (ended)
            return;

        // 効果はすべて同時（順序はリスト順）
        foreach (var e in card.effects)
            if (e != null)
                e.Apply(ctx);

        hand.Remove(card);
        deck.Discard(card);
        UpdateHandUI();
    }

    void EndStage(bool cleared)
    {
        ended = true;
        resultModal.SetActive(true);
        resultText.text = cleared ? "STAGE CLEAR!" : "GAME OVER";

        nextButton.gameObject.SetActive(cleared);
        retryButton.gameObject.SetActive(true);

        nextButton.onClick.RemoveAllListeners();
        retryButton.onClick.RemoveAllListeners();

        nextButton.onClick.AddListener(() =>
        {
            // MVP：次ステージは goal と時間だけ上げる
            goal = Mathf.RoundToInt(goal * 1.35f + 200);
            stageTime = Mathf.Max(60f, stageTime - 5f);
            StartStage();
        });

        retryButton.onClick.AddListener(() =>
        {
            // MVP：施設の建設数は保持したまま再挑戦しても気持ちいい（後で調整）
            StartStage();
        });
    }
}
