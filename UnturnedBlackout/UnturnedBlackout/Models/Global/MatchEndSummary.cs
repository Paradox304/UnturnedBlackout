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
                return Plugin.Instance.Config;
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

            var minutesPlayed = (int)Math.Floor((EndTime - StartTime).TotalMinutes);
            var data = Config.WinningValues.FileData;
            var global = Plugin.Instance.DB.ServerOptions;

            PendingCredits = MatchXP == 0 ? 0 : ((MatchXP / data.PointsDivisible) + (minutesPlayed * data.PointsPerMinutePlayed));
            MatchXPBonus = (int)((Kills > 0 ? (MatchXP / (HasWon ? data.BonusXPVictoryDivisible : data.BonusXPDefeatDivisible)) : 0) * (1f + player.Data.XPBooster + global.XPBooster + (player.Data.HasPrime ? data.PrimeXPBooster : 0f)));
            if (MatchXPBonus != 0)
            {
                MatchXPBonus += minutesPlayed * data.BonusXPPerMinutePlayed;
            }
            AchievementXPBonus = (int)Math.Floor(MatchXP * player.Data.AchievementXPBooster);
            OtherXPBonus = 0; // Havent got formula for this

            BattlepassXP = (int)(Kills > 0 ? (data.BPXPPerMinutePlayed * minutesPlayed * (1f + (HasWon ? data.BPXPVictoryBonus : data.BPXPDefeatBonus))) : 0); // Left for later on when I get the formula
            BattlepassBonusXP = (int)Math.Floor(BattlepassXP * (player.Data.BPBooster + global.BPBooster + (player.Data.HasPrime ? data.PrimeBPXPBooster : 0f)));

            KD = Deaths == 0 ? 0f : Kills / (float)Deaths;

            // Final Values
            TotalXP = MatchXP + MatchXPBonus + AchievementXPBonus + OtherXPBonus;
            PendingXP = MatchXPBonus + AchievementXPBonus + OtherXPBonus;
        }
    }
}
