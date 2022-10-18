using System;

namespace UnturnedBlackout.Models.Webhook;

[Serializable]
public class Field
{
    public string Name { get; set; }
    public string Value { get; set; }
    public bool Inline { get; set; }

    public Field(string name, string value, bool inline)
    {
        Name = name;
        Value = value;
        Inline = inline;
    }
}