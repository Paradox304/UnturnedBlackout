﻿using Rocket.Unturned.Player;
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
        Config Config { get; set; }
        public List<UIHandler> UIHandlers { get; set; }

        public const ushort FFAID = 27620;
        public const short FFAKey = 27620;

        public const short SoundsKey = 27634;

        public const ushort DeathID = 27635;
        public const short DeathKey = 27635;

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

        public void SendDeathUI(GamePlayer victim, PlayerData victimData, PlayerData killerData, string gunName)
        {
            victim.Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);

            EffectManager.sendUIEffect(DeathID, DeathKey, victim.TransportConnection, true);
            EffectManager.sendUIEffectText(DeathKey, victim.TransportConnection, true, "WeaponName", gunName.ToUpper());
            EffectManager.sendUIEffectImageURL(DeathKey, victim.TransportConnection, true, "SelfIcon", victimData.AvatarLink);
            EffectManager.sendUIEffectText(DeathKey, victim.TransportConnection, true, "SelfName", victimData.SteamName.ToUpper());
            EffectManager.sendUIEffectText(DeathKey, victim.TransportConnection, true, "SelfXPNum", Plugin.Instance.Translate("Level_Show", victimData.Level).ToRich());
            EffectManager.sendUIEffectImageURL(DeathKey, victim.TransportConnection, true, "EnemyIcon", killerData.AvatarLink);
            EffectManager.sendUIEffectText(DeathKey, victim.TransportConnection, true, "DeathName", killerData.SteamName.ToUpper());
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

        // FFA RELATED UI
        public void ShowFFAHUD(GamePlayer player)
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

        public void ClearFFAHUD(GamePlayer player)
        {
            EffectManager.askEffectClearByID(FFAID, player.TransportConnection);
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
