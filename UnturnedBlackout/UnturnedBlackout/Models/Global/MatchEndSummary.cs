using System;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Managers;

namespace UnturnedBlackout.Models.Global
{
    public class MatchEndSummary
    {
        public ConfigManager Config
        {
            get
            {
                return Plugin.Instance.ConfigManager;
            }
        }

        public GamePlayer Player { get; set; }

        public int PendingCredits { get; set; }

        public int TotalXP { get; set; }
        public int PendingXP { get; set; }
        public int MatchXP { get; set; }
        public int MatchXPBonus { get; set; }
        public int AchievementXPBonus { get; set; }
        public int OtherXPBonus { get; set; }

        public int StartingLevel { get; set; }
        public int EndingLevel { get; set; }
        public int StartingXP { get; set; }

        public int BattlepassXP { get; set; }
        public int BattlepassBonusXP { get; set; }

        public int Kills { get; set; }
        public int Deaths { get; set; }
        public float KD { get; set; }
        public int Assists { get; set; }

        public int HighestKillstreak { get; set; }
        public int HighestMK { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public EGameType GameType { get; set; }
        public bool HasWon { get; set; }

        public MatchEndSummary(GamePlayer player, int matchXP, int startingLevel, int startingXP, int kills, int deaths, int assists, int highestKillstreak, int highestMK, DateTime startTime, EGameType gameType, bool hasWon)
        {
            // Set values
            Player = player;
            MatchXP = matchXP;
            StartingLevel = startingLevel;
            EndingLevel = player.Data.Level;
            StartingXP = startingXP;
            Kills = kills;
            Deaths = deaths;
            Assists = assists;
            HighestKillstreak = highestKillstreak;
            HighestMK = highestMK;
            StartTime = startTime;
            GameType = gameType;
            HasWon = hasWon;
            EndTime = DateTime.UtcNow;

            // Calculated Values
            MatchXPBonus = (int)Math.Ceiling(MatchXP * player.Data.XPBooster);
            AchievementXPBonus = (int)Math.Ceiling(MatchXP * player.Data.AchievementXPBooster);
            OtherXPBonus = (int)Math.Ceiling(MatchXP * (HasWon ? GetWinMultiplier() : 0.2f));
            BattlepassXP = 0; // Left for later on when I get the formula
            BattlepassBonusXP = (int)Math.Ceiling(BattlepassXP * player.Data.BPBooster);
            KD = Kills / (float)Deaths;
            PendingCredits = 0; // Left for later on when I get the formula

            // Final Values
            TotalXP = MatchXP + MatchXPBonus + AchievementXPBonus + OtherXPBonus;
            PendingXP = MatchXPBonus + AchievementXPBonus + OtherXPBonus;
        }

        public float GetWinMultiplier() =>
             GameType switch
             {
                 EGameType.FFA => Config.FFA.FileData.WinMultiplier,
                 EGameType.CTF => Config.CTF.FileData.WinMultiplier,
                 EGameType.TDM => Config.TDM.FileData.WinMultiplier,
                 EGameType.KC => Config.KC.FileData.WinMultiplier,
                 _ => throw new ArgumentOutOfRangeException("GameType is not as expected")
             };
    }
}
