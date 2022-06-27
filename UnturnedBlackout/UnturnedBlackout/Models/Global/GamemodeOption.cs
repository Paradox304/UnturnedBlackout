using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Models.Global
{
    public class GamemodeOption
    {
        public string GamemodeName { get; set; }
        public EGameType GameType { get; set; }
        public int GamemodeWeight { get; set; }
        public bool HasHardcore { get; set; }
        public int HardcoreChance { get; set; }
        [XmlArrayItem(ElementName = "IgnoredLocation")]
        public List<int> IgnoredLocations { get; set; }

        public GamemodeOption()
        {

        }

        public GamemodeOption(string gamemodeName, int gamemodeWeight, bool hasHardcore, int hardcoreChance, List<int> ignoredLocations)
        {
            GamemodeName = gamemodeName;
            GamemodeWeight = gamemodeWeight;
            HasHardcore = hasHardcore;
            HardcoreChance = hardcoreChance;
            IgnoredLocations = ignoredLocations;
            if (Enum.TryParse(gamemodeName, true, out EGameType gameType))
            {
                GameType = gameType;
            }
        }
    }
}
