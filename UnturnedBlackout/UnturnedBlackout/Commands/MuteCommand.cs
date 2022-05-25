using Rocket.API;
using Rocket.Core.Steam;
using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Managers;
using UnturnedBlackout.Models.Webhook;

namespace UnturnedBlackout.Commands
{
    class MuteCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "mute";

        public string Help => "Mute a player";

        public string Syntax => "/mute (PlayerName/SteamID) [Seconds] [Reason]";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 3)
            {
                Utility.Say(caller, Plugin.Instance.Translate("Correct_Usage", Syntax).ToRich());
                return;
            }

            var steamID = CSteamID.Nil;
            if (!ulong.TryParse(command[0], out ulong steamid))
            {
                steamID = PlayerTool.getPlayer(command[0])?.channel?.owner?.playerID?.steamID ?? CSteamID.Nil;
            } else
            {
                steamID = new CSteamID(steamid);
            }

            if (steamID == CSteamID.Nil)
            {
                Utility.Say(caller, "<color=red>Player not found</color>");
                return;
            }

            if (!int.TryParse(command[1], out int seconds))
            {
                Utility.Say(caller, "<color=red>Seconds is not in the correct format</color>");
                return;
            }

            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                Profile profile;
                try
                {
                    profile = new Profile(steamID.m_SteamID);
                } catch (Exception)
                {
                    TaskDispatcher.QueueOnMainThread(() => Utility.Say(caller, "<color=red>Player not found</color>"));
                    return;
                }

                var expiry = DateTimeOffset.UtcNow.AddSeconds(seconds);
                await Plugin.Instance.DBManager.ChangePlayerMutedAsync(steamID, true);
                await Plugin.Instance.DBManager.ChangePlayerMuteExpiryAsync(steamID, expiry);

                if (Provider.clients.Exists(k => k.playerID.steamID == steamID))
                {
                    TaskDispatcher.QueueOnMainThread(() => Utility.Say(UnturnedPlayer.FromCSteamID(steamID), Plugin.Instance.Translate("Muted", seconds, command[2]).ToRich()));
                }

                TaskDispatcher.QueueOnMainThread(() => Utility.Say(caller, $"<color=green>Player has been muted for {seconds} for {command[2]}</color>"));

                Embed embed = new Embed(null, $"**{profile.SteamID}** was muted for **{seconds}** second(s)", null, "15105570", DateTime.UtcNow.ToString("s"),
                                        new Footer(Provider.serverName, Provider.configData.Browser.Icon),
                                        new Author(profile.SteamID, $"https://steamcommunity.com/profiles/{profile.SteamID64}/", profile.AvatarIcon.ToString()),
                                        new Field[]
                                        {
                                            new Field("**Reason:**", $"**{command[2]}**", true),
                                            new Field("**Expiry:**", $"__**{expiry.UtcDateTime}**__", true),
                                            new Field("**Muter:**", $"{(caller is UnturnedPlayer player ? $"[**{player.SteamName}**](https://steamcommunity.com/profiles/{player.CSteamID}/)" : "**Console**")}", true),
                                            new Field("**Time:**", DateTime.UtcNow.ToString(), true)
                                        },
                                        null, null);
                if (!string.IsNullOrEmpty(Plugin.Instance.Configuration.Instance.WebhookURL))
                    DiscordManager.SendEmbed(embed, "Player Muted", Plugin.Instance.Configuration.Instance.WebhookURL);
            });
        }
    }
}
