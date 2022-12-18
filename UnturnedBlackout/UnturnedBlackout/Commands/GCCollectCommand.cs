using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rocket.API;
using UnturnedBlackout.Extensions;

namespace UnturnedBlackout.Commands;

public class GCCollectCommand : IRocketCommand
{
    public void Execute(IRocketPlayer caller, string[] command)
    {
        Logging.Debug($"Generation 0: {GC.CollectionCount(0)}", ConsoleColor.Cyan);
        Logging.Debug($"Generation 1: {GC.CollectionCount(1)}", ConsoleColor.Cyan);
        Logging.Debug($"Generation 2: {GC.CollectionCount(2)}", ConsoleColor.Cyan);
        Logging.Debug("INTIATED GARBAGE COLLECTION", ConsoleColor.Cyan);
        Task.Run(GC.Collect);
    }

    public AllowedCaller AllowedCaller => AllowedCaller.Console;
    public string Name => "gccollect";
    public string Help => "Initiates a garbage collection, DO NOT USE IT NOW AND THEN";
    public string Syntax => "/gccollect";
    public List<string> Aliases => new();
    public List<string> Permissions => new();
}