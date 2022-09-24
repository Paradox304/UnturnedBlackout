using Rocket.API;
using Rocket.Core.Steam;
using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnturnedBlackout.Managers;
using UnturnedBlackout.Models.Webhook;

namespace UnturnedBlackout.Commands
{
    class UnmuteCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "unmute";

        public string Help => "Unmute a player";

        public string Syntax => "/unmute (PlayerName/SteamID)";

        public List<string> Aliases => new();

        public List<string> Permissions => new();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length == 0)
            {
                Utility.Say(caller, Plugin.Instance.Translate("Correct_Usage", Syntax).ToRich());
                return;
            }

            CSteamID steamID = CSteamID.Nil;
            if (!ulong.TryParse(command[0], out ulong steamid))
            {
                steamID = PlayerTool.getPlayer(command[0])?.channel?.owner?.playerID?.steamID ?? CSteamID.Nil;
            }
            else
            {
                steamID = new CSteamID(steamid);
            }

            if (steamID == CSteamID.Nil)
            {
                Utility.Say(caller, "<color=red>Player not found</color>");
                return;
            }

            Task.Run(async () =>
            {
                Profile profile;
                try
                {
                    profile = new Profile(steamID.m_SteamID);
                }
                catch (Exception)
                {
                    TaskDispatcher.QueueOnMainThread(() => Utility.Say(caller, "<color=red>Player not found</color>"));
                    return;
                }

                await Plugin.Instance.DB.ChangePlayerMutedAsync(steamID, false);

                if (Provider.clients.Exists(k => k.playerID.steamID == steamID))
                {
                    TaskDispatcher.QueueOnMainThread(() => Utility.Say(UnturnedPlayer.FromCSteamID(steamID), Plugin.Instance.Translate("Unmuted").ToRich()));
                }

                TaskDispatcher.QueueOnMainThread(() => Utility.Say(caller, $"<color=green>Player has been unmuted</color>"));

                Embed embed = new(null, $"**{profile.SteamID}** was unmuted", null, "15105570", DateTime.UtcNow.ToString("s"),
                                        new Footer(Provider.serverName, Provider.configData.Browser.Icon),
                                        new Author(profile.SteamID, $"https://steamcommunity.com/profiles/{profile.SteamID64}/", profile.AvatarIcon.ToString()),
                                        new Field[]
                                        {
                                            new Field("**Unmuter:**", $"{(caller is UnturnedPlayer player ? $"[**{player.SteamName}**](https://steamcommunity.com/profiles/{player.CSteamID}/)" : "**Console**")}", true),
                                            new Field("**Time:**", DateTime.UtcNow.ToString(), true)
                                        },
                                        null, null);
                if (!string.IsNullOrEmpty(Plugin.Instance.Configuration.Instance.WebhookURL))
                {
                    DiscordManager.SendEmbed(embed, "Player Unmuted", Plugin.Instance.Configuration.Instance.WebhookURL);
                }
            });
        }
    }
}
