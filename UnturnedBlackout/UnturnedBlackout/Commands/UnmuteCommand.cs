using Rocket.API;
using Rocket.Core.Steam;
using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UnturnedBlackout.Extensions;
using UnturnedBlackout.Models.Webhook;

namespace UnturnedBlackout.Commands;

internal class UnmuteCommand : IRocketCommand
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
            Utility.Say(caller, Plugin.Instance.Translate("Correct_Usage", Syntax));
            return;
        }

        var steamID = CSteamID.Nil;
        steamID = !ulong.TryParse(command[0], out var steamid) ? PlayerTool.getPlayer(command[0])?.channel?.owner?.playerID?.steamID ?? CSteamID.Nil : new(steamid);

        if (steamID == CSteamID.Nil)
        {
            Utility.Say(caller, "[color=red]Player not found[/color]");
            return;
        }

        _ = Task.Run(() =>
        {
            Profile profile;
            try
            {
                profile = new(steamID.m_SteamID);
            }
            catch (Exception)
            {
                TaskDispatcher.QueueOnMainThread(() => Utility.Say(caller, "[color=red]Player not found[/color]"));
                return;
            }

            Plugin.Instance.DB.ChangePlayerMuted(steamID, false);

            if (Provider.clients.Exists(k => k.playerID.steamID == steamID))
                TaskDispatcher.QueueOnMainThread(() => Utility.Say(UnturnedPlayer.FromCSteamID(steamID), Plugin.Instance.Translate("Unmuted")));

            TaskDispatcher.QueueOnMainThread(() => Utility.Say(caller, $"[color=green]Player has been unmuted[/color]"));

            Embed embed = new(null, $"**{profile.SteamID}** was unmuted", null, "15105570", DateTime.UtcNow.ToString("s"), new(Provider.serverName, Provider.configData.Browser.Icon), new(profile.SteamID, $"https://steamcommunity.com/profiles/{profile.SteamID64}/", profile.AvatarIcon.ToString()),
                new Field[] { new("**Unmuter:**", $"{(caller is UnturnedPlayer player ? $"[**{player.SteamName}**](https://steamcommunity.com/profiles/{player.CSteamID}/)" : "**Console**")}", true), new("**Time:**", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture), true) }, null, null);

            if (!string.IsNullOrEmpty(Plugin.Instance.Config.Webhooks.FileData.UnmuteWebhookLink))
                Plugin.Instance.Discord.SendEmbed(embed, "Player Unmuted", Plugin.Instance.Config.Webhooks.FileData.UnmuteWebhookLink);
        });
    }
}