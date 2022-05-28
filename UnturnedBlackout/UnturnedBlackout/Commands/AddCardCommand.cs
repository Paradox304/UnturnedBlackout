using Rocket.API;
using Steamworks;
using System.Collections.Generic;
using System.Threading;

namespace UnturnedBlackout.Commands
{
    class AddCardCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "addcard";

        public string Help => "Add a card to a player";

        public string Syntax => "/addcard (SteamID) (CardID)";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 2)
            {
                Utility.Say(caller, $"<color=red>Correct Usage: {Syntax}</color>");
                return;
            }

            if (!ulong.TryParse(command[0], out ulong steamid))
            {
                Utility.Say(caller, $"<color=red>SteamID is not in the correct format</color>");
                return;
            }

            var steamID = new CSteamID(steamid);

            if (!int.TryParse(command[1], out int cardID))
            {
                Utility.Say(caller, $"<color=red>CardID is not in the correct format</color>");
                return;
            }

            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await Plugin.Instance.DBManager.AddPlayerCardAsync(steamID, cardID, true);
            });

            Utility.Say(caller, $"<color=green>Added card with id {cardID} to {steamID}</color>");
        }
    }
}
