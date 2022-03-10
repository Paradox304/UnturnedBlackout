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

        public ELoadoutSubPage LoadoutSubPage { get; set; }
        public int LoadoutSubPageID { get; set; }
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
