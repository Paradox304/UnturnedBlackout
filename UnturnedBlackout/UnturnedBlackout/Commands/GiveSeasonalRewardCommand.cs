using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Commands
{
    class GiveSeasonalRewardCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "giveseasonalreward";

        public string Help => "Giving out seasonal rewards";

        public string Syntax => "/giveseasonalreward";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = caller as UnturnedPlayer;
            Plugin.Instance.DBManager.IsPendingSeasonalWipe = true;
        }
    }
}
