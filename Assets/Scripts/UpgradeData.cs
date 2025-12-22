// Assets/Scripts/UpgradeData.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Upgrade/UpgradeData")]
public class UpgradeData : ScriptableObject
{
    public string title;
    [TextArea] public string description;

    // 適用される効果
    public CardEffect addEffect;   // カードに追加するEffect
}
