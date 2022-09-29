namespace UnturnedBlackout.Models.Webhook;

public class Embed
{
    public string title { get; set; } = null;
    public string description { get; set; } = null;
    public string url { get; set; } = null;
    public string color { get; set; } = null;
    public string timestamp { get; set; } = null;
    public Footer footer { get; set; } = null;
    public Author author { get; set; } = null;
    public Field[] fields { get; set; } = null;
    public string thumbnail { get; set; } = null;
    public string image { get; set; } = null;

    public Embed(
        string title,
        string description,
        string url,
        string color,
        string timestamp,
        Footer footer,
        Author author,
        Field[] fields,
        string thumbnail,
        string image)
    {
        this.title = title;
        this.description = description;
        this.url = url;
        this.color = color;
        this.timestamp = timestamp;
        this.footer = footer;
        this.author = author;
        this.fields = fields;
        this.thumbnail = thumbnail;
        this.image = image;
    }
}