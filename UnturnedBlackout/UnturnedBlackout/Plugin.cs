using HarmonyLib;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using SDG.Unturned;
using Steamworks;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnturnedBlackout.Managers;
using Logger = Rocket.Core.Logging.Logger;

namespace UnturnedBlackout
{
    public class Plugin : RocketPlugin<Config>
    {
        protected override void Load()
        {
            Instance = this;
            if (Harmony == null)
            {
                Harmony = new Harmony("UnturnedBlackout");
                Harmony.PatchAll(Assembly);
            }

            DBManager = new DatabaseManager();
            DataManager = new DataManager();
            UIManager = new UIManager();

            StartCoroutine(Day());

            Level.onPostLevelLoaded += OnLevelLoaded;
            PlayerVoice.onRelayVoice += OnVoice;

            ObjectManager.onDamageObjectRequested += OnDamageObject;
            ResourceManager.onDamageResourceRequested += OnDamageResource;
            BarricadeManager.onDamageBarricadeRequested += OnDamageBarricade;
            StructureManager.onDamageStructureRequested += OnDamageStructure;

            Logger.Log("Unturned Blackout has been loaded");
        }

        protected override void Unload()
        {
            GameManager.Destroy();
            HUDManager.Destroy();
            UIManager.Destroy();

            Level.onPostLevelLoaded -= OnLevelLoaded;
            PlayerVoice.onRelayVoice -= OnVoice;

            ObjectManager.onDamageObjectRequested -= OnDamageObject;
            ResourceManager.onDamageResourceRequested -= OnDamageResource;
            BarricadeManager.onDamageBarricadeRequested -= OnDamageBarricade;
            StructureManager.onDamageStructureRequested -= OnDamageStructure;

            StopAllCoroutines();

            Logger.Log("Unturned Blackout has been unloaded");
        }

        public IEnumerator Day()
        {
            while (true)
            {
                yield return new WaitForSeconds(60);
                LightingManager.time = (uint)(LightingManager.cycle * LevelLighting.transition);
            }
        }

        private void OnDamageStructure(CSteamID instigatorSteamID, Transform structureTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            shouldAllow = false;
        }

        private void OnDamageBarricade(CSteamID instigatorSteamID, Transform barricadeTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            var drop = BarricadeManager.FindBarricadeByRootTransform(barricadeTransform);
            if (drop != null)
            {
                if (!Configuration.Instance.AllowDamageBarricades.Contains(drop.asset.id))
                {
                    shouldAllow = false;
                }
            }
        }

        private void OnDamageResource(CSteamID instigatorSteamID, Transform objectTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            shouldAllow = false;
        }

        private void OnDamageObject(CSteamID instigatorSteamID, Transform objectTransform, byte section, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            shouldAllow = false;
        }

        private void OnVoice(PlayerVoice speaker, bool wantsToUseWalkieTalkie, ref bool shouldAllow, ref bool shouldBroadcastOverRadio, ref PlayerVoice.RelayVoiceCullingHandler cullingHandler)
        {
            shouldBroadcastOverRadio = true;
            GameManager.GetGamePlayer(speaker.player).OnTalking();
        }

        private void OnLevelLoaded(int level)
        {
            GameManager = new GameManager();
            HUDManager = new HUDManager();

            var shouldFillAfterDetach = typeof(ItemMagazineAsset).GetProperty("shouldFillAfterDetach", BindingFlags.Public | BindingFlags.Instance);
            var magazines = Assets.find(EAssetType.ITEM).OfType<ItemMagazineAsset>();
            foreach (var mag in magazines)
            {
                shouldFillAfterDetach.GetSetMethod(true).Invoke(mag, new object[] { true });
            }

            var isEarpiece = typeof(ItemMaskAsset).GetField("_isEarpiece", BindingFlags.NonPublic | BindingFlags.Instance);
            var masks = Assets.find(EAssetType.ITEM).OfType<ItemMaskAsset>();
            foreach (var mask in masks)
            {
                isEarpiece.SetValue(mask, true);
            }
        }

        public override TranslationList DefaultTranslations => new TranslationList()
        {
            { "Correct_Usage", "[color=red]Correct Usage: {0}[/color]" },
            { "Location_Not_Found", "[color=red]Location with that ID not found[/color]" },
            { "Group_Not_Found", "[color=red]Group with that ID not found[/color]" },
            { "FFA_Spawnpoint_Set", "[color=green]FFA spawnpoint set for arena {0}[/color]" },
            { "TDM_SpawnPoint_Set", "[color=green]TDM spawnpoint set for arena {0} for group {1}[/color]" },
            { "CTF_SpawnPoint_Set", "[color=green]CTF spawnpoint set for arena {0} for group {1}" },
            { "CTF_Flag_SpawnPoint_Set", "[color=green]CTF Flag spawnpoint set for arena {0} for group {1}" },
            { "Game_Not_Found_With_ID", "[color=red]No game found with that ID[/color]" },
            { "Game_Full", "[color=red]Game is full, can't join[/color]" },
            { "Game_Voting", "[color=red]There is voting going on in that game, can't join[/color]" },
            { "Not_Ingame", "[color=red]You aren't in any game[/color]" },
            { "Ingame", "[color=red]You are already in a game[/color]" },
            { "Headshot_Kill", "Headshot Kill" },
            { "Normal_Kill", "Normal Kill" },
            { "Domination_Kill", "Domination" },
            { "Shutdown_Kill", "Shutdown" },
            { "Assist_Kill", "Kill Assist ({0})" },
            { "Melee_Kill", "Melee Kill" },
            { "Kill_Confirmed", "Kill Confirmed" },
            { "Kill_Denied", "Kill Denied" },
            { "Multiple_Kills_Show", "Multiple Kills x{0}" },
            { "Multiple_Kills_Show_2", "2 Multiple Kills" },
            { "Multiple_Kills_Show_3", "3 Multiple Kills" },
            { "Multiple_Kills_Show_4", "4 Multiple Kills" },
            { "Multiple_Kills_Show_5", "5 Multiple Kills" },
            { "Level_Show", "LVL. {0}" },
            { "Vote0_MapName", "{0} [color=#ffa142]{1}[/color]" },
            { "Vote1_MapName", "[color=#3672ff]{1}[/color] {0}" },
            { "Victory_Text", "VICTORY" },
            { "Defeat_Text", "DEFEAT" },
            { "Winning_Text", "WINNING" },
            { "Losing_Text", "LOSING" },
            { "FFA_Name", "FFA" },
            { "FFA_Name_Full", "Free-For-All" },
            { "FFA_Desc", "Eliminate other players." },
            { "FFA_Victory_Desc", "Score limit reached" },
            { "FFA_Defeat_Desc", "You could'nt reach the score limit" },
            { "TDM_Name", "TDM" },
            { "TDM_Name_Full", "Team-Deathmatch" },
            { "TDM_Desc", "Eliminate the enemy at all costs." },
            { "TDM_Victory_Desc", "Score limit reached" },
            { "TDM_Defeat_Desc", "You could'nt reach the score limit" },
            { "KC_Name", "Kill Confirmed" },
            { "KC_Name_Full", "Kill-Confirmed" },
            { "KC_Desc", "Eliminate hostiles and recover their dog tags." },
            { "KC_Victory_Desc", "Score limit reached" },
            { "KC_Defeat_Desc", "You could'nt reach the score limit" },
            { "SAFETY", "SAFETY" },
            { "AUTO", "AUTO" },
            { "BURST", "BURST" },
            { "SEMI", "SEMI" },
            { "Level_Up_Desc", "New Level: {0}" },
            { "Level_Up_Text", "LEVELLED UP!"}
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
