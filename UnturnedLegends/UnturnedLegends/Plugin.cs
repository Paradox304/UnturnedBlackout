using HarmonyLib;
using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using SDG.Unturned;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnturnedLegends.Managers;
using Logger = Rocket.Core.Logging.Logger;

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
            UIManager = new UIManager();

            StartCoroutine(Day());

            Level.onPostLevelLoaded += OnLevelLoaded;

            Logger.Log("Unturned Legends has been loaded");
        }


        protected override void Unload()
        {
            GameManager.Destroy();
            HUDManager.Destroy();

            Level.onPostLevelLoaded -= OnLevelLoaded;
            StopAllCoroutines();

            Logger.Log("Unturned Legends has been unloaded");
        }

        public IEnumerator Day()
        {
            ConsolePlayer console = new ConsolePlayer();
            while (true)
            {
                yield return new WaitForSeconds(60);
                LightingManager.time = (uint)(LightingManager.cycle * LevelLighting.transition);
            }
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
            { "Game_Not_Found_With_ID", "[color=red]No game found with that ID[/color]" },
            { "Not_Ingame", "[color=red]You aren't in any game[/color]" }
            { "FFA_Name", "Free-for-all" },
            { "FFA_Desc", "Eliminate other players." },
            { "Arena_Name", "{0} Arena" },
            { "Headshot_Kill", "Headshot Kill" },
            { "Normal_Kill", "Normal Kill" },
            { "KillStreak_Show", "Killstreak x{0}" },
            { "Multiple_Kills_Show", "Multiple Kills x{0}" },
            { "Level_Show", "LVL. {0}" },
            { "SAFETY", "SAFETY" },
            { "AUTO", "AUTO" },
            { "BURST", "BURST" },
            { "SEMI", "SEMI" }
        };

        public static Harmony Harmony { get; set; }
        public UIManager UIManager { get; set; }
        public HUDManager HUDManager { get; set; }
        public GameManager GameManager { get; set; }
        public DataManager DataManager { get; set; }
        public DatabaseManager DBManager { get; set; }
        public static Plugin Instance { get; set; }
    }
}
