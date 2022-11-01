using Rocket.API;
using Rocket.Core.Steam;
using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnturnedBlackout.Extensions;
using UnturnedBlackout.Managers;
using UnturnedBlackout.Models.Webhook;

namespace UnturnedBlackout.Commands;

internal class MuteCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Both;

    public string Name => "mute";

    public string Help => "Mute a player";

    public string Syntax => "/mute (PlayerName/SteamID) (Time) (Reason)";

    public List<string> Aliases => new();

    public List<string> Permissions => new();

    public void Execute(IRocketPlayer caller, string[] command)
    {
        if (command.Length < 3)
        {
            Utility.Say(caller, Plugin.Instance.Translate("Correct_Usage", Syntax).ToRich());
            return;
        }

        // ReSharper disable once Unity.NoNullPropagation
        var steamID = !ulong.TryParse(command[0], out var steamid) ? PlayerTool.getPlayer(command[0])?.channel?.owner?.playerID?.steamID ?? CSteamID.Nil : new(steamid);

        if (steamID == CSteamID.Nil)
        {
            Utility.Say(caller, "<color=red>Player not found</color>");
            return;
        }

        var regex = new Regex("[0-9]+");
        var match = regex.Match(command[1]);
        if (!match.Success || (!command[1].Contains("d") && !command[1].Contains("h")))
        {
            Utility.Say(caller, "<color=red>Time is not in the correct format</color>");
            return;
        }
        
        if (!int.TryParse(match.Value, out var time))
        {
            Utility.Say(caller, "<color=red>Time is not in the correct format</color>");
            return;
        }

        var expiry = command[1].Contains("d") ? DateTimeOffset.UtcNow.AddDays(time) : DateTimeOffset.UtcNow.AddHours(time);
        var amount = $"{time} {(command[1].Contains("d") ? "day(s)" : "hour(s)")}";
        
        _ = Task.Run(() =>
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

            Plugin.Instance.DB.ChangePlayerMuted(steamID, true);
            Plugin.Instance.DB.ChangePlayerMuteExpiry(steamID, expiry);

            if (Provider.clients.Exists(k => k.playerID.steamID == steamID))
                TaskDispatcher.QueueOnMainThread(() => Utility.Say(UnturnedPlayer.FromCSteamID(steamID), Plugin.Instance.Translate("Muted", amount, command[2]).ToRich()));

            TaskDispatcher.QueueOnMainThread(() => Utility.Say(caller, $"<color=green>Player has been muted for {amount} for {command[2]}</color>"));

            Embed embed = new(null, $"**{profile.SteamID}** was muted for **{time}** {(command[1].Contains("d") ? "day(s)" : "hour(s)")}", null, "15105570", DateTime.UtcNow.ToString("s"), new(Provider.serverName, Provider.configData.Browser.Icon),
                new(profile.SteamID, $"https://steamcommunity.com/profiles/{profile.SteamID64}/", profile.AvatarIcon.ToString()),
                new Field[]
                {
                    new("**Reason:**", $"**{command[2]}**", true), new("**Expiry:**", $"__**{expiry.UtcDateTime}**__", true), new("**Muter:**", $"{(caller is UnturnedPlayer player ? $"[**{player.SteamName}**](https://steamcommunity.com/profiles/{player.CSteamID}/)" : "**Console**")}", true),
                    new("**Time:**", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture), true)
                }, null, null);

            if (!string.IsNullOrEmpty(Plugin.Instance.Configuration.Instance.WebhookURL))
                Plugin.Instance.Discord.SendEmbed(embed, "Player Muted", Plugin.Instance.Configuration.Instance.WebhookURL);
        });
    }
}