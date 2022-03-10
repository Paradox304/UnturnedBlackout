using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnturnedBlackout.Database.Base;
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

        public ELoadoutTab LoadoutTab { get; set; }
        public int LoadoutTabPageID { get; set; }
        public object SelectedItemID { get; set; }

        public Dictionary<int, PageLoadout> LoadoutPages { get; set; }
        public Dictionary<int, PageGun> PistolPages { get; set; }
        public Dictionary<int, PageGun> SMGPages { get; set; }
        public Dictionary<int, PageGun> LMGPages { get; set; }
        public Dictionary<int, PageGun> ShotgunPages { get; set; }
        public Dictionary<int, PageGun> ARPages { get; set; }
        public Dictionary<int, PageGun> SniperPages { get; set; }
        public Dictionary<ushort, Dictionary<int, PageAttachment>> AttachmentPages { get; set; }
        public Dictionary<ushort, Dictionary<int, PageGunSkin>> GunSkinPages { get; set; }
        public Dictionary<int, PageKnife> KnifePages { get; set; }
        public Dictionary<ushort, Dictionary<int, PageKnifeSkin>> KnifeSkinPages { get; set; }
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
            BuildPistolPages();
            BuildSMGPages();
            BuildShotgunPages();
            BuildSniperPages();
            BuildLMGPages();
            BuildARPages();
            BuildGunSkinPages();
            BuildAttachmentPages();
            BuildKnifePages();
            BuildKnifeSkinPages();
            BuildPerkPages();
            BuildTacticalPages();
            BuildLethalPages();
            BuildCardPages();
            BuildGlovePages();
            BuildKillstreakPages();
        }

        public void BuildLoadoutPages()
        {
            Logging.Debug($"Creating loadout pages for {Player.CharacterName}, found {PlayerLoadout.Loadouts.Count} loadouts for player");
            LoadoutPages = new Dictionary<int, PageLoadout>();
            int index = 0;
            int page = 1;
            var loadouts = new Dictionary<int, Loadout>();

            foreach (var loadout in PlayerLoadout.Loadouts)
            {
                loadouts.Add(index, loadout.Value);
                if (index == 7)
                {
                    LoadoutPages.Add(page, new PageLoadout(page, loadouts));
                    index = 0;
                    page++;
                    loadouts = new Dictionary<int, Loadout>();
                    continue;
                }
                index++;
            }
            if (loadouts.Count != 0)
            {
                LoadoutPages.Add(page, new PageLoadout(page, loadouts));
            }
            Logging.Debug($"Created {LoadoutPages.Count} loadout pages for {Player.CharacterName}");
        }

        public void BuildPistolPages()
        {
            var guns = PlayerLoadout.Guns.Values.Where(k => k.Gun.GunType == EGun.PISTOL).ToList();
            var gunItems = new Dictionary<int, LoadoutGun>();
            PistolPages = new Dictionary<int, PageGun>();
            int index = 0;
            int page = 1;
            Logging.Debug($"Creating pistol pages for {Player.CharacterName}, found {guns.Count()} pistols");

            foreach (var gun in guns)
            {
                gunItems.Add(index, gun);
                if (index == 4)
                {
                    PistolPages.Add(page, new PageGun(page, gunItems));
                    index = 0;
                    page++;
                    gunItems = new Dictionary<int, LoadoutGun>();
                    continue;
                }
                index++;
            }
            if (gunItems.Count != 0)
            {
                PistolPages.Add(page, new PageGun(page, gunItems));
            }
            Logging.Debug($"Created {PistolPages.Count} pistol pages for {Player.CharacterName}");
        }

        public void BuildSMGPages()
        {
            var guns = PlayerLoadout.Guns.Values.Where(k => k.Gun.GunType == EGun.SUBMACHINE_GUNS).ToList();
            var gunItems = new Dictionary<int, LoadoutGun>();
            SMGPages = new Dictionary<int, PageGun>();
            int index = 0;
            int page = 1;
            Logging.Debug($"Creating SMG pages for {Player.CharacterName}, found {guns.Count()} SMGs");

            foreach (var gun in guns)
            {
                gunItems.Add(index, gun);
                if (index == 4)
                {
                    SMGPages.Add(page, new PageGun(page, gunItems));
                    index = 0;
                    page++;
                    gunItems = new Dictionary<int, LoadoutGun>();
                    continue;
                }
                index++;
            }
            if (gunItems.Count != 0)
            {
                SMGPages.Add(page, new PageGun(page, gunItems));
            }
            Logging.Debug($"Created {SMGPages.Count} SMG pages for {Player.CharacterName}");
        }

        public void BuildShotgunPages()
        {
            var guns = PlayerLoadout.Guns.Values.Where(k => k.Gun.GunType == EGun.SHOTGUNS).ToList();
            var gunItems = new Dictionary<int, LoadoutGun>();
            ShotgunPages = new Dictionary<int, PageGun>();
            int index = 0;
            int page = 1;
            Logging.Debug($"Creating shotgun pages for {Player.CharacterName}, found {guns.Count()} shotguns");

            foreach (var gun in guns)
            {
                gunItems.Add(index, gun);
                if (index == 4)
                {
                    ShotgunPages.Add(page, new PageGun(page, gunItems));
                    index = 0;
                    page++;
                    gunItems = new Dictionary<int, LoadoutGun>();
                    continue;
                }
                index++;
            }
            if (gunItems.Count != 0)
            {
                ShotgunPages.Add(page, new PageGun(page, gunItems));
            }
            Logging.Debug($"Created {ShotgunPages.Count} shotgun pages for {Player.CharacterName}");
        }

        public void BuildLMGPages()
        {
            var guns = PlayerLoadout.Guns.Values.Where(k => k.Gun.GunType == EGun.LIGHT_MACHINE_GUNS).ToList();
            var gunItems = new Dictionary<int, LoadoutGun>();
            LMGPages = new Dictionary<int, PageGun>();
            int index = 0;
            int page = 1;
            Logging.Debug($"Creating LMG pages for {Player.CharacterName}, found {guns.Count()} LMGs");

            foreach (var gun in guns)
            {
                gunItems.Add(index, gun);
                if (index == 4)
                {
                    LMGPages.Add(page, new PageGun(page, gunItems));
                    index = 0;
                    page++;
                    gunItems = new Dictionary<int, LoadoutGun>();
                    continue;
                }
                index++;
            }
            if (gunItems.Count != 0)
            {
                LMGPages.Add(page, new PageGun(page, gunItems));
            }
            Logging.Debug($"Created {LMGPages.Count} LMG pages for {Player.CharacterName}");
        }

        public void BuildARPages()
        {
            var guns = PlayerLoadout.Guns.Values.Where(k => k.Gun.GunType == EGun.ASSAULT_RIFLES).ToList();
            var gunItems = new Dictionary<int, LoadoutGun>();
            ARPages = new Dictionary<int, PageGun>();
            int index = 0;
            int page = 1;
            Logging.Debug($"Creating AR pages for {Player.CharacterName}, found {guns.Count()} ARs");

            foreach (var gun in guns)
            {
                gunItems.Add(index, gun);
                if (index == 4)
                {
                    ARPages.Add(page, new PageGun(page, gunItems));
                    index = 0;
                    page++;
                    gunItems = new Dictionary<int, LoadoutGun>();
                    continue;
                }
                index++;
            }
            if (gunItems.Count != 0)
            {
                ARPages.Add(page, new PageGun(page, gunItems));
            }
            Logging.Debug($"Created {ARPages.Count} AR pages for {Player.CharacterName}");
        }

        public void BuildSniperPages()
        {
            var guns = PlayerLoadout.Guns.Values.Where(k => k.Gun.GunType == EGun.SNIPER_RIFLES).ToList();
            var gunItems = new Dictionary<int, LoadoutGun>();
            PistolPages = new Dictionary<int, PageGun>();
            int index = 0;
            int page = 1;
            Logging.Debug($"Creating sniper pages for {Player.CharacterName}, found {guns.Count()} snipers");

            foreach (var gun in guns)
            {
                gunItems.Add(index, gun);
                if (index == 4)
                {
                    SniperPages.Add(page, new PageGun(page, gunItems));
                    index = 0;
                    page++;
                    gunItems = new Dictionary<int, LoadoutGun>();
                    continue;
                }
                index++;
            }
            if (gunItems.Count != 0)
            {
                SniperPages.Add(page, new PageGun(page, gunItems));
            }
            Logging.Debug($"Created {SniperPages.Count} sniper pages for {Player.CharacterName}");
        }

        public void BuildGunSkinPages()
        {
            GunSkinPages = new Dictionary<ushort, Dictionary<int, PageGunSkin>>();
            Logging.Debug($"Creating gun skin pages for {Player.CharacterName}, found {PlayerLoadout.GunSkinsSearchByGunID.Count} guns which have skins");
            foreach (var gun in PlayerLoadout.GunSkinsSearchByGunID)
            {
                Logging.Debug($"Creating gun skin pages for gun with id {gun.Key} for {Player.CharacterName}, found {gun.Value.Count} gun skins for that gun");
                int index = 0;
                int page = 1;
                var gunSkins = new Dictionary<int, GunSkin>();
                GunSkinPages.Add(gun.Key, new Dictionary<int, PageGunSkin>());
                foreach (var gunSkin in gun.Value)
                {
                    gunSkins.Add(index, gunSkin);
                    if (index == 4)
                    {
                        GunSkinPages[gun.Key].Add(page, new PageGunSkin(page, gunSkins));
                        gunSkins = new Dictionary<int, GunSkin>();
                        index = 0;
                        page++;
                    }
                    index++;
                }
                if (gunSkins.Count != 0)
                {
                    GunSkinPages[gun.Key].Add(page, new PageGunSkin(page, gunSkins));
                }
                Logging.Debug($"Created {GunSkinPages[gun.Key].Count} gun skin pages for gun with id {gun.Key} for {Player.CharacterName}");
            }
        }

        public void BuildAttachmentPages()
        {
            AttachmentPages = new Dictionary<ushort, Dictionary<int, PageAttachment>>();
            Logging.Debug($"Creating attachment pages for {Player.CharacterName}, found {PlayerLoadout.Guns.Count} guns");
            foreach (var gun in PlayerLoadout.Guns)
            {
                Logging.Debug($"Creating attachment pages for gun with id {gun.Key} for {Player.CharacterName}");
                int index = 0;
                int page = 1;
                var attachments = new Dictionary<int, LoadoutAttachment>();
                AttachmentPages.Add(gun.Key, new Dictionary<int, PageAttachment>());
                foreach (var attachment in gun.Value.Attachments)
                {
                    attachments.Add(index, attachment.Value);
                    if (index == 4)
                    {
                        AttachmentPages[gun.Key].Add(page, new PageAttachment(page, attachments));
                        attachments = new Dictionary<int, LoadoutAttachment>();
                        index = 0;
                        page++;
                    }
                    index++;
                }
                if (attachments.Count != 0)
                {
                    AttachmentPages[gun.Key].Add(page, new PageAttachment(page, attachments));
                }
                Logging.Debug($"Created {AttachmentPages[gun.Key].Count} attachment pages for gun with id {gun.Key} for {Player.CharacterName}");
            }
        }

        public void BuildKnifePages()
        {
            KnifePages = new Dictionary<int, PageKnife>();
            Logging.Debug($"Creating knife pages for {Player.CharacterName}, found {PlayerLoadout.Knives.Count} knives");
            int index = 0;
            int page = 1;
            var knives = new Dictionary<int, LoadoutKnife>();
            foreach (var knife in PlayerLoadout.Knives)
            {
                knives.Add(index, knife.Value);
                if (index == 4)
                {
                    KnifePages.Add(page, new PageKnife(page, knives));
                    knives = new Dictionary<int, LoadoutKnife>();
                    index = 0;
                    page++;
                }
            }
            if (knives.Count != 0)
            {
                KnifePages.Add(page, new PageKnife(page, knives));
            }
            Logging.Debug($"Created {KnifePages.Count} knife pages for {Player.CharacterName}");
        }

        public void BuildKnifeSkinPages()
        {
            KnifeSkinPages = new Dictionary<ushort, Dictionary<int, PageKnifeSkin>>();
            Logging.Debug($"Creating knife skin pages for {Player.CharacterName}, found {PlayerLoadout.KnifeSkinsSearchByKnifeID.Count} knives which have skins");
            foreach (var knife in PlayerLoadout.KnifeSkinsSearchByKnifeID)
            {
                Logging.Debug($"Creating knife skin pages for knife with id {knife.Key} for {Player.CharacterName}, found {knife.Value.Count} knife skins for that knife");
                int index = 0;
                int page = 1;
                var knifeSkins = new Dictionary<int, KnifeSkin>();
                KnifeSkinPages.Add(knife.Key, new Dictionary<int, PageKnifeSkin>());
                foreach (var knifeSkin in knife.Value)
                {
                    knifeSkins.Add(index, knifeSkin);
                    if (index == 4)
                    {
                        KnifeSkinPages[knife.Key].Add(page, new PageKnifeSkin(page, knifeSkins));
                        knifeSkins = new Dictionary<int, KnifeSkin>();
                        index = 0;
                        page++;
                    }
                    index++;
                }
                if (knifeSkins.Count != 0)
                {
                    KnifeSkinPages[knife.Key].Add(page, new PageKnifeSkin(page, knifeSkins));
                }
                Logging.Debug($"Created {GunSkinPages[knife.Key].Count} knife skin pages for knife with id {knife.Key} for {Player.CharacterName}");
            }
        }

        public void BuildPerkPages()
        {
            PerkPages = new Dictionary<int, PagePerk>();
            Logging.Debug($"Creating perk pages for {Player.CharacterName}, found {PlayerLoadout.Perks.Count} perks");
            int index = 0;
            int page = 1;
            var perks = new Dictionary<int, LoadoutPerk>();
            foreach (var perk in PlayerLoadout.Perks)
            {
                perks.Add(index, perk.Value);
                if (index == 4)
                {
                    PerkPages.Add(page, new PagePerk(page, perks));
                    perks = new Dictionary<int, LoadoutPerk>();
                    index = 0;
                    page++;
                }
            }
            if (perks.Count != 0)
            {
                PerkPages.Add(page, new PagePerk(page, perks));
            }
            Logging.Debug($"Created {PerkPages.Count} perk pages for {Player.CharacterName}");
        }

        public void BuildTacticalPages()
        {
            TacticalPages = new Dictionary<int, PageGadget>();
            var gadgets = PlayerLoadout.Gadgets.Values.Where(k => k.Gadget.IsTactical).ToList();
            Logging.Debug($"Creating tactical pages for {Player.CharacterName}, found {gadgets.Count} tacticals");
            int index = 0;
            int page = 1;
            var gadgetItems = new Dictionary<int, LoadoutGadget>();
            foreach (var gadget in gadgets)
            {
                gadgetItems.Add(index, gadget);
                if (index == 4)
                {
                    TacticalPages.Add(page, new PageGadget(page, gadgetItems));
                    gadgetItems = new Dictionary<int, LoadoutGadget>();
                    index = 0;
                    page++;
                }
            }
            if (gadgetItems.Count != 0)
            {
                TacticalPages.Add(page, new PageGadget(page, gadgetItems));
            }
            Logging.Debug($"Created {TacticalPages.Count} tactical pages for {Player.CharacterName}");
        }

        public void BuildLethalPages()
        {
            LethalPages = new Dictionary<int, PageGadget>();
            var gadgets = PlayerLoadout.Gadgets.Values.Where(k => !k.Gadget.IsTactical).ToList();
            Logging.Debug($"Creating lethal pages for {Player.CharacterName}, found {gadgets.Count} lethals");
            int index = 0;
            int page = 1;
            var gadgetItems = new Dictionary<int, LoadoutGadget>();
            foreach (var gadget in gadgets)
            {
                gadgetItems.Add(index, gadget);
                if (index == 4)
                {
                    LethalPages.Add(page, new PageGadget(page, gadgetItems));
                    gadgetItems = new Dictionary<int, LoadoutGadget>();
                    index = 0;
                    page++;
                }
            }
            if (gadgetItems.Count != 0)
            {
                LethalPages.Add(page, new PageGadget(page, gadgetItems));
            }
            Logging.Debug($"Created {LethalPages.Count} lethal pages for {Player.CharacterName}");
        }

        public void BuildCardPages()
        {
            CardPages = new Dictionary<int, PageCard>();
            Logging.Debug($"Creating card pages for {Player.CharacterName}, found {PlayerLoadout.Cards.Count} cards");
            int index = 0;
            int page = 1;
            var cards = new Dictionary<int, LoadoutCard>();
            foreach (var card in PlayerLoadout.Cards)
            {
                cards.Add(index, card.Value);
                if (index == 4)
                {
                    CardPages.Add(page, new PageCard(page, cards));
                    cards = new Dictionary<int, LoadoutCard>();
                    index = 0;
                    page++;
                }
            }
            if (cards.Count != 0)
            {
                CardPages.Add(page, new PageCard(page, cards));
            }
            Logging.Debug($"Created {CardPages.Count} card pages for {Player.CharacterName}");
        }

        public void BuildGlovePages()
        {
            GlovePages = new Dictionary<int, PageGlove>();
            Logging.Debug($"Creating glove pages for {Player.CharacterName}, found {PlayerLoadout.Cards.Count} gloves");
            int index = 0;
            int page = 1;
            var gloves = new Dictionary<int, LoadoutGlove>();
            foreach (var glove in PlayerLoadout.Gloves)
            {
                gloves.Add(index, glove.Value);
                if (index == 4)
                {
                    GlovePages.Add(page, new PageGlove(page, gloves));
                    gloves = new Dictionary<int, LoadoutGlove>();
                    index = 0;
                    page++;
                }
            }
            if (gloves.Count != 0)
            {
                GlovePages.Add(page, new PageGlove(page, gloves));
            }
            Logging.Debug($"Created {GlovePages.Count} glove pages for {Player.CharacterName}");
        }

        public void BuildKillstreakPages()
        {
            KillstreakPages = new Dictionary<int, PageKillstreak>();
            Logging.Debug($"Creating killstreak pages for {Player.CharacterName}, found {PlayerLoadout.Killstreaks.Count} killstreaks");
            int index = 0;
            int page = 1;
            var killstreaks = new Dictionary<int, LoadoutKillstreak>();
            foreach (var killstreak in PlayerLoadout.Killstreaks)
            {
                killstreaks.Add(index, killstreak.Value);
                if (index == 4)
                {
                    KillstreakPages.Add(page, new PageKillstreak(page, killstreaks));
                    killstreaks = new Dictionary<int, LoadoutKillstreak>();
                    index = 0;
                    page++;
                }
            }
            if (killstreaks.Count != 0)
            {
                KillstreakPages.Add(page, new PageKillstreak(page, killstreaks));
            }
            Logging.Debug($"Created {KillstreakPages.Count} killstreak pages for {Player.CharacterName}");
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
            Logging.Debug($"Showing loadouts to {Player.CharacterName}");
            MainPage = EMainPage.Leaderboard;

            // Code to turn off all side objects for loadout page
            if (!LoadoutPages.TryGetValue(1, out PageLoadout firstPage))
            {
                Logging.Debug($"Error finding first page of loadouts for {Player.CharacterName}");
                LoadoutPageID = 0;
                // Code to turn off the page forward and backward button
                return;
            }

            ShowLoadoutPage(firstPage);
        }

        public void ShowLoadoutPage(PageLoadout page)
        {
            LoadoutPageID = page.PageID;

            // Code to send all the UI objects
        }

        public void ForwardLoadoutPage()
        {
            if (LoadoutPageID == 0)
            {
                return;
            }
            Logging.Debug($"Forwarding loadout page for {Player.CharacterName}, Current Page {LoadoutPageID}");

            if (!LoadoutPages.TryGetValue(LoadoutPageID + 1, out PageLoadout nextPage) && !LoadoutPages.TryGetValue(1, out nextPage))
            {
                ShowLoadouts();
                return;
            }

            ShowLoadoutPage(nextPage);
        }

        public void BackwardLoadoutPage()
        {
            if (LoadoutPageID == 0)
            {
                return;
            }
            Logging.Debug($"Backwarding loadout page for {Player.CharacterName}, Current Page {LoadoutPageID}");

            if (!LoadoutPages.TryGetValue(LoadoutPageID - 1, out PageLoadout prevPage) && !LoadoutPages.TryGetValue(LoadoutPages.Keys.Max(), out prevPage))
            {
                ShowLoadouts();
                return;
            }

            ShowLoadoutPage(prevPage);
        }

        public void ReloadLoadout()
        {
            Logging.Debug($"Reloading loadout for {Player.CharacterName}");
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Couldnt find the current selected loadout");
                return;
            }

            ShowLoadout(currentLoadout);
        }

        public void SelectedLoadout(int selected)
        {
            Logging.Debug($"{Player.CharacterName} selected loadout at {selected}");
            if (!LoadoutPages.TryGetValue(LoadoutPageID, out PageLoadout currentPage))
            {
                Logging.Debug($"Couldnt find the current selected page at {LoadoutPageID}");
                return;
            }

            if (!currentPage.Loadouts.TryGetValue(selected, out Loadout currentLoadout))
            {
                Logging.Debug($"Couldnt find the selected loadout at {selected}");
                return;
            }

            ShowLoadout(currentLoadout);
        }

        public void ShowLoadout(Loadout loadout)
        {
            LoadoutID = loadout.LoadoutID;

            // Code to show the objects
        }

        // Loadout Sub Page

        public void ShowLoadoutSubPage(ELoadoutPage page)
        {
            Logging.Debug($"Showing loadout sub page {page} for {Player.CharacterName}");
            LoadoutPage = page;
            switch (page)
            {
                case ELoadoutPage.Primary:
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item ARs Button", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Pistols Button", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item SMGs Button", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item SRs Button", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Shotguns Button", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item LMGs Button", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Skins Button", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item All Button", false);
                    break;
                case ELoadoutPage.Secondary:
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item ARs Button", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Pistols Button", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item SMGs Button", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item SRs Button", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Shotguns Button", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item LMGs Button", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Skins Button", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item All Button", false);
                    break;
                case ELoadoutPage.Knife:
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item ARs Button", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Pistols Button", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item SMGs Button", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item SRs Button", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Shotguns Button", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item LMGs Button", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Skins Button", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item All Button", true);
                    break;
                default:
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item ARs Button", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Pistols Button", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item SMGs Button", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item SRs Button", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Shotguns Button", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item LMGs Button", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Skins Button", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item All Button", true);
                    break;
            }
        }

        public void ShowLoadoutTab(ELoadoutTab tab)
        {
            Logging.Debug($"Showing loadout tab {tab} for {Player.CharacterName}");
            LoadoutTab = tab;
            
            if (LoadoutTab == ELoadoutTab.ALL)
            {
                if (LoadoutPage == ELoadoutPage.PrimaryAttachment)
                {
                    if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
                    {
                        Logging.Debug($"Error finding the selected loadout with id {LoadoutID} for {Player.CharacterName}");
                        LoadoutTabPageID = 0;
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    if (!AttachmentPages.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out Dictionary<int, PageAttachment> attachmentPages))
                    {
                        Logging.Debug($"Error finding the primary attachment pages for gun with id {loadout.Primary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                        LoadoutTabPageID = 0;
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    if (!attachmentPages.TryGetValue(1, out PageAttachment firstPage))
                    {
                        Logging.Debug($"Error finding the first attachment page for gun with id {loadout.Primary.Gun.GunID} for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowAttachmentPage(firstPage);
                }
                else if (LoadoutPage == ELoadoutPage.SecondaryAttachment)
                {
                    if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
                    {
                        Logging.Debug($"Error finding the selected loadout with id {LoadoutID} for {Player.CharacterName}");
                        LoadoutTabPageID = 0;
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    if (!AttachmentPages.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out Dictionary<int, PageAttachment> attachmentPages))
                    {
                        Logging.Debug($"Error finding the secondary attachment pages for gun with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                        LoadoutTabPageID = 0;
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    if (!attachmentPages.TryGetValue(1, out PageAttachment firstPage))
                    {
                        Logging.Debug($"Error finding the first attachment page for gun with id {loadout.Primary.Gun.GunID} for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowAttachmentPage(firstPage);
                }
                else if (LoadoutPage == ELoadoutPage.Perk)
                {
                    if (!PerkPages.TryGetValue(1, out PagePerk firstPage))
                    {
                        Logging.Debug($"Error getting first page for perks for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);

                    ShowPerkPage(firstPage);
                }
                else if (LoadoutPage == ELoadoutPage.Lethal)
                {
                    if (!LethalPages.TryGetValue(1, out PageGadget firstPage))
                    {
                        Logging.Debug($"Error finding the first page for lethals for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowGadgetPage(firstPage);
                }
                else if (LoadoutPage == ELoadoutPage.Tactical)
                {
                    if (!TacticalPages.TryGetValue(1, out PageGadget firstPage))
                    {
                        Logging.Debug($"Error finding the first page for tacticals for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowGadgetPage(firstPage);
                }
                else if (LoadoutPage == ELoadoutPage.Knife)
                {
                    if (!KnifePages.TryGetValue(1, out PageKnife firstPage))
                    {
                        Logging.Debug($"Error finding the first page for knives for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowKnifePage(firstPage);
                }
                else if (LoadoutPage == ELoadoutPage.Killstreak)
                {
                    if (!KillstreakPages.TryGetValue(1, out PageKillstreak firstPage))
                    {
                        Logging.Debug($"Error finding the first page for killstreaks for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowKillstreakPage(firstPage);
                }
                else if (LoadoutPage == ELoadoutPage.Glove)
                {
                    if (!GlovePages.TryGetValue(1, out PageGlove firstPage))
                    {
                        Logging.Debug($"Error finding the first page for gloves for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowGlovePage(firstPage);
                }
                else if (LoadoutPage == ELoadoutPage.Card)
                {
                    if (!CardPages.TryGetValue(1, out PageCard firstPage))
                    {
                        Logging.Debug($"Error finding the first page for cards for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowCardPage(firstPage);
                }
            }
            else if (LoadoutTab == ELoadoutTab.SKINS)
            {
                if (LoadoutPage == ELoadoutPage.Primary)
                {
                    if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
                    {
                        Logging.Debug($"Error getting current loadout with id {LoadoutID} for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    if (!GunSkinPages.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out Dictionary<int, PageGunSkin> gunSkinPages))
                    {
                        Logging.Debug($"Error getting gun skin pages for gun with id {loadout.Primary?.Gun?.GunID ?? 0}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    if (!gunSkinPages.TryGetValue(1, out PageGunSkin firstPage))
                    {
                        Logging.Debug($"Error finding the first page for gun skins for gun with id {loadout.Primary.Gun.GunID} for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowGunSkinPage(firstPage);
                }
                else if (LoadoutPage == ELoadoutPage.Secondary)
                {
                    if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
                    {
                        Logging.Debug($"Error getting current loadout with id {LoadoutID} for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    if (!GunSkinPages.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out Dictionary<int, PageGunSkin> gunSkinPages))
                    {
                        Logging.Debug($"Error getting gun skin pages for gun with id {loadout.Secondary?.Gun?.GunID ?? 0}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    if (!gunSkinPages.TryGetValue(1, out PageGunSkin firstPage))
                    {
                        Logging.Debug($"Error finding the first page for gun skins for gun with id {loadout.Secondary.Gun.GunID} for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowGunSkinPage(firstPage);
                }
                else if (LoadoutPage == ELoadoutPage.Knife)
                {
                    if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
                    {
                        Logging.Debug($"Error getting current loadout with id {LoadoutID} for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    if (!KnifeSkinPages.TryGetValue(loadout.Knife?.Knife?.KnifeID ?? 0, out Dictionary<int, PageKnifeSkin> knifeSkinPages))
                    {
                        Logging.Debug($"Error getting gun skin pages for knife with id {loadout.Knife?.Knife?.KnifeID ?? 0}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    if (!knifeSkinPages.TryGetValue(1, out PageKnifeSkin firstPage))
                    {
                        Logging.Debug($"Error finding the first page for gun skins for gun with id {loadout.Knife.Knife.KnifeID} for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowKnifeSkinPage(firstPage);
                }
            }
            else if (LoadoutTab == ELoadoutTab.PISTOLS)
            {
                if (!PistolPages.TryGetValue(1, out PageGun firstPage))
                {
                    Logging.Debug($"Error finding first page for pistols for {Player.CharacterName}");
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                    return;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                ShowGunPage(firstPage);
            }
            else if (LoadoutTab == ELoadoutTab.SUBMACHINE_GUNS)
            {
                if (!SMGPages.TryGetValue(1, out PageGun firstPage))
                {
                    Logging.Debug($"Error finding first page for smgs for {Player.CharacterName}");
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                    return;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                ShowGunPage(firstPage);
            }
            else if (LoadoutTab == ELoadoutTab.SHOTGUNS)
            {
                if (!ShotgunPages.TryGetValue(1, out PageGun firstPage))
                {
                    Logging.Debug($"Error finding first page for shotguns for {Player.CharacterName}");
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                    return;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                ShowGunPage(firstPage);
            }
            else if (LoadoutTab == ELoadoutTab.LIGHT_MACHINE_GUNS)
            {
                if (!LMGPages.TryGetValue(1, out PageGun firstPage))
                {
                    Logging.Debug($"Error finding first page for lmgs for {Player.CharacterName}");
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                    return;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                ShowGunPage(firstPage);
            }
            else if (LoadoutTab == ELoadoutTab.ASSAULT_RIFLES)
            {
                if (!ARPages.TryGetValue(1, out PageGun firstPage))
                {
                    Logging.Debug($"Error finding first page for ARs for {Player.CharacterName}");
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                    return;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                ShowGunPage(firstPage);
            }
            else if (LoadoutTab == ELoadoutTab.SNIPER_RIFLES)
            {
                if (!SniperPages.TryGetValue(1, out PageGun firstPage))
                {
                    Logging.Debug($"Error finding first page for snipers for {Player.CharacterName}");
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                    return;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                ShowGunPage(firstPage);
            }
        }

        public void ForwardLoadoutTab()
        {
            Logging.Debug($"{Player.CharacterName} is trying to forward the loadout tab {LoadoutTab}, Current Page {LoadoutTabPageID}");
            if (LoadoutTab == ELoadoutTab.ALL)
            {
                if (LoadoutPage == ELoadoutPage.PrimaryAttachment)
                {
                    if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
                    {
                        Logging.Debug($"Error finding the selected loadout with id {LoadoutID} for {Player.CharacterName}");
                        return;
                    }

                    if (!AttachmentPages.TryGetValue(loadout.Primary.Gun.GunID, out Dictionary<int, PageAttachment> attachmentPages))
                    {
                        Logging.Debug($"Error finding the primary attachment pages for gun with id {loadout.Primary.Gun.GunID} for {Player.CharacterName}");
                        return;
                    }

                    if (!attachmentPages.TryGetValue(LoadoutTabPageID + 1, out PageAttachment nextPage) && !attachmentPages.TryGetValue(1, out nextPage))
                    {
                        Logging.Debug($"Error finding the next attachment page for gun with id {loadout.Primary.Gun.GunID} for {Player.CharacterName}");
                        return;
                    }

                    ShowAttachmentPage(nextPage);
                }
                else if (LoadoutPage == ELoadoutPage.SecondaryAttachment)
                {
                    if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
                    {
                        Logging.Debug($"Error finding the selected loadout with id {LoadoutID} for {Player.CharacterName}");
                        LoadoutTabPageID = 0;
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    if (!AttachmentPages.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out Dictionary<int, PageAttachment> attachmentPages))
                    {
                        Logging.Debug($"Error finding the secondary attachment pages for gun with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                        LoadoutTabPageID = 0;
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    if (!attachmentPages.TryGetValue(1, out PageAttachment firstPage))
                    {
                        Logging.Debug($"Error finding the first attachment page for gun with id {loadout.Primary.Gun.GunID} for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowAttachmentPage(firstPage);
                }
                else if (LoadoutPage == ELoadoutPage.Perk)
                {
                    if (!PerkPages.TryGetValue(1, out PagePerk firstPage))
                    {
                        Logging.Debug($"Error getting first page for perks for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);

                    ShowPerkPage(firstPage);
                }
                else if (LoadoutPage == ELoadoutPage.Lethal)
                {
                    if (!LethalPages.TryGetValue(1, out PageGadget firstPage))
                    {
                        Logging.Debug($"Error finding the first page for lethals for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowGadgetPage(firstPage);
                }
                else if (LoadoutPage == ELoadoutPage.Tactical)
                {
                    if (!TacticalPages.TryGetValue(1, out PageGadget firstPage))
                    {
                        Logging.Debug($"Error finding the first page for tacticals for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowGadgetPage(firstPage);
                }
                else if (LoadoutPage == ELoadoutPage.Knife)
                {
                    if (!KnifePages.TryGetValue(1, out PageKnife firstPage))
                    {
                        Logging.Debug($"Error finding the first page for knives for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowKnifePage(firstPage);
                }
                else if (LoadoutPage == ELoadoutPage.Killstreak)
                {
                    if (!KillstreakPages.TryGetValue(1, out PageKillstreak firstPage))
                    {
                        Logging.Debug($"Error finding the first page for killstreaks for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowKillstreakPage(firstPage);
                }
                else if (LoadoutPage == ELoadoutPage.Glove)
                {
                    if (!GlovePages.TryGetValue(1, out PageGlove firstPage))
                    {
                        Logging.Debug($"Error finding the first page for gloves for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowGlovePage(firstPage);
                }
                else if (LoadoutPage == ELoadoutPage.Card)
                {
                    if (!CardPages.TryGetValue(1, out PageCard firstPage))
                    {
                        Logging.Debug($"Error finding the first page for cards for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowCardPage(firstPage);
                }
            }
            else if (LoadoutTab == ELoadoutTab.SKINS)
            {
                if (LoadoutPage == ELoadoutPage.Primary)
                {
                    if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
                    {
                        Logging.Debug($"Error getting current loadout with id {LoadoutID} for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    if (!GunSkinPages.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out Dictionary<int, PageGunSkin> gunSkinPages))
                    {
                        Logging.Debug($"Error getting gun skin pages for gun with id {loadout.Primary?.Gun?.GunID ?? 0}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    if (!gunSkinPages.TryGetValue(1, out PageGunSkin firstPage))
                    {
                        Logging.Debug($"Error finding the first page for gun skins for gun with id {loadout.Primary.Gun.GunID} for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowGunSkinPage(firstPage);
                }
                else if (LoadoutPage == ELoadoutPage.Secondary)
                {
                    if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
                    {
                        Logging.Debug($"Error getting current loadout with id {LoadoutID} for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    if (!GunSkinPages.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out Dictionary<int, PageGunSkin> gunSkinPages))
                    {
                        Logging.Debug($"Error getting gun skin pages for gun with id {loadout.Secondary?.Gun?.GunID ?? 0}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    if (!gunSkinPages.TryGetValue(1, out PageGunSkin firstPage))
                    {
                        Logging.Debug($"Error finding the first page for gun skins for gun with id {loadout.Secondary.Gun.GunID} for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowGunSkinPage(firstPage);
                }
                else if (LoadoutPage == ELoadoutPage.Knife)
                {
                    if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
                    {
                        Logging.Debug($"Error getting current loadout with id {LoadoutID} for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    if (!KnifeSkinPages.TryGetValue(loadout.Knife?.Knife?.KnifeID ?? 0, out Dictionary<int, PageKnifeSkin> knifeSkinPages))
                    {
                        Logging.Debug($"Error getting gun skin pages for knife with id {loadout.Knife?.Knife?.KnifeID ?? 0}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    if (!knifeSkinPages.TryGetValue(1, out PageKnifeSkin firstPage))
                    {
                        Logging.Debug($"Error finding the first page for gun skins for gun with id {loadout.Knife.Knife.KnifeID} for {Player.CharacterName}");
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                        return;
                    }

                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                    ShowKnifeSkinPage(firstPage);
                }
            }
            else if (LoadoutTab == ELoadoutTab.PISTOLS)
            {
                if (!PistolPages.TryGetValue(1, out PageGun firstPage))
                {
                    Logging.Debug($"Error finding first page for pistols for {Player.CharacterName}");
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                    return;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                ShowGunPage(firstPage);
            }
            else if (LoadoutTab == ELoadoutTab.SUBMACHINE_GUNS)
            {
                if (!SMGPages.TryGetValue(1, out PageGun firstPage))
                {
                    Logging.Debug($"Error finding first page for smgs for {Player.CharacterName}");
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                    return;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                ShowGunPage(firstPage);
            }
            else if (LoadoutTab == ELoadoutTab.SHOTGUNS)
            {
                if (!ShotgunPages.TryGetValue(1, out PageGun firstPage))
                {
                    Logging.Debug($"Error finding first page for shotguns for {Player.CharacterName}");
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                    return;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                ShowGunPage(firstPage);
            }
            else if (LoadoutTab == ELoadoutTab.LIGHT_MACHINE_GUNS)
            {
                if (!LMGPages.TryGetValue(1, out PageGun firstPage))
                {
                    Logging.Debug($"Error finding first page for lmgs for {Player.CharacterName}");
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                    return;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                ShowGunPage(firstPage);
            }
            else if (LoadoutTab == ELoadoutTab.ASSAULT_RIFLES)
            {
                if (!ARPages.TryGetValue(1, out PageGun firstPage))
                {
                    Logging.Debug($"Error finding first page for ARs for {Player.CharacterName}");
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                    return;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                ShowGunPage(firstPage);
            }
            else if (LoadoutTab == ELoadoutTab.SNIPER_RIFLES)
            {
                if (!SniperPages.TryGetValue(1, out PageGun firstPage))
                {
                    Logging.Debug($"Error finding first page for snipers for {Player.CharacterName}");
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", " ");
                    return;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                ShowGunPage(firstPage);
            }
        }

        public void BackwardLoadoutTab()
        {

        }

        public void ShowGunPage(PageGun page)
        {

        }

        public void ShowAttachmentPage(PageAttachment page)
        {

        }

        public void ShowGunSkinPage(PageGunSkin page)
        {

        }

        public void ShowKnifePage(PageKnife page)
        {

        }

        public void ShowKnifeSkinPage(PageKnifeSkin page)
        {

        }

        public void ShowPerkPage(PagePerk page)
        {

        } 

        public void ShowGadgetPage(PageGadget page)
        {

        }

        public void ShowCardPage(PageCard page)
        {

        }

        public void ShowGlovePage(PageGlove page)
        {

        }

        public void ShowKillstreakPage(PageKillstreak page)
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
