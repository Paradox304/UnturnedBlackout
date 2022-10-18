using System;
// ReSharper disable InconsistentNaming

namespace UnturnedBlackout.Models.Webhook;

[Serializable]
public class Footer
{
    public string text { get; set; }
    public string icon_url { get; set; }

    public Footer(string text, string iconURL)
    {
        this.text = text;
        icon_url = iconURL;
    }
}