using System.Collections.Generic;
using System.Xml.Serialization;
using Rocket.API;

namespace UnturnedBlackout;

public class Config : IRocketPluginConfiguration
{
    public string DatabaseHost { get; set; }
    public string DatabaseUsername { get; set; }
    public string DatabaseName { get; set; }
    public string DatabasePassword { get; set; }
    public string DatabasePort { get; set; }
    public uint ConnectionTimeout { get; set; }
    public string WebhookURL { get; set; }
    public string URL { get; set; }
    public bool UnlockAllItems { get; set; }
    public bool AllowedToWipeDailyWeekly { get; set; }
    public int ServerID { get; set; }
    public int SurgeThreshold { get; set; }
    public float SurgeMultiplier { get; set; }
    public int SurgeSeconds { get; set; }
    [XmlArrayItem("SurgeServerID")]
    public List<int> SurgeServers { get; set; }
    
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
        ConnectionTimeout = 3;
        AllowedToWipeDailyWeekly = true;
        ServerID = 0;
        SurgeThreshold = 50;
        SurgeMultiplier = 0.5f;
        SurgeSeconds = 600;
        SurgeServers = new() { 1, 2 };
    }
}