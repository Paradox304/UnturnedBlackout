using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;

namespace UnturnedBlackout.Commands;

internal class GetNavMeshCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Player;

    public string Name => "getnavmesh";

    public string Help => "Get the nav mesh of the area you're in";

    public string Syntax => "/getnavmesh";

    public List<string> Aliases => new();

    public List<string> Permissions => new();

    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (caller is not UnturnedPlayer player)
            return;

        if (LevelNavigation.tryGetNavigation(player.Position, out var nav))
            UnturnedChat.Say(caller, $"Nav Mesh: {nav}");
    }
}