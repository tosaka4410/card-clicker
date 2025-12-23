// Assets/Scripts/BuildingSystem.cs
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BuildingDef
{
    public string id = "Outpost";
    public int baseCost = 80;
    public float scorePerSec = 2f;
}

public class BuildingSystem
{
    public float rate = 0.6f;
    public int totalBuiltCount { get; private set; } = 0;

    // 同種建設数（ステージ跨ぎで保持する前提）
    private readonly Dictionary<string, int> builtCount = new();
    private readonly Dictionary<string, int> activeCount = new(); // 今ランで稼働している数

    public int GetBuiltCount(string id) => builtCount.TryGetValue(id, out var c) ? c : 0;

    public int GetActiveCount(string id) => activeCount.TryGetValue(id, out var c) ? c : 0;

    public int GetCost(BuildingDef def, float costMultiplier = 1f)
    {
        int n = GetBuiltCount(def.id);
        float raw = def.baseCost * (1f + n * rate);
        return Mathf.CeilToInt(raw * costMultiplier);
    }

    public bool TryBuild(BuildingDef def, float costMultiplier, System.Func<int, bool> tryPay)
    {
        int cost = GetCost(def, costMultiplier);
        if (!tryPay(cost))
            return false;

        builtCount[def.id] = GetBuiltCount(def.id) + 1;
        activeCount[def.id] = GetActiveCount(def.id) + 1;
        totalBuiltCount++;
        return true;
    }

    public float GetTotalScorePerSec(List<BuildingDef> defs)
    {
        float total = 0f;
        foreach (var def in defs)
            total += def.scorePerSec * GetActiveCount(def.id);
        return total;
    }

    // ステージ切替で「稼働数」は持ち越す仕様なら空にしない。
    // MVPでは稼働も持ち越しにして気持ちよさ優先でOK。
}
