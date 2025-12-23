// Assets/Scripts/RelicData.cs
using UnityEngine;

public enum RelicType
{
    DrawSpeedUp,
    ScoreMultiplier,
    BuildingCostDown,
    TimeBonus
}

[CreateAssetMenu(menuName = "Relic/RelicData")]
public class RelicData : ScriptableObject
{
    public string relicName;
    [TextArea] public string description;
    public RelicType type;

    // 数値（用途に応じて解釈）
    public float value;
}
