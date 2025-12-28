// Assets/Scripts/GameManager.cs
using System;
using System.Collections;
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
    private CountdownController countdownController;

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
    private long score;

    private bool ended;

    // ---- snapshots for retry ----
    private List<CardData> initialDeckSnapshot;
    private int initialGoal;
    private float initialStageTime;

    // ---- score per second (measured) ----
    private float scoreMeasureTimer = 0f;
    private long scoreAccumulatedThisSecond = 0;
    private long lastMeasuredScorePerSec = 0;

    // ---- upgrade button ----
    private int totalUpgradeCount = 0;

    // 一時バフ管理
    private float tempScoreMultiplier = 1f;
    private float tempScoreTimer = 0f;

    private readonly ModalGuard modalGuard = new();
    public ModalGuard ModalGuard => modalGuard;

    private bool isStarting = true;
    private float startCountdownTime = 4f;
    private bool startCountdownSePlayed = false;

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

        if (isStarting)
        {
            return;
        }

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
        if (countdownController != null)
        {
            bool isGoalMet = score >= goal;
            countdownController.UpdateTime(timeLeft, isGoalMet);
        }
    }

    void StartStage()
    {
        modalGuard.ForceReset();
        modalGuard.Lock();

        AudioManager.Instance?.PlayBGM(BGMType.Stage);
        RefreshRelicHUD();

        ended = false;

        isStarting = true;
        countdownController?.PlayStartCountdown(
            on3: () => AudioManager.Instance?.PlaySE(SEType.StageStart),
            on2: null,
            on1: null
        );
        StartCoroutine(CoStartUnlock());

        timeLeft = stageTime + relicSystem.TimeBonus;
        drawTimer = 0f;

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

        // ショップの開放コスト更新
        shopController.RefreshOpenCostUI();
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

    public void AddScore(long amount, ScoreSource source = ScoreSource.Other)
    {
        double mul = 1.0;

        // 全体倍率
        mul *= relicSystem.ScoreMultiplier;

        // カード由来だけ別倍率
        if (source == ScoreSource.Card)
            mul *= relicSystem.CardScoreMultiplier;

        mul *= tempScoreMultiplier;

        long v = (long)System.Math.Round(amount * mul);

        if (v < 0)
            v = 0;

        score += v;
        scoreAccumulatedThisSecond += v;

        shopController.RefreshOpenCostUI();
    }

    public bool TryPayScore(int amount)
    {
        if (score < amount)
            return false;

        score -= amount;

        shopController.RefreshOpenCostUI(); // ★追加
        return true;
    }

    public long ConsumeAllScore()
    {
        long lost = score;
        score = 0;

        shopController.RefreshOpenCostUI(); // ★追加
        return lost;
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

    // ---- card play ----

    void PlayCard(CardData card)
    {
        if (ended)
            return;

        long before = score;

        var actor = (card.kind == CardKind.Cow) ? Actor.CowGirl : Actor.DogGirl;
        portraitController.React(card, actor);

        hand.Remove(card);

        ctx.Multiplier = 1;
        ctx.ExhaustThisCard = false;

        foreach (var e in card.effects)
        {
            if (e == null)
                continue;
            e.Apply(ctx);
        }

        long gained = score - before;
        if (gained > 0)
            scorePopupSpawner?.Show(gained);

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

        shopController.RefreshOpenCostUI();
    }

    void ShowUpgradeChoicesFromButton()
    {
        var upgrades = upgradeSystem.RollUpgrades(3);
        var choices = new List<UpgradeChoice>();

        foreach (var up in upgrades)
        {
            if (startingDeck.Count == 0)
                continue;

            var target = startingDeck[UnityEngine.Random.Range(0, startingDeck.Count)];
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
        shopController.RefreshOpenCostUI();
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

                // goal = Mathf.RoundToInt(goal * 1.35f + 200);
                goal = GetGoalForStage(CurrentStage);
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

        SendUnityroomScore((int)MathF.Min(score, int.MaxValue));

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

        // アップグレード初期化
        totalUpgradeCount = 0;

        // ショップ初期化
        shopSystem.ResetRun();

        // モーダル状態も初期化
        modalGuard.ForceReset();

        // スコアリセット
        score = 0;

        Debug.Log("[Run] ResetRun done.");
    }

    IEnumerable<string> BuildTickerMessages()
    {
        yield return $"現在ステージ {CurrentStage} 。";
        yield return "ステージは全部で３つ！";
        yield return "山札がなくなると捨て札がシャッフルされるよ！";
        yield return "手札は５枚が上限だよ！";
        yield return "消滅したカードは次のステージで復活するよ！";
        yield return "カード効果は順番にすべて発動するよ！";
        yield return "アップグレードは積極的にしよう！";
        yield return "とんでもないコンボが生まれるかも！？";
        yield return "ドローは５秒ごとに自動で行われるよ！";
        yield return "「効果が倍増する」効果は時間を増やす効果にも適用されるよ！";
        yield return "「効果が倍増する」効果は重複するよ！";
        yield return "ショップでしか手に入らない効果もあるよ！";
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

    public long GetScore() => score;

    private IEnumerator CoStartUnlock()
    {
        // 3秒 + START表示0.35秒 と同じにしておく（上の実装と合わせる）
        yield return new WaitForSecondsRealtime(3f + 0.35f);
        isStarting = false;
        modalGuard.Unlock(); 
    }

    private int GetGoalForStage(int stage)
    {
        // デフォルトは今のgoal（成長式を使いたいならここに入れる）
        int g = goal;

        if (stage == 2)
            return 1000;
        if (stage == 3)
            return 10000;

        return g;
    }
}
