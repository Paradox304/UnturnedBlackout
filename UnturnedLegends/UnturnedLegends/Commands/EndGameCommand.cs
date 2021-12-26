using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedLegends.Commands
{
    class EndGameCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "endgame";

        public string Help => "Ends game instantly";

        public string Syntax => "/endgame";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (Plugin.Instance.GameManager.Game != null)
            {
                Plugin.Instance.GameManager.Game.GameEnd();
            }
        }
    }
}
