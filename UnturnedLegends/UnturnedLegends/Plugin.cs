using HarmonyLib;
using Rocket.API.Collections;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using SDG.Unturned;
using System.Linq;
using System.Reflection;
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
            Utility.Debug("LEVEL LOADED, INITIALIZING GAME MANAGER AND HUD MANAGER");
            GameManager = new GameManager();
            HUDManager = new HUDManager();
            Utility.Debug("CHANGING ALL MAGAZINES TO REFILL WHEN THE PLAYER DIES");
            var shouldFillAfterDetach = typeof(ItemMagazineAsset).GetProperty("shouldFillAfterDetach", BindingFlags.Public | BindingFlags.Instance);
            var magazines = Assets.find(EAssetType.ITEM).OfType<ItemMagazineAsset>();
            foreach (var mag in magazines)
            {
                shouldFillAfterDetach.GetSetMethod(true).Invoke(mag, new object[] { true });
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
            { "Arena_Name", "{0} Arena" },
            { "Headshot_Kill", "Headshot Kill" },
            { "Normal_Kill", "Normal Kill" },
            { "KillStreak_Show", "Killstreak x{0}" },
            { "Multiple_Kills_Show", "Multiple Kills x{0}" }
        };

        public static Harmony Harmony { get; set; }
        public HUDManager HUDManager { get; set; }
        public GameManager GameManager { get; set; }
        public DataManager DataManager { get; set; }
        public DatabaseManager DBManager { get; set; }
        public static Plugin Instance { get; set; }
    }
}
