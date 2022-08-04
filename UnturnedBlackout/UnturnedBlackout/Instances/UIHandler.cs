﻿using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;
using UnturnedBlackout.GameTypes;
using UnturnedBlackout.Managers;
using UnturnedBlackout.Models.Global;
using UnturnedBlackout.Models.UI;

namespace UnturnedBlackout.Instances
{
    public class UIHandler
    {
        public const ushort ID = 27632;
        public const short Key = 27632;
        public const ushort MidgameLoadoutSelectionID = 27643;
        public const short MidgameLoadoutSelectionKey = 27643;
        public const int MaxItemsPerPage = 19;
        public const int MaxSkinsCharmsPerPage = 15;

        public DatabaseManager DB { get; set; }
        public CSteamID SteamID { get; set; }
        public UnturnedPlayer Player { get; set; }
        public PlayerLoadout PlayerLoadout { get; set; }
        public PlayerData PlayerData { get; set; }

        public Coroutine TimerRefresher { get; set; }
        public Coroutine AchievementPageShower { get; set; }
        public Coroutine MatchEndSummaryShower { get; set; }

        public ITransportConnection TransportConnection { get; set; }
        public ConfigManager Config
        {
            get
            {
                return Plugin.Instance.ConfigManager;
            }
        }

        public EMainPage MainPage { get; set; }

        // Play 

        public EPlayPage PlayPage { get; set; }
        public int SelectedGameID { get; set; }

        // Loadout
        public ELoadoutPage LoadoutPage { get; set; }
        public int LoadoutPageID { get; set; }
        public int LoadoutID { get; set; }

        public string LoadoutNameText { get; set; }

        public ELoadoutTab LoadoutTab { get; set; }
        public int LoadoutTabPageID { get; set; }
        public object SelectedItemID { get; set; }

        // Leaderboard
        public ELeaderboardPage LeaderboardPage { get; set; }
        public ELeaderboardTab LeaderboardTab { get; set; }
        public int LeaderboardPageID { get; set; }

        // Achievement
        public int AchievementMainPage { get; set; }
        public int AchievementSubPage { get; set; }
        public int SelectedAchievementID { get; set; }

        // Battlepass
        public (bool, int) SelectedBattlepassTierID { get; set; }

        public Dictionary<int, PageLoadout> LoadoutPages { get; set; }
        public Dictionary<int, PageGun> PistolPages { get; set; }
        public Dictionary<int, PageGun> SMGPages { get; set; }
        public Dictionary<int, PageGun> LMGPages { get; set; }
        public Dictionary<int, PageGun> ShotgunPages { get; set; }
        public Dictionary<int, PageGun> ARPages { get; set; }
        public Dictionary<int, PageGun> SniperPages { get; set; }
        public Dictionary<int, PageGun> CarbinePages { get; set; }
        public Dictionary<ushort, Dictionary<EAttachment, Dictionary<int, PageAttachment>>> AttachmentPages { get; set; }
        public Dictionary<int, PageGunCharm> GunCharmPages { get; set; }
        public Dictionary<ushort, Dictionary<int, PageGunSkin>> GunSkinPages { get; set; }
        public Dictionary<int, PageKnife> KnifePages { get; set; }
        public Dictionary<int, Dictionary<int, PagePerk>> PerkPages { get; set; }
        public Dictionary<int, PageGadget> TacticalPages { get; set; }
        public Dictionary<int, PageGadget> LethalPages { get; set; }
        public Dictionary<int, PageCard> CardPages { get; set; }
        public Dictionary<int, PageGlove> GlovePages { get; set; }
        public Dictionary<int, PageKillstreak> KillstreakPages { get; set; }
        public Dictionary<int, Dictionary<int, PageAchievement>> AchievementPages { get; set; }

        public UIHandler(UnturnedPlayer player)
        {
            Logging.Debug($"Creating UIHandler for {player.CSteamID}");
            SteamID = player.CSteamID;
            Player = player;
            TransportConnection = player.Player.channel.GetOwnerTransportConnection();
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

            TimerRefresher = Plugin.Instance.StartCoroutine(RefreshTimer());

            BuildPages();
            ResetUIValues();
            ShowUI();
        }

        public void Destroy()
        {
            if (TimerRefresher != null)
            {
                Plugin.Instance.StopCoroutine(TimerRefresher);
            }

            if (AchievementPageShower != null)
            {
                Plugin.Instance.StopCoroutine(AchievementPageShower);
            }

            if (MatchEndSummaryShower != null)
            {
                Plugin.Instance.StopCoroutine(MatchEndSummaryShower);
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
            BuildCarbinePages();
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
            BuildAchievementPages();
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
                if (index == 8)
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
                if (index == MaxItemsPerPage)
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
                if (index == MaxItemsPerPage)
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
                if (index == MaxItemsPerPage)
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
                if (index == MaxItemsPerPage)
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
                if (index == MaxItemsPerPage)
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
                if (index == MaxItemsPerPage)
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

        public void BuildCarbinePages()
        {
            var guns = PlayerLoadout.Guns.Values.Where(k => k.Gun.GunType == EGun.CARBINES).OrderBy(k => k.Gun.LevelRequirement).ToList();
            var gunItems = new Dictionary<int, LoadoutGun>();
            CarbinePages = new Dictionary<int, PageGun>();
            int index = 0;
            int page = 1;
            Logging.Debug($"Creating carbine pages for {Player.CharacterName}, found {guns.Count()} snipers");

            foreach (var gun in guns)
            {
                gunItems.Add(index, gun);
                if (index == MaxItemsPerPage)
                {
                    CarbinePages.Add(page, new PageGun(page, gunItems));
                    index = 0;
                    page++;
                    gunItems = new Dictionary<int, LoadoutGun>();
                    continue;
                }
                index++;
            }
            if (gunItems.Count != 0)
            {
                CarbinePages.Add(page, new PageGun(page, gunItems));
            }
            Logging.Debug($"Created {CarbinePages.Count} carbine pages for {Player.CharacterName}");
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
                    if (index == MaxSkinsCharmsPerPage)
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
                    if (index == MaxItemsPerPage)
                    {
                        AttachmentPages[gun.Gun.GunID][attachmentType].Add(page, new PageAttachment(page, attachments));
                        attachments = new Dictionary<int, LoadoutAttachment>();
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
                if (index == MaxSkinsCharmsPerPage)
                {
                    GunCharmPages.Add(page, new PageGunCharm(page, gunCharms));
                    gunCharms = new Dictionary<int, LoadoutGunCharm>();
                    index = 0;
                    page++;
                    continue;
                }
                index++;
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
                if (index == MaxSkinsCharmsPerPage)
                {
                    KnifePages.Add(page, new PageKnife(page, knives));
                    knives = new Dictionary<int, LoadoutKnife>();
                    index = 0;
                    page++;
                    continue;
                }
                index++;
            }
            if (knives.Count != 0)
            {
                KnifePages.Add(page, new PageKnife(page, knives));
            }
            Logging.Debug($"Created {KnifePages.Count} knife pages for {Player.CharacterName}");
        }

        public void BuildPerkPages()
        {
            PerkPages = new Dictionary<int, Dictionary<int, PagePerk>>();
            Logging.Debug($"Creating perk pages for {Player.CharacterName}, found {PlayerLoadout.Perks.Count} perks");
            for (int i = 1; i <= 3; i++)
            {
                Logging.Debug($"Creating perk pages for perk type {i} for {Player.CharacterName}");
                PerkPages.Add(i, new Dictionary<int, PagePerk>());
                int index = 0;
                int page = 1;
                var perks = new Dictionary<int, LoadoutPerk>();
                foreach (var perk in PlayerLoadout.Perks.Values.Where(k => k.Perk.PerkType == i).OrderBy(k => k.Perk.LevelRequirement))
                {
                    perks.Add(index, perk);
                    if (index == MaxItemsPerPage)
                    {
                        PerkPages[i].Add(page, new PagePerk(page, perks));
                        perks = new Dictionary<int, LoadoutPerk>();
                        index = 0;
                        page++;
                        continue;
                    }
                    index++;
                }
                if (perks.Count != 0)
                {
                    PerkPages[i].Add(page, new PagePerk(page, perks));
                }
                Logging.Debug($"Created {PerkPages[i].Count} for perk type {i} for {Player.CharacterName}");
            }
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
                if (index == MaxItemsPerPage)
                {
                    TacticalPages.Add(page, new PageGadget(page, gadgetItems));
                    gadgetItems = new Dictionary<int, LoadoutGadget>();
                    index = 0;
                    page++;
                    continue;
                }
                index++;
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
                if (index == MaxItemsPerPage)
                {
                    LethalPages.Add(page, new PageGadget(page, gadgetItems));
                    gadgetItems = new Dictionary<int, LoadoutGadget>();
                    index = 0;
                    page++;
                    continue;
                }
                index++;
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
                if (index == MaxSkinsCharmsPerPage)
                {
                    CardPages.Add(page, new PageCard(page, cards));
                    cards = new Dictionary<int, LoadoutCard>();
                    index = 0;
                    page++;
                    continue;
                }
                index++;
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
                if (index == MaxSkinsCharmsPerPage)
                {
                    GlovePages.Add(page, new PageGlove(page, gloves));
                    gloves = new Dictionary<int, LoadoutGlove>();
                    index = 0;
                    page++;
                    continue;
                }
                index++;
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
                if (index == MaxItemsPerPage)
                {
                    KillstreakPages.Add(page, new PageKillstreak(page, killstreaks));
                    killstreaks = new Dictionary<int, LoadoutKillstreak>();
                    index = 0;
                    page++;
                    continue;
                }
                index++;
            }
            if (killstreaks.Count != 0)
            {
                KillstreakPages.Add(page, new PageKillstreak(page, killstreaks));
            }
            Logging.Debug($"Created {KillstreakPages.Count} killstreak pages for {Player.CharacterName}");
        }

        public void BuildAchievementPages()
        {
            AchievementPages = new Dictionary<int, Dictionary<int, PageAchievement>>();
            Logging.Debug($"Creating achievement pages for {Player.CharacterName}, found {PlayerData.Achievements.Count} achievements");
            for (int i = 1; i <= 5; i++)
            {
                Logging.Debug($"Creating achievement pages for main page {i} for {Player.CharacterName}");
                AchievementPages.Add(i, new Dictionary<int, PageAchievement>());
                int index = 0;
                int page = 1;
                var achievements = new Dictionary<int, PlayerAchievement>();
                foreach (var achievement in PlayerData.Achievements.Where(k => k.Achievement.PageID == i).OrderByDescending(k => k.CurrentTier).ThenByDescending(k => k.TryGetNextTier(out AchievementTier nextTier) ? (k.Amount * 100 / nextTier.TargetAmount) : 100))
                {
                    achievements.Add(index, achievement);
                    if (index == 48)
                    {
                        AchievementPages[i].Add(page, new PageAchievement(page, achievements));
                        achievements = new Dictionary<int, PlayerAchievement>();
                        index = 0;
                        page++;
                        continue;
                    }
                    index++;
                }
                if (achievements.Count != 0)
                {
                    AchievementPages[i].Add(page, new PageAchievement(page, achievements));
                }
                Logging.Debug($"Created {AchievementPages[i].Count} for achievement type {i} for {Player.CharacterName}");
            }
        }

        // Main Page

        public void ShowUI(MatchEndSummary summary = null)
        {
            EffectManager.sendUIEffect(ID, Key, TransportConnection, true);
            Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);
            SetupMainMenu();
            if (summary != null)
            {
                MatchEndSummaryShower = Plugin.Instance.StartCoroutine(ShowMatchEndSummary(summary));
            }
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

        public void SetupMainMenu()
        {
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Player Icon", PlayerData.AvatarLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Player Name", PlayerData.SteamName);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Unbox BUTTON", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Store BUTTON", false);

            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Version TEXT", Plugin.Instance.Translate("Version").ToRich());
            ClearChat();
            ShowXP();
            ShowQuestCompletion();
            OnMusicChanged(PlayerData.Music);
        }

        public void ReloadMainMenu()
        {
            // Code to update the balance or sumthin
        }

        public void ShowXP()
        {
            var ui = Plugin.Instance.UIManager;
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER XP Num", Plugin.Instance.Translate("Level_Show", PlayerData.Level).ToRich());
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER XP Icon", Plugin.Instance.DBManager.Levels.TryGetValue(PlayerData.Level, out XPLevel level) ? level.IconLinkMedium : "");
            int spaces = 0;
            if (PlayerData.TryGetNeededXP(out int neededXP))
            {
                spaces = Math.Min(176, neededXP == 0 ? 0 : PlayerData.XP * 176 / neededXP);
            }
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER XP Bar Fill", spaces == 0 ? " " : new string(' ', spaces));
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

        public void ShowPlayPage()
        {
            MainPage = EMainPage.Play;
            ShowPlayPage(EPlayPage.Games);
        }

        public void ShowPlayPage(EPlayPage playPage)
        {
            SelectedGameID = 0;
            if (playPage == EPlayPage.Games)
            {
                ShowGames();
            }
            else if (playPage == EPlayPage.Servers)
            {
                ShowServers();
            }
        }

        public void SelectedPlayButton(int selected)
        {
            var games = Plugin.Instance.GameManager.Games;
            var servers = Plugin.Instance.DBManager.Servers;
            Logging.Debug($"{Player.CharacterName} selected play button with id {selected}");
            if (PlayPage == EPlayPage.Games)
            {
                if ((selected + 1) > games.Count)
                {
                    return;
                }
                ShowGame(games[selected]);
            }
            else if (PlayPage == EPlayPage.Servers)
            {
                if ((selected + 1) > servers.Count)
                {
                    return;
                }
                ShowServer(servers[selected]);
            }

            SelectedGameID = selected;
        }

        public void ClickedJoinButton()
        {
            if (PlayPage == EPlayPage.Games)
            {
                Plugin.Instance.GameManager.AddPlayerToGame(Player, SelectedGameID);
            }
            else if (PlayPage == EPlayPage.Servers)
            {
                var server = Plugin.Instance.DBManager.Servers[SelectedGameID];
                if (server.IsOnline)
                {
                    Player.Player.sendRelayToServer(server.IPNo, server.PortNo, "", false);
                }
            }
        }

        // Play Games Page

        public void ShowGames()
        {
            var games = Plugin.Instance.GameManager.Games;
            PlayPage = EPlayPage.Games;
            Logging.Debug($"Showing games to {Player.CharacterName}, with SelectedGameID {SelectedGameID}");

            for (int i = 0; i <= 13; i++)
            {
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Play BUTTON {i}", false);
            }

            var maxCount = Math.Min(14, games.Count);

            for (int index = 0; index < maxCount; index++)
            {
                var game = games[index];
                var gameMode = Config.Gamemode.FileData.GamemodeOptions.FirstOrDefault(k => k.GameType == game.GameMode);
                if (gameMode == null)
                {
                    return;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Play BUTTON {index}", true);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Play Map TEXT {index}", game.Location.LocationName);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Play Mode TEXT {index}", (game.IsHardcore ? $"<color={Config.Base.FileData.HardcoreColor}>Hardcore</color> " : "") + $"<color={gameMode.GamemodeColor}>{Plugin.Instance.Translate($"{game.GameMode}_Name_Full")}</color>");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Play Players TEXT {index}", $"{game.GetPlayerCount()}/{game.Location.GetMaxPlayers(game.GameMode)}");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Play Status TEXT {index}", game.GamePhase.ToFriendlyName());
            }

            SelectedPlayButton(SelectedGameID);
        }

        public void ShowGame(Game game)
        {
            var gameMode = Config.Gamemode.FileData.GamemodeOptions.FirstOrDefault(k => k.GameType == game.GameMode);
            if (gameMode == null)
            {
                return;
            }

            Logging.Debug($"Showing game to play with ID {SelectedGameID}");
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Play Server TEXT", "");
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Play Mode TEXT", (game.IsHardcore ? $"<color={Config.Base.FileData.HardcoreColor}>Hardcore</color> " : "") + $"<color={gameMode.GamemodeColor}>{Plugin.Instance.Translate($"{game.GameMode}_Name_Full")}</color>");
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Play IMAGE", game.Location.ImageLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Play Map TEXT", game.Location.LocationName);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Play Description TEXT", Plugin.Instance.Translate($"{game.GameMode}_Description_Full"));
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Play Join BUTTON", game.GamePhase != EGamePhase.Ending && game.GamePhase != EGamePhase.Ended);
        }

        public void UpdateGamePlayerCount(Game game)
        {
            int index = Plugin.Instance.GameManager.Games.IndexOf(game);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Play Players TEXT {index}", $"{game.GetPlayerCount()}/{game.Location.GetMaxPlayers(game.GameMode)}");
        }

        // Play Servers Page

        public void ShowServers()
        {
            PlayPage = EPlayPage.Servers;
            for (int i = 0; i <= 13; i++)
            {
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Play BUTTON {i}", false);
            }

            var servers = DB.Servers;
            var maxCount = Math.Min(14, servers.Count);

            for (int index = 0; index < maxCount; index++)
            {
                var server = servers[index];
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Play BUTTON {index}", true);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Play Server TEXT {index}", string.IsNullOrEmpty(server.Name) ? server.ServerName : server.Name);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Play Status TEXT {index}", server.IsOnline ? "<color=#36ff3c>Online</color>" : ((DateTime.UtcNow - server.LastOnline).TotalSeconds < 120 ? "<color=#f5fa73>Restarting</color>" : "<color=#ed2626>Offline</color>"));
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Play Players TEXT {index}", server.IsOnline ? $"{server.Players}/{server.MaxPlayers}" : "0/0");
            }

            SelectedPlayButton(SelectedGameID);
        }

        public void ShowServer(Server server)
        {
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Play Server TEXT", string.IsNullOrEmpty(server.Name) ? server.ServerName : server.Name);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Play Mode TEXT", " ");
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Play IMAGE", server.ServerBanner);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Play Map TEXT", " ");
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Play Description TEXT", server.ServerDesc);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Play Join BUTTON", server.IsOnline);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Play Ping TEXT", " ");
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Play IP TEXT", $"{server.FriendlyIP}:{server.Port}");
        }

        // Loadout Page

        public void ShowLoadouts()
        {
            MainPage = EMainPage.Loadout;

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

            if (!LoadoutPages.TryGetValue(LoadoutPageID - 1, out PageLoadout prevPage) && !LoadoutPages.TryGetValue(LoadoutPages.Keys.Max(), out prevPage))
            {
                ShowLoadouts();
                return;
            }

            ShowLoadoutPage(prevPage);
        }

        public void ReloadLoadoutPage()
        {
            if (!LoadoutPages.TryGetValue(LoadoutPageID, out PageLoadout page))
            {
                Logging.Debug($"Error finding current loadout page with page id {LoadoutPageID} for {Player.CharacterName}");
                return;
            }

            ShowLoadoutPage(page);
        }

        public void ShowLoadoutPage(PageLoadout page)
        {
            LoadoutPageID = page.PageID;

            for (int i = 0; i <= 9; i++)
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
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Couldnt find the current selected loadout");
                return;
            }

            ShowLoadout(currentLoadout);
        }

        public void SelectedLoadout(int selected)
        {
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

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Loadout Equip BUTTON", !loadout.IsActive);
            // Primary
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Loadout Primary IMAGE", loadout.PrimarySkin == null ? (loadout.Primary == null ? "" : loadout.Primary.Gun.IconLink) : loadout.PrimarySkin.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Loadout Primary TEXT", loadout.Primary == null ? "" : loadout.Primary.Gun.GunName);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Loadout Primary Level TEXT", loadout.Primary == null ? "" : loadout.Primary.Level.ToString());
            for (int i = 0; i <= 3; i++)
            {
                var attachmentType = (EAttachment)i;
                loadout.PrimaryAttachments.TryGetValue(attachmentType, out LoadoutAttachment attachment);
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Loadout Primary {attachmentType} IMAGE", attachment == null ? Utility.GetDefaultAttachmentImage(attachmentType.ToString()) : attachment.Attachment.IconLink);
            }
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Loadout Primary Charm IMAGE", loadout.PrimaryGunCharm == null ? Utility.GetDefaultAttachmentImage("charm") : loadout.PrimaryGunCharm.GunCharm.IconLink);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Loadout Primary Skin IMAGE", loadout.PrimarySkin == null ? Utility.GetDefaultAttachmentImage("skin") : loadout.PrimarySkin.PatternLink);

            // Secondary
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Loadout Secondary IMAGE", loadout.SecondarySkin == null ? (loadout.Secondary == null ? "" : loadout.Secondary.Gun.IconLink) : loadout.SecondarySkin.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Loadout Secondary TEXT", loadout.Secondary == null ? "" : loadout.Secondary.Gun.GunName);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Loadout Secondary Level TEXT", loadout.Secondary == null ? "" : loadout.Secondary.Level.ToString());
            for (int i = 0; i <= 3; i++)
            {
                var attachmentType = (EAttachment)i;
                loadout.SecondaryAttachments.TryGetValue(attachmentType, out LoadoutAttachment attachment);
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Loadout Secondary {attachmentType} IMAGE", attachment == null ? Utility.GetDefaultAttachmentImage(attachmentType.ToString()) : attachment.Attachment.IconLink);
            }
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Loadout Secondary Charm IMAGE", loadout.SecondaryGunCharm == null ? Utility.GetDefaultAttachmentImage("charm") : loadout.SecondaryGunCharm.GunCharm.IconLink);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Loadout Secondary Skin IMAGE", loadout.SecondarySkin == null ? Utility.GetDefaultAttachmentImage("skin") : loadout.SecondarySkin.PatternLink);

            // Knife
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Loadout Knife IMAGE", loadout.Knife == null ? "" : loadout.Knife.Knife.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Loadout Knife TEXT", loadout.Knife == null ? "" : loadout.Knife.Knife.KnifeName);

            // Tactical
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Loadout Tactical IMAGE", loadout.Tactical == null ? "" : loadout.Tactical.Gadget.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Loadout Tactical TEXT", loadout.Tactical == null ? "" : loadout.Tactical.Gadget.GadgetName);

            // Lethal
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Loadout Lethal IMAGE", loadout.Lethal == null ? "" : loadout.Lethal.Gadget.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Loadout Lethal TEXT", loadout.Lethal == null ? "" : loadout.Lethal.Gadget.GadgetName);

            // Perk
            for (int i = 1; i <= 3; i++)
            {
                loadout.Perks.TryGetValue(i, out LoadoutPerk perk);
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Loadout Perk IMAGE {i}", perk == null ? "" : loadout.Perks[i].Perk.IconLink);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Loadout Perk TEXT {i}", perk == null ? "" : loadout.Perks[i].Perk.PerkName);
            }

            // Killstreak
            for (int i = 0; i <= 2; i++)
            {
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Loadout Killstreak IMAGE {i}", loadout.Killstreaks.Count < (i + 1) ? "" : loadout.Killstreaks[i].Killstreak.IconLink);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Loadout Killstreak TEXT {i}", loadout.Killstreaks.Count < (i + 1) ? "" : loadout.Killstreaks[i].Killstreak.KillstreakName);
            }

            // Card
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Loadout Card IMAGE", loadout.Card == null ? "" : loadout.Card.Card.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Loadout Card TEXT", loadout.Card == null ? "" : loadout.Card.Card.CardName);

            // Glove
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Loadout Glove IMAGE", loadout.Glove == null ? "" : loadout.Glove.Glove.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Loadout Glove TEXT", loadout.Glove == null ? "" : loadout.Glove.Glove.GloveName);
        }

        public void EquipLoadout()
        {
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
            LoadoutNameText = name;
        }

        public void RenameLoadout()
        {
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

        // Midgame Loadout Selection

        public void ShowMidgameLoadouts()
        {
            MainPage = EMainPage.Loadout;

            Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);
            EffectManager.sendUIEffect(MidgameLoadoutSelectionID, MidgameLoadoutSelectionKey, TransportConnection, true);
            if (!LoadoutPages.TryGetValue(1, out PageLoadout firstPage))
            {
                Logging.Debug($"Error finding first page of loadouts midgame for {Player.CharacterName}");
                LoadoutPageID = 0;
                EffectManager.sendUIEffectVisibility(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Next BUTTON", false);
                EffectManager.sendUIEffectVisibility(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Previous Button", false);
                EffectManager.sendUIEffectText(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Page TEXT", "");
                return;
            }

            EffectManager.sendUIEffectVisibility(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Next BUTTON", true);
            EffectManager.sendUIEffectVisibility(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Previous Button", true);
            ShowMidgameLoadoutPage(firstPage);
            SelectedMidgameLoadout(0);
        }

        public void ForwardMidgameLoadoutPage()
        {
            if (LoadoutPageID == 0)
            {
                return;
            }

            if (!LoadoutPages.TryGetValue(LoadoutPageID + 1, out PageLoadout nextPage) && !LoadoutPages.TryGetValue(1, out nextPage))
            {
                ShowLoadouts();
                return;
            }

            ShowMidgameLoadoutPage(nextPage);
        }

        public void BackwardMidgameLoadoutPage()
        {
            if (LoadoutPageID == 0)
            {
                return;
            }

            if (!LoadoutPages.TryGetValue(LoadoutPageID - 1, out PageLoadout prevPage) && !LoadoutPages.TryGetValue(LoadoutPages.Keys.Max(), out prevPage))
            {
                ShowLoadouts();
                return;
            }

            ShowMidgameLoadoutPage(prevPage);
        }

        public void ShowMidgameLoadoutPage(PageLoadout page)
        {
            LoadoutPageID = page.PageID;

            for (int i = 0; i <= 9; i++)
            {
                if (!page.Loadouts.TryGetValue(i, out Loadout loadout))
                {
                    EffectManager.sendUIEffectVisibility(MidgameLoadoutSelectionKey, TransportConnection, true, $"SERVER Loadout BUTTON {i}", false);
                    continue;
                }

                EffectManager.sendUIEffectVisibility(MidgameLoadoutSelectionKey, TransportConnection, true, $"SERVER Loadout BUTTON {i}", true);
                EffectManager.sendUIEffectText(MidgameLoadoutSelectionKey, TransportConnection, true, $"SERVER Loadout TEXT {i}", loadout.LoadoutName);
                EffectManager.sendUIEffectVisibility(MidgameLoadoutSelectionKey, TransportConnection, true, $"SERVER Loadout Equipped {i}", loadout.IsActive);
            }
        }

        public void SelectedMidgameLoadout(int selected)
        {
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

            ShowMidgameLoadout(currentLoadout);
        }

        public void ShowMidgameLoadout(Loadout loadout)
        {
            LoadoutID = loadout.LoadoutID;

            EffectManager.sendUIEffectVisibility(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Equip BUTTON", !loadout.IsActive);
            // Primary
            EffectManager.sendUIEffectImageURL(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Primary IMAGE", loadout.PrimarySkin == null ? (loadout.Primary == null ? "" : loadout.Primary.Gun.IconLink) : loadout.PrimarySkin.IconLink);
            EffectManager.sendUIEffectText(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Primary TEXT", loadout.Primary == null ? "" : loadout.Primary.Gun.GunName);
            EffectManager.sendUIEffectText(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Primary Level TEXT", loadout.Primary == null ? "" : loadout.Primary.Level.ToString());
            for (int i = 0; i <= 3; i++)
            {
                var attachmentType = (EAttachment)i;
                loadout.PrimaryAttachments.TryGetValue(attachmentType, out LoadoutAttachment attachment);
                EffectManager.sendUIEffectImageURL(MidgameLoadoutSelectionKey, TransportConnection, true, $"SERVER Loadout Primary {attachmentType} IMAGE", attachment == null ? Utility.GetDefaultAttachmentImage(attachmentType.ToString()) : attachment.Attachment.IconLink);
            }
            EffectManager.sendUIEffectImageURL(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Primary Charm IMAGE", loadout.PrimaryGunCharm == null ? Utility.GetDefaultAttachmentImage("charm") : loadout.PrimaryGunCharm.GunCharm.IconLink);
            EffectManager.sendUIEffectImageURL(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Primary Skin IMAGE", loadout.PrimarySkin == null ? Utility.GetDefaultAttachmentImage("skin") : loadout.PrimarySkin.PatternLink);

            // Secondary
            EffectManager.sendUIEffectImageURL(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Secondary IMAGE", loadout.SecondarySkin == null ? (loadout.Secondary == null ? "" : loadout.Secondary.Gun.IconLink) : loadout.SecondarySkin.IconLink);
            EffectManager.sendUIEffectText(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Secondary TEXT", loadout.Secondary == null ? "" : loadout.Secondary.Gun.GunName);
            EffectManager.sendUIEffectText(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Secondary Level TEXT", loadout.Secondary == null ? "" : loadout.Secondary.Level.ToString());
            for (int i = 0; i <= 3; i++)
            {
                var attachmentType = (EAttachment)i;
                loadout.SecondaryAttachments.TryGetValue(attachmentType, out LoadoutAttachment attachment);
                EffectManager.sendUIEffectImageURL(MidgameLoadoutSelectionKey, TransportConnection, true, $"SERVER Loadout Secondary {attachmentType} IMAGE", attachment == null ? Utility.GetDefaultAttachmentImage(attachmentType.ToString()) : attachment.Attachment.IconLink);
            }
            EffectManager.sendUIEffectImageURL(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Secondary Charm IMAGE", loadout.SecondaryGunCharm == null ? Utility.GetDefaultAttachmentImage("charm") : loadout.SecondaryGunCharm.GunCharm.IconLink);
            EffectManager.sendUIEffectImageURL(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Secondary Skin IMAGE", loadout.SecondarySkin == null ? Utility.GetDefaultAttachmentImage("skin") : loadout.SecondarySkin.PatternLink);

            // Knife
            EffectManager.sendUIEffectImageURL(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Knife IMAGE", loadout.Knife == null ? "" : loadout.Knife.Knife.IconLink);
            EffectManager.sendUIEffectText(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Knife TEXT", loadout.Knife == null ? "" : loadout.Knife.Knife.KnifeName);

            // Tactical
            EffectManager.sendUIEffectImageURL(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Tactical IMAGE", loadout.Tactical == null ? "" : loadout.Tactical.Gadget.IconLink);
            EffectManager.sendUIEffectText(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Tactical TEXT", loadout.Tactical == null ? "" : loadout.Tactical.Gadget.GadgetName);

            // Lethal
            EffectManager.sendUIEffectImageURL(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Lethal IMAGE", loadout.Lethal == null ? "" : loadout.Lethal.Gadget.IconLink);
            EffectManager.sendUIEffectText(MidgameLoadoutSelectionKey, TransportConnection, true, "SERVER Loadout Lethal TEXT", loadout.Lethal == null ? "" : loadout.Lethal.Gadget.GadgetName);

            // Perk
            for (int i = 1; i <= 3; i++)
            {
                loadout.Perks.TryGetValue(i, out LoadoutPerk perk);
                EffectManager.sendUIEffectImageURL(MidgameLoadoutSelectionKey, TransportConnection, true, $"SERVER Loadout Perk IMAGE {i}", perk == null ? "" : loadout.Perks[i].Perk.IconLink);
                EffectManager.sendUIEffectText(MidgameLoadoutSelectionKey, TransportConnection, true, $"SERVER Loadout Perk TEXT {i}", perk == null ? "" : loadout.Perks[i].Perk.PerkName);
            }

            // Killstreak
            for (int i = 0; i <= 2; i++)
            {
                EffectManager.sendUIEffectImageURL(MidgameLoadoutSelectionKey, TransportConnection, true, $"SERVER Loadout Killstreak IMAGE {i}", loadout.Killstreaks.Count < (i + 1) ? "" : loadout.Killstreaks[i].Killstreak.IconLink);
                EffectManager.sendUIEffectText(MidgameLoadoutSelectionKey, TransportConnection, true, $"SERVER Loadout Killstreak TEXT {i}", loadout.Killstreaks.Count < (i + 1) ? "" : loadout.Killstreaks[i].Killstreak.KillstreakName);
            }

            // Card
            EffectManager.sendUIEffectImageURL(MidgameLoadoutSelectionKey, TransportConnection, true, $"SERVER Loadout Card IMAGE", loadout.Card == null ? "" : loadout.Card.Card.IconLink);
            EffectManager.sendUIEffectText(MidgameLoadoutSelectionKey, TransportConnection, true, $"SERVER Loadout Card TEXT", loadout.Card == null ? "" : loadout.Card.Card.CardName);

            // Glove
            EffectManager.sendUIEffectImageURL(MidgameLoadoutSelectionKey, TransportConnection, true, $"SERVER Loadout Glove IMAGE", loadout.Glove == null ? "" : loadout.Glove.Glove.IconLink);
            EffectManager.sendUIEffectText(MidgameLoadoutSelectionKey, TransportConnection, true, $"SERVER Loadout Glove TEXT", loadout.Glove == null ? "" : loadout.Glove.Glove.GloveName);
        }

        public void EquipMidgameLoadout()
        {
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
                    ClearMidgameLoadouts();
                    var gPlayer = Plugin.Instance.GameManager.GetGamePlayer(Player);
                    if (gPlayer != null)
                    {
                        gPlayer.IsPendingLoadoutChange = true;
                    }
                });
            });
        }

        public void ClearMidgameLoadouts()
        {
            var gPlayer = Plugin.Instance.GameManager.GetGamePlayer(Player);
            if (gPlayer != null)
            {
                gPlayer.HasMidgameLoadout = false;
            }
            EffectManager.askEffectClearByID(MidgameLoadoutSelectionID, TransportConnection);
            Player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
        }

        // Loadout Sub Page

        public void ShowLoadoutSubPage(ELoadoutPage page)
        {
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Type TEXT", page.ToFriendlyName());
            LoadoutPage = page;

            switch (LoadoutPage)
            {
                case ELoadoutPage.Primary:
                    ShowLoadoutTab(ELoadoutTab.ASSAULT_RIFLES);
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
            LoadoutTab = tab;

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", false);

            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", "");
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding current loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            for (int i = 0; i <= MaxItemsPerPage; i++)
            {
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", false);
            }

            for (int i = 0; i <= MaxSkinsCharmsPerPage; i++)
            {
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", false);
            }

            switch (LoadoutTab)
            {
                case ELoadoutTab.ALL:
                    {
                        switch (LoadoutPage)
                        {
                            case ELoadoutPage.PrimarySkin:
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

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", gunSkinPages.Count > 1);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", gunSkinPages.Count > 1);
                                    ShowGunSkinPage(firstPage);
                                    break;
                                }

                            case ELoadoutPage.SecondarySkin:
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

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", gunSkinPages.Count > 1);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", gunSkinPages.Count > 1);
                                    ShowGunSkinPage(firstPage);
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

                                    if (!attachmentPages.TryGetValue(1, out PageAttachment firstPage))
                                    {
                                        Logging.Debug($"Error finding first page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

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

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", GunCharmPages.Count > 1);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", GunCharmPages.Count > 1);

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

                                    ShowAttachmentPage(firstPage, gun);
                                    break;
                                }

                            case ELoadoutPage.Perk1:
                            case ELoadoutPage.Perk2:
                            case ELoadoutPage.Perk3:
                                {
                                    if (!int.TryParse(LoadoutPage.ToString().Replace("Perk", ""), out int perkType))
                                    {
                                        Logging.Debug($"Error getting perk type from {LoadoutPage}");
                                        return;
                                    }

                                    if (!PerkPages[perkType].TryGetValue(1, out PagePerk firstPage))
                                    {
                                        Logging.Debug($"Error getting first page for perks for {Player.CharacterName}");
                                        return;
                                    }

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

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", KnifePages.Count > 1);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", KnifePages.Count > 1);
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

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", GlovePages.Count > 1);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", GlovePages.Count > 1);
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

                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Next BUTTON", CardPages.Count > 1);
                                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Previous BUTTON", CardPages.Count > 1);
                                    ShowCardPage(firstPage);
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
                            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", "");
                            return;
                        }

                        ShowGunPage(firstPage);
                        break;
                    }

                case ELoadoutTab.SUBMACHINE_GUNS:
                    {
                        if (!SMGPages.TryGetValue(1, out PageGun firstPage))
                        {
                            Logging.Debug($"Error finding first page for smgs for {Player.CharacterName}");
                            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", "");
                            return;
                        }

                        ShowGunPage(firstPage);
                        break;
                    }

                case ELoadoutTab.SHOTGUNS:
                    {
                        if (!ShotgunPages.TryGetValue(1, out PageGun firstPage))
                        {
                            Logging.Debug($"Error finding first page for shotguns for {Player.CharacterName}");
                            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", "");
                            return;
                        }

                        ShowGunPage(firstPage);
                        break;
                    }

                case ELoadoutTab.LIGHT_MACHINE_GUNS:
                    {
                        if (!LMGPages.TryGetValue(1, out PageGun firstPage))
                        {
                            Logging.Debug($"Error finding first page for lmgs for {Player.CharacterName}");
                            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", "");
                            return;
                        }

                        ShowGunPage(firstPage);
                        break;
                    }

                case ELoadoutTab.ASSAULT_RIFLES:
                    {
                        if (!ARPages.TryGetValue(1, out PageGun firstPage))
                        {
                            Logging.Debug($"Error finding first page for ARs for {Player.CharacterName}");
                            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", "");
                            return;
                        }

                        ShowGunPage(firstPage);
                        break;
                    }

                case ELoadoutTab.SNIPER_RIFLES:
                    {
                        if (!SniperPages.TryGetValue(1, out PageGun firstPage))
                        {
                            Logging.Debug($"Error finding first page for snipers for {Player.CharacterName}");
                            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", "");
                            return;
                        }

                        ShowGunPage(firstPage);
                        break;
                    }

                case ELoadoutTab.CARBINES:
                    {
                        if (!CarbinePages.TryGetValue(1, out PageGun firstPage))
                        {
                            Logging.Debug($"Error finding first page for carbines for {Player.CharacterName}");
                            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Page TEXT", "");
                            return;
                        }

                        ShowGunPage(firstPage);
                        break;
                    }
            }
        }

        public void ForwardLoadoutTab()
        {
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
                            case ELoadoutPage.PrimarySkin:
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

                            case ELoadoutPage.SecondarySkin:
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

                                    if (!attachmentPages.TryGetValue(LoadoutTabPageID + 1, out PageAttachment nextPage) && !attachmentPages.TryGetValue(1, out nextPage))
                                    {
                                        Logging.Debug($"Error finding next page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

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

                                    if (!attachmentPages.TryGetValue(LoadoutTabPageID + 1, out PageAttachment nextPage) && !attachmentPages.TryGetValue(1, out nextPage))
                                    {
                                        Logging.Debug($"Error finding next page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowAttachmentPage(nextPage, gun);
                                    break;
                                }

                            case ELoadoutPage.Perk1:
                            case ELoadoutPage.Perk2:
                            case ELoadoutPage.Perk3:
                                {
                                    if (!int.TryParse(LoadoutPage.ToString().Replace("Perk", ""), out int perkType))
                                    {
                                        Logging.Debug($"Error getting perk type from {LoadoutPage}");
                                        return;
                                    }

                                    if (!PerkPages[perkType].TryGetValue(LoadoutTabPageID + 1, out PagePerk nextPage) && !PerkPages[perkType].TryGetValue(1, out nextPage))
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

                case ELoadoutTab.CARBINES:
                    {
                        if (!CarbinePages.TryGetValue(LoadoutTabPageID + 1, out PageGun nextPage) && !CarbinePages.TryGetValue(1, out nextPage))
                        {
                            Logging.Debug($"Error finding next page for carbines for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(nextPage);
                        break;
                    }
            }
        }

        public void BackwardLoadoutTab()
        {

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
                            case ELoadoutPage.PrimarySkin:
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

                            case ELoadoutPage.SecondarySkin:
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

                                    if (!attachmentPages.TryGetValue(LoadoutTabPageID - 1, out PageAttachment prevPage) && !attachmentPages.TryGetValue(attachmentPages.Keys.Max(), out prevPage))
                                    {
                                        Logging.Debug($"Error finding previous page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }


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

                                    if (!attachmentPages.TryGetValue(LoadoutTabPageID - 1, out PageAttachment prevPage) && !attachmentPages.TryGetValue(attachmentPages.Keys.Max(), out prevPage))
                                    {
                                        Logging.Debug($"Error finding previous page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }


                                    ShowAttachmentPage(prevPage, gun);
                                    break;
                                }

                            case ELoadoutPage.Perk1:
                            case ELoadoutPage.Perk2:
                            case ELoadoutPage.Perk3:
                                {
                                    if (!int.TryParse(LoadoutPage.ToString().Replace("Perk", ""), out int perkType))
                                    {
                                        Logging.Debug($"Error getting perk type from {LoadoutPage}");
                                        return;
                                    }

                                    if (!PerkPages[perkType].TryGetValue(LoadoutTabPageID - 1, out PagePerk prevPage) && !PerkPages[perkType].TryGetValue(PerkPages.Keys.Max(), out prevPage))
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

                case ELoadoutTab.CARBINES:
                    {
                        if (!CarbinePages.TryGetValue(LoadoutTabPageID - 1, out PageGun prevPage) && !CarbinePages.TryGetValue(CarbinePages.Keys.Max(), out prevPage))
                        {
                            Logging.Debug($"Error finding next page for carbines for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(prevPage);
                        break;
                    }
            }
        }

        public void ReloadLoadoutTab()
        {
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
                            case ELoadoutPage.PrimarySkin:
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

                            case ELoadoutPage.SecondarySkin:
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
                                        Logging.Debug($"Error finding current page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

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

                                    if (!attachmentPages.TryGetValue(LoadoutTabPageID, out PageAttachment page))
                                    {
                                        Logging.Debug($"Error finding current page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                                        return;
                                    }

                                    ShowAttachmentPage(page, gun);
                                    break;
                                }

                            case ELoadoutPage.Perk1:
                            case ELoadoutPage.Perk2:
                            case ELoadoutPage.Perk3:
                                {
                                    if (!int.TryParse(LoadoutPage.ToString().Replace("Perk", ""), out int perkType))
                                    {
                                        Logging.Debug($"Error getting perk type from {LoadoutPage}");
                                        return;
                                    }

                                    if (!PerkPages[perkType].TryGetValue(LoadoutTabPageID, out PagePerk page))
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

                case ELoadoutTab.CARBINES:
                    {
                        if (!CarbinePages.TryGetValue(LoadoutTabPageID, out PageGun page))
                        {
                            Logging.Debug($"Error finding current page for carbines for {Player.CharacterName}");
                            return;
                        }

                        ShowGunPage(page);
                        break;
                    }
            }
        }

        public void ShowGunPage(PageGun page)
        {
            LoadoutTabPageID = page.PageID;

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
                return;
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
            for (int i = 0; i <= MaxItemsPerPage; i++)
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
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Lock Overlay TEXT {i}", !gun.IsBought && gun.Gun.LevelRequirement > PlayerData.Level ? Plugin.Instance.Translate("Unlock_Level", gun.Gun.LevelRequirement) : "");
                SendRarity("SERVER Item", gun.Gun.GunRarity, i);
            }
        }

        public void ShowAttachmentPage(PageAttachment page, LoadoutGun gun)
        {
            LoadoutTabPageID = page.PageID;

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
                return;
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
            for (int i = 0; i <= MaxItemsPerPage; i++)
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
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Lock Overlay TEXT {i}", !attachment.IsBought && attachment.LevelRequirement > gun.Level ? Plugin.Instance.Translate("Unlock_Gun_Level", attachment.LevelRequirement) : "");
                SendRarity("SERVER Item", attachment.Attachment.AttachmentRarity, i);
            }
        }

        public void ShowGunCharmPage(PageGunCharm page)
        {
            LoadoutTabPageID = page.PageID;

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
                return;
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
            for (int i = 0; i <= MaxSkinsCharmsPerPage; i++)
            {
                if (!page.GunCharms.TryGetValue(i, out LoadoutGunCharm gunCharm))
                {
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", false);
                    continue;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Grid Equipped {i}", (LoadoutPage.ToString().StartsWith("AttachmentPrimary") && currentLoadout.PrimaryGunCharm == gunCharm) || (LoadoutPage.ToString().StartsWith("AttachmentSecondary") && currentLoadout.SecondaryGunCharm == gunCharm));
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Item Grid IMAGE {i}", gunCharm.GunCharm.IconLink);
                SendRarity("SERVER Item Grid", gunCharm.GunCharm.CharmRarity, i);
            }
        }

        public void ShowGunSkinPage(PageGunSkin page)
        {
            LoadoutTabPageID = page.PageID;

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
                return;
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
            for (int i = 0; i <= MaxSkinsCharmsPerPage; i++)
            {
                if (!page.GunSkins.TryGetValue(i, out GunSkin skin))
                {
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", false);
                    continue;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Grid Equipped {i}", (LoadoutPage == ELoadoutPage.PrimarySkin && currentLoadout.PrimarySkin == skin) || (LoadoutPage == ELoadoutPage.SecondarySkin && currentLoadout.SecondarySkin == skin));
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Item Grid IMAGE {i}", skin.IconLink);
                SendRarity("SERVER Item Grid", skin.SkinRarity, i);
            }
        }

        public void ShowKnifePage(PageKnife page)
        {
            LoadoutTabPageID = page.PageID;

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
                return;
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
            for (int i = 0; i <= MaxSkinsCharmsPerPage; i++)
            {
                if (!page.Knives.TryGetValue(i, out LoadoutKnife knife))
                {
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", false);
                    continue;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Grid Equipped {i}", currentLoadout.Knife == knife);
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Item Grid IMAGE {i}", knife.Knife.IconLink);
                SendRarity("SERVER Item Grid", knife.Knife.KnifeRarity, i);
            }
        }

        public void ShowPerkPage(PagePerk page)
        {
            LoadoutTabPageID = page.PageID;

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
                return;
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
            for (int i = 0; i <= MaxItemsPerPage; i++)
            {
                if (!page.Perks.TryGetValue(i, out LoadoutPerk perk))
                {
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", false);
                    continue;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item BUTTON {i}", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Equipped {i}", currentLoadout.Perks.ContainsValue(perk));
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Item IMAGE {i}", perk.Perk.IconLink);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item TEXT {i}", perk.Perk.PerkName);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Lock Overlay {i}", !perk.IsBought && perk.Perk.LevelRequirement > PlayerData.Level);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Lock Overlay TEXT {i}", !perk.IsBought && perk.Perk.LevelRequirement > PlayerData.Level ? Plugin.Instance.Translate("Unlock_Level", perk.Perk.LevelRequirement) : "");
                SendRarity("SERVER Item", perk.Perk.PerkRarity, i);
            }
        }

        public void ShowGadgetPage(PageGadget page)
        {
            LoadoutTabPageID = page.PageID;

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
                return;
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
            for (int i = 0; i <= MaxItemsPerPage; i++)
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
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Lock Overlay TEXT {i}", !gadget.IsBought && gadget.Gadget.LevelRequirement > PlayerData.Level ? Plugin.Instance.Translate("Unlock_Level", gadget.Gadget.LevelRequirement) : "");
                SendRarity("SERVER Item", gadget.Gadget.GadgetRarity, i);
            }
        }

        public void ShowCardPage(PageCard page)
        {
            LoadoutTabPageID = page.PageID;

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
                return;
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
            for (int i = 0; i <= MaxSkinsCharmsPerPage; i++)
            {
                if (!page.Cards.TryGetValue(i, out LoadoutCard card))
                {
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", false);
                    continue;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Grid Equipped {i}", currentLoadout.Card == card);
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Item Grid IMAGE {i}", card.Card.IconLink);
                SendRarity("SERVER Item Grid", card.Card.CardRarity, i);
            }
        }

        public void ShowGlovePage(PageGlove page)
        {
            LoadoutTabPageID = page.PageID;

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
                return;
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
            for (int i = 0; i <= MaxSkinsCharmsPerPage; i++)
            {
                if (!page.Gloves.TryGetValue(i, out LoadoutGlove glove))
                {
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", false);
                    continue;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", true);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Grid Equipped {i}", currentLoadout.Glove == glove);
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Item Grid IMAGE {i}", glove.Glove.IconLink);
                SendRarity("SERVER Item Grid", glove.Glove.GloveRarity, i);
            }
        }

        public void ShowKillstreakPage(PageKillstreak page)
        {
            LoadoutTabPageID = page.PageID;

            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout currentLoadout))
            {
                Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
                return;
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
            for (int i = 0; i <= MaxItemsPerPage; i++)
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
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Lock Overlay TEXT {i}", !killstreak.IsBought && killstreak.Killstreak.LevelRequirement > PlayerData.Level ? Plugin.Instance.Translate("Unlock_Level", killstreak.Killstreak.LevelRequirement) : "");
                SendRarity("SERVER Item", killstreak.Killstreak.KillstreakRarity, i);
            }
        }

        public void ReloadSelectedItem()
        {
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

                        if (!gun.Attachments.TryGetValue((ushort)SelectedItemID, out LoadoutAttachment attachment))
                        {
                            Logging.Debug($"Error finding attachment with attachment id {SelectedItemID} for gun {gun.Gun.GunName}");
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
                            Logging.Debug($"Error finding secondary that has been selected with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                            return;
                        }

                        if (!gun.Attachments.TryGetValue((ushort)SelectedItemID, out LoadoutAttachment attachment))
                        {
                            Logging.Debug($"Error finding attachment with attachment id {SelectedItemID} for gun {gun.Gun.GunName}");
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

                case ELoadoutPage.Perk1:
                case ELoadoutPage.Perk2:
                case ELoadoutPage.Perk3:
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
                        if (!PlayerLoadout.Gloves.TryGetValue((int)SelectedItemID, out LoadoutGlove glove))
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
                            case ELoadoutPage.PrimarySkin:
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

                            case ELoadoutPage.SecondarySkin:
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

                            case ELoadoutPage.Perk1:
                            case ELoadoutPage.Perk2:
                            case ELoadoutPage.Perk3:
                                {
                                    if (!int.TryParse(LoadoutPage.ToString().Replace("Perk", ""), out int perkType))
                                    {
                                        Logging.Debug($"Error getting perk type from {LoadoutPage}");
                                        return;
                                    }

                                    if (!PerkPages[perkType].TryGetValue(LoadoutTabPageID, out PagePerk page))
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

                case ELoadoutTab.CARBINES:
                    {
                        if (!CarbinePages.TryGetValue(LoadoutTabPageID, out PageGun page))
                        {
                            Logging.Debug($"Error finding carbine page {LoadoutTabPageID} for {Player.CharacterName}");
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
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Buy BUTTON", !gun.IsBought && PlayerData.Level >= gun.Gun.LevelRequirement);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Buy TEXT", !gun.IsBought && PlayerData.Level >= gun.Gun.LevelRequirement ? $"BUY ({gun.Gun.BuyPrice} CREDITS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Unlock BUTTON", !gun.IsBought && gun.Gun.LevelRequirement > PlayerData.Level);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Unlock TEXT", !gun.IsBought && gun.Gun.LevelRequirement > PlayerData.Level ? $"UNLOCK ({gun.Gun.GetCoins(PlayerData.Level)} COINS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Equip BUTTON", gun.IsBought && ((LoadoutPage == ELoadoutPage.Primary && loadout.Primary != gun) || (LoadoutPage == ELoadoutPage.Secondary && loadout.Secondary != gun)));
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", gun.IsBought && ((LoadoutPage == ELoadoutPage.Primary && loadout.Primary == gun) || (LoadoutPage == ELoadoutPage.Secondary && loadout.Secondary == gun)));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Description TEXT", gun.Gun.GunDesc);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Item IMAGE", gun.Gun.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item TEXT", gun.Gun.GunName);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Level TEXT", gun.Level.ToString());
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item ProsCons", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Description TEXT", true);
            gun.TryGetNeededXP(out int neededXP);
            var spaces = neededXP != 0 ? (gun.XP * 188 / neededXP) : 0;
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item XP Bar Fill", spaces == 0 ? " " : new string(' ', spaces));
            SendRarityName(gun.Gun.GunRarity);
        }

        public void ShowAttachment(LoadoutAttachment attachment, LoadoutGun gun)
        {
            SelectedItemID = attachment.Attachment.AttachmentID;
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Buy BUTTON", !attachment.IsBought && gun.Level >= attachment.LevelRequirement);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Buy TEXT", !attachment.IsBought && gun.Level >= attachment.LevelRequirement ? $"BUY ({attachment.Attachment.BuyPrice} CREDITS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Unlock BUTTON", !attachment.IsBought && attachment.LevelRequirement > gun.Level);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Unlock TEXT", !attachment.IsBought && attachment.LevelRequirement > gun.Level ? $"UNLOCK ({attachment.GetCoins(gun.Level)} COINS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Equip BUTTON", attachment.IsBought && ((LoadoutPage.ToString().StartsWith("AttachmentPrimary") && !loadout.PrimaryAttachments.ContainsValue(attachment)) || (LoadoutPage.ToString().StartsWith("AttachmentSecondary") && !loadout.SecondaryAttachments.ContainsValue(attachment))));
            if (attachment.Attachment.AttachmentType != EAttachment.Magazine)
            {
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", attachment.IsBought && ((LoadoutPage.ToString().StartsWith("AttachmentPrimary") && loadout.PrimaryAttachments.ContainsValue(attachment)) || (LoadoutPage.ToString().StartsWith("AttachmentSecondary") && loadout.SecondaryAttachments.ContainsValue(attachment))));
            }
            else
            {
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", false);
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Description TEXT", false);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Item IMAGE", attachment.Attachment.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item TEXT", attachment.Attachment.AttachmentName);
            SendRarityName(attachment.Attachment.AttachmentRarity);

            Logging.Debug($"Pros: {attachment.Attachment.AttachmentPros.Count}, Cons: {attachment.Attachment.AttachmentCons.Count}");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item ProsCons", !(attachment.Attachment.AttachmentPros.Count == 0 && attachment.Attachment.AttachmentCons.Count == 0));
            Logging.Debug($"Set visiblity to {attachment.Attachment.AttachmentPros.Count == 0 && attachment.Attachment.AttachmentCons.Count == 0}");
            for (int i = 0; i <= 2; i++)
            {
                Logging.Debug($"i: {i}");
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Pro {i}", attachment.Attachment.AttachmentPros.Count > i);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Pro TEXT {i}", attachment.Attachment.AttachmentPros.Count > i ? attachment.Attachment.AttachmentPros[i].Trim() : "");

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Item Con {i}", attachment.Attachment.AttachmentCons.Count > i);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Item Con TEXT {i}", attachment.Attachment.AttachmentCons.Count > i ? attachment.Attachment.AttachmentCons[i].Trim() : "");
            }
        }

        public void ShowGunCharm(LoadoutGunCharm gunCharm)
        {
            SelectedItemID = gunCharm.GunCharm.CharmID;
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Buy BUTTON", !gunCharm.IsBought && PlayerData.Level >= gunCharm.GunCharm.LevelRequirement);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Buy TEXT", !gunCharm.IsBought && PlayerData.Level >= gunCharm.GunCharm.LevelRequirement ? $"BUY ({gunCharm.GunCharm.BuyPrice} CREDITS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Unlock BUTTON", !gunCharm.IsBought && gunCharm.GunCharm.LevelRequirement > PlayerData.Level);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Unlock TEXT", !gunCharm.IsBought && gunCharm.GunCharm.LevelRequirement > PlayerData.Level ? $"UNLOCK ({gunCharm.GunCharm.GetCoins(PlayerData.Level)} COINS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Equip BUTTON", gunCharm.IsBought && ((LoadoutPage.ToString().StartsWith("AttachmentPrimary") && loadout.PrimaryGunCharm != gunCharm) || (LoadoutPage.ToString().StartsWith("AttachmentSecondary") && loadout.SecondaryGunCharm != gunCharm)));
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", gunCharm.IsBought && ((LoadoutPage.ToString().StartsWith("AttachmentPrimary") && loadout.PrimaryGunCharm == gunCharm) || (LoadoutPage.ToString().StartsWith("AttachmentSecondary") && loadout.SecondaryGunCharm == gunCharm)));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Description TEXT", gunCharm.GunCharm.CharmDesc);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Item IMAGE", gunCharm.GunCharm.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item TEXT", gunCharm.GunCharm.CharmName);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item ProsCons", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Description TEXT", true);
            SendRarityName(gunCharm.GunCharm.CharmRarity);
        }

        public void ShowGunSkin(GunSkin skin)
        {
            SelectedItemID = skin.ID;
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Buy BUTTON", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Unlock BUTTON", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Equip BUTTON", (LoadoutPage == ELoadoutPage.PrimarySkin && loadout.PrimarySkin != skin) || (LoadoutPage == ELoadoutPage.SecondarySkin && loadout.SecondarySkin != skin));
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", (LoadoutPage == ELoadoutPage.PrimarySkin && loadout.PrimarySkin == skin) || (LoadoutPage == ELoadoutPage.SecondarySkin && loadout.SecondarySkin == skin));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Description TEXT", skin.SkinDesc);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Item IMAGE", skin.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item TEXT", skin.SkinName);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item ProsCons", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Description TEXT", true);
            SendRarityName(skin.SkinRarity);
        }

        public void ShowKnife(LoadoutKnife knife)
        {
            SelectedItemID = knife.Knife.KnifeID;
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Buy BUTTON", !knife.IsBought && PlayerData.Level >= knife.Knife.LevelRequirement);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Buy TEXT", !knife.IsBought && PlayerData.Level >= knife.Knife.LevelRequirement ? $"BUY ({knife.Knife.BuyPrice} CREDITS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Unlock BUTTON", !knife.IsBought && knife.Knife.LevelRequirement > PlayerData.Level);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Unlock TEXT", !knife.IsBought && knife.Knife.LevelRequirement > PlayerData.Level ? $"UNLOCK ({knife.Knife.GetCoins(PlayerData.Level)} COINS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Equip BUTTON", knife.IsBought && loadout.Knife != knife);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", false);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Description TEXT", knife.Knife.KnifeDesc);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Item IMAGE", knife.Knife.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item TEXT", knife.Knife.KnifeName);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item ProsCons", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Description TEXT", true);
            SendRarityName(knife.Knife.KnifeRarity);
        }

        public void ShowPerk(LoadoutPerk perk)
        {
            SelectedItemID = perk.Perk.PerkID;
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Buy BUTTON", !perk.IsBought && PlayerData.Level >= perk.Perk.LevelRequirement);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Buy TEXT", !perk.IsBought && PlayerData.Level >= perk.Perk.LevelRequirement ? $"BUY ({perk.Perk.BuyPrice} CREDITS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Unlock BUTTON", !perk.IsBought && perk.Perk.LevelRequirement > PlayerData.Level);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Unlock TEXT", !perk.IsBought && perk.Perk.LevelRequirement > PlayerData.Level ? $"UNLOCK ({perk.Perk.GetCoins(PlayerData.Level)} COINS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Equip BUTTON", perk.IsBought && !loadout.Perks.ContainsValue(perk));
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", perk.IsBought && loadout.Perks.ContainsValue(perk));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Description TEXT", perk.Perk.PerkDesc);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Item IMAGE", perk.Perk.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item TEXT", perk.Perk.PerkName);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item ProsCons", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Description TEXT", true);
            SendRarityName(perk.Perk.PerkRarity);
        }

        public void ShowGadget(LoadoutGadget gadget)
        {
            SelectedItemID = gadget.Gadget.GadgetID;
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Buy BUTTON", !gadget.IsBought && PlayerData.Level >= gadget.Gadget.LevelRequirement);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Buy TEXT", !gadget.IsBought && PlayerData.Level >= gadget.Gadget.LevelRequirement ? $"BUY ({gadget.Gadget.BuyPrice} CREDITS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Unlock BUTTON", !gadget.IsBought && gadget.Gadget.LevelRequirement > PlayerData.Level);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Unlock TEXT", !gadget.IsBought && gadget.Gadget.LevelRequirement > PlayerData.Level ? $"UNLOCK ({gadget.Gadget.GetCoins(PlayerData.Level)} COINS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Equip BUTTON", gadget.IsBought && ((LoadoutPage == ELoadoutPage.Tactical && loadout.Tactical != gadget) || (LoadoutPage == ELoadoutPage.Lethal && loadout.Lethal != gadget)));
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", gadget.IsBought && ((LoadoutPage == ELoadoutPage.Tactical && loadout.Tactical == gadget) || (LoadoutPage == ELoadoutPage.Lethal && loadout.Lethal == gadget)));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Description TEXT", gadget.Gadget.GadgetDesc);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Item IMAGE", gadget.Gadget.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item TEXT", gadget.Gadget.GadgetName);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item ProsCons", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Description TEXT", true);
            SendRarityName(gadget.Gadget.GadgetRarity);
        }

        public void ShowCard(LoadoutCard card)
        {
            SelectedItemID = card.Card.CardID;
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Buy BUTTON", !card.IsBought && PlayerData.Level >= card.Card.LevelRequirement);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Buy TEXT", !card.IsBought && PlayerData.Level >= card.Card.LevelRequirement ? $"BUY ({card.Card.BuyPrice} CREDITS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Unlock BUTTON", !card.IsBought && card.Card.LevelRequirement > PlayerData.Level);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Unlock TEXT", !card.IsBought && card.Card.LevelRequirement > PlayerData.Level ? $"UNLOCK ({card.Card.GetCoins(PlayerData.Level)} COINS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Equip BUTTON", card.IsBought && loadout.Card != card);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", card.IsBought && loadout.Card == card);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Description TEXT", card.Card.CardDesc);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Item Card IMAGE", card.Card.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item TEXT", card.Card.CardName);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item ProsCons", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Description TEXT", true);
            SendRarityName(card.Card.CardRarity);
        }

        public void ShowGlove(LoadoutGlove glove)
        {
            SelectedItemID = glove.Glove.GloveID;
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Buy BUTTON", !glove.IsBought && PlayerData.Level >= glove.Glove.LevelRequirement);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Buy TEXT", !glove.IsBought && PlayerData.Level >= glove.Glove.LevelRequirement ? $"BUY ({glove.Glove.BuyPrice} CREDITS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Unlock BUTTON", !glove.IsBought && glove.Glove.LevelRequirement > PlayerData.Level);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Unlock TEXT", !glove.IsBought && glove.Glove.LevelRequirement > PlayerData.Level ? $"UNLOCK ({glove.Glove.GetCoins(PlayerData.Level)} COINS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Equip BUTTON", glove.IsBought && loadout.Glove != glove);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", glove.IsBought && loadout.Glove == glove);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Description TEXT", glove.Glove.GloveDesc);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Item IMAGE", glove.Glove.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item TEXT", glove.Glove.GloveName);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item ProsCons", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Description TEXT", true);
            SendRarityName(glove.Glove.GloveRarity);
        }

        public void ShowKillstreak(LoadoutKillstreak killstreak)
        {
            SelectedItemID = killstreak.Killstreak.KillstreakID;
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Buy BUTTON", !killstreak.IsBought && PlayerData.Level >= killstreak.Killstreak.LevelRequirement);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Buy TEXT", !killstreak.IsBought && PlayerData.Level >= killstreak.Killstreak.LevelRequirement ? $"BUY ({killstreak.Killstreak.BuyPrice} CREDITS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Unlock BUTTON", !killstreak.IsBought && killstreak.Killstreak.LevelRequirement > PlayerData.Level);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Unlock TEXT", !killstreak.IsBought && killstreak.Killstreak.LevelRequirement > PlayerData.Level ? $"UNLOCK ({killstreak.Killstreak.GetCoins(PlayerData.Level)} COINS)" : "");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Equip BUTTON", killstreak.IsBought && !loadout.Killstreaks.Contains(killstreak));
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Dequip BUTTON", killstreak.IsBought && loadout.Killstreaks.Contains(killstreak));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Description TEXT", killstreak.Killstreak.KillstreakDesc);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Item IMAGE", killstreak.Killstreak.IconLink);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item TEXT", killstreak.Killstreak.KillstreakName);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item ProsCons", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Item Description TEXT", true);
            SendRarityName(killstreak.Killstreak.KillstreakRarity);
        }

        public void SendRarity(string objectName, string rarity, int selected)
        {
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"{objectName} {rarity} {selected}", true);

            /*var rarities = new string[] { "COMMON", "UNCOMMON", "RARE", "EPIC", "LEGENDARY", "MYTHICAL", "YELLOW", "ORANGE", "CYAN", "GREEN" };
            foreach (var r in rarities)
            {
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"{objectName} {r} {selected}", rarity == r);
            }*/
        }

        public void SendRarityName(string rarity)
        {
            var rarities = new List<string> { "COMMON", "UNCOMMON", "RARE", "EPIC", "LEGENDARY", "MYTHICAL" };
            var colors = new List<string> { "#FFFFFF", "#1F871F", "#4B64FA", "#964BFA", "#C832FA", "#FA3219" };

            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Item Rarity TEXT", rarities.Contains(rarity) ? $"<color={colors[rarities.IndexOf(rarity)]}>{rarity}</color>" : "");
        }

        public void BuySelectedItem()
        {
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
                            if (PlayerData.Credits >= gun.Gun.BuyPrice && !gun.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, gun.Gun.BuyPrice);
                                await DB.UpdatePlayerGunBoughtAsync(Player.CSteamID, gun.Gun.GunID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
                                    ReloadLoadoutTab();
                                    ReloadSelectedItem();
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
                            if (PlayerData.Credits >= attachment.Attachment.BuyPrice && !attachment.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, attachment.Attachment.BuyPrice);
                                await DB.UpdatePlayerGunAttachmentBoughtAsync(Player.CSteamID, gun.Gun.GunID, attachment.Attachment.AttachmentID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
                                    ReloadLoadoutTab();
                                    ReloadSelectedItem();
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
                            if (PlayerData.Credits >= gun.Gun.BuyPrice && !gun.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, gun.Gun.BuyPrice);
                                await DB.UpdatePlayerGunBoughtAsync(Player.CSteamID, gun.Gun.GunID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
                                    ReloadLoadoutTab();
                                    ReloadSelectedItem();
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
                            if (PlayerData.Credits >= attachment.Attachment.BuyPrice && !attachment.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, attachment.Attachment.BuyPrice);
                                await DB.UpdatePlayerGunAttachmentBoughtAsync(Player.CSteamID, gun.Gun.GunID, attachment.Attachment.AttachmentID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
                                    ReloadLoadoutTab();
                                    ReloadSelectedItem();
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
                            if (PlayerData.Credits >= gunCharm.GunCharm.BuyPrice && !gunCharm.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, gunCharm.GunCharm.BuyPrice);
                                await DB.UpdatePlayerGunCharmBoughtAsync(Player.CSteamID, gunCharm.GunCharm.CharmID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
                                    ReloadLoadoutTab();
                                    ReloadSelectedItem();
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
                            if (PlayerData.Credits >= knife.Knife.BuyPrice && !knife.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, knife.Knife.BuyPrice);
                                await DB.UpdatePlayerKnifeBoughtAsync(Player.CSteamID, knife.Knife.KnifeID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
                                    ReloadLoadoutTab();
                                    ReloadSelectedItem();
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
                            if (PlayerData.Credits >= gadget.Gadget.BuyPrice && !gadget.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, gadget.Gadget.BuyPrice);
                                await DB.UpdatePlayerGadgetBoughtAsync(Player.CSteamID, gadget.Gadget.GadgetID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
                                    ReloadLoadoutTab();
                                    ReloadSelectedItem();
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
                            if (PlayerData.Credits >= gadget.Gadget.BuyPrice && !gadget.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, gadget.Gadget.BuyPrice);
                                await DB.UpdatePlayerGadgetBoughtAsync(Player.CSteamID, gadget.Gadget.GadgetID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
                                    ReloadLoadoutTab();
                                    ReloadSelectedItem();
                                });
                            }
                        });
                        break;
                    }

                case ELoadoutPage.Perk1:
                case ELoadoutPage.Perk2:
                case ELoadoutPage.Perk3:
                    {
                        if (!PlayerLoadout.Perks.TryGetValue((int)SelectedItemID, out LoadoutPerk perk))
                        {
                            Logging.Debug($"Error finding perk with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (PlayerData.Credits >= perk.Perk.BuyPrice && !perk.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, perk.Perk.BuyPrice);
                                await DB.UpdatePlayerPerkBoughtAsync(Player.CSteamID, perk.Perk.PerkID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
                                    ReloadLoadoutTab();
                                    ReloadSelectedItem();
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
                            if (PlayerData.Credits >= killstreak.Killstreak.BuyPrice && !killstreak.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, killstreak.Killstreak.BuyPrice);
                                await DB.UpdatePlayerKillstreakBoughtAsync(Player.CSteamID, killstreak.Killstreak.KillstreakID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
                                    ReloadLoadoutTab();
                                    ReloadSelectedItem();
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
                            if (PlayerData.Credits >= card.Card.BuyPrice && !card.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, card.Card.BuyPrice);
                                await DB.UpdatePlayerCardBoughtAsync(Player.CSteamID, card.Card.CardID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
                                    ReloadLoadoutTab();
                                    ReloadSelectedItem();
                                });
                            }
                        });
                        break;
                    }

                case ELoadoutPage.Glove:
                    {
                        if (!PlayerLoadout.Gloves.TryGetValue((int)SelectedItemID, out LoadoutGlove glove))
                        {
                            Logging.Debug($"Error finding glove with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            if (PlayerData.Credits >= glove.Glove.BuyPrice && !glove.IsBought)
                            {
                                await DB.DecreasePlayerCreditsAsync(Player.CSteamID, glove.Glove.BuyPrice);
                                await DB.UpdatePlayerGloveBoughtAsync(Player.CSteamID, glove.Glove.GloveID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
                                    ReloadLoadoutTab();
                                    ReloadSelectedItem();
                                });
                            }
                        });
                        break;
                    }
            }
        }

        public void UnlockSelectedItem()
        {
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
                            var cost = gun.Gun.GetCoins(PlayerData.Level);
                            if (PlayerData.Coins >= cost && !gun.IsBought && gun.Gun.LevelRequirement > PlayerData.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, cost);
                                await DB.UpdatePlayerGunBoughtAsync(Player.CSteamID, gun.Gun.GunID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
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
                            var cost = attachment.GetCoins(gun.Level);
                            if (PlayerData.Coins >= cost && !attachment.IsBought && attachment.LevelRequirement > gun.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, cost);
                                await DB.UpdatePlayerGunAttachmentBoughtAsync(Player.CSteamID, gun.Gun.GunID, attachment.Attachment.AttachmentID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
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
                            var cost = gun.Gun.GetCoins(PlayerData.Level);
                            if (PlayerData.Coins >= cost && !gun.IsBought && gun.Gun.LevelRequirement > PlayerData.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, cost);
                                await DB.UpdatePlayerGunBoughtAsync(Player.CSteamID, gun.Gun.GunID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
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
                            var cost = attachment.GetCoins(gun.Level);
                            if (PlayerData.Coins >= cost && !attachment.IsBought && attachment.LevelRequirement > gun.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, cost);
                                await DB.UpdatePlayerGunAttachmentBoughtAsync(Player.CSteamID, gun.Gun.GunID, attachment.Attachment.AttachmentID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
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
                            var cost = gunCharm.GunCharm.GetCoins(PlayerData.Level);
                            if (PlayerData.Coins >= cost && !gunCharm.IsBought && gunCharm.GunCharm.LevelRequirement > PlayerData.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, cost);
                                await DB.UpdatePlayerGunCharmBoughtAsync(Player.CSteamID, gunCharm.GunCharm.CharmID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
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
                            var cost = knife.Knife.GetCoins(PlayerData.Level);
                            if (PlayerData.Coins >= cost && !knife.IsBought && knife.Knife.LevelRequirement > PlayerData.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, cost);
                                await DB.UpdatePlayerKnifeBoughtAsync(Player.CSteamID, knife.Knife.KnifeID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
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
                            var cost = gadget.Gadget.GetCoins(PlayerData.Level);
                            if (PlayerData.Coins >= cost && !gadget.IsBought && gadget.Gadget.LevelRequirement > PlayerData.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, cost);
                                await DB.UpdatePlayerGadgetBoughtAsync(Player.CSteamID, gadget.Gadget.GadgetID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
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
                            var cost = gadget.Gadget.GetCoins(PlayerData.Level);
                            if (PlayerData.Coins >= cost && !gadget.IsBought && gadget.Gadget.LevelRequirement > PlayerData.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, cost);
                                await DB.UpdatePlayerGadgetBoughtAsync(Player.CSteamID, gadget.Gadget.GadgetID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
                                    ReloadSelectedItem();
                                    ReloadLoadoutTab();
                                });
                            }
                        });
                        break;
                    }

                case ELoadoutPage.Perk1:
                case ELoadoutPage.Perk2:
                case ELoadoutPage.Perk3:
                    {
                        if (!PlayerLoadout.Perks.TryGetValue((int)SelectedItemID, out LoadoutPerk perk))
                        {
                            Logging.Debug($"Error finding perk with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            var cost = perk.Perk.GetCoins(PlayerData.Level);
                            if (PlayerData.Coins >= cost && !perk.IsBought && perk.Perk.LevelRequirement > PlayerData.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, perk.Perk.GetCoins(PlayerData.Level));
                                await DB.UpdatePlayerPerkBoughtAsync(Player.CSteamID, perk.Perk.PerkID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
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
                            var cost = killstreak.Killstreak.GetCoins(PlayerData.Level);
                            if (PlayerData.Coins >= cost && !killstreak.IsBought && killstreak.Killstreak.LevelRequirement > PlayerData.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, cost);
                                await DB.UpdatePlayerKillstreakBoughtAsync(Player.CSteamID, killstreak.Killstreak.KillstreakID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
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
                            var cost = card.Card.GetCoins(PlayerData.Level);
                            if (PlayerData.Coins >= cost && !card.IsBought && card.Card.LevelRequirement > PlayerData.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, cost);
                                await DB.UpdatePlayerCardBoughtAsync(Player.CSteamID, card.Card.CardID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
                                    ReloadSelectedItem();
                                    ReloadLoadoutTab();
                                });
                            }
                        });
                        break;
                    }

                case ELoadoutPage.Glove:
                    {
                        if (!PlayerLoadout.Gloves.TryGetValue((int)SelectedItemID, out LoadoutGlove glove))
                        {
                            Logging.Debug($"Error finding glove with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        ThreadPool.QueueUserWorkItem(async (o) =>
                        {
                            var cost = glove.Glove.GetCoins(PlayerData.Level);
                            if (PlayerData.Coins >= cost && !glove.IsBought && glove.Glove.LevelRequirement > PlayerData.Level)
                            {
                                await DB.DecreasePlayerCoinsAsync(Player.CSteamID, cost);
                                await DB.UpdatePlayerGloveBoughtAsync(Player.CSteamID, glove.Glove.GloveID, true);
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    EquipSelectedItem();
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
            var loadoutManager = Plugin.Instance.LoadoutManager;
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding selected loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            switch (LoadoutPage)
            {
                case ELoadoutPage.PrimarySkin:
                    {
                        if (!PlayerLoadout.GunSkinsSearchByID.TryGetValue((int)SelectedItemID, out GunSkin skin))
                        {
                            Logging.Debug($"Error finding gun skin with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        loadoutManager.EquipGunSkin(Player, LoadoutID, skin.ID, true);
                        break;
                    }

                case ELoadoutPage.SecondarySkin:
                    {
                        if (!PlayerLoadout.GunSkinsSearchByID.TryGetValue((int)SelectedItemID, out GunSkin skin))
                        {
                            Logging.Debug($"Error finding gun skin with id {SelectedItemID} for {Player.CharacterName}");
                            return;
                        }

                        loadoutManager.EquipGunSkin(Player, LoadoutID, skin.ID, false);
                        break;
                    }

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

                case ELoadoutPage.Perk1:
                case ELoadoutPage.Perk2:
                case ELoadoutPage.Perk3:
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
                        if (!PlayerLoadout.Gloves.TryGetValue((int)SelectedItemID, out LoadoutGlove glove))
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
            var loadoutManager = Plugin.Instance.LoadoutManager;
            if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out Loadout loadout))
            {
                Logging.Debug($"Error finding selected loadout with id {LoadoutID} for {Player.CharacterName}");
                return;
            }

            switch (LoadoutPage)
            {
                case ELoadoutPage.PrimarySkin:
                    {
                        loadoutManager.DequipGunSkin(Player, LoadoutID, true);
                        break;
                    }

                case ELoadoutPage.SecondarySkin:
                    {
                        loadoutManager.DequipGunSkin(Player, LoadoutID, false);
                        break;
                    }

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

                case ELoadoutPage.Perk1:
                case ELoadoutPage.Perk2:
                case ELoadoutPage.Perk3:
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

        // Leaderboard

        public void ShowLeaderboards()
        {
            MainPage = EMainPage.Leaderboard;
            SelectLeaderboardPage(ELeaderboardPage.Daily);
        }

        public void SelectLeaderboardPage(ELeaderboardPage page)
        {
            LeaderboardPage = page;
            SelectLeaderboardTab(ELeaderboardTab.Kill);
        }

        public void SelectLeaderboardTab(ELeaderboardTab tab)
        {
            LeaderboardTab = tab;
            ShowLeaderboard();
        }

        public void ShowLeaderboard()
        {
            var data = GetLeaderboardData();
            var dataLookup = GetLeaderboardDataLookup();

            for (int i = 0; i <= 10; i++)
            {
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Leaderboards BUTTON {i}", false);
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Leaderboards Level BUTTON", LeaderboardPage == ELeaderboardPage.All);
            if (data.Count == 0)
            {
                return;
            }

            if (dataLookup.TryGetValue(SteamID, out LeaderboardData playerData))
            {
                var kills = (decimal)(playerData.Kills + playerData.HeadshotKills);
                var deaths = (decimal)playerData.Deaths;

                var ratio = playerData.Deaths == 0 ? String.Format("{0:n}", kills) : String.Format("{0:n}", Math.Round(kills / deaths, 2));

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Leaderboards BUTTON 10", true);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Leaderboards Rank TEXT 10", (data.IndexOf(playerData) + 1).ToString());
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Leaderboards Level TEXT 10", playerData.Level.ToString());
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Leaderboards Level IMAGE 10", Plugin.Instance.DBManager.Levels.TryGetValue(playerData.Level, out XPLevel level) ? level.IconLinkMedium : "");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Leaderboards Name TEXT 10", playerData.SteamName);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Leaderboards Kills TEXT 10", (playerData.Kills + playerData.HeadshotKills).ToString());
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Leaderboards Deaths TEXT 10", playerData.Deaths.ToString());
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Leaderboards KDR TEXT 10", ratio.ToString());
            }

            ShowLeaderboardPage(1);
        }

        public void ShowLeaderboardPage(int pageNum)
        {
            LeaderboardPageID = pageNum;
            var data = GetLeaderboardData();

            for (int i = 0; i <= 9; i++)
            {
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Leaderboards BUTTON {i}", false);
            }

            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Leaderboards Page TEXT", $"Page {pageNum}");
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Leaderboards Reset TEXT", GetLeaderboardRefreshTime());

            var lowerIndex = 10 * (pageNum - 1);
            var upperIndex = Math.Min(lowerIndex + 9, data.Count - 1);

            var index = 0;
            for (int i = lowerIndex; i <= upperIndex; i++)
            {
                var playerData = data[i];
                var kills = (decimal)(playerData.Kills + playerData.HeadshotKills);
                var deaths = (decimal)playerData.Deaths;

                var ratio = playerData.Deaths == 0 ? String.Format("{0:n}", kills) : String.Format("{0:n}", Math.Round(kills / deaths, 2));

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Leaderboards BUTTON {index}", true);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Leaderboards Rank TEXT {index}", (i + 1).ToString());
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Leaderboards Level TEXT {index}", playerData.Level.ToString());
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Leaderboards Level IMAGE {index}", Plugin.Instance.DBManager.Levels.TryGetValue(playerData.Level, out XPLevel level) ? level.IconLinkMedium : "");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Leaderboards Name TEXT {index}", playerData.SteamName);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Leaderboards Kills TEXT {index}", (playerData.Kills + playerData.HeadshotKills).ToString());
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Leaderboards Deaths TEXT {index}", playerData.Deaths.ToString());
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Leaderboards KDR TEXT {index}", ratio.ToString());
                index++;
            }
        }

        public void ForwardLeaderboardPage()
        {
            var data = GetLeaderboardData();

            if ((data.Count - 1) < LeaderboardPageID * 8)
            {
                ShowLeaderboard();
                return;
            }

            ShowLeaderboardPage(LeaderboardPageID + 1);
        }

        public void BackwardLeaderboardPage()
        {
            if (LeaderboardPageID == 1)
            {
                return;
            }

            ShowLeaderboardPage(LeaderboardPageID - 1);
        }

        public void SearchLeaderboardPlayer(string input)
        {
            var data = GetLeaderboardData();

            for (int i = 0; i <= 9; i++)
            {
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Leaderboards BUTTON {i}", false);
            }

            ThreadPool.QueueUserWorkItem((o) =>
            {
                var inputLower = input.ToLower();
                var searchPlayers = data.Where(k => k.SteamName.ToLower().Contains(inputLower)).Take(10).ToList();
                TaskDispatcher.QueueOnMainThread(() =>
                {
                    var maxCount = Math.Min(10, searchPlayers.Count);
                    for (int i = 0; i < maxCount; i++)
                    {
                        var playerData = searchPlayers[i];
                        var kills = (decimal)(playerData.Kills + playerData.HeadshotKills);
                        var deaths = (decimal)playerData.Deaths;

                        var ratio = playerData.Deaths == 0 ? String.Format("{0:n}", kills) : String.Format("{0:n}", Math.Round(kills / deaths, 2));

                        EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Leaderboards BUTTON {i}", true);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Leaderboards Rank TEXT {i}", (data.IndexOf(playerData) + 1).ToString());
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Leaderboards Level TEXT {i}", playerData.Level.ToString());
                        EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Leaderboards Level IMAGE {i}", Plugin.Instance.DBManager.Levels.TryGetValue(playerData.Level, out XPLevel level) ? level.IconLinkMedium : "");
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Leaderboards Name TEXT {i}", playerData.SteamName);
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Leaderboards Kills TEXT {i}", (playerData.Kills + playerData.HeadshotKills).ToString());
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Leaderboards Deaths TEXT {i}", playerData.Deaths.ToString());
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Leaderboards KDR TEXT {i}", ratio.ToString());
                    }
                });
            });
        }

        public List<LeaderboardData> GetLeaderboardData() =>
            LeaderboardPage switch
            {
                ELeaderboardPage.Daily => DB.PlayerDailyLeaderboard,
                ELeaderboardPage.Weekly => DB.PlayerWeeklyLeaderboard,
                ELeaderboardPage.Seasonal => DB.PlayerSeasonalLeaderboard,
                ELeaderboardPage.All => LeaderboardTab == ELeaderboardTab.Kill ? DB.PlayerAllTimeKill : DB.PlayerAllTimeLevel,
                _ => throw new ArgumentOutOfRangeException("LeaderboardPage is not as expected")
            };

        public Dictionary<CSteamID, LeaderboardData> GetLeaderboardDataLookup() =>
            LeaderboardPage switch
            {
                ELeaderboardPage.Daily => DB.PlayerDailyLeaderboardLookup,
                ELeaderboardPage.Weekly => DB.PlayerWeeklyLeaderboardLookup,
                ELeaderboardPage.Seasonal => DB.PlayerSeasonalLeaderboardLookup,
                ELeaderboardPage.All => DB.PlayerAllTimeLeaderboardLookup,
                _ => throw new ArgumentOutOfRangeException("LeaderboardPage is not as expected")
            };

        public string GetLeaderboardRefreshTime() =>
            LeaderboardPage switch
            {
                ELeaderboardPage.Daily => (DB.ServerOptions.DailyLeaderboardWipe.UtcDateTime - DateTime.UtcNow).ToString(@"hh\:mm\:ss"),
                ELeaderboardPage.Weekly => (DB.ServerOptions.WeeklyLeaderboardWipe.UtcDateTime - DateTime.UtcNow).ToString(@"dd\:hh\:mm\:ss"),
                _ => "00:00:00"
            };

        // Quest

        public void ShowQuests()
        {
            var quests = PlayerData.Quests.OrderBy(k => (int)k.Quest.QuestTier).ToList();
            var maxCount = Math.Min(6, quests.Count);
            for (int i = 0; i < maxCount; i++)
            {
                var quest = quests[i];
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Quest Complete {i} Toggler", quest.Amount >= quest.Quest.TargetAmount);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Quest Description TEXT {i}", quest.Quest.QuestDesc);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Quest Title TEXT {i}", quest.Quest.QuestTitle);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Quest Target TEXT {i}", $"{quest.Amount}/{quest.Quest.TargetAmount}");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Quest Reward TEXT {i}", $"+{quest.Quest.XP}XP");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Quest Bar Fill {i}", quest.Amount == 0 ? " " : new string(' ', Math.Min(256, quest.Amount * 256 / quest.Quest.TargetAmount)));
            }
        }

        public void ShowQuestCompletion()
        {
            var completedQuests = PlayerData.Quests.Count(k => k.Amount >= k.Quest.TargetAmount);
            var totalQuests = PlayerData.Quests.Count;

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Quest Complete", completedQuests == totalQuests);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Quest Complete Count TEXT", $"{completedQuests}/{totalQuests}");
        }

        // Achievement

        public void ShowAchievements()
        {
            MainPage = EMainPage.Achievements;
            SelectedAchievementMainPage(1);

            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Achievements Page 1 TEXT", "Weapons");
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Achievements Page 2 TEXT", "Other");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Achievements Page 3 BUTTON", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Achievements Page 4 BUTTON", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Achievements Page 5 BUTTON", false);
        }

        public void SelectedAchievementMainPage(int mainPage)
        {
            AchievementMainPage = mainPage;

            if (AchievementPageShower != null)
            {
                Plugin.Instance.StopCoroutine(AchievementPageShower);
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Achievements Previous BUTTON", false);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Achievements Next BUTTON", false);

            if (!AchievementPages.TryGetValue(mainPage, out Dictionary<int, PageAchievement> achievementPages))
            {
                Logging.Debug($"Error finding achievement pages for main page {mainPage}");
                return;
            }

            if (!achievementPages.TryGetValue(1, out PageAchievement firstPage))
            {
                Logging.Debug($"Error finding first page of achievement of main page {mainPage}");
                return;
            }


            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Achievements Previous BUTTON", achievementPages.Count > 1);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Achievements Next BUTTON", achievementPages.Count > 1);

            AchievementPageShower = Plugin.Instance.StartCoroutine(ShowAchievementSubPage(firstPage));
        }

        public IEnumerator ShowAchievementSubPage(PageAchievement page)
        {
            AchievementSubPage = page.PageID;
            Logging.Debug($"Showing achievement page to {Player.CharacterName} with main page {AchievementMainPage} and sub page {AchievementSubPage}");

            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Achievements Page TEXT", $"Page {page.PageID}");

            for (int i = 0; i <= 48; i++)
            {
                yield return new WaitForSeconds(0.01f);
                if (!page.Achievements.TryGetValue(i, out PlayerAchievement achievement))
                {
                    break;
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Achievements BUTTON {i}", true);
                var tier = achievement.GetCurrentTier();
                if (tier != null)
                {
                    EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Achievements IMAGE {i}", tier.TierPrevLarge);
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Achievements Basic {i}", achievement.CurrentTier == 0);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Achievements Bronze {i}", achievement.CurrentTier == 1);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Achievements Silver {i}", achievement.CurrentTier == 2);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Achievements Gold {i}", achievement.CurrentTier == 3);

                if (achievement.TryGetNextTier(out AchievementTier nextTier))
                {

                    var fillTxt = achievement.Amount == 0 ? " " : new string(' ', Math.Min(68, achievement.Amount * 68 / nextTier.TargetAmount));

                    switch (achievement.CurrentTier)
                    {
                        case 0:
                            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Achievements Basic Fill {i}", fillTxt);
                            break;
                        case 1:
                            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Achievements Bronze Fill {i}", fillTxt);
                            break;
                        case 2:
                            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Achievements Silver Fill {i}", fillTxt);
                            break;
                        case 3:
                            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Achievements Gold Fill {i}", fillTxt);
                            break;
                    }
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Achievements Claimable {i}", nextTier != null && achievement.Amount >= nextTier.TargetAmount);
            }
        }

        public void ForwardAchievementSubPage()
        {
            if (!AchievementPages.TryGetValue(AchievementMainPage, out Dictionary<int, PageAchievement> achievementPages))
            {
                Logging.Debug($"Error finding achievement pages for main page {AchievementMainPage}");
                ShowAchievements();
                return;
            }

            if (!achievementPages.TryGetValue(AchievementSubPage + 1, out PageAchievement nextPage) || !achievementPages.TryGetValue(1, out nextPage))
            {
                Logging.Debug($"Error finding next achievement page");
                SelectedAchievementMainPage(AchievementMainPage);
                return;
            }

            if (AchievementPageShower != null)
            {
                Plugin.Instance.StopCoroutine(AchievementPageShower);
            }
            AchievementPageShower = Plugin.Instance.StartCoroutine(ShowAchievementSubPage(nextPage));
        }

        public void BackwardAchievementSubPage()
        {
            if (!AchievementPages.TryGetValue(AchievementMainPage, out Dictionary<int, PageAchievement> achievementPages))
            {
                Logging.Debug($"Error finding achievement pages for main page {AchievementMainPage}");
                ShowAchievements();
                return;
            }

            if (!achievementPages.TryGetValue(AchievementSubPage - 1, out PageAchievement nextPage) || !achievementPages.TryGetValue(achievementPages.Keys.Max(), out nextPage))
            {
                Logging.Debug("Error finding next achievement page");
                SelectedAchievementMainPage(AchievementMainPage);
                return;
            }

            if (AchievementPageShower != null)
            {
                Plugin.Instance.StopCoroutine(AchievementPageShower);
            }
            AchievementPageShower = Plugin.Instance.StartCoroutine(ShowAchievementSubPage(nextPage));
        }

        public void ReloadAchievementSubPage()
        {
            if (!AchievementPages.TryGetValue(AchievementMainPage, out Dictionary<int, PageAchievement> achievementPages) || !achievementPages.TryGetValue(AchievementSubPage, out PageAchievement page))
            {
                Logging.Debug($"Unable to find selected page with main page {AchievementMainPage} and sub page {AchievementSubPage}");
                return;
            }

            if (AchievementPageShower != null)
            {
                Plugin.Instance.StopCoroutine(AchievementPageShower);
            }
            AchievementPageShower = Plugin.Instance.StartCoroutine(ShowAchievementSubPage(page));
        }

        public void SelectedAchievement(int selected)
        {
            if (!AchievementPages.TryGetValue(AchievementMainPage, out Dictionary<int, PageAchievement> achievementPages) || !achievementPages.TryGetValue(AchievementSubPage, out PageAchievement page))
            {
                Logging.Debug($"Error getting current page of achievement for {Player.CharacterName}");
                return;
            }

            if (!page.Achievements.TryGetValue(selected, out PlayerAchievement achievement))
            {
                Logging.Debug($"Error getting selected achievement with id {selected} for {Player.CharacterName}");
                return;
            }

            ShowAchievement(achievement);
        }

        public void ShowAchievement(PlayerAchievement achievement)
        {
            SelectedAchievementID = achievement.Achievement.AchievementID;

            var tier = achievement.GetCurrentTier();
            if (tier != null)
            {
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Achievements IMAGE", tier.TierPrevLarge);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Achievements TEXT", tier.TierTitle);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Achievements Description TEXT", tier.TierDesc);

                var targetAmount = tier.TargetAmount;
                if (achievement.TryGetNextTier(out AchievementTier nextTier))
                {
                    targetAmount = nextTier.TargetAmount;
                    if (nextTier.Rewards.Count >= 1 && TryGetAchievementRewardInfo(nextTier.Rewards[0], out string rewardName, out _, out _))
                    {
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Achievements Item TEXT", rewardName);
                    }
                    else
                    {
                        EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Achievements Item TEXT", "None");
                    }
                }
                else
                {
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Achievements Item TEXT", "None");
                }

                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Achievements Claim BUTTON", nextTier != null && achievement.Amount >= targetAmount);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Achievements Target TEXT", $"{achievement.Amount}/{targetAmount}");

                var fill = achievement.Amount == 0 ? " " : new string(' ', Math.Min(291, achievement.Amount * 291 / targetAmount));
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Achievements Fill 0", fill);
            }

            for (int i = 1; i <= 4; i++)
            {
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Achievements Reward Claimed {i}", achievement.CurrentTier >= i);

                if (!achievement.Achievement.TiersLookup.TryGetValue(i, out AchievementTier rewardTier) || rewardTier.Rewards.Count == 0 || !TryGetAchievementRewardInfo(rewardTier.Rewards[0], out _, out string rewardImage, out _))
                {
                    EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Achievements Reward IMAGE {i}", "");
                    continue;
                }

                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Achievements Reward IMAGE {i}", rewardImage);
            }
        }

        public void ReloadSelectedAchievement()
        {
            if (!PlayerData.AchievementsSearchByID.TryGetValue(SelectedAchievementID, out PlayerAchievement achievement))
            {
                Logging.Debug($"Unable to find selected achievement with id {SelectedAchievementID} for {Player.CharacterName}");
                return;
            }

            ShowAchievement(achievement);
        }

        public bool TryGetAchievementRewardInfo(Reward reward, out string rewardName, out string rewardImage, out string rewardRarity)
        {
            rewardName = "";
            rewardImage = "";
            rewardRarity = "";
            switch (reward.RewardType)
            {
                case ERewardType.Card:
                    if (!DB.Cards.TryGetValue(Convert.ToInt32(reward.RewardValue), out Card card))
                    {
                        return false;
                    }
                    rewardName = $"<color={Utility.GetRarityColor(card.CardRarity)}>{card.CardName}</color>";
                    rewardImage = card.IconLink;
                    rewardRarity = card.CardRarity;
                    return true;
                case ERewardType.GunSkin:
                    if (!DB.GunSkinsSearchByID.TryGetValue(Convert.ToInt32(reward.RewardValue), out GunSkin skin))
                    {
                        return false;
                    }
                    rewardName = $"<color={Utility.GetRarityColor(skin.SkinRarity)}>{skin.SkinName}</color>";
                    rewardImage = skin.IconLink;
                    rewardRarity = skin.SkinRarity;
                    return true;
                case ERewardType.Glove:
                    if (!DB.Gloves.TryGetValue(Convert.ToUInt16(reward.RewardValue), out Glove glove))
                    {
                        return false;
                    }
                    rewardName = $"<color={Utility.GetRarityColor(glove.GloveRarity)}>{glove.GloveName}</color>";
                    rewardImage = glove.IconLink;
                    rewardRarity = glove.GloveRarity;
                    return true;
                case ERewardType.Gun:
                    if (!DB.Guns.TryGetValue(Convert.ToUInt16(reward.RewardValue), out Gun gun))
                    {
                        return false;
                    }
                    rewardName = $"<color={Utility.GetRarityColor(gun.GunRarity)}>{gun.GunName}</color>";
                    rewardImage = gun.IconLink;
                    rewardRarity = gun.GunRarity;
                    return true;
                case ERewardType.GunCharm:
                    if (!DB.GunCharms.TryGetValue(Convert.ToUInt16(reward.RewardValue), out GunCharm gunCharm))
                    {
                        return false;
                    }
                    rewardName = $"<color={Utility.GetRarityColor(gunCharm.CharmRarity)}>{gunCharm.CharmName}</color>";
                    rewardImage = gunCharm.IconLink;
                    rewardRarity = gunCharm.CharmRarity;
                    return true;
                case ERewardType.Coin:
                    rewardName = $"<color=white>{reward.RewardValue} Coins</color>";
                    return true;
                case ERewardType.Credit:
                    rewardName = $"<color=white>{reward.RewardValue} Credits</color>";
                    return true;
                case ERewardType.LevelXP:
                    rewardName = $"<color=white>{reward.RewardValue} XP</color>";
                    return true;
                default:
                    return false;
            }
        }

        // Battlepass

        public void SetupBattlepass()
        {
            MainPage = EMainPage.Battlepass;
            ShowBattlepass();
            // Setup all 50 objects
            for (int i = 1; i <= 50; i++)
            {
                ShowBattlepassTier(i);
            }

            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Battlepass Claim BUTTON", false);
        }

        public void ShowBattlepass()
        {
            Logging.Debug($"Showing bp for {Player.CharacterName}");
            var bp = PlayerData.Battlepass;

            if (!DB.BattlepassTiersSearchByID.TryGetValue(bp.CurrentTier, out BattlepassTier currentTier))
            {
                Logging.Debug($"Error finding current battlepass tier for {Player.CharacterName}, returning");
                return;
            }

            var isBattlePassCompleted = !DB.BattlepassTiersSearchByID.TryGetValue(bp.CurrentTier + 1, out BattlepassTier nextTier);
            Logging.Debug($"Is Battlepass Completed: {isBattlePassCompleted}, next tier null: {nextTier == null}, current xp: {bp.XP}, current tier xp: {currentTier.XP}, next tier xp: {nextTier?.XP ?? 0}");

            // Setup the XP bar
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Battlepass Tier Target TEXT", $"{bp.XP}/{(isBattlePassCompleted ? currentTier.XP : nextTier.XP)}");
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Battlepass Tier TEXT", $"{bp.CurrentTier}");
            var fill = new string(' ', Math.Min(72, bp.XP == 0 ? 1 : bp.XP * 72 / (isBattlePassCompleted ? currentTier.XP : nextTier.XP)));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Battlepass Tier XP Fill", fill);
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Battlepass IMAGE", "");

            // Setup the preview section
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Battlepass Expire Timer", false);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Battlepass Expire TEXT", "365 Days");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Battlepass Buy Pass BUTTON", !PlayerData.HasBattlepass);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Battlepass Tier Skip", !isBattlePassCompleted);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Battlepass Tier Skip TEXT", $"{Config.Base.FileData.BattlepassTierSkipCost}");
            EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Battlepass Tier Skip IMAGE", "");
        }

        public void ShowBattlepassTier(int tierID)
        {
            var bp = PlayerData.Battlepass;
            if (!DB.BattlepassTiersSearchByID.TryGetValue(tierID, out BattlepassTier tier))
            {
                return;
            }

            var isTierUnlocked = bp.CurrentTier >= tierID;
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Battlepass Tier Completed Toggler {tierID}", isTierUnlocked);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Battlepass Tier Fill {tierID}", new string(' ', bp.CurrentTier > tierID ? 70 : (bp.CurrentTier == tierID ? Math.Min(70, bp.XP == 0 ? 1 : bp.XP * 70 / tier.XP) : 1)));

            // Setup top reward (free reward)
            var isRewardClaimed = bp.ClaimedFreeRewards.Contains(tierID);
            if (tier.FreeReward != null && TryGetBattlepassRewardInfo(tier.FreeReward, out string topRewardName, out string topRewardImage, out string topRewardRarity))
            {
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Battlepass T IMAGE {tierID}", topRewardImage);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Battlepass T TEXT {tierID}", topRewardName);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Battlepass T Locked {tierID}", !isTierUnlocked || isRewardClaimed);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Battlepass T Claimed {tierID}", isRewardClaimed);
                SendRarity("SERVER Battlepass T", topRewardRarity, tierID);
            }
            else
            {
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Battlepass T IMAGE {tierID}", "");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Battlepass T TEXT {tierID}", " ");
                if (isTierUnlocked)
                {
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Battlepass T Locked {tierID}", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Battlepass T Claimed {tierID}", true);
                }
            }

            // Setup bottom reward (premium reward)
            isRewardClaimed = bp.ClaimedPremiumRewards.Contains(tierID);
            if (tier.PremiumReward != null && TryGetBattlepassRewardInfo(tier.PremiumReward, out string bottomRewardName, out string bottomRewardImage, out string bottomRewardRarity))
            {
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Battlepass B IMAGE {tierID}", bottomRewardImage);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Battlepass B TEXT {tierID}", bottomRewardName);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Battlepass B Locked {tierID}", !PlayerData.HasBattlepass || !isTierUnlocked || isRewardClaimed);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Battlepass B Claimed {tierID}", isRewardClaimed);
                SendRarity("SERVER Battlepass B", bottomRewardRarity, tierID);
            }
            else
            {
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, $"SERVER Battlepass B IMAGE {tierID}", "");
                EffectManager.sendUIEffectText(Key, TransportConnection, true, $"SERVER Battlepass B TEXT {tierID}", " ");

                if (isTierUnlocked)
                {
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Battlepass B Locked {tierID}", true);
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, $"SERVER Battlepass B Claimed {tierID}", true);
                }
            }
        }

        public void SelectedBattlepassTier(bool isTop, int tierID)
        {
            Logging.Debug($"{Player.CharacterName} clicked top button: {isTop}, id: {tierID}");
            SelectedBattlepassTierID = (isTop, tierID);

            if (!DB.BattlepassTiersSearchByID.TryGetValue(tierID, out BattlepassTier tier))
            {
                Logging.Debug($"Error finding selected battlepass tier for {Player.CharacterName} with selected {tierID}");
                return;
            }

            if (((isTop && tier.FreeReward != null) || (!isTop && tier.PremiumReward != null)) && TryGetAchievementRewardInfo(isTop ? tier.FreeReward : tier.PremiumReward, out _, out string rewardImage, out _))
            {
                EffectManager.sendUIEffectImageURL(Key, TransportConnection, true, "SERVER Battlepass IMAGE", rewardImage);
                EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "SERVER Battlepass Claim BUTTON", true);
            }
        }

        public bool TryGetBattlepassRewardInfo(Reward reward, out string rewardName, out string rewardImage, out string rewardRarity)
        {
            rewardName = " ";
            rewardImage = "";
            rewardRarity = "";
            switch (reward.RewardType)
            {
                case ERewardType.Card:
                    if (!DB.Cards.TryGetValue(Convert.ToInt32(reward.RewardValue), out Card card))
                    {
                        return false;
                    }
                    rewardImage = card.IconLink;
                    rewardRarity = card.CardRarity;
                    return true;
                case ERewardType.GunSkin:
                    if (!DB.GunSkinsSearchByID.TryGetValue(Convert.ToInt32(reward.RewardValue), out GunSkin skin))
                    {
                        return false;
                    }
                    rewardImage = skin.IconLink;
                    rewardRarity = skin.SkinRarity;
                    return true;
                case ERewardType.Glove:
                    if (!DB.Gloves.TryGetValue(Convert.ToUInt16(reward.RewardValue), out Glove glove))
                    {
                        return false;
                    }
                    rewardImage = glove.IconLink;
                    rewardRarity = glove.GloveRarity;
                    return true;
                case ERewardType.Gun:
                    if (!DB.Guns.TryGetValue(Convert.ToUInt16(reward.RewardValue), out Gun gun))
                    {
                        return false;
                    }
                    rewardImage = gun.IconLink;
                    rewardRarity = gun.GunRarity;
                    return true;
                case ERewardType.GunCharm:
                    if (!DB.GunCharms.TryGetValue(Convert.ToUInt16(reward.RewardValue), out GunCharm gunCharm))
                    {
                        return false;
                    }
                    rewardImage = gunCharm.IconLink;
                    rewardRarity = gunCharm.CharmRarity;
                    return true;
                case ERewardType.Knife:
                    if (!DB.Knives.TryGetValue(Convert.ToUInt16(reward.RewardValue), out Knife knife))
                    {
                        return false;
                    }
                    rewardImage = knife.IconLink;
                    rewardRarity = knife.KnifeRarity;
                    return true;
                case ERewardType.BPBooster:
                    rewardName = $"<color=white>{Convert.ToDecimal(reward.RewardValue) * 100}%</color>";
                    return true;
                case ERewardType.XPBooster:
                    rewardName = $"<color=white>{Convert.ToDecimal(reward.RewardValue) * 100}%</color>";
                    return true;
                case ERewardType.GunXPBooster:
                    rewardName = $"<color=white>{Convert.ToDecimal(reward.RewardValue) * 100}%</color>";
                    return true;
                case ERewardType.Coin:
                    rewardName = $"<color=white>{reward.RewardValue}</color>";
                    rewardImage = "https://cdn.discordapp.com/attachments/458038940847439903/1000193091459891239/BlackoutTags.png";
                    return true;
                case ERewardType.Credit:
                    rewardName = $"<color=white>{reward.RewardValue}</color>";
                    rewardImage = "https://cdn.discordapp.com/attachments/458038940847439903/1000193090906226708/BlackoutPoints.png";
                    return true;
                case ERewardType.LevelXP:
                    rewardName = $"<color=white>{reward.RewardValue}</color>";
                    return true;
                default:
                    return false;
            }
        }

        // Match End Summary

        public IEnumerator ShowMatchEndSummary(MatchEndSummary summary)
        {
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "Scene Summary", true);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "Scene Summary XP Toggle", true);

            // Set the current level, xp and next level xp to animate the bar
            var currentLevel = summary.StartingLevel;
            var currentXP = summary.StartingXP;
            var nextLevelXP = DB.Levels.TryGetValue(currentLevel + 1, out XPLevel level) ? level.XPNeeded : 0;

            // Send the filled amount of bar and set the toggle to true and animate the text

            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP 1 TEXT", $"Match <color=#AD6816>{summary.MatchXP}</color> XP");
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP 2 TEXT", $"Match <color=#AD6816>{summary.MatchXPBonus}</color> Bonus XP");
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP 3 TEXT", $"Achievement <color=#AD6816>{summary.AchievementXPBonus}</color> Bonus XP");
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP 4 TEXT", $"Other <color=#AD6816>{summary.OtherXPBonus}</color> Bonus XP");

            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary Level 0 TEXT", currentLevel.ToString("D3"));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary Level 1 TEXT", nextLevelXP == 0 ? "MAX" : (currentLevel + 1).ToString("D3"));

            // Animate Match XP
            yield return new WaitForSeconds(1f);
            var boldSpaces = currentXP == 0 ? 1 : Math.Max(1, Math.Min(113, currentXP * 113 / (nextLevelXP == 0 ? 1 : nextLevelXP)));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 0", new string(' ', boldSpaces));
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "Scene Summary XP 0 Toggle", true);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary Type TEXT", "Match XP");

            var b = 0;
            for (int i = 1; i <= summary.MatchXP; i++)
            {
                yield return new WaitForSeconds(0.01f);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP 0 TEXT", $"+{i} XP");
                if (nextLevelXP != 0 && (currentXP + b) >= nextLevelXP)
                {
                    // Level has changed
                    b = 0;
                    currentLevel += 1;
                    currentXP = 0;
                    nextLevelXP = DB.Levels.TryGetValue(currentLevel + 1, out level) ? level.XPNeeded : 0;
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary Level 0 TEXT", currentLevel.ToString("D3"));
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary Level 1 TEXT", nextLevelXP == 0 ? "MAX" : (currentLevel + 1).ToString("D3"));

                    boldSpaces = 0;
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 0", " ");
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 1", " ");

                    continue;
                }
                else
                {
                    b += 1;
                }

                var highlightedSpaces = Math.Max(1, Math.Min(113 - boldSpaces, b * (113 - boldSpaces) / (nextLevelXP - currentXP)));
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 1", new string(' ', highlightedSpaces));
                if (i == summary.MatchXP)
                {
                    currentXP += b;
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "Scene Summary XP 0 Toggle", false);
                }
            }

            // Animate Match Bonus XP
            yield return new WaitForSeconds(0.18f);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP 0 TEXT", $"+0 XP");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "Scene Summary XP 1 Toggle", true);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 1", " ");
            boldSpaces = currentXP == 0 ? 1 : Math.Max(1, Math.Min(113, currentXP * 113 / (nextLevelXP == 0 ? 1 : nextLevelXP)));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 0", new string(' ', boldSpaces));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary Type TEXT", "Match Bonus XP");

            yield return new WaitForSeconds(0.05f);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "Scene Summary XP 0 Toggle", true);

            b = 0;
            for (int i = 1; i <= summary.MatchXPBonus; i++)
            {
                yield return new WaitForSeconds(0.010f);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP 0 TEXT", $"+{i} XP");
                if (nextLevelXP != 0 && (currentXP + b) >= nextLevelXP)
                {
                    // Level has changed
                    b = 0;
                    currentLevel += 1;
                    currentXP = 0;
                    nextLevelXP = DB.Levels.TryGetValue(currentLevel + 1, out level) ? level.XPNeeded : 0;
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary Level 0 TEXT", currentLevel.ToString("D3"));
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary Level 1 TEXT", nextLevelXP == 0 ? "MAX" : (currentLevel + 1).ToString("D3"));

                    boldSpaces = 0;
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 0", " ");
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 1", " ");

                    continue;
                }
                else
                {
                    b += 1;
                }

                var highlightedSpaces = Math.Max(1, Math.Min(113 - boldSpaces, b * (113 - boldSpaces) / (nextLevelXP - currentXP)));
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 1", new string(' ', highlightedSpaces));
                if (i == summary.MatchXPBonus)
                {
                    currentXP += b;
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "Scene Summary XP 0 Toggle", false);
                }
            }

            // Animate Achievement Bonus XP
            yield return new WaitForSeconds(0.18f);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP 0 TEXT", $"+0 XP");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "Scene Summary XP 2 Toggle", true);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 1", " ");
            boldSpaces = currentXP == 0 ? 1 : Math.Max(1, Math.Min(113, currentXP * 113 / (nextLevelXP == 0 ? 1 : nextLevelXP)));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 0", new string(' ', boldSpaces));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary Type TEXT", "Achievement Bonus XP");

            yield return new WaitForSeconds(0.05f);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "Scene Summary XP 0 Toggle", true);

            b = 0;
            for (int i = 1; i <= summary.AchievementXPBonus; i++)
            {
                yield return new WaitForSeconds(0.01f);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP 0 TEXT", $"+{i} XP");
                if (nextLevelXP != 0 && (currentXP + b) >= nextLevelXP)
                {
                    // Level has changed
                    b = 0;
                    currentLevel += 1;
                    currentXP = 0;
                    nextLevelXP = DB.Levels.TryGetValue(currentLevel + 1, out level) ? level.XPNeeded : 0;
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary Level 0 TEXT", currentLevel.ToString("D3"));
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary Level 1 TEXT", nextLevelXP == 0 ? "MAX" : (currentLevel + 1).ToString("D3"));

                    boldSpaces = 0;
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 0", " ");
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 1", " ");

                    continue;
                }
                else
                {
                    b += 1;
                }

                var highlightedSpaces = Math.Max(1, Math.Min(113 - boldSpaces, b * (113 - boldSpaces) / (nextLevelXP - currentXP)));
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 1", new string(' ', highlightedSpaces));
                if (i == summary.AchievementXPBonus)
                {
                    currentXP += b;
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "Scene Summary XP 0 Toggle", false);
                }
            }

            // Animate Other Bonus XP
            yield return new WaitForSeconds(0.18f);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP 0 TEXT", $"+0 XP");
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "Scene Summary XP 3 Toggle", true);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 1", " ");
            boldSpaces = currentXP == 0 ? 1 : Math.Max(1, Math.Min(113, currentXP * 113 / (nextLevelXP == 0 ? 1 : nextLevelXP)));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 0", new string(' ', boldSpaces));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary Type TEXT", "Other Bonus XP");

            yield return new WaitForSeconds(0.05f);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "Scene Summary XP 0 Toggle", true);

            b = 0;
            for (int i = 1; i <= summary.OtherXPBonus; i++)
            {
                yield return new WaitForSeconds(0.01f);
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP 0 TEXT", $"+{i} XP");
                if (nextLevelXP != 0 && (currentXP + b) >= nextLevelXP)
                {
                    // Level has changed
                    b = 0;
                    currentLevel += 1;
                    currentXP = 0;
                    nextLevelXP = DB.Levels.TryGetValue(currentLevel + 1, out level) ? level.XPNeeded : 0;
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary Level 0 TEXT", currentLevel.ToString("D3"));
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary Level 1 TEXT", nextLevelXP == 0 ? "MAX" : (currentLevel + 1).ToString("D3"));

                    boldSpaces = 0;
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 0", " ");
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 1", " ");

                    continue;
                }
                else
                {
                    b += 1;
                }


                var highlightedSpaces = Math.Max(1, Math.Min(113 - boldSpaces, b * (113 - boldSpaces) / (nextLevelXP - currentXP)));
                EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 1", new string(' ', highlightedSpaces));
                if (i == summary.OtherXPBonus)
                {
                    currentXP += b;
                    EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "Scene Summary XP 0 Toggle", false);
                }
            }

            yield return new WaitForSeconds(0.18f);
            EffectManager.sendUIEffectVisibility(Key, TransportConnection, true, "Scene Summary XP 4 Toggle", true);
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 1", " ");
            boldSpaces = currentXP == 0 ? 1 : Math.Max(1, Math.Min(113, currentXP * 113 / (nextLevelXP == 0 ? 1 : nextLevelXP)));
            EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Summary XP Bar Fill 0", new string(' ', boldSpaces));
        }

        // Events

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

        public IEnumerator RefreshTimer()
        {
            while (PlayerData != null)
            {
                yield return new WaitForSeconds(1f);
                if (MainPage == EMainPage.Leaderboard)
                {
                    EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Leaderboards Reset TEXT", GetLeaderboardRefreshTime());
                }

                EffectManager.sendUIEffectText(Key, TransportConnection, true, "SERVER Quest Expire TEXT", $"NEW QUESTS IN: {(DateTimeOffset.UtcNow > PlayerData.Quests[0].QuestEnd ? "00:00:00" : (DateTimeOffset.UtcNow - PlayerData.Quests[0].QuestEnd).ToString(@"hh\:mm\:ss"))}");
            }
        }
    }
}
