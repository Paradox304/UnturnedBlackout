using System;
using JetBrains.Annotations;
// ReSharper disable InconsistentNaming

namespace UnturnedBlackout.Models.Webhook;

[Serializable]
public class Thumbnail
{
    public string url { get; set; }
    
    public Thumbnail(string url)
    {
        this.url = url;
    }
}