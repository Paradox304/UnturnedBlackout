namespace UnturnedBlackout.Models.Webhook;

public class Author
{
    public string Name { get; set; }
    public string URL { get; set; }
    public string IconURL { get; set; }

    public Author(string name, string url, string iconURL)
    {
        Name = name;
        URL = url;
        IconURL = iconURL;
    }
}