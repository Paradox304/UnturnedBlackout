using System;

namespace UnturnedBlackout.Models.Bot;

[Serializable]
public class BotLeaderboardReward
{
    public string leaderboard { get; set; }
    public int total_players { get; set; }
    public BotReward[] rewards { get; set; }

    public BotLeaderboardReward(string leaderboard, int totalPlayers, BotReward[] rewards)
    {
        this.leaderboard = leaderboard;
        total_players = totalPlayers;
        this.rewards = rewards;
    }
}

[Serializable]
public class BotReward
{
    public string steam_id { get; set; }
    public int rank { get; set; }
    public int percentile { get; set; }

    public BotReward(string steamID, int rank, int percentile)
    {
        steam_id = steamID;
        this.rank = rank;
        this.percentile = percentile;
    }
}