using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedLegends.Commands
{
    class SetLobbyCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "setlobby";

        public string Help => "Set the lobby for players to join at your position";

        public string Syntax => "/setlobby";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = caller as UnturnedPlayer;

            Plugin.Instance.Configuration.Instance.LobbySpawn = player.Player.transform.position;
            Plugin.Instance.Configuration.Save();
        }
    }
}
