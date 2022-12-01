using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Timers;
using UnturnedBlackout.Extensions;
using UnturnedBlackout.Models.Webhook;
using Logger = Rocket.Core.Logging.Logger;

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

        var messageCount = 0;

        List<KeyValuePair<string, (string, string, List<Embed>)>> pendingWebhooks;
        lock (PendingWebhooks)
        {
            pendingWebhooks = PendingWebhooks.ToList();
            PendingWebhooks.Clear();
        }

        try
        {
            Logging.Debug($"Sending {pendingWebhooks.Sum(k => k.Value.Item3.Count)} pending webhooks on {pendingWebhooks.Count} links");
            foreach (var pendingWebhook in pendingWebhooks.ToList())
            {
                var shouldBreak = false;
                Logging.Debug($"Sending {pendingWebhook.Value.Item3.Count} for {pendingWebhook.Key}");
                while (pendingWebhook.Value.Item3.Count > 0)
                {
                    Logging.Debug($"Embeds {pendingWebhook.Value.Item3.Count}");
                    Embed[] embeds;
                    if (pendingWebhook.Value.Item3.Count > 10)
                    {
                        Logging.Debug("Embeds more than 10, take only 10");
                        embeds = pendingWebhook.Value.Item3.Take(10).ToArray();
                        pendingWebhook.Value.Item3.RemoveRange(0, 10);
                    }
                    else
                    {
                        Logging.Debug("Embeds less than 10, take all");
                        embeds = pendingWebhook.Value.Item3.ToArray();
                        pendingWebhook.Value.Item3.Clear();
                    }

                    Logging.Debug($"Embeds left {pendingWebhook.Value.Item3.Count}");
                    Logging.Debug($"Sending hook with username: {pendingWebhook.Value.Item1} and avatar: {pendingWebhook.Value.Item2} and embeds: {embeds.Length} to {pendingWebhook.Key}");
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
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Error sending embeds");
        }
        finally
        {
            if (pendingWebhooks.Count > 0)
            {
                Logging.Debug("Pending webhooks are remaining, add them back to the queue to clean");
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
        }
    }

    public void SendEmbed(Embed embed, string name, string webhookURL, string avatarURL = "")
    {
        if (string.IsNullOrEmpty(webhookURL))
            return;
        
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