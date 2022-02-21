using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using UnturnedBlackout.Database;
using UnturnedBlackout.Enums;
using UnturnedBlackout.GameTypes;
using UnturnedBlackout.Instances;
using UnturnedBlackout.Models.CTF;
using UnturnedBlackout.Models.Feed;
using UnturnedBlackout.Models.FFA;
using UnturnedBlackout.Models.Global;
using UnturnedBlackout.Models.KC;
using UnturnedBlackout.Models.Level;
using UnturnedBlackout.Models.TDM;

namespace UnturnedBlackout.Managers
{
    public class UIManager
    {
        public Config Config { get; set; }

        public Dictionary<uint, LevelIcon> Icons { get; set; }
        public Dictionary<uint, LevelXP> LevelsXPNeeded { get; set; }
        public Dictionary<ushort, FeedIcon> KillFeedIcons { get; set; }

        public List<UIHandler> UIHandlers { get; set; }

        public const ushort WaitingForPlayersID = 1111;
        public const short WaitingForPlayersKey = 1111;

        public const ushort FFAID = 27620;
        public const short FFAKey = 27620;

        public const ushort TDMID = 27621;
        public const short TDMKey = 27621;

        public const ushort KCID = 27621;
        public const short KCKey = 27621;

        public const ushort CTFID = 27623;
        public const short CTFKey = 27623;

        public const short SoundsKey = 27634;

        public const ushort DeathID = 27635;
        public const short DeathKey = 27635;

        public const ushort PreEndingUIID = 27636;
        public const short PreEndingUIKey = 27636;

        public const ushort LevelUpID = 27638;
        public const short LevelUpKey = 27638;

        public const ushort LoadingUIID = 27640;
        public const short LoadingUIKey = 27640;

        public UIManager()
        {
            Config = Plugin.Instance.Configuration.Instance;
            Icons = Config.LevelIcons.ToDictionary(k => k.Level);
            LevelsXPNeeded = Config.LevelsXP.ToDictionary(k => k.Level);
            KillFeedIcons = Config.KillFeedIcons.ToDictionary(k => k.WeaponID);

            UIHandlers = new List<UIHandler>();

            EffectManager.onEffectButtonClicked += OnButtonClicked;
        }

        public void RegisterUIHandler(UnturnedPlayer player)
        {
            if (UIHandlers.Exists(k => k.SteamID == player.CSteamID))
            {
                UIHandlers.RemoveAll(k => k.SteamID == player.CSteamID);
            }

            UIHandlers.Add(new UIHandler(player));
        }

        public void UnregisterUIHandler(UnturnedPlayer player)
        {
            UIHandlers.RemoveAll(k => k.SteamID == player.CSteamID);
        }

        public void ShowMenuUI(UnturnedPlayer player)
        {
            var handler = UIHandlers.FirstOrDefault(k => k.SteamID == player.CSteamID);
            if (handler != null)
            {
                handler.ShowUI();
            }
        }

        public void HideMenuUI(UnturnedPlayer player)
        {
            var handler = UIHandlers.FirstOrDefault(k => k.SteamID == player.CSteamID);
            if (handler != null)
            {
                handler.HideUI();
            }
        }

        // ALL GAMES RELATED UI
        public void SendWaitingForPlayersUI(GamePlayer player)
        {

        }

        public void ClearWaitingForPlayersUI(GamePlayer player)
        {

        }

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

        public void ShowXPUI(GamePlayer player, int xp, string xpGained)
        {
            EffectManager.sendUIEffect(27630, 27630, player.TransportConnection, true, $"+{xp} XP", xpGained);
        }

        public void SendMultiKillSound(GamePlayer player, int multiKill)
        {
            switch (multiKill)
            {
                case 0:
                    EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, "Kill", false);
                    EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, "Kill", true);
                    return;
                case 1:
                    EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, "Kill", false);
                    EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, "Kill", true);
                    return;
                case 2:
                    EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, "MultiKill1", false);
                    EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, "MultiKill1", true);
                    return;
                case 3:
                    EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, "MultiKill2", false);
                    EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, "MultiKill2", true);
                    return;
                case 4:
                    EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, "MultiKill3", false);
                    EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, "MultiKill3", true);
                    return;
                case 5:
                    EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, "MultiKill4", false);
                    EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, "MultiKill4", true);
                    return;
                case 6:
                    EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, "MultiKill5", false);
                    EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, "MultiKill5", true);
                    return;
                default:
                    EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, "MultiKill5", false);
                    EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, "MultiKill5", true);
                    return;
            };
        }

        public void SendLevelUpAnimation(GamePlayer player, uint newRank)
        {
            if (!Icons.TryGetValue(newRank, out LevelIcon icon))
            {
                if (!Icons.TryGetValue(0, out icon))
                {
                    return;
                }
            }

            EffectManager.sendUIEffect(LevelUpID, LevelUpKey, player.TransportConnection, true);
            EffectManager.sendUIEffectImageURL(LevelUpKey, player.TransportConnection, true, "LevelUpIcon", icon.IconLink);
            EffectManager.sendUIEffectText(LevelUpKey, player.TransportConnection, true, "LevelUpDesc", Plugin.Instance.Translate("Level_Up_Desc", newRank).ToRich());
            EffectManager.sendUIEffectText(LevelUpKey, player.TransportConnection, true, "LevelUpText", Plugin.Instance.Translate("Level_Up_Text").ToRich());
        }

        public void SendHitmarkerSound(GamePlayer player)
        {
            EffectManager.sendUIEffect(27637, 27637, player.TransportConnection, true);
        }

        public void SendKillfeed(List<GamePlayer> players, EGameType type, List<Feed> killfeed)
        {
            short key;
            switch (type)
            {
                case EGameType.FFA:
                    key = FFAKey;
                    break;
                case EGameType.TDM:
                    key = TDMKey;
                    break;
                case EGameType.KC:
                    key = TDMKey;
                    break;
                case EGameType.CTF:
                    key = CTFKey;
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
                feedText = $"<size={Config.KillFeedFont}>{feedText}</size>";
            }
            foreach (var player in players)
            {
                EffectManager.sendUIEffectText(key, player.TransportConnection, true, "Killfeed", feedText);
            }
        }

        public void SendVoiceChat(List<GamePlayer> players, EGameType type, bool isEnding, List<GamePlayer> playersTalking)
        {
            short key = PreEndingUIKey;
            if (!isEnding)
            {
                switch (type)
                {
                    case EGameType.FFA:
                        key = FFAKey;
                        break;
                    case EGameType.TDM:
                        key = TDMKey;
                        break;
                    case EGameType.KC:
                        key = TDMKey;
                        break;
                    case EGameType.CTF:
                        key = CTFKey;
                        break;
                    default:
                        return;
                }
            }

            var voiceChatText = "";
            foreach (var talking in playersTalking)
            {
                voiceChatText += $" {talking.Player.CharacterName.ToUnrich()} \n";
            }
            if (!string.IsNullOrEmpty(voiceChatText))
            {
                voiceChatText = $"<size={Config.VoiceChatFont}>{voiceChatText}</size>";
            }
            foreach (var player in players)
            {
                EffectManager.sendUIEffectText(key, player.TransportConnection, true, "VoiceChatUsers", voiceChatText);
            }
        }

        public void SendDeathUI(GamePlayer victim, PlayerData killerData)
        {
            victim.Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);

            EffectManager.sendUIEffect(DeathID, DeathKey, victim.TransportConnection, true);
            EffectManager.sendUIEffectImageURL(DeathKey, victim.TransportConnection, true, "EnemyIcon", killerData.AvatarLink);
            EffectManager.sendUIEffectImageURL(DeathKey, victim.TransportConnection, true, "EnemyXPIcon", Icons.TryGetValue(killerData.Level, out LevelIcon icon) ? icon.IconLink54 : (Icons.TryGetValue(0, out icon) ? icon.IconLink54 : ""));
            EffectManager.sendUIEffectText(DeathKey, victim.TransportConnection, true, "EnemyName", killerData.SteamName.ToUpper());
            EffectManager.sendUIEffectText(DeathKey, victim.TransportConnection, true, "EnemyXPNum", Plugin.Instance.Translate("Level_Show", killerData.Level).ToRich());
            EffectManager.sendUIEffectImageURL(DeathKey, victim.TransportConnection, true, "DeathBanner", "https://cdn.discordapp.com/attachments/899796442649092119/927985217975758898/Senosan-85382-HG-Dark-grey-600x600.png");
        }

        public void UpdateRespawnTimer(GamePlayer player, string timer)
        {
            EffectManager.sendUIEffectText(DeathKey, player.TransportConnection, true, "RespawnTime", timer);
        }

        public void ClearDeathUI(GamePlayer player)
        {
            player.Player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
            EffectManager.askEffectClearByID(DeathID, player.TransportConnection);
        }

        public void SendLoadingUI(GamePlayer player, EGameType gameMode, ArenaLocation location)
        {
            player.Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);
            EffectManager.sendUIEffect(LoadingUIID, LoadingUIKey, player.TransportConnection, true);
            EffectManager.sendUIEffectImageURL(LoadingUIKey, player.TransportConnection, true, "MapIMG", location.ImageLink);
            EffectManager.sendUIEffectVisibility(LoadingUIKey, player.TransportConnection, true, $"Gamemode{gameMode}Icon", true);
            EffectManager.sendUIEffectText(LoadingUIKey, player.TransportConnection, true, "MapTxt", location.LocationName);
            EffectManager.sendUIEffectText(LoadingUIKey, player.TransportConnection, true, "GamemodeTxt", Plugin.Instance.Translate($"{gameMode}_Name_Full").ToRich());
        }

        public void ClearLoadingUI(GamePlayer player)
        {
            player.Player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
            EffectManager.askEffectClearByID(LoadingUIID, player.TransportConnection);
        }

        public void SendPreEndingUI(GamePlayer player)
        {
            EffectManager.sendUIEffect(PreEndingUIID, PreEndingUIKey, player.TransportConnection, true);
        }

        public void SetupPreEndingUI(GamePlayer player, EGameType gameMode, bool hasWon, int blueScore, int redScore, string blueName, string redName)
        {
            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, hasWon ? "Victory" : "Defeat", true);
            EffectManager.sendUIEffectText(PreEndingUIKey, player.TransportConnection, true, hasWon ? "VictoryTxt" : "DefeatTxt", Plugin.Instance.Translate(hasWon ? $"{gameMode}_Victory_Desc" : $"{gameMode}_Defeat_Desc").ToRich());

            if (gameMode != EGameType.FFA)
            {
                EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Scores", true);
                EffectManager.sendUIEffectText(PreEndingUIKey, player.TransportConnection, true, "BlueSideScore", blueScore.ToString());
                EffectManager.sendUIEffectText(PreEndingUIKey, player.TransportConnection, true, "BlueSideName", blueName);
                EffectManager.sendUIEffectText(PreEndingUIKey, player.TransportConnection, true, "RedSideScore", redScore.ToString());
                EffectManager.sendUIEffectText(PreEndingUIKey, player.TransportConnection, true, "RedSideName", redName);
            }
        }

        public void ClearPreEndingUI(GamePlayer player)
        {
            EffectManager.askEffectClearByID(PreEndingUIID, player.TransportConnection);
        }

        // FFA RELATED UI

        public void SendFFAHUD(GamePlayer player)
        {
            EffectManager.sendUIEffect(FFAID, FFAKey, player.TransportConnection, true);

            EffectManager.sendUIEffectVisibility(FFAKey, player.TransportConnection, true, "ScoreCounter", true);
            EffectManager.sendUIEffect(27610, 27610, player.TransportConnection, true, Plugin.Instance.Translate("FFA_Name").ToRich(), Plugin.Instance.Translate("FFA_Desc").ToRich());
            EffectManager.sendUIEffectVisibility(FFAKey, player.TransportConnection, true, "Timer", true);
        }

        public void UpdateFFATimer(GamePlayer player, string text)
        {
            EffectManager.sendUIEffectText(FFAKey, player.TransportConnection, true, "TimerTxt", text);
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

                EffectManager.sendUIEffectVisibility(FFAKey, player.GamePlayer.TransportConnection, true, "CounterWinning", true);
                EffectManager.sendUIEffectVisibility(FFAKey, player.GamePlayer.TransportConnection, true, "CounterLosing", false);
            }
            else
            {
                EffectManager.sendUIEffectVisibility(FFAKey, player.GamePlayer.TransportConnection, true, "CounterWinning", false);
                EffectManager.sendUIEffectVisibility(FFAKey, player.GamePlayer.TransportConnection, true, "CounterLosing", true);
            }

            EffectManager.sendUIEffectText(FFAKey, player.GamePlayer.TransportConnection, true, "1stPlacementName", firstPlayer.GamePlayer.Player.CharacterName.ToUnrich());
            EffectManager.sendUIEffectText(FFAKey, player.GamePlayer.TransportConnection, true, "1stPlacementScore", firstPlayer.Kills.ToString());

            EffectManager.sendUIEffectText(FFAKey, player.GamePlayer.TransportConnection, true, "2ndPlacementPlace", secondPlayer != null ? Utility.GetOrdinal(Players.IndexOf(secondPlayer) + 1) : "0");
            EffectManager.sendUIEffectText(FFAKey, player.GamePlayer.TransportConnection, true, "2ndPlacementName", secondPlayer != null ? secondPlayer.GamePlayer.Player.CharacterName.ToUnrich() : "NONE");
            EffectManager.sendUIEffectText(FFAKey, player.GamePlayer.TransportConnection, true, "2ndPlacementScore", secondPlayer != null ? secondPlayer.Kills.ToString() : "0");
        }

        public void SetupFFALeaderboard(List<FFAPlayer> players, ArenaLocation location, bool isPlaying)
        {
            foreach (var player in players)
            {
                SetupFFALeaderboard(player, players, location, isPlaying);
            }
        }

        public void SetupFFALeaderboard(FFAPlayer ply, List<FFAPlayer> players, ArenaLocation location, bool isPlaying)
        {
            EffectManager.sendUIEffectText(PreEndingUIKey, ply.GamePlayer.TransportConnection, true, "MatchResult1", Plugin.Instance.Translate(players.IndexOf(ply) == 0 ? (isPlaying ? "Winning_Text" : "Victory_Text") : (isPlaying ? "Losing_Text" : "Defeat_Text")).ToRich());
            EffectManager.sendUIEffectText(PreEndingUIKey, ply.GamePlayer.TransportConnection, true, "MapName1", location.LocationName.ToUpper());
            EffectManager.sendUIEffectText(PreEndingUIKey, ply.GamePlayer.TransportConnection, true, "GamemodeName1", Plugin.Instance.Translate("FFA_Name_Full").ToRich());

            for (int i = 0; i <= 19; i++)
            {
                EffectManager.sendUIEffectVisibility(PreEndingUIKey, ply.GamePlayer.TransportConnection, true, $"PlayerStats{i}", false);
            }

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                bool isPlayer = player == ply;
                if (!Plugin.Instance.DBManager.PlayerCache.TryGetValue(player.GamePlayer.SteamID, out PlayerData data))
                {
                    return;
                }

                var kills = (decimal)player.Kills;
                var deaths = (decimal)player.Deaths;

                var ratio = player.Deaths == 0 ? String.Format("{0:n}", kills) : String.Format("{0:n}", Math.Round(kills / deaths, 2));

                EffectManager.sendUIEffectVisibility(PreEndingUIKey, ply.GamePlayer.TransportConnection, true, $"PlayerStats{i}", true);
                EffectManager.sendUIEffectText(PreEndingUIKey, ply.GamePlayer.TransportConnection, true, $"NameTxt{i}", data.SteamName.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, ply.GamePlayer.TransportConnection, true, $"KillsTxt{i}", player.Kills.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, ply.GamePlayer.TransportConnection, true, $"DeathsTxt{i}", player.Deaths.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, ply.GamePlayer.TransportConnection, true, $"KDRTxt{i}", ratio.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, ply.GamePlayer.TransportConnection, true, $"ScoreTxt{i}", player.Score.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, ply.GamePlayer.TransportConnection, true, $"LvlTxt{i}", data.Level.ToColor(isPlayer));
                EffectManager.sendUIEffectImageURL(PreEndingUIKey, ply.GamePlayer.TransportConnection, true, $"LvlIcon{i}", Icons.TryGetValue(data.Level, out LevelIcon icon) ? icon.IconLink28 : (Icons.TryGetValue(0, out icon) ? icon.IconLink28 : ""));
                EffectManager.sendUIEffectText(PreEndingUIKey, ply.GamePlayer.TransportConnection, true, $"AssistsTxt{i}", player.Assists.ToColor(isPlayer));
            }
        }

        public void ShowFFALeaderboard(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Victory", false);
            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Defeat", false);
            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Scores", false);

            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Scoreboard1", true);
        }

        public void HideFFALeaderboard(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Scoreboard1", false);
        }

        public void ClearFFAHUD(GamePlayer player)
        {
            EffectManager.askEffectClearByID(FFAID, player.TransportConnection);
        }


        // TDM Related UI

        public void SendTDMHUD(TDMPlayer player, TDMTeam blueTeam, TDMTeam redTeam)
        {
            EffectManager.sendUIEffect(TDMID, TDMKey, player.GamePlayer.TransportConnection, true);
            EffectManager.sendUIEffectVisibility(TDMKey, player.GamePlayer.TransportConnection, true, player.Team.TeamID == (byte)ETeam.Blue ? "BlueTeam" : "RedTeam", true);
            EffectManager.sendUIEffect(27611, 27611, player.GamePlayer.TransportConnection, true, Plugin.Instance.Translate("TDM_Name").ToRich(), Plugin.Instance.Translate("TDM_Desc").ToRich());
            EffectManager.sendUIEffectVisibility(TDMKey, player.GamePlayer.TransportConnection, true, "Timer", true);
            EffectManager.sendUIEffectVisibility(TDMKey, player.GamePlayer.TransportConnection, true, "Team", true);
            EffectManager.sendUIEffectText(TDMKey, player.GamePlayer.TransportConnection, true, "TeamName", $"<color={player.Team.Info.TeamColorHexCode}>{player.Team.Info.TeamName}</color>");

            int index = player.Team.TeamID == (byte)ETeam.Blue ? 1 : 0;
            int blueSpaces = blueTeam.Score * 96 / Config.TDM.ScoreLimit;
            int redSpaces = redTeam.Score * 96 / Config.TDM.ScoreLimit;
            EffectManager.sendUIEffectText(TDMKey, player.GamePlayer.TransportConnection, true, $"RedNum{index}", redTeam.Score.ToString());
            EffectManager.sendUIEffectText(TDMKey, player.GamePlayer.TransportConnection, true, $"RedBarFill{index}", redSpaces == 0 ? " " : new string(' ', redSpaces));

            EffectManager.sendUIEffectText(TDMKey, player.GamePlayer.TransportConnection, true, $"BlueNum{index}", blueTeam.Score.ToString());
            EffectManager.sendUIEffectText(TDMKey, player.GamePlayer.TransportConnection, true, $"BlueBarFill{index}", blueSpaces == 0 ? " " : new string(' ', blueSpaces));
        }

        public void UpdateTDMTimer(GamePlayer player, string text)
        {
            EffectManager.sendUIEffectText(TDMKey, player.TransportConnection, true, "TimerTxt", text);
        }

        public void UpdateTDMScore(TDMPlayer player, TDMTeam changeTeam)
        {
            int index = player.Team.TeamID == (byte)ETeam.Blue ? 1 : 0;
            var team = (ETeam)changeTeam.TeamID;
            int spaces = changeTeam.Score * 96 / Config.TDM.ScoreLimit;

            EffectManager.sendUIEffectText(TDMKey, player.GamePlayer.TransportConnection, true, $"{team}Num{index}", changeTeam.Score.ToString());
            EffectManager.sendUIEffectText(TDMKey, player.GamePlayer.TransportConnection, true, $"{team}BarFill{index}", spaces == 0 ? " " : new string(' ', spaces));
        }

        public void SetupTDMLeaderboard(List<TDMPlayer> players, ArenaLocation location, TDMTeam wonTeam, TDMTeam blueTeam, TDMTeam redTeam, bool isPlaying)
        {
            foreach (var player in players)
            {
                SetupTDMLeaderboard(player, players, location, wonTeam, blueTeam, redTeam, isPlaying);
            }
        }

        public void SetupTDMLeaderboard(TDMPlayer player, List<TDMPlayer> players, ArenaLocation location, TDMTeam wonTeam, TDMTeam blueTeam, TDMTeam redTeam, bool isPlaying)
        {
            var bluePlayers = players.Where(k => k.Team.TeamID == (byte)ETeam.Blue).ToList();
            var redPlayers = players.Where(k => k.Team.TeamID == (byte)ETeam.Red).ToList();

            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "MatchResult0", Plugin.Instance.Translate(player.Team == wonTeam ? (isPlaying ? "Winning_Text" : "Victory_Text") : (isPlaying ? "Losing_Text" : "Defeat_Text")).ToRich());
            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "MapName0", location.LocationName.ToUpper());
            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "TeamNameR0", redTeam.Info.TeamName);
            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "TeamScoreR0", redTeam.Score.ToString());
            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "TeamNameB0", blueTeam.Info.TeamName);
            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "TeamScoreB0", blueTeam.Score.ToString());
            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "GamemodeName1", Plugin.Instance.Translate("TDM_Name_Full").ToRich());

            for (int i = 0; i <= 9; i++)
            {
                EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}B0", false);
                EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}R0", false);
            }

            for (int i = 0; i < bluePlayers.Count; i++)
            {
                var ply = bluePlayers[i];
                bool isPlayer = player == ply;
                if (!Plugin.Instance.DBManager.PlayerCache.TryGetValue(ply.GamePlayer.SteamID, out PlayerData data))
                {
                    continue;
                }

                var kills = (decimal)ply.Kills;
                var deaths = (decimal)ply.Deaths;

                var ratio = ply.Deaths == 0 ? String.Format("{0:n}", kills) : String.Format("{0:n}", Math.Round(kills / deaths, 2));

                EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}B0", true);
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"NameTxt{i}B0", data.SteamName.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"KillsTxt{i}B0", ply.Kills.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"DeathsTxt{i}B0", ply.Deaths.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"KDRTxt{i}B0", ratio.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"ScoreTxt{i}B0", ply.Score.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"LvlTxt{i}B0", data.Level.ToColor(isPlayer));
                EffectManager.sendUIEffectImageURL(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"LvlIcon{i}B0", Icons.TryGetValue(data.Level, out LevelIcon icon) ? icon.IconLink28 : (Icons.TryGetValue(0, out icon) ? icon.IconLink28 : ""));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"AssistsTxt{i}B0", ply.Assists.ToColor(isPlayer));
            }

            for (int i = 0; i < redPlayers.Count; i++)
            {
                var ply = redPlayers[i];
                bool isPlayer = player == ply;
                if (!Plugin.Instance.DBManager.PlayerCache.TryGetValue(ply.GamePlayer.SteamID, out PlayerData data))
                {
                    continue;
                }

                var kills = (decimal)ply.Kills;
                var deaths = (decimal)ply.Deaths;

                var ratio = ply.Deaths == 0 ? String.Format("{0:n}", kills) : String.Format("{0:n}", Math.Round(kills / deaths, 2));

                EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}R0", true);
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"NameTxt{i}R0", data.SteamName.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"KillsTxt{i}R0", ply.Kills.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"DeathsTxt{i}R0", ply.Deaths.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"KDRTxt{i}R0", ratio.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"ScoreTxt{i}R0", ply.Score.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"LvlTxt{i}R0", data.Level.ToColor(isPlayer));
                EffectManager.sendUIEffectImageURL(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"LvlIcon{i}R0", Icons.TryGetValue(data.Level, out LevelIcon icon) ? icon.IconLink28 : (Icons.TryGetValue(0, out icon) ? icon.IconLink28 : ""));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"AssistsTxt{i}R0", ply.Assists.ToColor(isPlayer));
            }
        }

        public void ShowTDMLeaderboard(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Victory", false);
            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Defeat", false);
            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Scores", false);

            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Scoreboard0", true);
        }

        public void HideTDMLeaderboard(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Scoreboard0", false);
        }

        public void ClearTDMHUD(GamePlayer player)
        {
            EffectManager.askEffectClearByID(TDMID, player.TransportConnection);
        }

        // KC Related UI

        public void SendKCHUD(KCPlayer player, KCTeam blueTeam, KCTeam redTeam)
        {
            EffectManager.sendUIEffect(KCID, KCKey, player.GamePlayer.TransportConnection, true);
            EffectManager.sendUIEffectVisibility(KCKey, player.GamePlayer.TransportConnection, true, player.Team.TeamID == (byte)ETeam.Blue ? "BlueTeam" : "RedTeam", true);
            EffectManager.sendUIEffect(27612, 27612, player.GamePlayer.TransportConnection, true, Plugin.Instance.Translate("KC_Name").ToRich(), Plugin.Instance.Translate("KC_Desc").ToRich());
            EffectManager.sendUIEffectVisibility(KCKey, player.GamePlayer.TransportConnection, true, "Timer", true);
            EffectManager.sendUIEffectVisibility(KCKey, player.GamePlayer.TransportConnection, true, "Team", true);
            EffectManager.sendUIEffectText(KCKey, player.GamePlayer.TransportConnection, true, "TeamName", $"<color={player.Team.Info.TeamColorHexCode}>{player.Team.Info.TeamName}</color>");

            int index = player.Team.TeamID == (byte)ETeam.Blue ? 1 : 0;
            int blueSpaces = blueTeam.Score * 96 / Config.TDM.ScoreLimit;
            int redSpaces = redTeam.Score * 96 / Config.TDM.ScoreLimit;
            EffectManager.sendUIEffectText(KCKey, player.GamePlayer.TransportConnection, true, $"RedNum{index}", redTeam.Score.ToString());
            EffectManager.sendUIEffectText(KCKey, player.GamePlayer.TransportConnection, true, $"RedBarFill{index}", redSpaces == 0 ? " " : new string(' ', redSpaces));

            EffectManager.sendUIEffectText(KCKey, player.GamePlayer.TransportConnection, true, $"BlueNum{index}", blueTeam.Score.ToString());
            EffectManager.sendUIEffectText(KCKey, player.GamePlayer.TransportConnection, true, $"BlueBarFill{index}", blueSpaces == 0 ? " " : new string(' ', blueSpaces));
        }

        public void UpdateKCTimer(GamePlayer player, string text)
        {
            EffectManager.sendUIEffectText(KCKey, player.TransportConnection, true, "TimerTxt", text);
        }

        public void UpdateKCScore(KCPlayer player, KCTeam changeTeam)
        {
            int index = player.Team.TeamID == (byte)ETeam.Blue ? 1 : 0;
            var team = (ETeam)changeTeam.TeamID;
            int spaces = changeTeam.Score * 96 / Config.TDM.ScoreLimit;

            EffectManager.sendUIEffectText(KCKey, player.GamePlayer.TransportConnection, true, $"{team}Num{index}", changeTeam.Score.ToString());
            EffectManager.sendUIEffectText(KCKey, player.GamePlayer.TransportConnection, true, $"{team}BarFill{index}", spaces == 0 ? " " : new string(' ', spaces));
        }

        public void SetupKCLeaderboard(List<KCPlayer> players, ArenaLocation location, KCTeam wonTeam, KCTeam blueTeam, KCTeam redTeam, bool isPlaying)
        {
            foreach (var player in players)
            {
                SetupKCLeaderboard(player, players, location, wonTeam, blueTeam, redTeam, isPlaying);
            }
        }

        public void SetupKCLeaderboard(KCPlayer player, List<KCPlayer> players, ArenaLocation location, KCTeam wonTeam, KCTeam blueTeam, KCTeam redTeam, bool isPlaying)
        {
            var bluePlayers = players.Where(k => k.Team.TeamID == (byte)ETeam.Blue).ToList();
            var redPlayers = players.Where(k => k.Team.TeamID == (byte)ETeam.Red).ToList();

            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "MatchResult2", Plugin.Instance.Translate(player.Team == wonTeam ? (isPlaying ? "Winning_Text" : "Victory_Text") : (isPlaying ? "Losing_Text" : "Defeat_Text")).ToRich());
            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "MapName2", location.LocationName.ToUpper());
            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "TeamNameR1", redTeam.Info.TeamName);
            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "TeamScoreR1", redTeam.Score.ToString());
            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "TeamNameB1", blueTeam.Info.TeamName);
            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "TeamScoreB1", blueTeam.Score.ToString());
            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "GamemodeName2", Plugin.Instance.Translate("KC_Name_Full").ToRich());

            for (int i = 0; i <= 9; i++)
            {
                EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}B1", false);
                EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}R1", false);
            }

            for (int i = 0; i < bluePlayers.Count; i++)
            {
                var ply = bluePlayers[i];
                bool isPlayer = player == ply;
                if (!Plugin.Instance.DBManager.PlayerCache.TryGetValue(ply.GamePlayer.SteamID, out PlayerData data))
                {
                    continue;
                }

                var kills = (decimal)ply.Kills;
                var deaths = (decimal)ply.Deaths;
                var objective = ply.KillsConfirmed + ply.KillsDenied;

                var ratio = ply.Deaths == 0 ? String.Format("{0:n}", kills) : String.Format("{0:n}", Math.Round(kills / deaths, 2));

                EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}B1", true);
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"NameTxt{i}B1", data.SteamName.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"KillsTxt{i}B1", ply.Kills.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"DeathsTxt{i}B1", ply.Deaths.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"KDRTxt{i}B1", ratio.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"ScoreTxt{i}B1", ply.Score.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"LvlTxt{i}B1", data.Level.ToColor(isPlayer));
                EffectManager.sendUIEffectImageURL(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"LvlIcon{i}B1", Icons.TryGetValue(data.Level, out LevelIcon icon) ? icon.IconLink28 : (Icons.TryGetValue(0, out icon) ? icon.IconLink28 : ""));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"AssistsTxt{i}B1", ply.Assists.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"ObjectiveTxt{i}B1", objective.ToColor(isPlayer));
            }

            for (int i = 0; i < redPlayers.Count; i++)
            {
                var ply = redPlayers[i];
                bool isPlayer = player == ply;
                if (!Plugin.Instance.DBManager.PlayerCache.TryGetValue(ply.GamePlayer.SteamID, out PlayerData data))
                {
                    continue;
                }

                var kills = (decimal)ply.Kills;
                var deaths = (decimal)ply.Deaths;
                var objective = ply.KillsConfirmed + ply.KillsDenied;

                var ratio = ply.Deaths == 0 ? String.Format("{0:n}", kills) : String.Format("{0:n}", Math.Round(kills / deaths, 2));

                EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}R1", true);
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"NameTxt{i}R1", data.SteamName.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"KillsTxt{i}R1", ply.Kills.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"DeathsTxt{i}R1", ply.Deaths.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"KDRTxt{i}R1", ratio.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"ScoreTxt{i}R1", ply.Score.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"LvlTxt{i}R1", data.Level.ToColor(isPlayer));
                EffectManager.sendUIEffectImageURL(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"LvlIcon{i}R1", Icons.TryGetValue(data.Level, out LevelIcon icon) ? icon.IconLink28 : (Icons.TryGetValue(0, out icon) ? icon.IconLink28 : ""));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"AssistsTxt{i}R1", ply.Assists.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"ObjectiveTxt{i}R1", objective.ToColor(isPlayer));
            }
        }

        public void ShowKCLeaderboard(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Victory", false);
            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Defeat", false);
            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Scores", false);

            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Scoreboard2", true);
        }

        public void HideKCLeaderboard(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Scoreboard2", false);
        }

        public void SendKillConfirmedSound(GamePlayer player)
        {
            var random = UnityEngine.Random.Range(0, 4);
            EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, $"KillConfirmed{random}", false);
            EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, $"KillConfirmed{random}", true);
        }

        public void SendKillDeniedSound(GamePlayer player)
        {
            var random = UnityEngine.Random.Range(0, 3);
            EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, $"KillDenied{random}", false);
            EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, $"KillDenied{random}", true);
        }

        public void ClearKCHUD(GamePlayer player)
        {
            EffectManager.askEffectClearByID(KCID, player.TransportConnection);
        }

        // CTF Related UI

        public void SendCTFHUD(CTFPlayer player, CTFTeam blueTeam, CTFTeam redTeam, List<CTFPlayer> players)
        {
            var bluePlayers = players.Where(k => k.Team.TeamID == blueTeam.TeamID);
            var redPlayers = players.Where(k => k.Team.TeamID == redTeam.TeamID);
            var index = player.Team.TeamID == blueTeam.TeamID ? 1 : 0;

            EffectManager.sendUIEffect(CTFID, CTFKey, player.GamePlayer.TransportConnection, true);
            EffectManager.sendUIEffectVisibility(CTFKey, player.GamePlayer.TransportConnection, true, "Timer", true);
            EffectManager.sendUIEffect(27613, 27613, player.GamePlayer.TransportConnection, true, Plugin.Instance.Translate("CTF_Name").ToRich(), Plugin.Instance.Translate("CTF_Desc").ToRich());
            EffectManager.sendUIEffectVisibility(CTFKey, player.GamePlayer.TransportConnection, true, player.Team == blueTeam ? "BlueTeam" : "RedTeam", true);
            EffectManager.sendUIEffectText(CTFKey, player.GamePlayer.TransportConnection, true, $"RedNum{index}", redTeam.Score.ToString());
            EffectManager.sendUIEffectText(CTFKey, player.GamePlayer.TransportConnection, true, $"BlueNum{index}", blueTeam.Score.ToString());
            EffectManager.sendUIEffectText(CTFKey, player.GamePlayer.TransportConnection, true, $"RedFlag{index}", redTeam.HasFlag ? "" : "");
            EffectManager.sendUIEffectText(CTFKey, player.GamePlayer.TransportConnection, true, $"BlueFlag{index}", blueTeam.HasFlag ? "" : "");

            var blueFlagTaker = redPlayers.FirstOrDefault(k => k.IsCarryingFlag);
            var redFlagTaker = bluePlayers.FirstOrDefault(k => k.IsCarryingFlag);

            EffectManager.sendUIEffectText(CTFKey, player.GamePlayer.TransportConnection, true, $"RedTxt{index}", redTeam.HasFlag ? "Home" : (redFlagTaker == null ? "Away" : redFlagTaker.GamePlayer.Player.CharacterName.ToUnrich()));
            EffectManager.sendUIEffectText(CTFKey, player.GamePlayer.TransportConnection, true, $"BlueTxt{index}", blueTeam.HasFlag ? "Home" : (redFlagTaker == null ? "Away" : redFlagTaker.GamePlayer.Player.CharacterName.ToUnrich()));
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

                EffectManager.sendUIEffect(CTFID, CTFKey, player.GamePlayer.TransportConnection, true);
                EffectManager.sendUIEffect(27613, 27613, player.GamePlayer.TransportConnection, true, Plugin.Instance.Translate("CTF_Name").ToRich(), Plugin.Instance.Translate("CTF_Desc").ToRich());
                EffectManager.sendUIEffectVisibility(CTFKey, player.GamePlayer.TransportConnection, true, player.Team == blueTeam ? "BlueTeam" : "RedTeam", true);
                EffectManager.sendUIEffectText(CTFKey, player.GamePlayer.TransportConnection, true, $"RedNum{index}", redTeam.Score.ToString());
                EffectManager.sendUIEffectText(CTFKey, player.GamePlayer.TransportConnection, true, $"BlueNum{index}", blueTeam.Score.ToString());
                EffectManager.sendUIEffectText(CTFKey, player.GamePlayer.TransportConnection, true, $"RedFlag{index}", redTeam.HasFlag ? "" : "");
                EffectManager.sendUIEffectText(CTFKey, player.GamePlayer.TransportConnection, true, $"BlueFlag{index}", blueTeam.HasFlag ? "" : "");
                EffectManager.sendUIEffectVisibility(CTFKey, player.GamePlayer.TransportConnection, true, "Timer", true);

                EffectManager.sendUIEffectText(CTFKey, player.GamePlayer.TransportConnection, true, $"RedTxt{index}", redTeam.HasFlag ? "Home" : (redFlagTaker == null ? "Away" : redFlagTaker.GamePlayer.Player.CharacterName.ToUnrich()));
                EffectManager.sendUIEffectText(CTFKey, player.GamePlayer.TransportConnection, true, $"BlueTxt{index}", blueTeam.HasFlag ? "Home" : (redFlagTaker == null ? "Away" : redFlagTaker.GamePlayer.Player.CharacterName.ToUnrich()));
            }
        }

        public void UpdateCTFTimer(GamePlayer player, string text)
        {
            EffectManager.sendUIEffectText(CTFKey, player.TransportConnection, true, "TimerTxt", text);
        }

        public void UpdateCTFHUD(List<CTFPlayer> players, CTFTeam changeTeam)
        {
            var team = (ETeam)changeTeam.TeamID == ETeam.Blue ? "Blue" : "Red";
            var otherTeamPlayers = players.Where(k => k.Team.TeamID != changeTeam.TeamID);
            var teamFlagTaker = otherTeamPlayers.FirstOrDefault(k => k.IsCarryingFlag);

            foreach (var player in players)
            {
                var index = (ETeam)player.Team.TeamID == ETeam.Blue ? 1 : 0;

                EffectManager.sendUIEffectText(CTFKey, player.GamePlayer.TransportConnection, true, $"{team}Num{index}", changeTeam.Score.ToString());
                EffectManager.sendUIEffectText(CTFKey, player.GamePlayer.TransportConnection, true, $"{team}Flag{index}", changeTeam.HasFlag ? "" : "");
                EffectManager.sendUIEffectText(CTFKey, player.GamePlayer.TransportConnection, true, $"{team}Txt{index}", changeTeam.HasFlag ? "Home" : (teamFlagTaker == null ? "Away" : teamFlagTaker.GamePlayer.Player.CharacterName.ToUnrich()));
            }
        }

        public void SetupCTFLeaderboard(List<CTFPlayer> players, ArenaLocation location, CTFTeam wonTeam, CTFTeam blueTeam, CTFTeam redTeam, bool isPlaying)
        {
            foreach (var player in players)
            {
                SetupCTFLeaderboard(player, players, location, wonTeam, blueTeam, redTeam, isPlaying);
            }
        }

        public void SetupCTFLeaderboard(CTFPlayer player, List<CTFPlayer> players, ArenaLocation location, CTFTeam wonTeam, CTFTeam blueTeam, CTFTeam redTeam, bool isPlaying)
        {
            var bluePlayers = players.Where(k => k.Team.TeamID == (byte)ETeam.Blue).ToList();
            var redPlayers = players.Where(k => k.Team.TeamID == (byte)ETeam.Red).ToList();

            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "MatchResult2", Plugin.Instance.Translate(player.Team == wonTeam ? (isPlaying ? "Winning_Text" : "Victory_Text") : (isPlaying ? "Losing_Text" : "Defeat_Text")).ToRich());
            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "MapName2", location.LocationName.ToUpper());
            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "TeamNameR1", redTeam.Info.TeamName);
            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "TeamScoreR1", redTeam.Score.ToString());
            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "TeamNameB1", blueTeam.Info.TeamName);
            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "TeamScoreB1", blueTeam.Score.ToString());
            EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, "GamemodeName2", Plugin.Instance.Translate("CTF_Name_Full").ToRich());

            for (int i = 0; i <= 9; i++)
            {
                EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}B1", false);
                EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}R1", false);
            }

            for (int i = 0; i < bluePlayers.Count; i++)
            {
                var ply = bluePlayers[i];
                bool isPlayer = player == ply;
                if (!Plugin.Instance.DBManager.PlayerCache.TryGetValue(ply.GamePlayer.SteamID, out PlayerData data))
                {
                    continue;
                }

                var kills = (decimal)ply.Kills;
                var deaths = (decimal)ply.Deaths;
                var objective = ply.FlagsCaptured + ply.FlagsSaved;

                var ratio = ply.Deaths == 0 ? String.Format("{0:n}", kills) : String.Format("{0:n}", Math.Round(kills / deaths, 2));

                EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}B1", true);
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"NameTxt{i}B1", data.SteamName.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"KillsTxt{i}B1", ply.Kills.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"DeathsTxt{i}B1", ply.Deaths.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"KDRTxt{i}B1", ratio.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"ScoreTxt{i}B1", ply.Score.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"LvlTxt{i}B1", data.Level.ToColor(isPlayer));
                EffectManager.sendUIEffectImageURL(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"LvlIcon{i}B1", Icons.TryGetValue(data.Level, out LevelIcon icon) ? icon.IconLink28 : (Icons.TryGetValue(0, out icon) ? icon.IconLink28 : ""));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"AssistsTxt{i}B1", ply.Assists.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"ObjectiveTxt{i}B1", objective.ToColor(isPlayer));
            }

            for (int i = 0; i < redPlayers.Count; i++)
            {
                var ply = redPlayers[i];
                bool isPlayer = player == ply;
                if (!Plugin.Instance.DBManager.PlayerCache.TryGetValue(ply.GamePlayer.SteamID, out PlayerData data))
                {
                    continue;
                }

                var kills = (decimal)ply.Kills;
                var deaths = (decimal)ply.Deaths;
                var objective = ply.FlagsSaved + ply.FlagsCaptured;

                var ratio = ply.Deaths == 0 ? String.Format("{0:n}", kills) : String.Format("{0:n}", Math.Round(kills / deaths, 2));

                EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}R1", true);
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"NameTxt{i}R1", data.SteamName.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"KillsTxt{i}R1", ply.Kills.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"DeathsTxt{i}R1", ply.Deaths.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"KDRTxt{i}R1", ratio.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"ScoreTxt{i}R1", ply.Score.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"LvlTxt{i}R1", data.Level.ToColor(isPlayer));
                EffectManager.sendUIEffectImageURL(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"LvlIcon{i}R1", Icons.TryGetValue(data.Level, out LevelIcon icon) ? icon.IconLink28 : (Icons.TryGetValue(0, out icon) ? icon.IconLink28 : ""));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"AssistsTxt{i}R1", ply.Assists.ToColor(isPlayer));
                EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"ObjectiveTxt{i}R1", objective.ToColor(isPlayer));
            }
        }

        public void ShowCTFLeaderboard(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Victory", false);
            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Defeat", false);
            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Scores", false);

            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Scoreboard2", true);
        }

        public void HideCTFLeaderboard(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Scoreboard2", false);
        }

        public void SendCTFFlagStates(CTFTeam team, ETeam flag, List<CTFPlayer> players, EFlagState state)
        {
            var flagID = flag == ETeam.Blue ? 27901 : 27900;

            foreach (var player in players)
            {
                EffectManager.sendUIEffect((ushort)flagID, (short)flagID, player.GamePlayer.TransportConnection, true);
                EffectManager.sendUIEffectText((short)flagID, player.GamePlayer.TransportConnection, true, "FlagTxt", Plugin.Instance.Translate($"CTF_{(player.Team.TeamID == team.TeamID ? "Team" : "Enemy")}_{state}_Flag").ToRich());
            }
        }

        public void SendFlagSavedSound(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, "FlagSaved0", false);
            EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, "FlagSaved0", true);
        }

        public void SendFlagCapturedSound(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, "FlagSaved0", false);
            EffectManager.sendUIEffectVisibility(SoundsKey, player.TransportConnection, true, "FlagSaved0", true);
        }

        public void ClearCTFHUD(GamePlayer player)
        {
            EffectManager.askEffectClearByID(CTFID, player.TransportConnection);
        }

        // EVENTS
        public void OnGamesUpdated()
        {
            foreach (var handler in UIHandlers)
            {
                if (handler.CurrentPage == EPage.Play)
                {
                    handler.ShowGames();
                }
            }
        }

        public void OnGameUpdated(Game game)
        {
            foreach (var handler in UIHandlers)
            {
                if (handler.CurrentPage == EPage.Play)
                {
                    handler.ShowGame(game);
                }
            }
        }

        public void OnGameCountUpdated(Game game)
        {
            foreach (var handler in UIHandlers)
            {
                if (handler.CurrentPage == EPage.Play)
                {
                    handler.UpdateGamePlayerCount(game);
                }
            }
        }

        public void OnGameVoteCountUpdated(Game game)
        {
            foreach (var handler in UIHandlers)
            {
                if (handler.CurrentPage == EPage.Play)
                {
                    handler.UpdateVoteCount(game);
                }
            }
        }

        public void OnGameVoteTimerUpdated(Game game, string timer)
        {
            foreach (var handler in UIHandlers)
            {
                if (handler.CurrentPage == EPage.Play)
                {
                    handler.UpdateVoteTimer(game, timer);
                }
            }
        }

        public void OnXPChanged(UnturnedPlayer player)
        {
            var handler = UIHandlers.FirstOrDefault(k => k.SteamID == player.CSteamID);
            if (handler != null && handler.CurrentPage == EPage.Play)
            {
                handler.OnXPChanged();
            }
        }

        public void OnPlayerMusicChanged(Player player, bool isMusic)
        {
            var handler = UIHandlers.FirstOrDefault(k => k.SteamID == player.channel.owner.playerID.steamID);
            if (handler != null && handler.CurrentPage == EPage.Play)
            {
                handler.OnMusicChanged(isMusic);
            }
        }

        private void OnButtonClicked(Player player, string buttonName)
        {
            Utility.Debug($"{player.channel.owner.playerID.characterName} clicked {buttonName}");
            var ply = UnturnedPlayer.FromPlayer(player);

            switch (buttonName)
            {
                case "KnobOff":
                    OnPlayerMusicChanged(player, true);
                    return;
                case "KnobOn":
                    OnPlayerMusicChanged(player, false);
                    return;
                default:
                    break;
            }

            if (buttonName.EndsWith("JoinButton"))
            {
                if (int.TryParse(buttonName.Replace("Lobby", "").Replace("JoinButton", ""), out int selected))
                {
                    Plugin.Instance.GameManager.AddPlayerToGame(ply, selected);
                }
            }
            else if (buttonName.EndsWith("Vote0"))
            {
                if (int.TryParse(buttonName.Replace("Lobby", "").Replace("Vote0", ""), out int selected))
                {
                    Plugin.Instance.GameManager.OnPlayerVoted(ply, selected, 0);
                }
            }
            else if (buttonName.EndsWith("Vote1"))
            {
                if (int.TryParse(buttonName.Replace("Lobby", "").Replace("Vote1", ""), out int selected))
                {
                    Plugin.Instance.GameManager.OnPlayerVoted(ply, selected, 1);
                }
            }
        }

        public void Destroy()
        {
            EffectManager.onEffectButtonClicked -= OnButtonClicked;
        }
    }
}
