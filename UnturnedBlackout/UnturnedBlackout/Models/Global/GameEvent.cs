using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Models.Global;

[Serializable]
public class GameEvent
{
    public int EventID { get; set; }
    public int EventWeight { get; set; }
    public string EventName { get; set; }
    public string EventColor { get; set; }
    
    public bool IsHardcore { get; set; }

    [XmlArrayItem("LocationID")]
    public List<int> IgnoredLocations { get; set; }
    [XmlArrayItem("Gamemode")]
    public List<EGameType> IgnoredGameModes { get; set; }
    
    public GameEvent()
    {
    }
}