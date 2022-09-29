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

namespace UnturnedBlackout.Commands;

internal class MuteCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Both;

    public string Name => "mute";

    public string Help => "Mute a player";

    public string Syntax => "/mute (PlayerName/SteamID) [Seconds] [Reason]";

    public List<string> Aliases => new();

    public List<string> Permissions => new();

    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (command.Length < 3)
        {
            Utility.Say(caller, Plugin.Instance.Translate("Correct_Usage", Syntax).ToRich());
            return;
        }

        var steamID = CSteamID.Nil;
        steamID = !ulong.TryParse(command[0], out var steamid)
            ? PlayerTool.getPlayer(command[0])?.channel?.owner?.playerID?.steamID ?? CSteamID.Nil
            : new(steamid);

        if (steamID == CSteamID.Nil)
        {
            Utility.Say(caller, "<color=red>Player not found</color>");
            return;
        }

        if (!int.TryParse(command[1], out var seconds))
        {
            Utility.Say(caller, "<color=red>Seconds is not in the correct format</color>");
            return;
        }

        _ = Task.Run(async () =>
        {
            Profile profile;
            try
            {
                profile = new(steamID.m_SteamID);
            }
            catch (Exception)
            {
                TaskDispatcher.QueueOnMainThread(() => Utility.Say(caller, "<color=red>Player not found</color>"));
                return;
            }

            var expiry = DateTimeOffset.UtcNow.AddSeconds(seconds);
            await Plugin.Instance.DB.ChangePlayerMutedAsync(steamID, true);
            await Plugin.Instance.DB.ChangePlayerMuteExpiryAsync(steamID, expiry);

            if (Provider.clients.Exists(k => k.playerID.steamID == steamID))
                TaskDispatcher.QueueOnMainThread(() => Utility.Say(UnturnedPlayer.FromCSteamID(steamID),
                    Plugin.Instance.Translate("Muted", seconds, command[2]).ToRich()));

            TaskDispatcher.QueueOnMainThread(() =>
                Utility.Say(caller, $"<color=green>Player has been muted for {seconds} for {command[2]}</color>"));

            Embed embed = new(null, $"**{profile.SteamID}** was muted for **{seconds}** second(s)", null, "15105570",
                DateTime.UtcNow.ToString("s"), new(Provider.serverName, Provider.configData.Browser.Icon),
                new(profile.SteamID, $"https://steamcommunity.com/profiles/{profile.SteamID64}/",
                    profile.AvatarIcon.ToString()), new Field[]
                {
                    new("**Reason:**", $"**{command[2]}**", true),
                    new("**Expiry:**", $"__**{expiry.UtcDateTime}**__", true),
                    new("**Muter:**",
                        $"{(caller is UnturnedPlayer player ? $"[**{player.SteamName}**](https://steamcommunity.com/profiles/{player.CSteamID}/)" : "**Console**")}",
                        true),
                    new("**Time:**", DateTime.UtcNow.ToString(), true)
                }, null, null);
            if (!string.IsNullOrEmpty(Plugin.Instance.Configuration.Instance.WebhookURL))
                DiscordManager.SendEmbed(embed, "Player Muted", Plugin.Instance.Configuration.Instance.WebhookURL);
        });
    }
}