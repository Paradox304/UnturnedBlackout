using System.Net;
using System.Text;
using System.Text.Json;
using UnturnedBlackout.Helpers;
using UnturnedBlackout.Models.Webhook;

namespace UnturnedBlackout.Managers;

public static class DiscordManager
{
    public static void SendEmbed(Embed embed, string name, string webhookurl, string avatarURL = "")
    {
        Logging.Debug($"Embed Sent: \n {JsonSerializer.Serialize(embed)}");
        Message webhookMessage = new(name, avatarURL, new[] { embed });
        SendHook(webhookMessage, webhookurl);
    }

    private static void SendHook(Message message, string webhookUrl)
    {
        Logging.Debug($"Message Sent: \n {JsonSerializer.Serialize(message)}");
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        using WebClient webClient = new();
        var headers = webClient.Headers;
        headers.Set(HttpRequestHeader.ContentType, "application/json");
        _ = webClient.UploadData(webhookUrl, bytes);
    }
}