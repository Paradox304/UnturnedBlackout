using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Models.CTF;

namespace UnturnedBlackout.Commands
{
    class CTFSPCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "ctfsp";

        public string Help => "Set the ctf spawnpoints for an area location for a group and the flag sp";

        public string Syntax => "/ctfsp (LocationID) (GroupID) [IsFlag]";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = caller as UnturnedPlayer;

            if (command.Length < 2)
            {
                Utility.Say(caller, Plugin.Instance.Translate("Correct_Usage", Syntax).ToRich());
                return;
            }

            if (!int.TryParse(command[0], out int locationID))
            {
                Utility.Say(caller, Plugin.Instance.Translate("Location_Not_Found").ToRich());
                return;
            }

            if (!int.TryParse(command[1], out int groupID))
            {
                Utility.Say(caller, Plugin.Instance.Translate("Group_Not_Found").ToRich());
                return;
            }

            bool isFlag = false;
            if (command.Length > 2)
            {
                if (command[2] == "true" || command[2] == "t")
                {
                    isFlag = true;
                }
            }

            var location = Plugin.Instance.Configuration.Instance.ArenaLocations.FirstOrDefault(k => k.LocationID == locationID);
            if (location == null)
            {
                Utility.Say(caller, Plugin.Instance.Translate("Location_Not_Found").ToRich());
                return;
            }

            Utility.Say(caller, isFlag == true ? Plugin.Instance.Translate("CTF_Flag_SpawnPoint_Set", location.LocationName, groupID).ToRich() : Plugin.Instance.Translate("CTF_SpawnPoint_Set", location.LocationName, groupID).ToRich());
            Plugin.Instance.DataManager.Data.CTFSpawnPoints.Add(new CTFSpawnPoint(locationID, groupID, player.Player.transform.position.x, player.Player.transform.position.y, player.Player.transform.position.z, isFlag));
            Plugin.Instance.DataManager.SaveJson();
        }
    }
}
