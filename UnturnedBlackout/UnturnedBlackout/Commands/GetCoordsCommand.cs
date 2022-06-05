using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Commands
{
    class GetCoordsCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "getcoords";

        public string Help => "Get your coordinates";

        public string Syntax => "/getcoords";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = caller as UnturnedPlayer;
            Utility.Say(caller, $"X: {player.Position.x}, Y: {player.Position.z}, Z: {player.Position.z}");
        }
    }
}
