using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data
{
    public class PlayerCase
    {
        public CSteamID SteamID { get; set; }
        public Case Case { get; set; }
        public int Amount { get; set; }

        public PlayerCase(CSteamID steamID, Case @case, int amount)
        {
            SteamID = steamID;
            Case = @case;
            Amount = amount;
        }
    }
}
