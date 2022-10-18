using System;

namespace UnturnedBlackout.Models.Webhook;

[Serializable]
public class Footer
{
    public string Text { get; set; }
    public string IconURL { get; set; }

    public Footer(string text, string iconURL)
    {
        Text = text;
        IconURL = iconURL;
    }
}