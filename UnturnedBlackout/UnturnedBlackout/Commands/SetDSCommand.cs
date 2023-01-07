using System.Collections.Generic;
using Rocket.API;
using Rocket.Unturned.Player;
using UnturnedBlackout.Extensions;

namespace UnturnedBlackout.Commands;

public class SetDSCommand : IRocketCommand
{
    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (caller is not UnturnedPlayer player)
            return;
        
        if (command.Length == 0 || !int.TryParse(command[0], out var deaths))
        {
            Utility.Say(caller, Plugin.Instance.Translate("Correct_Usage", Syntax));
            return;
        }

        var gPlayer = Plugin.Instance.Game.GetGamePlayer(player);
        if (gPlayer == null)
        {
            Utility.Say(caller, "[color=red]Some error happened[/color]");
            return;
        }

        if (gPlayer.CurrentGame == null)
        {
            Utility.Say(caller, "[color=red]You are not in a game[/color]");
            return;
        }
        
        gPlayer.UpdateDeathstreak(deaths);
        Utility.Say(caller, $"[color=green]Setted deathstreak to {deaths}[/color]");
    }

    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "setds";
    public string Help => "Set your deathstreak in a game";
    public string Syntax => "/setds (deaths)";
    public List<string> Aliases => new();
    public List<string> Permissions => new();
}