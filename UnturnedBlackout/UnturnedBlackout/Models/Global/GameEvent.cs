using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Models.Global;

[Serializable]
public class GameEvent
{
    public string EventName { get; set; }
    public string EventColor { get; set; }
    public int EventWeight { get; set; }
    
    public bool IsHardcore { get; set; }

    [XmlArrayItem("IgnoredLocation")]
    public List<int> IgnoredLocations { get; set; }
    [XmlArrayItem("IgnoredGameMode")]
    public List<EGameType> IgnoredGameModes { get; set; }
    
    public GameEvent()
    {
    }
}