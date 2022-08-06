using Rocket.API;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnturnedBlackout.Commands
{
    class AddPrimeCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "addprime";

        public string Help => "Add prime to a player";

        public string Syntax => "/addprime (SteamID) (Days)";

        public List<string> Aliases => new();

        public List<string> Permissions => new();

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

            if (!int.TryParse(command[1].ToString(), out int days))
            {
                Utility.Say(caller, $"<color=red>Days is not in the correct format</color>");
                return;
            }

            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await Plugin.Instance.DBManager.AddPlayerPrimeAsync(steamID, days);
            });

            Utility.Say(caller, $"<color=green>Added prime with days {days} to {steamID}</color>");
        }
    }
}
