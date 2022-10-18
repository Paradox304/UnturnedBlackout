// ReSharper disable UnusedAutoPropertyAccessor.Local

using System;

namespace UnturnedBlackout.Models.Webhook;

[Serializable]
public class Embed
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string URL { get; set; }
    public string Color { get; set; }
    public string Timestamp { get; set; }
    public Footer Footer { get; set; }
    public Author Author { get; set; }
    public Field[] Fields { get; set; }
    public string Thumbnail { get; set; }
    public string Image { get; set; }

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