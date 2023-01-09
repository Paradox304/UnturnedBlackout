using UnturnedBlackout.FileReaders;
using UnturnedBlackout.Models.Configuration;
using UnturnedBlackout.Models.CTF;
using UnturnedBlackout.Models.FFA;
using UnturnedBlackout.Models.KC;
using UnturnedBlackout.Models.TDM;

namespace UnturnedBlackout.Managers;

public class ConfigManager
{
    public XmlFileReader<BaseValuesConfig> Base { get; set; }
    public XmlFileReader<PointsConfig> Points { get; set; }
    public XmlFileReader<MedalsConfig> Medals { get; set; }
    public XmlFileReader<LoadoutConfig> Loadout { get; set; }
    public XmlFileReader<DefaultSkillsConfig> DefaultSkills { get; set; }
    public XmlFileReader<TeamsConfig> Teams { get; set; }
    public XmlFileReader<FFAConfig> FFA { get; set; }
    public XmlFileReader<TDMConfig> TDM { get; set; }
    public XmlFileReader<KCConfig> KC { get; set; }
    public XmlFileReader<CTFConfig> CTF { get; set; }
    public XmlFileReader<LocationsConfig> Locations { get; set; }
    public XmlFileReader<KillFeedConfig> Killfeed { get; set; }
    public XmlFileReader<GamemodeConfig> Gamemode { get; set; }
    public XmlFileReader<WinningValuesConfig> WinningValues { get; set; }
    public XmlFileReader<RoundEndCasesConfig> RoundEndCases { get; set; }
    public XmlFileReader<IconsConfig> Icons { get; set; }
    public XmlFileReader<KillstreakConfig> Killstreaks { get; set; }
    public XmlFileReader<DeathstreakConfig> Deathstreaks { get; set; }
    public XmlFileReader<AbilitiesConfig> Abilities { get; set; }
    public XmlFileReader<WebhooksConfig> Webhooks { get; set; }
    public XmlFileReader<GameEventConfig> Events { get; set; }
    
    public ConfigManager()
    {
        var configInstance = Plugin.Instance.Configuration.Instance;

        Base = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.BaseValues.xml", configInstance.URL + "Configuration.BaseValues.xml");
        Points = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.Points.xml", configInstance.URL + "Configuration.Points.xml");
        Medals = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.Medals.xml", configInstance.URL + "Configuration.Medals.xml");
        FFA = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.FFA.xml", configInstance.URL + "Configuration.FFA.xml");
        TDM = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.TDM.xml", configInstance.URL + "Configuration.TDM.xml");
        KC = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.KC.xml", configInstance.URL + "Configuration.KC.xml");
        CTF = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.CTF.xml", configInstance.URL + "Configuration.CTF.xml");
        Loadout = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.Loadouts.xml", configInstance.URL + "Configuration.Loadouts.xml");
        DefaultSkills = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.Skills.xml", configInstance.URL + "Configuration.Skills.xml");
        Teams = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.Teams.xml", configInstance.URL + "Configuration.Teams.xml");
        Locations = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.Locations.xml", configInstance.URL + "Configuration.Locations.xml");
        Killfeed = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.Killfeed.xml", configInstance.URL + "Configuration.Killfeed.xml");
        Gamemode = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.Gamemode.xml", configInstance.URL + "Configuration.Gamemode.xml");
        WinningValues = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.WinningValues.xml", configInstance.URL + "Configuration.WinningValues.xml");
        RoundEndCases = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.RoundEndCases.xml", configInstance.URL + "Configuration.RoundEndCases.xml");
        Icons = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.Icons.xml", configInstance.URL + "Configuration.Icons.xml");
        Killstreaks = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.Killstreaks.xml", configInstance.URL + "Configuration.Killstreaks.xml");
        Deathstreaks = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.Deathstreaks.xml", configInstance.URL + "Configuration.Deathstreaks.xml");
        Abilities = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.Abilities.xml", configInstance.URL + "Configuration.Abilities.xml");
        Webhooks = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.Webhooks.xml", configInstance.URL + "Configuration.Webhooks.xml");
        Events = new(Plugin.Instance.Directory + "/LocalStorage/Configuration.Events.xml", configInstance.URL + "Configuration.Events.xml");
    }
}