using System;

namespace UnturnedLegends.Structs
{
    public class FFAPlayer
    {
        public GamePlayer GamePlayer { get; set; }

        public int Kills { get; set; }
        public int KillStreak { get; set; }
        public int MultipleKills { get; set; }
        public DateTime LastKill { get; set; }

        public int XP { get; set; }

        public FFAPlayer(GamePlayer gamePlayer)
        {
            GamePlayer = gamePlayer;

            Kills = 0;
            KillStreak = 0;
            MultipleKills = 0;
            LastKill = DateTime.UtcNow;
        }

        public void OnDeath()
        {
            KillStreak = 0;
            MultipleKills = 0;
            LastKill = DateTime.UtcNow;
        }
    }
}
