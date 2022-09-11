using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;
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

namespace UnturnedBlackout.Managers
{
    public class UIManager
    {
        public ConfigManager Config
        {
            get
            {
                return Plugin.Instance.Config;
            }
        }

        public Dictionary<ushort, FeedIcon> KillFeedIcons { get; set; }
        public List<UIHandler> UIHandlers { get; set; }
        public Dictionary<CSteamID, UIHandler> UIHandlersLookup { get; set; }

        public Dictionary<CSteamID, Coroutine> TipSender { get; set; }

        public const ushort WAITING_FOR_PLAYERS_ID = 27641;
        public const short WAITING_FOR_PLAYERS_KEY = 27641;

        public const ushort FFA_ID = 27620;
        public const short FFA_KEY = 27620;

        public const ushort TDM_ID = 27621;
        public const short TDM_KEY = 27621;

        public const ushort KC_ID = 27621;
        public const short KC_KEY = 27621;

        public const ushort CTF_ID = 27623;
        public const short CTF_KEY = 27623;

        public const short SOUNDS_KEY = 27634;

        public const ushort DEATH_ID = 27635;
        public const short DEATH_KEY = 27635;

        public const ushort KILLCARD_ID = 27644;
        public const short KILLCARD_KEY = 27644;

        public const ushort PRE_ENDING_UI_ID = 27636;
        public const short PRE_ENDING_UI_KEY = 27636;

        public const ushort LEVEL_UP_ID = 27638;
        public const short LEVEL_UP_KEY = 27638;

        public const ushort GUN_LEVEL_UP_ID = 27642;
        public const short GUN_LEVEL_UP_KEY = 27642;

        public const ushort ITEM_UNLOCK_ID = 27647;
        public const short ITEM_UNLOCK_KEY = 27647;

        public const ushort QUEST_COMPLETION_ID = 27648;
        public const short QUEST_COMPLETION_KEY = 27648;

        public const ushort BP_TIER_COMPLETION_ID = 27645;
        public const short BP_TIER_COMPLETION_KEY = 27645;

        public const ushort ACHIEVEMENT_COMPLETION_ID = 27646;
        public const short ACHIEVEMENT_COMPLETION_KEY = 27646;

        public const ushort LOADING_UI_ID = 27640;
        public const short LOADING_UI_KEY = 27640;

        public const ushort FLAG_POPUP_UI = 27900;
        public const short FLAG_POPUP_KEY = 27900;

        public const ushort GAMEMODE_POPUP_ID = 27610;
        public const short GAMEMODE_POPUP_KEY = 27610;

        public const ushort QUEST_PROGRESSION_ID = 27639;
        public const short QUEST_PROGRESSION_KEY = 27639;

        public const ushort VOICE_CHAT_ID = 27622;
        public const short VOICE_CHAT_KEY = 27622;

        public const int MAX_SPACES_TDM_SCORE = 100;

        public const string PRIME_SYMBOL = " ";

        public UIManager()
        {
            KillFeedIcons = Config.Killfeed.FileData.KillFeedIcons.ToDictionary(k => k.WeaponID);

            UIHandlers = new();
            UIHandlersLookup = new();
            TipSender = new();
            EffectManager.onEffectButtonClicked += OnButtonClicked;
            EffectManager.onEffectTextCommitted += OnTextCommitted;
        }

        public void RegisterUIHandler(UnturnedPlayer player)
        {
            UIHandlers.RemoveAll(k => k.SteamID == player.CSteamID);
            UIHandlersLookup.Remove(player.CSteamID);

            var handler = new UIHandler(player);
            UIHandlersLookup.Add(player.CSteamID, handler);
            UIHandlers.Add(handler);
        }

        public void UnregisterUIHandler(UnturnedPlayer player)
        {
            if (UIHandlersLookup.TryGetValue(player.CSteamID, out UIHandler handler))
            {
                handler.Destroy();

                UIHandlers.Remove(handler);
                UIHandlersLookup.Remove(player.CSteamID);
            }

            if (TipSender.TryGetValue(player.CSteamID, out Coroutine tipSender))
            {
                if (tipSender != null)
                {
                    Plugin.Instance.StopCoroutine(tipSender);
                }
            }
        }

        public void ShowMenuUI(UnturnedPlayer player, MatchEndSummary summary = null)
        {
            if (UIHandlersLookup.TryGetValue(player.CSteamID, out UIHandler handler))
            {
                handler.ShowUI(summary);
            }
        }

        public void HideMenuUI(UnturnedPlayer player)
        {
            if (UIHandlersLookup.TryGetValue(player.CSteamID, out UIHandler handler))
            {
                handler.HideUI();
            }
        }


        // ALL GAMES RELATED UI

        // WAITING FOR PLAYERS
        public void SendWaitingForPlayersUI(GamePlayer player, int playerCount, int waitingPlayers)
        {
            EffectManager.sendUIEffect(WAITING_FOR_PLAYERS_ID, WAITING_FOR_PLAYERS_KEY, player.TransportConnection, true, Plugin.Instance.Translate("Waiting_For_Players_Show", playerCount, waitingPlayers).ToRich());
        }

        public void UpdateWaitingForPlayersUI(GamePlayer player, int playerCount, int waitingPlayers)
        {
            EffectManager.sendUIEffectText(WAITING_FOR_PLAYERS_KEY, player.TransportConnection, true, "Waiting", Plugin.Instance.Translate("Waiting_For_Players_Show", playerCount, waitingPlayers).ToRich());
        }

        public void ClearWaitingForPlayersUI(GamePlayer player)
        {
            EffectManager.askEffectClearByID(WAITING_FOR_PLAYERS_ID, player.TransportConnection);
        }

        // GAME START COUNTDOWN

        public void ShowCountdownUI(GamePlayer player)
        {
            EffectManager.sendUIEffect(27633, 27633, player.TransportConnection, true);
            EffectManager.sendUIEffectVisibility(27633, player.TransportConnection, true, "StartCountdown", true);
        }

        public void SendCountdownSeconds(GamePlayer player, int seconds)
        {
            EffectManager.sendUIEffectText(27633, player.TransportConnection, true, "CountdownNum", seconds.ToString());
        }

        public void ClearCountdownUI(GamePlayer player)
        {
            EffectManager.askEffectClearByID(27633, player.TransportConnection);
        }

        // XP UI

        public void ShowXPUI(GamePlayer player, int xp, string xpGained)
        {
            EffectManager.sendUIEffect(27630, 27630, player.TransportConnection, true, $"+{xp} XP", xpGained);
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
            };
        }

        // ANIMATION

        public void SendAnimation(GamePlayer player, AnimationInfo animationInfo)
        {
            if (player.HasAnimationGoingOn)
            {
                player.PendingAnimations.Add(animationInfo);
                return;
            }

            if (player.AnimationChecker != null)
            {
                Plugin.Instance.StopCoroutine(player.AnimationChecker);
            }
            player.AnimationChecker = Plugin.Instance.StartCoroutine(player.CheckAnimation());

            switch (animationInfo.AnimationType)
            {
                case EAnimationType.LevelUp:
                    {
                        if (!Plugin.Instance.DB.Levels.TryGetValue((int)animationInfo.Info, out XPLevel level))
                        {
                            return;
                        }

                        EffectManager.sendUIEffect(LEVEL_UP_ID, LEVEL_UP_KEY, player.TransportConnection, true);
                        EffectManager.sendUIEffectImageURL(LEVEL_UP_KEY, player.TransportConnection, true, "LevelUpIcon", level.IconLinkLarge);
                        EffectManager.sendUIEffectText(LEVEL_UP_KEY, player.TransportConnection, true, "LevelUpDesc", " ");
                        EffectManager.sendUIEffectText(LEVEL_UP_KEY, player.TransportConnection, true, "LevelUpTxt", $"LEVEL {level.Level}");
                        break;
                    }
                case EAnimationType.GunLevelUp:
                    {
                        var gun = animationInfo.Info as AnimationItemUnlock;
                        EffectManager.sendUIEffect(GUN_LEVEL_UP_ID, GUN_LEVEL_UP_KEY, player.TransportConnection, true);
                        EffectManager.sendUIEffectImageURL(GUN_LEVEL_UP_KEY, player.TransportConnection, true, "LevelUpIcon", gun.ItemIcon);
                        EffectManager.sendUIEffectText(GUN_LEVEL_UP_KEY, player.TransportConnection, true, "LevelUpDesc", $"Level {gun.ItemType}");
                        EffectManager.sendUIEffectText(GUN_LEVEL_UP_KEY, player.TransportConnection, true, "LevelUpTxt", gun.ItemName);
                        break;
                    }
                case EAnimationType.QuestCompletion:
                    {
                        var quest = animationInfo.Info as Quest;
                        EffectManager.sendUIEffect(QUEST_COMPLETION_ID, QUEST_COMPLETION_KEY, player.TransportConnection, true);
                        EffectManager.sendUIEffectText(QUEST_COMPLETION_KEY, player.TransportConnection, true, "LevelUpDesc", quest.QuestTitle);
                        break;
                    }
                case EAnimationType.BattlepassTierCompletion:
                    {
                        var tier = animationInfo.Info as BattlepassTier;
                        EffectManager.sendUIEffect(BP_TIER_COMPLETION_ID, BP_TIER_COMPLETION_KEY, player.TransportConnection, true);
                        EffectManager.sendUIEffectText(BP_TIER_COMPLETION_KEY, player.TransportConnection, true, "LevelUpDesc", tier.TierID.ToString());
                        break;
                    }
                case EAnimationType.ItemUnlock:
                    {
                        var itemUnlock = animationInfo.Info as AnimationItemUnlock;
                        EffectManager.sendUIEffect(ITEM_UNLOCK_ID, ITEM_UNLOCK_KEY, player.TransportConnection, true);
                        EffectManager.sendUIEffectImageURL(ITEM_UNLOCK_KEY, player.TransportConnection, true, "LevelUpIcon", itemUnlock.ItemIcon);
                        EffectManager.sendUIEffectText(ITEM_UNLOCK_KEY, player.TransportConnection, true, "LevelUpTxt", "UNLOCKED");
                        EffectManager.sendUIEffectText(ITEM_UNLOCK_KEY, player.TransportConnection, true, "LevelUpDesc", itemUnlock.ItemName);
                        break;
                    }
                case EAnimationType.AchievementCompletion:
                    {
                        var achievement = animationInfo.Info as AchievementTier;
                        EffectManager.sendUIEffect(ACHIEVEMENT_COMPLETION_ID, ACHIEVEMENT_COMPLETION_KEY, player.TransportConnection, true);
                        EffectManager.sendUIEffectImageURL(ACHIEVEMENT_COMPLETION_KEY, player.TransportConnection, true, "LevelUpIcon", achievement.TierPrevLarge);
                        EffectManager.sendUIEffectText(ACHIEVEMENT_COMPLETION_KEY, player.TransportConnection, true, "LevelUpTxt", achievement.TierTitle);
                        EffectManager.sendUIEffectText(ACHIEVEMENT_COMPLETION_KEY, player.TransportConnection, true, "LevelUpDesc", achievement.TierDesc);
                        break;
                    }
                default:
                    break;
            }
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
            {
                feedText += feed.KillMessage + "\n";
            }
            if (!string.IsNullOrEmpty(feedText))
            {
                feedText = $"<size={Config.Base.FileData.KillFeedFont}>{feedText}</size>";
            }
            foreach (var player in players)
            {
                var playerName = player.Player.CharacterName;
                var updatedText = new Regex($@"<color=[^>]*>{playerName.Replace("[", @"\[").Replace("]", @"\]").Replace("(", @"\(").Replace(")", @"\)").Replace("|", @"\|")}<\/color>", RegexOptions.IgnoreCase).Replace(feedText, $"<color={Config.Base.FileData.PlayerColorHexCode}>{playerName}</color>");
                EffectManager.sendUIEffectText(key, player.TransportConnection, true, "Killfeed", updatedText);
            }
        }

        // VOICE CHAT

        public void SendVoiceChatUI(GamePlayer player)
        {
            EffectManager.sendUIEffect(VOICE_CHAT_ID, VOICE_CHAT_KEY, player.TransportConnection, true);
        }

        public void UpdateVoiceChatUI(List<GamePlayer> players, List<GamePlayer> playersTalking)
        {
            var voiceChatText = "";
            foreach (var talking in playersTalking)
            {
                voiceChatText += $" {talking.Player.CharacterName.ToUnrich()} \n";
            }
            if (!string.IsNullOrEmpty(voiceChatText))
            {
                voiceChatText = $"<size={Config.Base.FileData.VoiceChatFont}>{voiceChatText}</size>";
            }
            foreach (var player in players)
            {
                EffectManager.sendUIEffectText(VOICE_CHAT_KEY, player.TransportConnection, true, "VoiceChatUsers", voiceChatText);
            }
        }

        public void ClearVoiceChatUI(GamePlayer player)
        {
            EffectManager.askEffectClearByID(VOICE_CHAT_ID, player.TransportConnection);
        }

        // DEATH UI

        public void SendDeathUI(GamePlayer victim, GamePlayer killer, PlayerData killerData)
        {
            victim.Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);

            EffectManager.sendUIEffect(DEATH_ID, DEATH_KEY, victim.TransportConnection, true);
            EffectManager.sendUIEffectImageURL(DEATH_KEY, victim.TransportConnection, true, "EnemyIcon", killerData.AvatarLink);
            EffectManager.sendUIEffectImageURL(DEATH_KEY, victim.TransportConnection, true, "EnemyXPIcon", Plugin.Instance.DB.Levels.TryGetValue(killerData.Level, out XPLevel level) ? level.IconLinkMedium : "");
            EffectManager.sendUIEffectText(DEATH_KEY, victim.TransportConnection, true, "EnemyName", (killerData.HasPrime ? PRIME_SYMBOL : "") + killerData.SteamName.ToUpper());
            EffectManager.sendUIEffectText(DEATH_KEY, victim.TransportConnection, true, "EnemyXPNum", killerData.Level.ToString());
            EffectManager.sendUIEffectImageURL(DEATH_KEY, victim.TransportConnection, true, "DeathBanner", killer.ActiveLoadout?.Card?.Card?.CardLink ?? "https://cdn.discordapp.com/attachments/899796442649092119/927985217975758898/Senosan-85382-HG-Dark-grey-600x600.png");
        }

        public void UpdateRespawnTimer(GamePlayer player, string timer)
        {
            EffectManager.sendUIEffectText(DEATH_KEY, player.TransportConnection, true, "RespawnTime", timer);
        }

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
            EffectManager.sendUIEffectImageURL(KILLCARD_KEY, killer.TransportConnection, true, "EnemyIcon", victimData.AvatarLink);
            EffectManager.sendUIEffectImageURL(KILLCARD_KEY, killer.TransportConnection, true, "EnemyXPIcon", Plugin.Instance.DB.Levels.TryGetValue(victimData.Level, out XPLevel level) ? level.IconLinkMedium : "");
            EffectManager.sendUIEffectText(KILLCARD_KEY, killer.TransportConnection, true, "EnemyName", (victimData.HasPrime ? PRIME_SYMBOL : "") + victimData.SteamName.ToUpper());
            EffectManager.sendUIEffectText(KILLCARD_KEY, killer.TransportConnection, true, "EnemyXPNum", victimData.Level.ToString());
            EffectManager.sendUIEffectImageURL(KILLCARD_KEY, killer.TransportConnection, true, "DeathBanner", victim.ActiveLoadout?.Card?.Card?.CardLink ?? "https://cdn.discordapp.com/attachments/899796442649092119/927985217975758898/Senosan-85382-HG-Dark-grey-600x600.png");
        }

        public void RemoveKillCard(GamePlayer player)
        {
            EffectManager.askEffectClearByID(KILLCARD_ID, player.TransportConnection);
        }

        // LOADING UI

        public void SendLoadingUI(UnturnedPlayer player, bool isMatch, EGameType gameMode, ArenaLocation location, string loadingText = "LOADING...")
        {
            var transportConnection = player.Player.channel.owner.transportConnection;

            player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);

            EffectManager.sendUIEffect(LOADING_UI_ID, LOADING_UI_KEY, transportConnection, true);
            EffectManager.sendUIEffectVisibility(LOADING_UI_KEY, transportConnection, true, "Scene Loading Match Toggler", isMatch);
            EffectManager.sendUIEffectVisibility(LOADING_UI_KEY, transportConnection, true, "Scene Loading Menu Toggler", !isMatch);
            EffectManager.sendUIEffectText(LOADING_UI_KEY, transportConnection, true, "LOADING Bar TEXT", loadingText);

            if (isMatch)
            {
                var gameModeOption = Config.Gamemode.FileData.GamemodeOptions.FirstOrDefault(k => k.GameType == gameMode);
                if (gameModeOption == null)
                {
                    return;
                }

                EffectManager.sendUIEffectImageURL(LOADING_UI_KEY, transportConnection, true, "LOADING Map Image", location.ImageLink);
                EffectManager.sendUIEffectImageURL(LOADING_UI_KEY, transportConnection, true, $"LOADING Gamemode Icon", gameModeOption.GamemodeIcon);
                EffectManager.sendUIEffectText(LOADING_UI_KEY, transportConnection, true, "LOADING Map TEXT", location.LocationName);
                EffectManager.sendUIEffectText(LOADING_UI_KEY, transportConnection, true, "LOADING Gamemode TEXT", Plugin.Instance.Translate($"{gameMode}_Name_Full").ToRich());
                EffectManager.sendUIEffectText(LOADING_UI_KEY, transportConnection, true, "LOADING Bar Fill", "　");
            }

            if (TipSender.TryGetValue(player.CSteamID, out Coroutine tipSender))
            {
                if (tipSender != null)
                {
                    Plugin.Instance.StopCoroutine(tipSender);
                }
                TipSender.Remove(player.CSteamID);
            }

            TipSender.Add(player.CSteamID, Plugin.Instance.StartCoroutine(SendTip(player)));
        }

        public void UpdateLoadingBar(UnturnedPlayer player, string bar, string loadingText = "LOADING...")
        {
            var transportConnection = player.Player.channel.owner.transportConnection;
            EffectManager.sendUIEffectText(LOADING_UI_KEY, transportConnection, true, "LOADING Bar TEXT", loadingText);
            EffectManager.sendUIEffectText(LOADING_UI_KEY, transportConnection, true, "LOADING Bar Fill", bar);
        }

        public void UpdateLoadingTip(UnturnedPlayer player, string tip)
        {
            EffectManager.sendUIEffectText(LOADING_UI_KEY, player.Player.channel.owner.transportConnection, true, "LOADING Tip Description TEXT", tip);
        }

        public void ClearLoadingUI(UnturnedPlayer player)
        {
            if (TipSender.TryGetValue(player.CSteamID, out Coroutine tipSender))
            {
                if (tipSender != null)
                {
                    Plugin.Instance.StopCoroutine(tipSender);
                }
                TipSender.Remove(player.CSteamID);
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

        public void SendPreEndingUI(GamePlayer player)
        {
            EffectManager.sendUIEffect(PRE_ENDING_UI_ID, PRE_ENDING_UI_KEY, player.TransportConnection, true);
        }

        public void SetupPreEndingUI(GamePlayer player, EGameType gameMode, bool hasWon, int blueScore, int redScore, string blueName, string redName)
        {
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, hasWon ? "Victory" : "Defeat", true);
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.TransportConnection, true, hasWon ? "VictoryTxt" : "DefeatTxt", Plugin.Instance.Translate(hasWon ? $"{gameMode}_Victory_Desc" : $"{gameMode}_Defeat_Desc").ToRich());

            if (gameMode != EGameType.FFA)
            {
                EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scores", true);
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.TransportConnection, true, "BlueSideScore", blueScore.ToString());
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.TransportConnection, true, "BlueSideName", blueName);
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.TransportConnection, true, "RedSideScore", redScore.ToString());
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.TransportConnection, true, "RedSideName", redName);
            }
        }

        public void ClearPreEndingUI(GamePlayer player)
        {
            EffectManager.askEffectClearByID(PRE_ENDING_UI_ID, player.TransportConnection);
        }

        // MIDGAME LOADOUT UI

        public void ShowMidgameLoadoutUI(GamePlayer player)
        {
            if (UIHandlersLookup.TryGetValue(player.SteamID, out UIHandler handler))
            {
                handler.ShowMidgameLoadouts();
            }
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
            {
                return;
            }
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
                EffectManager.sendUIEffectText(QUEST_PROGRESSION_KEY, player.TransportConnection, true, $"QUEST Progress {i} Fill", quest.Amount == 0 ? " " : new string(' ', Math.Min(267, quest.Amount * 267 / quest.Quest.TargetAmount)));
            }
        }

        // FFA RELATED UI

        public void SendFFAHUD(GamePlayer player)
        {
            EffectManager.sendUIEffect(FFA_ID, FFA_KEY, player.TransportConnection, true);

            EffectManager.sendUIEffectVisibility(FFA_KEY, player.TransportConnection, true, "ScoreCounter", true);
            SendGamemodePopup(player, EGameType.FFA);
            EffectManager.sendUIEffectVisibility(FFA_KEY, player.TransportConnection, true, "Timer", true);
        }

        public void UpdateFFATimer(GamePlayer player, string text)
        {
            EffectManager.sendUIEffectText(FFA_KEY, player.TransportConnection, true, "TimerTxt", text);
        }

        public void UpdateFFATopUI(FFAPlayer player, List<FFAPlayer> Players)
        {
            if (Players.Count == 0)
            {
                return;
            }

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

            EffectManager.sendUIEffectText(FFA_KEY, player.GamePlayer.TransportConnection, true, "1stPlacementName", firstPlayer.GamePlayer.Player.CharacterName.ToUnrich());
            EffectManager.sendUIEffectText(FFA_KEY, player.GamePlayer.TransportConnection, true, "1stPlacementScore", firstPlayer.Kills.ToString());

            EffectManager.sendUIEffectText(FFA_KEY, player.GamePlayer.TransportConnection, true, "2ndPlacementPlace", secondPlayer != null ? Utility.GetOrdinal(Players.IndexOf(secondPlayer) + 1) : "0");
            EffectManager.sendUIEffectText(FFA_KEY, player.GamePlayer.TransportConnection, true, "2ndPlacementName", secondPlayer != null ? secondPlayer.GamePlayer.Player.CharacterName.ToUnrich() : "NONE");
            EffectManager.sendUIEffectText(FFA_KEY, player.GamePlayer.TransportConnection, true, "2ndPlacementScore", secondPlayer != null ? secondPlayer.Kills.ToString() : "0");
        }

        public void SetupFFALeaderboard(List<FFAPlayer> players, ArenaLocation location, bool isPlaying, bool isHardcore)
        {
            foreach (var player in players)
            {
                SetupFFALeaderboard(player, players, location, isPlaying, isHardcore);
            }
        }

        public void SetupFFALeaderboard(FFAPlayer ply, List<FFAPlayer> players, ArenaLocation location, bool isPlaying, bool isHardcore)
        {
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, "MatchResult1", Plugin.Instance.Translate(players.IndexOf(ply) == 0 ? (isPlaying ? "Winning_Text" : "Victory_Text") : (isPlaying ? "Losing_Text" : "Defeat_Text")).ToRich());
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, "MapName1", location.LocationName.ToUpper());
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, "GamemodeName1", (isHardcore ? "Hardcore " : "") + Plugin.Instance.Translate("FFA_Name_Full").ToRich());

            for (int i = 0; i <= 19; i++)
            {
                EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, $"PlayerStats{i}", false);
            }

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                bool isPlayer = player == ply;
                var data = player.GamePlayer.Data;

                var kills = (decimal)player.Kills;
                var deaths = (decimal)player.Deaths;

                var ratio = player.Deaths == 0 ? String.Format("{0:n}", kills) : String.Format("{0:n}", Math.Round(kills / deaths, 2));

                EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, $"PlayerStats{i}", true);
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, $"NameTxt{i}", (data.HasPrime ? PRIME_SYMBOL : "") + data.SteamName.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, $"KillsTxt{i}", player.Kills.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, $"DeathsTxt{i}", player.Deaths.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, $"KDRTxt{i}", ratio.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, $"ScoreTxt{i}", player.Score.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, $"LvlTxt{i}", data.Level.ToColor(isPlayer));
                EffectManager.sendUIEffectImageURL(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, $"LvlIcon{i}", Plugin.Instance.DB.Levels.TryGetValue(data.Level, out XPLevel level) ? level.IconLinkSmall : "");
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, ply.GamePlayer.TransportConnection, true, $"AssistsTxt{i}", player.Assists.ToColor(isPlayer));
            }
        }

        public void ShowFFALeaderboard(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Victory", false);
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Defeat", false);
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scores", false);

            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scoreboard1", true);
        }

        public void HideFFALeaderboard(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scoreboard1", false);
        }

        public void ClearFFAHUD(GamePlayer player)
        {
            EffectManager.askEffectClearByID(FFA_ID, player.TransportConnection);
        }


        // TDM Related UI

        public void SendTDMHUD(TDMPlayer player, TDMTeam blueTeam, TDMTeam redTeam)
        {
            EffectManager.sendUIEffect(TDM_ID, TDM_KEY, player.GamePlayer.TransportConnection, true);
            EffectManager.sendUIEffectVisibility(TDM_KEY, player.GamePlayer.TransportConnection, true, player.Team.TeamID == (byte)ETeam.Blue ? "BlueTeam" : "RedTeam", true);
            SendGamemodePopup(player.GamePlayer, EGameType.TDM);
            EffectManager.sendUIEffectVisibility(TDM_KEY, player.GamePlayer.TransportConnection, true, "Timer", true);
            EffectManager.sendUIEffectVisibility(TDM_KEY, player.GamePlayer.TransportConnection, true, "Team", true);
            EffectManager.sendUIEffectText(TDM_KEY, player.GamePlayer.TransportConnection, true, "TeamName", $"<color={player.Team.Info.TeamColorHexCode}>{player.Team.Info.TeamName}</color>");

            int index = player.Team.TeamID == (byte)ETeam.Blue ? 1 : 0;
            int blueSpaces = blueTeam.Score * MAX_SPACES_TDM_SCORE / Config.TDM.FileData.ScoreLimit;
            int redSpaces = redTeam.Score * MAX_SPACES_TDM_SCORE / Config.TDM.FileData.ScoreLimit;
            EffectManager.sendUIEffectText(TDM_KEY, player.GamePlayer.TransportConnection, true, $"RedNum{index}", redTeam.Score.ToString());
            EffectManager.sendUIEffectText(TDM_KEY, player.GamePlayer.TransportConnection, true, $"RedBarFill{index}", redSpaces == 0 ? " " : new string(' ', redSpaces));

            EffectManager.sendUIEffectText(TDM_KEY, player.GamePlayer.TransportConnection, true, $"BlueNum{index}", blueTeam.Score.ToString());
            EffectManager.sendUIEffectText(TDM_KEY, player.GamePlayer.TransportConnection, true, $"BlueBarFill{index}", blueSpaces == 0 ? " " : new string(' ', blueSpaces));
        }

        public void UpdateTDMTimer(GamePlayer player, string text)
        {
            EffectManager.sendUIEffectText(TDM_KEY, player.TransportConnection, true, "TimerTxt", text);
        }

        public void UpdateTDMScore(TDMPlayer player, TDMTeam changeTeam)
        {
            int index = player.Team.TeamID == (byte)ETeam.Blue ? 1 : 0;
            var team = (ETeam)changeTeam.TeamID;
            int spaces = changeTeam.Score * MAX_SPACES_TDM_SCORE / Config.TDM.FileData.ScoreLimit;

            EffectManager.sendUIEffectText(TDM_KEY, player.GamePlayer.TransportConnection, true, $"{team}Num{index}", changeTeam.Score.ToString());
            EffectManager.sendUIEffectText(TDM_KEY, player.GamePlayer.TransportConnection, true, $"{team}BarFill{index}", spaces == 0 ? " " : new string(' ', spaces));
        }

        public void SetupTDMLeaderboard(List<TDMPlayer> players, ArenaLocation location, TDMTeam wonTeam, TDMTeam blueTeam, TDMTeam redTeam, bool isPlaying, bool isHardcore)
        {
            foreach (var player in players)
            {
                SetupTDMLeaderboard(player, players, location, wonTeam, blueTeam, redTeam, isPlaying, isHardcore);
            }
        }

        public void SetupTDMLeaderboard(TDMPlayer player, List<TDMPlayer> players, ArenaLocation location, TDMTeam wonTeam, TDMTeam blueTeam, TDMTeam redTeam, bool isPlaying, bool isHardcore)
        {
            var bluePlayers = players.Where(k => k.Team.TeamID == (byte)ETeam.Blue).ToList();
            var redPlayers = players.Where(k => k.Team.TeamID == (byte)ETeam.Red).ToList();

            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "MatchResult0", Plugin.Instance.Translate(player.Team == wonTeam ? (isPlaying ? "Winning_Text" : "Victory_Text") : (isPlaying ? "Losing_Text" : "Defeat_Text")).ToRich());
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "MapName0", location.LocationName.ToUpper());
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamNameR0", redTeam.Info.TeamName);
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamScoreR0", redTeam.Score.ToString());
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamNameB0", blueTeam.Info.TeamName);
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamScoreB0", blueTeam.Score.ToString());
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "GamemodeName1", (isHardcore ? "Hardcore " : "") + Plugin.Instance.Translate("TDM_Name_Full").ToRich());

            for (int i = 0; i <= 9; i++)
            {
                EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}B0", false);
                EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}R0", false);
            }

            for (int i = 0; i < bluePlayers.Count; i++)
            {
                var ply = bluePlayers[i];
                bool isPlayer = player == ply;
                var data = ply.GamePlayer.Data;

                var kills = (decimal)ply.Kills;
                var deaths = (decimal)ply.Deaths;

                var ratio = ply.Deaths == 0 ? String.Format("{0:n}", kills) : String.Format("{0:n}", Math.Round(kills / deaths, 2));

                EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}B0", true);
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"NameTxt{i}B0", (data.HasPrime ? PRIME_SYMBOL : "") + data.SteamName.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KillsTxt{i}B0", ply.Kills.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"DeathsTxt{i}B0", ply.Deaths.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KDRTxt{i}B0", ratio.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"ScoreTxt{i}B0", ply.Score.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlTxt{i}B0", data.Level.ToColor(isPlayer));
                EffectManager.sendUIEffectImageURL(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlIcon{i}B0", Plugin.Instance.DB.Levels.TryGetValue(data.Level, out XPLevel level) ? level.IconLinkSmall : "");
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"AssistsTxt{i}B0", ply.Assists.ToColor(isPlayer));
            }

            for (int i = 0; i < redPlayers.Count; i++)
            {
                var ply = redPlayers[i];
                bool isPlayer = player == ply;
                var data = ply.GamePlayer.Data;

                var kills = (decimal)ply.Kills;
                var deaths = (decimal)ply.Deaths;

                var ratio = ply.Deaths == 0 ? String.Format("{0:n}", kills) : String.Format("{0:n}", Math.Round(kills / deaths, 2));

                EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}R0", true);
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"NameTxt{i}R0", (data.HasPrime ? PRIME_SYMBOL : "") + data.SteamName.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KillsTxt{i}R0", ply.Kills.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"DeathsTxt{i}R0", ply.Deaths.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KDRTxt{i}R0", ratio.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"ScoreTxt{i}R0", ply.Score.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlTxt{i}R0", data.Level.ToColor(isPlayer));
                EffectManager.sendUIEffectImageURL(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlIcon{i}R0", Plugin.Instance.DB.Levels.TryGetValue(data.Level, out XPLevel level) ? level.IconLinkSmall : "");
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"AssistsTxt{i}R0", ply.Assists.ToColor(isPlayer));
            }
        }

        public void ShowTDMLeaderboard(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Victory", false);
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Defeat", false);
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scores", false);

            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scoreboard0", true);
        }

        public void HideTDMLeaderboard(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scoreboard0", false);
        }

        public void ClearTDMHUD(GamePlayer player)
        {
            EffectManager.askEffectClearByID(TDM_ID, player.TransportConnection);
        }

        // KC Related UI

        public void SendKCHUD(KCPlayer player, KCTeam blueTeam, KCTeam redTeam)
        {
            EffectManager.sendUIEffect(KC_ID, KC_KEY, player.GamePlayer.TransportConnection, true);
            EffectManager.sendUIEffectVisibility(KC_KEY, player.GamePlayer.TransportConnection, true, player.Team.TeamID == (byte)ETeam.Blue ? "BlueTeam" : "RedTeam", true);
            SendGamemodePopup(player.GamePlayer, EGameType.KC);
            EffectManager.sendUIEffectVisibility(KC_KEY, player.GamePlayer.TransportConnection, true, "Timer", true);
            EffectManager.sendUIEffectVisibility(KC_KEY, player.GamePlayer.TransportConnection, true, "Team", true);
            EffectManager.sendUIEffectText(KC_KEY, player.GamePlayer.TransportConnection, true, "TeamName", $"<color={player.Team.Info.TeamColorHexCode}>{player.Team.Info.TeamName}</color>");

            int index = player.Team.TeamID == (byte)ETeam.Blue ? 1 : 0;
            int blueSpaces = blueTeam.Score * MAX_SPACES_TDM_SCORE / Config.TDM.FileData.ScoreLimit;
            int redSpaces = redTeam.Score * MAX_SPACES_TDM_SCORE / Config.TDM.FileData.ScoreLimit;
            EffectManager.sendUIEffectText(KC_KEY, player.GamePlayer.TransportConnection, true, $"RedNum{index}", redTeam.Score.ToString());
            EffectManager.sendUIEffectText(KC_KEY, player.GamePlayer.TransportConnection, true, $"RedBarFill{index}", redSpaces == 0 ? " " : new string(' ', redSpaces));

            EffectManager.sendUIEffectText(KC_KEY, player.GamePlayer.TransportConnection, true, $"BlueNum{index}", blueTeam.Score.ToString());
            EffectManager.sendUIEffectText(KC_KEY, player.GamePlayer.TransportConnection, true, $"BlueBarFill{index}", blueSpaces == 0 ? " " : new string(' ', blueSpaces));
        }

        public void UpdateKCTimer(GamePlayer player, string text)
        {
            EffectManager.sendUIEffectText(KC_KEY, player.TransportConnection, true, "TimerTxt", text);
        }

        public void UpdateKCScore(KCPlayer player, KCTeam changeTeam)
        {
            int index = player.Team.TeamID == (byte)ETeam.Blue ? 1 : 0;
            var team = (ETeam)changeTeam.TeamID;
            int spaces = changeTeam.Score * MAX_SPACES_TDM_SCORE / Config.TDM.FileData.ScoreLimit;

            EffectManager.sendUIEffectText(KC_KEY, player.GamePlayer.TransportConnection, true, $"{team}Num{index}", changeTeam.Score.ToString());
            EffectManager.sendUIEffectText(KC_KEY, player.GamePlayer.TransportConnection, true, $"{team}BarFill{index}", spaces == 0 ? " " : new string(' ', spaces));
        }

        public void SetupKCLeaderboard(List<KCPlayer> players, ArenaLocation location, KCTeam wonTeam, KCTeam blueTeam, KCTeam redTeam, bool isPlaying, bool isHardcore)
        {
            foreach (var player in players)
            {
                SetupKCLeaderboard(player, players, location, wonTeam, blueTeam, redTeam, isPlaying, isHardcore);
            }
        }

        public void SetupKCLeaderboard(KCPlayer player, List<KCPlayer> players, ArenaLocation location, KCTeam wonTeam, KCTeam blueTeam, KCTeam redTeam, bool isPlaying, bool isHardcore)
        {
            var bluePlayers = players.Where(k => k.Team.TeamID == (byte)ETeam.Blue).ToList();
            var redPlayers = players.Where(k => k.Team.TeamID == (byte)ETeam.Red).ToList();

            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "MatchResult2", Plugin.Instance.Translate(player.Team == wonTeam ? (isPlaying ? "Winning_Text" : "Victory_Text") : (isPlaying ? "Losing_Text" : "Defeat_Text")).ToRich());
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "MapName2", location.LocationName.ToUpper());
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamNameR1", redTeam.Info.TeamName);
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamScoreR1", redTeam.Score.ToString());
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamNameB1", blueTeam.Info.TeamName);
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamScoreB1", blueTeam.Score.ToString());
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "GamemodeName2", (isHardcore ? "Hardcore " : "") + Plugin.Instance.Translate("KC_Name_Full").ToRich());

            for (int i = 0; i <= 9; i++)
            {
                EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}B1", false);
                EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}R1", false);
            }

            for (int i = 0; i < bluePlayers.Count; i++)
            {
                var ply = bluePlayers[i];
                bool isPlayer = player == ply;
                var data = ply.GamePlayer.Data;

                var kills = (decimal)ply.Kills;
                var deaths = (decimal)ply.Deaths;
                var objective = ply.KillsConfirmed + ply.KillsDenied;

                var ratio = ply.Deaths == 0 ? String.Format("{0:n}", kills) : String.Format("{0:n}", Math.Round(kills / deaths, 2));

                EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}B1", true);
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"NameTxt{i}B1", (data.HasPrime ? PRIME_SYMBOL : "") + data.SteamName.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KillsTxt{i}B1", ply.Kills.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"DeathsTxt{i}B1", ply.Deaths.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KDRTxt{i}B1", ratio.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"ScoreTxt{i}B1", ply.Score.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlTxt{i}B1", data.Level.ToColor(isPlayer));
                EffectManager.sendUIEffectImageURL(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlIcon{i}B1", Plugin.Instance.DB.Levels.TryGetValue(data.Level, out XPLevel level) ? level.IconLinkSmall : "");
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"AssistsTxt{i}B1", ply.Assists.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"ObjectiveTxt{i}B1", objective.ToColor(isPlayer));
            }

            for (int i = 0; i < redPlayers.Count; i++)
            {
                var ply = redPlayers[i];
                bool isPlayer = player == ply;
                var data = ply.GamePlayer.Data;

                var kills = (decimal)ply.Kills;
                var deaths = (decimal)ply.Deaths;
                var objective = ply.KillsConfirmed + ply.KillsDenied;

                var ratio = ply.Deaths == 0 ? String.Format("{0:n}", kills) : String.Format("{0:n}", Math.Round(kills / deaths, 2));

                EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}R1", true);
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"NameTxt{i}R1", (data.HasPrime ? PRIME_SYMBOL : "") + data.SteamName.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KillsTxt{i}R1", ply.Kills.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"DeathsTxt{i}R1", ply.Deaths.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KDRTxt{i}R1", ratio.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"ScoreTxt{i}R1", ply.Score.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlTxt{i}R1", data.Level.ToColor(isPlayer));
                EffectManager.sendUIEffectImageURL(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlIcon{i}R1", Plugin.Instance.DB.Levels.TryGetValue(data.Level, out XPLevel level) ? level.IconLinkSmall : "");
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"AssistsTxt{i}R1", ply.Assists.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"ObjectiveTxt{i}R1", objective.ToColor(isPlayer));
            }
        }

        public void ShowKCLeaderboard(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Victory", false);
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Defeat", false);
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scores", false);

            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scoreboard2", true);
        }

        public void HideKCLeaderboard(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scoreboard2", false);
        }

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

        public void ClearKCHUD(GamePlayer player)
        {
            EffectManager.askEffectClearByID(KC_ID, player.TransportConnection);
        }

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

            EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"RedTxt{index}", redTeam.HasFlag ? "Home" : (redFlagTaker == null ? "Away" : redFlagTaker.GamePlayer.Player.CharacterName.ToUnrich()));
            EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"BlueTxt{index}", blueTeam.HasFlag ? "Home" : (redFlagTaker == null ? "Away" : redFlagTaker.GamePlayer.Player.CharacterName.ToUnrich()));
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

                EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"RedTxt{index}", redTeam.HasFlag ? "Home" : (redFlagTaker == null ? "Away" : redFlagTaker.GamePlayer.Player.CharacterName.ToUnrich()));
                EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"BlueTxt{index}", blueTeam.HasFlag ? "Home" : (redFlagTaker == null ? "Away" : redFlagTaker.GamePlayer.Player.CharacterName.ToUnrich()));
            }
        }

        public void UpdateCTFTimer(GamePlayer player, string text)
        {
            EffectManager.sendUIEffectText(CTF_KEY, player.TransportConnection, true, "TimerTxt", text);
        }

        public void UpdateCTFHUD(List<CTFPlayer> players, CTFTeam changeTeam)
        {
            var team = (ETeam)changeTeam.TeamID == ETeam.Blue ? "Blue" : "Red";
            var otherTeamPlayers = players.Where(k => k.Team.TeamID != changeTeam.TeamID);
            var teamFlagTaker = otherTeamPlayers.FirstOrDefault(k => k.IsCarryingFlag);

            foreach (var player in players)
            {
                var index = (ETeam)player.Team.TeamID == ETeam.Blue ? 1 : 0;

                EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"{team}Num{index}", changeTeam.Score.ToString());
                EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"{team}Flag{index}", changeTeam.HasFlag ? "" : "");
                EffectManager.sendUIEffectText(CTF_KEY, player.GamePlayer.TransportConnection, true, $"{team}Txt{index}", changeTeam.HasFlag ? "Home" : (teamFlagTaker == null ? "Away" : teamFlagTaker.GamePlayer.Player.CharacterName.ToUnrich()));
            }
        }

        public void SetupCTFLeaderboard(List<CTFPlayer> players, ArenaLocation location, CTFTeam wonTeam, CTFTeam blueTeam, CTFTeam redTeam, bool isPlaying, bool isHardcore)
        {
            foreach (var player in players)
            {
                SetupCTFLeaderboard(player, players, location, wonTeam, blueTeam, redTeam, isPlaying, isHardcore);
            }
        }

        public void SetupCTFLeaderboard(CTFPlayer player, List<CTFPlayer> players, ArenaLocation location, CTFTeam wonTeam, CTFTeam blueTeam, CTFTeam redTeam, bool isPlaying, bool isHardcore)
        {
            var bluePlayers = players.Where(k => k.Team.TeamID == (byte)ETeam.Blue).ToList();
            var redPlayers = players.Where(k => k.Team.TeamID == (byte)ETeam.Red).ToList();

            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "MatchResult2", Plugin.Instance.Translate(player.Team == wonTeam ? (isPlaying ? "Winning_Text" : "Victory_Text") : (isPlaying ? "Losing_Text" : "Defeat_Text")).ToRich());
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "MapName2", location.LocationName.ToUpper());
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamNameR1", redTeam.Info.TeamName);
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamScoreR1", redTeam.Score.ToString());
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamNameB1", blueTeam.Info.TeamName);
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "TeamScoreB1", blueTeam.Score.ToString());
            EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, "GamemodeName2", (isHardcore ? "Hardcore " : "") + Plugin.Instance.Translate("CTF_Name_Full").ToRich());

            for (int i = 0; i <= 9; i++)
            {
                EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}B1", false);
                EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}R1", false);
            }

            for (int i = 0; i < bluePlayers.Count; i++)
            {
                var ply = bluePlayers[i];
                bool isPlayer = player == ply;
                var data = ply.GamePlayer.Data;

                var kills = (decimal)ply.Kills;
                var deaths = (decimal)ply.Deaths;
                var objective = ply.FlagsCaptured + ply.FlagsSaved;

                var ratio = ply.Deaths == 0 ? String.Format("{0:n}", kills) : String.Format("{0:n}", Math.Round(kills / deaths, 2));

                EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}B1", true);
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"NameTxt{i}B1", (data.HasPrime ? PRIME_SYMBOL : "") + data.SteamName.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KillsTxt{i}B1", ply.Kills.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"DeathsTxt{i}B1", ply.Deaths.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KDRTxt{i}B1", ratio.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"ScoreTxt{i}B1", ply.Score.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlTxt{i}B1", data.Level.ToColor(isPlayer));
                EffectManager.sendUIEffectImageURL(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlIcon{i}B1", Plugin.Instance.DB.Levels.TryGetValue(data.Level, out XPLevel level) ? level.IconLinkSmall : "");
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"AssistsTxt{i}B1", ply.Assists.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"ObjectiveTxt{i}B1", objective.ToColor(isPlayer));
            }

            for (int i = 0; i < redPlayers.Count; i++)
            {
                var ply = redPlayers[i];
                bool isPlayer = player == ply;
                var data = ply.GamePlayer.Data;

                var kills = (decimal)ply.Kills;
                var deaths = (decimal)ply.Deaths;
                var objective = ply.FlagsSaved + ply.FlagsCaptured;

                var ratio = ply.Deaths == 0 ? String.Format("{0:n}", kills) : String.Format("{0:n}", Math.Round(kills / deaths, 2));

                EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}R1", true);
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"NameTxt{i}R1", (data.HasPrime ? PRIME_SYMBOL : "") + data.SteamName.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KillsTxt{i}R1", ply.Kills.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"DeathsTxt{i}R1", ply.Deaths.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"KDRTxt{i}R1", ratio.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"ScoreTxt{i}R1", ply.Score.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlTxt{i}R1", data.Level.ToColor(isPlayer));
                EffectManager.sendUIEffectImageURL(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"LvlIcon{i}R1", Plugin.Instance.DB.Levels.TryGetValue(data.Level, out XPLevel level) ? level.IconLinkSmall : "");
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"AssistsTxt{i}R1", ply.Assists.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.GamePlayer.TransportConnection, true, $"ObjectiveTxt{i}R1", objective.ToColor(isPlayer));
            }
        }

        public void ShowCTFLeaderboard(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Victory", false);
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Defeat", false);
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scores", false);

            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scoreboard2", true);
        }

        public void HideCTFLeaderboard(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, "Scoreboard2", false);
        }

        public void SendCTFFlagStates(CTFTeam team, ETeam flag, List<CTFPlayer> players, EFlagState state)
        {
            foreach (var player in players)
            {
                EffectManager.sendUIEffect(FLAG_POPUP_UI, FLAG_POPUP_KEY, player.GamePlayer.TransportConnection, true);
                EffectManager.sendUIEffectVisibility(FLAG_POPUP_KEY, player.GamePlayer.TransportConnection, true, $"FLAG {flag} Toggler", true);
                EffectManager.sendUIEffectText(FLAG_POPUP_KEY, player.GamePlayer.TransportConnection, true, "FlagTxt", Plugin.Instance.Translate($"CTF_{(player.Team.TeamID == team.TeamID ? "Team" : "Enemy")}_{state}_Flag").ToRich());
            }
        }

        public void SendFlagSavedSound(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "FlagSaved0", false);
            EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "FlagSaved0", true);
        }

        public void SendFlagCapturedSound(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "FlagSaved0", false);
            EffectManager.sendUIEffectVisibility(SOUNDS_KEY, player.TransportConnection, true, "FlagSaved0", true);
        }

        public void ClearCTFHUD(GamePlayer player)
        {
            EffectManager.askEffectClearByID(CTF_ID, player.TransportConnection);
        }

        // ALL GAME RELATED UI

        public IEnumerator SetupRoundEndDrops(List<GamePlayer> players, List<(GamePlayer, Case)> roundEndCases, int v)
        {
            foreach (var player in players)
            {
                Logging.Debug($"Setting Drops{v} to true for {player.Player.CharacterName}");
                EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, $"Drops{v}", true);
            }

            for (int i = 0; i < roundEndCases.Count; i++)
            {
                Logging.Debug($"i: {i}");
                var roundEndCase = roundEndCases[i];
                Logging.Debug($"Player: {roundEndCase.Item1.Player.CharacterName}, Case: {roundEndCase.Item2.CaseName}");
                foreach (var player in players)
                {
                    Logging.Debug($"Setting SERVER Scoreboard{v} Drop {i} to true for {player.Player.CharacterName}");
                    EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, $"SERVER Scoreboard{v} Drop {i}", true);
                    Logging.Debug($"Setting SERVER Scoreboard{v} Drop {roundEndCase.Item2.CaseRarity} {i} to true");
                    EffectManager.sendUIEffectVisibility(PRE_ENDING_UI_KEY, player.TransportConnection, true, $"SERVER Scoreboard{v} Drop {roundEndCase.Item2.CaseRarity} {i}", true);
                    Logging.Debug($"Setting SERVER Scoreboard{v} Drop IMAGE {i} to {roundEndCase.Item2.IconLink} for {player.Player.CharacterName}");
                    EffectManager.sendUIEffectImageURL(PRE_ENDING_UI_KEY, player.TransportConnection, true, $"SERVER Scoreboard{v} Drop IMAGE {i}", roundEndCase.Item2.IconLink);
                    Logging.Debug($"Setting SERVER Scoreboard{v} Drop TEXT {i} to {roundEndCase.Item1.Player.CharacterName} for {player.Player.CharacterName}");
                    EffectManager.sendUIEffectText(PRE_ENDING_UI_KEY, player.TransportConnection, true, $"SERVER Scoreboard{v} Drop TEXT {i}", roundEndCase.Item1.Player.CharacterName);
                }
                yield return new WaitForSeconds(1f);
            }
        }

        // EVENTS

        public void OnUIUpdated(CSteamID steamID, EUIPage page)
        {
            if (!UIHandlersLookup.TryGetValue(steamID, out UIHandler handler))
            {
                return;
            }

            switch (page)
            {
                case EUIPage.Pistol:
                    handler.BuildPistolPages();
                    return;
                case EUIPage.SMG:
                    handler.BuildSMGPages();
                    return;
                case EUIPage.LMG:
                    handler.BuildLMGPages();
                    return;
                case EUIPage.Shotgun:
                    handler.BuildShotgunPages();
                    return;
                case EUIPage.AR:
                    handler.BuildARPages();
                    return;
                case EUIPage.Sniper:
                    handler.BuildSniperPages();
                    return;
                case EUIPage.Carbine:
                    handler.BuildCarbinePages();
                    return;
                case EUIPage.GunCharm:
                    handler.BuildGunCharmPages();
                    return;
                case EUIPage.GunSkin:
                    handler.BuildGunSkinPages();
                    return;
                case EUIPage.Knife:
                    handler.BuildKnifePages();
                    return;
                case EUIPage.Tactical:
                    handler.BuildTacticalPages();
                    return;
                case EUIPage.Lethal:
                    handler.BuildLethalPages();
                    return;
                case EUIPage.Card:
                    handler.BuildCardPages();
                    return;
                case EUIPage.Glove:
                    handler.BuildGlovePages();
                    return;
                case EUIPage.Killstreak:
                    handler.BuildKillstreakPages();
                    return;
                case EUIPage.Achievement:
                    handler.BuildAchievementPages();
                    return;
                case EUIPage.Case:
                    handler.BuildUnboxingInventoryPages();
                    return;
                default:
                    throw new ArgumentOutOfRangeException("page", "EUIPage is not as expected");
            }
        }

        public void OnCurrencyUpdated(CSteamID steamID, ECurrency currency)
        {
            if (UIHandlersLookup.TryGetValue(steamID, out UIHandler handler))
            {
                handler.OnCurrencyUpdated(currency);
            }
        }

        public void OnGameUpdated()
        {
            foreach (var handler in UIHandlers)
            {
                if (handler.MainPage == EMainPage.Play && handler.PlayPage == EPlayPage.Games)
                {
                    handler.ShowGames();
                }
            }
        }

        public void OnGameCountUpdated(Game game)
        {
            foreach (var handler in UIHandlers)
            {
                if (handler.MainPage == EMainPage.Play && handler.PlayPage == EPlayPage.Games)
                {
                    handler.UpdateGamePlayerCount(game);
                }
            }
        }

        public void OnServersUpdated()
        {
            foreach (var handler in UIHandlers)
            {
                if (handler.MainPage == EMainPage.Play && handler.PlayPage == EPlayPage.Servers)
                {
                    handler.ShowServers();
                }
            }
        }

        public void OnAchievementsUpdated(CSteamID steamID)
        {
            if (UIHandlersLookup.TryGetValue(steamID, out UIHandler handler))
            {
                if (handler.MainPage == EMainPage.Achievements)
                {
                    handler.ReloadAchievementSubPage();
                    handler.ReloadSelectedAchievement();
                }
            }
        }

        public void OnBattlepassTierUpdated(CSteamID steamID, int tierID)
        {
            if (UIHandlersLookup.TryGetValue(steamID, out UIHandler handler))
            {
                if (handler.MainPage == EMainPage.Battlepass)
                {
                    handler.ShowBattlepassTier(tierID);
                }
            }
        }

        public void OnBattlepassUpdated(CSteamID steamID)
        {
            if (UIHandlersLookup.TryGetValue(steamID, out UIHandler handler))
            {
                if (handler.MainPage == EMainPage.Battlepass)
                {
                    handler.ShowBattlepass();
                }
            }
        }

        private void OnButtonClicked(Player player, string buttonName)
        {
            var ply = UnturnedPlayer.FromPlayer(player);
            bool isGame = Plugin.Instance.Game.TryGetCurrentGame(ply.CSteamID, out _);
            var gPly = Plugin.Instance.Game.GetGamePlayer(ply);
            if (gPly == null)
            {
                Logging.Debug($"Error finding game player for {ply.CharacterName}");
                return;
            }

            if (!UIHandlersLookup.TryGetValue(ply.CSteamID, out UIHandler handler))
            {
                Logging.Debug($"Error finding handler for {player.channel.owner.playerID.characterName}");
                return;
            }

            Logging.Debug($"{player.channel.owner.playerID.characterName} clicked {buttonName}");
            switch (buttonName)
            {
                case "SERVER Play BUTTON":
                    handler.ShowPlayPage();
                    return;
                case "SERVER Loadout BUTTON":
                    handler.ShowLoadouts();
                    return;
                case "SERVER Loadout Close BUTTON":
                    handler.ClearMidgameLoadouts();
                    return;
                case "SERVER Leaderboards BUTTON":
                    handler.ShowLeaderboards();
                    return;
                case "SERVER Quest BUTTON":
                    handler.ShowQuests();
                    return;
                case "SERVER Achievements BUTTON":
                    handler.ShowAchievements();
                    return;
                case "SERVER Unbox BUTTON":
                    handler.ShowUnboxingPage(EUnboxingPage.Inventory);
                    return;
                case "SERVER Battlepass BUTTON":
                    handler.SetupBattlepass();
                    return;
                case "SERVER Exit BUTTON":
                    Provider.kick(player.channel.owner.playerID.steamID, "You exited");
                    return;
                case "SERVER Play Games BUTTON":
                    handler.ShowPlayPage(EPlayPage.Games);
                    return;
                case "SERVER Play Servers BUTTON":
                    handler.ShowPlayPage(EPlayPage.Servers);
                    return;
                case "SERVER Play Join BUTTON":
                    handler.ClickedJoinButton();
                    return;
                case "SERVER Play Back BUTTON":
                    return;
                case "SERVER Loadout Next BUTTON":
                    if (!isGame)
                    {
                        handler.ForwardLoadoutPage();
                    }
                    else
                    {
                        handler.ForwardMidgameLoadoutPage();
                    }
                    return;
                case "SERVER Loadout Previous BUTTON":
                    if (!isGame)
                    {
                        handler.BackwardLoadoutPage();
                    }
                    else
                    {
                        handler.BackwardMidgameLoadoutPage();
                    }
                    return;
                case "SERVER Loadout Back BUTTON":
                    return;
                case "SERVER Loadout Equip BUTTON":
                    if (!isGame)
                    {
                        handler.EquipLoadout();
                    }
                    else
                    {
                        handler.EquipMidgameLoadout();
                    }
                    return;
                case "SERVER Loadout Rename Confirm BUTTON":
                    handler.RenameLoadout();
                    return;
                case "Cancel Button":
                    handler.ExitRenameLoadout();
                    return;
                case "SERVER Loadout Card BUTTON":
                    handler.ShowLoadoutSubPage(ELoadoutPage.Card);
                    return;
                case "SERVER Loadout Glove BUTTON":
                    handler.ShowLoadoutSubPage(ELoadoutPage.Glove);
                    return;
                case "SERVER Loadout Killstreak BUTTON":
                    handler.ShowLoadoutSubPage(ELoadoutPage.Killstreak);
                    return;
                case "SERVER Loadout Perk BUTTON 1":
                    handler.ShowLoadoutSubPage(ELoadoutPage.Perk1);
                    return;
                case "SERVER Loadout Perk BUTTON 2":
                    handler.ShowLoadoutSubPage(ELoadoutPage.Perk2);
                    return;
                case "SERVER Loadout Perk BUTTON 3":
                    handler.ShowLoadoutSubPage(ELoadoutPage.Perk3);
                    return;
                case "SERVER Loadout Primary BUTTON":
                    handler.ShowLoadoutSubPage(ELoadoutPage.Primary);
                    return;
                case "SERVER Loadout Primary Magazine BUTTON":
                    handler.ShowLoadoutSubPage(ELoadoutPage.AttachmentPrimaryMagazine);
                    return;
                case "SERVER Loadout Primary Sights BUTTON":
                    handler.ShowLoadoutSubPage(ELoadoutPage.AttachmentPrimarySights);
                    return;
                case "SERVER Loadout Primary Grip BUTTON":
                    handler.ShowLoadoutSubPage(ELoadoutPage.AttachmentPrimaryGrip);
                    return;
                case "SERVER Loadout Primary Charm BUTTON":
                    handler.ShowLoadoutSubPage(ELoadoutPage.AttachmentPrimaryCharm);
                    return;
                case "SERVER Loadout Primary Barrel BUTTON":
                    handler.ShowLoadoutSubPage(ELoadoutPage.AttachmentPrimaryBarrel);
                    return;
                case "SERVER Loadout Secondary BUTTON":
                    handler.ShowLoadoutSubPage(ELoadoutPage.Secondary);
                    return;
                case "SERVER Loadout Secondary Magazine BUTTON":
                    handler.ShowLoadoutSubPage(ELoadoutPage.AttachmentSecondaryMagazine);
                    return;
                case "SERVER Loadout Secondary Sights BUTTON":
                    handler.ShowLoadoutSubPage(ELoadoutPage.AttachmentSecondarySights);
                    return;
                case "SERVER Loadout Secondary Charm BUTTON":
                    handler.ShowLoadoutSubPage(ELoadoutPage.AttachmentSecondaryCharm);
                    return;
                case "SERVER Loadout Secondary Barrel BUTTON":
                    handler.ShowLoadoutSubPage(ELoadoutPage.AttachmentSecondaryBarrel);
                    return;
                case "SERVER Loadout Knife BUTTON":
                    handler.ShowLoadoutSubPage(ELoadoutPage.Knife);
                    return;
                case "SERVER Loadout Lethal BUTTON":
                    handler.ShowLoadoutSubPage(ELoadoutPage.Lethal);
                    return;
                case "SERVER Loadout Tactical BUTTON":
                    handler.ShowLoadoutSubPage(ELoadoutPage.Tactical);
                    return;
                case "SERVER Loadout Primary Skin BUTTON":
                    handler.ShowLoadoutSubPage(ELoadoutPage.PrimarySkin);
                    return;
                case "SERVER Loadout Secondary Skin BUTTON":
                    handler.ShowLoadoutSubPage(ELoadoutPage.SecondarySkin);
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
                    handler.BuySelectedItem();
                    return;
                case "SERVER Item Unlock BUTTON":
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
                    handler.SelectLeaderboardPage(ELeaderboardPage.Weekly);
                    return;
                case "SERVER Leaderboards All BUTTON":
                    handler.SelectLeaderboardPage(ELeaderboardPage.All);
                    return;
                case "SERVER Leaderboards Daily BUTTON":
                    handler.SelectLeaderboardPage(ELeaderboardPage.Daily);
                    return;
                case "SERVER Leaderboards Seasonal BUTTON":
                    handler.SelectLeaderboardPage(ELeaderboardPage.Seasonal);
                    return;
                case "SERVER Leaderboards Kill BUTTON":
                    handler.SelectLeaderboardTab(ELeaderboardTab.Kill);
                    return;
                case "SERVER Leaderboards Level BUTTON":
                    handler.SelectLeaderboardTab(ELeaderboardTab.Level);
                    return;
                case "SERVER Leaderboards Next BUTTON":
                    handler.ForwardLeaderboardPage();
                    return;
                case "SERVER Leaderboards Previous BUTTON":
                    handler.BackwardLeaderboardPage();
                    return;
                case "SERVER Leaderboards Back BUTTON":
                    return;
                case "SERVER Achievements Next BUTTON":
                    handler.ForwardAchievementSubPage();
                    return;
                case "SERVER Achievements Previous BUTTON":
                    handler.BackwardAchievementSubPage();
                    return;
                case "SERVER Achievements Claim BUTTON":
                    if (handler.MainPage == EMainPage.Achievements)
                    {
                        Plugin.Instance.Achievement.ClaimAchievementTier(ply.CSteamID, handler.SelectedAchievementID);
                    }
                    return;
                case "SERVER Battlepass Tier Skip BUTTON":
                    return;
                case "SERVER Battlepass Confirm BUTTON":
                    if (handler.MainPage == EMainPage.Battlepass)
                        Plugin.Instance.BP.SkipTier(gPly);
                    return;
                case "SERVER Battlepass Claim BUTTON":
                    if (handler.MainPage == EMainPage.Battlepass)
                        Plugin.Instance.BP.ClaimReward(gPly, handler.SelectedBattlepassTierID.Item1, handler.SelectedBattlepassTierID.Item2);
                    return;
                case "SERVER Unbox Inventory BUTTON":
                    handler.ShowUnboxingPage(EUnboxingPage.Inventory);
                    return;
                case "SERVER Unbox Inventory Previous BUTTON":
                    handler.BackwardUnboxingInventoryPage();
                    return;
                case "SERVER Unbox Inventory Next BUTTON":
                    handler.ForwardUnboxingInventoryPage();
                    return;
                case "SERVER Unbox BuyCrate BUTTON":
                    handler.ShowUnboxingPage(EUnboxingPage.Buy);
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
                    handler.BuyUnboxingStoreCase(ECurrency.Coins);
                    return;
                case "SERVER Unbox Buy Scrap BUTTON":
                    handler.BuyUnboxingStoreCase(ECurrency.Scrap);
                    return;
                case "SERVER Unbox Confirm BUTTON":
                    handler.ConfirmUnboxingStoreCase();
                    return;
                case "SERVER Unbox Content Back BUTTON":
                    if (handler.UnboxingPage == EUnboxingPage.Open)
                    {
                        handler.ShowUnboxingPage(EUnboxingPage.Inventory);
                        if (handler.CrateUnboxer != null)
                        {
                            Plugin.Instance.StopCoroutine(handler.CrateUnboxer);
                        }
                    }
                    return;
                case "SERVER Unbox Content Unbox BUTTON":
                    if (handler.UnboxingPage == EUnboxingPage.Open && !handler.IsUnboxing)
                    {
                        handler.CrateUnboxer = Plugin.Instance.StartCoroutine(handler.UnboxCase());
                    }
                    return;
                case "SERVER Summary Close BUTTON":
                    if (handler.MatchEndSummaryShower != null)
                    {
                        Plugin.Instance.StopCoroutine(handler.MatchEndSummaryShower);
                    }
                    return;
                case "KnobOff":
                    handler.OnMusicChanged(true);
                    return;
                case "KnobOn":
                    handler.OnMusicChanged(false);
                    return;
                default:
                    break;
            }

            var numberRegexMatch = new Regex(@"([0-9]+)").Match(buttonName).Value;
            if (!int.TryParse(numberRegexMatch, out int selected))
            {
                Logging.Debug($"Unable to find any number within the button name match: {numberRegexMatch}, returning");
                return;
            }

            if (buttonName.EndsWith("JoinButton"))
            {
                Plugin.Instance.Game.AddPlayerToGame(ply, selected);
            }
            else if (buttonName.StartsWith("SERVER Item BUTTON") || buttonName.StartsWith("SERVER Item Grid BUTTON"))
            {
                handler.SelectedItem(selected);
            }
            else if (buttonName.StartsWith("SERVER Achievements Page"))
            {
                handler.SelectedAchievementMainPage(selected);
            }
            else if (buttonName.StartsWith("SERVER Achievements BUTTON"))
            {
                handler.SelectedAchievement(selected);
            }
            else if (buttonName.StartsWith("SERVER Loadout BUTTON"))
            {
                if (!isGame)
                {
                    handler.SelectedLoadout(selected);
                }
                else
                {
                    handler.SelectedMidgameLoadout(selected);
                }
            }
            else if (buttonName.StartsWith("SERVER Play BUTTON"))
            {
                handler.SelectedPlayButton(selected);
            }
            else if (buttonName.StartsWith("SERVER Battlepass"))
            {
                handler.SelectedBattlepassTier(buttonName.Split(' ')[2] == "T", selected);
            }
            else if (buttonName.StartsWith("SERVER Unbox Crate BUTTON"))
            {
                handler.ShowUnboxingPage(EUnboxingPage.Open, selected);
            }
            else if (buttonName.StartsWith("SERVER Unbox Buy BUTTON"))
            {
                handler.SelectedUnboxingStoreCase(selected);
            }
        }

        private void OnTextCommitted(Player player, string buttonName, string text)
        {
            Logging.Debug($"{player.channel.owner.playerID.characterName} committed text to modal {buttonName} with text {text}");
            if (!UIHandlersLookup.TryGetValue(player.channel.owner.playerID.steamID, out UIHandler handler))
            {
                Logging.Debug($"Error finding UI handler for player, returning");
                return;
            }

            switch (buttonName)
            {
                case "SERVER Loadout Rename INPUTFIELD":
                    handler.SendLoadoutName(text);
                    return;
                case "SERVER Leaderboards Search INPUTFIELD":
                    handler.SearchLeaderboardPlayer(text);
                    return;
            }
        }

        public void Destroy()
        {
            EffectManager.onEffectButtonClicked -= OnButtonClicked;
            EffectManager.onEffectTextCommitted -= OnTextCommitted;
        }
    }
}
