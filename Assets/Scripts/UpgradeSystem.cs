using System.Collections.Generic;
using UnityEngine;

public class UpgradeSystem : MonoBehaviour
{
    [Header("追加効果プール（ここから付与）")]
    public List<CardEffect> possibleEffects = new();
    public List<UpgradeData> upgradePool = new();

    public CardData CloneAndAddEffect(CardData original, CardEffect addEffect)
    {
        // Runtime clone（アセットは汚れない）
        var clone = ScriptableObject.Instantiate(original);
        clone.cardName = original.cardName + " +";
        clone.effects = new List<CardEffect>(original.effects);
        clone.effects.Add(addEffect);
        return clone;
    }

    public List<UpgradeData> RollUpgrades(int count = 3)
    {
        var results = new List<UpgradeData>();

        if (upgradePool.Count == 0)
            return results;

        var temp = new List<UpgradeData>(upgradePool);

        for (int i = 0; i < count && temp.Count > 0; i++)
        {
            int idx = Random.Range(0, temp.Count);
            results.Add(temp[idx]);
            temp.RemoveAt(idx); // 重複防止
        }

        return results;
    }
}
