using System;
using System.Collections.Generic;
using UnityEngine;

public class RelicSystem : MonoBehaviour
{
    public List<RelicData> relicPool = new();

    private readonly List<RelicData> ownedRelics = new();

    // ★追加：レリックごとの所持数
    private readonly Dictionary<RelicData, int> ownedCounts = new();

    public int OwnedRelicCount => ownedRelics.Count;

    // ★追加：特定レリックの所持数
    public int GetOwnedCount(RelicData relic)
    {
        if (relic == null)
            return 0;
        return ownedCounts.TryGetValue(relic, out var c) ? c : 0;
    }

    // ★追加：HUD表示などで使う（コピーを返して安全に）
    public IReadOnlyDictionary<RelicData, int> GetAllOwnedCounts() =>
        new Dictionary<RelicData, int>(ownedCounts);

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
        if (relic == null)
            return;

        ownedRelics.Add(relic);

        // ★追加：カウント更新
        ownedCounts[relic] = GetOwnedCount(relic) + 1;

        if (appliers.TryGetValue(relic.type, out var apply))
            apply(relic.value);

        Debug.Log($"[Relic] Gained {relic.relicName} (x{GetOwnedCount(relic)})");
    }

    public List<RelicData> RollRelics(int count = 3) => RandomPicker.PickUnique(relicPool, count);

    public void ResetRelics()
    {
        ownedRelics.Clear();

        DrawIntervalMultiplier = 1f;
        ScoreMultiplier = 1f;
        BuildingCostMultiplier = 1f;
        TimeBonus = 0f;

        Debug.Log("[Relic] Reset");
    }
}
