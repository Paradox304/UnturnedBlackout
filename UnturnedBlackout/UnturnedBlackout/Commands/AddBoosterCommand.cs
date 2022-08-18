using Rocket.API;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Commands
{
    class AddBoosterCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "addbooster";

        public string Help => "Add a booster to a player";

        public string Syntax => "/addbooster (SteamID) (BoosterType) (BoosterValue) (ExpirationDays)";

        public List<string> Aliases => new();

        public List<string> Permissions => new();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 4)
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

            if (!Enum.TryParse(command[1], true, out EBoosterType boosterType))
            {
                Utility.Say(caller, $"<color=red>Booster Type is not in the correct format</color>");
                return;
            }

            if (!float.TryParse(command[2], out float boosterValue))
            {
                Utility.Say(caller, $"<color=red>Booster Value is not in the correct format</color>");
                return;
            }

            if (!int.TryParse(command[3], out int expirationDays))
            {
                Utility.Say(caller, $"<color=red>Expiration Days is not in the correct format</color>");
                return;
            }

            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await Plugin.Instance.DB.AddPlayerBoosterAsync(steamID, boosterType, boosterValue, expirationDays);
            });

            Utility.Say(caller, $"<color=green>Added booster with type {boosterType}, value {boosterValue}, days {expirationDays} to {steamID}</color>");
        }
    }
}
