// Assets/Scripts/GameManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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

    [Header("Building Milestone")]
    public int buildingMilestoneStep = 10; // 10個ごと
    public float buildingBonusRate = 1.15f; // 施設強化倍率（15%）

    private Dictionary<string, int> nextBuildingMilestone = new();

    [Header("Relic")]
    public RelicSystem relicSystem;
    public GameObject relicModal;
    public Transform relicOptionsRoot;
    public Button relicButtonPrefab;

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
    private List<UpgradeChoice> currentUpgradeChoices = new();

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
        while (drawTimer >= drawInterval * relicSystem.drawIntervalMultiplier)
        {
            drawTimer -= drawInterval * relicSystem.drawIntervalMultiplier;
            DrawCards(1);
        }

        UpdateUITextsOnly();

        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            EndStage(score >= goal);
        }
        HandleCardKeyInput();
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
        InitBuildingMilestones();
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
                if (buildings.TryBuild(def, relicSystem.buildingCostMultiplier, TryPayScore))
                {
                    UpdateBuildButtonLabels();
                    CheckBuildingMilestone(def);
                }
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
            int cost = buildings.GetCost(def, relicSystem.buildingCostMultiplier);
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
        int v = Mathf.RoundToInt(amount * relicSystem.scoreMultiplier);
        score += Mathf.Max(0, v);
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
                ShowRelicChoices();
            });
        }

        retryButton.onClick.AddListener(() => StartStage());
    }

    void ShowUpgradeChoices()
    {
        upgradeModal.SetActive(true);
        upgradeModal.transform.SetAsLastSibling();
        upgradeTitleText.text = "Choose one upgrade";

        foreach (Transform c in upgradeOptionsRoot)
            Destroy(c.gameObject);

        currentUpgradeChoices.Clear();

        // Upgradeを3つ引く
        var upgrades = upgradeSystem.RollUpgrades(3);

        foreach (var up in upgrades)
        {
            if (startingDeck.Count == 0)
                continue;

            // ★ このUpgrade専用の対象カードを確定
            var targetCard = startingDeck[Random.Range(0, startingDeck.Count)];

            var choice = new UpgradeChoice { upgrade = up, targetCard = targetCard };
            currentUpgradeChoices.Add(choice);

            var btn = Instantiate(upgradeButtonPrefab, upgradeOptionsRoot);
            var view = btn.GetComponent<UpgradeButtonView>();

            // 表示内容
            string title = up.title;
            string desc = $"{up.description}\n\n" + $"Target: {targetCard.cardName}";

            view.Bind(title, desc);

            btn.onClick.AddListener(() =>
            {
                ApplyUpgrade(choice);
                upgradeModal.SetActive(false);

                goal = Mathf.RoundToInt(goal * 1.35f + 200);
                stageTime = Mathf.Max(60f, stageTime - 5f);
                StartStage();
            });
        }
    }

    void ApplyUpgrade(UpgradeChoice choice)
    {
        var target = choice.targetCard;

        int idx = startingDeck.IndexOf(target);
        if (idx < 0)
        {
            Debug.LogWarning("[Upgrade] Target card not found in deck");
            return;
        }

        var upgraded = upgradeSystem.CloneAndAddEffect(target, choice.upgrade.addEffect);
        startingDeck[idx] = upgraded;

        Debug.Log($"[Upgrade] {target.cardName} gets {choice.upgrade.addEffect.name}");
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
            var go = Instantiate(shopItemButtonPrefab, shopItemsRoot);

            var view = go.GetComponent<CardView>();
            if (view == null)
            {
                Debug.LogError("[Shop] ShopItemPrefab has no CardView");
                continue;
            }

            // カード表示は CardView に任せる
            view.Bind(
                item.card,
                () =>
                {
                    if (!TryPayScore(item.cost))
                        return;

                    BuyCard(item.card);
                    view.button.interactable = false;

                    // 購入後の表示を変えたい場合
                    view.bodyText.text += "\n<SOLD>";
                }
            );

            // ★ Shop 用にコスト表示を足す
            view.bodyText.text += $"\nCost: {item.cost}";
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

    void CheckBuildingMilestone(BuildingDef def)
    {
        int built = buildings.GetBuiltCount(def.id);
        int next = nextBuildingMilestone[def.id];

        if (built < next)
            return;

        // 次のマイルストーンへ
        nextBuildingMilestone[def.id] += buildingMilestoneStep;

        OnBuildingMilestoneReached(def, built);
    }

    void OnBuildingMilestoneReached(BuildingDef def, int builtCount)
    {
        Debug.Log($"[Milestone] {def.id} built {builtCount}");

        // ① この施設だけ強化
        def.scorePerSec *= buildingBonusRate;

        // ② 時間停止
        Time.timeScale = 0f;

        // ③ アップグレード3択を表示
        ShowUpgradeChoices();
    }

    void InitBuildingMilestones()
    {
        nextBuildingMilestone.Clear();
        foreach (var def in buildingDefs)
        {
            nextBuildingMilestone[def.id] = buildingMilestoneStep;
        }
    }

    bool CanPlayCardByKey()
    {
        if (ended)
            return false;
        if (shopModal != null && shopModal.activeInHierarchy)
            return false;
        if (deckViewModal != null && deckViewModal.activeInHierarchy)
            return false;
        if (upgradeModal != null && upgradeModal.activeInHierarchy)
            return false;
        if (Time.timeScale == 0f)
            return false;

        return true;
    }

    void HandleCardKeyInput()
    {
        if (!CanPlayCardByKey())
            return;

        if (Keyboard.current == null)
            return;

        int max = Mathf.Min(hand.Count, 9);

        for (int i = 0; i < max; i++)
        {
            var key = Key.Digit1 + i; // 1〜9
            if (Keyboard.current[key].wasPressedThisFrame)
            {
                var card = hand[i];
                Debug.Log($"[Input] Play card by key: {i + 1} -> {card.cardName}");
                PlayCard(card);
                return;
            }
        }
    }

    void ShowRelicChoices()
    {
        relicModal.SetActive(true);
        relicModal.transform.SetAsLastSibling();

        foreach (Transform c in relicOptionsRoot)
            Destroy(c.gameObject);

        var relics = relicSystem.RollRelics(3);

        foreach (var relic in relics)
        {
            var btn = Instantiate(relicButtonPrefab, relicOptionsRoot);
            var texts = btn.GetComponentsInChildren<Text>();

            texts[0].text = relic.relicName;
            texts[1].text = relic.description;

            btn.onClick.AddListener(() =>
            {
                relicSystem.AddRelic(relic);
                relicModal.SetActive(false);

                // 次ステージへ
                goal = Mathf.RoundToInt(goal * 1.35f + 200);
                stageTime = Mathf.Max(60f, stageTime - 5f);
                StartStage();
            });
        }
    }
}
