using Newtonsoft.Json;

namespace UnturnedBlackout.Models.Webhook
{
    [JsonObject]
    public class Message
    {
        public string username;

        public string avatar_url;

        public Embed[] embeds;

        public Message(string username, string avatar_url, Embed[] embeds)
        {
            this.username = username;
            this.avatar_url = avatar_url;
            this.embeds = embeds;
        }
    }
}
