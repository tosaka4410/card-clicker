// Assets/Scripts/Facilities/FacilityInstance.cs
using System;

[Serializable]
public class FacilityInstance
{
    public FacilityDef def;
    public int level = 1;

    public float PerSec => def == null ? 0f : def.basePerSec * level;
}
