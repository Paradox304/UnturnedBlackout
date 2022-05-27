using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnturnedBlackout.GameTypes;

namespace UnturnedBlackout.Commands
{
    class LoadoutsCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "loadouts";

        public string Help => "Opens the loadout menu to change loadout midgame";

        public string Syntax => "/loadouts";

        public List<string> Aliases => new List<string> { "loadout" };

        public List<string> Permissions => new List<string>();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = caller as UnturnedPlayer;
            var gPlayer = Plugin.Instance.GameManager.GetGamePlayer(player);
            if (gPlayer == null)
            {
                return;
            }
            if (Plugin.Instance.GameManager.TryGetCurrentGame(gPlayer.SteamID, out Game game))
            {
                if (game.GamePhase == Enums.EGamePhase.Started || game.GamePhase == Enums.EGamePhase.WaitingForPlayers)
                {
                    Plugin.Instance.UIManager.ShowMidgameLoadoutUI(gPlayer);
                }
            }
        }
    }
}
