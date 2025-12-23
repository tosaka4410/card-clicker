// Assets/Scripts/RelicSystem.cs
using System.Collections.Generic;
using UnityEngine;

public class RelicSystem : MonoBehaviour
{
    public List<RelicData> relicPool = new();

    private readonly List<RelicData> ownedRelics = new();

    // 効果用の集計値
    public float drawIntervalMultiplier { get; private set; } = 1f;
    public float scoreMultiplier { get; private set; } = 1f;
    public float buildingCostMultiplier { get; private set; } = 1f;
    public float timeBonus { get; private set; } = 0f;

    public void AddRelic(RelicData relic)
    {
        ownedRelics.Add(relic);

        switch (relic.type)
        {
            case RelicType.DrawSpeedUp:
                drawIntervalMultiplier *= relic.value;
                break;

            case RelicType.ScoreMultiplier:
                scoreMultiplier *= relic.value;
                break;

            case RelicType.BuildingCostDown:
                buildingCostMultiplier *= relic.value;
                break;

            case RelicType.TimeBonus:
                timeBonus += relic.value;
                break;
        }

        Debug.Log($"[Relic] Gained {relic.relicName}");
    }

    public List<RelicData> RollRelics(int count = 3)
    {
        var result = new List<RelicData>();
        var temp = new List<RelicData>(relicPool);

        for (int i = 0; i < count && temp.Count > 0; i++)
        {
            int idx = Random.Range(0, temp.Count);
            result.Add(temp[idx]);
            temp.RemoveAt(idx);
        }
        return result;
    }
}
