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
    public List<BuildingDef> buildingDefs = new();

    [Header("Systems")]
    public UpgradeSystem upgradeSystem;
    public ShopSystem shopSystem;
    public RelicSystem relicSystem;

    [Header("Building Milestone")]
    public int buildingMilestoneStep = 10;
    public float buildingBonusRate = 1.15f;
    private readonly Dictionary<string, int> nextBuildingMilestone = new();

    [Header("Controllers")]
    public HUDController hudController;
    public HandController handController;
    public BuildingPanelController buildingPanelController;
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
    private float autoScoreBuffer;

    private bool ended;

    // ---- snapshots for retry ----
    private List<CardData> initialDeckSnapshot;
    private Dictionary<string, float> initialBuildingSps;
    private int initialGoal;
    private float initialStageTime;

    // ---- score per second (measured) ----
    private float scoreMeasureTimer = 0f;
    private int scoreAccumulatedThisSecond = 0;
    private int lastMeasuredScorePerSec = 0;

    private readonly ModalGuard modalGuard = new();
    public ModalGuard ModalGuard => modalGuard;

    private readonly DeckSystem deck = new();
    private readonly List<CardData> hand = new();
    private BuildingSystem buildings = new();

    private GameContext ctx;

    void Start()
    {
        ctx = new GameContext(this);

        // 初期値保存（最初の1回だけ）
        initialDeckSnapshot = new List<CardData>(startingDeck);
        initialBuildingSps = new Dictionary<string, float>();
        foreach (var def in buildingDefs)
            if (def != null)
                initialBuildingSps[def.id] = def.scorePerSec;

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

        // auto score
        float sps = buildings.GetTotalScorePerSec(buildingDefs, relicSystem);

        autoScoreBuffer += sps * Time.deltaTime;
        int add = Mathf.FloorToInt(autoScoreBuffer);
        if (add > 0)
        {
            autoScoreBuffer -= add;
            AddScore(add, ScoreSource.Auto);
        }

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

        // HUD + build labels only（手札は変更時にのみ）
        hudController.Render(
            timeLeft,
            score,
            goal,
            lastMeasuredScorePerSec,
            relicSystem.GetAllOwnedCounts()
        );
        buildingPanelController.UpdateLabels(
            buildingDefs,
            def => buildings.GetCost(def, relicSystem.BuildingCostMultiplier),
            def => buildings.GetActiveCount(def.id),
            () => score,
            () => ended
        );

        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            EndStage(score >= goal);
        }
    }

    void StartStage()
    {
        modalGuard.ForceReset();

        AudioManager.Instance?.PlayBGM(BGMType.Stage);
        RefreshRelicHUD();

        ended = false;

        timeLeft = stageTime + relicSystem.TimeBonus; // TimeBonusを使うなら
        drawTimer = 0f;
        score = 0;
        autoScoreBuffer = 0f;

        deck.Init(startingDeck);
        hand.Clear();
        DrawCards(startingHand);

        InitBuildingMilestones();

        buildingPanelController.BuildButtons(
            buildingDefs,
            def =>
            {
                if (ended)
                    return false;

                bool ok = buildings.TryBuild(def, relicSystem.BuildingCostMultiplier, TryPayScore);
                if (ok)
                {
                    AudioManager.Instance?.PlaySE(SEType.Build);
                    CheckBuildingMilestone(def);
                }
                return ok;
            }
        );

        // render once
        hudController.Render(
            timeLeft,
            score,
            goal,
            lastMeasuredScorePerSec,
            relicSystem.GetAllOwnedCounts()
        );
        handController.Render(hand, PlayCard);
        buildingPanelController.UpdateLabels(
            buildingDefs,
            def => buildings.GetCost(def, relicSystem.BuildingCostMultiplier),
            def => buildings.GetActiveCount(def.id),
            () => score,
            () => ended
        );
    }

    bool CanPlayCardByKey()
    {
        if (ended)
            return false;
        if (modalGuard.IsLocked)
            return false; // ここが肝
        return true;
    }

    // ---- score/time api (used by GameContext / effects) ----

    public void AddScore(int amount, ScoreSource source = ScoreSource.Other)
    {
        float mul = 1f;

        // 既存：全体倍率（将来の別レリック等に使える）
        mul *= relicSystem.ScoreMultiplier;

        // カード由来だけ別倍率
        if (source == ScoreSource.Card)
        {
            mul *= relicSystem.CardScoreMultiplier;
            mul *= relicSystem.GetDynamicCardScoreMultiplier(id => buildings.GetActiveCount(id));
        }

        int v = Mathf.RoundToInt(amount * mul);
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

        int before = score; // ★ここで記録

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

        int gained = score - before; // ★カード1枚の純増
        if (gained > 0)
            scorePopupSpawner?.Show(gained);

        hand.Remove(card);

        if (!ctx.ExhaustThisCard)
            deck.Discard(card);

        handController.Render(hand, PlayCard);
    }

    // ---- stage end / flow ----

    void EndStage(bool cleared)
    {
        ended = true;

        AudioManager.Instance?.PlaySE(cleared ? SEType.StageClear : SEType.GameOver);
        resultController.Show(
            cleared,
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
        // ここでステージを進める
        CurrentStage++;

        // 10ステージ超えたら終了
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

                goal = Mathf.RoundToInt(goal * 1.35f + 200);
                stageTime = Mathf.Max(60f, stageTime - 5f);
                StartStage();
            }
        );
    }

    // ---- building milestone -> upgrade ----

    void InitBuildingMilestones()
    {
        nextBuildingMilestone.Clear();

        foreach (var def in buildingDefs)
        {
            int built = buildings.GetBuiltCount(def.id);

            // built=0..9 -> next=10
            // built=10..19 -> next=20
            // built=20..29 -> next=30
            int next = ((built / buildingMilestoneStep) + 1) * buildingMilestoneStep;

            nextBuildingMilestone[def.id] = next;
        }
    }

    void CheckBuildingMilestone(BuildingDef def)
    {
        int built = buildings.GetBuiltCount(def.id);
        int next = nextBuildingMilestone[def.id];
        if (built < next)
            return;

        nextBuildingMilestone[def.id] += buildingMilestoneStep;
        OnBuildingMilestoneReached(def, built);
    }

    void OnBuildingMilestoneReached(BuildingDef def, int builtCount)
    {
        // facility buff
        def.scorePerSec *= buildingBonusRate;

        // create choices
        var upgrades = upgradeSystem.RollUpgrades(3);
        var choices = new List<UpgradeChoice>();

        foreach (var up in upgrades)
        {
            if (startingDeck.Count == 0)
                continue;
            var target = startingDeck[Random.Range(0, startingDeck.Count)];
            choices.Add(new UpgradeChoice { upgrade = up, targetCard = target });
        }

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
        // ModalGuard / UpgradeController が Unlock している前提ならここは不要
        // 念のため入れるなら：
        // modalGuard.ForceReset();

        // 手札やHUDの再描画（必要なら）
        hudController.Render(
            timeLeft,
            score,
            goal,
            lastMeasuredScorePerSec,
            relicSystem.GetAllOwnedCounts()
        );
        handController.Render(hand, PlayCard);

        // 建物ボタンの表示も更新
        buildingPanelController.UpdateLabels(
            buildingDefs,
            def => buildings.GetCost(def, relicSystem.BuildingCostMultiplier),
            def => buildings.GetActiveCount(def.id),
            () => score,
            () => ended
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
            onNext: () =>
            {
                // 例：メニューに戻す
                SceneLoader.LoadMenu();
            },
            onRetry: () =>
            {
                // 最初からやり直す
                ResetRun();
                StartStage();
            }
        );
    }

    void ResetRun()
    {
        // ステージ進行（もし使ってるなら）
        CurrentStage = 1;

        // ゴール/制限時間を初期に戻す
        goal = initialGoal;
        stageTime = initialStageTime;

        // ★デッキを初期に戻す（参照を戻すだけでOK）
        startingDeck.Clear();
        startingDeck.AddRange(initialDeckSnapshot);

        // ★建物の「強化された scorePerSec」を初期値に戻す
        foreach (var def in buildingDefs)
        {
            if (def == null)
                continue;
            if (initialBuildingSps.TryGetValue(def.id, out var sps))
                def.scorePerSec = sps;
        }

        // ★建物システムを初期化（builtCount/activeCount/totalBuiltCountリセット）
        buildings = new BuildingSystem();

        // ★レリック初期化
        relicSystem.ResetRelics();

        // ついで：モーダル状態も初期化
        modalGuard.ForceReset();

        Debug.Log("[Run] ResetRun done.");
    }

    IEnumerable<string> BuildTickerMessages()
    {
        yield return "現在ステージ {stage} です。";
        yield return "ステージ10をクリアするとゲームクリア！";
        yield return "時間切れになるとゲームオーバーだよ。";
        yield return "目標スコアを達成してステージを突破しよう！";
        yield return "スコアは施設とカードで増えていくよ。";
        yield return "同じ施設をたくさん建てると建設コストが上がるぞ。";
        yield return "カードは使うと捨て札に行くよ。";
        yield return "山札がなくなると捨て札がシャッフルされるよ。";
        yield return "手札が上限を超えるとカードは引けないよ。";
        yield return "強化されたカードはデッキに永続的に残る！";
        yield return "消滅したカードは次のステージで復活するぞ！";
        yield return "カード効果は順番にすべて発動するよ。";
        yield return "DPSは Drink Per Second の略だよ！";
        yield return "建物は毎秒スコアを生み出すぞ。";
        yield return "建物を10個建てるとカードのアップグレードが発生！";
        yield return "ステージをまたいでも建物は引き継がれるよ。";
        yield return "建物は10個建てるごとに性能が強化されるぞ！";
    }

    void SendUnityroomScore(int finalScore)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!sendScoreToUnityroom)
            return;

        // unityroom用
        // using unityroom.Api; が必要
        UnityroomApiClient.Instance.SendScore(
            unityroomBoardNo,
            (float)finalScore,
            ScoreboardWriteMode.HighScoreDesc // 例：ハイスコア（降順）
        );
#else
        Debug.Log($"[unityroom] (dry-run) SendScore board={unityroomBoardNo} score={finalScore}");
#endif
    }
}
