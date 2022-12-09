using System.Collections.Generic;
using JetBrains.Annotations;
using Rocket.API;
using SDG.Unturned;
using UnturnedBlackout.Extensions;

namespace UnturnedBlackout.Commands;

[UsedImplicitly]
internal class TPSCommand : IRocketCommand
{
    public void Execute(IRocketPlayer caller, string[] command)
    {
        Utility.Say(caller, $"[color=green]TPS: {Provider.debugTPS}[/color]");
        Utility.Say(caller, $"[color=green]UPS: {Provider.debugUPS}[/color]");
    }

    public AllowedCaller AllowedCaller => AllowedCaller.Both;
    public string Name => "tps";
    public string Help => "Check the tps of the server";
    public string Syntax => "/tps";
    public List<string> Aliases => new();
    public List<string> Permissions => new();
}