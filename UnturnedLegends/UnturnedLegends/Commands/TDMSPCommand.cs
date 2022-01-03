﻿using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;
using UnturnedLegends.Models;

namespace UnturnedLegends.Commands
{
    class TDMSPCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "tdmsp";

        public string Help => "Set the tdm spawnpoints for an area location for a team";

        public string Syntax => "/tdmsp (LocationID) (TeamID)";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = caller as UnturnedPlayer;

            if (command.Length == 0)
            {
                Utility.Say(caller, Plugin.Instance.Translate("Correct_Usage", Syntax).ToRich());
                return;
            }

            if (!int.TryParse(command[0], out int teamID) || (teamID != 0 && teamID != 1))
            {
                Utility.Say(caller, Plugin.Instance.Translate("Team_Not_Found").ToRich());
                return;
            }

            if (!int.TryParse(command[0], out int locationID))
            {
                Utility.Say(caller, Plugin.Instance.Translate("Location_Not_Found").ToRich());
                return;
            }

            var location = Plugin.Instance.Configuration.Instance.ArenaLocations.FirstOrDefault(k => k.LocationID == locationID);
            if (location == null)
            {
                Utility.Say(caller, Plugin.Instance.Translate("Location_Not_Found").ToRich());
                return;
            }

            Utility.Say(caller, Plugin.Instance.Translate("TDM_SpawnPoint_Set", location.LocationName, teamID == 0 ? "BLUE TEAM" : "RED TEAM").ToRich());
            Plugin.Instance.DataManager.Data.TDMSpawnPoints.Add(new TDMSpawnPoint(locationID, teamID, player.Player.transform.position.x, player.Player.transform.position.y, player.Player.transform.position.z));
            Plugin.Instance.DataManager.SaveJson();
        }
    }
}
