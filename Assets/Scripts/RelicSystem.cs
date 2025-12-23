using System;
using System.Collections.Generic;
using UnityEngine;

public class RelicSystem : MonoBehaviour
{
    public List<RelicData> relicPool = new();

    private readonly List<RelicData> ownedRelics = new();

    public float DrawIntervalMultiplier { get; private set; } = 1f;
    public float ScoreMultiplier { get; private set; } = 1f;
    public float BuildingCostMultiplier { get; private set; } = 1f;
    public float TimeBonus { get; private set; } = 0f;

    private Dictionary<RelicType, Action<float>> appliers;

    void Awake()
    {
        appliers = new Dictionary<RelicType, Action<float>>
        {
            { RelicType.DrawSpeedUp, v => DrawIntervalMultiplier *= v },
            { RelicType.ScoreMultiplier, v => ScoreMultiplier *= v },
            { RelicType.BuildingCostDown, v => BuildingCostMultiplier *= v },
            { RelicType.TimeBonus, v => TimeBonus += v },
        };
    }

    public void AddRelic(RelicData relic)
    {
        if (relic == null) return;

        ownedRelics.Add(relic);

        if (appliers.TryGetValue(relic.type, out var apply))
            apply(relic.value);

        Debug.Log($"[Relic] Gained {relic.relicName}");
    }

    public List<RelicData> RollRelics(int count = 3)
        => RandomPicker.PickUnique(relicPool, count);
}
