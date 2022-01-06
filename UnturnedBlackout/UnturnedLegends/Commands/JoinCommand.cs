using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;

namespace UnturnedBlackout.Commands
{
    class JoinCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "join";

        public string Help => "Join the game going on";

        public string Syntax => "/join (id)";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = caller as UnturnedPlayer;
            if (!int.TryParse(command[0].ToString(), out int id))
            {
                id = 0;
            }

            Plugin.Instance.GameManager.AddPlayerToGame(player, id);
        }
    }
}
