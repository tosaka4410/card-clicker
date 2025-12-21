// Assets/Scripts/Cards/CardDef.cs
using UnityEngine;

public enum CardRarity { Common, Uncommon, Rare }
public enum CardKind { Score, Draw, Buff, Facility }

[CreateAssetMenu(menuName = "Proto/Card")]
public class CardDef : ScriptableObject
{
    public string cardId;
    public string displayName;
    [TextArea] public string description;

    public CardKind kind = CardKind.Score;
    public CardRarity rarity = CardRarity.Common;

    public float cooldownSec = 0f;

    // Simple params (MVP)
    public int scoreValue = 0;          // add score
    public int drawCount = 0;           // draw cards
    public int nextCardMultiplier = 1;  // buff: next card multiplier

    // Facility ops (MVP)
    public bool buildFacility = false;  // build random facility
    public int upgradeFacilityCount = 0;// upgrade N facilities (lowest first)

    // Upgrade (MVP): how many extra times to repeat effect (the "もうひとつ")
    public int repeatBonus = 0;
}
