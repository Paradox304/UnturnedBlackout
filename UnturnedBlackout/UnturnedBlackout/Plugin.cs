using System;
using HarmonyLib;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned.Permissions;
using SDG.Unturned;
using Steamworks;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnturnedBlackout.Helpers;
using UnturnedBlackout.Managers;
using Logger = Rocket.Core.Logging.Logger;

// ReSharper disable RedundantAssignment

namespace UnturnedBlackout;

public class Plugin : RocketPlugin<Config>
{
    protected override void Load()
    {
        Instance = this;
        if (Harmony == null)
        {
            Harmony = new("UnturnedBlackout");
            Harmony.PatchAll(Assembly);
        }

        Level.onLevelLoaded += OnLevelLoaded;
        PlayerVoice.onRelayVoice += OnVoice;

        ObjectManager.onDamageObjectRequested += OnDamageObject;
        ResourceManager.onDamageResourceRequested += OnDamageResource;
        StructureManager.onDamageStructureRequested += OnDamageStructure;
        UseableConsumeable.onPerformingAid += OnPerformingAid;
        BarricadeDrop.OnSalvageRequested_Global += OnSalvageBarricade;
        BarricadeManager.onOpenStorageRequested += OnStorageOpen;

        UnturnedPermissions.OnJoinRequested += OnJoining;

        PlayerInput.onPluginKeyTick += OnHotkeyPressed;

        Rocket.Core.Logging.Logger.Log("Unturned Blackout has been loaded");
    }

    protected override void Unload()
    {
        Game.Destroy();
        UI.Destroy();
        DB.Destroy();

        Level.onLevelLoaded -= OnLevelLoaded;
        PlayerVoice.onRelayVoice -= OnVoice;

        ObjectManager.onDamageObjectRequested -= OnDamageObject;
        ResourceManager.onDamageResourceRequested -= OnDamageResource;
        StructureManager.onDamageStructureRequested -= OnDamageStructure;
        UseableConsumeable.onPerformingAid -= OnPerformingAid;
        BarricadeDrop.OnSalvageRequested_Global -= OnSalvageBarricade;
        BarricadeManager.onOpenStorageRequested -= OnStorageOpen;
        UnturnedPermissions.OnJoinRequested -= OnJoining;

        PlayerInput.onPluginKeyTick -= OnHotkeyPressed;
        StopAllCoroutines();

        Rocket.Core.Logging.Logger.Log("Unturned Blackout has been unloaded");
    }

    private void OnStorageOpen(CSteamID instigator, InteractableStorage storage, ref bool shouldAllow) => shouldAllow = false;

    private void OnSalvageBarricade(BarricadeDrop barricade, SteamPlayer instigatorClient, ref bool shouldAllow) => shouldAllow = false;

    private void OnPerformingAid(Player instigator, Player target, ItemConsumeableAsset asset, ref bool shouldAllow) => shouldAllow = false;

    private void OnJoining(CSteamID player, ref ESteamRejection? rejectionReason)
    {
        var ply = Provider.pending.FirstOrDefault(k => k.playerID.steamID == player);
        if (ply == null)
            return;

        var newName = ply.playerID.characterName.ToUnrich().Trim();
        var chars = newName.Count();

        if (chars == 0)
        {
            rejectionReason = ESteamRejection.NAME_CHARACTER_INVALID;
            return;
        }

        if (chars > Config.Base.FileData.MaxPlayerNameCharacters)
        {
            newName = newName.Remove(Config.Base.FileData.MaxPlayerNameCharacters, chars - Config.Base.FileData.MaxPlayerNameCharacters);
            newName += "...";
        }

        ply.playerID.characterName = newName;
        ply.playerID.nickName = newName;

        ply.hatItem = 0;
        ply.maskItem = 0;
        ply.glassesItem = 0;
        ply.backpackItem = 0;
        ply.shirtItem = 0;
        ply.pantsItem = 0;
        ply.vestItem = 0;

        ply.skinItems = Array.Empty<int>();

        //var randColor = _allowedColors[UnityEngine.Random.Range(0, _allowedColors.Length)];

        //var field = typeof(SteamPending).GetField("_color", BindingFlags.Instance | BindingFlags.NonPublic);
        //if (field == null)
        //return;
        //Logging.Debug($"Field found for {ply.playerID.characterName}, setting color: {randColor[0]}, {randColor[1]}, {randColor[2]}");
        //field.SetValue(ply, new Color(randColor[0], randColor[1], randColor[2]));
    }

    private void OnHotkeyPressed(Player player, uint simulation, byte key, bool state)
    {
        if (state == false)
            return;

        var gPlayer = Game.GetGamePlayer(player);

        var game = gPlayer?.CurrentGame;
        if (game == null)
            return;

        switch (key)
        {
            case 0:
                game.OnChangeFiremode(gPlayer);
                break;
            case 1:
                if (game.GamePhase != Enums.EGamePhase.ENDING && !gPlayer.HasMidgameLoadout)
                {
                    gPlayer.HasMidgameLoadout = true;
                    UI.ShowMidgameLoadoutUI(gPlayer);
                }

                break;
            default:
                return;
        }
    }

    private IEnumerator Day()
    {
        while (true)
        {
            yield return new WaitForSeconds(60);

            //LightingManager.time = (uint)(LightingManager.cycle * LevelLighting.transition);
            Logging.Debug($"TPS: {Provider.debugTPS}", ConsoleColor.Yellow);
            Logging.Debug($"UPS: {Provider.debugUPS}", ConsoleColor.Yellow);
        }
        // ReSharper disable once IteratorNeverReturns
    }

    private void OnDamageStructure(CSteamID instigatorSteamID, Transform structureTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin) => shouldAllow = false;

    private void OnDamageResource(CSteamID instigatorSteamID, Transform objectTransform, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin) => shouldAllow = false;

    private void OnDamageObject(CSteamID instigatorSteamID, Transform objectTransform, byte section, ref ushort pendingTotalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin) => shouldAllow = false;

    private void OnVoice(PlayerVoice speaker, bool wantsToUseWalkieTalkie, ref bool shouldAllow, ref bool shouldBroadcastOverRadio, ref PlayerVoice.RelayVoiceCullingHandler cullingHandler)
    {
        if (!DB.PlayerData.TryGetValue(speaker.channel.owner.playerID.steamID, out var data) || data.IsMuted)
        {
            shouldAllow = false;
            return;
        }

        shouldBroadcastOverRadio = true;
        Game.GetGamePlayer(speaker.player)?.OnTalking();
    }

    private void OnLevelLoaded(int level)
    {
        Logger = new();
        Config = new();
        Logging.Debug("Initialized Config");
        UI = new();
        Logging.Debug("Initialized UI");
        Reward = new();
        Logging.Debug("Initialized Reward");
        Discord = new();
        Logging.Debug("Initialized Discord");
        DB = new();
        Logging.Debug("Initialized Database");
        BP = new();
        Logging.Debug("Initialized BP");
        Loadout = new();
        Logging.Debug("Initialized Loadout");
        Data = new();
        Logging.Debug("Initialized Data");
        Quest = new();
        Logging.Debug("Initialized Quest");
        Achievement = new();
        Logging.Debug("Initialized Achievement");
        Unbox = new();
        Logging.Debug("Initialized Unbox");
        Game = new();
        Logging.Debug("Initialized Game");

        _ = StartCoroutine(Day());

        var ignoreMags = Config.Killstreaks.FileData.KillstreaksData.Where(k => k.MagID != 0).Select(k => k.MagID).ToList();
        var shouldFillAfterDetach = typeof(ItemMagazineAsset).GetProperty("shouldFillAfterDetach", BindingFlags.Public | BindingFlags.Instance);
        var magazines = Assets.find(EAssetType.ITEM).OfType<ItemMagazineAsset>();
        foreach (var mag in magazines)
        {
            if (ignoreMags.Contains(mag.id))
                continue;

            _ = shouldFillAfterDetach?.GetSetMethod(true).Invoke(mag, new object[] { true });
        }

        var isEarpiece = typeof(ItemMaskAsset).GetField("_isEarpiece", BindingFlags.NonPublic | BindingFlags.Instance);
        var masks = Assets.find(EAssetType.ITEM).OfType<ItemMaskAsset>();
        foreach (var mask in masks)
            isEarpiece?.SetValue(mask, true);
    }

    public override TranslationList DefaultTranslations => new()
    {
        { "Correct_Usage", "[color=red]Correct Usage: {0}[/color]" },
        { "Location_Not_Found", "[color=red]Location with that ID not found[/color]" },
        { "Group_Not_Found", "[color=red]Group with that ID not found[/color]" },
        { "FFA_Spawnpoint_Set", "[color=green]FFA spawnpoint set for arena {0}[/color]" },
        { "TDM_SpawnPoint_Set", "[color=green]TDM spawnpoint set for arena {0} for group {1}[/color]" },
        { "CTF_SpawnPoint_Set", "[color=green]CTF spawnpoint set for arena {0} for group {1}[/color]" },
        { "CTF_Flag_SpawnPoint_Set", "[color=green]CTF Flag spawnpoint set for arena {0} for group {1}[/color]" },
        { "Muted", "[color=red]You have been muted for {0} seconds for {1}[/color]" },
        { "Unmuted", "[color=green]You have been unmuted[/color]" },
        { "Game_Not_Found_With_ID", "[color=red]No game found with that ID[/color]" },
        { "Game_Full", "[color=red]Game is full, can't join[/color]" },
        { "Game_Voting", "[color=red]There is voting going on in that game, can't join[/color]" },
        { "Not_Ingame", "[color=red]You aren't in any game[/color]" },
        { "Ingame", "[color=red]You are already in a game[/color]" },
        { "Headshot_Kill", "Headshot Kill" },
        { "Normal_Kill", "Normal Kill" },
        { "Domination_Kill", "Domination" },
        { "Shutdown_Kill", "Shutdown" },
        { "Revenge_Kill", "Revenge" },
        { "First_Kill", "First Kill" },
        { "Longshot_Kill", "Longshot" },
        { "Survivor_Kill", "Survivor" },
        { "Assist_Kill", "Kill Assist ({0})" },
        { "Melee_Kill", "Melee Kill" },
        { "Lethal_Kill", "Lethal Kill" },
        { "Lethal_Hit", "Lethal Hit" },
        { "Turret_Destroy", "Turret Destroyed" },
        { "Killstreak_Kill", "Killstreak Kill" },
        { "Kill_Confirmed", "Kill Confirmed" },
        { "Kill_Denied", "Kill Denied" },
        { "Collector", "Collector" },
        { "Flag_Captured", "Flag Captured" },
        { "Flag_Saved", "Flag Saved" },
        { "Flag_Killer", "Flag Killer" },
        { "Flag_Denied", "Flag Denied" },
        { "Multiple_Kills_Show", "Multiple Kills x{0}" },
        { "Multiple_Kills_Show_2", "2 Multiple Kills" },
        { "Multiple_Kills_Show_3", "3 Multiple Kills" },
        { "Multiple_Kills_Show_4", "4 Multiple Kills" },
        { "Multiple_Kills_Show_5", "5 Multiple Kills" },
        { "Waiting_For_Players_Show", "Waiting for players ({0}/{1})" },
        { "Level_Show", "LVL. {0}" },
        { "Victory_Text", "VICTORY" },
        { "Defeat_Text", "DEFEAT" },
        { "Winning_Text", "WINNING" },
        { "Losing_Text", "LOSING" },
        { "FFA_Name", "FFA" },
        { "FFA_Name_Full", "Free For All" },
        { "FFA_Desc", "Eliminate other players." },
        { "FFA_Description_Full", "DESCRIPTION FOR FFA HERE, SET IN TRANSLATION!" },
        { "FFA_Victory_Desc", "Score limit reached" },
        { "FFA_Defeat_Desc", "You could'nt reach the score limit" },
        { "TDM_Name", "TDM" },
        { "TDM_Name_Full", "Team Deathmatch" },
        { "TDM_Desc", "Eliminate the enemy at all costs." },
        { "TDM_Description_Full", "DESCRIPTION FOR TDM HERE, SET IN TRANSLATION!" },
        { "TDM_Victory_Desc", "Score limit reached" },
        { "TDM_Defeat_Desc", "You could'nt reach the score limit" },
        { "KC_Name", "Kill Confirmed" },
        { "KC_Name_Full", "Kill Confirmed" },
        { "KC_Desc", "Eliminate hostiles and recover their dog tags." },
        { "KC_Description_Full", "DESCRIPTION FOR KC HERE, SET IN TRANSLATION!" },
        { "KC_Victory_Desc", "Score limit reached" },
        { "KC_Defeat_Desc", "You couldn't reach the score limit" },
        { "CTF_Name", "CTF" },
        { "CTF_Name_Full", "Capture The Flag" },
        { "CTF_Desc", "Capture the enemy flag." },
        { "CTF_Description_Full", "DESCRIPTION FOR CTF HERE, SET IN TRANSLATION" },
        { "CTF_Victory_Desc", "Score limit reached" },
        { "CTF_Defeat_Desc", "You couldn't reach the score limit" },
        { "CTF_Team_Captured_Flag", "Your team has captured the flag" },
        { "CTF_Enemy_Captured_Flag", "The enemy has captured your flag" },
        { "CTF_Team_Recovered_Flag", "Your team has recovered the flag" },
        { "CTF_Enemy_Recovered_Flag", "The enemy has recovered the flag" },
        { "CTF_Team_Taken_Flag", "Your team has taken the flag" },
        { "CTF_Enemy_Taken_Flag", "The enemy has taken the flag" },
        { "CTF_Team_Dropped_Flag", "Your team has dropped the flag" },
        { "CTF_Enemy_Dropped_Flag", "The enemy has the dropped the flag" },
        { "CTF_Team_Picked_Flag", "Your team has picked up the flag" },
        { "CTF_Enemy_Picked_Flag", "The enemy has picked up the flag" },
        { "Unlock_Gun_Level", "UNLOCK WITH GUN LEVEL {0}" },
        { "Unlock_Level", "UNLOCK WITH LEVEL {0}" },
        { "Level_Up_Desc", "New Level: {0}" },
        { "Level_Up_Text", "LEVELLED UP!" },
        { "Gun_Level_Up_Desc", "{0} levelled up to level {1}" },
        { "Gun_Level_Up_Text", "WEAPON LEVEL UP!" },
        { "Not_Enough_Currency", "You dont have enough {0} to make this purchase. Would you like to check out our Store?" },
        { "Version", "VERSION: 1.0.1" }
    };

    public ConfigManager Config { get; set; }
    public QuestManager Quest { get; set; }
    public AchievementManager Achievement { get; set; }
    public UIManager UI { get; set; }
    public GameManager Game { get; set; }
    public DataManager Data { get; set; }
    public DatabaseManager DB { get; set; }
    public LoadoutManager Loadout { get; set; }
    public RewardManager Reward { get; set; }
    public UnboxManager Unbox { get; set; }
    public LoggingManager Logger { get; set; }
    public DiscordManager Discord { get; set; }
    // ReSharper disable once InconsistentNaming
    public BPManager BP { get; set; }
    private static Harmony Harmony { get; set; }
    public static Plugin Instance { get; private set; }

    private static int[][] _allowedColors = new int[][]
    {
        new int[] { 244, 230, 210 }, new int[] { 217, 202, 180 }, new int[] { 190, 165, 130 }, new int[] { 157, 136, 107 }, new int[] { 148, 118, 75 }, new int[] { 112, 96, 73 }, new int[] { 83, 71, 54 }, new int[] { 75, 61, 49 }, new int[] { 51, 44, 37 }, new int[] { 35, 31, 28 }
    };
}