using System.Net;
using System.Text;
using System.Text.Json;
using UnturnedBlackout.Models.Webhook;

namespace UnturnedBlackout.Managers;

public static class DiscordManager
{
    public static void SendEmbed(Embed embed, string name, string webhookurl)
    {
        Message webhookMessage = new(name, null, new Embed[1] { embed });
        SendHook(webhookMessage, webhookurl);
    }

    public static void SendHook(Message embed, string webhookUrl)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(embed));
        using WebClient webClient = new();
        var headers = webClient.Headers;
        headers.Set(HttpRequestHeader.ContentType, "application/json");
        _ = webClient.UploadData(webhookUrl, bytes);
    }
}