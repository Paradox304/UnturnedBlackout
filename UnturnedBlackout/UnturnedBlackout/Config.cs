using Rocket.API;

namespace UnturnedBlackout;

public class Config : IRocketPluginConfiguration
{
    public string DatabaseHost { get; set; }
    public string DatabaseUsername { get; set; }
    public string DatabaseName { get; set; }
    public string DatabasePassword { get; set; }
    public string DatabasePort { get; set; }

    public string WebhookURL { get; set; }
    public string URL { get; set; }
    public bool UnlockAllItems { get; set; }
    public string IP { get; set; }

    public void LoadDefaults()
    {
        DatabaseHost = "136.243.135.46";
        DatabaseUsername = "u476_0TqwYpW0Pe";
        DatabaseName = "s476_deathmatch";
        DatabasePassword = "k8gxtTbytcA5DXlqbn86e@+1";
        DatabasePort = "3306";
        WebhookURL = "https://discord.com/api/webhooks/979000847197409280/e7Pbmjj_8bALCCDCbEDMCEVLX2ZSuIG3ymxbd-yb-IxkQ-sToxCkLJRmneeqB6LYVwgC";
        URL = "http://213.32.6.3:27090/";
        UnlockAllItems = true;
        IP = "";
    }
}
