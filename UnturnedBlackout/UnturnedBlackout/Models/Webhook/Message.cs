namespace UnturnedBlackout.Models.Webhook;

public class Message
{
    public string username { get; set; }

    public string avatar_url { get; set; }

    public Embed[] embeds { get; set; }

    public Message()
    {
    }

    public Message(string username, string avatar_url, Embed[] embeds)
    {
        this.username = username;
        this.avatar_url = avatar_url;
        this.embeds = embeds;
    }
}