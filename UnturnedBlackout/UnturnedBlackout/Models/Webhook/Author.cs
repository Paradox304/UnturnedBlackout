using System;
// ReSharper disable InconsistentNaming

namespace UnturnedBlackout.Models.Webhook;

[Serializable]
public class Author
{
    public string name { get; set; }
    public string url { get; set; }
    public string icon_url { get; set; }

    public Author(string name, string url, string iconURL)
    {
        this.name = name;
        this.url = url;
        icon_url = iconURL;
    }
}