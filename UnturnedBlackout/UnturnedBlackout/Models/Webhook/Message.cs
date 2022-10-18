using System;

namespace UnturnedBlackout.Models.Webhook;

[Serializable]
public class Message
{
    public string Username { get; set; }

    public string AvatarURL { get; set; }

    public Embed[] Embeds { get; set; }

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