using System;
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
        Utility.Say(caller, $"TPS: {Provider.debugTPS}");
        Utility.Say(caller, $"UPS: {Provider.debugUPS}");
    }

    public AllowedCaller AllowedCaller => AllowedCaller.Both;
    public string Name => "tps";
    public string Help => "Check the tps of the server";
    public string Syntax => "/tps";
    public List<string> Aliases => new();
    public List<string> Permissions => new();
}