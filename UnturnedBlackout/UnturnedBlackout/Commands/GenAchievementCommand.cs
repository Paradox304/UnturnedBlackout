using Rocket.API;
using Rocket.Core.Utils;
using Rocket.Unturned.Chat;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnturnedBlackout.Extensions;

namespace UnturnedBlackout.Commands;

internal class GenAchievementCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Both;

    public string Name => "genachievement";

    public string Help => "Create 5 achievement tiers in the database";

    public string Syntax => "/genachievement (AchievementID) (TierTitle)";

    public List<string> Aliases => new();

    public List<string> Permissions => new();

    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (command.Length < 2)
        {
            Utility.Say(caller, Plugin.Instance.Translate("Correct_Usage", Syntax));
            return;
        }

        if (!int.TryParse(command[0], out var achievementID))
        {
            Utility.Say(caller, "[color=green]Achievement tier is not correct[/color]");
            return;
        }

        _ = Task.Run(async () =>
        {
            await Plugin.Instance.DB.GenerateAchievementTiersAsync(achievementID, command[1]);
            TaskDispatcher.QueueOnMainThread(() => Utility.Say(caller, $"[color=green]Tiers generated for {achievementID} with title {command[1]}[/color]"));
        });
    }
}