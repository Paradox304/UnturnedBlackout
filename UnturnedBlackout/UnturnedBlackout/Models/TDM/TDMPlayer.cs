﻿using Steamworks;
using System;
using System.Collections.Generic;
using UnturnedBlackout.Database;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.TDM
{
    public class TDMPlayer
    {
        public GamePlayer GamePlayer { get; set; }

        public TDMTeam Team { get; set; }

        public int XP { get; set; }
        public int Score { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public int KillStreak { get; set; }
        public int MultipleKills { get; set; }

        public DateTime LastKill { get; set; }
        public Dictionary<CSteamID, int> PlayersKilled { get; set; }

        public TDMPlayer(GamePlayer gamePlayer, TDMTeam team)
        {
            GamePlayer = gamePlayer;
            Team = team;

            XP = 0;
            Score = 0;
            Kills = 0;
            Deaths = 0;
            Assists = 0;
            KillStreak = 0;
            MultipleKills = 0;

            LastKill = DateTime.UtcNow;
            PlayersKilled = new Dictionary<CSteamID, int>();
        }

        public void OnDeath(CSteamID killer)
        {
            KillStreak = 0;
            MultipleKills = 0;
            Deaths++;

            LastKill = DateTime.UtcNow;
            PlayersKilled.Remove(killer);
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
