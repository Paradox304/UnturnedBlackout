using System;
using System.Collections;
using UnityEngine;
using UnturnedLegends.Database;

namespace UnturnedLegends.Models
{
    public class FFAPlayer
    {
        public GamePlayer GamePlayer { get; set; }

        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int KillStreak { get; set; }
        public int MultipleKills { get; set; }
        public int HighestKillStreak { get; set; }
        public int HighestMultipleKills { get; set; }

        public DateTime LastKill { get; set; }

        public FFAPlayer(GamePlayer gamePlayer)
        {
            GamePlayer = gamePlayer;

            Kills = 0;
            Deaths = 0;
            KillStreak = 0;
            MultipleKills = 0;
            HighestKillStreak = 0;
            HighestMultipleKills = 0;

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

            if (KillStreak > HighestKillStreak)
            {
                HighestKillStreak = KillStreak;
                data.CheckKillstreak(KillStreak);
            }

            if (MultipleKills > HighestMultipleKills)
            {
                MultipleKills = HighestMultipleKills;
                data.CheckMultipleKills(MultipleKills);
            }
        }
    }
}
