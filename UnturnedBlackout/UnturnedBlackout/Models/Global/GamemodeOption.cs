using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Models.Global;

[Serializable]
public class GamemodeOption
{
    public EGameType GameType { get; set; }
    public string GamemodeIcon { get; set; }
    public int GamemodeWeight { get; set; }
    public string GamemodeColor { get; set; }
    public int EventChance { get; set; }

    [XmlArrayItem(ElementName = "IgnoredLocation")]
    public List<int> IgnoredLocations { get; set; }

    public GamemodeOption()
    {
    }
}