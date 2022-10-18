// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable InconsistentNaming

using System;

namespace UnturnedBlackout.Models.Webhook;

[Serializable]
public class Embed
{
    public string title { get; set; }
    public string description { get; set; }
    public string url { get; set; }
    public string color { get; set; }
    public string timestamp { get; set; }
    public Footer footer { get; set; }
    public Author author { get; set; }
    public Field[] fields { get; set; }
    public string thumbnail { get; set; }
    public string image { get; set; }

    public Embed(string title, string description, string url, string color, string timestamp, Footer footer, Author author, Field[] fields, string thumbnail, string image)
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