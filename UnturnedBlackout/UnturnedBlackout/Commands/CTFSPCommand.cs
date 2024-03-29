﻿using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;
using UnturnedBlackout.Extensions;

namespace UnturnedBlackout.Commands;

internal class CTFSPCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Player;

    public string Name => "ctfsp";

    public string Help => "Set the ctf spawnpoints for an area location for a group and the flag sp";

    public string Syntax => "/ctfsp (LocationID) (GroupID) [IsFlag]";

    public List<string> Aliases => new();

    public List<string> Permissions => new();

    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (caller is not UnturnedPlayer player)
            return;
        
        if (command.Length < 2)
        {
            Utility.Say(caller, Plugin.Instance.Translate("Correct_Usage", Syntax));
            return;
        }

        if (!int.TryParse(command[0], out var locationID))
        {
            Utility.Say(caller, Plugin.Instance.Translate("Location_Not_Found"));
            return;
        }

        if (!int.TryParse(command[1], out var groupID))
        {
            Utility.Say(caller, Plugin.Instance.Translate("Group_Not_Found"));
            return;
        }

        var isFlag = false;
        if (command.Length > 2)
        {
            if (command[2] == "true" || command[2] == "t")
                isFlag = true;
        }

        var location = Plugin.Instance.Config.Locations.FileData.ArenaLocations.FirstOrDefault(k => k.LocationID == locationID);
        if (location == null)
        {
            Utility.Say(caller, Plugin.Instance.Translate("Location_Not_Found"));
            return;
        }

        Utility.Say(caller, isFlag ? Plugin.Instance.Translate("CTF_Flag_SpawnPoint_Set", location.LocationName, groupID) : Plugin.Instance.Translate("CTF_SpawnPoint_Set", location.LocationName, groupID));

        var transform = player.Player.transform;
        var position = transform.position;
        Plugin.Instance.Data.Data.CTFSpawnPoints.Add(new(locationID, groupID, position.x, position.y, position.z, transform.eulerAngles.y, isFlag));

        Plugin.Instance.Data.SaveJson();
    }
}