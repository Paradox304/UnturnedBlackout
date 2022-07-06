using Rocket.API;
using Rocket.Core.Utils;
using Rocket.Unturned.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnturnedBlackout.Commands
{
    class GenAchievementCommand : IRocketCommand
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
                UnturnedChat.Say(caller, $"Correct Usage: {Syntax}");
                return;
            }

            if (!int.TryParse(command[0], out int achievementID))
            {
                UnturnedChat.Say(caller, "Achievement tier is not correct");
                return;
            }

            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await Plugin.Instance.DBManager.GenerateAchievementTiersAsync(achievementID, command[1]);

                TaskDispatcher.QueueOnMainThread(() => UnturnedChat.Say(caller, $"Tiers generated for {achievementID} with title {command[1]}"));
            });
        }
    }
}
