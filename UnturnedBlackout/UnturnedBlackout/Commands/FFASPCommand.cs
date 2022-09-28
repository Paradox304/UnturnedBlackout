﻿using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;
using UnturnedBlackout.Models.FFA;

namespace UnturnedBlackout.Commands;

public class FFASPCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller
    {
        get
        {
            return AllowedCaller.Player;
        }
    }

    public string Name
    {
        get
        {
            return "ffasp";
        }
    }

    public string Help
    {
        get
        {
            return "Set the ffa spawnpoints for an area location";
        }
    }

    public string Syntax
    {
        get
        {
            return "/ffasp (LocationID)";
        }
    }

    public List<string> Aliases
    {
        get
        {
            return new();
        }
    }

    public List<string> Permissions
    {
        get
        {
            return new();
        }
    }

    public void Execute(IRocketPlayer caller, string[] command)
    {
        var player = caller as UnturnedPlayer;

        if (command.Length == 0)
        {
            Utility.Say(caller, Plugin.Instance.Translate("Correct_Usage", Syntax).ToRich());
            return;
        }

        if (!int.TryParse(command[0], out var locationID))
        {
            Utility.Say(caller, Plugin.Instance.Translate("Location_Not_Found").ToRich());
            return;
        }

        var location = Plugin.Instance.Config.Locations.FileData.ArenaLocations.FirstOrDefault(k => k.LocationID == locationID);
        if (location == null)
        {
            Utility.Say(caller, Plugin.Instance.Translate("Location_Not_Found").ToRich());
            return;
        }

        Utility.Say(caller, Plugin.Instance.Translate("FFA_Spawnpoint_Set", location.LocationName).ToRich());
        Plugin.Instance.Data.Data.FFASpawnPoints.Add(new FFASpawnPoint(locationID, player.Player.transform.position.x, player.Player.transform.position.y, player.Player.transform.position.z, player.Player.transform.eulerAngles.y));
        Plugin.Instance.Data.SaveJson();
    }
}
