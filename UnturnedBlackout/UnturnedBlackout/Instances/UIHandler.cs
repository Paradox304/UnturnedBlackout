using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using UnityEngine;
using UnturnedBlackout.Database;
using UnturnedBlackout.Enums;
using UnturnedBlackout.GameTypes;
using UnturnedBlackout.Models;

namespace UnturnedBlackout.Instances
{
    public class UIHandler
    {
        public CSteamID SteamID { get; set; }
        public UnturnedPlayer Player { get; set; }

        public ITransportConnection TransportConnection { get; set; }
        public Config Config { get; set; }

        public EPage CurrentPage { get; set; }

        public const ushort ID = 27632;
        public const short Key = 27632;

        public UIHandler(UnturnedPlayer player)
        {
            Utility.Debug($"Creating UIHandler for {player.CSteamID}");
            SteamID = player.CSteamID;
            Player = player;
            TransportConnection = player.Player.channel.GetOwnerTransportConnection();
            Config = Plugin.Instance.Configuration.Instance;

            ResetUIValues();
        }

        public void ShowUI()
        {
            Utility.Debug($"Showing Menu UI for player {Player.CharacterName}");
            EffectManager.sendUIEffect(ID, Key, TransportConnection, true);
            //Plugin.Instance.HUDManager.HideHUD(Player);
            Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);
            ResetUIValues();
            ShowGames();
            ClearChat();
            SetupUI();
        }

        public void HideUI()
        {
            Utility.Debug($"Hiding menu UI for player {Player.CharacterName}");
            EffectManager.askEffectClearByID(ID, TransportConnection);
            //Plugin.Instance.HUDManager.ShowHUD(Player);
            Player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
            ResetUIValues();
        }

        public void ResetUIValues()
        {
            Utility.Debug($"Resetting UI stats for player {Player.CharacterName}");
            CurrentPage = EPage.None;
        }

        public void SetupUI()
        {
            Utility.Debug($"Setting up UI for {Player.CharacterName}");
            if (!Plugin.Instance.DBManager.PlayerCache.TryGetValue(SteamID, out PlayerData data))
            {
                Utility.Debug("Could'nt find data, returning");
                return;
            }

            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "PlayerIcon", data.AvatarLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerName", data.SteamName);
            OnXPChanged();
        }

        public void ClearChat()
        {
            var steamPlayer = Player.SteamPlayer();
            for (int i = 0; i <= 10; i++)
            {
                ChatManager.serverSendMessage("", Color.white, toPlayer: steamPlayer);
            }
        }

        // Play Page

        public void ShowGames()
        {
            Utility.Debug($"Showing games for {Player.CharacterName}");
            CurrentPage = EPage.Play;

            for (int i = 0; i <= 9; i++)
            {
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"Lobby{i}", false);
            }

            var games = Plugin.Instance.GameManager.Games;
            for (int i = 0; i < games.Count; i++)
            {
                var game = games[i];
                Utility.Debug($"i: {i}, players: {game.GetPlayerCount()}, max players: {game.Location.MaxPlayers}, phase: {game.GamePhase}");
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"Lobby{i}", true);
                ShowGame(game);
            }
        }

        public void ShowGame(Game game)
        {
            int index = Plugin.Instance.GameManager.Games.IndexOf(game);

            if (index == -1)
            {
                return;
            }

            if (game.GamePhase == EGamePhase.Starting || game.GamePhase == EGamePhase.Started || game.GamePhase == EGamePhase.Ending)
            {
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"Lobby{index}Join", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"Lobby{index}Vote", false);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"Lobby{index}Waiting", false);

                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"Lobby{index}IMG", game.Location.ImageLink);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"Lobby{index}Map", game.Location.LocationName);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"Lobby{index}Mode", Plugin.Instance.Translate($"{game.GameMode}_Name").ToRich());
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"Lobby{index}Count", $"{game.GetPlayerCount()}/{game.Location.MaxPlayers}");

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, game.GamePhase == EGamePhase.Starting || game.GamePhase == EGamePhase.Started ? $"Lobby{index}JoinButton" : $"Lobby{index}EndingButton", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, game.GamePhase == EGamePhase.Starting || game.GamePhase == EGamePhase.Started ? $"Lobby{index}EndingButton" : $"Lobby{index}JoinButton", false);
                return;
            }

            if (game.GamePhase == EGamePhase.WaitingForVoting)
            {
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"Lobby{index}Join", false);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"Lobby{index}Vote", false);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"Lobby{index}Waiting", true);
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"Lobby{index}Join", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"Lobby{index}Vote", true);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"Lobby{index}Waiting", false);

            for (int choice = 0; choice <= 1; choice++)
            {
                var vote = game.VoteChoices[choice];

                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"Lobby{index}IMG{choice}", vote.Location.ImageLink);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"Lobby{index}VoteMapName{choice}", Plugin.Instance.Translate($"Vote{choice}_MapName", vote.Location.LocationName, choice == 0 ? game.Vote0.Count : game.Vote1.Count).ToRich());
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"Lobby{index}VoteMode{choice}", Plugin.Instance.Translate($"{vote.GameMode}_Name").ToRich());
            }
        }

        public void UpdateGamePlayerCount(Game game)
        {
            int index = Plugin.Instance.GameManager.Games.IndexOf(game);

            if (index == -1)
            {
                return;
            }

            if (game.GamePhase == EGamePhase.Starting || game.GamePhase == EGamePhase.Started)
            {
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"Lobby{index}Count", $"{game.GetPlayerCount()}/{game.Location.MaxPlayers}");
            }
        }

        public void UpdateVoteCount(Game game)
        {
            int index = Plugin.Instance.GameManager.Games.IndexOf(game);

            if (index == -1)
            {
                return;
            }

            if (game.GamePhase != EGamePhase.Voting)
            {
                return;
            }

            for (int choice = 0; choice <= 1; choice++)
            {
                var vote = game.VoteChoices[choice];

                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"Lobby{index}VoteMapName{choice}", Plugin.Instance.Translate($"Vote{choice}_MapName", vote.Location.LocationName, choice == 0 ? game.Vote0.Count : game.Vote1.Count).ToRich());
            }
        }

        public void UpdateVoteTimer(Game game, string timer)
        {
            int index = Plugin.Instance.GameManager.Games.IndexOf(game);

            if (index == -1)
            {
                return;
            }

            if (game.GamePhase == EGamePhase.Voting)
            {
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"Lobby{index}VoteTimer", timer);
            }
        }

        // Events
        public void OnXPChanged()
        {
            if (!Plugin.Instance.DBManager.PlayerCache.TryGetValue(SteamID, out PlayerData data))
            {
                return;
            }

            var ui = Plugin.Instance.UIManager;
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "XPNum", Plugin.Instance.Translate("Level_Show", data.Level).ToRich());
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "XPIcon", ui.Icons.TryGetValue(data.Level, out LevelIcon icon) ? icon.IconLink54 : (ui.Icons.TryGetValue(0, out icon) ? icon.IconLink54 : ""));
            int spaces = 0;
            if (data.TryGetNeededXP(out int neededXP))
            {
                spaces = Math.Min(96, neededXP == 0 ? 0 : (int)(data.XP * 96 / neededXP));
            }
            Utility.Debug($"XP changed {Player.CharacterName}, XP: {data.XP}, Needed XP: {neededXP}, Spaces: {spaces}");
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "XPBarFill", spaces == 0 ? " " : new string(' ', spaces));
        }
    }
}
