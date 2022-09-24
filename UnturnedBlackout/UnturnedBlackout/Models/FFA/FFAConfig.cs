using System.Collections.Generic;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.FFA
{
    public class FFAConfig
    {
        public int StartSeconds { get; set; }
        public int EndSeconds { get; set; }

        public Kit Kit { get; set; }
        public List<TeamGlove> TeamGloves { get; set; }
        public string KillFeedHexCode { get; set; }
        public string ChatPlayerHexCode { get; set; }
        public string ChatMessageHexCode { get; set; }
        public float WinMultiplier { get; set; }

        public int ScoreLimit { get; set; }
        public int SpawnProtectionSeconds { get; set; }
        public int RespawnSeconds { get; set; }

        public FFAConfig()
        {
            StartSeconds = 10;
            EndSeconds = 600;
            Kit = new Kit(new List<ushort> { 54500, 54501, 54502, 54503, 54504, 54505, 54506 });
            TeamGloves = new List<TeamGlove> { new TeamGlove(1, 13024), new TeamGlove(2, 13026) };
            KillFeedHexCode = "#dcb4ff";
            ChatPlayerHexCode = "#dcb4ff";
            ChatMessageHexCode = "#dcb4ff";
            WinMultiplier = 0.5f;
            ScoreLimit = 25;
            SpawnProtectionSeconds = 2;
            RespawnSeconds = 3;
        }
    }
}
