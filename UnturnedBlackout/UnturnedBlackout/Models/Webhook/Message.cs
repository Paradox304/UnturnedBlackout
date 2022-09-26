namespace UnturnedBlackout.Models.Webhook
{
    public class Message
    {
        public string username;

        public string avatar_url;

        public Embed[] embeds;

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
}
