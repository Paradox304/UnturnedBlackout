using System.Collections.Generic;
using System.Xml.Serialization;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Models.Global
{
    public class GamemodeOption
    {
        public EGameType GameType { get; set; }
        public int GamemodeWeight { get; set; }
        public bool HasHardcore { get; set; }
        public int HardcoreChance { get; set; }
        [XmlArrayItem(ElementName = "IgnoredLocation")]
        public List<int> IgnoredLocations { get; set; }

        public GamemodeOption()
        {

        }

        public GamemodeOption(EGameType gameType, int gamemodeWeight, bool hasHardcore, int hardcoreChance, List<int> ignoredLocations)
        {
            GameType = gameType;
            GamemodeWeight = gamemodeWeight;
            HasHardcore = hasHardcore;
            HardcoreChance = hardcoreChance;
            IgnoredLocations = ignoredLocations;
        }
    }
}
