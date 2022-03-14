using Rocket.Core.Utils;
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
        public PlayerData PlayerData { get; set; }

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
        public Dictionary<ushort, Dictionary<EAttachment, Dictionary<int, PageAttachment>>> AttachmentPages { get; set; }
        public Dictionary<int, PageGunCharm> GunCharmPages { get; set; }
        public Dictionary<ushort, Dictionary<int, PageGunSkin>> GunSkinPages { get; set; }
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
            if (!DB.PlayerData.TryGetValue(player.CSteamID, out PlayerData data))
            {
                Logging.Debug($"Error finding player data for {player.CharacterName}, failed to initialize UIHandler for player");
                return;
            }

            PlayerData = data;
            PlayerLoadout = loadout;
            BuildPages();
            ResetUIValues();
        }

        public void ShowUI()
        {
            EffectManager.sendUIEffect(ID, Key, TransportConnection, true);
            Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);
            ResetUIValues();
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

            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Player Icon", data.AvatarLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Player Name", data.SteamName);

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
            BuildGunCharmPages();
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
            var guns = PlayerLoadout.Guns.Values.Where(k => k.Gun.GunType == EGun.PISTOL).OrderBy(k => k.Gun.LevelRequirement).ToList();
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
            var guns = PlayerLoadout.Guns.Values.Where(k => k.Gun.GunType == EGun.SUBMACHINE_GUNS).OrderBy(k => k.Gun.LevelRequirement).ToList();
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
            var guns = PlayerLoadout.Guns.Values.Where(k => k.Gun.GunType == EGun.SHOTGUNS).OrderBy(k => k.Gun.LevelRequirement).ToList();
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
            var guns = PlayerLoadout.Guns.Values.Where(k => k.Gun.GunType == EGun.LIGHT_MACHINE_GUNS).OrderBy(k => k.Gun.LevelRequirement).ToList();
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
            var guns = PlayerLoadout.Guns.Values.Where(k => k.Gun.GunType == EGun.ASSAULT_RIFLES).OrderBy(k => k.Gun.LevelRequirement).ToList();
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
            var guns = PlayerLoadout.Guns.Values.Where(k => k.Gun.GunType == EGun.SNIPER_RIFLES).OrderBy(k => k.Gun.LevelRequirement).ToList();
            var gunItems = new Dictionary<int, LoadoutGun>();
            SniperPages = new Dictionary<int, PageGun>();
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
                        continue;
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
            AttachmentPages = new Dictionary<ushort, Dictionary<EAttachment, Dictionary<int, PageAttachment>>>();
            Logging.Debug($"Creating attachment pages for {Player.CharacterName}, found {PlayerLoadout.Guns.Count} guns");
            foreach (var gun in PlayerLoadout.Guns)
            {
                BuildAttachmentPages(gun.Value);
            }
        }

        public void BuildAttachmentPages(LoadoutGun gun)
        {
            Logging.Debug($"Creating attachment pages for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
            if (AttachmentPages.ContainsKey(gun.Gun.GunID))
            {
                AttachmentPages.Remove(gun.Gun.GunID);
            }
            AttachmentPages.Add(gun.Gun.GunID, new Dictionary<EAttachment, Dictionary<int, PageAttachment>>());

            for (int i = 0; i <= 3; i++)
            {
                var attachmentType = (EAttachment)i;
                Logging.Debug($"Creating {attachmentType} pages");
                int index = 0;
                int page = 1;
                var attachments = new Dictionary<int, LoadoutAttachment>();
                AttachmentPages[gun.Gun.GunID].Add(attachmentType, new Dictionary<int, PageAttachment>());

                foreach (var attachment in gun.Attachments.Values.Where(k => k.Attachment.AttachmentType == attachmentType).OrderBy(k => k.LevelRequirement))
                {
                    attachments.Add(index, attachment);
                    if (index == 4)
                    {
                        AttachmentPages[gun.Gun.GunID][attachmentType].Add(page, new PageAttachment(page, attachments));
                        index = 0;
                        page++;
                        continue;
                    }
                    index++;
                }
                if (attachments.Count != 0)
                {
                    AttachmentPages[gun.Gun.GunID][attachmentType].Add(page, new PageAttachment(page, attachments));
                }
                Logging.Debug($"Created {AttachmentPages[gun.Gun.GunID][attachmentType].Count} {attachmentType} pages for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
            }
        }

        public void BuildGunCharmPages()
        {
            GunCharmPages = new Dictionary<int, PageGunCharm>();
            Logging.Debug($"Creating gun charm pages for {Player.CharacterName}, found {PlayerLoadout.GunCharms.Count} gun charms");
            int index = 0;
            int page = 1;
            var gunCharms = new Dictionary<int, LoadoutGunCharm>();
            foreach (var gunCharm in PlayerLoadout.GunCharms.Values.OrderBy(k => k.GunCharm.LevelRequirement))
            {
                gunCharms.Add(index, gunCharm);
                if (index == 4)
                {
                    GunCharmPages.Add(page, new PageGunCharm(page, gunCharms));
                    gunCharms = new Dictionary<int, LoadoutGunCharm>();
                    index = 0;
                    page++;
                }
            }
            if (gunCharms.Count != 0)
            {
                GunCharmPages.Add(page, new PageGunCharm(page, gunCharms));
            }
            Logging.Debug($"Created {GunCharmPages.Count} gun charm pages for {Player.CharacterName}");
        }

        public void BuildKnifePages()
        {
            KnifePages = new Dictionary<int, PageKnife>();
            Logging.Debug($"Creating knife pages for {Player.CharacterName}, found {PlayerLoadout.Knives.Count} knives");
            int index = 0;
            int page = 1;
            var knives = new Dictionary<int, LoadoutKnife>();
            foreach (var knife in PlayerLoadout.Knives.Values.OrderBy(k => k.Knife.LevelRequirement))
            {
                knives.Add(index, knife);
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

        public void BuildPerkPages()
        {
            PerkPages = new Dictionary<int, PagePerk>();
            Logging.Debug($"Creating perk pages for {Player.CharacterName}, found {PlayerLoadout.Perks.Count} perks");
            int index = 0;
            int page = 1;
            var perks = new Dictionary<int, LoadoutPerk>();
            foreach (var perk in PlayerLoadout.Perks.Values.OrderBy(k => k.Perk.LevelRequirement))
            {
                perks.Add(index, perk);
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
            var gadgets = PlayerLoadout.Gadgets.Values.Where(k => k.Gadget.IsTactical).OrderBy(k => k.Gadget.LevelRequirement).ToList();
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
            var gadgets = PlayerLoadout.Gadgets.Values.Where(k => !k.Gadget.IsTactical).OrderBy(k => k.Gadget.LevelRequirement).ToList();
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
            foreach (var card in PlayerLoadout.Cards.Values.OrderBy(k => k.Card.LevelRequirement))
            {
                cards.Add(index, card);
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
            foreach (var glove in PlayerLoadout.Gloves.Values.OrderBy(k => k.Glove.LevelRequirement))
            {
                gloves.Add(index, glove);
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
            foreach (var killstreak in PlayerLoadout.Killstreaks.Values.OrderBy(k => k.Killstreak.LevelRequirement))
            {
                killstreaks.Add(index, killstreak);
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

            if (!LoadoutPages.TryGetValue(1, out PageLoadout firstPage))
            {
                Logging.Debug($"Error finding first page of loadouts for {Player.CharacterName}");
                LoadoutPageID = 0;
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Loadout Next BUTTON", false);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Loadout Previous Button", false);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Loadout Page TEXT", "");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Loadout Next BUTTON", true);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Loadout Previous Button", true);
            ShowLoadoutPage(firstPage);
            SelectedLoadout(0);
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

        public void ReloadLoadoutPage()
        {
            Logging.Debug($"Reloading loadout page for {Player.CharacterName}, Current Page {LoadoutPageID}");
            if (!LoadoutPages.TryGetValue(LoadoutPageID, out PageLoadout page))
            {
                Logging.Debug($"Error finding current loadout page with page id {LoadoutPageID} for {Player.CharacterName}");
                return;
            }

            ShowLoadoutPage(page);
        }

        public void ShowLoadoutPage(PageLoadout page)
        {
            Logging.Debug($"Showing loadout page for {Player.CharacterName} with id {page.PageID}");
            LoadoutPageID = page.PageID;

            for (int i = 0; i <= 7; i++)
            {
                if (!page.Loadouts.TryGetValue(i, out Loadout loadout))
                {
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Loadout BUTTON {i}", false);
                    continue;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Loadout BUTTON {i}", true);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Loadout TEXT {i}", loadout.LoadoutName);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Loadout Equipped {i}", loadout.IsActive);
            }
        }

        public void ReloadLoadout()
        {
            Logging.Debug($"Reloading loadout for {Player.CharacterName}, selected loadout {LoadoutID}");
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
            Logging.Debug($"Showing loadout with id {loadout.LoadoutID} for {Player.CharacterName}");
            LoadoutID = loadout.LoadoutID;

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Loadout Equip BUTTON", !loadout.IsActive);
            // Primary
            Logging.Debug($"Primary is null {loadout.Primary == null}, is skin null {loadout.SecondarySkin == null}");
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Loadout Primary IMAGE", loadout.PrimarySkin == null ? (loadout.Primary == null ? "" : loadout.Primary.Gun.IconLink) : loadout.PrimarySkin.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Loadout Primary TEXT", loadout.Primary == null ? "" : loadout.Primary.Gun.GunName);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Loadout Primary Level TEXT", loadout.Primary == null ? "" : loadout.Primary.Level.ToString());
            for (int i = 0; i <= 3; i++)
            {
                var attachmentType = (EAttachment)i;
                loadout.PrimaryAttachments.TryGetValue(attachmentType, out LoadoutAttachment attachment);
                Logging.Debug($"Primary attachment {attachmentType} is null {attachment == null}");
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Loadout Primary {attachmentType} IMAGE", attachment == null ? "" : attachment.Attachment.IconLink);
            }
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Loadout Primary Charm IMAGE", loadout.PrimaryGunCharm == null ? "" : loadout.PrimaryGunCharm.GunCharm.IconLink);
            
            // Secondary
            Logging.Debug($"Secondary is null {loadout.Secondary == null}, is skin null {loadout.SecondarySkin == null}");
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Loadout Secondary IMAGE", loadout.SecondarySkin == null ? (loadout.Secondary == null ? "" : loadout.Secondary.Gun.IconLink) : loadout.SecondarySkin.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Loadout Secondary TEXT", loadout.Secondary == null ? "" : loadout.Secondary.Gun.GunName);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Loadout Secondary Level TEXT", loadout.Secondary == null ? "" : loadout.Secondary.Level.ToString());
            for (int i = 0; i <= 3; i++)
            {
                var attachmentType = (EAttachment)i;
                loadout.SecondaryAttachments.TryGetValue(attachmentType, out LoadoutAttachment attachment);
                Logging.Debug($"Secondary attachment {attachmentType} is null {attachment == null}");
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Loadout Secondary {attachmentType} IMAGE", attachment == null ? "" : attachment.Attachment.IconLink);
            }
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Loadout Secondary Charm IMAGE", loadout.SecondaryGunCharm == null ? "" : loadout.SecondaryGunCharm.GunCharm.IconLink);
            
                // Knife
            Logging.Debug($"Knife is null {loadout.Knife == null}");
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Loadout Knife IMAGE", loadout.Knife == null ? "" : loadout.Knife.Knife.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Loadout Knife TEXT", loadout.Knife == null ? "" : loadout.Knife.Knife.KnifeName);

            // Tactical
            Logging.Debug($"Tactical is null {loadout.Tactical == null}");
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Loadout Tactical IMAGE", loadout.Tactical == null ? "" : loadout.Tactical.Gadget.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Loadout Tactical TEXT", loadout.Tactical == null ? "" : loadout.Tactical.Gadget.GadgetName);

            // Lethal
            Logging.Debug($"Lethal is null {loadout.Lethal == null}");
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Loadout Lethal IMAGE", loadout.Lethal == null ? "" : loadout.Lethal.Gadget.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Loadout Lethal TEXT", loadout.Lethal == null ? "" : loadout.Lethal.Gadget.GadgetName);

            // Perk
            for (int i = 0; i <= 2; i++)
            {
                Logging.Debug($"Perk at {i} is null {loadout.Perks.Count < (i + 1)}");
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Loadout Perk IMAGE {i}", loadout.Perks.Count < (i + 1) ? "" : loadout.Perks[i].Perk.IconLink);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Loadout Perk TEXT {i}", loadout.Perks.Count < (i + 1) ? "" : loadout.Perks[i].Perk.PerkName);
            }

            // Killstreak
            for (int i = 0; i <= 2; i++)
            {
                Logging.Debug($"Killstreak at {i} is null {loadout.Killstreaks.Count < (i + 1)}");
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Loadout Killstreak IMAGE {i}", loadout.Killstreaks.Count < (i + 1) ? "" : loadout.Killstreaks[i].Killstreak.IconLink);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Loadout Killstreak TEXT {i}", loadout.Killstreaks.Count < (i + 1) ? "" : loadout.Killstreaks[i].Killstreak.KillstreakName);
            }

            // Card
            Logging.Debug($"Card is null {loadout.Card == null}");
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Loadout Card IMAGE", loadout.Card == null ? "" : loadout.Card.Card.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Loadout Card TEXT", loadout.Card == null ? "" : loadout.Card.Card.CardName);

            // Glove
            Logging.Debug($"Glove is null {loadout.Glove == null}");
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Loadout Glove IMAGE", loadout.Glove == null ? "" : loadout.Glove.Glove.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Loadout Glove TEXT", loadout.Glove == null ? "" : loadout.Glove.Glove.GloveName);
        }

        public void EquipLoadout()
        {
            Logging.Debug($"{Player.CharacterName} activated loadout with id {LoadoutID}");
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                foreach (var activeLoadout in PlayerLoadout.Loadouts.Values.Where(k => k.IsActive))
                {
                    await DB.UpdatePlayerLoadoutActiveAsync(Player.CSteamID, activeLoadout.LoadoutID, false);
                }

                await DB.UpdatePlayerLoadoutActiveAsync(Player.CSteamID, LoadoutID, true);
                TaskDispatcher.QueueOnMainThread(() =>
                {
                    ReloadLoadoutPage();
                    ReloadLoadout();
                });
            });
        }

        public void SendLoadoutName(string name)
        {
            Logging.Debug($"{Player.CharacterName} sent loadout name with {name} for loadout with id {LoadoutID}");
            LoadoutNameText = name;
        }

        public void RenameLoadout()
        {
            Logging.Debug($"{Player.CharacterName} is trying to rename loadout to {LoadoutNameText} for loadout with id {LoadoutID}");
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error getting current loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            if (!string.IsNullOrEmpty(LoadoutNameText))
            {
                if (LoadoutNameText.Length > 40)
                {
                    return;
                }
                loadout.LoadoutName = LoadoutNameText;
                ThreadPool.QueueUserWorkItem(async (o) =>
                {
                    await DB.UpdatePlayerLoadoutAsync(Player.CSteamID, LoadoutID);
                });
                ReloadLoadoutPage();
            }
        }

        public void ExitRenameLoadout()
        {
            LoadoutNameText = "";
        }

        // Loadout Sub Page

        public void ShowLoadoutSubPage(ELoadoutPage page)
        {
            Logging.Debug($"Showing loadout sub page {page} for {Player.CharacterName}");
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Type TEXT", page.ToString().StartsWith("AttachmentPrimary") || page.ToString().StartsWith("AttachmentSecondary") ? "ATTACHMENT" : page.ToString().ToUpper());
            LoadoutPage = page;

            switch (LoadoutPage)
            {
                case ELoadoutPage.Primary:
                    ShowLoadoutTab(ELoadoutTab.SUBMACHINE_GUNS);
                    break;
                case ELoadoutPage.Secondary:
                    ShowLoadoutTab(ELoadoutTab.PISTOLS);
                    break;
                default:
                    ShowLoadoutTab(ELoadoutTab.ALL);
                    break;
            }
        }

        public void ShowLoadoutTab(ELoadoutTab tab)
        {
            Logging.Debug($"Showing loadout tab {tab} for {Player.CharacterName}");
            LoadoutTab = tab;

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", "");
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding current loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            for (int i = 0; i <= 4; i++)
            {
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", false);
            }

            switch (LoadoutTab)
            {
                case ELoadoutTab.ALL:
                    {
                        switch (LoadoutPage)
                        {
                            case ELoadoutPage.AttachmentPrimaryBarrel:
                            case ELoadoutPage.AttachmentPrimaryGrip:
                            case ELoadoutPage.AttachmentPrimaryMagazine:
                            case ELoadoutPage.AttachmentPrimarySights:
                                {
                                    if (!Enum.TryParse(LoadoutPage.ToString().Replace("AttachmentPrimary", ""), false, out EAttachment attachmentType))
                                    {
                                        Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                                        return;
                                    }

                                    if (!PlayerLoadout.Guns.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out LoadoutGun gun))
                                    {
                                        Logging.Debug($"Error finding primary that has been selected with id {loadout.Primary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                                    {
                                        Logging.Debug($"Error finding primary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!attachmentTypePages.TryGetValue(attachmentType, out Dictionary<int, PageAttachment> attachmentPages))
                                    {
                                        Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!attachmentPages.TryGetValue(1, out PageAttachment firstPage))
                                    {
                                        Logging.Debug($"Error finding first page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                                    ShowAttachmentPage(firstPage, gun);
                                    break;
                                }

                            case ELoadoutPage.AttachmentPrimaryCharm:
                            case ELoadoutPage.AttachmentSecondaryCharm:
                                {
                                    if (!GunCharmPages.TryGetValue(1, out PageGunCharm firstPage))
                                    {
                                        Logging.Debug($"Error getting first page for gun charms for {Player.CharacterName}");
                                        return;
                                    }

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);

                                    ShowGunCharmPage(firstPage);
                                    break;
                                }

                            case ELoadoutPage.AttachmentSecondaryBarrel:
                            case ELoadoutPage.AttachmentSecondaryMagazine:
                            case ELoadoutPage.AttachmentSecondarySights:
                                {
                                    if (!Enum.TryParse(LoadoutPage.ToString().Replace("AttachmentSecondary", ""), false, out EAttachment attachmentType))
                                    {
                                        Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                                        return;
                                    }

                                    if (!PlayerLoadout.Guns.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out LoadoutGun gun))
                                    {
                                        Logging.Debug($"Error finding secondary that has been selected with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                                    {
                                        Logging.Debug($"Error finding secondary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!attachmentTypePages.TryGetValue(attachmentType, out Dictionary<int, PageAttachment> attachmentPages))
                                    {
                                        Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!attachmentPages.TryGetValue(1, out PageAttachment firstPage))
                                    {
                                        Logging.Debug($"Error finding first page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                                    ShowAttachmentPage(firstPage, gun);
                                    break;
                                }

                            case ELoadoutPage.Perk:
                                {
                                    if (!PerkPages.TryGetValue(1, out PagePerk firstPage))
                                    {
                                        Logging.Debug($"Error getting first page for perks for {Player.CharacterName}");
                                        return;
                                    }

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);

                                    ShowPerkPage(firstPage);
                                    break;
                                }

                            case ELoadoutPage.Lethal:
                                {
                                    if (!LethalPages.TryGetValue(1, out PageGadget firstPage))
                                    {
                                        Logging.Debug($"Error finding the first page for lethals for {Player.CharacterName}");
                                        return;
                                    }

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                                    ShowGadgetPage(firstPage);
                                    break;
                                }

                            case ELoadoutPage.Tactical:
                                {
                                    if (!TacticalPages.TryGetValue(1, out PageGadget firstPage))
                                    {
                                        Logging.Debug($"Error finding the first page for tacticals for {Player.CharacterName}");
                                        return;
                                    }

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                                    ShowGadgetPage(firstPage);
                                    break;
                                }

                            case ELoadoutPage.Knife:
                                {
                                    if (!KnifePages.TryGetValue(1, out PageKnife firstPage))
                                    {
                                        Logging.Debug($"Error finding the first page for knives for {Player.CharacterName}");
                                        return;
                                    }

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                                    ShowKnifePage(firstPage);
                                    break;
                                }

                            case ELoadoutPage.Killstreak:
                                {
                                    if (!KillstreakPages.TryGetValue(1, out PageKillstreak firstPage))
                                    {
                                        Logging.Debug($"Error finding the first page for killstreaks for {Player.CharacterName}");
                                        return;
                                    }

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                                    ShowKillstreakPage(firstPage);
                                    break;
                                }

                            case ELoadoutPage.Glove:
                                {
                                    if (!GlovePages.TryGetValue(1, out PageGlove firstPage))
                                    {
                                        Logging.Debug($"Error finding the first page for gloves for {Player.CharacterName}");
                                        return;
                                    }

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                                    ShowGlovePage(firstPage);
                                    break;
                                }

                            case ELoadoutPage.Card:
                                {
                                    if (!CardPages.TryGetValue(1, out PageCard firstPage))
                                    {
                                        Logging.Debug($"Error finding the first page for cards for {Player.CharacterName}");
                                        return;
                                    }

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                                    ShowCardPage(firstPage);
                                    break;
                                }
                        }

                        break;
                    }

                case ELoadoutTab.SKINS:
                    {
                        switch (LoadoutPage)
                        {
                            case ELoadoutPage.Primary:
                                {
                                    if (!GunSkinPages.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out Dictionary<int, PageGunSkin> gunSkinPages))
                                    {
                                        Logging.Debug($"Error getting gun skin pages for gun with id {loadout.Primary?.Gun?.GunID ?? 0}");
                                        return;
                                    }

                                    if (!gunSkinPages.TryGetValue(1, out PageGunSkin firstPage))
                                    {
                                        Logging.Debug($"Error finding the first page for gun skins for gun with id {loadout.Primary.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                                    ShowGunSkinPage(firstPage);
                                    break;
                                }

                            case ELoadoutPage.Secondary:
                                {
                                    if (!GunSkinPages.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out Dictionary<int, PageGunSkin> gunSkinPages))
                                    {
                                        Logging.Debug($"Error getting gun skin pages for gun with id {loadout.Secondary?.Gun?.GunID ?? 0}");
                                        return;
                                    }

                                    if (!gunSkinPages.TryGetValue(1, out PageGunSkin firstPage))
                                    {
                                        Logging.Debug($"Error finding the first page for gun skins for gun with id {loadout.Secondary.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);
                                    ShowGunSkinPage(firstPage);
                                    break;
                                }
                        }

                        break;
                    }

                case ELoadoutTab.PISTOLS:
                    {
                        if (!PistolPages.TryGetValue(1, out PageGun firstPage))
                        {
                            Logging.Debug($"Error finding first page for pistols for {Player.CharacterName}");
                            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", "");
                            return;
                        }

                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                        ShowGunPage(firstPage);
                        break;
                    }

                case ELoadoutTab.SUBMACHINE_GUNS:
                    {
                        if (!SMGPages.TryGetValue(1, out PageGun firstPage))
                        {
                            Logging.Debug($"Error finding first page for smgs for {Player.CharacterName}");
                            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", "");
                            return;
                        }

                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                        ShowGunPage(firstPage);
                        break;
                    }

                case ELoadoutTab.SHOTGUNS:
                    {
                        if (!ShotgunPages.TryGetValue(1, out PageGun firstPage))
                        {
                            Logging.Debug($"Error finding first page for shotguns for {Player.CharacterName}");
                            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", "");
                            return;
                        }

                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                        ShowGunPage(firstPage);
                        break;
                    }

                case ELoadoutTab.LIGHT_MACHINE_GUNS:
                    {
                        if (!LMGPages.TryGetValue(1, out PageGun firstPage))
                        {
                            Logging.Debug($"Error finding first page for lmgs for {Player.CharacterName}");
                            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", "");
                            return;
                        }

                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                        ShowGunPage(firstPage);
                        break;
                    }

                case ELoadoutTab.ASSAULT_RIFLES:
                    {
                        if (!ARPages.TryGetValue(1, out PageGun firstPage))
                        {
                            Logging.Debug($"Error finding first page for ARs for {Player.CharacterName}");
                            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", "");
                            return;
                        }

                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                        ShowGunPage(firstPage);
                        break;
                    }

                case ELoadoutTab.SNIPER_RIFLES:
                    {
                        if (!SniperPages.TryGetValue(1, out PageGun firstPage))
                        {
                            Logging.Debug($"Error finding first page for snipers for {Player.CharacterName}");
                            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
                            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);
                            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", "");
                            return;
                        }

                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                        ShowGunPage(firstPage);
                        break;
                    }
            }
        }

        public void ForwardLoadoutTab()
        {
            Logging.Debug($"{Player.CharacterName} is trying to forward the loadout tab {LoadoutTab}, Current Page {LoadoutTabPageID}");
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            switch (LoadoutTab)
            {
                case ELoadoutTab.ALL:
                    {
                        switch (LoadoutPage)
                        {
                            case ELoadoutPage.AttachmentPrimaryBarrel:
                            case ELoadoutPage.AttachmentPrimaryGrip:
                            case ELoadoutPage.AttachmentPrimaryMagazine:
                            case ELoadoutPage.AttachmentPrimarySights:
                                {
                                    if (!Enum.TryParse(LoadoutPage.ToString().Replace("AttachmentPrimary", ""), false, out EAttachment attachmentType))
                                    {
                                        Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                                        return;
                                    }

                                    if (!PlayerLoadout.Guns.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out LoadoutGun gun))
                                    {
                                        Logging.Debug($"Error finding primary that has been selected with id {loadout.Primary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                                    {
                                        Logging.Debug($"Error finding primary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!attachmentTypePages.TryGetValue(attachmentType, out Dictionary<int, PageAttachment> attachmentPages))
                                    {
                                        Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (attachmentPages.TryGetValue(LoadoutTabPageID + 1, out PageAttachment nextPage) && !attachmentPages.TryGetValue(1, out nextPage))
                                    {
                                        Logging.Debug($"Error finding next page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                                    ShowAttachmentPage(nextPage, gun);
                                    break;
                                }

                            case ELoadoutPage.AttachmentPrimaryCharm:
                            case ELoadoutPage.AttachmentSecondaryCharm:
                                {
                                    if (!GunCharmPages.TryGetValue(LoadoutTabPageID + 1, out PageGunCharm nextPage) && !GunCharmPages.TryGetValue(1, out nextPage))
                                    {
                                        Logging.Debug($"Error getting next page for gun charms for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGunCharmPage(nextPage);
                                    break;
                                }

                            case ELoadoutPage.AttachmentSecondaryBarrel:
                            case ELoadoutPage.AttachmentSecondaryMagazine:
                            case ELoadoutPage.AttachmentSecondarySights:
                                {
                                    if (!Enum.TryParse(LoadoutPage.ToString().Replace("AttachmentSecondary", ""), false, out EAttachment attachmentType))
                                    {
                                        Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                                        return;
                                    }

                                    if (!PlayerLoadout.Guns.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out LoadoutGun gun))
                                    {
                                        Logging.Debug($"Error finding secondary that has been selected with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                                    {
                                        Logging.Debug($"Error finding secondary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!attachmentTypePages.TryGetValue(attachmentType, out Dictionary<int, PageAttachment> attachmentPages))
                                    {
                                        Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (attachmentPages.TryGetValue(LoadoutTabPageID + 1, out PageAttachment nextPage) && !attachmentPages.TryGetValue(1, out nextPage))
                                    {
                                        Logging.Debug($"Error finding next page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                                    ShowAttachmentPage(nextPage, gun);
                                    break;
                                }

                            case ELoadoutPage.Perk:
                                {
                                    if (!PerkPages.TryGetValue(LoadoutTabPageID + 1, out PagePerk nextPage) && !PerkPages.TryGetValue(1, out nextPage))
                                    {
                                        Logging.Debug($"Error getting next page for perks for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowPerkPage(nextPage);
                                    break;
                                }

                            case ELoadoutPage.Lethal:
                                {
                                    if (!LethalPages.TryGetValue(LoadoutTabPageID + 1, out PageGadget nextPage) && !LethalPages.TryGetValue(1, out nextPage))
                                    {
                                        Logging.Debug($"Error finding the next page for lethals for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGadgetPage(nextPage);
                                    break;
                                }

                            case ELoadoutPage.Tactical:
                                {
                                    if (!TacticalPages.TryGetValue(LoadoutTabPageID + 1, out PageGadget nextPage) && !TacticalPages.TryGetValue(1, out nextPage))
                                    {
                                        Logging.Debug($"Error finding the next page for tacticals for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGadgetPage(nextPage);
                                    break;
                                }

                            case ELoadoutPage.Knife:
                                {
                                    if (!KnifePages.TryGetValue(LoadoutTabPageID + 1, out PageKnife nextPage) && !KnifePages.TryGetValue(1, out nextPage))
                                    {
                                        Logging.Debug($"Error finding the next page for knives for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowKnifePage(nextPage);
                                    break;
                                }

                            case ELoadoutPage.Killstreak:
                                {
                                    if (!KillstreakPages.TryGetValue(LoadoutTabPageID + 1, out PageKillstreak nextPage) && !KillstreakPages.TryGetValue(1, out nextPage))
                                    {
                                        Logging.Debug($"Error finding the next page for killstreaks for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowKillstreakPage(nextPage);
                                    break;
                                }

                            case ELoadoutPage.Glove:
                                {
                                    if (!GlovePages.TryGetValue(LoadoutTabPageID + 1, out PageGlove nextPage) && !GlovePages.TryGetValue(1, out nextPage))
                                    {
                                        Logging.Debug($"Error finding the next page for gloves for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGlovePage(nextPage);
                                    break;
                                }

                            case ELoadoutPage.Card:
                                {
                                    if (!CardPages.TryGetValue(LoadoutTabPageID + 1, out PageCard nextPage) && !CardPages.TryGetValue(1, out nextPage))
                                    {
                                        Logging.Debug($"Error finding the next page for cards for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowCardPage(nextPage);
                                    break;
                                }
                        }

                        break;
                    }

                case ELoadoutTab.SKINS:
                    {
                        switch (LoadoutPage)
                        {
                            case ELoadoutPage.Primary:
                                {
                                    if (!GunSkinPages.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out Dictionary<int, PageGunSkin> gunSkinPages))
                                    {
                                        Logging.Debug($"Error getting gun skin pages for gun with id {loadout.Primary?.Gun?.GunID ?? 0}");
                                        return;
                                    }

                                    if (!gunSkinPages.TryGetValue(LoadoutTabPageID + 1, out PageGunSkin nextPage) && !gunSkinPages.TryGetValue(1, out nextPage))
                                    {
                                        Logging.Debug($"Error finding the next page for gun skins for gun with id {loadout.Primary.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGunSkinPage(nextPage);
                                    break;
                                }

                            case ELoadoutPage.Secondary:
                                {
                                    if (!GunSkinPages.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out Dictionary<int, PageGunSkin> gunSkinPages))
                                    {
                                        Logging.Debug($"Error getting gun skin pages for gun with id {loadout.Secondary?.Gun?.GunID ?? 0}");
                                        return;
                                    }

                                    if (!gunSkinPages.TryGetValue(LoadoutTabPageID + 1, out PageGunSkin nextPage) && !gunSkinPages.TryGetValue(1, out nextPage))
                                    {
                                        Logging.Debug($"Error finding the next page for gun skins for gun with id {loadout.Secondary.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGunSkinPage(nextPage);
                                    break;
                                }
                        }

                        break;
                    }

                case ELoadoutTab.PISTOLS:
                    {
                        if (!PistolPages.TryGetValue(LoadoutTabPageID + 1, out PageGun nextPage) && !PistolPages.TryGetValue(1, out nextPage))
                        {
                            Logging.Debug($"Error finding next page for pistols for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(nextPage);
                        break;
                    }

                case ELoadoutTab.SUBMACHINE_GUNS:
                    {
                        if (!SMGPages.TryGetValue(LoadoutTabPageID + 1, out PageGun nextPage) && !SMGPages.TryGetValue(1, out nextPage))
                        {
                            Logging.Debug($"Error finding next page for smgs for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(nextPage);
                        break;
                    }

                case ELoadoutTab.SHOTGUNS:
                    {
                        if (!ShotgunPages.TryGetValue(LoadoutTabPageID + 1, out PageGun nextPage) && !ShotgunPages.TryGetValue(1, out nextPage))
                        {
                            Logging.Debug($"Error finding next page for shotguns for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(nextPage);
                        break;
                    }

                case ELoadoutTab.LIGHT_MACHINE_GUNS:
                    {
                        if (!LMGPages.TryGetValue(LoadoutTabPageID + 1, out PageGun nextPage) && !LMGPages.TryGetValue(1, out nextPage))
                        {
                            Logging.Debug($"Error finding next page for lmgs for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(nextPage);
                        break;
                    }

                case ELoadoutTab.ASSAULT_RIFLES:
                    {
                        if (!ARPages.TryGetValue(LoadoutTabPageID + 1, out PageGun nextPage) && !ARPages.TryGetValue(1, out nextPage))
                        {
                            Logging.Debug($"Error finding next page for ARs for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(nextPage);
                        break;
                    }

                case ELoadoutTab.SNIPER_RIFLES:
                    {
                        if (!SniperPages.TryGetValue(LoadoutTabPageID + 1, out PageGun nextPage) && !SniperPages.TryGetValue(1, out nextPage))
                        {
                            Logging.Debug($"Error finding next page for snipers for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(nextPage);
                        break;
                    }
            }
        }

        public void BackwardLoadoutTab()
        {
            Logging.Debug($"{Player.CharacterName} is trying to backward the loadout tab {LoadoutTab}, Current Page {LoadoutTabPageID}");

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding current loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            switch (LoadoutTab)
            {
                case ELoadoutTab.ALL:
                    {
                        switch (LoadoutPage)
                        {
                            case ELoadoutPage.AttachmentPrimaryBarrel:
                            case ELoadoutPage.AttachmentPrimaryGrip:
                            case ELoadoutPage.AttachmentPrimaryMagazine:
                            case ELoadoutPage.AttachmentPrimarySights:
                                {
                                    if (!Enum.TryParse(LoadoutPage.ToString().Replace("AttachmentPrimary", ""), false, out EAttachment attachmentType))
                                    {
                                        Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                                        return;
                                    }

                                    if (!PlayerLoadout.Guns.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out LoadoutGun gun))
                                    {
                                        Logging.Debug($"Error finding primary that has been selected with id {loadout.Primary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                                    {
                                        Logging.Debug($"Error finding primary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!attachmentTypePages.TryGetValue(attachmentType, out Dictionary<int, PageAttachment> attachmentPages))
                                    {
                                        Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (attachmentPages.TryGetValue(LoadoutTabPageID - 1, out PageAttachment prevPage) && !attachmentPages.TryGetValue(attachmentPages.Keys.Max(), out prevPage))
                                    {
                                        Logging.Debug($"Error finding previous page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                                    ShowAttachmentPage(prevPage, gun);
                                    break;
                                }

                            case ELoadoutPage.AttachmentPrimaryCharm:
                            case ELoadoutPage.AttachmentSecondaryCharm:
                                {
                                    if (!GunCharmPages.TryGetValue(LoadoutTabPageID - 1, out PageGunCharm prevPage) && !GunCharmPages.TryGetValue(GunCharmPages.Keys.Max(), out prevPage))
                                    {
                                        Logging.Debug($"Error getting prev page for gun charms for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGunCharmPage(prevPage);
                                    break;
                                }

                            case ELoadoutPage.AttachmentSecondaryBarrel:
                            case ELoadoutPage.AttachmentSecondaryMagazine:
                            case ELoadoutPage.AttachmentSecondarySights:
                                {
                                    if (!Enum.TryParse(LoadoutPage.ToString().Replace("AttachmentSecondary", ""), false, out EAttachment attachmentType))
                                    {
                                        Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                                        return;
                                    }

                                    if (!PlayerLoadout.Guns.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out LoadoutGun gun))
                                    {
                                        Logging.Debug($"Error finding Secondary that has been selected with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                                    {
                                        Logging.Debug($"Error finding Secondary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!attachmentTypePages.TryGetValue(attachmentType, out Dictionary<int, PageAttachment> attachmentPages))
                                    {
                                        Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (attachmentPages.TryGetValue(LoadoutTabPageID - 1, out PageAttachment prevPage) && !attachmentPages.TryGetValue(attachmentPages.Keys.Max(), out prevPage))
                                    {
                                        Logging.Debug($"Error finding previous page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                                    ShowAttachmentPage(prevPage, gun);
                                    break;
                                }

                            case ELoadoutPage.Perk:
                                {
                                    if (!PerkPages.TryGetValue(LoadoutTabPageID - 1, out PagePerk prevPage) && !PerkPages.TryGetValue(PerkPages.Keys.Max(), out prevPage))
                                    {
                                        Logging.Debug($"Error getting prev page for perks for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowPerkPage(prevPage);
                                    break;
                                }

                            case ELoadoutPage.Lethal:
                                {
                                    if (!LethalPages.TryGetValue(LoadoutTabPageID - 1, out PageGadget prevPage) && !LethalPages.TryGetValue(LethalPages.Keys.Max(), out prevPage))
                                    {
                                        Logging.Debug($"Error finding the prev page for lethals for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGadgetPage(prevPage);
                                    break;
                                }

                            case ELoadoutPage.Tactical:
                                {
                                    if (!TacticalPages.TryGetValue(LoadoutTabPageID - 1, out PageGadget prevPage) && !TacticalPages.TryGetValue(TacticalPages.Keys.Max(), out prevPage))
                                    {
                                        Logging.Debug($"Error finding the prev page for tacticals for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGadgetPage(prevPage);
                                    break;
                                }

                            case ELoadoutPage.Knife:
                                {
                                    if (!KnifePages.TryGetValue(LoadoutTabPageID - 1, out PageKnife prevPage) && !KnifePages.TryGetValue(KnifePages.Keys.Max(), out prevPage))
                                    {
                                        Logging.Debug($"Error finding the prev page for knives for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowKnifePage(prevPage);
                                    break;
                                }

                            case ELoadoutPage.Killstreak:
                                {
                                    if (!KillstreakPages.TryGetValue(LoadoutTabPageID - 1, out PageKillstreak prevPage) && !KillstreakPages.TryGetValue(KillstreakPages.Keys.Max(), out prevPage))
                                    {
                                        Logging.Debug($"Error finding the prev page for killstreaks for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowKillstreakPage(prevPage);
                                    break;
                                }

                            case ELoadoutPage.Glove:
                                {
                                    if (!GlovePages.TryGetValue(LoadoutTabPageID - 1, out PageGlove prevPage) && !GlovePages.TryGetValue(GlovePages.Keys.Max(), out prevPage))
                                    {
                                        Logging.Debug($"Error finding the prev page for gloves for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGlovePage(prevPage);
                                    break;
                                }

                            case ELoadoutPage.Card:
                                {
                                    if (!CardPages.TryGetValue(LoadoutTabPageID - 1, out PageCard prevPage) && !CardPages.TryGetValue(CardPages.Keys.Max(), out prevPage))
                                    {
                                        Logging.Debug($"Error finding the prev page for cards for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowCardPage(prevPage);
                                    break;
                                }
                        }

                        break;
                    }

                case ELoadoutTab.SKINS:
                    {
                        switch (LoadoutPage)
                        {
                            case ELoadoutPage.Primary:
                                {
                                    if (!GunSkinPages.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out Dictionary<int, PageGunSkin> gunSkinPages))
                                    {
                                        Logging.Debug($"Error getting gun skin pages for gun with id {loadout.Primary?.Gun?.GunID ?? 0}");
                                        return;
                                    }

                                    if (!gunSkinPages.TryGetValue(LoadoutTabPageID - 1, out PageGunSkin prevPage) && !gunSkinPages.TryGetValue(gunSkinPages.Keys.Max(), out prevPage))
                                    {
                                        Logging.Debug($"Error finding the prev page for gun skins for gun with id {loadout.Primary.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGunSkinPage(prevPage);
                                    break;
                                }

                            case ELoadoutPage.Secondary:
                                {
                                    if (!GunSkinPages.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out Dictionary<int, PageGunSkin> gunSkinPages))
                                    {
                                        Logging.Debug($"Error getting gun skin pages for gun with id {loadout.Secondary?.Gun?.GunID ?? 0}");
                                        return;
                                    }

                                    if (!gunSkinPages.TryGetValue(LoadoutTabPageID - 1, out PageGunSkin prevPage) && !gunSkinPages.TryGetValue(gunSkinPages.Keys.Max(), out prevPage))
                                    {
                                        Logging.Debug($"Error finding the prev page for gun skins for gun with id {loadout.Secondary.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGunSkinPage(prevPage);
                                    break;
                                }
                        }

                        break;
                    }

                case ELoadoutTab.PISTOLS:
                    {
                        if (!PistolPages.TryGetValue(LoadoutTabPageID - 1, out PageGun prevPage) && !PistolPages.TryGetValue(PistolPages.Keys.Max(), out prevPage))
                        {
                            Logging.Debug($"Error finding next page for pistols for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(prevPage);
                        break;
                    }

                case ELoadoutTab.SUBMACHINE_GUNS:
                    {
                        if (!SMGPages.TryGetValue(LoadoutTabPageID - 1, out PageGun prevPage) && !SMGPages.TryGetValue(SMGPages.Keys.Max(), out prevPage))
                        {
                            Logging.Debug($"Error finding next page for smgs for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(prevPage);
                        break;
                    }

                case ELoadoutTab.SHOTGUNS:
                    {
                        if (!ShotgunPages.TryGetValue(LoadoutTabPageID - 1, out PageGun prevPage) && !ShotgunPages.TryGetValue(ShotgunPages.Keys.Max(), out prevPage))
                        {
                            Logging.Debug($"Error finding next page for shotguns for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(prevPage);
                        break;
                    }

                case ELoadoutTab.LIGHT_MACHINE_GUNS:
                    {
                        if (!LMGPages.TryGetValue(LoadoutTabPageID - 1, out PageGun prevPage) && !LMGPages.TryGetValue(LMGPages.Keys.Max(), out prevPage))
                        {
                            Logging.Debug($"Error finding next page for lmgs for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(prevPage);
                        break;
                    }

                case ELoadoutTab.ASSAULT_RIFLES:
                    {
                        if (!ARPages.TryGetValue(LoadoutTabPageID - 1, out PageGun prevPage) && !ARPages.TryGetValue(ARPages.Keys.Max(), out prevPage))
                        {
                            Logging.Debug($"Error finding next page for ARs for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(prevPage);
                        break;
                    }

                case ELoadoutTab.SNIPER_RIFLES:
                    {
                        if (!SniperPages.TryGetValue(LoadoutTabPageID - 1, out PageGun prevPage) && !SniperPages.TryGetValue(SniperPages.Keys.Max(), out prevPage))
                        {
                            Logging.Debug($"Error finding next page for snipers for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(prevPage);
                        break;
                    }
            }
        }

        public void ReloadLoadoutTab()
        {
            Logging.Debug($"Reloading current loadout tab page for {Player.CharacterName}, Current Page {LoadoutTabPageID}");
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            switch (LoadoutTab)
            {
                case ELoadoutTab.ALL:
                    {
                        switch (LoadoutPage)
                        {
                            case ELoadoutPage.AttachmentPrimaryBarrel:
                            case ELoadoutPage.AttachmentPrimaryGrip:
                            case ELoadoutPage.AttachmentPrimaryMagazine:
                            case ELoadoutPage.AttachmentPrimarySights:
                                {
                                    if (!Enum.TryParse(LoadoutPage.ToString().Replace("AttachmentPrimary", ""), false, out EAttachment attachmentType))
                                    {
                                        Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                                        return;
                                    }

                                    if (!PlayerLoadout.Guns.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out LoadoutGun gun))
                                    {
                                        Logging.Debug($"Error finding primary that has been selected with id {loadout.Primary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                                    {
                                        Logging.Debug($"Error finding primary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!attachmentTypePages.TryGetValue(attachmentType, out Dictionary<int, PageAttachment> attachmentPages))
                                    {
                                        Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (attachmentPages.TryGetValue(LoadoutTabPageID, out PageAttachment page))
                                    {
                                        Logging.Debug($"Error finding current page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                                    ShowAttachmentPage(page, gun);
                                    break;
                                }

                            case ELoadoutPage.AttachmentPrimaryCharm:
                            case ELoadoutPage.AttachmentSecondaryCharm:
                                {
                                    if (!GunCharmPages.TryGetValue(LoadoutTabPageID, out PageGunCharm page))
                                    {
                                        Logging.Debug($"Error getting current page for gun charms for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGunCharmPage(page);
                                    break;
                                }

                            case ELoadoutPage.AttachmentSecondaryBarrel:
                            case ELoadoutPage.AttachmentSecondaryMagazine:
                            case ELoadoutPage.AttachmentSecondarySights:
                                {
                                    if (!Enum.TryParse(LoadoutPage.ToString().Replace("AttachmentSecondary", ""), false, out EAttachment attachmentType))
                                    {
                                        Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                                        return;
                                    }

                                    if (!PlayerLoadout.Guns.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out LoadoutGun gun))
                                    {
                                        Logging.Debug($"Error finding secondary that has been selected with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                                    {
                                        Logging.Debug($"Error finding secondary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!attachmentTypePages.TryGetValue(attachmentType, out Dictionary<int, PageAttachment> attachmentPages))
                                    {
                                        Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (attachmentPages.TryGetValue(LoadoutTabPageID, out PageAttachment page))
                                    {
                                        Logging.Debug($"Error finding current page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", true);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", true);

                                    ShowAttachmentPage(page, gun);
                                    break;
                                }

                            case ELoadoutPage.Perk:
                                {
                                    if (!PerkPages.TryGetValue(LoadoutTabPageID, out PagePerk page))
                                    {
                                        Logging.Debug($"Error getting current page for perks for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowPerkPage(page);
                                    break;
                                }

                            case ELoadoutPage.Lethal:
                                {
                                    if (!LethalPages.TryGetValue(LoadoutTabPageID, out PageGadget page))
                                    {
                                        Logging.Debug($"Error finding the current page for lethals for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGadgetPage(page);
                                    break;
                                }

                            case ELoadoutPage.Tactical:
                                {
                                    if (!TacticalPages.TryGetValue(LoadoutTabPageID, out PageGadget page))
                                    {
                                        Logging.Debug($"Error finding the current page for tacticals for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGadgetPage(page);
                                    break;
                                }

                            case ELoadoutPage.Knife:
                                {
                                    if (!KnifePages.TryGetValue(LoadoutTabPageID, out PageKnife page))
                                    {
                                        Logging.Debug($"Error finding the current page for knives for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowKnifePage(page);
                                    break;
                                }

                            case ELoadoutPage.Killstreak:
                                {
                                    if (!KillstreakPages.TryGetValue(LoadoutTabPageID, out PageKillstreak page))
                                    {
                                        Logging.Debug($"Error finding the current page for killstreaks for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowKillstreakPage(page);
                                    break;
                                }

                            case ELoadoutPage.Glove:
                                {
                                    if (!GlovePages.TryGetValue(LoadoutTabPageID, out PageGlove page))
                                    {
                                        Logging.Debug($"Error finding the current page for gloves for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGlovePage(page);
                                    break;
                                }

                            case ELoadoutPage.Card:
                                {
                                    if (!CardPages.TryGetValue(LoadoutTabPageID, out PageCard page))
                                    {
                                        Logging.Debug($"Error finding the current page for cards for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowCardPage(page);
                                    break;
                                }
                        }

                        break;
                    }

                case ELoadoutTab.SKINS:
                    {
                        switch (LoadoutPage)
                        {
                            case ELoadoutPage.Primary:
                                {
                                    if (!GunSkinPages.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out Dictionary<int, PageGunSkin> gunSkinPages))
                                    {
                                        Logging.Debug($"Error getting gun skin pages for gun with id {loadout.Primary?.Gun?.GunID ?? 0}");
                                        return;
                                    }

                                    if (!gunSkinPages.TryGetValue(LoadoutTabPageID, out PageGunSkin page))
                                    {
                                        Logging.Debug($"Error finding the current page for gun skins for gun with id {loadout.Primary.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGunSkinPage(page);
                                    break;
                                }

                            case ELoadoutPage.Secondary:
                                {
                                    if (!GunSkinPages.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out Dictionary<int, PageGunSkin> gunSkinPages))
                                    {
                                        Logging.Debug($"Error getting gun skin pages for gun with id {loadout.Secondary?.Gun?.GunID ?? 0}");
                                        return;
                                    }

                                    if (!gunSkinPages.TryGetValue(LoadoutTabPageID, out PageGunSkin page))
                                    {
                                        Logging.Debug($"Error finding the current page for gun skins for gun with id {loadout.Secondary.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGunSkinPage(page);
                                    break;
                                }
                        }

                        break;
                    }

                case ELoadoutTab.PISTOLS:
                    {
                        if (!PistolPages.TryGetValue(LoadoutTabPageID, out PageGun page))
                        {
                            Logging.Debug($"Error finding current page for pistols for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(page);
                        break;
                    }

                case ELoadoutTab.SUBMACHINE_GUNS:
                    {
                        if (!SMGPages.TryGetValue(LoadoutTabPageID, out PageGun page))
                        {
                            Logging.Debug($"Error finding current page for smgs for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(page);
                        break;
                    }

                case ELoadoutTab.SHOTGUNS:
                    {
                        if (!ShotgunPages.TryGetValue(LoadoutTabPageID, out PageGun page))
                        {
                            Logging.Debug($"Error finding current page for shotguns for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(page);
                        break;
                    }

                case ELoadoutTab.LIGHT_MACHINE_GUNS:
                    {
                        if (!LMGPages.TryGetValue(LoadoutTabPageID, out PageGun page))
                        {
                            Logging.Debug($"Error finding current page for lmgs for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(page);
                        break;
                    }

                case ELoadoutTab.ASSAULT_RIFLES:
                    {
                        if (!ARPages.TryGetValue(LoadoutTabPageID, out PageGun page))
                        {
                            Logging.Debug($"Error finding current page for ARs for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(page);
                        break;
                    }

                case ELoadoutTab.SNIPER_RIFLES:
                    {
                        if (!SniperPages.TryGetValue(LoadoutTabPageID, out PageGun page))
                        {
                            Logging.Debug($"Error finding current page for snipers for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(page);
                        break;
                    }
            }
        }

        public void ShowGunPage(PageGun page)
        {
            Logging.Debug($"Showing gun page to {Player.CharacterName} with page id {page.PageID}");
            LoadoutTabPageID = page.PageID;

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
                return;
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
            for (int i = 0; i <= 4; i++)
            {
                if (!page.Guns.TryGetValue(i, out LoadoutGun gun))
                {
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", false);
                    continue;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Equipped {i}", (LoadoutPage == ELoadoutPage.Primary && currentLoadout.Primary == gun) || (LoadoutPage == ELoadoutPage.Secondary && currentLoadout.Secondary == gun));
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Item IMAGE {i}", gun.Gun.IconLink);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item TEXT {i}", gun.Gun.GunName);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Lock Overlay {i}", !gun.IsBought && gun.Gun.LevelRequirement > PlayerData.Level);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Lock Overlay TEXT {i}", !gun.IsBought && gun.Gun.LevelRequirement > PlayerData.Level ? $"UNLOCK WITH LEVEL {gun.Gun.LevelRequirement}" : "");
            }
        }

        public void ShowAttachmentPage(PageAttachment page, LoadoutGun gun)
        {
            Logging.Debug($"Showing attachment page to {Player.CharacterName} with page id {page.PageID}");
            LoadoutTabPageID = page.PageID;

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
                return;
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
            for (int i = 0; i <= 4; i++)
            {
                if (!page.Attachments.TryGetValue(i, out LoadoutAttachment attachment))
                {
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", false);
                    continue;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Equipped {i}", (LoadoutPage.ToString().StartsWith("AttachmentPrimary") && currentLoadout.PrimaryAttachments.ContainsValue(attachment)) || (LoadoutPage.ToString().StartsWith("AttachmentSecondary") && currentLoadout.SecondaryAttachments.ContainsValue(attachment)));
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Item IMAGE {i}", attachment.Attachment.IconLink);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item TEXT {i}", attachment.Attachment.AttachmentName);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Lock Overlay {i}", !attachment.IsBought && attachment.LevelRequirement > gun.Level);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Lock Overlay TEXT {i}", !attachment.IsBought && attachment.LevelRequirement > gun.Level ? $"UNLOCK WITH GUN LEVEL {attachment.LevelRequirement}" : "");
            }
        }

        public void ShowGunCharmPage(PageGunCharm page)
        {
            Logging.Debug($"Showing gun charm page to {Player.CharacterName} with page id {page.PageID}");
            LoadoutTabPageID = page.PageID;

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
                return;
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
            for (int i = 0; i <= 4; i++)
            {
                if (!page.GunCharms.TryGetValue(i, out LoadoutGunCharm gunCharm))
                {
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", false);
                    continue;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Equipped {i}", (LoadoutPage.ToString().StartsWith("AttachmentPrimary") && currentLoadout.PrimaryGunCharm == gunCharm) || (LoadoutPage.ToString().StartsWith("AttachmentSecondary") && currentLoadout.SecondaryGunCharm == gunCharm));
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Item IMAGE {i}", gunCharm.GunCharm.IconLink);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item TEXT {i}", gunCharm.GunCharm.CharmName);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Lock Overlay {i}", !gunCharm.IsBought && gunCharm.GunCharm.LevelRequirement > PlayerData.Level);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Lock Overlay TEXT {i}", !gunCharm.IsBought && gunCharm.GunCharm.LevelRequirement > PlayerData.Level ? $"UNLOCK WITH LEVEL {gunCharm.GunCharm.LevelRequirement}" : "");
            }
        }

        public void ShowGunSkinPage(PageGunSkin page)
        {
            Logging.Debug($"Showing gun skin page to {Player.CharacterName} with page id {page.PageID}");
            LoadoutTabPageID = page.PageID;

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
                return;
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
            for (int i = 0; i <= 4; i++)
            {
                if (!page.GunSkins.TryGetValue(i, out GunSkin skin))
                {
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", false);
                    continue;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Equipped {i}", (LoadoutPage == ELoadoutPage.Primary && currentLoadout.PrimarySkin == skin) || (LoadoutPage == ELoadoutPage.Secondary && currentLoadout.SecondarySkin == skin));
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Item IMAGE {i}", skin.IconLink);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item TEXT {i}", skin.SkinName);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Lock Overlay {i}", false);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Lock Overlay TEXT {i}", "");
            }
        }

        public void ShowKnifePage(PageKnife page)
        {
            Logging.Debug($"Showing knife page to {Player.CharacterName} with page id {page.PageID}");
            LoadoutTabPageID = page.PageID;

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
                return;
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
            for (int i = 0; i <= 4; i++)
            {
                if (!page.Knives.TryGetValue(i, out LoadoutKnife knife))
                {
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", false);
                    continue;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Equipped {i}", currentLoadout.Knife == knife);
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Item IMAGE {i}", knife.Knife.IconLink);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item TEXT {i}", knife.Knife.KnifeName);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Lock Overlay {i}", !knife.IsBought && knife.Knife.LevelRequirement > PlayerData.Level);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Lock Overlay TEXT {i}", !knife.IsBought && knife.Knife.LevelRequirement > PlayerData.Level ? $"UNLOCK WITH LEVEL {knife.Knife.LevelRequirement}" : "");
            }
        }

        public void ShowPerkPage(PagePerk page)
        {
            Logging.Debug($"Showing perk page to {Player.CharacterName} with page id {page.PageID}");
            LoadoutTabPageID = page.PageID;

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
                return;
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
            for (int i = 0; i <= 4; i++)
            {
                if (!page.Perks.TryGetValue(i, out LoadoutPerk perk))
                {
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", false);
                    continue;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Equipped {i}", currentLoadout.Perks.Contains(perk));
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Item IMAGE {i}", perk.Perk.IconLink);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item TEXT {i}", perk.Perk.PerkName);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Lock Overlay {i}", !perk.IsBought && perk.Perk.LevelRequirement > PlayerData.Level);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Lock Overlay TEXT {i}", !perk.IsBought && perk.Perk.LevelRequirement > PlayerData.Level ? $"UNLOCK WITH LEVEL {perk.Perk.LevelRequirement}" : "");
            }
        }

        public void ShowGadgetPage(PageGadget page)
        {
            Logging.Debug($"Showing gadget page to {Player.CharacterName} with page id {page.PageID}");
            LoadoutTabPageID = page.PageID;

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
                return;
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
            for (int i = 0; i <= 4; i++)
            {
                if (!page.Gadgets.TryGetValue(i, out LoadoutGadget gadget))
                {
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", false);
                    continue;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Equipped {i}", (LoadoutPage == ELoadoutPage.Tactical && currentLoadout.Tactical == gadget) || (LoadoutPage == ELoadoutPage.Lethal && currentLoadout.Lethal == gadget));
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Item IMAGE {i}", gadget.Gadget.IconLink);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item TEXT {i}", gadget.Gadget.GadgetName);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Lock Overlay {i}", !gadget.IsBought && gadget.Gadget.LevelRequirement > PlayerData.Level);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Lock Overlay TEXT {i}", !gadget.IsBought && gadget.Gadget.LevelRequirement > PlayerData.Level ? $"UNLOCK WITH LEVEL {gadget.Gadget.LevelRequirement}" : "");
            }
        }

        public void ShowCardPage(PageCard page)
        {
            Logging.Debug($"Showing card page to {Player.CharacterName} with page id {page.PageID}");
            LoadoutTabPageID = page.PageID;

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
                return;
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
            for (int i = 0; i <= 4; i++)
            {
                if (!page.Cards.TryGetValue(i, out LoadoutCard card))
                {
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", false);
                    continue;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Equipped {i}", currentLoadout.Card == card);
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Item IMAGE {i}", card.Card.IconLink);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item TEXT {i}", card.Card.CardName);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Lock Overlay {i}", !card.IsBought && card.Card.LevelRequirement > PlayerData.Level);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Lock Overlay TEXT {i}", !card.IsBought && card.Card.LevelRequirement > PlayerData.Level ? $"UNLOCK WITH LEVEL {card.Card.LevelRequirement}" : "");
            }
        }

        public void ShowGlovePage(PageGlove page)
        {
            Logging.Debug($"Showing glove page to {Player.CharacterName} with page id {page.PageID}");
            LoadoutTabPageID = page.PageID;

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
                return;
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
            for (int i = 0; i <= 4; i++)
            {
                if (!page.Gloves.TryGetValue(i, out LoadoutGlove glove))
                {
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", false);
                    continue;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Equipped {i}", currentLoadout.Glove == glove);
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Item IMAGE {i}", glove.Glove.IconLink);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item TEXT {i}", glove.Glove.GloveName);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Lock Overlay {i}", !glove.IsBought && glove.Glove.LevelRequirement > PlayerData.Level);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Lock Overlay TEXT {i}", !glove.IsBought && glove.Glove.LevelRequirement > PlayerData.Level ? $"UNLOCK WITH LEVEL {glove.Glove.LevelRequirement}" : "");
            }
        }

        public void ShowKillstreakPage(PageKillstreak page)
        {
            Logging.Debug($"Showing killstreak page to {Player.CharacterName} with page id {page.PageID}");
            LoadoutTabPageID = page.PageID;

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
                return;
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
            for (int i = 0; i <= 4; i++)
            {
                if (!page.Killstreaks.TryGetValue(i, out LoadoutKillstreak killstreak))
                {
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", false);
                    continue;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Equipped {i}", currentLoadout.Killstreaks.Contains(killstreak));
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Item IMAGE {i}", killstreak.Killstreak.IconLink);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item TEXT {i}", killstreak.Killstreak.KillstreakName);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Lock Overlay {i}", !killstreak.IsBought && killstreak.Killstreak.LevelRequirement > PlayerData.Level);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Lock Overlay TEXT {i}", !killstreak.IsBought && killstreak.Killstreak.LevelRequirement > PlayerData.Level ? $"UNLOCK WITH LEVEL {killstreak.Killstreak.LevelRequirement}" : "");
            }
        }

        public void ReloadSelectedItem()
        {
            Logging.Debug($"Reloading selected item for {Player.CharacterName}, selected item {SelectedItemID}, page {LoadoutPage}");
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding current loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            switch (LoadoutPage)
            {
                case ELoadoutPage.Primary:
                    {
                        if (!PlayerLoadout.Guns.TryGetValue((ushort)SelectedItemID, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding gun at {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGun(gun);
                        break;
                    }

                case ELoadoutPage.Secondary:
                    {
                        if (!PlayerLoadout.Guns.TryGetValue((ushort)SelectedItemID, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding gun at {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGun(gun);
                        break;
                    }

                case ELoadoutPage.AttachmentPrimaryBarrel:
                case ELoadoutPage.AttachmentPrimaryGrip:
                case ELoadoutPage.AttachmentPrimaryMagazine:
                case ELoadoutPage.AttachmentPrimarySights:
                    {
                        if (!Enum.TryParse(LoadoutPage.ToString().Replace("AttachmentPrimary", ""), false, out EAttachment attachmentType))
                        {
                            Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                            return;
                        }

                        if (!PlayerLoadout.Guns.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding primary that has been selected with id {loadout.Primary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                            return;
                        }

                        if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                        {
                            Logging.Debug($"Error finding primary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentTypePages.TryGetValue(attachmentType, out Dictionary<int, PageAttachment> attachmentPages))
                        {
                            Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentPages.TryGetValue(LoadoutTabPageID, out PageAttachment page))
                        {
                            Logging.Debug($"Error finding page {LoadoutTabPageID} of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        if (page.Attachments.TryGetValue((int)SelectedItemID, out LoadoutAttachment attachment))
                        {
                            Logging.Debug($"Error finding attachment at page id {LoadoutTabPageID} with position {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ShowAttachment(attachment, gun);
                        break;
                    }

                case ELoadoutPage.AttachmentSecondaryBarrel:
                case ELoadoutPage.AttachmentSecondaryMagazine:
                case ELoadoutPage.AttachmentSecondarySights:
                    {
                        if (!Enum.TryParse(LoadoutPage.ToString().Replace("AttachmentSecondary", ""), false, out EAttachment attachmentType))
                        {
                            Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                            return;
                        }

                        if (!PlayerLoadout.Guns.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding Secondary that has been selected with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                            return;
                        }

                        if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                        {
                            Logging.Debug($"Error finding Secondary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentTypePages.TryGetValue(attachmentType, out Dictionary<int, PageAttachment> attachmentPages))
                        {
                            Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentPages.TryGetValue(LoadoutTabPageID, out PageAttachment page))
                        {
                            Logging.Debug($"Error finding page {LoadoutTabPageID} of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        if (page.Attachments.TryGetValue((int)SelectedItemID, out LoadoutAttachment attachment))
                        {
                            Logging.Debug($"Error finding attachment at page id {LoadoutTabPageID} with position {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ShowAttachment(attachment, gun);
                        break;
                    }

                case ELoadoutPage.AttachmentPrimaryCharm:
                case ELoadoutPage.AttachmentSecondaryCharm:
                    {
                        if (!PlayerLoadout.GunCharms.TryGetValue((ushort)SelectedItemID, out LoadoutGunCharm gunCharm))
                        {
                            Logging.Debug($"Error finding gun charm with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGunCharm(gunCharm);
                        break;
                    }

                case ELoadoutPage.Lethal:
                    {
                        if (!PlayerLoadout.Gadgets.TryGetValue((ushort)SelectedItemID, out LoadoutGadget gadget))
                        {
                            Logging.Debug($"Error finding gadget with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGadget(gadget);
                        break;
                    }

                case ELoadoutPage.Tactical:
                    {
                        if (!PlayerLoadout.Gadgets.TryGetValue((ushort)SelectedItemID, out LoadoutGadget gadget))
                        {
                            Logging.Debug($"Error finding gadget with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGadget(gadget);
                        break;
                    }

                case ELoadoutPage.Perk:
                    {
                        if (!PlayerLoadout.Perks.TryGetValue((int)SelectedItemID, out LoadoutPerk perk))
                        {
                            Logging.Debug($"Error finding perk with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ShowPerk(perk);
                        break;
                    }

                case ELoadoutPage.Knife:
                    {
                        if (!PlayerLoadout.Knives.TryGetValue((ushort)SelectedItemID, out LoadoutKnife knife))
                        {
                            Logging.Debug($"Error finding knife with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ShowKnife(knife);
                        break;
                    }

                case ELoadoutPage.Killstreak:
                    {
                        if (!PlayerLoadout.Killstreaks.TryGetValue((int)SelectedItemID, out LoadoutKillstreak killstreak))
                        {
                            Logging.Debug($"Error finding kilsltreak with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ShowKillstreak(killstreak);
                        break;
                    }

                case ELoadoutPage.Glove:
                    {
                        if (!PlayerLoadout.Gloves.TryGetValue((ushort)SelectedItemID, out LoadoutGlove glove))
                        {
                            Logging.Debug($"Error finding glove with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGlove(glove);
                        break;
                    }

                case ELoadoutPage.Card:
                    {
                        if (!PlayerLoadout.Cards.TryGetValue((int)SelectedItemID, out LoadoutCard card))
                        {
                            Logging.Debug($"Error finding card with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ShowCard(card);
                        break;
                    }
            }
        }

        public void SelectedItem(int selected)
        {
            Logging.Debug($"{Player.CharacterName} selected an item at {selected}, Tab {LoadoutTab}, Page {LoadoutPage}");
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding current loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            switch (LoadoutTab)
            {
                case ELoadoutTab.ALL:
                    {
                        switch (LoadoutPage)
                        {
                            case ELoadoutPage.AttachmentPrimaryBarrel:
                            case ELoadoutPage.AttachmentPrimaryGrip:
                            case ELoadoutPage.AttachmentPrimaryMagazine:
                            case ELoadoutPage.AttachmentPrimarySights:
                                {
                                    if (!Enum.TryParse(LoadoutPage.ToString().Replace("AttachmentPrimary", ""), false, out EAttachment attachmentType))
                                    {
                                        Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                                        return;
                                    }

                                    if (!PlayerLoadout.Guns.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out LoadoutGun gun))
                                    {
                                        Logging.Debug($"Error finding primary that has been selected with id {loadout.Primary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                                    {
                                        Logging.Debug($"Error finding primary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!attachmentTypePages.TryGetValue(attachmentType, out Dictionary<int, PageAttachment> attachmentPages))
                                    {
                                        Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!attachmentPages.TryGetValue(LoadoutTabPageID, out PageAttachment page))
                                    {
                                        Logging.Debug($"Error finding page {LoadoutTabPageID} of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!page.Attachments.TryGetValue(selected, out LoadoutAttachment attachment))
                                    {
                                        Logging.Debug($"Error finding attachment at page id {LoadoutTabPageID} with position {selected} for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowAttachment(attachment, gun);
                                    break;
                                }

                            case ELoadoutPage.AttachmentPrimaryCharm:
                            case ELoadoutPage.AttachmentSecondaryCharm:
                                {
                                    if (!GunCharmPages.TryGetValue(LoadoutTabPageID, out PageGunCharm page))
                                    {
                                        Logging.Debug($"Error finding gun charm page with id {LoadoutTabPageID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!page.GunCharms.TryGetValue(selected, out LoadoutGunCharm gunCharm))
                                    {
                                        Logging.Debug($"Error finding gun charm at {selected} at page {LoadoutTabPageID} for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGunCharm(gunCharm);
                                    break;
                                }

                            case ELoadoutPage.AttachmentSecondaryBarrel:
                            case ELoadoutPage.AttachmentSecondaryMagazine:
                            case ELoadoutPage.AttachmentSecondarySights:
                                {
                                    if (!Enum.TryParse(LoadoutPage.ToString().Replace("AttachmentSecondary", ""), false, out EAttachment attachmentType))
                                    {
                                        Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                                        return;
                                    }

                                    if (!PlayerLoadout.Guns.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out LoadoutGun gun))
                                    {
                                        Logging.Debug($"Error finding Secondary that has been selected with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                                    {
                                        Logging.Debug($"Error finding Secondary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!attachmentTypePages.TryGetValue(attachmentType, out Dictionary<int, PageAttachment> attachmentPages))
                                    {
                                        Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!attachmentPages.TryGetValue(LoadoutTabPageID, out PageAttachment page))
                                    {
                                        Logging.Debug($"Error finding page {LoadoutTabPageID} of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!page.Attachments.TryGetValue(selected, out LoadoutAttachment attachment))
                                    {
                                        Logging.Debug($"Error finding attachment at page id {LoadoutTabPageID} with position {selected} for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowAttachment(attachment, gun);
                                    break;
                                }

                            case ELoadoutPage.Perk:
                                {
                                    if (!PerkPages.TryGetValue(LoadoutTabPageID, out PagePerk page))
                                    {
                                        Logging.Debug($"Error finding perk page with id {LoadoutTabPageID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!page.Perks.TryGetValue(selected, out LoadoutPerk perk))
                                    {
                                        Logging.Debug($"Error finding perk at {selected} at page {LoadoutTabPageID} for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowPerk(perk);
                                    break;
                                }

                            case ELoadoutPage.Lethal:
                                {
                                    if (!LethalPages.TryGetValue(LoadoutTabPageID, out PageGadget page))
                                    {
                                        Logging.Debug($"Error finding lethal page with id {LoadoutTabPageID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!page.Gadgets.TryGetValue(selected, out LoadoutGadget gadget))
                                    {
                                        Logging.Debug($"Error finding lethal at {selected} at page {LoadoutTabPageID} for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGadget(gadget);
                                    break;
                                }

                            case ELoadoutPage.Tactical:
                                {
                                    if (!TacticalPages.TryGetValue(LoadoutTabPageID, out PageGadget page))
                                    {
                                        Logging.Debug($"Error finding tactical page with id {LoadoutTabPageID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!page.Gadgets.TryGetValue(selected, out LoadoutGadget gadget))
                                    {
                                        Logging.Debug($"Error finding tactical at {selected} at page {LoadoutTabPageID} for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGadget(gadget);
                                    break;
                                }

                            case ELoadoutPage.Knife:
                                {
                                    if (!KnifePages.TryGetValue(LoadoutTabPageID, out PageKnife page))
                                    {
                                        Logging.Debug($"Error finding knife page with id {LoadoutTabPageID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!page.Knives.TryGetValue(selected, out LoadoutKnife knife))
                                    {
                                        Logging.Debug($"Error finding knife at {selected} at page {LoadoutTabPageID} for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowKnife(knife);
                                    break;
                                }

                            case ELoadoutPage.Killstreak:
                                {
                                    if (!KillstreakPages.TryGetValue(LoadoutTabPageID, out PageKillstreak page))
                                    {
                                        Logging.Debug($"Error finding killstreak page with id {LoadoutTabPageID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!page.Killstreaks.TryGetValue(selected, out LoadoutKillstreak killstreak))
                                    {
                                        Logging.Debug($"Error finding killstreak at {selected} at page {LoadoutTabPageID} for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowKillstreak(killstreak);
                                    break;
                                }

                            case ELoadoutPage.Glove:
                                {
                                    if (!GlovePages.TryGetValue(LoadoutTabPageID, out PageGlove page))
                                    {
                                        Logging.Debug($"Error finding glove page with id {LoadoutTabPageID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!page.Gloves.TryGetValue(selected, out LoadoutGlove glove))
                                    {
                                        Logging.Debug($"Error finding glove at {selected} at page {LoadoutTabPageID} for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGlove(glove);
                                    break;
                                }

                            case ELoadoutPage.Card:
                                {
                                    if (!CardPages.TryGetValue(LoadoutTabPageID, out PageCard page))
                                    {
                                        Logging.Debug($"Error finding card page with id {LoadoutTabPageID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!page.Cards.TryGetValue(selected, out LoadoutCard card))
                                    {
                                        Logging.Debug($"Error finding card at {selected} at page {LoadoutTabPageID} for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowCard(card);
                                    break;
                                }
                        }
                        break;
                    }

                case ELoadoutTab.SKINS:
                    {
                        switch (LoadoutPage)
                        {
                            case ELoadoutPage.Primary:
                                {
                                    if (!GunSkinPages.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out Dictionary<int, PageGunSkin> skinsPage))
                                    {
                                        Logging.Debug($"Error finding gun skin pages for primary with id {loadout.Primary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!skinsPage.TryGetValue(LoadoutTabPageID, out PageGunSkin pageSkin))
                                    {
                                        Logging.Debug($"Error finding gun skin page at id {LoadoutTabPageID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!pageSkin.GunSkins.TryGetValue(selected, out GunSkin skin))
                                    {
                                        Logging.Debug($"Error finding skin at {selected} at page with id {LoadoutTabPageID} for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGunSkin(skin);
                                    break;
                                }

                            case ELoadoutPage.Secondary:
                                {
                                    if (!GunSkinPages.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out Dictionary<int, PageGunSkin> skinsPage))
                                    {
                                        Logging.Debug($"Error finding gun skin pages for secondary with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!skinsPage.TryGetValue(LoadoutTabPageID, out PageGunSkin pageSkin))
                                    {
                                        Logging.Debug($"Error finding gun skin page at id {LoadoutTabPageID} for {Player.CharacterName}");
                                        return;
                                    }

                                    if (!pageSkin.GunSkins.TryGetValue(selected, out GunSkin skin))
                                    {
                                        Logging.Debug($"Error finding skin at {selected} at page with id {LoadoutTabPageID} for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowGunSkin(skin);
                                    break;
                                }
                        }
                        break;
                    }

                case ELoadoutTab.PISTOLS:
                    {
                        if (!PistolPages.TryGetValue(LoadoutTabPageID, out PageGun page))
                        {
                            Logging.Debug($"Error finding pistol page {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        if (!page.Guns.TryGetValue(selected, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding gun at {selected} for page with id {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGun(gun);
                        break;
                    }

                case ELoadoutTab.SUBMACHINE_GUNS:
                    {
                        if (!SMGPages.TryGetValue(LoadoutTabPageID, out PageGun page))
                        {
                            Logging.Debug($"Error finding smg page {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        if (!page.Guns.TryGetValue(selected, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding gun at {selected} for page with id {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGun(gun);
                        break;
                    }

                case ELoadoutTab.SHOTGUNS:
                    {
                        if (!ShotgunPages.TryGetValue(LoadoutTabPageID, out PageGun page))
                        {
                            Logging.Debug($"Error finding shotgun page {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        if (!page.Guns.TryGetValue(selected, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding gun at {selected} for page with id {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGun(gun);
                        break;
                    }

                case ELoadoutTab.SNIPER_RIFLES:
                    {
                        if (!SniperPages.TryGetValue(LoadoutTabPageID, out PageGun page))
                        {
                            Logging.Debug($"Error finding sniper page {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        if (!page.Guns.TryGetValue(selected, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding gun at {selected} for page with id {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGun(gun);
                        break;
                    }

                case ELoadoutTab.LIGHT_MACHINE_GUNS:
                    {
                        if (!LMGPages.TryGetValue(LoadoutTabPageID, out PageGun page))
                        {
                            Logging.Debug($"Error finding lmg page {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        if (!page.Guns.TryGetValue(selected, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding gun at {selected} for page with id {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGun(gun);
                        break;
                    }

                case ELoadoutTab.ASSAULT_RIFLES:
                    {
                        if (!ARPages.TryGetValue(LoadoutTabPageID, out PageGun page))
                        {
                            Logging.Debug($"Error finding ar page {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        if (!page.Guns.TryGetValue(selected, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding gun at {selected} for page with id {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGun(gun);
                        break;
                    }
            }
        }

        public void ShowGun(LoadoutGun gun)
        {
            SelectedItemID = gun.Gun.GunID;
            Logging.Debug($"Showing gun with id {gun.Gun.GunID} to {Player.CharacterName}");
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Buy BUTTON", !gun.IsBought && PlayerData.Level >= gun.Gun.LevelRequirement);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Buy TEXT", !gun.IsBought && PlayerData.Level >= gun.Gun.LevelRequirement ? $"BUY ({gun.Gun.BuyPrice} CREDITS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Unlock BUTTON", !gun.IsBought && gun.Gun.LevelRequirement > PlayerData.Level);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Unlock TEXT", !gun.IsBought && gun.Gun.LevelRequirement > PlayerData.Level ? $"UNLOCK ({gun.Gun.Coins} COINS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Equip BUTTON", gun.IsBought && ((LoadoutPage == ELoadoutPage.Primary && loadout.Primary != gun) || (LoadoutPage == ELoadoutPage.Secondary && loadout.Secondary != gun)));
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", gun.IsBought && ((LoadoutPage == ELoadoutPage.Primary && loadout.Primary == gun) || (LoadoutPage == ELoadoutPage.Secondary && loadout.Secondary == gun)));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Description TEXT", gun.Gun.GunDesc);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Item IMAGE", gun.Gun.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item TEXT", gun.Gun.GunName);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Level TEXT", gun.Level.ToString());
            gun.TryGetNeededXP(out int neededXP);
            var spaces = neededXP != 0 ? (gun.XP * 97 / neededXP) : 0;
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item XP Bar Fill", spaces == 0 ? "" : new string(' ', spaces));
        }

        public void ShowAttachment(LoadoutAttachment attachment, LoadoutGun gun)
        {
            SelectedItemID = attachment.Attachment.AttachmentID;
            Logging.Debug($"Showing attachment with id {attachment.Attachment.AttachmentID} to {Player.CharacterName}");
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Buy BUTTON", !attachment.IsBought && gun.Level >= attachment.LevelRequirement);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Buy TEXT", !attachment.IsBought && gun.Level >= attachment.LevelRequirement ? $"BUY ({attachment.Attachment.BuyPrice} CREDITS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Unlock BUTTON", !attachment.IsBought && attachment.LevelRequirement > gun.Level);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Unlock TEXT", !attachment.IsBought && attachment.LevelRequirement > gun.Level ? $"UNLOCK ({attachment.Attachment.Coins} COINS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Equip BUTTON", attachment.IsBought && ((LoadoutPage.ToString().StartsWith("AttachmentPrimary") && !loadout.PrimaryAttachments.ContainsValue(attachment)) || (LoadoutPage.ToString().StartsWith("AttachmentSecondary") && !loadout.SecondaryAttachments.ContainsValue(attachment))));
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", attachment.IsBought && ((LoadoutPage.ToString().StartsWith("AttachmentPrimary") && loadout.PrimaryAttachments.ContainsValue(attachment)) || (LoadoutPage.ToString().StartsWith("AttachmentSecondary") && loadout.SecondaryAttachments.ContainsValue(attachment))));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Description TEXT", attachment.Attachment.AttachmentDesc);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Item IMAGE", attachment.Attachment.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item TEXT", attachment.Attachment.AttachmentName);
        }

        public void ShowGunCharm(LoadoutGunCharm gunCharm)
        {
            SelectedItemID = gunCharm.GunCharm.CharmID;
            Logging.Debug($"Showing gun charm with id {gunCharm.GunCharm.CharmID} to {Player.CharacterName}");
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Buy BUTTON", !gunCharm.IsBought && PlayerData.Level >= gunCharm.GunCharm.LevelRequirement);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Buy TEXT", !gunCharm.IsBought && PlayerData.Level >= gunCharm.GunCharm.LevelRequirement ? $"BUY ({gunCharm.GunCharm.BuyPrice} CREDITS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Unlock BUTTON", !gunCharm.IsBought && gunCharm.GunCharm.LevelRequirement > PlayerData.Level);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Unlock TEXT", !gunCharm.IsBought && gunCharm.GunCharm.LevelRequirement > PlayerData.Level ? $"UNLOCK ({gunCharm.GunCharm.Coins} COINS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Equip BUTTON", gunCharm.IsBought && ((LoadoutPage.ToString().StartsWith("AttachmentPrimary") && loadout.PrimaryGunCharm != gunCharm) || (LoadoutPage.ToString().StartsWith("AttachmentSecondary") && loadout.SecondaryGunCharm != gunCharm)));
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", gunCharm.IsBought && ((LoadoutPage.ToString().StartsWith("AttachmentPrimary") && loadout.PrimaryGunCharm == gunCharm) || (LoadoutPage.ToString().StartsWith("AttachmentSecondary") && loadout.SecondaryGunCharm == gunCharm)));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Description TEXT", gunCharm.GunCharm.CharmDesc);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Item IMAGE", gunCharm.GunCharm.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item TEXT", gunCharm.GunCharm.CharmName);
        }

        public void ShowGunSkin(GunSkin skin)
        {
            SelectedItemID = skin.ID;
            Logging.Debug($"Showing gun skin with id {skin.ID} to {Player.CharacterName}");
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Buy BUTTON", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Unlock BUTTON", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Equip BUTTON", (LoadoutPage == ELoadoutPage.Primary && loadout.PrimarySkin != skin) || (LoadoutPage == ELoadoutPage.Secondary && loadout.SecondarySkin != skin));
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", (LoadoutPage == ELoadoutPage.Primary && loadout.PrimarySkin == skin) || (LoadoutPage == ELoadoutPage.Secondary && loadout.SecondarySkin == skin));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Description TEXT", skin.SkinDesc);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Item IMAGE", skin.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item TEXT", skin.SkinName);
        }

        public void ShowKnife(LoadoutKnife knife)
        {
            SelectedItemID = knife.Knife.KnifeID;
            Logging.Debug($"Showing knife with id {knife.Knife.KnifeID} to {Player.CharacterName}");
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Buy BUTTON", !knife.IsBought && PlayerData.Level >= knife.Knife.LevelRequirement);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Buy TEXT", !knife.IsBought && PlayerData.Level >= knife.Knife.LevelRequirement ? $"BUY ({knife.Knife.BuyPrice} CREDITS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Unlock BUTTON", !knife.IsBought && knife.Knife.LevelRequirement > PlayerData.Level);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Unlock TEXT", !knife.IsBought && knife.Knife.LevelRequirement > PlayerData.Level ? $"UNLOCK ({knife.Knife.Coins} COINS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Equip BUTTON", knife.IsBought && loadout.Knife != knife);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", false);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Description TEXT", knife.Knife.KnifeDesc);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Item IMAGE", knife.Knife.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item TEXT", knife.Knife.KnifeName);
        }

        public void ShowPerk(LoadoutPerk perk)
        {
            SelectedItemID = perk.Perk.PerkID;
            Logging.Debug($"Showing perk with id {perk.Perk.PerkID} to {Player.CharacterName}");
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Buy BUTTON", !perk.IsBought && PlayerData.Level >= perk.Perk.LevelRequirement);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Buy TEXT", !perk.IsBought && PlayerData.Level >= perk.Perk.LevelRequirement ? $"BUY ({perk.Perk.BuyPrice} CREDITS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Unlock BUTTON", !perk.IsBought && perk.Perk.LevelRequirement > PlayerData.Level);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Unlock TEXT", !perk.IsBought && perk.Perk.LevelRequirement > PlayerData.Level ? $"UNLOCK ({perk.Perk.Coins} COINS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Equip BUTTON", perk.IsBought && !loadout.Perks.Contains(perk));
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", perk.IsBought && loadout.Perks.Contains(perk));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Description TEXT", perk.Perk.PerkDesc);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Item IMAGE", perk.Perk.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item TEXT", perk.Perk.PerkName);
        }

        public void ShowGadget(LoadoutGadget gadget)
        {
            SelectedItemID = gadget.Gadget.GadgetID;
            Logging.Debug($"Showing gadget with id {gadget.Gadget.GadgetID} to {Player.CharacterName}");
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Buy BUTTON", !gadget.IsBought && PlayerData.Level >= gadget.Gadget.LevelRequirement);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Buy TEXT", !gadget.IsBought && PlayerData.Level >= gadget.Gadget.LevelRequirement ? $"BUY ({gadget.Gadget.BuyPrice} CREDITS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Unlock BUTTON", !gadget.IsBought && gadget.Gadget.LevelRequirement > PlayerData.Level);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Unlock TEXT", !gadget.IsBought && gadget.Gadget.LevelRequirement > PlayerData.Level ? $"UNLOCK ({gadget.Gadget.Coins} COINS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Equip BUTTON", gadget.IsBought && ((LoadoutPage == ELoadoutPage.Tactical && loadout.Tactical != gadget) || (LoadoutPage == ELoadoutPage.Lethal && loadout.Lethal != gadget)));
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", gadget.IsBought && ((LoadoutPage == ELoadoutPage.Tactical && loadout.Tactical == gadget) || (LoadoutPage == ELoadoutPage.Lethal && loadout.Lethal == gadget)));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Description TEXT", gadget.Gadget.GadgetDesc);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Item IMAGE", gadget.Gadget.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item TEXT", gadget.Gadget.GadgetName);
        }

        public void ShowCard(LoadoutCard card)
        {
            SelectedItemID = card.Card.CardID;
            Logging.Debug($"Showing card with id {card.Card.CardID} to {Player.CharacterName}");
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Buy BUTTON", !card.IsBought && PlayerData.Level >= card.Card.LevelRequirement);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Buy TEXT", !card.IsBought && PlayerData.Level >= card.Card.LevelRequirement ? $"BUY ({card.Card.BuyPrice} CREDITS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Unlock BUTTON", !card.IsBought && card.Card.LevelRequirement > PlayerData.Level);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Unlock TEXT", !card.IsBought && card.Card.LevelRequirement > PlayerData.Level ? $"UNLOCK ({card.Card.Coins} COINS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Equip BUTTON", card.IsBought && loadout.Card != card);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", card.IsBought && loadout.Card == card);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Description TEXT", card.Card.CardDesc);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Item IMAGE", card.Card.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item TEXT", card.Card.CardName);
        }

        public void ShowGlove(LoadoutGlove glove)
        {
            SelectedItemID = glove.Glove.GloveID;
            Logging.Debug($"Showing glove with id {glove.Glove.GloveID} to {Player.CharacterName}");
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Buy BUTTON", !glove.IsBought && PlayerData.Level >= glove.Glove.LevelRequirement);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Buy TEXT", !glove.IsBought && PlayerData.Level >= glove.Glove.LevelRequirement ? $"BUY ({glove.Glove.BuyPrice} CREDITS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Unlock BUTTON", !glove.IsBought && glove.Glove.LevelRequirement > PlayerData.Level);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Unlock TEXT", !glove.IsBought && glove.Glove.LevelRequirement > PlayerData.Level ? $"UNLOCK ({glove.Glove.Coins} COINS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Equip BUTTON", glove.IsBought && loadout.Glove != glove);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", glove.IsBought && loadout.Glove == glove);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Description TEXT", glove.Glove.GloveDesc);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Item IMAGE", glove.Glove.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item TEXT", glove.Glove.GloveName);
        }

        public void ShowKillstreak(LoadoutKillstreak killstreak)
        {
            SelectedItemID = killstreak.Killstreak.KillstreakID;
            Logging.Debug($"Showing killstreak with id {killstreak.Killstreak.KillstreakID} to {Player.CharacterName}");
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Buy BUTTON", !killstreak.IsBought && PlayerData.Level >= killstreak.Killstreak.LevelRequirement);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Buy TEXT", !killstreak.IsBought && PlayerData.Level >= killstreak.Killstreak.LevelRequirement ? $"BUY ({killstreak.Killstreak.BuyPrice} CREDITS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Unlock BUTTON", !killstreak.IsBought && killstreak.Killstreak.LevelRequirement > PlayerData.Level);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Unlock TEXT", !killstreak.IsBought && killstreak.Killstreak.LevelRequirement > PlayerData.Level ? $"UNLOCK ({killstreak.Killstreak.Coins} COINS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Equip BUTTON", killstreak.IsBought && !loadout.Killstreaks.Contains(killstreak));
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", killstreak.IsBought && loadout.Killstreaks.Contains(killstreak));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Description TEXT", killstreak.Killstreak.KillstreakDesc);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Item IMAGE", killstreak.Killstreak.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item TEXT", killstreak.Killstreak.KillstreakName);
        }

        public void BuySelectedItem()
        {
            Logging.Debug($"{Player.CharacterName} trying to buy selected item with id {SelectedItemID}, page {LoadoutPage}, tab {LoadoutTab}");
            if (!DB.PlayerData.TryGetValue(Player.CSteamID, out PlayerData data))
            {
                Logging.Debug($"Error finding player data with steam id {Player.CSteamID}");
                return;
            }

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            switch (LoadoutPage)
            {
                case ELoadoutPage.Primary:
                    {
                        if (!PlayerLoadout.Guns.TryGetValue((ushort)SelectedItemID, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding gun with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Credits >= gun.Gun.BuyPrice && !gun.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, (uint)gun.Gun.BuyPrice);
                                await DB.UpdatePlayerGunBoughtAsync(Player.CSteamID, gun.Gun.GunID, true);
                                TaskDispatcher.QueueOnMainThread(() => ReloadSelectedItem());
                            }
                        });
                        break;
                    }

                case ELoadoutPage.AttachmentPrimaryBarrel:
                case ELoadoutPage.AttachmentPrimaryGrip:
                case ELoadoutPage.AttachmentPrimaryMagazine:
                case ELoadoutPage.AttachmentPrimarySights:
                    {
                        if (!PlayerLoadout.Guns.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding primary with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        if (!gun.Attachments.TryGetValue((ushort)SelectedItemID, out LoadoutAttachment attachment))
                        {
                            Logging.Debug($"Error finding attachment with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Credits >= attachment.Attachment.BuyPrice && !attachment.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, (uint)attachment.Attachment.BuyPrice);
                                await DB.UpdatePlayerGunAttachmentBoughtAsync(Player.CSteamID, gun.Gun.GunID, attachment.Attachment.AttachmentID, true);
                                TaskDispatcher.QueueOnMainThread(() => ReloadSelectedItem());
                            }
                        });
                        break;
                    }

                case ELoadoutPage.Secondary:
                    {
                        if (!PlayerLoadout.Guns.TryGetValue((ushort)SelectedItemID, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding gun with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Credits >= gun.Gun.BuyPrice && !gun.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, (uint)gun.Gun.BuyPrice);
                                await DB.UpdatePlayerGunBoughtAsync(Player.CSteamID, gun.Gun.GunID, true);
                                TaskDispatcher.QueueOnMainThread(() => ReloadSelectedItem());
                            }
                        });
                        break;
                    }

                case ELoadoutPage.AttachmentSecondaryBarrel:
                case ELoadoutPage.AttachmentSecondaryMagazine:
                case ELoadoutPage.AttachmentSecondarySights:
                    {
                        if (!PlayerLoadout.Guns.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding secondary with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        if (!gun.Attachments.TryGetValue((ushort)SelectedItemID, out LoadoutAttachment attachment))
                        {
                            Logging.Debug($"Error finding attachment with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Credits >= attachment.Attachment.BuyPrice && !attachment.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, (uint)attachment.Attachment.BuyPrice);
                                await DB.UpdatePlayerGunAttachmentBoughtAsync(Player.CSteamID, gun.Gun.GunID, attachment.Attachment.AttachmentID, true);
                                TaskDispatcher.QueueOnMainThread(() => ReloadSelectedItem());
                            }
                        });
                        break;
                    }

                case ELoadoutPage.AttachmentPrimaryCharm:
                case ELoadoutPage.AttachmentSecondaryCharm:
                    {
                        if (!PlayerLoadout.GunCharms.TryGetValue((ushort)SelectedItemID, out LoadoutGunCharm gunCharm))
                        {
                            Logging.Debug($"Error finding gun charm with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Credits >= gunCharm.GunCharm.BuyPrice && !gunCharm.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, (uint)gunCharm.GunCharm.BuyPrice);
                                await DB.UpdatePlayerGunCharmBoughtAsync(Player.CSteamID, gunCharm.GunCharm.CharmID, true);
                                TaskDispatcher.QueueOnMainThread(() => ReloadSelectedItem());
                            }
                        });
                        break;
                    }

                case ELoadoutPage.Knife:
                    {
                        if (!PlayerLoadout.Knives.TryGetValue((ushort)SelectedItemID, out LoadoutKnife knife))
                        {
                            Logging.Debug($"Error finding knife with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Credits >= knife.Knife.BuyPrice && !knife.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, (uint)knife.Knife.BuyPrice);
                                await DB.UpdatePlayerKnifeBoughtAsync(Player.CSteamID, knife.Knife.KnifeID, true);
                                TaskDispatcher.QueueOnMainThread(() => ReloadSelectedItem());
                            }
                        });
                        break;
                    }

                case ELoadoutPage.Tactical:
                    {
                        if (!PlayerLoadout.Gadgets.TryGetValue((ushort)SelectedItemID, out LoadoutGadget gadget))
                        {
                            Logging.Debug($"Error finding gadget with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Credits >= gadget.Gadget.BuyPrice && !gadget.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, (uint)gadget.Gadget.BuyPrice);
                                await DB.UpdatePlayerGadgetBoughtAsync(Player.CSteamID, gadget.Gadget.GadgetID, true);
                                TaskDispatcher.QueueOnMainThread(() => ReloadSelectedItem());
                            }
                        });
                        break;
                    }

                case ELoadoutPage.Lethal:
                    {
                        if (!PlayerLoadout.Gadgets.TryGetValue((ushort)SelectedItemID, out LoadoutGadget gadget))
                        {
                            Logging.Debug($"Error finding gadget with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Credits >= gadget.Gadget.BuyPrice && !gadget.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, (uint)gadget.Gadget.BuyPrice);
                                await DB.UpdatePlayerGadgetBoughtAsync(Player.CSteamID, gadget.Gadget.GadgetID, true);
                                TaskDispatcher.QueueOnMainThread(() => ReloadSelectedItem());
                            }
                        });
                        break;
                    }

                case ELoadoutPage.Perk:
                    {
                        if (!PlayerLoadout.Perks.TryGetValue((int)SelectedItemID, out LoadoutPerk perk))
                        {
                            Logging.Debug($"Error finding perk with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Credits >= perk.Perk.BuyPrice && !perk.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, (uint)perk.Perk.BuyPrice);
                                await DB.UpdatePlayerPerkBoughtAsync(Player.CSteamID, perk.Perk.PerkID, true);
                                TaskDispatcher.QueueOnMainThread(() => ReloadSelectedItem());
                            }
                        });
                        break;
                    }

                case ELoadoutPage.Killstreak:
                    {
                        if (!PlayerLoadout.Killstreaks.TryGetValue((int)SelectedItemID, out LoadoutKillstreak killstreak))
                        {
                            Logging.Debug($"Error finding killstreak with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Credits >= killstreak.Killstreak.BuyPrice && !killstreak.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, (uint)killstreak.Killstreak.BuyPrice);
                                await DB.UpdatePlayerKillstreakBoughtAsync(Player.CSteamID, killstreak.Killstreak.KillstreakID, true);
                                TaskDispatcher.QueueOnMainThread(() => ReloadSelectedItem());
                            }
                        });
                        break;
                    }

                case ELoadoutPage.Card:
                    {
                        if (!PlayerLoadout.Cards.TryGetValue((int)SelectedItemID, out LoadoutCard card))
                        {
                            Logging.Debug($"Error finding card with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Credits >= card.Card.BuyPrice && !card.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, (uint)card.Card.BuyPrice);
                                await DB.UpdatePlayerCardBoughtAsync(Player.CSteamID, card.Card.CardID, true);
                                TaskDispatcher.QueueOnMainThread(() => ReloadSelectedItem());
                            }
                        });
                        break;
                    }

                case ELoadoutPage.Glove:
                    {
                        if (!PlayerLoadout.Gloves.TryGetValue((ushort)SelectedItemID, out LoadoutGlove glove))
                        {
                            Logging.Debug($"Error finding glove with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Credits >= glove.Glove.BuyPrice && !glove.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, (uint)glove.Glove.BuyPrice);
                                await DB.UpdatePlayerGloveBoughtAsync(Player.CSteamID, glove.Glove.GloveID, true);
                                TaskDispatcher.QueueOnMainThread(() => ReloadSelectedItem());
                            }
                        });
                        break;
                    }
            }
        }

        public void UnlockSelectedItem()
        {
            Logging.Debug($"{Player.CharacterName} trying to unlock selected item with id {SelectedItemID}, page {LoadoutPage}, tab {LoadoutTab}");
            if (!DB.PlayerData.TryGetValue(Player.CSteamID, out PlayerData data))
            {
                Logging.Debug($"Error finding player data with steam id {Player.CSteamID}");
                return;
            }

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            switch (LoadoutPage)
            {
                case ELoadoutPage.Primary:
                    {
                        if (!PlayerLoadout.Guns.TryGetValue((ushort)SelectedItemID, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding gun with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Coins >= gun.Gun.Coins && !gun.IsBought && gun.Gun.LevelRequirement > PlayerData.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, (uint)gun.Gun.Coins);
                                await DB.UpdatePlayerGunBoughtAsync(Player.CSteamID, gun.Gun.GunID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    ReloadSelectedItem();
                                    ReloadLoadoutTab();
                                });
                            }
                        });
                        break;
                    }

                case ELoadoutPage.AttachmentPrimaryBarrel:
                case ELoadoutPage.AttachmentPrimaryGrip:
                case ELoadoutPage.AttachmentPrimaryMagazine:
                case ELoadoutPage.AttachmentPrimarySights:
                    {
                        if (!PlayerLoadout.Guns.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding primary with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        if (!gun.Attachments.TryGetValue((ushort)SelectedItemID, out LoadoutAttachment attachment))
                        {
                            Logging.Debug($"Error finding attachment with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Coins >= attachment.Attachment.Coins && !attachment.IsBought && attachment.LevelRequirement > gun.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, (uint)attachment.Attachment.Coins);
                                await DB.UpdatePlayerGunAttachmentBoughtAsync(Player.CSteamID, gun.Gun.GunID, attachment.Attachment.AttachmentID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    ReloadSelectedItem();
                                    ReloadLoadoutTab();
                                });
                            }
                        });
                        break;
                    }

                case ELoadoutPage.Secondary:
                    {
                        if (!PlayerLoadout.Guns.TryGetValue((ushort)SelectedItemID, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding gun with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Coins >= gun.Gun.Coins && !gun.IsBought && gun.Gun.LevelRequirement > PlayerData.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, (uint)gun.Gun.Coins);
                                await DB.UpdatePlayerGunBoughtAsync(Player.CSteamID, gun.Gun.GunID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    ReloadSelectedItem();
                                    ReloadLoadoutTab();
                                });
                            }
                        });
                        break;
                    }

                case ELoadoutPage.AttachmentSecondaryBarrel:
                case ELoadoutPage.AttachmentSecondaryMagazine:
                case ELoadoutPage.AttachmentSecondarySights:
                    {
                        if (!PlayerLoadout.Guns.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding secondary with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        if (!gun.Attachments.TryGetValue((ushort)SelectedItemID, out LoadoutAttachment attachment))
                        {
                            Logging.Debug($"Error finding attachment with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Coins >= attachment.Attachment.Coins && !attachment.IsBought && attachment.LevelRequirement > gun.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, (uint)attachment.Attachment.Coins);
                                await DB.UpdatePlayerGunAttachmentBoughtAsync(Player.CSteamID, gun.Gun.GunID, attachment.Attachment.AttachmentID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    ReloadSelectedItem();
                                    ReloadLoadoutTab();
                                });
                            }
                        });
                        break;
                    }

                case ELoadoutPage.AttachmentPrimaryCharm:
                case ELoadoutPage.AttachmentSecondaryCharm:
                    {
                        if (!PlayerLoadout.GunCharms.TryGetValue((ushort)SelectedItemID, out LoadoutGunCharm gunCharm))
                        {
                            Logging.Debug($"Error finding gun charm with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Coins >= gunCharm.GunCharm.Coins && !gunCharm.IsBought && gunCharm.GunCharm.LevelRequirement > PlayerData.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, (uint)gunCharm.GunCharm.Coins);
                                await DB.UpdatePlayerGunCharmBoughtAsync(Player.CSteamID, gunCharm.GunCharm.CharmID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    ReloadSelectedItem();
                                    ReloadLoadoutTab();
                                });
                            }
                        });
                        break;
                    }

                case ELoadoutPage.Knife:
                    {
                        if (!PlayerLoadout.Knives.TryGetValue((ushort)SelectedItemID, out LoadoutKnife knife))
                        {
                            Logging.Debug($"Error finding knife with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Coins >= knife.Knife.Coins && !knife.IsBought && knife.Knife.LevelRequirement > PlayerData.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, (uint)knife.Knife.Coins);
                                await DB.UpdatePlayerKnifeBoughtAsync(Player.CSteamID, knife.Knife.KnifeID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    ReloadSelectedItem();
                                    ReloadLoadoutTab();
                                });
                            }
                        });
                        break;
                    }

                case ELoadoutPage.Tactical:
                    {
                        if (!PlayerLoadout.Gadgets.TryGetValue((ushort)SelectedItemID, out LoadoutGadget gadget))
                        {
                            Logging.Debug($"Error finding gadget with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Coins >= gadget.Gadget.Coins && !gadget.IsBought && gadget.Gadget.LevelRequirement > PlayerData.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, (uint)gadget.Gadget.Coins);
                                await DB.UpdatePlayerGadgetBoughtAsync(Player.CSteamID, gadget.Gadget.GadgetID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    ReloadSelectedItem();
                                    ReloadLoadoutTab();
                                });
                            }
                        });
                        break;
                    }

                case ELoadoutPage.Lethal:
                    {
                        if (!PlayerLoadout.Gadgets.TryGetValue((ushort)SelectedItemID, out LoadoutGadget gadget))
                        {
                            Logging.Debug($"Error finding gadget with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Coins >= gadget.Gadget.Coins && !gadget.IsBought && gadget.Gadget.LevelRequirement > PlayerData.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, (uint)gadget.Gadget.Coins);
                                await DB.UpdatePlayerGadgetBoughtAsync(Player.CSteamID, gadget.Gadget.GadgetID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    ReloadSelectedItem();
                                    ReloadLoadoutTab();
                                });
                            }
                        });
                        break;
                    }

                case ELoadoutPage.Perk:
                    {
                        if (!PlayerLoadout.Perks.TryGetValue((int)SelectedItemID, out LoadoutPerk perk))
                        {
                            Logging.Debug($"Error finding perk with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Coins >= perk.Perk.Coins && !perk.IsBought && perk.Perk.LevelRequirement > PlayerData.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, (uint)perk.Perk.Coins);
                                await DB.UpdatePlayerPerkBoughtAsync(Player.CSteamID, perk.Perk.PerkID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    ReloadSelectedItem();
                                    ReloadLoadoutTab();
                                });
                            }
                        });
                        break;
                    }

                case ELoadoutPage.Killstreak:
                    {
                        if (!PlayerLoadout.Killstreaks.TryGetValue((int)SelectedItemID, out LoadoutKillstreak killstreak))
                        {
                            Logging.Debug($"Error finding killstreak with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Coins >= killstreak.Killstreak.Coins && !killstreak.IsBought && killstreak.Killstreak.LevelRequirement > PlayerData.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, (uint)killstreak.Killstreak.Coins);
                                await DB.UpdatePlayerKillstreakBoughtAsync(Player.CSteamID, killstreak.Killstreak.KillstreakID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    ReloadSelectedItem();
                                    ReloadLoadoutTab();
                                });
                            }
                        });
                        break;
                    }

                case ELoadoutPage.Card:
                    {
                        if (!PlayerLoadout.Cards.TryGetValue((int)SelectedItemID, out LoadoutCard card))
                        {
                            Logging.Debug($"Error finding card with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Coins >= card.Card.Coins && !card.IsBought && card.Card.LevelRequirement > PlayerData.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, (uint)card.Card.Coins);
                                await DB.UpdatePlayerCardBoughtAsync(Player.CSteamID, card.Card.CardID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    ReloadSelectedItem();
                                    ReloadLoadoutTab();
                                });
                            }
                        });
                        break;
                    }

                case ELoadoutPage.Glove:
                    {
                        if (!PlayerLoadout.Gloves.TryGetValue((ushort)SelectedItemID, out LoadoutGlove glove))
                        {
                            Logging.Debug($"Error finding glove with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (data.Coins >= glove.Glove.Coins && !glove.IsBought && glove.Glove.LevelRequirement > PlayerData.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, (uint)glove.Glove.Coins);
                                await DB.UpdatePlayerGloveBoughtAsync(Player.CSteamID, glove.Glove.GloveID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    ReloadSelectedItem();
                                    ReloadLoadoutTab();
                                });
                            }
                        });
                        break;
                    }
            }
        }

        public void EquipSelectedItem()
        {
            Logging.Debug($"{Player.CharacterName} trying to equip selected item with id {SelectedItemID}, page {LoadoutPage}, tab {LoadoutTab}");
            var loadoutManager = Plugin.Instance.LoadoutManager;
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding selected loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            if (LoadoutTab == ELoadoutTab.SKINS)
            {
                switch (LoadoutPage)
                {
                    case ELoadoutPage.Primary:
                        {
                            if (!PlayerLoadout.GunSkinsSearchByID.TryGetValue((int)SelectedItemID, out GunSkin skin))
                            {
                                Logging.Debug($"Error finding gun skin with id {SelectedItemID} for {Player.CharacterName}");
                                return;
                            }

                            loadoutManager.EquipGunSkin(Player, LoadoutID, skin.ID, true);
                            break;
                        }

                    case ELoadoutPage.Secondary:
                        {
                            if (!PlayerLoadout.GunSkinsSearchByID.TryGetValue((int)SelectedItemID, out GunSkin skin))
                            {
                                Logging.Debug($"Error finding gun skin with id {SelectedItemID} for {Player.CharacterName}");
                                return;
                            }

                            loadoutManager.EquipGunSkin(Player, LoadoutID, skin.ID, false);
                            break;
                        }
                }

                ReloadLoadout();
                return;
            }

            switch (LoadoutPage)
            {
                case ELoadoutPage.Primary:
                    {
                        if (!PlayerLoadout.Guns.TryGetValue((ushort)SelectedItemID, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding gun with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        if (gun.IsBought)
                        {
                            loadoutManager.EquipGun(Player, LoadoutID, gun.Gun.GunID, true);
                        }

                        break;
                    }

                case ELoadoutPage.AttachmentPrimaryBarrel:
                case ELoadoutPage.AttachmentPrimaryGrip:
                case ELoadoutPage.AttachmentPrimaryMagazine:
                case ELoadoutPage.AttachmentPrimarySights:
                    {
                        if (!PlayerLoadout.Guns.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding gun with id {loadout.Primary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                            return;
                        }

                        if (!gun.Attachments.TryGetValue((ushort)SelectedItemID, out LoadoutAttachment attachment))
                        {
                            Logging.Debug($"Error finding attachment with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        if (attachment.IsBought)
                        {
                            loadoutManager.EquipAttachment(Player, attachment.Attachment.AttachmentID, LoadoutID, true);
                        }

                        break;
                    }

                case ELoadoutPage.Secondary:
                    {
                        if (!PlayerLoadout.Guns.TryGetValue((ushort)SelectedItemID, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding primary with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        if (gun.IsBought)
                        {
                            loadoutManager.EquipGun(Player, LoadoutID, gun.Gun.GunID, false);
                        }

                        break;
                    }

                case ELoadoutPage.AttachmentSecondaryBarrel:
                case ELoadoutPage.AttachmentSecondaryMagazine:
                case ELoadoutPage.AttachmentSecondarySights:
                    {
                        if (!PlayerLoadout.Guns.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding secondary with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                            return;
                        }

                        if (!gun.Attachments.TryGetValue((ushort)SelectedItemID, out LoadoutAttachment attachment))
                        {
                            Logging.Debug($"Error finding attachment with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        if (attachment.IsBought)
                        {
                            loadoutManager.EquipAttachment(Player, attachment.Attachment.AttachmentID, LoadoutID, false);
                        }

                        break;
                    }

                case ELoadoutPage.AttachmentPrimaryCharm:
                case ELoadoutPage.AttachmentSecondaryCharm:
                    {
                        if (!PlayerLoadout.GunCharms.TryGetValue((ushort)SelectedItemID, out LoadoutGunCharm gunCharm))
                        {
                            Logging.Debug($"Error finding gun charm with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        if (gunCharm.IsBought)
                        {
                            loadoutManager.EquipGunCharm(Player, LoadoutID, gunCharm.GunCharm.CharmID, LoadoutPage.ToString().StartsWith("AttachmentPrimary"));
                        }
                        break;
                    }

                case ELoadoutPage.Tactical:
                    {
                        if (!PlayerLoadout.Gadgets.TryGetValue((ushort)SelectedItemID, out LoadoutGadget gadget))
                        {
                            Logging.Debug($"Error finding tactical with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        if (gadget.IsBought)
                        {
                            loadoutManager.EquipTactical(Player, LoadoutID, gadget.Gadget.GadgetID);
                        }

                        break;
                    }

                case ELoadoutPage.Lethal:
                    {
                        if (!PlayerLoadout.Gadgets.TryGetValue((ushort)SelectedItemID, out LoadoutGadget gadget))
                        {
                            Logging.Debug($"Error finding lethal with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        if (gadget.IsBought)
                        {
                            loadoutManager.EquipLethal(Player, LoadoutID, gadget.Gadget.GadgetID);
                        }

                        break;
                    }

                case ELoadoutPage.Perk:
                    {
                        if (!PlayerLoadout.Perks.TryGetValue((int)SelectedItemID, out LoadoutPerk perk))
                        {
                            Logging.Debug($"Error finding perk with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        if (perk.IsBought)
                        {
                            loadoutManager.EquipPerk(Player, LoadoutID, perk.Perk.PerkID);
                        }

                        break;
                    }

                case ELoadoutPage.Knife:
                    {
                        if (!PlayerLoadout.Knives.TryGetValue((ushort)SelectedItemID, out LoadoutKnife knife))
                        {
                            Logging.Debug($"Error finding knife with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        if (knife.IsBought)
                        {
                            loadoutManager.EquipKnife(Player, LoadoutID, knife.Knife.KnifeID);
                        }

                        break;
                    }

                case ELoadoutPage.Killstreak:
                    {
                        if (!PlayerLoadout.Killstreaks.TryGetValue((int)SelectedItemID, out LoadoutKillstreak killstreak))
                        {
                            Logging.Debug($"Error finding killstreak with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        if (killstreak.IsBought)
                        {
                            loadoutManager.EquipKillstreak(Player, LoadoutID, killstreak.Killstreak.KillstreakID);
                        }

                        break;
                    }

                case ELoadoutPage.Glove:
                    {
                        if (!PlayerLoadout.Gloves.TryGetValue((ushort)SelectedItemID, out LoadoutGlove glove))
                        {
                            Logging.Debug($"Error finding glove with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        if (glove.IsBought)
                        {
                            loadoutManager.EquipGlove(Player, LoadoutID, glove.Glove.GloveID);
                        }

                        break;
                    }

                case ELoadoutPage.Card:
                    {
                        if (!PlayerLoadout.Cards.TryGetValue((int)SelectedItemID, out LoadoutCard card))
                        {
                            Logging.Debug($"Error finding card with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        if (card.IsBought)
                        {
                            loadoutManager.EquipCard(Player, LoadoutID, card.Card.CardID);
                        }

                        break;
                    }
            }
            ReloadLoadout();
        }

        public void DequipSelectedItem()
        {
            Logging.Debug($"{Player.CharacterName} trying to dequip selected item with id {SelectedItemID}, page {LoadoutPage}, tab {LoadoutTab}");
            var loadoutManager = Plugin.Instance.LoadoutManager;
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding selected loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            if (LoadoutTab == ELoadoutTab.SKINS)
            {
                switch (LoadoutPage)
                {
                    case ELoadoutPage.Primary:
                        {
                            loadoutManager.DequipGunSkin(Player, LoadoutID, true);
                            break;
                        }

                    case ELoadoutPage.Secondary:
                        {
                            loadoutManager.DequipGunSkin(Player, LoadoutID, false);
                            break;
                        }
                }

                ReloadLoadout();
                return;
            }

            switch (LoadoutPage)
            {
                case ELoadoutPage.Primary:
                    {
                        loadoutManager.EquipGun(Player, LoadoutID, 0, true);
                        break;
                    }

                case ELoadoutPage.AttachmentPrimaryBarrel:
                case ELoadoutPage.AttachmentPrimaryGrip:
                case ELoadoutPage.AttachmentPrimaryMagazine:
                case ELoadoutPage.AttachmentPrimarySights:
                    {
                        if (!PlayerLoadout.Guns.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding gun with id {loadout.Primary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                            return;
                        }

                        if (!gun.Attachments.TryGetValue((ushort)SelectedItemID, out LoadoutAttachment attachment))
                        {
                            Logging.Debug($"Error finding attachment with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        loadoutManager.DequipAttachment(Player, attachment.Attachment.AttachmentID, LoadoutID, true);
                        break;
                    }

                case ELoadoutPage.Secondary:
                    {
                        loadoutManager.EquipGun(Player, LoadoutID, 0, false);
                        break;
                    }

                case ELoadoutPage.AttachmentSecondaryBarrel:
                case ELoadoutPage.AttachmentSecondaryMagazine:
                case ELoadoutPage.AttachmentSecondarySights:
                    {
                        if (!PlayerLoadout.Guns.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding secondary with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                            return;
                        }

                        if (!gun.Attachments.TryGetValue((ushort)SelectedItemID, out LoadoutAttachment attachment))
                        {
                            Logging.Debug($"Error finding attachment with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        loadoutManager.DequipAttachment(Player, attachment.Attachment.AttachmentID, LoadoutID, false);
                        break;
                    }

                case ELoadoutPage.AttachmentPrimaryCharm:
                case ELoadoutPage.AttachmentSecondaryCharm:
                    {
                        loadoutManager.EquipGunCharm(Player, LoadoutID, 0, LoadoutPage.ToString().StartsWith("AttachmentPrimary"));
                        break;
                    }

                case ELoadoutPage.Tactical:
                    {
                        loadoutManager.EquipTactical(Player, LoadoutID, 0);
                        break;
                    }

                case ELoadoutPage.Lethal:
                    {
                        loadoutManager.EquipLethal(Player, LoadoutID, 0);
                        break;
                    }

                case ELoadoutPage.Perk:
                    {
                        if (!PlayerLoadout.Perks.TryGetValue((int)SelectedItemID, out LoadoutPerk perk))
                        {
                            Logging.Debug($"Error finding perk with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        loadoutManager.DequipPerk(Player, LoadoutID, perk.Perk.PerkID);
                        break;
                    }

                case ELoadoutPage.Knife:
                    {
                        loadoutManager.EquipKnife(Player, LoadoutID, 0);
                        break;
                    }

                case ELoadoutPage.Killstreak:
                    {
                        if (!PlayerLoadout.Killstreaks.TryGetValue((int)SelectedItemID, out LoadoutKillstreak killstreak))
                        {
                            Logging.Debug($"Error finding killstreak with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        loadoutManager.DequipKillstreak(Player, LoadoutID, killstreak.Killstreak.KillstreakID);
                        break;
                    }

                case ELoadoutPage.Glove:
                    {
                        loadoutManager.EquipGlove(Player, LoadoutID, 0);
                        break;
                    }

                case ELoadoutPage.Card:
                    {
                        loadoutManager.EquipCard(Player, LoadoutID, 0);
                        break;
                    }
            }
            ReloadLoadout();
        }

        // Events

        public void OnXPChanged()
        {
            if (!Plugin.Instance.DBManager.PlayerData.TryGetValue(SteamID, out PlayerData data))
            {
                return;
            }

            var ui = Plugin.Instance.UIManager;
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER XP Num", Plugin.Instance.Translate("Level_Show", data.Level).ToRich());
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER XP Icon", Plugin.Instance.DBManager.Levels.TryGetValue((int)data.Level, out XPLevel level) ? level.IconLinkMedium : "");
            int spaces = 0;
            if (data.TryGetNeededXP(out int neededXP))
            {
                spaces = Math.Min(96, neededXP == 0 ? 0 : (int)(data.XP * 96 / neededXP));
            }
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER XP Bar Fill", spaces == 0 ? " " : new string(' ', spaces));
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
