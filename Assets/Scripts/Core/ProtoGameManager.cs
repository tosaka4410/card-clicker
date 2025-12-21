// Assets/Scripts/Core/ProtoGameManager.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ProtoGameManager : MonoBehaviour
{
    [Header("Run Params")]
    public float runDurationSec = 180f;
    public float drawIntervalSec = 1.2f;
    public int handLimit = 5;
    public int startHand = 3;

    [Header("Definitions")]
    public List<CardDef> initialDeck = new();
    public List<CardDef> cardPool = new(); // reward pool
    public List<FacilityDef> facilityPool = new();

    [Header("UI")]
    public ProtoUI ui;

    private RunState state;
    private Deck<CardDef> deck;
    private readonly List<CardDef> hand = new();
    private readonly List<FacilityInstance> facilities = new();

    private float drawTimer;
    private float eventTimer; // for 30s event
    private bool paused;

    // upgrade tracking: per-card repeat bonus (for "もうひとつ")
    private readonly Dictionary<string, int> repeatUpgrades = new();

    private const float EVENT_INTERVAL = 30f;

    private void Start()
    {
        StartRun();
    }

    public void StartRun()
    {
        state = new RunState(runDurationSec);
        deck = new Deck<CardDef>(initialDeck);
        hand.Clear();
        facilities.Clear();
        repeatUpgrades.Clear();

        drawTimer = 0f;
        eventTimer = 0f;
        paused = false;

        for (int i = 0; i < startHand; i++) DrawToHand();

        ui.Bind(this);
        ui.RefreshAll();
    }

    private void Update()
    {
        if (paused) return;

        float dt = Time.deltaTime;
        state.timeLeft -= dt;
        if (state.timeLeft <= 0f)
        {
            state.timeLeft = 0f;
            paused = true;
            ui.ShowRunEnd(state.score);
            return;
        }

        // Facility production
        double perSec = facilities.Sum(f => (double)f.PerSec);
        state.score += perSec * dt;

        // Timed draw
        drawTimer += dt;
        while (drawTimer >= drawIntervalSec)
        {
            drawTimer -= drawIntervalSec;
            if (hand.Count < handLimit) DrawToHand();
        }

        // Timed deck-management event
        eventTimer += dt;
        if (eventTimer >= EVENT_INTERVAL)
        {
            eventTimer -= EVENT_INTERVAL;
            paused = true;
            ui.ShowManageModal();
        }

        ui.RefreshTopBar(perSec);
        ui.RefreshHand();
        ui.RefreshFacilities();
    }

    private void DrawToHand()
    {
        var c = deck.Draw();
        if (c != null) hand.Add(c);
    }

    public IReadOnlyList<CardDef> GetHand() => hand;
    public IReadOnlyList<FacilityInstance> GetFacilities() => facilities;
    public RunState GetState() => state;

    public void PlayCard(int handIndex)
    {
        if (paused) return;
        if (handIndex < 0 || handIndex >= hand.Count) return;

        var card = hand[handIndex];
        float now = Time.time;

        if (state.IsOnCooldown(card, now)) return;

        // remove from hand -> discard
        hand.RemoveAt(handIndex);
        deck.AddToDiscard(card);

        // compute repeats (theme "もうひとつ")
        int repeat = 1;
        if (!string.IsNullOrEmpty(card.cardId) && repeatUpgrades.TryGetValue(card.cardId, out var r))
            repeat += r;
        repeat += card.repeatBonus;

        // apply effects repeat times
        for (int i = 0; i < repeat; i++)
            ApplyCardEffect(card);

        // consume next-card multiplier after first use
        state.nextCardMultiplier = 1;

        state.TriggerCooldown(card, now);

        ui.RefreshAll();
    }

    private void ApplyCardEffect(CardDef card)
    {
        if (card == null) return;

        int mult = Mathf.Max(1, state.nextCardMultiplier);

        // score
        if (card.scoreValue != 0)
            state.score += (double)(card.scoreValue * mult);

        // draw
        for (int i = 0; i < card.drawCount; i++)
            if (hand.Count < handLimit) DrawToHand();

        // buff
        if (card.nextCardMultiplier > 1)
            state.nextCardMultiplier = card.nextCardMultiplier;

        // facility build
        if (card.buildFacility && facilityPool.Count > 0)
        {
            var def = facilityPool[Random.Range(0, facilityPool.Count)];
            facilities.Add(new FacilityInstance { def = def, level = 1 });
        }

        // facility upgrade (lowest level first)
        if (card.upgradeFacilityCount > 0 && facilities.Count > 0)
        {
            for (int k = 0; k < card.upgradeFacilityCount; k++)
            {
                var target = facilities.OrderBy(f => f.level).FirstOrDefault();
                if (target == null) break;
                target.level += 1;
            }
        }
    }

    // ===== Manage modal actions (Add/Remove/Upgrade) =====

    public void ChooseAddCard()
    {
        // pick 3 random cards and let UI choose 1
        var options = cardPool.OrderBy(_ => Random.value).Take(3).ToList();
        ui.ShowAddChoice(options, onPick: (picked) =>
        {
            // add to discard so it appears soon, not immediately
            deck.AddToDiscard(picked);
            paused = false;
            ui.HideModal();
        });
    }

    public void ChooseRemoveCard()
    {
        // choose from all cards currently in deck (draw+discard); do not allow removing key cards if you want
        var all = deck.SnapshotAllCards();
        ui.ShowRemoveChoice(all, onPick: (picked) =>
        {
            // naive remove: rebuild deck without the picked instance
            RebuildDeckRemovingOne(picked);
            paused = false;
            ui.HideModal();
        });
    }

    public void ChooseUpgradeCard()
    {
        var all = deck.SnapshotAllCards();
        ui.ShowUpgradeChoice(all, onPick: (picked) =>
        {
            // upgrade = repeat +1 (もうひとつ)
            if (!string.IsNullOrEmpty(picked.cardId))
            {
                repeatUpgrades.TryGetValue(picked.cardId, out var r);
                repeatUpgrades[picked.cardId] = r + 1;
            }
            paused = false;
            ui.HideModal();
        });
    }

    private void RebuildDeckRemovingOne(CardDef toRemove)
    {
        // Simple: remove one occurrence from combined list, then create a fresh deck.
        var all = deck.SnapshotAllCards();
        bool removed = false;
        var newList = new List<CardDef>();
        foreach (var c in all)
        {
            if (!removed && c == toRemove)
            {
                removed = true;
                continue;
            }
            newList.Add(c);
        }
        deck = new Deck<CardDef>(newList);
        // hand remains as-is (player already has it). optionally also remove from hand.
    }

    // UI calls this when modal opens
    public void SetPaused(bool value) => paused = value;
}
