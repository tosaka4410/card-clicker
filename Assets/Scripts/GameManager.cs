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

    [Header("Upgrade")]
    public UpgradeSystem upgradeSystem;
    public GameObject upgradeModal;
    public Text upgradeTitleText;
    public Button upgradeButtonPrefab; // 既存ボタンPrefab流用でもOK
    public Transform upgradeOptionsRoot;

    [Header("Shop")]
    public ShopSystem shopSystem;
    public GameObject shopModal;
    public Transform shopItemsRoot;
    public Button shopItemButtonPrefab;
    public Button rerollButton;
    public Button openShopButton; // HUDに追加
    public Button closeShopButton; // HUDに追加

    [Header("Deck View")]
    public GameObject deckViewModal;
    public Transform deckViewContent;
    public GameObject deckCardItemPrefab;
    public Button openDeckViewButton;
    public Button closeDeckViewButton;

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
        openShopButton.onClick.AddListener(OpenShop);
        closeShopButton.onClick.AddListener(CloseShop);
        openDeckViewButton.onClick.AddListener(OpenDeckView);
        closeDeckViewButton.onClick.AddListener(CloseDeckView);
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

        Time.timeScale = 1f; // ← 念のため必ず戻す
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

    public void AddTime(float seconds)
    {
        if (ended)
            return;
        timeLeft += seconds;
    }

    public int ConsumeAllScore()
    {
        int lost = score;
        score = 0;
        return lost;
    }

    void PlayCard(CardData card)
    {
        if (ended)
            return;

        // ★このカードプレイ用の文脈を初期化
        ctx.Multiplier = 1;
        ctx.ExhaustThisCard = false;

        Debug.Log($"[GameManager] PlayCard: {card.cardName}");

        foreach (var e in card.effects)
        {
            if (e == null)
                continue;
            Debug.Log($"[GameManager] Apply effect: {e.name}");
            e.Apply(ctx);
        }

        hand.Remove(card);

        if (ctx.ExhaustThisCard)
        {
            Debug.Log($"[GameManager] Exhausted: {card.cardName} (removed from game)");
            // 捨て札にも戻さない＝消滅
            // ついでに startingDeck からも消したいなら下をON（MVP仕様次第）
            // startingDeck.Remove(card);
        }
        else
        {
            deck.Discard(card);
        }

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

        if (cleared)
        {
            nextButton.onClick.AddListener(() =>
            {
                resultModal.SetActive(false);
                ShowUpgradeChoices();
            });
        }

        retryButton.onClick.AddListener(() => StartStage());
    }

    void ShowUpgradeChoices()
    {
        upgradeModal.SetActive(true);
        upgradeTitleText.text = "Choose one upgrade";

        // 既存ボタン削除
        foreach (Transform c in upgradeOptionsRoot)
            Destroy(c.gameObject);

        var effects = upgradeSystem.Roll3Effects();

        for (int i = 0; i < effects.Count; i++)
        {
            var e = effects[i];
            var btn = Instantiate(upgradeButtonPrefab, upgradeOptionsRoot);
            btn.GetComponentInChildren<Text>().text = $"+ {e.name}";
            btn.onClick.AddListener(() =>
            {
                ApplyUpgradeToRandomCard(e);
                upgradeModal.SetActive(false);

                // 次ステージ進行（MVP）
                goal = Mathf.RoundToInt(goal * 1.35f + 200);
                stageTime = Mathf.Max(60f, stageTime - 5f);
                StartStage();
            });
        }
    }

    void ApplyUpgradeToRandomCard(CardEffect addEffect)
    {
        if (startingDeck.Count == 0)
            return;

        int idx = Random.Range(0, startingDeck.Count);
        var original = startingDeck[idx];

        var upgraded = upgradeSystem.CloneAndAddEffect(original, addEffect);
        startingDeck[idx] = upgraded;

        Debug.Log($"[Upgrade] {original.cardName} gets +{addEffect.name}");
    }

    void OpenShop()
    {
        if (ended)
            return;

        shopModal.SetActive(true);
        RefreshShop(false);
    }

    void RefreshShop(bool isReroll)
    {
        if (isReroll)
        {
            if (!TryPayScore(shopSystem.rerollCost))
                return;
        }

        foreach (Transform c in shopItemsRoot)
            Destroy(c.gameObject);

        var items = shopSystem.GenerateLineup();

        foreach (var item in items)
        {
            var btn = Instantiate(shopItemButtonPrefab, shopItemsRoot);
            btn.GetComponentInChildren<Text>().text = $"{item.card.cardName}\nCost: {item.cost}";

            btn.interactable = score >= item.cost;

            btn.onClick.AddListener(() =>
            {
                if (!TryPayScore(item.cost))
                    return;

                BuyCard(item.card);
                btn.interactable = false;
                btn.GetComponentInChildren<Text>().text += "\nSOLD";
            });
        }

        rerollButton.onClick.RemoveAllListeners();
        rerollButton.GetComponentInChildren<Text>().text = $"Reroll ({shopSystem.rerollCost})";
        rerollButton.interactable = score >= shopSystem.rerollCost;
        rerollButton.onClick.AddListener(() => RefreshShop(true));
    }

    void BuyCard(CardData card)
    {
        startingDeck.Add(card);

        Debug.Log($"[Shop] Bought card: {card.cardName}");

        // すぐ出したいならこれも可（任意）
        // deck.AddToDiscard(card);
    }

    public void CloseShop()
    {
        shopModal.SetActive(false);
    }

    void OpenDeckView()
    {
        if (ended)
            return;

        Time.timeScale = 0f;
        deckViewModal.SetActive(true);

        RefreshDeckView();
    }

    void RefreshDeckView()
    {
        if (deckViewContent == null || deckCardItemPrefab == null)
        {
            return;
        }

        int before = deckViewContent.childCount;
        for (int i = deckViewContent.childCount - 1; i >= 0; i--)
            Destroy(deckViewContent.GetChild(i).gameObject);

        foreach (var card in startingDeck)
        {
            var go = Instantiate(deckCardItemPrefab, deckViewContent);

            var view = go.GetComponent<DeckCardItemView>();
            if (view == null)
            {
                Debug.LogError("[DeckView] DeckCardItemPrefab has no DeckCardItemView");
                continue;
            }

            view.Bind(card);
        }
    }

    void CloseDeckView()
    {
        Debug.Log("[DeckView] Close");

        deckViewModal.SetActive(false);
        Time.timeScale = 1f;
    }
}
