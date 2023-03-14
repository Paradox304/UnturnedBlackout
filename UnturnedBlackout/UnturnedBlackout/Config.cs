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
        DatabaseHost = "localhost";
        DatabaseUsername = "username";
        DatabaseName = "name";
        DatabasePassword = "pass";
        DatabasePort = "3306";
        URL = "http://127.0.0.1:27015/"; // Example
        UnlockAllItems = true;
        AllowedToWipeDailyWeekly = true;
        ServerID = 0;
        SurgeThreshold = 50;
        SurgeMultiplier = 0.5f;
        SurgeSeconds = 600;
        SurgeServers = new() { 1, 2 };
    }
}