namespace UnturnedBlackout.Models.Webhook;

public class Message
{
    private string Username { get; set; }

    private string AvatarURL { get; set; }

    private Embed[] Embeds { get; set; }

    public Message()
    {
    }

    public Message(string username, string avatarURL, Embed[] embeds)
    {
        Username = username;
        AvatarURL = avatarURL;
        Embeds = embeds;
    }
}