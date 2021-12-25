﻿using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;
using UnturnedLegends.SpawnPoints;

namespace UnturnedLegends.Commands
{
    public class FFASPCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "ffasp";

        public string Help => "Set the ffa spawnpoints for an area location";

        public string Syntax => "/ffasp (LocationID)";

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

            Utility.Say(caller, Plugin.Instance.Translate("FFA_Spawnpoint_Set", location.LocationName).ToRich());
            Plugin.Instance.DataManager.Data.FFASpawnPoints.Add(new FFASpawnPoint(locationID, player.Player.transform.position.x, player.Player.transform.position.y, player.Player.transform.position.z));
            Plugin.Instance.DataManager.SaveJson();
        }
    }
}
