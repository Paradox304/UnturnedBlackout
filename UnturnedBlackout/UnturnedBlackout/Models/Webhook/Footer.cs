namespace UnturnedBlackout.Models.Webhook;

public class Footer
{
    public string text { get; set; }
    public string icon_url { get; set; }

    public Footer(string text, string icon_url)
    {
        this.text = text;
        this.icon_url = icon_url;
    }
}