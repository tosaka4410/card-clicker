using System.Collections.Generic;
using UnityEngine;

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

    // state
    private float timeLeft;
    private float drawTimer;
    private int score;
    private float autoScoreBuffer;

    private bool ended;

    private readonly ModalGuard modalGuard = new();
    public ModalGuard ModalGuard => modalGuard;

    private readonly DeckSystem deck = new();
    private readonly List<CardData> hand = new();
    private BuildingSystem buildings = new();

    private GameContext ctx;

    void Start()
    {
        ctx = new GameContext(this);

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
        float sps = buildings.GetTotalScorePerSec(buildingDefs);
        autoScoreBuffer += sps * Time.deltaTime;
        int add = Mathf.FloorToInt(autoScoreBuffer);
        if (add > 0)
        {
            autoScoreBuffer -= add;
            AddScore(add);
        }

        // time draw
        float interval = drawInterval * relicSystem.DrawIntervalMultiplier;
        while (drawTimer >= interval)
        {
            drawTimer -= interval;
            DrawCards(1);
        }

        // HUD + build labels only（手札は変更時にのみ）
        hudController.Render(timeLeft, score, goal, relicSystem.GetAllOwnedCounts());
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
        hudController.Render(timeLeft, score, goal, relicSystem.GetAllOwnedCounts());
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

    public void AddScore(int amount)
    {
        int v = Mathf.RoundToInt(amount * relicSystem.ScoreMultiplier);
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

        AudioManager.Instance?.PlaySE(SEType.CardPlay);

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
            onRetry: () => StartStage()
        );
    }

    void ShowRelicChoices()
    {
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

        var upgraded = upgradeSystem.CloneAndAddEffect(target, choice.upgrade.addEffect, "+");
        startingDeck[idx] = upgraded;
    }

    void ResumeAfterUpgrade()
    {
        // ModalGuard / UpgradeController が Unlock している前提ならここは不要
        // 念のため入れるなら：
        // modalGuard.ForceReset();

        // 手札やHUDの再描画（必要なら）
        hudController.Render(timeLeft, score, goal, relicSystem.GetAllOwnedCounts());
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
}
