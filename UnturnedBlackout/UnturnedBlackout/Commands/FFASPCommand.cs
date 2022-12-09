using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnturnedBlackout.Extensions;

namespace UnturnedBlackout.Commands;

[UsedImplicitly]
public class FFASPCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Player;

    public string Name => "ffasp";

    public string Help => "Set the ffa spawnpoints for an area location";

    public string Syntax => "/ffasp (LocationID)";

    public List<string> Aliases => new();

    public List<string> Permissions => new();

    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (caller is not UnturnedPlayer player)
            return;
        
        if (command.Length == 0)
        {
            Utility.Say(caller, Plugin.Instance.Translate("Correct_Usage", Syntax));
            return;
        }

        if (!int.TryParse(command[0], out var locationID))
        {
            Utility.Say(caller, Plugin.Instance.Translate("Location_Not_Found"));
            return;
        }

        var location = Plugin.Instance.Config.Locations.FileData.ArenaLocations.FirstOrDefault(k => k.LocationID == locationID);
        if (location == null)
        {
            Utility.Say(caller, Plugin.Instance.Translate("Location_Not_Found"));
            return;
        }

        Utility.Say(caller, Plugin.Instance.Translate("FFA_Spawnpoint_Set", location.LocationName));
        var transform = player.Player.transform;
        var position = transform.position;
        Plugin.Instance.Data.Data.FFASpawnPoints.Add(new(locationID, position.x, position.y, position.z, transform.eulerAngles.y));
        Plugin.Instance.Data.SaveJson();
    }
}