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

            DBManager = new DatabaseManager();
            DataManager = new DataManager();
            GameManager = new GameManager();

            Logger.Log("Unturned Legends has been loaded");
        }


        protected override void Unload()
        {
            GameManager.Destroy();

            Logger.Log("Unturned Legends has been unloaded");
        }

        public GameManager GameManager { get; set; }
        public DataManager DataManager { get; set; }
        public DatabaseManager DBManager { get; set; }
        public static Plugin Instance { get; set; }
    }
}
