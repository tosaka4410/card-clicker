// Assets/Scripts/GameManager.cs
using System.Collections.Generic;
using UnityEngine;
using unityroom.Api;

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

    [Header("Systems")]
    public UpgradeSystem upgradeSystem;
    public ShopSystem shopSystem;
    public RelicSystem relicSystem;

    [Header("Controllers")]
    public HUDController hudController;
    public HandController handController;
    public ShopController shopController;
    public UpgradeController upgradeController;
    public RelicController relicController;
    public DeckViewController deckViewController;
    public ResultController resultController;
    public SettingsUI settingsUI;
    public HandKeyInput handKeyInput;
    public PortraitController portraitController;
    public RelicHUDController relicHUDController;

    [SerializeField]
    private TickerController tickerController;

    public ScorePopupSpawner scorePopupSpawner;

    [Header("Upgrade Button (施設廃止の代替)")]
    public UpgradeButtonController upgradeButtonController;
    public int upgradeButtonBaseCost = 250;
    public float upgradeButtonCostRate = 1.35f;

    [Header("Stage Progress")]
    [SerializeField]
    private int maxStage = 10;

    [Header("Unityroom Score Submission")]
    [SerializeField]
    private int unityroomBoardNo = 1;

    [SerializeField]
    private bool sendScoreToUnityroom = true;

    public int CurrentStage { get; private set; } = 1;

    // state
    private float timeLeft;
    private float drawTimer;
    private int score;

    private bool ended;

    // ---- snapshots for retry ----
    private List<CardData> initialDeckSnapshot;
    private int initialGoal;
    private float initialStageTime;

    // ---- score per second (measured) ----
    private float scoreMeasureTimer = 0f;
    private int scoreAccumulatedThisSecond = 0;
    private int lastMeasuredScorePerSec = 0;

    // ---- upgrade button ----
    private int totalUpgradeCount = 0;

    // 一時バフ管理
    private float tempScoreMultiplier = 1f;
    private float tempScoreTimer = 0f;

    private readonly ModalGuard modalGuard = new();
    public ModalGuard ModalGuard => modalGuard;

    private readonly DeckSystem deck = new();
    private readonly List<CardData> hand = new();

    private GameContext ctx;

    void Start()
    {
        ctx = new GameContext(this);

        // 初期値保存（最初の1回だけ）
        initialDeckSnapshot = new List<CardData>(startingDeck);
        initialGoal = goal;
        initialStageTime = stageTime;

        // init controllers
        upgradeController.Init(modalGuard);
        relicController.Init(modalGuard);
        resultController.Init(modalGuard);

        shopController.Init(
            shopSystem,
            modalGuard,
            () => score,
            TryPayScore,
            card => startingDeck.Add(card)
        );

        deckViewController.Init(modalGuard, () => startingDeck);
        settingsUI.Init(modalGuard);

        if (handKeyInput != null)
        {
            handKeyInput.CanPlay = CanPlayCardByKey;
            handKeyInput.GetHandCount = () => hand.Count;
            handKeyInput.PlayHandIndex = i => PlayCard(hand[i]);
        }

        if (tickerController != null)
        {
            tickerController.SetProvider(() => BuildTickerMessages());
        }

        StartStage();
    }

    void Update()
    {
        if (ended)
            return;

        // timers
        timeLeft -= Time.deltaTime;
        drawTimer += Time.deltaTime;

        // ---- measure score/sec ----
        scoreMeasureTimer += Time.deltaTime;
        if (scoreMeasureTimer >= 1f)
        {
            scoreMeasureTimer -= 1f;
            lastMeasuredScorePerSec = scoreAccumulatedThisSecond;
            scoreAccumulatedThisSecond = 0;
        }

        // time draw
        float interval = drawInterval * relicSystem.DrawIntervalMultiplier;
        while (drawTimer >= interval)
        {
            drawTimer -= interval;
            DrawCards(1);
        }

        // HUD（手札は変更時のみ Render）
        hudController.Render(
            timeLeft,
            score,
            goal,
            lastMeasuredScorePerSec,
            relicSystem.GetAllOwnedCounts(),
            deck.DrawCount,
            deck.DiscardCount
        );

        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            EndStage(score >= goal);
        }

        // 一時スコアバフの減衰
        if (tempScoreTimer > 0f)
        {
            tempScoreTimer -= Time.deltaTime;
            if (tempScoreTimer <= 0f)
            {
                tempScoreMultiplier = 1f;
                tempScoreTimer = 0f;
            }
        }
    }

    void StartStage()
    {
        modalGuard.ForceReset();

        AudioManager.Instance?.PlayBGM(BGMType.Stage);
        RefreshRelicHUD();

        ended = false;

        timeLeft = stageTime + relicSystem.TimeBonus;
        drawTimer = 0f;
        score = 0;

        // 測定用もリセット（好み）
        scoreMeasureTimer = 0f;
        scoreAccumulatedThisSecond = 0;
        lastMeasuredScorePerSec = 0;

        deck.Init(startingDeck);
        hand.Clear();
        DrawCards(startingHand);

        // アップグレードボタン（ステージごとに回数リセット）
        if (upgradeButtonController != null)
        {
            upgradeButtonController.Init(
                getCost: GetUpgradeButtonCost,
                getScore: () => score,
                canUse: CanUseUpgradeButton,
                onClick: TryOpenUpgradeFromButton,
                getUses: null
            );
        }

        // render once
        hudController.Render(
            timeLeft,
            score,
            goal,
            lastMeasuredScorePerSec,
            relicSystem.GetAllOwnedCounts(),
            deck.DrawCount,
            deck.DiscardCount
        );
        handController.Render(hand, PlayCard);
    }

    bool CanPlayCardByKey()
    {
        if (ended)
            return false;
        if (modalGuard.IsLocked)
            return false;
        return true;
    }

    // ---- score/time api (used by GameContext / effects) ----

    public void AddScore(int amount, ScoreSource source = ScoreSource.Other)
    {
        float mul = 1f;

        // 全体倍率
        mul *= relicSystem.ScoreMultiplier;

        // カード由来だけ別倍率（施設依存倍率は廃止）
        if (source == ScoreSource.Card)
        {
            mul *= relicSystem.CardScoreMultiplier;
        }
        mul *= tempScoreMultiplier;

        int v = Mathf.RoundToInt(amount * mul);
        score += Mathf.Max(0, v);

        scoreAccumulatedThisSecond += v;
        shopController.RefreshOpenCostUI();
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
        bool changed = false;

        for (int i = 0; i < amount; i++)
        {
            if (hand.Count >= handLimit)
                break;

            var c = deck.DrawOne();
            if (c == null)
                break;

            hand.Add(c);
            AudioManager.Instance?.PlaySE(SEType.CardDraw);
            changed = true;
        }

        if (changed)
            handController.Render(hand, PlayCard);
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

    // ---- card play ----

    void PlayCard(CardData card)
    {
        if (ended)
            return;

        int before = score;

        var actor = (card.kind == CardKind.Cow) ? Actor.CowGirl : Actor.DogGirl;
        portraitController.React(card, actor);

        ctx.Multiplier = 1;
        ctx.ExhaustThisCard = false;

        foreach (var e in card.effects)
        {
            if (e == null)
                continue;
            e.Apply(ctx);
        }

        int gained = score - before;
        if (gained > 0)
            scorePopupSpawner?.Show(gained);

        hand.Remove(card);

        if (!ctx.ExhaustThisCard)
            deck.Discard(card);

        AudioManager.Instance?.PlaySE(SEType.CardPlay);
        handController.Render(hand, PlayCard);
    }

    // ---- upgrade button flow ----

    int GetUpgradeButtonCost()
    {
        return Mathf.CeilToInt(
            upgradeButtonBaseCost * Mathf.Pow(upgradeButtonCostRate, totalUpgradeCount)
        );
    }

    bool CanUseUpgradeButton()
    {
        if (ended)
            return false;
        if (modalGuard.IsLocked)
            return false;

        return true;
    }

    void TryOpenUpgradeFromButton()
    {
        if (!CanUseUpgradeButton())
            return;

        int cost = GetUpgradeButtonCost();
        if (!TryPayScore(cost))
        {
            AudioManager.Instance?.PlaySE(SEType.Error);
            return;
        }

        totalUpgradeCount++;
        // それっぽいSE（専用があれば差し替え推奨）
        AudioManager.Instance?.PlaySE(SEType.Buy);

        ShowUpgradeChoicesFromButton();
    }

    void ShowUpgradeChoicesFromButton()
    {
        var upgrades = upgradeSystem.RollUpgrades(3);
        var choices = new List<UpgradeChoice>();

        foreach (var up in upgrades)
        {
            if (startingDeck.Count == 0)
                continue;

            var target = startingDeck[Random.Range(0, startingDeck.Count)];
            choices.Add(new UpgradeChoice { upgrade = up, targetCard = target });
        }

        if (choices.Count == 0)
            return;

        upgradeController.Show(
            choices,
            choice =>
            {
                ApplyUpgrade(choice);
                ResumeAfterUpgrade();
            }
        );
    }

    void ApplyUpgrade(UpgradeChoice choice)
    {
        var target = choice.targetCard;
        int idx = startingDeck.IndexOf(target);
        if (idx < 0)
            return;

        var add = choice.upgrade.addEffect;
        Debug.Log($"[Upgrade] addEffect={(add == null ? "NULL" : add.name)}");

        var upgraded = upgradeSystem.CloneAndAddEffect(target, add, "+");
        startingDeck[idx] = upgraded;

        // 中身を確認
        for (int i = 0; i < upgraded.effects.Count; i++)
        {
            var e = upgraded.effects[i];
            Debug.Log($"[Upgrade] upgraded.effects[{i}]={(e == null ? "NULL" : e.name)}");
        }
    }

    void ResumeAfterUpgrade()
    {
        // 手札やHUDの再描画（必要なら）
        hudController.Render(
            timeLeft,
            score,
            goal,
            lastMeasuredScorePerSec,
            relicSystem.GetAllOwnedCounts(),
            deck.DrawCount,
            deck.DiscardCount
        );
        handController.Render(hand, PlayCard);
    }

    // ---- stage end / flow ----

    void EndStage(bool cleared)
    {
        ended = true;

        AudioManager.Instance?.PlaySE(cleared ? SEType.StageClear : SEType.GameOver);
        resultController.Show(
            cleared,
            CurrentStage,
            isFinal: false,
            onNext: () => ShowRelicChoices(),
            onRetry: () =>
            {
                ResetRun();
                StartStage();
            }
        );
    }

    void ShowRelicChoices()
    {
        CurrentStage++;

        if (CurrentStage > maxStage)
        {
            ShowFinalResult();
            return;
        }

        var relics = relicSystem.RollRelics(3);

        relicController.Show(
            relics,
            relic =>
            {
                relicSystem.AddRelic(relic);
                RefreshRelicHUD();

                goal = Mathf.RoundToInt(goal * 1.35f + 200);
                stageTime = Mathf.Max(60f, stageTime - 5f);
                StartStage();
            }
        );
    }

    void RefreshRelicHUD()
    {
        if (relicHUDController == null || relicSystem == null)
            return;

        relicHUDController.Refresh(relicSystem.GetAllOwnedCounts());
    }

    void ShowFinalResult()
    {
        ended = true;

        SendUnityroomScore(score);

        resultController.Show(
            true,
            CurrentStage,
            isFinal: true,
            onNext: () =>
            {
                SceneLoader.LoadMenu();
            },
            onRetry: () =>
            {
                ResetRun();
                StartStage();
            }
        );
    }

    void ResetRun()
    {
        CurrentStage = 1;

        goal = initialGoal;
        stageTime = initialStageTime;

        // デッキを初期に戻す
        startingDeck.Clear();
        startingDeck.AddRange(initialDeckSnapshot);

        // レリック初期化
        relicSystem.ResetRelics();
        shopSystem.ResetRun();

        // モーダル状態も初期化
        modalGuard.ForceReset();

        Debug.Log("[Run] ResetRun done.");
    }

    IEnumerable<string> BuildTickerMessages()
    {
        yield return $"現在ステージ {CurrentStage} です。";
        yield return "ステージ３をクリアするとゲームクリア！";
        yield return "時間切れになるとゲームオーバーだよ。";
        yield return "目標スコアを達成してステージを突破しよう！";
        yield return "スコアはカードで増えていくよ。";
        yield return "カードは使うと捨て札に行くよ。";
        yield return "山札がなくなると捨て札がシャッフルされるよ。";
        yield return "手札が上限を超えるとカードは引けないよ。";
        yield return "強化されたカードはデッキに永続的に残る！";
        yield return "消滅したカードは次のステージで復活するぞ！";
        yield return "カード効果は順番にすべて発動するよ。";
        yield return "アップグレードボタンでカードを強化できるぞ！";
        yield return "強化でとんでもないコンボが生まれるかも！？";
    }

    void SendUnityroomScore(int finalScore)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!sendScoreToUnityroom)
            return;

        UnityroomApiClient.Instance.SendScore(
            unityroomBoardNo,
            (float)finalScore,
            ScoreboardWriteMode.HighScoreDesc
        );
#else
        Debug.Log($"[unityroom] (dry-run) SendScore board={unityroomBoardNo} score={finalScore}");
#endif
    }

    public void AddTempScoreMultiplier(float multiplier, float duration)
    {
        tempScoreMultiplier *= multiplier;
        tempScoreTimer = Mathf.Max(tempScoreTimer, duration);
    }
    public int GetScore() => score;

}
