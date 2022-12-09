using Rocket.API;
using System;
using System.Collections.Generic;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Extensions;

namespace UnturnedBlackout.Commands;

internal class WipeSpawnsCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Both;

    public string Name => "wipespawns";

    public string Help => "Wipe spawns";

    public string Syntax => "/wipespawns (MapID) (Gamemode)";

    public List<string> Aliases => new();

    public List<string> Permissions => new();

    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (command.Length < 2 || !int.TryParse(command[0], out var mapID) || !Enum.TryParse(command[1], true, out EGameType gameMode))
        {
            Utility.Say(caller, Plugin.Instance.Translate("Correct_Usage", Syntax));
            return;
        }

        switch (gameMode)
        {
            case EGameType.FFA:
                _ = Plugin.Instance.Data.Data.FFASpawnPoints.RemoveAll(k => k.LocationID == mapID);
                break;
            case EGameType.KC:
            case EGameType.TDM:
                _ = Plugin.Instance.Data.Data.TDMSpawnPoints.RemoveAll(k => k.LocationID == mapID);
                break;
            case EGameType.CTF:
                _ = Plugin.Instance.Data.Data.CTFSpawnPoints.RemoveAll(k => k.LocationID == mapID);
                break;
        }

        Plugin.Instance.Data.SaveJson();
        Utility.Say(caller, $"[color=green]Wiped spawnpoints for map {mapID} for gamemode {gameMode}[/color]");
    }
}