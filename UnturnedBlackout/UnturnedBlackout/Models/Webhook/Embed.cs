// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace UnturnedBlackout.Models.Webhook;

public class Embed
{
    private string Title { get; set; }
    private string Description { get; set; }
    private string URL { get; set; }
    private string Color { get; set; }
    private string Timestamp { get; set; }
    private Footer Footer { get; set; }
    private Author Author { get; set; }
    public Field[] Fields { get; set; }
    private string Thumbnail { get; set; }
    private string Image { get; set; }

    public Embed(string title, string description, string url, string color, string timestamp, Footer footer, Author author, Field[] fields, string thumbnail, string image)
    {
        Title = title;
        Description = description;
        URL = url;
        Color = color;
        Timestamp = timestamp;
        Footer = footer;
        Author = author;
        Fields = fields;
        Thumbnail = thumbnail;
        Image = image;
    }
}