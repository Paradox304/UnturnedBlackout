﻿using Rocket.Core.Utils;
using Rocket.Unturned;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SDG.NetTransport.Loopback;
using UnityEngine;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Extensions;
using UnturnedBlackout.GameTypes;
using UnturnedBlackout.Instances;
using UnturnedBlackout.Models.Animation;
using UnturnedBlackout.Models.CTF;
using UnturnedBlackout.Models.Feed;
using UnturnedBlackout.Models.FFA;
using UnturnedBlackout.Models.Global;
using UnturnedBlackout.Models.KC;
using UnturnedBlackout.Models.TDM;
using PlayerQuest = UnturnedBlackout.Database.Data.PlayerQuest;

namespace UnturnedBlackout.Managers;

public class UIManager
{
    public ConfigManager Config => Plugin.Instance.Config;

    public Dictionary<ushort, FeedIcon> KillFeedIcons { get; set; }
    public List<UIHandler> UIHandlers { get; set; }
    public Dictionary<CSteamID, UIHandler> UIHandlersLookup { get; set; }

    public Dictionary<CSteamID, Coroutine> TipSender { get; set; }

    public const ushort GAMEMODE_POPUP_ID = 27610;
    public const short GAMEMODE_POPUP_KEY = 27610;

    public const ushort FFA_ID = 27620;
    public const short FFA_KEY = 27620;

    public const ushort TDM_ID = 27621;
    public const short TDM_KEY = 27621;

    public const ushort KC_ID = 27621;
    public const short KC_KEY = 27621;

    public const ushort VOICE_CHAT_ID = 27622;
    public const short VOICE_CHAT_KEY = 27622;

    public const ushort CTF_ID = 27623;
    public const short CTF_KEY = 27623;

    public const ushort HUD_ID = 27631;
    public const short HUD_KEY = 27631;

    public const ushort SOUNDS_ID = 27634;
    public const short SOUNDS_KEY = 27634;

    public const ushort DEATH_ID = 27635;
    public const short DEATH_KEY = 27635;

    public const ushort PRE_ENDING_UI_ID = 27636;
    public const short PRE_ENDING_UI_KEY = 27636;

    public const ushort LEVEL_UP_ID = 27638;
    public const short LEVEL_UP_KEY = 27638;

    public const ushort QUEST_PROGRESSION_ID = 27639;
    public const short QUEST_PROGRESSION_KEY = 27639;

    public const ushort LOADING_UI_ID = 27640;
    public const short LOADING_UI_KEY = 27640;

    public const ushort WAITING_FOR_PLAYERS_ID = 27641;
    public const short WAITING_FOR_PLAYERS_KEY = 27641;

    public const ushort GUN_LEVEL_UP_ID = 27642;
    public const short GUN_LEVEL_UP_KEY = 27642;

    public const ushort KILLCARD_ID = 27644;
    public const short KILLCARD_KEY = 27644;

    public const ushort BP_TIER_COMPLETION_ID = 27645;
    public const short BP_TIER_COMPLETION_KEY = 27645;

    public const ushort ACHIEVEMENT_COMPLETION_ID = 27646;
    public const short ACHIEVEMENT_COMPLETION_KEY = 27646;

    public const ushort ITEM_UNLOCK_ID = 27647;
    public const short ITEM_UNLOCK_KEY = 27647;

    public const ushort QUEST_COMPLETION_ID = 27648;
    public const short QUEST_COMPLETION_KEY = 27648;

    public const ushort KILLSTREAK_ID = 27649;
    public const short KILLSTREAK_KEY = 27649;

    public const ushort KILLSTREAK_AVAILABLE_ID = 27650;
    public const short KILLSTREAK_AVAILABLE_KEY = 27650;

    public const ushort DEATHSTREAK_ACTIVE_ID = 27651;
    public const short DEATHSTREAK_ACTIVE_KEY = 27651;
    
    public const ushort FLAG_POPUP_UI = 27900;
    public const short FLAG_POPUP_KEY = 27900;

    public const int MAX_SPACES_TDM_SCORE = 98;
    public const int MAX_SPACES_KILLSTREAK = 18;

    public const string PRIME_SYMBOL = " ";

    public const string HAIRSPACE_SYMBOL_STRING = "⬞";
    public const char HAIRSPACE_SYMBOL_CHAR = ' ';

    public const string VERY_SMALL_SQUARE = "⬞";

    public UIManager()
    {
        KillFeedIcons = Config.Killfeed.FileData.KillFeedIcons.ToDictionary(k => k.WeaponID);

        UIHandlers = new();
        UIHandlersLookup = new();
        TipSender = new();

        UseableGun.onChangeMagazineRequested += OnMagazineChanged;
        UseableGun.onBulletSpawned += OnBulletShot;
        UseableGun.onProjectileSpawned += OnProjectileShot;

        PlayerEquipment.OnUseableChanged_Global += OnUseableChanged;

        U.Events.OnPlayerConnected += OnConnected;
        U.Events.OnPlayerDisconnected += OnDisconnected;

        UnturnedPlayerEvents.OnPlayerUpdateStamina += OnStaminaUpdated;

        EffectManager.onEffectButtonClicked += OnButtonClicked;
        EffectManager.onEffectTextCommitted += OnTextCommitted;
    }

    public void Destroy()
    {
        EffectManager.onEffectButtonClicked -= OnButtonClicked;
        EffectManager.onEffectTextCommitted -= OnTextCommitted;

        UseableGun.onChangeMagazineRequested -= OnMagazineChanged;
        UseableGun.onBulletSpawned -= OnBulletShot;
        UseableGun.onProjectileSpawned -= OnProjectileShot;

        PlayerEquipment.OnUseableChanged_Global -= OnUseableChanged;

        U.Events.OnPlayerConnected -= OnConnected;
        U.Events.OnPlayerDisconnected -= OnDisconnected;

        UnturnedPlayerEvents.OnPlayerUpdateStamina -= OnStaminaUpdated;
    }

    public void RegisterUIHandler(UnturnedPlayer player)
    {
        _ = UIHandlers.RemoveAll(k => k.SteamID == player.CSteamID);
        _ = UIHandlersLookup.Remove(player.CSteamID);

        UIHandler handler = new(player);
        UIHandlersLookup.Add(player.CSteamID, handler);
        UIHandlers.Add(handler);
    }

    public void UnregisterUIHandler(UnturnedPlayer player)
    {
        if (UIHandlersLookup.TryGetValue(player.CSteamID, out var handler))
        {
            _ = UIHandlers.Remove(handler);
            _ = UIHandlersLookup.Remove(player.CSteamID);
            handler.Dispose();
        }

        if (TipSender.TryGetValue(player.CSteamID, out var tipSender))
            tipSender.Stop();
    }

    public void ShowMenuUI(UnturnedPlayer player, MatchEndSummary summary = null)
    {
        if (UIHandlersLookup.TryGetValue(player.CSteamID, out var handler))
            handler.ShowUI(summary);
    }

    public void HideMenuUI(UnturnedPlayer player)
    {
        if (UIHandlersLookup.TryGetValue(player.CSteamID, out var handler))
            handler.HideUI();
    }

    // ALL GAMES RELATED UI

    // WAITING FOR PLAYERS

    public void SendWaitingForPlayersUI(GamePlayer player, int playerCount, int waitingPlayers) =>
        EffectManager.sendUIEffect(WAITING_FOR_PLAYERS_ID, WAITING_FOR_PLAYERS_KEY, player.TransportConnection, true, Plugin.Instance.Translate("Waiting_For_Players_Show", playerCount, waitingPlayers).ToRich());

    public void UpdateWaitingForPlayersUI(GamePlayer player, int playerCount, int waitingPlayers) => EffectManager.sendUIEffectText(WAITING_FOR_PLAYERS_KEY, player.TransportConnection, true, "Waiting", Plugin.Instance.Translate("Waiting_For_Players_Show", playerCount, waitingPlayers).ToRich());

    public void ClearWaitingForPlayersUI(GamePlayer player) => EffectManager.askEffectClearByID(WAITING_FOR_PLAYERS_ID, player.TransportConnection);

    // GAME START COUNTDOWN

    public void ShowCountdownUI(GamePlayer player)
    {
        EffectManager.sendUIEffect(27633, 27633, player.TransportConnection, true);
        EffectManager.sendUIEffectVisibility(27633, player.TransportConnection, true, "StartCountdown", true);
    }

    public void SendCountdownSeconds(GamePlayer player, int seconds) => EffectManager.sendUIEffectText(27633, player.TransportConnection, true, "CountdownNum", seconds.ToString());

    public void ClearCountdownUI(GamePlayer player) => EffectManager.askEffectClearByID(27633, player.TransportConnection);

    // XP UI

    public void ShowXPUI(GamePlayer player, int xp, string xpGained)
    {
        var showXP = xp;
        if ((DateTime.UtcNow - player.LastXPPopup).TotalSeconds <= Config.Base.FileData.XPPopupStaySeconds)
            showXP += player.LastXP;

        player.LastXP = showXP;
        player.LastXPPopup = DateTime.UtcNow;
        EffectManager.sendUIEffect(27630, 27630, player.TransportConnection, true, $"+{showXP} XP", xpGained);
    }

    // MULTIKILL SOUND

    public void SendMultiKillSound(GamePlayer player, int multiKill)
    {
        switch (multiKill)
        {
            case 0:
                EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "Kill", false);
                EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "Kill", true);
                return;
            case 1:
                EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "Kill", false);
                EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "Kill", true);
                return;
            case 2:
                EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "MultiKill1", false);
                EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "MultiKill1", true);
                return;
            case 3:
                EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "MultiKill2", false);
                EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "MultiKill2", true);
                return;
            case 4:
                EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "MultiKill3", false);
                EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "MultiKill3", true);
                return;
            case 5:
                EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "MultiKill4", false);
                EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "MultiKill4", true);
                return;
            case 6:
                EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "MultiKill5", false);
                EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "MultiKill5", true);
                return;
            default:
                EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "MultiKill5", false);
                EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "MultiKill5", true);
                return;
        }

        ;
    }

    // ANIMATION

    public void SendAnimation(GamePlayer player, AnimationInfo animationInfo)
    {
        var game = player.CurrentGame;
        if (game != null && game.GamePhase != EGamePhase.STARTED)
            return;

        if (player.HasAnimationGoingOn)
        {
            player.PendingAnimations.Add(animationInfo);
            return;
        }

        player.AnimationChecker.Stop();
        player.AnimationChecker = Plugin.Instance.StartCoroutine(player.CheckAnimation());

        switch (animationInfo.AnimationType)
        {
            case EAnimationType.LEVEL_UP:
            {
                if (!Plugin.Instance.DB.Levels.TryGetValue((int)animationInfo.Info, out var level))
                    return;

                EffectManager.sendUIEffect(LEVEL_UP_ID, LEVEL_UP_KEY, player.TransportConnection, true);
                EffectManager.sendUIEffectImageURL(LEVEL_UP_KEY, player.TransportConnection, true, "LevelUpIcon", level.IconLinkLarge);
                EffectManager.sendUIEffectText(LEVEL_UP_KEY, player.TransportConnection, true, "LevelUpDesc", " ");
                EffectManager.sendUIEffectText(LEVEL_UP_KEY, player.TransportConnection, true, "LevelUpTxt", $"LEVEL {level.Level}");
                break;
            }
            case EAnimationType.GUN_LEVEL_UP:
            {
                var gun = animationInfo.Info as AnimationItemUnlock;
                EffectManager.sendUIEffect(GUN_LEVEL_UP_ID, GUN_LEVEL_UP_KEY, player.TransportConnection, true);
                EffectManager.sendUIEffectImageURL(GUN_LEVEL_UP_KEY, player.TransportConnection, true, "LevelUpIcon", gun.ItemIcon);
                EffectManager.sendUIEffectText(GUN_LEVEL_UP_KEY, player.TransportConnection, true, "LevelUpDesc", $"Level {gun.ItemType}");
                EffectManager.sendUIEffectText(GUN_LEVEL_UP_KEY, player.TransportConnection, true, "LevelUpTxt", gun.ItemName);
                break;
            }
            case EAnimationType.QUEST_COMPLETION:
            {
                var quest = animationInfo.Info as Quest;
                EffectManager.sendUIEffect(QUEST_COMPLETION_ID, QUEST_COMPLETION_KEY, player.TransportConnection, true);
                EffectManager.sendUIEffectText(QUEST_COMPLETION_KEY, player.TransportConnection, true, "LevelUpDesc", quest.QuestTitle);
                break;
            }
            case EAnimationType.BATTLEPASS_TIER_COMPLETION:
            {
                var tier = animationInfo.Info as BattlepassTier;
                EffectManager.sendUIEffect(BP_TIER_COMPLETION_ID, BP_TIER_COMPLETION_KEY, player.TransportConnection, true);
                EffectManager.sendUIEffectText(BP_TIER_COMPLETION_KEY, player.TransportConnection, true, "LevelUpDesc", tier.TierID.ToString());
                break;
            }
            case EAnimationType.ITEM_UNLOCK:
            {
                var itemUnlock = animationInfo.Info as AnimationItemUnlock;
                EffectManager.sendUIEffect(ITEM_UNLOCK_ID, ITEM_UNLOCK_KEY, player.TransportConnection, true);
                EffectManager.sendUIEffectImageURL(ITEM_UNLOCK_KEY, player.TransportConnection, true, "LevelUpIcon", itemUnlock.ItemIcon);
                EffectManager.sendUIEffectText(ITEM_UNLOCK_KEY, player.TransportConnection, true, "LevelUpTxt", "UNLOCKED");
                EffectManager.sendUIEffectText(ITEM_UNLOCK_KEY, player.TransportConnection, true, "LevelUpDesc", itemUnlock.ItemName);
                break;
            }
            case EAnimationType.ACHIEVEMENT_COMPLETION:
            {
                var achievement = animationInfo.Info as AchievementTier;
                EffectManager.sendUIEffect(ACHIEVEMENT_COMPLETION_ID, ACHIEVEMENT_COMPLETION_KEY, player.TransportConnection, true);
                EffectManager.sendUIEffectImageURL(ACHIEVEMENT_COMPLETION_KEY, player.TransportConnection, true, "LevelUpIcon", achievement.TierPrevLarge);
                EffectManager.sendUIEffectText(ACHIEVEMENT_COMPLETION_KEY, player.TransportConnection, true, "LevelUpTxt", achievement.TierTitle);
                EffectManager.sendUIEffectText(ACHIEVEMENT_COMPLETION_KEY, player.TransportConnection, true, "LevelUpDesc", achievement.TierDesc);
                break;
            }
            case EAnimationType.KILLSTREAK_AVAILABLE:
            {
                var killstreak = animationInfo.Info as Killstreak;
                EffectManager.sendUIEffect(KILLSTREAK_AVAILABLE_ID, KILLSTREAK_AVAILABLE_KEY, player.TransportConnection, true);
                EffectManager.sendUIEffectImageURL(KILLSTREAK_AVAILABLE_KEY, player.TransportConnection, true, "LevelUpIcon", killstreak.IconLink);
                EffectManager.sendUIEffectText(KILLSTREAK_AVAILABLE_KEY, player.TransportConnection, true, "LevelUpTxt", killstreak.KillstreakName.ToUpper());
                break;
            }
            default:
                break;
        }
    }

    public void ClearAnimations(GamePlayer player)
    {
        EffectManager.askEffectClearByID(LEVEL_UP_ID, player.TransportConnection);
        EffectManager.askEffectClearByID(GUN_LEVEL_UP_ID, player.TransportConnection);
        EffectManager.askEffectClearByID(QUEST_COMPLETION_ID, player.TransportConnection);
        EffectManager.askEffectClearByID(BP_TIER_COMPLETION_ID, player.TransportConnection);
        EffectManager.askEffectClearByID(ITEM_UNLOCK_ID, player.TransportConnection);
        EffectManager.askEffectClearByID(ACHIEVEMENT_COMPLETION_ID, player.TransportConnection);
        EffectManager.askEffectClearByID(KILLSTREAK_AVAILABLE_ID, player.TransportConnection);
    }

    // KILLFEED

    public void SendKillfeed(List<GamePlayer> players, EGameType type, List<Feed> killfeed)
    {
        short key;
        switch (type)
        {
            case EGameType.FFA:
                key = FFA_KEY;
                break;
            case EGameType.TDM:
                key = TDM_KEY;
                break;
            case EGameType.KC:
                key = TDM_KEY;
                break;
            case EGameType.CTF:
                key = CTF_KEY;
                break;
            default:
                return;
        }

        var feedText = "";
        foreach (var feed in killfeed)
            feedText += feed.KillMessage + "\n";

        if (!string.IsNullOrEmpty(feedText))
            feedText = $"<size={Config.Base.FileData.KillFeedFont}>{feedText}</size>";

        foreach (var player in players)
        {
            var playerName = player.Player.CharacterName;
            var updatedText = new Regex(Regex.Escape(playerName), RegexOptions.IgnoreCase).Replace(feedText, $"<color={Config.Base.FileData.PlayerColorHexCode}>{playerName}</color>");
            EffectManager.sendUIEffectText(key, player.TransportConnection, true, "Killfeed", updatedText);
        }
    }

    // VOICE CHAT

    public void SendVoiceChatUI(GamePlayer player) => EffectManager.sendUIEffect(VOICE_CHAT_ID, VOICE_CHAT_KEY, player.TransportConnection, true);

    public void UpdateVoiceChatUI(List<GamePlayer> players, List<GamePlayer> playersTalking)
    {
        var voiceChatText = "";
        foreach (var talking in playersTalking)
            voiceChatText += $" {talking.Player.CharacterName.ToUnrich()} \n";

        if (!string.IsNullOrEmpty(voiceChatText))
            voiceChatText = $"<size={Config.Base.FileData.VoiceChatFont}>{voiceChatText}</size>";

        foreach (var player in players)
            EffectManager.sendUIEffectText(VOICE_CHAT_KEY, player.TransportConnection, true, "VoiceChatUsers", voiceChatText);
    }

    public void ClearVoiceChatUI(GamePlayer player) => EffectManager.askEffectClearByID(VOICE_CHAT_ID, player.TransportConnection);

    // DEATH UI

    public void SendDeathUI(GamePlayer victim, GamePlayer killer, PlayerData killerData)
    {
        victim.Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);

        EffectManager.sendUIEffect(DEATH_ID, DEATH_KEY, victim.TransportConnection, true);
        EffectManager.sendUIEffectImageURL(DEATH_KEY, victim.TransportConnection, true, "EnemyIcon", killerData.AvatarLinks[2]);
        EffectManager.sendUIEffectImageURL(DEATH_KEY, victim.TransportConnection, true, "EnemyXPIcon", Plugin.Instance.DB.Levels.TryGetValue(killerData.Level, out var level) ? level.IconLinkMedium : "");
        EffectManager.sendUIEffectText(DEATH_KEY, victim.TransportConnection, true, "EnemyName", (killerData.HasPrime ? PRIME_SYMBOL : "") + killerData.SteamName.ToUpper());
        EffectManager.sendUIEffectText(DEATH_KEY, victim.TransportConnection, true, "EnemyXPNum", killerData.Level.ToString());
        EffectManager.sendUIEffectImageURL(DEATH_KEY, victim.TransportConnection, true, "DeathBanner", killer.ActiveLoadout?.Card?.Card?.CardLink ?? "https://cdn.discordapp.com/attachments/899796442649092119/927985217975758898/Senosan-85382-HG-Dark-grey-600x600.png");
    }

    public void UpdateRespawnTimer(GamePlayer player, string timer) => EffectManager.sendUIEffectText(DEATH_KEY, player.TransportConnection, true, "RespawnTime", timer);

    public void ClearDeathUI(GamePlayer player)
    {
        if (!player.HasMidgameLoadout)
            player.Player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);

        EffectManager.askEffectClearByID(DEATH_ID, player.TransportConnection);
    }

    // KILL CARD UI

    public void SendKillCard(GamePlayer killer, GamePlayer victim, PlayerData victimData)
    {
        EffectManager.sendUIEffect(KILLCARD_ID, KILLCARD_KEY, killer.TransportConnection, true);
        EffectManager.sendUIEffectImageURL(KILLCARD_KEY, killer.TransportConnection, true, "EnemyIcon", victimData.AvatarLinks[2]);
        EffectManager.sendUIEffectImageURL(KILLCARD_KEY, killer.TransportConnection, true, "EnemyXPIcon", Plugin.Instance.DB.Levels.TryGetValue(victimData.Level, out var level) ? level.IconLinkMedium : "");
        EffectManager.sendUIEffectText(KILLCARD_KEY, killer.TransportConnection, true, "EnemyName", (victimData.HasPrime ? PRIME_SYMBOL : "") + victimData.SteamName.ToUpper());
        EffectManager.sendUIEffectText(KILLCARD_KEY, killer.TransportConnection, true, "EnemyXPNum", victimData.Level.ToString());
        EffectManager.sendUIEffectImageURL(KILLCARD_KEY, killer.TransportConnection, true, "DeathBanner", victim.ActiveLoadout?.Card?.Card?.CardLink ?? "https://cdn.discordapp.com/attachments/899796442649092119/927985217975758898/Senosan-85382-HG-Dark-grey-600x600.png");
    }

    public void RemoveKillCard(GamePlayer player) => EffectManager.askEffectClearByID(KILLCARD_ID, player.TransportConnection);

    // LOADING UI

    public void SendLoadingUI(UnturnedPlayer player, bool isMatch, Game game, string loadingText = "LOADING...")
    {
        var transportConnection = player.Player.channel.owner.transportConnection;

        player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);

        EffectManager.sendUIEffect(LOADING_UI_ID, LOADING_UI_KEY, transportConnection, true);
        EffectManager.sendUIEffectVisibility(LOADING_UI_KEY, transportConnection, true, "Scene Loading Match Toggler", isMatch);
        EffectManager.sendUIEffectVisibility(LOADING_UI_KEY, transportConnection, true, "Scene Loading Menu Toggler", !isMatch);
        EffectManager.sendUIEffectText(LOADING_UI_KEY, transportConnection, true, "LOADING Bar TEXT", loadingText);

        if (isMatch)
        {
            var location = game.Location;
            var gameMode = game.GameMode;
            
            var gameModeOption = Config.Gamemode.FileData.GamemodeOptions.FirstOrDefault(k => k.GameType == gameMode);
            if (gameModeOption == null)
                return;

            EffectManager.sendUIEffectVisibility(LOADING_UI_KEY, transportConnection, true, $"SERVER Loading Map {location.LocationName} Enabler", true);
            EffectManager.sendUIEffectImageURL(LOADING_UI_KEY, transportConnection, true, $"LOADING Gamemode Icon", gameModeOption.GamemodeIcon);
            EffectManager.sendUIEffectText(LOADING_UI_KEY, transportConnection, true, "LOADING Map TEXT", location.LocationName);
            EffectManager.sendUIEffectText(LOADING_UI_KEY, transportConnection, true, "LOADING Gamemode TEXT", (game.GameEvent == null ? "" : $"<color={game.GameEvent.EventColor}>{game.GameEvent.EventName}</color> ") + Plugin.Instance.Translate($"{gameMode}_Name_Full").ToRich());
            EffectManager.sendUIEffectText(LOADING_UI_KEY, transportConnection, true, "LOADING Bar Fill", "　");
        }

        if (TipSender.TryGetValue(player.CSteamID, out var tipSender))
        {
            tipSender.Stop();
            _ = TipSender.Remove(player.CSteamID);
        }

        TipSender.Add(player.CSteamID, Plugin.Instance.StartCoroutine(SendTip(player)));
    }

    public void UpdateLoadingBar(UnturnedPlayer player, string bar, string loadingText = "LOADING...")
    {
        var transportConnection = player.Player.channel.owner.transportConnection;
        EffectManager.sendUIEffectText(LOADING_UI_KEY, transportConnection, true, "LOADING Bar TEXT", loadingText);
        EffectManager.sendUIEffectText(LOADING_UI_KEY, transportConnection, true, "LOADING Bar Fill", bar);
    }

    public void UpdateLoadingText(UnturnedPlayer player, string loadingText)
    {
        EffectManager.sendUIEffectText(LOADING_UI_KEY, player.Player.channel.owner.transportConnection, true, "LOADING Bar TEXT", loadingText);
    }
    
    public void UpdateLoadingTip(UnturnedPlayer player, string tip) => EffectManager.sendUIEffectText(LOADING_UI_KEY, player.Player.channel.owner.transportConnection, true, "LOADING Tip Description TEXT", tip);

    public void ClearLoadingUI(UnturnedPlayer player)
    {
        if (TipSender.TryGetValue(player.CSteamID, out var tipSender))
        {
            tipSender.Stop();
            _ = TipSender.Remove(player.CSteamID);
        }

        player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
        EffectManager.askEffectClearByID(LOADING_UI_ID, player.Player.channel.owner.transportConnection);
    }

    public IEnumerator SendTip(UnturnedPlayer player)
    {
        var db = Plugin.Instance.DB;
        while (true)
        {
            UpdateLoadingTip(player, db.ServerOptions.GameTips[UnityEngine.Random.Range(0, db.ServerOptions.GameTips.Count)]);
            yield return new WaitForSeconds(10);
        }
    }

    // PRE ENDING UI

    public void SendPreEndingUI(GamePlayer player) => EffectManager.sendUIEffect(PRE_ENDING_UI_ID, PRE_ENDING_UI_KEY, player.TransportConnection, true);

    public void SetupPreEndingUI(GamePlayer player, EGameType gameMode, bool hasWon, int blueScore, int redScore, string blueName, string redName, bool isDraw)
    {
        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, isDraw ? "Draw" : hasWon ? "Victory" : "Defeat", true);
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.TransportConnection, true, isDraw ? "DrawTxt" : hasWon ? "VictoryTxt" : "DefeatTxt", Plugin.Instance.Translate(isDraw ? $"{gameMode}_Draw_Desc" : hasWon ? $"{gameMode}_Victory_Desc" : $"{gameMode}_Defeat_Desc").ToRich());

        if (gameMode == EGameType.FFA)
            return;

        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scores", true);
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.TransportConnection, true, "BlueSideScore", blueScore.ToString());
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.TransportConnection, true, "BlueSideName", blueName);
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.TransportConnection, true, "RedSideScore", redScore.ToString());
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.TransportConnection, true, "RedSideName", redName);
    }

    public void ClearPreEndingUI(GamePlayer player) => EffectManager.askEffectClearByID(PRE_ENDING_UI_ID, player.TransportConnection);

    // MIDGAME LOADOUT UI

    public void ShowMidgameLoadoutUI(GamePlayer player)
    {
        if (UIHandlersLookup.TryGetValue(player.SteamID, out var handler))
            handler.ShowMidgameLoadouts();
    }

    public void ClearMidgameLoadoutUI(GamePlayer player)
    {
        player.HasMidgameLoadout = false;
        player.Player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
        EffectManager.askEffectClearByID(27643, player.TransportConnection);
    }

    // GAMEMODE POPUP

    public void SendGamemodePopup(GamePlayer player, EGameType gameMode)
    {
        var option = Config.Gamemode.FileData.GamemodeOptions.FirstOrDefault(k => k.GameType == gameMode);
        if (option == null)
            return;

        EffectManager.sendUIEffect(GAMEMODE_POPUP_ID, GAMEMODE_POPUP_KEY, player.TransportConnection, true);
        EffectManager.sendUIEffectVisibility(GAMEMODE_POPUP_KEY, player.TransportConnection, true, $"GAMEMODE {gameMode} Toggler", true);
        EffectManager.sendUIEffectImageURL(GAMEMODE_POPUP_KEY, player.TransportConnection, true, "GAMEMODE Icon", option.GamemodeIcon);
        EffectManager.sendUIEffectText(GAMEMODE_POPUP_KEY, player.TransportConnection, true, "GAMEMODE Title TEXT", Plugin.Instance.Translate($"{gameMode}_Name").ToRich());
        EffectManager.sendUIEffectText(GAMEMODE_POPUP_KEY, player.TransportConnection, true, "GAMEMODE Description TEXT", Plugin.Instance.Translate($"{gameMode}_Desc").ToRich());
    }

    // QUEST PROGRESSION

    public void SendQuestProgression(GamePlayer player, List<PlayerQuest> questsUpdate)
    {
        EffectManager.sendUIEffect(QUEST_PROGRESSION_ID, QUEST_PROGRESSION_KEY, player.TransportConnection, true);
        foreach (var quest in questsUpdate)
        {
            var i = (int)quest.Quest.QuestTier;
            EffectManager.sendUIEffectVisibility(QUEST_PROGRESSION_KEY, player.TransportConnection, true, $"QUEST Item {i}", true);
            EffectManager.sendUIEffectText(QUEST_PROGRESSION_KEY, player.TransportConnection, true, $"QUEST Description {i} TEXT", quest.Quest.QuestDesc);
            EffectManager.sendUIEffectText(QUEST_PROGRESSION_KEY, player.TransportConnection, true, $"QUEST Target {i} TEXT", $"{quest.Amount} / {quest.Quest.TargetAmount}");
            EffectManager.sendUIEffectText(QUEST_PROGRESSION_KEY, player.TransportConnection, true, $"QUEST Progress {i} Fill", quest.Amount == 0 ? HAIRSPACE_SYMBOL_STRING : new(HAIRSPACE_SYMBOL_CHAR, Math.Min(267, quest.Amount * 267 / quest.Quest.TargetAmount)));
        }
    }

    // ROUND END DROPS

    public IEnumerator SetupRoundEndDrops(List<GamePlayer> players, List<(GamePlayer, Case)> roundEndCases, int v)
    {
        foreach (var player in players)
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, $"Drops{v}", true);

        for (var i = 0; i < roundEndCases.Count; i++)
        {
            yield return new WaitForSeconds(1f);

            var roundEndCase = roundEndCases[i];
            foreach (var player in players)
            {
                EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, $"SERVER Scoreboard{v} Drop {i}", true);
                EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, $"SERVER Scoreboard{v} Drop {roundEndCase.Item2.CaseRarity} {i}", true);
                EffectManager.sendUIEffectImageURL(PRE_ENDING_UI_KEY, player.TransportConnection, true, $"SERVER Scoreboard{v} Drop IMAGE {i}", roundEndCase.Item2.IconLink);
                EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, $"SERVER Scoreboard Drop Sound {roundEndCase.Item2.CaseRarity}", true);
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.TransportConnection, true, $"SERVER Scoreboard{v} Drop TEXT {i}", (roundEndCase.Item1.Data.HasPrime ? PRIME_SYMBOL : "") + roundEndCase.Item1.Player.CharacterName);
            }
        }
    }

    // KILLSTREAK 

    public void SetupKillstreakUI(GamePlayer player)
    {
        EffectManager.sendUIEffect(KILLSTREAK_ID, KILLSTREAK_KEY, player.TransportConnection, true);

        for (var i = 0; i <= 2; i++)
        {
            if (player.OrderedKillstreaks.Count < i + 1)
            {
                EffectManager.sendUIEffectVisibility(KILLSTREAK_KEY, player.TransportConnection, true, $"KillstreakIcon{i}", false);
                EffectManager.sendUIEffectVisibility(KILLSTREAK_KEY, player.TransportConnection, true, $"BarContainer{i}", false);
                continue;
            }

            var killstreak = player.OrderedKillstreaks[i];
            EffectManager.sendUIEffectImageURL(KILLSTREAK_KEY, player.TransportConnection, true, $"KillstreakIcon{i}", killstreak.Killstreak.KillstreakInfo.KillstreakHUDIconURL);
        }
    }

    public void UpdateKillstreakBars(GamePlayer player, int currentKillstreak)
    {
        var previousKillstreakRequirement = 0;
        for (var i = 0; i < player.OrderedKillstreaks.Count; i++)
        {
            var killstreak = player.OrderedKillstreaks[i];
            if (previousKillstreakRequirement > currentKillstreak)
            {
                EffectManager.sendUIEffectText(KILLSTREAK_KEY, player.TransportConnection, true, $"BarFill{i}", VERY_SMALL_SQUARE);
                continue;
            }

            var spaces = Math.Min(MAX_SPACES_KILLSTREAK, Math.Max(0, (currentKillstreak - previousKillstreakRequirement) * MAX_SPACES_KILLSTREAK / (killstreak.Killstreak.KillstreakRequired - previousKillstreakRequirement)));
            //if (spaces == 0)
            //{
            //EffectManager.sendUIEffectVisibility(KILLSTREAK_KEY, player.TransportConnection, true, $"BarEmptier{i}", true);
            //continue;
            //}

            EffectManager.sendUIEffectText(KILLSTREAK_KEY, player.TransportConnection, true, $"BarFill{i}", spaces == 0 ? VERY_SMALL_SQUARE : new(HAIRSPACE_SYMBOL_CHAR, spaces));
            previousKillstreakRequirement = killstreak.Killstreak.KillstreakRequired;
        }
    }

    public void UpdateKillstreakReady(GamePlayer player, LoadoutKillstreak killstreak)
    {
        var i = player.OrderedKillstreaks.IndexOf(killstreak);
        EffectManager.sendUIEffectVisibility(KILLSTREAK_KEY, player.TransportConnection, true, $"KillstreakReady{i}", player.AvailableKillstreaks[killstreak]);
    }

    public void SendKillstreakTimer(GamePlayer player, int seconds)
    {
        EffectManager.sendUIEffectVisibility(KILLSTREAK_KEY, player.TransportConnection, true, "KillstreakTimer", true);
        EffectManager.sendUIEffectText(KILLSTREAK_KEY, player.TransportConnection, true, "KillstreakTimerNum", seconds.ToString());
    }

    public void UpdateKillstreakTimer(GamePlayer player, int seconds) => EffectManager.sendUIEffectText(KILLSTREAK_KEY, player.TransportConnection, true, "KillstreakTimerNum", seconds.ToString());

    public void ClearKillstreakTimer(GamePlayer player) => EffectManager.sendUIEffectVisibility(KILLSTREAK_KEY, player.TransportConnection, true, "KillstreakTimer", false);

    public void ClearKillstreakUI(GamePlayer player) => EffectManager.askEffectClearByID(KILLSTREAK_ID, player.TransportConnection);

    // DEATHSTREAK

    public void SetupActiveDeathstreakUI(GamePlayer player)
    {
        var info = player.ActiveLoadout.Deathstreak.Deathstreak.DeathstreakInfo;
        EffectManager.sendUIEffect(DEATHSTREAK_ACTIVE_ID, DEATHSTREAK_ACTIVE_KEY, player.TransportConnection, true);
        EffectManager.sendUIEffectImageURL(DEATHSTREAK_ACTIVE_KEY, player.TransportConnection, true, "DeathstreakIcon", info.DeathstreakHUDIconURL);
        EffectManager.sendUIEffectText(DEATHSTREAK_ACTIVE_KEY, player.TransportConnection, true, "DeathstreakTimerNum", info.DeathstreakStaySeconds.ToString());
    }

    public void UpdateDeathstreakTimer(GamePlayer player, int seconds) => EffectManager.sendUIEffectText(DEATHSTREAK_ACTIVE_KEY, player.TransportConnection, true, "DeathstreakTimerNum", seconds.ToString());
    
    public void RemoveActiveDeathstreakUI(GamePlayer player) => EffectManager.askEffectClearByID(DEATHSTREAK_ACTIVE_ID, player.TransportConnection);
    
    // ABILITIES

    public void SendAbilityUI(GamePlayer player)
    {
        var ability = player.ActiveLoadout.Ability;
        if (ability == null)
            return;
        
        EffectManager.sendUIEffectVisibility(HUD_KEY, player.TransportConnection, true, "AbilityIcon", true);
        EffectManager.sendUIEffectImageURL(HUD_KEY, player.TransportConnection, true, "AbilityIcon", ability.Ability.AbilityInfo.AbilityHUDIconURL);
        EffectManager.sendUIEffectVisibility(HUD_KEY, player.TransportConnection, true, "AbilityReady", player.HasAbilityAvailable);
        EffectManager.sendUIEffectText(HUD_KEY, player.TransportConnection, true, "AbilityStatus", player.HasAbilityAvailable ? "READY" : ability.Ability.AbilityInfo.CooldownSeconds.ToString());
    }

    public void UpdateAbilityTimer(GamePlayer player, int seconds, bool isBeingUsed = false)
    {
        EffectManager.sendUIEffectText(HUD_KEY, player.TransportConnection, true, "AbilityStatus", isBeingUsed ? $"<color=orange>{seconds}</color>" : seconds.ToString());
    }

    public void UpdateAbilityReady(GamePlayer player)
    {
        EffectManager.sendUIEffectVisibility(HUD_KEY, player.TransportConnection, true, "AbilityReady", player.HasAbilityAvailable);
        EffectManager.sendUIEffectText(HUD_KEY, player.TransportConnection, true, "AbilityStatus", player.HasAbilityAvailable ? "READY" : player.ActiveLoadout.Ability.Ability.AbilityInfo.CooldownSeconds.ToString());
    }
    
    public void RemoveAbilityUI(GamePlayer player) => EffectManager.sendUIEffectVisibility(HUD_KEY, player.TransportConnection, true, "AbilityIcon", false);

    // HUD RELATED UI

    private void OnConnected(UnturnedPlayer player)
    {
        player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.ShowLifeMeters);
        player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.ShowStatusIcons);
        player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.ShowUseableGunStatus);
        player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.ShowDeathMenu);
        player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.ShowReputationChangeNotification);
        
        player.Player.equipment.onEquipRequested += OnEquipRequested;
        player.Player.equipment.onDequipRequested += OnDequipRequested;

        player.Player.inventory.onDropItemRequested += OnDropItemRequested;
        player.Player.stance.onStanceUpdated += () => OnStanceUpdated(player.Player);

        player.Player.voice.ServerSetPermissions(true, true);
        player.Player.inventory.items[2].resize(Config.Base.FileData.HandSlotWidth, Config.Base.FileData.HandSlotHeight);

        var transportConnection = player.Player.channel.owner.transportConnection;

        EffectManager.sendUIEffect(HUD_ID, HUD_KEY, transportConnection, true);
        RemoveGunUI(transportConnection);

        // SOUND UI
        EffectManager.sendUIEffect(SOUNDS_ID, SOUNDS_KEY, transportConnection, true);
    }

    private void OnDisconnected(UnturnedPlayer player)
    {
        player.Player.equipment.onEquipRequested -= OnEquipRequested;
        player.Player.equipment.onDequipRequested -= OnDequipRequested;
        player.Player.inventory.onDropItemRequested -= OnDropItemRequested;
        player.Player.stance.onStanceUpdated -= () => OnStanceUpdated(player.Player);
    }

    private void OnStaminaUpdated(UnturnedPlayer player, byte stamina)
    {
        if (stamina > 50)
            return;

        var gPlayer = Plugin.Instance.Game.GetGamePlayer(player);
        if (gPlayer.HasKillstreakActive && gPlayer.ActiveKillstreak.Killstreak.KillstreakInfo.HasInfiniteStamina)
            player.Player.life.serverModifyStamina(100);
    }

    private void OnStanceUpdated(Player player)
    {
        var gPlayer = Plugin.Instance.Game.GetGamePlayer(player);
        if (gPlayer.CurrentGame != null)
            gPlayer.OnStanceChanged(player.stance.stance);
    }

    private void OnDropItemRequested(PlayerInventory inventory, Item item, ref bool shouldAllow) => shouldAllow = false;

    public void SendGadgetIcons(GamePlayer player)
    {
        EffectManager.sendUIEffectImageURL(HUD_KEY, player.TransportConnection, true, "TacticalIcon", "https://cdn.discordapp.com/attachments/957636187114336257/958012815870930964/smoke_grenade.png");
        EffectManager.sendUIEffectImageURL(HUD_KEY, player.TransportConnection, true, "LethalIcon", "https://cdn.discordapp.com/attachments/957636187114336257/958012816470708284/grenade.png");
    }

    public void UpdateGadgetUsed(GamePlayer player, bool isTactical, bool isUsed) => EffectManager.sendUIEffectVisibility(HUD_KEY, player.TransportConnection, true, $"{(isTactical ? "Tactical" : "Lethal")} Used Toggler", isUsed);

    protected void OnEquipRequested(PlayerEquipment equipment, ItemJar jar, ItemAsset asset, ref bool shouldAllow)
    {
        var player = Plugin.Instance.Game.GetGamePlayer(equipment.player);
        var inv = player.Player.Player.inventory;
        var game = player.CurrentGame;
        if (game == null)
            return;

        var isCarryingFlag = game.IsPlayerCarryingFlag(player);
        if (isCarryingFlag && inv.getItem(0, 0) == jar)
        {
            shouldAllow = false;
            return;
        }

        if (player.ActiveLoadout == null)
            return;

        if ((jar.item.id == (player.ActiveLoadout.Tactical?.Gadget?.GadgetID ?? 0) && !player.HasTactical) || (jar.item.id == (player.ActiveLoadout.Tactical?.Gadget?.GadgetID ?? 0) && game.GamePhase != EGamePhase.STARTED))
        {
            shouldAllow = false;
            return;
        }

        if ((jar.item.id == (player.ActiveLoadout.Lethal?.Gadget?.GadgetID ?? 0) && !player.HasLethal) || (jar.item.id == (player.ActiveLoadout.Lethal?.Gadget?.GadgetID ?? 0) && game.GamePhase != EGamePhase.STARTED))
        {
            shouldAllow = false;
            return;
        }

        if (player.KillstreakTriggers.TryGetValue(jar.item.id, out var activateKillstreak))
        {
            shouldAllow = false;
            if (game.GamePhase == EGamePhase.STARTED && player.AvailableKillstreaks[activateKillstreak] && !isCarryingFlag && !player.HasKillstreakActive && !player.HasAbilityActive)
                player.ActivateKillstreak(activateKillstreak);

            return;
        }

        if (jar.item.id == (player.ActiveLoadout.Ability?.Ability.AbilityInfo.TriggerItemID ?? 0))
        {
            shouldAllow = false;
            if (game.GamePhase == EGamePhase.STARTED && player.HasAbilityAvailable && !isCarryingFlag && !player.HasKillstreakActive && !player.HasAbilityActive)
                player.ActivateAbility();

            return;
        }
        
        TaskDispatcher.QueueOnMainThread(() =>
        {
            var connection = player.TransportConnection;
            if (asset == null)
                return;

            if (player.HasKillstreakActive && player.ActiveKillstreak.Killstreak.KillstreakInfo.IsItem && asset.id != player.ActiveKillstreak.Killstreak.KillstreakInfo.ItemID)
                player.RemoveActiveKillstreak();

            if (player.HasAbilityActive && asset.id != player.ActiveLoadout.Ability.Ability.AbilityInfo.ItemID)
                player.RemoveActiveAbility();

            var isPrimarySecondaryMelee = asset.id == (player.ActiveLoadout.PrimarySkin?.SkinID ?? 0) || asset.id == (player.ActiveLoadout.Primary?.Gun?.GunID ?? 0) || asset.id == (player.ActiveLoadout.SecondarySkin?.SkinID ?? 0) || asset.id == (player.ActiveLoadout.Secondary?.Gun?.GunID ?? 0) ||
                                          asset.id == (player.ActiveLoadout.Knife?.Knife?.KnifeID ?? 0);

            player.ForceEquip = !isPrimarySecondaryMelee;
            if (isPrimarySecondaryMelee)
            {
                player.LastEquippedPage = equipment.equippedPage;
                player.LastEquippedX = equipment.equipped_x;
                player.LastEquippedY = equipment.equipped_y;
            }

            if (asset.type == EItemType.GUN)
            {
                int currentAmmo = equipment.state[10];
                var ammo = 0;

                if (Assets.find(EAssetType.ITEM, BitConverter.ToUInt16(equipment.state, 8)) is ItemMagazineAsset mAsset)
                    ammo = mAsset.amount;

                EffectManager.sendUIEffectText(HUD_KEY, connection, true, "AmmoNum", currentAmmo.ToString());
                EffectManager.sendUIEffectText(HUD_KEY, connection, true, "ReserveNum", $" / {ammo}");
            }
            else
            {
                EffectManager.sendUIEffectText(HUD_KEY, connection, true, "AmmoNum", " ");
                EffectManager.sendUIEffectText(HUD_KEY, connection, true, "ReserveNum", " ");
            }
        });
    }

    private void OnDequipRequested(PlayerEquipment equipment, ref bool shouldAllow)
    {
        var player = Plugin.Instance.Game.GetGamePlayer(equipment.player);
        if (player.HasKillstreakActive && player.ActiveKillstreak.Killstreak.KillstreakInfo.IsItem)
            TaskDispatcher.QueueOnMainThread(() => player.RemoveActiveKillstreak());
        
        if (player.HasAbilityActive)
            TaskDispatcher.QueueOnMainThread(() => player.RemoveActiveAbility());
    }

    public void OnUseableChanged(PlayerEquipment obj)
    {
        var player = Plugin.Instance.Game.GetGamePlayer(obj.player);

        if (player?.CurrentGame == null)
            return;

        if (player.ActiveLoadout == null)
            return;

        if (obj.useable != null)
            return;

        if (player.ForceEquip && !player.CurrentGame.IsPlayerCarryingFlag(player))
        {
            _ = Plugin.Instance.StartCoroutine(DelayedEquip(player.Player.Player.equipment, player.LastEquippedPage, player.LastEquippedX, player.LastEquippedY));
            return;
        }

        if (player.ForceEquip && player.Player.Player.inventory.getItem(1, 0) != null)
        {
            _ = Plugin.Instance.StartCoroutine(DelayedEquip(player.Player.Player.equipment, 1, 0, 0));
            return;
        }

        _ = Plugin.Instance.StartCoroutine(DelayedEquip(player.Player.Player.equipment, player.KnifePage, player.KnifeX, player.KnifeY));
    }

    public IEnumerator DelayedEquip(PlayerEquipment equipment, byte page, byte x, byte y)
    {
        yield return new WaitForSeconds(0.2f);

        if (equipment.itemID == 0 && equipment.canEquip)
            equipment.ServerEquip(page, x, y);
    }

    public void RemoveGunUI(ITransportConnection transportConnection)
    {
        EffectManager.sendUIEffectText(HUD_KEY, transportConnection, true, "AmmoNum", " ");
        EffectManager.sendUIEffectText(HUD_KEY, transportConnection, true, "ReserveNum", " ");
        EffectManager.sendUIEffectImageURL(HUD_KEY, transportConnection, true, "TacticalIcon", "");
        EffectManager.sendUIEffectImageURL(HUD_KEY, transportConnection, true, "LethalIcon", "");
        EffectManager.sendUIEffectVisibility(HUD_KEY, transportConnection, true, "AbilityIcon", false);
    }

    private void OnMagazineChanged(PlayerEquipment equipment, UseableGun gun, Item oldItem, ItemJar newItem, ref bool shouldAllow)
    {
        var amount = newItem == null ? 0 : newItem.item.amount;
        var transportConnection = equipment.player.channel.GetOwnerTransportConnection();

        EffectManager.sendUIEffectText(HUD_KEY, transportConnection, true, "AmmoNum", amount.ToString());
        EffectManager.sendUIEffectText(HUD_KEY, transportConnection, true, "ReserveNum", $" / {amount}");
    }

    private void OnBulletShot(UseableGun gun, BulletInfo bullet)
    {
        var ids = new ushort[] { 17001 };
        var ammo = ids.Contains(bullet.magazineAsset.id) ? 0 : gun.player.equipment.state[10];
        var player = Plugin.Instance.Game.GetGamePlayer(gun.player);
        EffectManager.sendUIEffectText(HUD_KEY, player.TransportConnection, true, "AmmoNum", ammo.ToString());

        if (ammo != 0)
            return;
        
        var killstreakInfo = player.ActiveKillstreak?.Killstreak?.KillstreakInfo; 
        if (player.HasKillstreakActive && killstreakInfo is { IsItem: true, RemoveWhenAmmoEmpty: true })
        {
            if (killstreakInfo.MagAmount > 0)
            {
                var inv = gun.player.inventory;
                var itemCount = inv.items[2].items.Count;
                for (var i = itemCount - 1; i >= 0; i--)
                {
                    var item = inv.getItem(2, (byte)i);
                    if ((item?.item?.id ?? 0) == killstreakInfo.MagID)
                        return;
                }
            }

            _ = Plugin.Instance.StartCoroutine(DelayedRemoveActiveKillstreak(player));
        }

        var abilityInfo = player.ActiveLoadout.Ability?.Ability.AbilityInfo;
        if (player.HasAbilityActive && abilityInfo is { RemoveWhenAmmoEmpty: true })
        {
            if (abilityInfo.MagAmount > 0)
            {
                var inv = gun.player.inventory;
                var itemCount = inv.items[2].items.Count;
                for (var i = itemCount - 1; i >= 0; i--)
                {
                    var item = inv.getItem(2, (byte)i);
                    if ((item?.item?.id ?? 0) == abilityInfo.MagID)
                        return;
                }
            }

            _ = Plugin.Instance.StartCoroutine(DelayedRemoveActiveAbility(player));
        }
    }

    private void OnProjectileShot(UseableGun sender, GameObject projectile)
    {
        var ammo = sender.player.equipment.state[10];
        var player = Plugin.Instance.Game.GetGamePlayer(sender.player);
        EffectManager.sendUIEffectText(HUD_KEY, player.TransportConnection, true, "AmmoNum", ammo.ToString());

        if (ammo != 0)
            return;
        
        var killstreakInfo = player.ActiveKillstreak?.Killstreak?.KillstreakInfo; 
        if (player.HasKillstreakActive && killstreakInfo is { IsItem: true, RemoveWhenAmmoEmpty: true })
        {
            if (killstreakInfo.MagAmount > 0)
            {
                var inv = sender.player.inventory;
                var itemCount = inv.items[2].items.Count;
                for (var i = itemCount - 1; i >= 0; i--)
                {
                    var item = inv.getItem(2, (byte)i);
                    if ((item?.item?.id ?? 0) == killstreakInfo.MagID)
                        return;
                }
            }

            _ = Plugin.Instance.StartCoroutine(DelayedRemoveActiveKillstreak(player));
        }

        var abilityInfo = player.ActiveLoadout.Ability?.Ability.AbilityInfo;
        if (player.HasAbilityActive && abilityInfo is { RemoveWhenAmmoEmpty: true })
        {
            if (abilityInfo.MagAmount > 0)
            {
                var inv = sender.player.inventory;
                var itemCount = inv.items[2].items.Count;
                for (var i = itemCount - 1; i >= 0; i--)
                {
                    var item = inv.getItem(2, (byte)i);
                    if ((item?.item?.id ?? 0) == abilityInfo.MagID)
                        return;
                }
            }

            _ = Plugin.Instance.StartCoroutine(DelayedRemoveActiveAbility(player));
        }
    }

    public IEnumerator DelayedRemoveActiveKillstreak(GamePlayer player)
    {
        yield return new WaitForSeconds(0.5f);

        player.RemoveActiveKillstreak();
    }

    public IEnumerator DelayedRemoveActiveAbility(GamePlayer player)
    {
        yield return new WaitForSeconds(0.5f);
        
        player.RemoveActiveAbility();
    }

    // FFA RELATED UI

    public void SendFFAHUD(GamePlayer player)
    {
        EffectManager.sendUIEffect(FFA_ID, FFA_KEY, player.TransportConnection, true);

        EffectManager.sendUIEffectVisibility(FFA_KEY, player.TransportConnection, true, "ScoreCounter", true);
        SendGamemodePopup(player, EGameType.FFA);
        EffectManager.sendUIEffectVisibility(FFA_KEY, player.TransportConnection, true, "Timer", true);
    }

    public void UpdateFFATimer(GamePlayer player, string text) => EffectManager.sendUIEffectText(FFA_KEY, player.TransportConnection, true, "TimerTxt", text);

    public void UpdateFFATopUI(FFAPlayer player, List<FFAPlayer> Players)
    {
        if (Players.Count == 0)
            return;

        var firstPlayer = Players[0];
        var secondPlayer = player;
        if (player.GamePlayer.SteamID == firstPlayer.GamePlayer.SteamID)
        {
            secondPlayer = Players.Count > 1 ? Players[1] : null;

            EffectManager.sendUIEffectVisibility(FFA_KEY, player.GamePlayer.TransportConnection, true, "CounterWinning", true);
            EffectManager.sendUIEffectVisibility(FFA_KEY, player.GamePlayer.TransportConnection, true, "CounterLosing", false);
        }
        else
        {
            EffectManager.sendUIEffectVisibility(FFA_KEY, player.GamePlayer.TransportConnection, true, "CounterWinning", false);
            EffectManager.sendUIEffectVisibility(FFA_KEY, player.GamePlayer.TransportConnection, true, "CounterLosing", true);
        }

        EffectManager.sendUIEffectText(FFA_KEY, player.GamePlayer.TransportConnection, true, "1stPlacementName", (firstPlayer.GamePlayer.Data.HasPrime ? PRIME_SYMBOL : "") + firstPlayer.GamePlayer.Player.CharacterName);
        EffectManager.sendUIEffectText(FFA_KEY, player.GamePlayer.TransportConnection, true, "1stPlacementScore", firstPlayer.Kills.ToString());

        EffectManager.sendUIEffectText(FFA_KEY, player.GamePlayer.TransportConnection, true, "2ndPlacementPlace", secondPlayer != null ? Utility.GetOrdinal(Players.IndexOf(secondPlayer) + 1) : "0");
        EffectManager.sendUIEffectText(FFA_KEY, player.GamePlayer.TransportConnection, true, "2ndPlacementName", secondPlayer != null ? (secondPlayer.GamePlayer.Data.HasPrime ? PRIME_SYMBOL : "") + secondPlayer.GamePlayer.Player.CharacterName : "NONE");
        EffectManager.sendUIEffectText(FFA_KEY, player.GamePlayer.TransportConnection, true, "2ndPlacementScore", secondPlayer != null ? secondPlayer.Kills.ToString() : "0");
    }

    public void SetupFFALeaderboard(List<FFAPlayer> players, Game game)
    {
        foreach (var player in players)
            SetupFFALeaderboard(player, players, game);
    }

    public void SetupFFALeaderboard(FFAPlayer ply, List<FFAPlayer> players, Game game)
    {
        var location = game.Location;
        var isPlaying = game.GamePhase != EGamePhase.ENDING;
        
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, "MatchResult1", Plugin.Instance.Translate(players.IndexOf(ply) == 0 ? isPlaying ? "Winning_Text" : "Victory_Text" : isPlaying ? "Losing_Text" : "Defeat_Text").ToRich());
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, "MapName1", location.LocationName.ToUpper());
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, "GamemodeName1", (game.GameEvent == null ? "" : $"{game.GameEvent.EventName} ") + Plugin.Instance.Translate("FFA_Name_Full").ToRich());

        for (var i = 0; i <= 11; i++)
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, $"PlayerStats{i}", false);

        for (var i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var isPlayer = player == ply;
            var data = player.GamePlayer.Data;

            decimal kills = player.Kills;
            decimal deaths = player.Deaths;

            var ratio = player.Deaths == 0 ? string.Format("{0:n}", kills) : string.Format("{0:n}", Math.Round(kills / deaths, 2));

            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, $"PlayerStats{i}", true);
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, $"NameTxt{i}", (data.HasPrime ? PRIME_SYMBOL : "") + data.SteamName.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, $"KillsTxt{i}", player.Kills.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, $"DeathsTxt{i}", player.Deaths.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, $"KDRTxt{i}", ratio.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, $"ScoreTxt{i}", player.Score.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, $"LvlTxt{i}", data.Level.ToColor(isPlayer));
            EffectManager.sendUIEffectImageURL(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, $"LvlIcon{i}", Plugin.Instance.DB.Levels.TryGetValue(data.Level, out var level) ? level.IconLinkSmall : "");
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, $"AssistsTxt{i}", player.Assists.ToColor(isPlayer));
        }
    }

    public void ShowFFALeaderboard(GamePlayer player)
    {
        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Victory", false);
        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Defeat", false);
        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Draw", false);
        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scores", false);
    
        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scoreboard1", true);
    }

    public void HideFFALeaderboard(GamePlayer player) => EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scoreboard1", false);

    public void ClearFFAHUD(GamePlayer player) => EffectManager.askEffectClearByID(FFA_ID, player.TransportConnection);

    // TDM Related UI

    public void SendTDMHUD(TDMPlayer player, TDMTeam blueTeam, TDMTeam redTeam)
    {
        EffectManager.sendUIEffect(TDM_ID, TDM_KEY, player.GamePlayer.TransportConnection, true);
        EffectManager.sendUIEffectVisibility(TDM_KEY, player.GamePlayer.TransportConnection, true, player.Team.TeamID == (byte)ETeam.BLUE ? "BlueTeam" : "RedTeam", true);
        SendGamemodePopup(player.GamePlayer, EGameType.TDM);
        EffectManager.sendUIEffectVisibility(TDM_KEY, player.GamePlayer.TransportConnection, true, "Timer", true);
        EffectManager.sendUIEffectVisibility(TDM_KEY, player.GamePlayer.TransportConnection, true, "Team", true);
        EffectManager.sendUIEffectText(TDM_KEY, player.GamePlayer.TransportConnection, true, "TeamName", $"<color={player.Team.Info.TeamColorHexCode}>{player.Team.Info.TeamName}</color>");

        var index = player.Team.TeamID == (byte)ETeam.BLUE ? 1 : 0;
        var blueSpaces = blueTeam.Score * MAX_SPACES_TDM_SCORE / Config.TDM.FileData.ScoreLimit;
        var redSpaces = redTeam.Score * MAX_SPACES_TDM_SCORE / Config.TDM.FileData.ScoreLimit;
        EffectManager.sendUIEffectText(TDM_KEY, player.GamePlayer.TransportConnection, true, $"RedNum{index}", redTeam.Score.ToString());
        EffectManager.sendUIEffectText(TDM_KEY, player.GamePlayer.TransportConnection, true, $"RedBarFill{index}", redSpaces == 0 ? HAIRSPACE_SYMBOL_STRING : new(HAIRSPACE_SYMBOL_CHAR, redSpaces));

        EffectManager.sendUIEffectText(TDM_KEY, player.GamePlayer.TransportConnection, true, $"BlueNum{index}", blueTeam.Score.ToString());
        EffectManager.sendUIEffectText(TDM_KEY, player.GamePlayer.TransportConnection, true, $"BlueBarFill{index}", blueSpaces == 0 ? HAIRSPACE_SYMBOL_STRING : new(HAIRSPACE_SYMBOL_CHAR, blueSpaces));
    }

    public void UpdateTDMTimer(GamePlayer player, string text) => EffectManager.sendUIEffectText(TDM_KEY, player.TransportConnection, true, "TimerTxt", text);

    public void UpdateTDMScore(TDMPlayer player, TDMTeam changeTeam)
    {
        var index = player.Team.TeamID == (byte)ETeam.BLUE ? 1 : 0;
        var team = (ETeam)changeTeam.TeamID;
        var spaces = changeTeam.Score * MAX_SPACES_TDM_SCORE / Config.TDM.FileData.ScoreLimit;

        EffectManager.sendUIEffectText(TDM_KEY, player.GamePlayer.TransportConnection, true, $"{team.ToUIName()}Num{index}", changeTeam.Score.ToString());
        EffectManager.sendUIEffectText(TDM_KEY, player.GamePlayer.TransportConnection, true, $"{team.ToUIName()}BarFill{index}", spaces == 0 ? HAIRSPACE_SYMBOL_STRING : new(HAIRSPACE_SYMBOL_CHAR, spaces));
    }

    public void SetupTDMLeaderboard(List<TDMPlayer> players, TDMTeam wonTeam, TDMTeam blueTeam, TDMTeam redTeam, Game game)
    {
        foreach (var player in players)
            SetupTDMLeaderboard(player, players, wonTeam, blueTeam, redTeam, game);
    }

    public void SetupTDMLeaderboard(TDMPlayer player, List<TDMPlayer> players, TDMTeam wonTeam, TDMTeam blueTeam, TDMTeam redTeam, Game game)
    {
        var location = game.Location;
        var isPlaying = game.GamePhase != EGamePhase.ENDING;
        
        var bluePlayers = players.Where(k => k.Team.TeamID == (byte)ETeam.BLUE).ToList();
        var redPlayers = players.Where(k => k.Team.TeamID == (byte)ETeam.RED).ToList();

        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "MatchResult0", Plugin.Instance.Translate(wonTeam.TeamID == -1 ? isPlaying ? "Drawing_Text" : "Draw_Text" : player.Team == wonTeam ? isPlaying ? "Winning_Text" : "Victory_Text" : isPlaying ? "Losing_Text" : "Defeat_Text").ToRich());

        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "MapName0", location.LocationName.ToUpper());
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamNameR0", redTeam.Info.TeamName);
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamScoreR0", redTeam.Score.ToString());
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamNameB0", blueTeam.Info.TeamName);
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamScoreB0", blueTeam.Score.ToString());
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "GamemodeName0", (game.GameEvent == null ? "" : $"{game.GameEvent.EventName} ") + Plugin.Instance.Translate("TDM_Name_Full").ToRich());

        for (var i = 0; i <= 7; i++)
        {
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}B0", false);
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}R0", false);
        }

        for (var i = 0; i < bluePlayers.Count; i++)
        {
            var ply = bluePlayers[i];
            var isPlayer = player == ply;
            var data = ply.GamePlayer.Data;

            decimal kills = ply.Kills;
            decimal deaths = ply.Deaths;

            var ratio = ply.Deaths == 0 ? string.Format("{0:n}", kills) : string.Format("{0:n}", Math.Round(kills / deaths, 2));

            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}B0", true);
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"NameTxt{i}B0", (data.HasPrime ? PRIME_SYMBOL : "") + data.SteamName.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KillsTxt{i}B0", ply.Kills.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"DeathsTxt{i}B0", ply.Deaths.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KDRTxt{i}B0", ratio.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"ScoreTxt{i}B0", ply.Score.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlTxt{i}B0", data.Level.ToColor(isPlayer));
            EffectManager.sendUIEffectImageURL(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlIcon{i}B0", Plugin.Instance.DB.Levels.TryGetValue(data.Level, out var level) ? level.IconLinkSmall : "");
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"AssistsTxt{i}B0", ply.Assists.ToColor(isPlayer));
        }

        for (var i = 0; i < redPlayers.Count; i++)
        {
            var ply = redPlayers[i];
            var isPlayer = player == ply;
            var data = ply.GamePlayer.Data;

            decimal kills = ply.Kills;
            decimal deaths = ply.Deaths;

            var ratio = ply.Deaths == 0 ? string.Format("{0:n}", kills) : string.Format("{0:n}", Math.Round(kills / deaths, 2));

            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}R0", true);
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"NameTxt{i}R0", (data.HasPrime ? PRIME_SYMBOL : "") + data.SteamName.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KillsTxt{i}R0", ply.Kills.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"DeathsTxt{i}R0", ply.Deaths.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KDRTxt{i}R0", ratio.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"ScoreTxt{i}R0", ply.Score.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlTxt{i}R0", data.Level.ToColor(isPlayer));
            EffectManager.sendUIEffectImageURL(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlIcon{i}R0", Plugin.Instance.DB.Levels.TryGetValue(data.Level, out var level) ? level.IconLinkSmall : "");
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"AssistsTxt{i}R0", ply.Assists.ToColor(isPlayer));
        }
    }

    public void ShowTDMLeaderboard(GamePlayer player)
    {
        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Victory", false);
        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Defeat", false);
        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Draw", false);
        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scores", false);

        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scoreboard0", true);
    }

    public void HideTDMLeaderboard(GamePlayer player) => EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scoreboard0", false);

    public void ClearTDMHUD(GamePlayer player) => EffectManager.askEffectClearByID(TDM_ID, player.TransportConnection);

    // KC Related UI

    public void SendKCHUD(KCPlayer player, KCTeam blueTeam, KCTeam redTeam)
    {
        EffectManager.sendUIEffect(KC_ID, KC_KEY, player.GamePlayer.TransportConnection, true);
        EffectManager.sendUIEffectVisibility(KC_KEY, player.GamePlayer.TransportConnection, true, player.Team.TeamID == (byte)ETeam.BLUE ? "BlueTeam" : "RedTeam", true);
        SendGamemodePopup(player.GamePlayer, EGameType.KC);
        EffectManager.sendUIEffectVisibility(KC_KEY, player.GamePlayer.TransportConnection, true, "Timer", true);
        EffectManager.sendUIEffectVisibility(KC_KEY, player.GamePlayer.TransportConnection, true, "Team", true);
        EffectManager.sendUIEffectText(KC_KEY, player.GamePlayer.TransportConnection, true, "TeamName", $"<color={player.Team.Info.TeamColorHexCode}>{player.Team.Info.TeamName}</color>");

        var index = player.Team.TeamID == (byte)ETeam.BLUE ? 1 : 0;
        var blueSpaces = blueTeam.Score * MAX_SPACES_TDM_SCORE / Config.TDM.FileData.ScoreLimit;
        var redSpaces = redTeam.Score * MAX_SPACES_TDM_SCORE / Config.TDM.FileData.ScoreLimit;
        EffectManager.sendUIEffectText(KC_KEY, player.GamePlayer.TransportConnection, true, $"RedNum{index}", redTeam.Score.ToString());
        EffectManager.sendUIEffectText(KC_KEY, player.GamePlayer.TransportConnection, true, $"RedBarFill{index}", redSpaces == 0 ? HAIRSPACE_SYMBOL_STRING : new(HAIRSPACE_SYMBOL_CHAR, redSpaces));

        EffectManager.sendUIEffectText(KC_KEY, player.GamePlayer.TransportConnection, true, $"BlueNum{index}", blueTeam.Score.ToString());
        EffectManager.sendUIEffectText(KC_KEY, player.GamePlayer.TransportConnection, true, $"BlueBarFill{index}", blueSpaces == 0 ? HAIRSPACE_SYMBOL_STRING : new(HAIRSPACE_SYMBOL_CHAR, blueSpaces));
    }

    public void UpdateKCTimer(GamePlayer player, string text) => EffectManager.sendUIEffectText(KC_KEY, player.TransportConnection, true, "TimerTxt", text);

    public void UpdateKCScore(KCPlayer player, KCTeam changeTeam)
    {
        var index = player.Team.TeamID == (byte)ETeam.BLUE ? 1 : 0;
        var team = (ETeam)changeTeam.TeamID;
        var spaces = changeTeam.Score * MAX_SPACES_TDM_SCORE / Config.KC.FileData.ScoreLimit;

        EffectManager.sendUIEffectText(KC_KEY, player.GamePlayer.TransportConnection, true, $"{team.ToUIName()}Num{index}", changeTeam.Score.ToString());
        EffectManager.sendUIEffectText(KC_KEY, player.GamePlayer.TransportConnection, true, $"{team.ToUIName()}BarFill{index}", spaces == 0 ? HAIRSPACE_SYMBOL_STRING : new(HAIRSPACE_SYMBOL_CHAR, spaces));
    }

    public void SetupKCLeaderboard(List<KCPlayer> players, KCTeam wonTeam, KCTeam blueTeam, KCTeam redTeam, Game game)
    {
        foreach (var player in players)
            SetupKCLeaderboard(player, players, wonTeam, blueTeam, redTeam, game);
    }

    public void SetupKCLeaderboard(KCPlayer player, List<KCPlayer> players, KCTeam wonTeam, KCTeam blueTeam, KCTeam redTeam, Game game)
    {
        var location = game.Location;
        var isPlaying = game.GamePhase != EGamePhase.ENDING;
        
        var bluePlayers = players.Where(k => k.Team.TeamID == (byte)ETeam.BLUE).ToList();
        var redPlayers = players.Where(k => k.Team.TeamID == (byte)ETeam.RED).ToList();

        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "MatchResult2", Plugin.Instance.Translate(wonTeam.TeamID == -1 ? isPlaying ? "Drawing_Text" : "Draw_Text" : player.Team == wonTeam ? isPlaying ? "Winning_Text" : "Victory_Text" : isPlaying ? "Losing_Text" : "Defeat_Text").ToRich());

        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "MapName2", location.LocationName.ToUpper());
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamNameR1", redTeam.Info.TeamName);
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamScoreR1", redTeam.Score.ToString());
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamNameB1", blueTeam.Info.TeamName);
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamScoreB1", blueTeam.Score.ToString());
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "GamemodeName2", (game.GameEvent == null ? "" : $"{game.GameEvent.EventName} ") + Plugin.Instance.Translate("KC_Name_Full").ToRich());

        for (var i = 0; i <= 7; i++)
        {
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}B1", false);
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}R1", false);
        }

        for (var i = 0; i < bluePlayers.Count; i++)
        {
            var ply = bluePlayers[i];
            var isPlayer = player == ply;
            var data = ply.GamePlayer.Data;

            decimal kills = ply.Kills;
            decimal deaths = ply.Deaths;
            var objective = ply.KillsConfirmed + ply.KillsDenied;

            var ratio = ply.Deaths == 0 ? string.Format("{0:n}", kills) : string.Format("{0:n}", Math.Round(kills / deaths, 2));

            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}B1", true);
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"NameTxt{i}B1", (data.HasPrime ? PRIME_SYMBOL : "") + data.SteamName.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KillsTxt{i}B1", ply.Kills.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"DeathsTxt{i}B1", ply.Deaths.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KDRTxt{i}B1", ratio.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"ScoreTxt{i}B1", ply.Score.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlTxt{i}B1", data.Level.ToColor(isPlayer));
            EffectManager.sendUIEffectImageURL(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlIcon{i}B1", Plugin.Instance.DB.Levels.TryGetValue(data.Level, out var level) ? level.IconLinkSmall : "");
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"AssistsTxt{i}B1", ply.Assists.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"ObjectiveTxt{i}B1", objective.ToColor(isPlayer));
        }

        for (var i = 0; i < redPlayers.Count; i++)
        {
            var ply = redPlayers[i];
            var isPlayer = player == ply;
            var data = ply.GamePlayer.Data;

            decimal kills = ply.Kills;
            decimal deaths = ply.Deaths;
            var objective = ply.KillsConfirmed + ply.KillsDenied;

            var ratio = ply.Deaths == 0 ? string.Format("{0:n}", kills) : string.Format("{0:n}", Math.Round(kills / deaths, 2));

            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}R1", true);
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"NameTxt{i}R1", (data.HasPrime ? PRIME_SYMBOL : "") + data.SteamName.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KillsTxt{i}R1", ply.Kills.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"DeathsTxt{i}R1", ply.Deaths.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KDRTxt{i}R1", ratio.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"ScoreTxt{i}R1", ply.Score.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlTxt{i}R1", data.Level.ToColor(isPlayer));
            EffectManager.sendUIEffectImageURL(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlIcon{i}R1", Plugin.Instance.DB.Levels.TryGetValue(data.Level, out var level) ? level.IconLinkSmall : "");
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"AssistsTxt{i}R1", ply.Assists.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"ObjectiveTxt{i}R1", objective.ToColor(isPlayer));
        }
    }

    public void ShowKCLeaderboard(GamePlayer player)
    {
        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Victory", false);
        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Defeat", false);
        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Draw", false);
        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scores", false);

        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scoreboard2", true);
    }

    public void HideKCLeaderboard(GamePlayer player) => EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scoreboard2", false);

    public void SendKillConfirmedSound(GamePlayer player)
    {
        var random = UnityEngine.Random.Range(0, 4);
        EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, $"KillConfirmed{random}", false);
        EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, $"KillConfirmed{random}", true);
    }

    public void SendKillDeniedSound(GamePlayer player)
    {
        var random = UnityEngine.Random.Range(0, 3);
        EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, $"KillDenied{random}", false);
        EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, $"KillDenied{random}", true);
    }

    public void ClearKCHUD(GamePlayer player) => EffectManager.askEffectClearByID(KC_ID, player.TransportConnection);

    // CTF Related UI

    public void SendCTFHUD(CTFPlayer player, CTFTeam blueTeam, CTFTeam redTeam, List<CTFPlayer> players)
    {
        var bluePlayers = players.Where(k => k.Team.TeamID == blueTeam.TeamID);
        var redPlayers = players.Where(k => k.Team.TeamID == redTeam.TeamID);
        var index = player.Team.TeamID == blueTeam.TeamID ? 1 : 0;

        EffectManager.sendUIEffect(CTF_ID, CTF_KEY, player.GamePlayer.TransportConnection, true);
        EffectManager.sendUIEffectVisibility(CTF_KEY, player.GamePlayer.TransportConnection, true, "Timer", true);
        SendGamemodePopup(player.GamePlayer, EGameType.CTF);
        EffectManager.sendUIEffectVisibility(CTF_KEY, player.GamePlayer.TransportConnection, true, player.Team == blueTeam ? "BlueTeam" : "RedTeam", true);
        EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"RedNum{index}", redTeam.Score.ToString());
        EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"BlueNum{index}", blueTeam.Score.ToString());
        EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"RedFlag{index}", redTeam.HasFlag ? "" : "");
        EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"BlueFlag{index}", blueTeam.HasFlag ? "" : "");

        var blueFlagTaker = redPlayers.FirstOrDefault(k => k.IsCarryingFlag);
        var redFlagTaker = bluePlayers.FirstOrDefault(k => k.IsCarryingFlag);

        EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"RedTxt{index}", redTeam.HasFlag ? "Home" : redFlagTaker == null ? "Away" : redFlagTaker.GamePlayer.Player.CharacterName.ToUnrich());
        EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"BlueTxt{index}", blueTeam.HasFlag ? "Home" : redFlagTaker == null ? "Away" : redFlagTaker.GamePlayer.Player.CharacterName.ToUnrich());
    }

    public void SendCTFHUD(CTFTeam blueTeam, CTFTeam redTeam, List<CTFPlayer> players)
    {
        var bluePlayers = players.Where(k => k.Team.TeamID == blueTeam.TeamID);
        var redPlayers = players.Where(k => k.Team.TeamID == redTeam.TeamID);

        var blueFlagTaker = redPlayers.FirstOrDefault(k => k.IsCarryingFlag);
        var redFlagTaker = bluePlayers.FirstOrDefault(k => k.IsCarryingFlag);

        foreach (var player in players)
        {
            var index = player.Team.TeamID == blueTeam.TeamID ? 1 : 0;

            EffectManager.sendUIEffect(CTF_ID, CTF_KEY, player.GamePlayer.TransportConnection, true);
            EffectManager.sendUIEffect(27613, 27613, player.GamePlayer.TransportConnection, true, Plugin.Instance.Translate("CTF_Name").ToRich(), Plugin.Instance.Translate("CTF_Desc").ToRich());
            EffectManager.sendUIEffectVisibility(CTF_KEY, player.GamePlayer.TransportConnection, true, player.Team == blueTeam ? "BlueTeam" : "RedTeam", true);
            EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"RedNum{index}", redTeam.Score.ToString());
            EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"BlueNum{index}", blueTeam.Score.ToString());
            EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"RedFlag{index}", redTeam.HasFlag ? "" : "");
            EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"BlueFlag{index}", blueTeam.HasFlag ? "" : "");
            EffectManager.sendUIEffectVisibility(CTF_KEY, player.GamePlayer.TransportConnection, true, "Timer", true);

            EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"RedTxt{index}", redTeam.HasFlag ? "Home" : redFlagTaker == null ? "Away" : redFlagTaker.GamePlayer.Player.CharacterName.ToUnrich());
            EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"BlueTxt{index}", blueTeam.HasFlag ? "Home" : redFlagTaker == null ? "Away" : redFlagTaker.GamePlayer.Player.CharacterName.ToUnrich());
        }
    }

    public void UpdateCTFTimer(GamePlayer player, string text) => EffectManager.sendUIEffectText(CTF_KEY, player.TransportConnection, true, "TimerTxt", text);

    public void UpdateCTFHUD(List<CTFPlayer> players, CTFTeam changeTeam)
    {
        var team = (ETeam)changeTeam.TeamID == ETeam.BLUE ? "Blue" : "Red";
        var teamFlagTaker = players.FirstOrDefault(k => !k.IsDisposed && k.Team.TeamID != changeTeam.TeamID && k.IsCarryingFlag);

        foreach (var player in players)
        {
            if (player.IsDisposed)
                continue;
            
            var index = (ETeam)player.Team.TeamID == ETeam.BLUE ? 1 : 0;

            EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"{team}Num{index}", changeTeam.Score.ToString());
            EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"{team}Flag{index}", changeTeam.HasFlag ? "" : "");
            EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"{team}Txt{index}", changeTeam.HasFlag ? "Home" : teamFlagTaker == null ? "Away" : teamFlagTaker.GamePlayer.Player.CharacterName.ToUnrich());
        }
    }

    public void SetupCTFLeaderboard(List<CTFPlayer> players, CTFTeam wonTeam, CTFTeam blueTeam, CTFTeam redTeam, Game game)
    {
        foreach (var player in players)
            SetupCTFLeaderboard(player, players, wonTeam, blueTeam, redTeam, game);
    }

    public void SetupCTFLeaderboard(CTFPlayer player, List<CTFPlayer> players, CTFTeam wonTeam, CTFTeam blueTeam, CTFTeam redTeam, Game game)
    {
        var location = game.Location;
        var isPlaying = game.GamePhase != EGamePhase.ENDING;
        
        var bluePlayers = players.Where(k => k.Team.TeamID == (byte)ETeam.BLUE).ToList();
        var redPlayers = players.Where(k => k.Team.TeamID == (byte)ETeam.RED).ToList();

        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "MatchResult2", Plugin.Instance.Translate(wonTeam.TeamID == -1 ? isPlaying ? "Drawing_Text" : "Draw_Text" : player.Team == wonTeam ? isPlaying ? "Winning_Text" : "Victory_Text" : isPlaying ? "Losing_Text" : "Defeat_Text").ToRich());
        if (wonTeam.TeamID == -1 && !isPlaying)
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "MatchResult2", "DRAW!");
        
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "MapName2", location.LocationName.ToUpper());
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamNameR1", redTeam.Info.TeamName);
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamScoreR1", redTeam.Score.ToString());
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamNameB1", blueTeam.Info.TeamName);
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamScoreB1", blueTeam.Score.ToString());
        EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "GamemodeName2", (game.GameEvent == null ? "" : $"{game.GameEvent.EventName} ") + Plugin.Instance.Translate("CTF_Name_Full").ToRich());

        for (var i = 0; i <= 7; i++)
        {
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}B1", false);
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}R1", false);
        }

        for (var i = 0; i < bluePlayers.Count; i++)
        {
            var ply = bluePlayers[i];
            var isPlayer = player == ply;
            var data = ply.GamePlayer.Data;

            decimal kills = ply.Kills;
            decimal deaths = ply.Deaths;
            var objective = ply.FlagsCaptured + ply.FlagsSaved;

            var ratio = ply.Deaths == 0 ? string.Format("{0:n}", kills) : string.Format("{0:n}", Math.Round(kills / deaths, 2));

            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}B1", true);
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"NameTxt{i}B1", (data.HasPrime ? PRIME_SYMBOL : "") + data.SteamName.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KillsTxt{i}B1", ply.Kills.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"DeathsTxt{i}B1", ply.Deaths.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KDRTxt{i}B1", ratio.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"ScoreTxt{i}B1", ply.Score.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlTxt{i}B1", data.Level.ToColor(isPlayer));
            EffectManager.sendUIEffectImageURL(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlIcon{i}B1", Plugin.Instance.DB.Levels.TryGetValue(data.Level, out var level) ? level.IconLinkSmall : "");
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"AssistsTxt{i}B1", ply.Assists.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"ObjectiveTxt{i}B1", objective.ToColor(isPlayer));
        }

        for (var i = 0; i < redPlayers.Count; i++)
        {
            var ply = redPlayers[i];
            var isPlayer = player == ply;
            var data = ply.GamePlayer.Data;

            decimal kills = ply.Kills;
            decimal deaths = ply.Deaths;
            var objective = ply.FlagsSaved + ply.FlagsCaptured;

            var ratio = ply.Deaths == 0 ? string.Format("{0:n}", kills) : string.Format("{0:n}", Math.Round(kills / deaths, 2));

            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}R1", true);
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"NameTxt{i}R1", (data.HasPrime ? PRIME_SYMBOL : "") + data.SteamName.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KillsTxt{i}R1", ply.Kills.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"DeathsTxt{i}R1", ply.Deaths.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KDRTxt{i}R1", ratio.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"ScoreTxt{i}R1", ply.Score.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlTxt{i}R1", data.Level.ToColor(isPlayer));
            EffectManager.sendUIEffectImageURL(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlIcon{i}R1", Plugin.Instance.DB.Levels.TryGetValue(data.Level, out var level) ? level.IconLinkSmall : "");
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"AssistsTxt{i}R1", ply.Assists.ToColor(isPlayer));
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"ObjectiveTxt{i}R1", objective.ToColor(isPlayer));
        }
    }

    public void ShowCTFLeaderboard(GamePlayer player)
    {
        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Victory", false);
        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Defeat", false);
        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Draw", false);
        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scores", false);

        EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scoreboard2", true);
    }

    public void HideCTFLeaderboard(GamePlayer player) => EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scoreboard2", false);

    public void SendCTFFlagStates(CTFTeam team, ETeam flag, List<CTFPlayer> players, EFlagState state)
    {
        foreach (var player in players.Where(player => !player.GamePlayer.IsLoading))
        {
            EffectManager.sendUIEffect(FLAG_POPUP_UI, FLAG_POPUP_KEY, player.GamePlayer.TransportConnection, true);
            EffectManager.sendUIEffectVisibility(FLAG_POPUP_KEY, player.GamePlayer.TransportConnection, true, $"FLAG {flag.ToUIName()} Toggler", true);
            EffectManager.sendUIEffectText(FLAG_POPUP_KEY, player.GamePlayer.TransportConnection, true, "FlagTxt", Plugin.Instance.Translate($"CTF_{(player.Team.TeamID == team.TeamID ? "Team" : "Enemy")}_{state.ToUIName()}_Flag").ToRich());
        }
    }

    public void SendFlagSavedSound(GamePlayer player)
    {
        EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "FlagSaved0", false);
        EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "FlagSaved0", true);
    }

    public void SendFlagCapturedSound(List<GamePlayer> players)
    {
        foreach (var player in players)
        {
            EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "FlagSaved0", false);
            EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "FlagSaved0", true);
        }
    }

    public void ClearCTFHUD(GamePlayer player) => EffectManager.askEffectClearByID(CTF_ID, player.TransportConnection);

    // EVENTS

    public void OnUIUpdated(CSteamID steamID, EUIPage page, LoadoutGun gun = null)
    {
        if (!UIHandlersLookup.TryGetValue(steamID, out var handler))
            return;

        switch (page)
        {
            case EUIPage.PISTOL:
                handler.BuildPistolPages();
                handler.BuildAttachmentPages(gun);
                return;
            case EUIPage.SMG:
                handler.BuildSMGPages();
                handler.BuildAttachmentPages(gun);
                return;
            case EUIPage.LMG:
                handler.BuildLMGPages();
                handler.BuildAttachmentPages(gun);
                return;
            case EUIPage.SHOTGUN:
                handler.BuildShotgunPages();
                handler.BuildAttachmentPages(gun);
                return;
            case EUIPage.AR:
                handler.BuildARPages();
                handler.BuildAttachmentPages(gun);
                return;
            case EUIPage.SNIPER:
                handler.BuildSniperPages();
                handler.BuildAttachmentPages(gun);
                return;
            case EUIPage.CARBINE:
                handler.BuildCarbinePages();
                handler.BuildAttachmentPages(gun);
                return;
            case EUIPage.GUN_CHARM:
                handler.BuildGunCharmPages();
                return;
            case EUIPage.GUN_SKIN:
                handler.BuildGunSkinPages();
                handler.BuildUnboxingInventoryPages();
                return;
            case EUIPage.KNIFE:
                handler.BuildKnifePages();
                handler.BuildUnboxingInventoryPages();
                return;
            case EUIPage.TACTICAL:
                handler.BuildTacticalPages();
                return;
            case EUIPage.LETHAL:
                handler.BuildLethalPages();
                return;
            case EUIPage.CARD:
                handler.BuildCardPages();
                return;
            case EUIPage.GLOVE:
                handler.BuildGlovePages();
                handler.BuildUnboxingInventoryPages();
                return;
            case EUIPage.KILLSTREAK:
                handler.BuildKillstreakPages();
                return;
            case EUIPage.DEATHSTREAK:
                handler.BuildDeathstreakPages();
                return;
            case EUIPage.ABILITY:
                handler.BuildAbilityPages();
                return;
            case EUIPage.ACHIEVEMENT:
                handler.BuildAchievementPages();
                return;
            case EUIPage.CASE:
                handler.BuildUnboxingCasesPages();
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(page), "EUIPage is not as expected");
        }
    }

    public void OnCurrencyUpdated(CSteamID steamID, ECurrency currency)
    {
        if (UIHandlersLookup.TryGetValue(steamID, out var handler))
            handler.OnCurrencyUpdated(currency);
    }

    public void OnGameUpdated()
    {
        foreach (var handler in UIHandlers.Where(handler => handler.MainPage == EMainPage.PLAY && handler.PlayPage == EPlayPage.GAMES).ToList())
            handler.ShowGames();
    }

    public void OnGameCountUpdated(Game game)
    {
        foreach (var handler in UIHandlers.Where(handler => handler.MainPage == EMainPage.PLAY && handler.PlayPage == EPlayPage.GAMES).ToList())
            handler.UpdateGamePlayerCount(game);
    }

    public void OnServersUpdated()
    {
        foreach (var handler in UIHandlers.Where(handler => handler.MainPage == EMainPage.PLAY && handler.PlayPage == EPlayPage.SERVERS).ToList())
            handler.ShowServers();
    }

    public void OnAchievementsUpdated(CSteamID steamID)
    {
        if (!UIHandlersLookup.TryGetValue(steamID, out var handler))
            return;

        if (handler.MainPage != EMainPage.ACHIEVEMENTS)
            return;

        handler.ReloadAchievementSubPage();
        handler.ReloadSelectedAchievement();
        handler.CheckAchievements = true;
    }

    public void OnBattlepassUpdated(CSteamID steamID)
    {
        if (!UIHandlersLookup.TryGetValue(steamID, out var handler))
            return;

        if (handler.MainPage != EMainPage.BATTLEPASS)
            return;

        handler.CheckBattlepass = true;
    }

    private void OnButtonClicked(Player player, string buttonName)
    {
        var ply = UnturnedPlayer.FromPlayer(player);
        var gPly = Plugin.Instance.Game.GetGamePlayer(ply);
        var isGame = gPly.CurrentGame != null;

        if (!UIHandlersLookup.TryGetValue(ply.CSteamID, out var handler))
        {
            Logging.Debug($"Error finding handler for {player.channel.owner.playerID.characterName}");
            return;
        }
        
        Logging.Debug($"{ply.CharacterName} pressed button {buttonName}");
        switch (buttonName)
        {
            case "SERVER Play BUTTON":
                handler.ShowPlayPage();
                handler.StopScrollableImages();
                return;
            case "SERVER Play Back BUTTON":
                handler.ReloadMainMenu();
                return;
            case "SERVER Loadout BUTTON":
                handler.ShowLoadouts();
                handler.StopScrollableImages();
                return;
            case "SERVER Loadout Back BUTTON":
                handler.ReloadMainMenu();
                return;
            case "SERVER Loadout Close BUTTON":
                handler.ClearMidgameLoadouts();
                return;
            case "SERVER Leaderboards BUTTON":
                handler.ShowLeaderboards();
                handler.StopScrollableImages();
                return;
            case "SERVER Leaderboards Back BUTTON":
                handler.ReloadMainMenu();
                return;
            case "SERVER Quest BUTTON":
                handler.ShowQuests();
                return;
            case "SERVER Staff BUTTON":
                handler.ActivateStaffMode();
                return;
            case "Scrollable Image BUTTON":
                handler.SendScrollableImageLink();
                return;
            case "Discord BUTTON":
                player.sendBrowserRequest("Our Discord:", "discord.gg/Uetk2UgMVs");
                return;
            case "SERVER Achievements BUTTON":
                handler.ShowAchievements();
                handler.StopScrollableImages();
                return;
            case "SERVER Achievements Back BUTTON":
                handler.ReloadMainMenu();
                return;
            case "SERVER Store BUTTON":
                player.sendBrowserRequest("Our Store:", "https://store.unturnedblackout.com/");
                return;
            case "SERVER Unbox BUTTON":
                handler.ShowUnboxingPage(EUnboxingPage.BUY);
                handler.StopScrollableImages();
                return;
            case "SERVER Unbox Back BUTTON":
                handler.ReloadMainMenu();
                return;
            case "SERVER Battlepass BUTTON":
                handler.ShowBattlepass();
                handler.StopScrollableImages();
                return;
            case "SERVER Battlepass Back BUTTON":
                handler.ReloadMainMenu();
                return;
            case "SERVER Options BUTTON":
                handler.ShowOptions();
                handler.StopScrollableImages();
                return;
            case "SERVER Options Back BUTTON":
                handler.ReloadMainMenu();
                return;
            case "SERVER Exit BUTTON":
                Provider.kick(player.channel.owner.playerID.steamID, "You exited");
                return;
            case "SERVER Enough Currency Yes BUTTON":
                player.sendBrowserRequest("Buy currency here:", "https://store.unturnedblackout.com/category/currency");
                return;
            case "SERVER Play Games BUTTON":
                handler.ShowPlayPage(EPlayPage.GAMES);
                return;
            case "SERVER Play Servers BUTTON":
                handler.ShowPlayPage(EPlayPage.SERVERS);
                return;
            case "SERVER Play Join BUTTON":
                handler.ClickedJoinButton();
                return;
            case "SERVER Loadout Next BUTTON":
                if (!isGame)
                    handler.ForwardLoadoutPage();
                else
                    handler.ForwardMidgameLoadoutPage();

                return;
            case "SERVER Loadout Previous BUTTON":
                if (!isGame)
                    handler.BackwardLoadoutPage();
                else
                    handler.BackwardMidgameLoadoutPage();

                return;
            case "SERVER Loadout Equip BUTTON":
                if (!isGame)
                    handler.EquipLoadout();
                else
                    handler.EquipMidgameLoadout();

                return;
            case "SERVER Loadout Rename Confirm BUTTON":
                handler.RenameLoadout();
                return;
            case "Cancel Button":
                handler.ExitRenameLoadout();
                return;
            case "SERVER Loadout Card BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.CARD);
                return;
            case "SERVER Loadout Glove BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.GLOVE);
                return;
            case "SERVER Loadout Killstreak BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.KILLSTREAK);
                return;
            case "SERVER Loadout Deathstreak BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.DEATHSTREAK);
                return;
            case "SERVER Loadout Ability BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.ABILITY);
                return;
            case "SERVER Loadout Perk BUTTON 1":
                handler.ShowLoadoutSubPage(ELoadoutPage.PERK1);
                return;
            case "SERVER Loadout Perk BUTTON 2":
                handler.ShowLoadoutSubPage(ELoadoutPage.PERK2);
                return;
            case "SERVER Loadout Perk BUTTON 3":
                handler.ShowLoadoutSubPage(ELoadoutPage.PERK3);
                return;
            case "SERVER Loadout Perk BUTTON 4":
                handler.ShowLoadoutSubPage(ELoadoutPage.PERK4);
                return;
            case "SERVER Loadout Primary BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.PRIMARY);
                return;
            case "SERVER Loadout Primary Magazine BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.ATTACHMENT_PRIMARY_MAGAZINE);
                return;
            case "SERVER Loadout Primary Sights BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.ATTACHMENT_PRIMARY_SIGHTS);
                return;
            case "SERVER Loadout Primary Grip BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.ATTACHMENT_PRIMARY_GRIP);
                return;
            case "SERVER Loadout Primary Charm BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.ATTACHMENT_PRIMARY_CHARM);
                return;
            case "SERVER Loadout Primary Barrel BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.ATTACHMENT_PRIMARY_BARREL);
                return;
            case "SERVER Loadout Secondary BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.SECONDARY);
                return;
            case "SERVER Loadout Secondary Magazine BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.ATTACHMENT_SECONDARY_MAGAZINE);
                return;
            case "SERVER Loadout Secondary Sights BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.ATTACHMENT_SECONDARY_SIGHTS);
                return;
            case "SERVER Loadout Secondary Charm BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.ATTACHMENT_SECONDARY_CHARM);
                return;
            case "SERVER Loadout Secondary Barrel BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.ATTACHMENT_SECONDARY_BARREL);
                return;
            case "SERVER Loadout Knife BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.KNIFE);
                return;
            case "SERVER Loadout Lethal BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.LETHAL);
                return;
            case "SERVER Loadout Tactical BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.TACTICAL);
                return;
            case "SERVER Loadout Primary Skin BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.PRIMARY_SKIN);
                return;
            case "SERVER Loadout Secondary Skin BUTTON":
                handler.ShowLoadoutSubPage(ELoadoutPage.SECONDARY_SKIN);
                return;
            case "SERVER Item All BUTTON":
                handler.ShowLoadoutTab(ELoadoutTab.ALL);
                return;
            case "SERVER Item ARs BUTTON":
                handler.ShowLoadoutTab(ELoadoutTab.ASSAULT_RIFLES);
                return;
            case "SERVER Item Pistols BUTTON":
                handler.ShowLoadoutTab(ELoadoutTab.PISTOLS);
                return;
            case "SERVER Item Shotguns BUTTON":
                handler.ShowLoadoutTab(ELoadoutTab.SHOTGUNS);
                return;
            case "SERVER Item SMGs BUTTON":
                handler.ShowLoadoutTab(ELoadoutTab.SUBMACHINE_GUNS);
                return;
            case "SERVER Item LMGs BUTTON":
                handler.ShowLoadoutTab(ELoadoutTab.LIGHT_MACHINE_GUNS);
                return;
            case "SERVER Item SRs BUTTON":
                handler.ShowLoadoutTab(ELoadoutTab.SNIPER_RIFLES);
                return;
            case "SERVER Item Carbines BUTTON":
                handler.ShowLoadoutTab(ELoadoutTab.CARBINES);
                return;
            case "SERVER Item Back BUTTON":
                handler.ReloadLoadout();
                return;
            case "SERVER Item Buy BUTTON":
                if ((DateTime.UtcNow - handler.LastButtonClicked).TotalSeconds < 0.5)
                    return;

                handler.LastButtonClicked = DateTime.UtcNow;
                handler.BuySelectedItem();
                return;
            case "SERVER Item Unlock BUTTON":
                if ((DateTime.UtcNow - handler.LastButtonClicked).TotalSeconds < 0.5)
                    return;

                handler.LastButtonClicked = DateTime.UtcNow;
                handler.UnlockSelectedItem();
                return;
            case "SERVER Item Dequip BUTTON":
                handler.DequipSelectedItem();
                return;
            case "SERVER Item Equip BUTTON":
                handler.EquipSelectedItem();
                return;
            case "SERVER Item Next BUTTON":
                handler.ForwardLoadoutTab();
                return;
            case "SERVER Item Previous BUTTON":
                handler.BackwardLoadoutTab();
                return;
            case "SERVER Leaderboards Weekly BUTTON":
                handler.SelectLeaderboardPage(ELeaderboardPage.WEEKLY);
                return;
            case "SERVER Leaderboards All BUTTON":
                handler.SelectLeaderboardPage(ELeaderboardPage.ALL);
                return;
            case "SERVER Leaderboards Daily BUTTON":
                handler.SelectLeaderboardPage(ELeaderboardPage.DAILY);
                return;
            case "SERVER Leaderboards Seasonal BUTTON":
                handler.SelectLeaderboardPage(ELeaderboardPage.SEASONAL);
                return;
            case "SERVER Leaderboards Kill BUTTON":
                handler.SelectLeaderboardTab(ELeaderboardTab.KILL);
                return;
            case "SERVER Leaderboards Level BUTTON":
                handler.SelectLeaderboardTab(ELeaderboardTab.LEVEL);
                return;
            case "SERVER Leaderboards Skins BUTTON":
                handler.SelectLeaderboardTab(ELeaderboardTab.SKINS);
                return;
            case "SERVER Leaderboards Next BUTTON":
                handler.ForwardLeaderboardPage();
                return;
            case "SERVER Leaderboards Next Fast BUTTON":
                handler.ForwardLeaderboardPageFast();
                return;
            case "SERVER Leaderboards Next End BUTTON":
                handler.ForwardLeaderboardPageEnd();
                return;
            case "SERVER Leaderboards Previous BUTTON":
                handler.BackwardLeaderboardPage();
                return;
            case "SERVER Leaderboards Previous Fast BUTTON":
                handler.BackwardLeaderboardPageFast();
                return;
            case "SERVER Leaderboards Previous End BUTTON":
                handler.BackwardLeaderboardPageEnd();
                return;
            case "SERVER Achievements Next BUTTON":
                handler.ForwardAchievementSubPage();
                return;
            case "SERVER Achievements Previous BUTTON":
                handler.BackwardAchievementSubPage();
                return;
            case "SERVER Achievements Claim BUTTON":
                if (handler.MainPage == EMainPage.ACHIEVEMENTS)
                    Plugin.Instance.Achievement.ClaimAchievementTier(ply.CSteamID, handler.SelectedAchievementID);

                return;
            case "SERVER Battlepass Tier Skip BUTTON":
                return;
            case "SERVER Battlepass Confirm BUTTON":
                handler.SkipBattlepassTier();
                return;
            case "SERVER Battlepass Claim BUTTON":
                handler.ClaimBattlepassTier();
                return;
            case "SERVER Battlepass Next BUTTON":
                handler.ForwardBattlepassPage();
                return;
            case "SERVER Battlepass Prev BUTTON":
                handler.BackwardBattlepassPage();
                return;
            case "SERVER Battlepass Buy Pass BUTTON":
                player.sendBrowserRequest("Buy premium battlepass here:", "https://store.unturnedblackout.com/category/battlepass");
                return;
            case "SERVER Unbox Cases BUTTON":
                handler.ShowUnboxingPage(EUnboxingPage.CASES);
                return;
            case "SERVER Unbox Cases Previous BUTTON":
                handler.BackwardUnboxingCasePage();
                return;
            case "SERVER Unbox Cases Next BUTTON":
                handler.ForwardUnboxingCasePage();
                return;
            case "SERVER Unbox BuyCrate BUTTON":
                handler.ShowUnboxingPage(EUnboxingPage.BUY);
                return;
            case "SERVER Unbox Buy Previous BUTTON":
                handler.BackwardUnboxingStorePage();
                return;
            case "SERVER Unbox Buy Next BUTTON":
                handler.ForwardUnboxingStorePage();
                return;
            case "SERVER Unbox Buy Preview BUTTON":
                handler.PreviewUnboxingStoreCase();
                return;
            case "SERVER Unbox Buy Coins BUTTON":
                handler.BuyUnboxingStoreCase(ECurrency.COIN);
                return;
            case "SERVER Unbox Buy Scrap BUTTON":
                handler.BuyUnboxingStoreCase(ECurrency.SCRAP);
                return;
            case "SERVER Unbox Confirm BUTTON":
                handler.ConfirmUnboxingStoreCase();
                return;
            case "SERVER Unbox Content Back BUTTON":
                if (handler.UnboxingPage != EUnboxingPage.OPEN)
                    return;

                handler.ShowUnboxingPage(EUnboxingPage.CASES);
                handler.CrateUnboxer.Stop();

                return;
            case "SERVER Unbox Content Unbox BUTTON":
                if (handler.UnboxingPage == EUnboxingPage.OPEN && !handler.IsUnboxing)
                    handler.CrateUnboxer = Plugin.Instance.StartCoroutine(handler.UnboxCase());
                else
                    Logging.Debug($"PLAYER TRYING TO UNBOX BUT DENIED AS IsUnboxing: {handler.IsUnboxing}, Page: {handler.UnboxingPage}");

                return;
            case "SERVER Unbox Inventory BUTTON":
                handler.ShowUnboxingInventoryPage();
                break;
            case "SERVER Inventory Next BUTTON":
                handler.ForwardUnboxingInventoryPage();
                break;
            case "SERVER Inventory Previous BUTTON":
                handler.BackwardUnboxingInventoryPage();
                break;
            case "SERVER Summary Close BUTTON":
                handler.MatchEndSummaryShower.Stop();

                return;
            case "SERVER Summary Skip BUTTON":
                handler.MatchEndSummaryShower.Stop();

                return;
            case "Music Toggle BUTTON":
                handler.MusicButtonPressed();
                return;
            case "Flag Toggle BUTTON":
                handler.FlagButtonPressed();
                return;
        }

        var numberRegexMatch = new Regex(@"([0-9]+)").Match(buttonName).Value;
        if (!int.TryParse(numberRegexMatch, out var selected))
            return;

        if (buttonName.EndsWith("JoinButton"))
            Plugin.Instance.Game.AddPlayerToGame(ply, selected);
        else if (buttonName.StartsWith("SERVER Item BUTTON") || buttonName.StartsWith("SERVER Item Grid BUTTON"))
            handler.SelectedItem(selected);
        else if (buttonName.StartsWith("SERVER Achievements Page"))
            handler.SelectedAchievementMainPage(selected);
        else if (buttonName.StartsWith("SERVER Achievements BUTTON"))
            handler.SelectedAchievement(selected);
        else if (buttonName.StartsWith("SERVER Loadout BUTTON"))
        {
            if (!isGame)
                handler.SelectedLoadout(selected);
            else
                handler.SelectedMidgameLoadout(selected);
        }
        else if (buttonName.StartsWith("SERVER Play BUTTON"))
            handler.SelectedPlayButton(selected);
        else if (buttonName.StartsWith("SERVER Battlepass"))
            handler.SelectedBattlepassTier(buttonName.Split(' ')[2] == "T", selected);
        else if (buttonName.StartsWith("SERVER Unbox Crate BUTTON"))
            handler.ShowUnboxingPage(EUnboxingPage.OPEN, selected);
        else if (buttonName.StartsWith("SERVER Unbox Buy BUTTON"))
            handler.SelectedUnboxingStoreCase(selected);
        else if (buttonName.StartsWith("Volume BUTTON"))
            handler.VolumeButtonPressed(selected);
        else if (buttonName.StartsWith("SERVER Scrollable Dot"))
            handler.ChangeScrollableImage(selected);
    }

    private void OnTextCommitted(Player player, string buttonName, string text)
    {
        if (!UIHandlersLookup.TryGetValue(player.channel.owner.playerID.steamID, out var handler))
        {
            Logging.Debug($"Error finding UI handler for player, returning");
            return;
        }

        Logging.Debug($"{player.channel.owner.playerID.characterName} committed text: {buttonName} ({text})");
        switch (buttonName)
        {
            case "SERVER Loadout Rename INPUTFIELD":
                handler.SendLoadoutName(text);
                return;
            case "SERVER Leaderboards Search INPUTFIELD":
                handler.SearchLeaderboardPlayer(text);
                return;
            case "SERVER Inventory Search INPUTFIELD":
                handler.SearchUnboxingInventoryPage(text);
                return;
            case "Tactical Hotkey INPUT":
                handler.SetHotkey(EHotkey.TACTICAL, text);
                return;
            case "Lethal Hotkey INPUT":
                handler.SetHotkey(EHotkey.LETHAL, text);
                return;
            case "Killstreak 1 Hotkey INPUT":
                handler.SetHotkey(EHotkey.KILLSTREAK_1, text);
                return;
            case "Killstreak 2 Hotkey INPUT":
                handler.SetHotkey(EHotkey.KILLSTREAK_2, text);
                return;
            case "Killstreak 3 Hotkey INPUT":
                handler.SetHotkey(EHotkey.KILLSTREAK_3, text);
                return;
            case "Ability Hotkey INPUT":
                handler.SetHotkey(EHotkey.ABILITY, text);
                return;
            case "SERVER Unbox Buy Amount INPUT":
                handler.SetUnboxingStoreBuyAmount(text);
                return;
        }
    }

    public void SendNotEnoughCurrencyModal(CSteamID steamID, ECurrency currency)
    {
        if (UIHandlersLookup.TryGetValue(steamID, out var handler))
            handler.SendNotEnoughCurrencyModal(currency);
    }
}