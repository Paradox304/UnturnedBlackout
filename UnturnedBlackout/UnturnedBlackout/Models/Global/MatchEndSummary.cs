using System;
using System.Linq;
using UnityEngine;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Managers;

namespace UnturnedBlackout.Models.Global;

public class MatchEndSummary
{
    public ConfigManager Config => Plugin.Instance.Config;

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

    public MatchEndSummary(GamePlayer player, GameEvent @event, int matchXP, int startingLevel, int startingXP, int kills, int deaths, int assists, int highestKillstreak, int highestMK, DateTime startTime, EGameType gameType, bool hasWon)
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

        PendingCredits = MatchXP == 0 ? 0 : MatchXP / data.PointsDivisible + minutesPlayed * data.PointsPerMinutePlayed;
        PendingCredits = player.Data.HasPrime ? Mathf.FloorToInt(PendingCredits * (1f + data.PrimePointsBooster)) : PendingCredits;
        MatchXPBonus = Kills > 0 ? MatchXP / (HasWon ? data.BonusXPVictoryDivisible : data.BonusXPDefeatDivisible) : 0;
        if (MatchXPBonus != 0)
            MatchXPBonus += minutesPlayed * data.BonusXPPerMinutePlayed;

        AchievementXPBonus = Mathf.FloorToInt(MatchXP * player.Data.AchievementXPBooster);
        OtherXPBonus = Mathf.RoundToInt(MatchXP * (1f + player.Data.XPBooster + global.XPBooster + (@event?.XPMultiplier ?? 0f) + (Plugin.Instance.DB.Servers.FirstOrDefault(k => k.IsCurrentServer)?.SurgeMultiplier ?? 0f)));

        BattlepassXP = (int)(Kills > 0 ? data.BPXPPerMinutePlayed * minutesPlayed * (1f + (HasWon ? data.BPXPVictoryBonus : data.BPXPDefeatBonus)) : 0);
        BattlepassBonusXP = Mathf.FloorToInt(BattlepassXP * (player.Data.BPBooster + global.BPBooster + (player.Data.HasPrime ? data.PrimeBPXPBooster : 0f) + (player.Data.HasBattlepass ? data.PremiumBattlepassBooster : 0f)));

        KD = Deaths == 0 ? 0f : Kills / (float)Deaths;

        // Final Values
        TotalXP = MatchXP + MatchXPBonus + AchievementXPBonus + OtherXPBonus;
        PendingXP = MatchXPBonus + AchievementXPBonus + OtherXPBonus;
    }
}