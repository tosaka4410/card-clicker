// Assets/Scripts/Core/RunState.cs
using System.Collections.Generic;
using UnityEngine;

public class RunState
{
    public float timeLeft;
    public double score;

    // Buff: next card multiplier
    public int nextCardMultiplier = 1;

    // Card cooldowns by id
    private readonly Dictionary<string, float> cdUntil = new();

    public RunState(float durationSec)
    {
        timeLeft = durationSec;
    }

    public bool IsOnCooldown(CardDef card, float now)
    {
        if (card == null || string.IsNullOrEmpty(card.cardId)) return false;
        return cdUntil.TryGetValue(card.cardId, out var until) && now < until;
    }

    public void TriggerCooldown(CardDef card, float now)
    {
        if (card == null || card.cooldownSec <= 0f || string.IsNullOrEmpty(card.cardId)) return;
        cdUntil[card.cardId] = now + card.cooldownSec;
    }
}
