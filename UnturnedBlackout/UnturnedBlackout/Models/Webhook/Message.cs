using System;
// ReSharper disable InconsistentNaming

namespace UnturnedBlackout.Models.Webhook;

[Serializable]
public class Message
{
    public string username { get; set; }

    public string avatar_url { get; set; }

    public Embed[] embeds { get; set; }

    public Message()
    {
    }

    public Message(string username, string avatarURL, Embed[] embeds)
    {
        this.username = username;
        avatar_url = avatarURL;
        this.embeds = embeds;
    }
}