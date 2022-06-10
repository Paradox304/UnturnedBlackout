using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
