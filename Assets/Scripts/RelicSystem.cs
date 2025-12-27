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
    public float CardScoreMultiplier { get; private set; } = 1f;

    private Dictionary<RelicType, Action<float>> appliers;
    private readonly Dictionary<string, float> buildingDpsMul = new();
    private readonly List<RelicData> cardPerBuildingRelics = new();

    void Awake()
    {
        appliers = new Dictionary<RelicType, Action<float>>
        {
            { RelicType.DrawSpeedUp, v => DrawIntervalMultiplier *= v },
            { RelicType.ScoreMultiplier, v => ScoreMultiplier *= v },
            { RelicType.BuildingCostDown, v => BuildingCostMultiplier *= v },
            { RelicType.TimeBonus, v => TimeBonus += v },
            { RelicType.CardScoreMultiplier, v => CardScoreMultiplier *= v },
        };
    }

    public void AddRelic(RelicData relic)
    {
        if (relic == null)
            return;

        ownedRelics.Add(relic);

        if (relic.type == RelicType.CardScorePerBuilding)
        {
            cardPerBuildingRelics.Add(relic);
            return;
        }
        if (relic.type == RelicType.BuildingDpsMultiplier)
        {
            if (string.IsNullOrEmpty(relic.targetBuildingId))
                return;

            float cur = buildingDpsMul.TryGetValue(relic.targetBuildingId, out var m) ? m : 1f;
            buildingDpsMul[relic.targetBuildingId] = cur * relic.value;
            return;
        }

        if (appliers.TryGetValue(relic.type, out var apply))
            apply(relic.value);
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

    public float GetBuildingDpsMultiplier(string buildingId)
    {
        if (string.IsNullOrEmpty(buildingId))
            return 1f;
        return buildingDpsMul.TryGetValue(buildingId, out var m) ? m : 1f;
    }

    public float GetDynamicCardScoreMultiplier(Func<string, int> getActiveBuildingCount)
    {
        float mul = 1f;

        foreach (var r in cardPerBuildingRelics)
        {
            if (r == null || string.IsNullOrEmpty(r.targetBuildingId))
                continue;

            int n = getActiveBuildingCount?.Invoke(r.targetBuildingId) ?? 0;

            // 例：n個で (1 + value*n) 倍  ※ value=0.1なら10%ずつ
            mul *= (1f + r.value * n);
        }

        return mul;
    }
}
