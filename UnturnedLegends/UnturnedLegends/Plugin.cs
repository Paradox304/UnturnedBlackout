using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedLegends.Managers;

namespace UnturnedLegends
{
    public class Plugin : RocketPlugin<Config>
    {
        protected override void Load()
        {
            Instance = this;
            DataManager = new DataManager();

            Logger.Log("Unturned Legends has been loaded");
        }


        protected override void Unload()
        {
            Logger.Log("Unturned Legends has been unloaded");
        }

        public DataManager DataManager { get; set; }
        public static Plugin Instance { get; set; }
    }
}
