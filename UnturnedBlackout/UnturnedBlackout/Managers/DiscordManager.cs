using System.Net;
using System.Text;
using UnturnedBlackout.Models.Webhook;
using System.Text.Json;

namespace UnturnedBlackout.Managers
{
    public static class DiscordManager
    {
        public static void SendEmbed(Embed embed, string name, string webhookurl)
        {
            Message webhookMessage = new(name, null, new Embed[1] { embed });
            SendHook(webhookMessage, webhookurl);
        }

        public static void SendHook(Message embed, string webhookUrl)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(embed));
            using WebClient webClient = new();
            WebHeaderCollection headers = webClient.Headers;
            headers.Set(HttpRequestHeader.ContentType, "application/json");
            webClient.UploadData(webhookUrl, bytes);
        }
    }
}
