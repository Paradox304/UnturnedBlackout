using Steamworks;
using System;
using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data
{
    public class PlayerQuest
    {
        public CSteamID SteamID { get; set; }
        public Quest Quest { get; set; }
        public int Amount { get; set; }
        public DateTimeOffset QuestEnd { get; set; }

        public PlayerQuest(CSteamID steamID, Quest quest, int amount, DateTimeOffset questEnd)
        {
            SteamID = steamID;
            Quest = quest;
            Amount = amount;
            QuestEnd = questEnd;
        }
    }
}
