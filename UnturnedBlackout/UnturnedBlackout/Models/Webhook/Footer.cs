namespace UnturnedBlackout.Models.Webhook;

public class Footer
{
    private string Text { get; set; }
    private string IconURL { get; set; }

    public Footer(string text, string iconURL)
    {
        Text = text;
        IconURL = iconURL;
    }
}