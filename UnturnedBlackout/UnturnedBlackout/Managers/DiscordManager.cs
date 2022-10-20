using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Timers;
using UnturnedBlackout.Helpers;
using UnturnedBlackout.Models.Webhook;

namespace UnturnedBlackout.Managers;

public class DiscordManager
{
    public Dictionary<string, (string, string, List<Embed>)> PendingWebhooks { get; set; }
    private Timer WebhookSender { get; set; }
    
    public DiscordManager()
    {
        PendingWebhooks = new();
        WebhookSender = new(10 * 1000);
        WebhookSender.Elapsed += SendWebhooks;
        WebhookSender.Start();
    }

    private void SendWebhooks(object sender, ElapsedEventArgs e)
    {
        if (PendingWebhooks.Count == 0)
            return;
        
        Logging.Debug($"Sending {PendingWebhooks.Sum(k => k.Value.Item3.Count)} pending webhooks on {PendingWebhooks.Count} links");
        var messageCount = 0;

        List<KeyValuePair<string, (string, string, List<Embed>)>> pendingWebhooks;
        lock (PendingWebhooks)
        {
            pendingWebhooks = PendingWebhooks.ToList();
            PendingWebhooks.Clear();
        }
        
        foreach (var pendingWebhook in pendingWebhooks)
        {
            var shouldBreak = false;
            while (pendingWebhook.Value.Item3.Count > 0)
            {
                Embed[] embeds;
                if (pendingWebhook.Value.Item3.Count > 10)
                {
                    embeds = pendingWebhook.Value.Item3.Take(10).ToArray();
                    pendingWebhook.Value.Item3.RemoveRange(0, 10);
                }
                else
                {
                    embeds = pendingWebhook.Value.Item3.ToArray();
                    pendingWebhook.Value.Item3.Clear();
                }
                
                SendHook(new(pendingWebhook.Value.Item1, pendingWebhook.Value.Item2, embeds), pendingWebhook.Key);
                messageCount++;

                if (messageCount != 30)
                    continue;

                shouldBreak = true;
                break;
            }

            if (shouldBreak)
                break;
            
            pendingWebhooks.RemoveAll(k => k.Key == pendingWebhook.Key);
        }

        if (pendingWebhooks.Count == 0)
            return;

        lock (PendingWebhooks)
        {
            foreach (var pendingWebhook in pendingWebhooks)
            {
                if (PendingWebhooks.ContainsKey(pendingWebhook.Key))
                    PendingWebhooks[pendingWebhook.Key].Item3.AddRange(pendingWebhook.Value.Item3);
                else
                    PendingWebhooks.Add(pendingWebhook.Key, pendingWebhook.Value);
            }
        }
    }

    public void SendEmbed(Embed embed, string name, string webhookURL, string avatarURL = "")
    {
        lock (PendingWebhooks)
        {
            if (PendingWebhooks.ContainsKey(webhookURL))
                PendingWebhooks[webhookURL].Item3.Add(embed);
            else
                PendingWebhooks.Add(webhookURL, (name, avatarURL, new() { embed }));
        }
    }

    public void ForceSendEmbed(Embed embed, string name, string webhookURL, string avatarURL = "")
    {
        SendHook(new(name, avatarURL, new[] { embed }), webhookURL);
    }
    
    private void SendHook(Message message, string webhookUrl)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        using WebClient webClient = new();
        var headers = webClient.Headers;
        headers.Set(HttpRequestHeader.ContentType, "application/json");
        var _= webClient.UploadData(webhookUrl, bytes);
    }
}