using System;

namespace UnturnedBlackout.Models.Configuration;

[Serializable]
public class WebhooksConfig
{
    public string LeaderboardWebhookLink { get; set; }
    public string UnboxedWebhookLink { get; set; }
    public string SpecialUnboxedWebhookLink { get; set; }
    public string CaseDroppedWebhookLink { get; set; }
    public string MuteWebhookLink { get; set; }
    public string UnmuteWebhookLink { get; set; }
    public string PluginWarningsWebhookLink { get; set; }

    public WebhooksConfig()
    {
        
    }
}