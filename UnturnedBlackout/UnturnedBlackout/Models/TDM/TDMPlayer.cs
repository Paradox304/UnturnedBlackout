using Steamworks;
using System;
using System.Collections.Generic;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.TDM
{
    public class TDMPlayer
    {
        public GamePlayer GamePlayer { get; set; }

        public TDMTeam Team { get; set; }

        public DateTime StartTime { get; set; }
        public int StartingLevel { get; set; }
        public int StartingXP { get; set; }
        public int XP { get; set; }
        public int Score { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public int Killstreak { get; private set; }
        public int MultipleKills { get; private set; }
        public int HighestKillstreak { get; set; }
        public int HighestMK { get; set; }

        public DateTime LastKill { get; set; }
        public Dictionary<CSteamID, int> PlayersKilled { get; set; }

        public TDMPlayer(GamePlayer gamePlayer, TDMTeam team)
        {
            GamePlayer = gamePlayer;
            Team = team;

            StartTime = DateTime.UtcNow;
            StartingLevel = gamePlayer.Data.Level;
            StartingXP = gamePlayer.Data.XP;
            XP = 0;
            Score = 0;
            Kills = 0;
            Deaths = 0;
            Assists = 0;
            Killstreak = 0;
            MultipleKills = 0;

            LastKill = DateTime.UtcNow;
            PlayersKilled = new Dictionary<CSteamID, int>();
        }

        public void OnDeath(CSteamID killer)
        {
            Killstreak = 0;
            MultipleKills = 0;
            Deaths++;

            LastKill = DateTime.UtcNow;
            PlayersKilled.Remove(killer);

            GamePlayer.UpdateKillstreak(0);
        }

        public void CheckKills()
        {
            Database.Data.PlayerData data = GamePlayer.Data;
            data.CheckKillstreak(Killstreak);
            data.CheckMultipleKills(MultipleKills);
        }

        public void SetKillstreak(int killstreak)
        {
            if (GamePlayer.Player.Player.life.isDead)
                return;

            Killstreak = killstreak;
            if (killstreak > HighestKillstreak)
            {
                HighestKillstreak = killstreak;
            }
            GamePlayer.UpdateKillstreak(killstreak);
        }

        public void SetMultipleKills(int multikills)
        {
            if (GamePlayer.Player.Player.life.isDead)
                return;

            MultipleKills = multikills;
            if (multikills > HighestMK)
            {
                HighestMK = multikills;
            }
        }
    }
}
