using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;
using UnturnedBlackout.Models.TDM;

namespace UnturnedBlackout.Commands;

class TDMSPCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Player;

    public string Name => "tdmsp";

    public string Help => "Set the tdm spawnpoints for an area location for a group";

    public string Syntax => "/tdmsp (LocationID) (GroupID)";

    public List<string> Aliases => new();

    public List<string> Permissions => new();

    public void Execute(IRocketPlayer caller, string[] command)
    {
        var player = caller as UnturnedPlayer;

        if (command.Length < 2)
        {
            Utility.Say(caller, Plugin.Instance.Translate("Correct_Usage", Syntax).ToRich());
            return;
        }

        if (!int.TryParse(command[0], out var locationID))
        {
            Utility.Say(caller, Plugin.Instance.Translate("Location_Not_Found").ToRich());
            return;
        }

        if (!int.TryParse(command[1], out var groupID))
        {
            Utility.Say(caller, Plugin.Instance.Translate("Group_Not_Found").ToRich());
            return;
        }

        var location = Plugin.Instance.Config.Locations.FileData.ArenaLocations.FirstOrDefault(k => k.LocationID == locationID);
        if (location == null)
        {
            Utility.Say(caller, Plugin.Instance.Translate("Location_Not_Found").ToRich());
            return;
        }

        Utility.Say(caller, Plugin.Instance.Translate("TDM_SpawnPoint_Set", location.LocationName, groupID).ToRich());
        Plugin.Instance.Data.Data.TDMSpawnPoints.Add(new TDMSpawnPoint(locationID, groupID, player.Player.transform.position.x, player.Player.transform.position.y, player.Player.transform.position.z, player.Player.transform.eulerAngles.y));
        Plugin.Instance.Data.SaveJson();
    }
}
