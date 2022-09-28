using Rocket.API;
using Rocket.Core.Utils;
using Rocket.Unturned.Chat;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnturnedBlackout.Commands;

class GenAchievementCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller
    {
        get
        {
            return AllowedCaller.Both;
        }
    }

    public string Name
    {
        get
        {
            return "genachievement";
        }
    }

    public string Help
    {
        get
        {
            return "Create 5 achievement tiers in the database";
        }
    }

    public string Syntax
    {
        get
        {
            return "/genachievement (AchievementID) (TierTitle)";
        }
    }

    public List<string> Aliases
    {
        get
        {
            return new();
        }
    }

    public List<string> Permissions
    {
        get
        {
            return new();
        }
    }

    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (command.Length < 2)
        {
            UnturnedChat.Say(caller, $"Correct Usage: {Syntax}");
            return;
        }

        if (!int.TryParse(command[0], out var achievementID))
        {
            UnturnedChat.Say(caller, "Achievement tier is not correct");
            return;
        }

        _ = Task.Run(async () =>
        {
            await Plugin.Instance.DB.GenerateAchievementTiersAsync(achievementID, command[1]);

            TaskDispatcher.QueueOnMainThread(() => UnturnedChat.Say(caller, $"Tiers generated for {achievementID} with title {command[1]}"));
        });
    }
}
