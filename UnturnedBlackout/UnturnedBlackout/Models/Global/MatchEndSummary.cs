using System;

namespace UnturnedBlackout.Models.Global
{
    public class MatchEndSummary
    {
        public GamePlayer Player { get; set; }

        public int TotalXP
        {
            get
            {
                return MatchXP + MatchXPBonus + AchievementXPBonus + OtherBonusXP;
            }
        }

        public int MatchXP { get; set; }
        public int MatchXPBonus
        {
            get
            {
                return (int)Math.Ceiling(MatchXP * (1f + Player.Data.XPBooster));
            }
        }
        public int AchievementXPBonus
        {
            get
            {
                return (int)Math.Ceiling(MatchXP * (1f + Player.Data.AchievementXPBooster));
            }
        }

        public int OtherBonusXP { get; set; }

        public int StartingLevel { get; set; }
        public int EndingLevel
        {
            get
            {
                return Player.Data.Level;
            }
        }

        public int BattlepassXP { get; set; }
        public int BattlepassBonusXP
        {
            get
            {
                return (int)Math.Ceiling(BattlepassXP * (1f + Player.Data.BPBooster));
            }
        }

        public int Kills { get; set; }
        public int Deaths { get; set; }
        public float KD
        {
            get
            {
                return Kills / (float)Deaths;
            }
        }
        public int Assists { get; set; }

        public int HighestKillstreak { get; set; }
        public int HighestMK { get; set; }

    }
}
