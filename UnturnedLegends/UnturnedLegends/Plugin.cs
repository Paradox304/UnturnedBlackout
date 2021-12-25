using HarmonyLib;
using Rocket.API.Collections;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            if (Harmony == null)
            {
                Harmony = new Harmony("UnturnedLegends");
                Harmony.PatchAll(Assembly);
            }

            DBManager = new DatabaseManager();
            DataManager = new DataManager();
            GameManager = new GameManager();
            HUDManager = new HUDManager();

            Level.onPostLevelLoaded += OnLevelLoaded;

            Logger.Log("Unturned Legends has been loaded");
        }


        protected override void Unload()
        {
            GameManager.Destroy();
            HUDManager.Destroy();

            Level.onPostLevelLoaded -= OnLevelLoaded;
            Logger.Log("Unturned Legends has been unloaded");
        }

        private void OnLevelLoaded(int level)
        {
            var shouldFillAfterDetach = typeof(ItemMagazineAsset).GetField("shouldFillAfterDetach", BindingFlags.NonPublic | BindingFlags.Instance);
            var magazines = Assets.find(EAssetType.ITEM).OfType<ItemMagazineAsset>();
            foreach (var mag in magazines)
            {
                shouldFillAfterDetach.SetValue(mag, true);
            }
        }

        public override TranslationList DefaultTranslations => new TranslationList()
        {
            { "Correct_Usage", "[color=red]Correct Usage: {0}[/color]" },
            { "Location_Not_Found", "[color=red]Location with that ID not found[/color]" },
            { "FFA_Spawnpoint_Set", "[color=red]FFA spawnpoint set for arena {0}[/color]" },
            { "No_Game_Going_On", "[color=red]There is no game going on at the moment[/color]" },
            { "FFA_Name", "Free-for-all" },
            { "FFA_Desc", "Eliminate other players." },
            { "Arena_Name", "{0} Arena" }
        };

        public static Harmony Harmony { get; set; }
        public HUDManager HUDManager { get; set; }
        public GameManager GameManager { get; set; }
        public DataManager DataManager { get; set; }
        public DatabaseManager DBManager { get; set; }
        public static Plugin Instance { get; set; }
    }
}
