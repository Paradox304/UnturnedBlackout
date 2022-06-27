﻿using Steamworks;
using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data
{
    public class PlayerAchievement
    {
        public CSteamID SteamID { get; set; }
        public Achievement Achievement { get; set; }
        public int CurrentTier { get; set; }
        public int Amount { get; set; }

        public PlayerAchievement(CSteamID steamID, Achievement achievement, int currentTier, int amount)
        {
            SteamID = steamID;
            Achievement = achievement;
            CurrentTier = currentTier;
            Amount = amount;
        }
    }
}
