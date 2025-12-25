using System.Collections.Generic;
using UnityEngine;

public class UpgradeSystem : MonoBehaviour
{
    [Header("追加効果プール（ここから付与）")]
    public List<CardEffect> possibleEffects = new();
    public List<UpgradeData> upgradePool = new();

    public CardData CloneAndAddEffect(CardData original, CardEffect addEffect, string suffix)
    {
        var clone = ScriptableObject.Instantiate(original);
        clone.cardName = $"{original.cardName}{suffix}";
        clone.effects = new List<CardEffect>(original.effects) { addEffect };
        return clone;
    }

    public List<UpgradeData> RollUpgrades(int count = 3) =>
        RandomPicker.PickUnique(upgradePool, count);
}
