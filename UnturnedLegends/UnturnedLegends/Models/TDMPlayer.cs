using System;
using UnturnedLegends.Database;

namespace UnturnedLegends.Models
{
    public class TDMPlayer
    {
        public GamePlayer GamePlayer { get; set; }

        public Team Team { get; set; }

        public int Score { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int KillStreak { get; set; }
        public int MultipleKills { get; set; }

        public DateTime LastKill { get; set; }

        public TDMPlayer(GamePlayer gamePlayer, Team team)
        {
            GamePlayer = gamePlayer;
            Team = team;

            Score = 0;
            Kills = 0;
            Deaths = 0;
            KillStreak = 0;
            MultipleKills = 0;

            LastKill = DateTime.UtcNow;
        }

        public void OnDeath()
        {
            KillStreak = 0;
            MultipleKills = 0;
            Deaths++;
            LastKill = DateTime.UtcNow;
        }

        public void CheckKills()
        {
            if (!Plugin.Instance.DBManager.PlayerCache.TryGetValue(GamePlayer.SteamID, out PlayerData data))
            {
                return;
            }
            data.CheckKillstreak(KillStreak);
            data.CheckMultipleKills(MultipleKills);
        }
    }
}
