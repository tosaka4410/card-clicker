// Assets/Scripts/RelicData.cs
using UnityEngine;

public enum RelicType
{
    DrawSpeedUp,
    ScoreMultiplier,
    BuildingCostDown,
    TimeBonus,
    CardScoreMultiplier,
    BuildingDpsMultiplier, 
    CardScorePerBuilding,
}

[CreateAssetMenu(menuName = "Relic/RelicData")]
public class RelicData : ScriptableObject
{
    public string relicName;
    [TextArea] public string description;
    public RelicType type;

    // 数値（用途に応じて解釈）
    public float value;
    [Header("Visual")]
    public Sprite icon; 
    [Header("Optional Target")]
    public string targetBuildingId;
}
