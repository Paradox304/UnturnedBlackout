using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;
using UnturnedBlackout.GameTypes;
using UnturnedBlackout.Managers;
using UnturnedBlackout.Models.Level;
using UnturnedBlackout.Models.UI;

namespace UnturnedBlackout.Instances
{
    public class UIHandler
    {
        public const ushort ID = 27632;
        public const short Key = 27632;

        public DatabaseManager DB { get; set; }
        public CSteamID SteamID { get; set; }
        public UnturnedPlayer Player { get; set; }
        public PlayerLoadout PlayerLoadout { get; set; }

        public ITransportConnection TransportConnection { get; set; }
        public Config Config { get; set; }

        public EMainPage MainPage { get; set; }

        // Loadout
        public ELoadoutPage LoadoutPage { get; set; }
        public int LoadoutPageID { get; set; }
        public int LoadoutID { get; set; }
        public string LoadoutNameText { get; set; }

        public ELoadoutSubPage LoadoutSubPage { get; set; }
        public int LoadoutSubPageID { get; set; }
        public object SelectedItemID { get; set; }

        public Dictionary<int, PageLoadout> LoadoutPages { get; set; }
        public Dictionary<int, PageGun> PistolPages { get; set; }
        public Dictionary<int, PageGun> SMGPages { get; set; }
        public Dictionary<int, PageGun> ShotgunPages { get; set; }
        public Dictionary<int, PageGun> ARPages { get; set; }
        public Dictionary<int, PageGun> SniperPages { get; set; }
        public Dictionary<ushort, Dictionary<int, PageAttachment>> AttachmentPages { get; set; }
        public Dictionary<int, PageKnife> KnifePages { get; set; }
        public Dictionary<int, PagePerk> PerkPages { get; set; }
        public Dictionary<int, PageGadget> TacticalPages { get; set; }
        public Dictionary<int, PageGadget> LethalPages { get; set; }
        public Dictionary<int, PageCard> CardPages { get; set; }
        public Dictionary<int, PageGlove> GlovePages { get; set; }
        public Dictionary<int, PageKillstreak> KillstreakPages { get; set; }

        public UIHandler(UnturnedPlayer player)
        {
            Logging.Debug($"Creating UIHandler for {player.CSteamID}");
            SteamID = player.CSteamID;
            Player = player;
            TransportConnection = player.Player.channel.GetOwnerTransportConnection();
            Config = Plugin.Instance.Configuration.Instance;
            DB = Plugin.Instance.DBManager;
            if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out PlayerLoadout loadout))
            {
                Logging.Debug($"Error finding player loadout for {player.CharacterName}, failed to initialize UIHandler for player");
                return;
            }

            PlayerLoadout = loadout;
            BuildPages();
            ResetUIValues();
        }

        public void ShowUI()
        {
            EffectManager.sendUIEffect(ID, Key, TransportConnection, true);
            Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);
            ResetUIValues();
            ShowGames();
            SetupUI();
        }

        public void HideUI()
        {
            EffectManager.askEffectClearByID(ID, TransportConnection);
            Player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
            ResetUIValues();
        }

        public void ResetUIValues()
        {
            MainPage = EMainPage.None;
        }

        public void SetupUI()
        {
            if (!Plugin.Instance.DBManager.PlayerData.TryGetValue(SteamID, out PlayerData data))
            {
                return;
            }

            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "PlayerIcon", data.AvatarLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "PlayerName", data.SteamName);

            ClearChat();
            OnXPChanged();
            OnMusicChanged(data.Music);
        }

        public void ClearChat()
        {
            var steamPlayer = Player.SteamPlayer();
            for (int i = 0; i <= 10; i++)
            {
                ChatManager.serverSendMessage("", Color.white, toPlayer: steamPlayer);
            }
        }

        public void BuildPages()
        {
            BuildLoadoutPages();
            BuildGunPages();
            BuildAttachmentPages();
            BuildKnifePages();
            BuildPerkPages();
            BuildTacticalPages();
            BuildLethalPages();
            BuildCardPages();
            BuildGlovePages();
            BuildKillstreakPages();
        }

        public void BuildLoadoutPages()
        {

        }

        public void BuildGunPages()
        {

        }

        public void BuildAttachmentPages()
        {

        }

        public void BuildKnifePages()
        {

        }

        public void BuildPerkPages()
        {

        }

        public void BuildTacticalPages()
        {

        }

        public void BuildLethalPages()
        {

        }

        public void BuildCardPages()
        {

        }

        public void BuildGlovePages()
        {

        }

        public void BuildKillstreakPages()
        {

        }


        // Play Page

        public void ShowGames()
        {
            Logging.Debug($"Showing games for {Player.CharacterName}");
            MainPage = EMainPage.Play;

            for (int i = 0; i <= 9; i++)
            {
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"Lobby{i}", false);
            }

            var games = Plugin.Instance.GameManager.Games;
            for (int i = 0; i < games.Count; i++)
            {
                var game = games[i];
                Logging.Debug($"i: {i}, players: {game.GetPlayerCount()}, max players: {game.Location.GetMaxPlayers(game.GameMode)}, phase: {game.GamePhase}");
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

            if (game.GamePhase == EGamePhase.Starting || game.GamePhase == EGamePhase.Started || game.GamePhase == EGamePhase.Ending || game.GamePhase == EGamePhase.WaitingForPlayers)
            {
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"Lobby{index}Join", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"Lobby{index}Vote", false);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"Lobby{index}Waiting", false);

                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"Lobby{index}IMG", game.Location.ImageLink);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"Lobby{index}Map", game.Location.LocationName);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"Lobby{index}Mode", Plugin.Instance.Translate($"{game.GameMode}_Name").ToRich());
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"Lobby{index}Count", $"{game.GetPlayerCount()}/{game.Location.GetMaxPlayers(game.GameMode)}");

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, game.GamePhase == EGamePhase.Ending ? $"Lobby{index}EndingButton" : $"Lobby{index}JoinButton", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, game.GamePhase == EGamePhase.Ending ? $"Lobby{index}JoinButton" : $"Lobby{index}EndingButton", false);
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
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"Lobby{index}Count", $"{game.GetPlayerCount()}/{game.Location.GetMaxPlayers(game.GameMode)}");
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

        // Loadout Page

        public void ShowLoadouts()
        {

        }

        public void ShowLoadout()
        {

        }

        public void ForwardLoadoutPage()
        {

        }

        public void BackwardLoadoutPage()
        {

        }

        // Loadout Sub Page

        public void ShowLoadoutSubPage()
        {

        }

        public void ShowSelectedItem()
        {

        }

        public void BuySelectedItem()
        {

        }

        public void EquipSelectedItem()
        {

        }

        public void DequipSelectedItem()
        {

        }

        public void ForwardLoadoutSubPage()
        {

        }

        public void BackwardLoadoutSubPage()
        {

        }


        // Events

        public void OnXPChanged()
        {
            if (!Plugin.Instance.DBManager.PlayerData.TryGetValue(SteamID, out PlayerData data))
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
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "XPBarFill", spaces == 0 ? " " : new string(' ', spaces));
        }

        public void OnMusicChanged(bool isMusic)
        {
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, isMusic ? "KnobOn" : "KnobOff", true);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, isMusic ? "KnobOff" : "KnobOn", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SwitchOn", isMusic);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "Music", isMusic);

            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await Plugin.Instance.DBManager.ChangePlayerMusicAsync(SteamID, isMusic);
            });
        }
    }
}
