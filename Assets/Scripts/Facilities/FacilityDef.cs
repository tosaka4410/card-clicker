// Assets/Scripts/Facilities/FacilityDef.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Proto/Facility")]
public class FacilityDef : ScriptableObject
{
    public string facilityId;
    public string displayName;
    public float basePerSec = 1f;
}
