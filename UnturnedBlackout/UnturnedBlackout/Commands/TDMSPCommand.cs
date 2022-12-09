using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnturnedBlackout.Extensions;

namespace UnturnedBlackout.Commands;

[UsedImplicitly]
internal class TDMSPCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Player;

    public string Name => "tdmsp";

    public string Help => "Set the tdm spawnpoints for an area location for a group";

    public string Syntax => "/tdmsp (LocationID) (GroupID)";

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

        var location = Plugin.Instance.Config.Locations.FileData.ArenaLocations.FirstOrDefault(k => k.LocationID == locationID);
        if (location == null)
        {
            Utility.Say(caller, Plugin.Instance.Translate("Location_Not_Found"));
            return;
        }

        Utility.Say(caller, Plugin.Instance.Translate("TDM_SpawnPoint_Set", location.LocationName, groupID));

        var transform = player.Player.transform;
        var position = transform.position;
        Plugin.Instance.Data.Data.TDMSpawnPoints.Add(new(locationID, groupID, position.x, position.y, position.z, transform.eulerAngles.y));

        Plugin.Instance.Data.SaveJson();
    }
}