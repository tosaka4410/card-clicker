using System.Collections.Generic;
using UnityEngine;

public class UpgradeSystem : MonoBehaviour
{
    [Header("追加効果プール（ここから付与）")]
    public List<CardEffect> possibleEffects = new();

    public List<CardEffect> Roll3Effects()
    {
        // 雑に3つ選ぶ（重複は許す or 後で禁止）
        var results = new List<CardEffect>();
        if (possibleEffects.Count == 0) return results;

        for (int i = 0; i < 3; i++)
        {
            var e = possibleEffects[Random.Range(0, possibleEffects.Count)];
            results.Add(e);
        }
        return results;
    }

    public CardData CloneAndAddEffect(CardData original, CardEffect addEffect)
    {
        // Runtime clone（アセットは汚れない）
        var clone = ScriptableObject.Instantiate(original);
        clone.cardName = original.cardName + " +";
        clone.effects = new List<CardEffect>(original.effects);
        clone.effects.Add(addEffect);
        return clone;
    }
}
