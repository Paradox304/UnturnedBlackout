using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedLegends.Database;
using UnturnedLegends.Enums;
using UnturnedLegends.GameTypes;
using UnturnedLegends.Instances;
using UnturnedLegends.Models;

namespace UnturnedLegends.Managers
{
    public class UIManager
    {
        public Config Config { get; set; }

        public List<UIHandler> UIHandlers { get; set; }

        public const ushort FFAID = 27620;
        public const short FFAKey = 27620;

        public const ushort TDMID = 27621;
        public const short TDMKey = 27621;

        public const short SoundsKey = 27634;

        public const ushort DeathID = 27635;
        public const short DeathKey = 27635;

        public const ushort PreEndingUIID = 27636;
        public const short PreEndingUIKey = 27636;

        public UIManager()
        {
            Config = Plugin.Instance.Configuration.Instance;
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

        public void SendDeathUI(GamePlayer victim, PlayerData killerData)
        {
            victim.Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);

            EffectManager.sendUIEffect(DeathID, DeathKey, victim.TransportConnection, true);
            EffectManager.sendUIEffectImageURL(DeathKey, victim.TransportConnection, true, "EnemyIcon", killerData.AvatarLink);
            EffectManager.sendUIEffectText(DeathKey, victim.TransportConnection, true, "EnemyName", killerData.SteamName.ToUpper());
            EffectManager.sendUIEffectText(DeathKey, victim.TransportConnection, true, "EnemyXPNum", Plugin.Instance.Translate("Level_Show", killerData.Level).ToRich());
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

        public void SendPreEndingUI(GamePlayer player, EGameType gameMode, bool hasWon, int blueScore, int redScore)
        {
            EffectManager.sendUIEffect(PreEndingUIID, PreEndingUIKey, player.TransportConnection, true);

            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, hasWon ? "Victory" : "Defeat", true);
            EffectManager.sendUIEffectText(PreEndingUIKey, player.TransportConnection, true, hasWon ? "VictoryTxt" : "DefeatTxt", Plugin.Instance.Translate(hasWon ? $"{gameMode}_Victory_Desc" : $"{gameMode}_Defeat_Desc").ToRich());

            if (gameMode != EGameType.FFA)
            {
                EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Scores", true);
                EffectManager.sendUIEffectText(PreEndingUIKey, player.TransportConnection, true, "BlueSideScore", blueScore.ToString());
                EffectManager.sendUIEffectText(PreEndingUIKey, player.TransportConnection, true, "BlueSideName", Plugin.Instance.Translate("Blue_Team_Name").ToRich());
                EffectManager.sendUIEffectText(PreEndingUIKey, player.TransportConnection, true, "RedSideScore", redScore.ToString());
                EffectManager.sendUIEffectText(PreEndingUIKey, player.TransportConnection, true, "RedSideName", Plugin.Instance.Translate("Red_Team_Name").ToRich());
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

            EffectManager.sendUIEffectText(FFAKey, player.GamePlayer.TransportConnection, true, "1stPlacementName", firstPlayer.GamePlayer.Player.CharacterName);
            EffectManager.sendUIEffectText(FFAKey, player.GamePlayer.TransportConnection, true, "1stPlacementScore", firstPlayer.Kills.ToString());

            EffectManager.sendUIEffectText(FFAKey, player.GamePlayer.TransportConnection, true, "2ndPlacementPlace", secondPlayer != null ? Utility.GetOrdinal(Players.IndexOf(secondPlayer) + 1) : "0");
            EffectManager.sendUIEffectText(FFAKey, player.GamePlayer.TransportConnection, true, "2ndPlacementName", secondPlayer != null ? secondPlayer.GamePlayer.Player.CharacterName : "NONE");
            EffectManager.sendUIEffectText(FFAKey, player.GamePlayer.TransportConnection, true, "2ndPlacementScore", secondPlayer != null ? secondPlayer.Kills.ToString() : "0");
        }

        public void SetupFFAEndingLeaderboard(List<FFAPlayer> players, ArenaLocation location)
        {
            for (int i = 0; i < players.Count; i++)
            {
                var ply = players[i];
                if (!Plugin.Instance.DBManager.PlayerCache.TryGetValue(ply.GamePlayer.SteamID, out PlayerData data))
                {
                    continue;
                }

                var ratio = ply.Deaths == 0 ? "0.00" : String.Format("{0:n}", Math.Round((decimal)(ply.Kills / ply.Deaths), 2));
                EffectManager.sendUIEffectText(PreEndingUIKey, ply.GamePlayer.TransportConnection, true, "MatchResult1", Plugin.Instance.Translate(i == 0 ? "Victory_Text" : "Defeat_Text").ToRich());
                EffectManager.sendUIEffectText(PreEndingUIKey, ply.GamePlayer.TransportConnection, true, "MapName1", location.LocationName.ToUpper());
                
                for (int i2 = 0; i2 < players.Count; i2++)
                {
                    var player = players[i2];
                    EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"PlayerStats{i}", true);
                    EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"NameTxt{i}", data.SteamName);
                    EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"KillsTxt{i}", ply.Kills.ToString());
                    EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"DeathsTxt{i}", ply.Deaths.ToString());
                    EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"RatioTxt{i}", ratio);
                    EffectManager.sendUIEffectText(PreEndingUIKey, player.GamePlayer.TransportConnection, true, $"ScoreTxt{i}", ply.Score.ToString());
                }
            }
        }

        public void ShowFFAEndingLeaderboard(GamePlayer player)
        {
            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Victory", false);
            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Defeat", false);
            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Scores", false);

            EffectManager.sendUIEffectVisibility(PreEndingUIKey, player.TransportConnection, true, "Scoreboard1", true);
        }

        public void ClearFFAHUD(GamePlayer player)
        {
            EffectManager.askEffectClearByID(FFAID, player.TransportConnection);
        }


        // TDM Related UI
        public void SendTDMHUD(TDMPlayer player, Team blueTeam, Team redTeam)
        {
            EffectManager.sendUIEffect(TDMID, TDMKey, player.GamePlayer.TransportConnection, true);
            EffectManager.sendUIEffectVisibility(TDMKey, player.GamePlayer.TransportConnection, true, player.Team.TeamID == (byte)ETeam.Blue ? "BlueTeam" : "RedTeam", true);
            EffectManager.sendUIEffect(27611, 27611, player.GamePlayer.TransportConnection, true, Plugin.Instance.Translate("TDM_Name").ToRich(), Plugin.Instance.Translate("TDM_Desc").ToRich());
            EffectManager.sendUIEffectVisibility(TDMKey, player.GamePlayer.TransportConnection, true, "Timer", true);

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

        public void UpdateTDMScore(TDMPlayer player, Team changeTeam)
        {
            int index = player.Team.TeamID == (byte)ETeam.Blue ? 1 : 0;
            var team = (ETeam)changeTeam.TeamID;
            int spaces = changeTeam.Score * 96 / Config.TDM.ScoreLimit;

            EffectManager.sendUIEffectText(TDMKey, player.GamePlayer.TransportConnection, true, $"{team}Num{index}", changeTeam.Score.ToString());
            EffectManager.sendUIEffectText(TDMKey, player.GamePlayer.TransportConnection, true, $"{team}BarFill{index}", spaces == 0 ? " " : new string(' ', spaces));
        }

        public void ClearTDMHUD(GamePlayer player)
        {
            EffectManager.askEffectClearByID(TDMID, player.TransportConnection);
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

        private void OnButtonClicked(Player player, string buttonName)
        {
            Utility.Debug($"{player.channel.owner.playerID.characterName} clicked {buttonName}");
            var ply = UnturnedPlayer.FromPlayer(player);

            if (buttonName.EndsWith("JoinButton"))
            {
                if (int.TryParse(buttonName.Replace("Lobby", "").Replace("JoinButton", ""), out int selected))
                {
                    Plugin.Instance.GameManager.AddPlayerToGame(ply, selected);
                }
            } else if (buttonName.EndsWith("Vote0"))
            {
                if (int.TryParse(buttonName.Replace("Lobby","").Replace("Vote0", ""), out int selected))
                {
                    Plugin.Instance.GameManager.OnPlayerVoted(ply, selected, 0);
                }
            } else if (buttonName.EndsWith("Vote1"))
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
