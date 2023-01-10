using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Models.Global;

[Serializable]
public class GameEvent
{
    public int EventID { get; set; }
    public string EventName { get; set; }
    public string EventColor { get; set; }
    public int EventWeight { get; set; }
    public int EventLimit { get; set; }
    
    public bool IsHardcore { get; set; }
    
    [XmlArrayItem("AllowedGunType")]
    public List<EGun> AllowedGunTypes { get; set; }
    [XmlArrayItem("BlacklistedGun")]
    public List<ushort> BlacklistedGuns { get; set; }
    
    public CustomGun OverridePrimary { get; set; }
    
    public bool AllowPrimary { get; set; }
    public bool AllowSecondary { get; set; }
    public bool KnifeDoesDamage { get; set; }
    public bool AllowKillstreaks { get; set; }
    public bool AllowDeathstreaks { get; set; }
    public bool AllowAbility { get; set; }
    public bool AllowTactical { get; set; }
    public bool AllowLethal { get; set; }
    public bool AllowPerks { get; set; }
    
    public bool AlwaysHaveLobby { get; set; }
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
    public float XPMultiplier { get; set; }
    public float GunXPMultiplier { get; set; }
    
    [XmlArrayItem("IgnoredLocation")]
    public List<int> IgnoredLocations { get; set; }
    [XmlArrayItem("IgnoredGameMode")]
    public List<EGameType> IgnoredGameModes { get; set; }
}