using System.Collections.Generic;
using UnturnedBlackout.FileReaders;
using UnturnedBlackout.Models.Configuration;
using UnturnedBlackout.Models.CTF;
using UnturnedBlackout.Models.FFA;
using UnturnedBlackout.Models.Global;
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
            Base = new(Plugin.Instance.Directory + "/Configuration.BaseValues.xml");
            Points = new(Plugin.Instance.Directory + "/Configuration.Points.xml");
            Medals = new(Plugin.Instance.Directory + "/Configuration.Medals.xml");
            FFA = new(Plugin.Instance.Directory + "/Configuration.FFA.xml");
            TDM = new(Plugin.Instance.Directory + "/Configuration.TDM.xml");
            KC = new(Plugin.Instance.Directory + "/Configuration.KC.xml");
            CTF = new(Plugin.Instance.Directory + "/Configuration.CTF.xml");
            Loadout = new(Plugin.Instance.Directory + "/Configuration.Loadouts.xml");
            DefaultSkills = new(Plugin.Instance.Directory + "/Configuration.Skills.xml");
            Teams = new(Plugin.Instance.Directory + "/Configuration.Teams.xml");
            Locations = new(Plugin.Instance.Directory + "/Configuration.Locations.xml");
            Killfeed = new(Plugin.Instance.Directory + "/Configuration.Killfeed.xml");
            Gamemode = new(Plugin.Instance.Directory + "/Configuration.Gamemode.xml");
        }
    }
}
