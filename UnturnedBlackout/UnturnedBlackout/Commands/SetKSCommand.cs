using System.Collections.Generic;
using Rocket.API;
using Rocket.Unturned.Player;
using UnturnedBlackout.Extensions;

namespace UnturnedBlackout.Commands;

public class SetKSCommand : IRocketCommand
{
    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (caller is not UnturnedPlayer player)
            return;
        
        if (command.Length == 0 || !int.TryParse(command[0], out var kills))
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
        
        gPlayer.UpdateKillstreak(kills);
        Utility.Say(caller, $"[color=green]Setted killstreak to {kills}[/color]");
    }

    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "setks";
    public string Help => "Set your killstreak in a game";
    public string Syntax => "/setks (kills)";
    public List<string> Aliases => new();
    public List<string> Permissions => new();
}