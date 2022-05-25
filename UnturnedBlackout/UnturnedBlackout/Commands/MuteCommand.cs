using Rocket.API;
using Rocket.Core.Steam;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnturnedBlackout.Commands
{
    class MuteCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "mute";

        public string Help => "Mute a player";

        public string Syntax => "/mute (PlayerName/SteamID) [Seconds] [Reason]";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = caller as UnturnedPlayer;

            if (command.Length < 3)
            {
                Utility.Say(caller, Plugin.Instance.Translate("Correct_Usage", Syntax).ToRich());
                return;
            }

            var steamID = CSteamID.Nil;
            if (!ulong.TryParse(command[0], out ulong steamid))
            {
                steamID = PlayerTool.getPlayer(command[0])?.channel?.owner?.playerID?.steamID ?? CSteamID.Nil;
            } else
            {
                steamID = new CSteamID(steamid);
            }

            if (steamID == CSteamID.Nil)
            {
                Utility.Say(caller, "<color=red>Player not found</color>");
                return;
            }

            if (!int.TryParse(command[1], out int seconds))
            {
                Utility.Say(caller, "<color=red>Seconds is not in the correct format</color>");
                return;
            }

            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                Profile profile;
                try
                {
                    profile = new Profile(steamID.m_SteamID);
                } catch (Exception)
                {
                    Utility.Say(caller, "<color=red>Player not found</color>");
                    return;
                }


            });
        }
    }
}
