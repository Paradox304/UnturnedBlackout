﻿using UnturnedBlackout.FileReaders;
using UnturnedBlackout.Models.Configuration;
using UnturnedBlackout.Models.CTF;
using UnturnedBlackout.Models.FFA;
using UnturnedBlackout.Models.KC;
using UnturnedBlackout.Models.TDM;

namespace UnturnedBlackout.Managers
{
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

        public ConfigManager()
        {
            var configInstance = Plugin.Instance.Configuration.Instance;

            Base = new(Plugin.Instance.Directory + "/Configuration.BaseValues.xml", configInstance.URL + "Configuration.BaseValues.xml");
            Points = new(Plugin.Instance.Directory + "/Configuration.Points.xml", configInstance.URL + "Configuration.Points.xml");
            Medals = new(Plugin.Instance.Directory + "/Configuration.Medals.xml", configInstance.URL + "Configuration.Medals.xml");
            FFA = new(Plugin.Instance.Directory + "/Configuration.FFA.xml", configInstance.URL + "Configuration.FFA.xml");
            TDM = new(Plugin.Instance.Directory + "/Configuration.TDM.xml", configInstance.URL + "Configuration.TDM.xml");
            KC = new(Plugin.Instance.Directory + "/Configuration.KC.xml", configInstance.URL + "Configuration.KC.xml");
            CTF = new(Plugin.Instance.Directory + "/Configuration.CTF.xml", configInstance.URL + "Configuration.CTF.xml");
            Loadout = new(Plugin.Instance.Directory + "/Configuration.Loadouts.xml", configInstance.URL + "Configuration.Loadouts.xml");
            DefaultSkills = new(Plugin.Instance.Directory + "/Configuration.Skills.xml", configInstance.URL + "Configuration.Skills.xml");
            Teams = new(Plugin.Instance.Directory + "/Configuration.Teams.xml", configInstance.URL + "Configuration.Teams.xml");
            Locations = new(Plugin.Instance.Directory + "/Configuration.Locations.xml", configInstance.URL + "Configuration.Locations.xml");
            Killfeed = new(Plugin.Instance.Directory + "/Configuration.Killfeed.xml", configInstance.URL + "Configuration.Killfeed.xml");
            Gamemode = new(Plugin.Instance.Directory + "/Configuration.Gamemode.xml", configInstance.URL + "Configuration.Gamemode.xml");
        }
    }
}
