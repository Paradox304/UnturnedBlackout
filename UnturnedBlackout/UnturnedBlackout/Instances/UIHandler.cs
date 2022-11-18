using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Extensions;
using UnturnedBlackout.GameTypes;
using UnturnedBlackout.Managers;
using UnturnedBlackout.Models.Global;
using UnturnedBlackout.Models.UI;
using UnturnedBlackout.Models.Webhook;
using Enum = System.Enum;
using Field = UnturnedBlackout.Models.Webhook.Field;
using Logger = Rocket.Core.Logging.Logger;

namespace UnturnedBlackout.Instances;

public class UIHandler
{
    public const ushort MAIN_MENU_ID = 27632;
    public const short MAIN_MENU_KEY = 27632;
    public const ushort MIDGAME_LOADOUT_ID = 27643;
    public const short MIDGAME_LOADOUT_KEY = 27643;
    public const int MAX_ITEMS_PER_PAGE = 29;
    public const int MAX_ITEMS_PER_GRID = 15;
    public const int MAX_CASES_PER_CASE_PAGE = 11;
    public const int MAX_CASES_PER_STORE_PAGE = 5;
    public const int MAX_SKINS_PER_INVENTORY_PAGE = 20;
    public const int MAX_ACHIEVEMENTS_PER_PAGE = 48;
    public const int MAX_PREVIEW_CONTENT_PER_CASE = 19;
    public const int MAX_ROLLING_CONTENT_PER_CASE = 23;
    private const int MAX_SPACES_MATCH_END_SUMMARY = 113;
    private const int MAX_LOADOUTS_PER_PAGE = 9;

    private const int MINIMUM_LOADOUT_PAGE_ATTACHMENT_PRIMARY = 4;
    private const int MAXIMUM_LOADOUT_PAGE_ATTACHMENT_PRIMARY = 8;

    private const char STAR = '★';

    public DateTime LastButtonClicked { get; set; }
    private DatabaseManager DB => Plugin.Instance.DB;
    public CSteamID SteamID { get; set; }
    public UnturnedPlayer Player { get; set; }
    public PlayerLoadout PlayerLoadout { get; set; }
    public PlayerData PlayerData { get; set; }

    public Coroutine TimerRefresher { get; set; }
    public Coroutine AchievementPageShower { get; set; }
    public Coroutine MatchEndSummaryShower { get; set; }
    public Coroutine CrateUnboxer { get; set; }
    public Coroutine StatsShower { get; set; }
    
    public Coroutine ImageScroller { get; set; }
    
    public ITransportConnection TransportConnection { get; set; }
    public ConfigManager Config => Plugin.Instance.Config;

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

    public bool ShowingStats { get; set; }
    
    // Leaderboard
    public ELeaderboardPage LeaderboardPage { get; set; }
    public ELeaderboardTab LeaderboardTab { get; set; }
    public int LeaderboardPageID { get; set; }

    // Achievement
    public int AchievementMainPage { get; set; }
    public int AchievementSubPage { get; set; }
    public int SelectedAchievementID { get; set; }

    // Unboxing
    public EUnboxingPage UnboxingPage { get; set; }
    public int UnboxingPageID { get; set; }
    public int SelectedCaseID { get; set; }
    public ECurrency SelectedCaseBuyMethod { get; set; }
    public int SelectedCaseBuyAmount { get; set; }
    public bool IsUnboxing { get; set; }

    // Battlepass
    public (bool, int) SelectedBattlepassTierID { get; set; }
    
    // Scrollable Image
    
    public int CurrentScrollableImage { get; set; }

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
    public Dictionary<int, PageUnboxCase> UnboxCasesPages { get; set; }
    public Dictionary<int, PageUnboxStore> UnboxStorePages { get; set; }
    public Dictionary<int, PageUnboxInventory> UnboxInventoryPages { get; set; }

    public UIHandler(UnturnedPlayer player)
    {
        Logging.Debug($"Creating UIHandler for {player.CSteamID}");
        SteamID = player.CSteamID;
        Player = player;
        TransportConnection = player.Player.channel.GetOwnerTransportConnection();
        if (!DB.PlayerLoadouts.TryGetValue(player.CSteamID, out var loadout))
        {
            Logging.Debug($"Error finding player loadout for {player.CharacterName}, failed to initialize UIHandler for player");
            return;
        }

        if (!DB.PlayerData.TryGetValue(player.CSteamID, out var data))
        {
            Logging.Debug($"Error finding player data for {player.CharacterName}, failed to initialize UIHandler for player");
            return;
        }

        LastButtonClicked = DateTime.UtcNow;
        PlayerData = data;
        PlayerLoadout = loadout;

        MainPage = EMainPage.NONE;
        BuildPages();
        ShowUI();
    }

    public void Destroy()
    {
        TimerRefresher.Stop();
        AchievementPageShower.Stop();
        MatchEndSummaryShower.Stop();
        CrateUnboxer.Stop();
        StatsShower.Stop();
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
        BuildUnboxingCasesPages();
        BuildUnboxingStorePages();
        BuildUnboxingInventoryPages();
    }

    public void BuildLoadoutPages()
    {
        LoadoutPages = new();
        var index = 0;
        var page = 1;
        Dictionary<int, Loadout> loadouts = new();

        foreach (var loadout in PlayerLoadout.Loadouts)
        {
            loadouts.Add(index, loadout.Value);
            if (index == MAX_LOADOUTS_PER_PAGE)
            {
                LoadoutPages.Add(page, new(page, loadouts));
                index = 0;
                page++;
                loadouts = new();
                continue;
            }

            index++;
        }

        if (loadouts.Count != 0)
            LoadoutPages.Add(page, new(page, loadouts));
    }

    public void BuildPistolPages()
    {
        var guns = PlayerLoadout.Guns.Values.Where(k => k.Gun.GunType == EGun.PISTOL).OrderBy(k => k.Gun.LevelRequirement).ToList();
        Dictionary<int, LoadoutGun> gunItems = new();
        PistolPages = new();
        var index = 0;
        var page = 1;

        foreach (var gun in guns)
        {
            gunItems.Add(index, gun);
            if (index == MAX_ITEMS_PER_PAGE)
            {
                PistolPages.Add(page, new(page, gunItems));
                index = 0;
                page++;
                gunItems = new();
                continue;
            }

            index++;
        }

        if (gunItems.Count != 0)
            PistolPages.Add(page, new(page, gunItems));
    }

    public void BuildSMGPages()
    {
        var guns = PlayerLoadout.Guns.Values.Where(k => k.Gun.GunType == EGun.SUBMACHINE_GUNS).OrderBy(k => k.Gun.LevelRequirement).ToList();
        Dictionary<int, LoadoutGun> gunItems = new();
        SMGPages = new();
        var index = 0;
        var page = 1;

        foreach (var gun in guns)
        {
            gunItems.Add(index, gun);
            if (index == MAX_ITEMS_PER_PAGE)
            {
                SMGPages.Add(page, new(page, gunItems));
                index = 0;
                page++;
                gunItems = new();
                continue;
            }

            index++;
        }

        if (gunItems.Count != 0)
            SMGPages.Add(page, new(page, gunItems));
    }

    public void BuildShotgunPages()
    {
        var guns = PlayerLoadout.Guns.Values.Where(k => k.Gun.GunType == EGun.SHOTGUNS).OrderBy(k => k.Gun.LevelRequirement).ToList();
        Dictionary<int, LoadoutGun> gunItems = new();
        ShotgunPages = new();
        var index = 0;
        var page = 1;

        foreach (var gun in guns)
        {
            gunItems.Add(index, gun);
            if (index == MAX_ITEMS_PER_PAGE)
            {
                ShotgunPages.Add(page, new(page, gunItems));
                index = 0;
                page++;
                gunItems = new();
                continue;
            }

            index++;
        }

        if (gunItems.Count != 0)
            ShotgunPages.Add(page, new(page, gunItems));
    }

    public void BuildLMGPages()
    {
        var guns = PlayerLoadout.Guns.Values.Where(k => k.Gun.GunType == EGun.LIGHT_MACHINE_GUNS).OrderBy(k => k.Gun.LevelRequirement).ToList();
        Dictionary<int, LoadoutGun> gunItems = new();
        LMGPages = new();
        var index = 0;
        var page = 1;

        foreach (var gun in guns)
        {
            gunItems.Add(index, gun);
            if (index == MAX_ITEMS_PER_PAGE)
            {
                LMGPages.Add(page, new(page, gunItems));
                index = 0;
                page++;
                gunItems = new();
                continue;
            }

            index++;
        }

        if (gunItems.Count != 0)
            LMGPages.Add(page, new(page, gunItems));
    }

    public void BuildARPages()
    {
        var guns = PlayerLoadout.Guns.Values.Where(k => k.Gun.GunType == EGun.ASSAULT_RIFLES).OrderBy(k => k.Gun.LevelRequirement).ToList();
        Dictionary<int, LoadoutGun> gunItems = new();
        ARPages = new();
        var index = 0;
        var page = 1;

        foreach (var gun in guns)
        {
            gunItems.Add(index, gun);
            if (index == MAX_ITEMS_PER_PAGE)
            {
                ARPages.Add(page, new(page, gunItems));
                index = 0;
                page++;
                gunItems = new();
                continue;
            }

            index++;
        }

        if (gunItems.Count != 0)
            ARPages.Add(page, new(page, gunItems));
    }

    public void BuildSniperPages()
    {
        var guns = PlayerLoadout.Guns.Values.Where(k => k.Gun.GunType == EGun.SNIPER_RIFLES).OrderBy(k => k.Gun.LevelRequirement).ToList();
        Dictionary<int, LoadoutGun> gunItems = new();
        SniperPages = new();
        var index = 0;
        var page = 1;

        foreach (var gun in guns)
        {
            gunItems.Add(index, gun);
            if (index == MAX_ITEMS_PER_PAGE)
            {
                SniperPages.Add(page, new(page, gunItems));
                index = 0;
                page++;
                gunItems = new();
                continue;
            }

            index++;
        }

        if (gunItems.Count != 0)
            SniperPages.Add(page, new(page, gunItems));
    }

    public void BuildCarbinePages()
    {
        var guns = PlayerLoadout.Guns.Values.Where(k => k.Gun.GunType == EGun.CARBINES).OrderBy(k => k.Gun.LevelRequirement).ToList();
        Dictionary<int, LoadoutGun> gunItems = new();
        CarbinePages = new();
        var index = 0;
        var page = 1;

        foreach (var gun in guns)
        {
            gunItems.Add(index, gun);
            if (index == MAX_ITEMS_PER_PAGE)
            {
                CarbinePages.Add(page, new(page, gunItems));
                index = 0;
                page++;
                gunItems = new();
                continue;
            }

            index++;
        }

        if (gunItems.Count != 0)
            CarbinePages.Add(page, new(page, gunItems));
    }

    public void BuildGunSkinPages()
    {
        GunSkinPages = new();
        foreach (var gun in PlayerLoadout.GunSkinsSearchByGunID)
        {
            var index = 0;
            var page = 1;
            Dictionary<int, GunSkin> gunSkins = new();
            GunSkinPages.Add(gun.Key, new());
            foreach (var gunSkin in gun.Value.OrderByDescending(k => (byte)k.SkinRarity))
            {
                gunSkins.Add(index, gunSkin);
                if (index == MAX_ITEMS_PER_GRID)
                {
                    GunSkinPages[gun.Key].Add(page, new(page, gunSkins));
                    gunSkins = new();
                    index = 0;
                    page++;
                    continue;
                }

                index++;
            }

            if (gunSkins.Count != 0)
                GunSkinPages[gun.Key].Add(page, new(page, gunSkins));
        }
    }

    public void BuildAttachmentPages()
    {
        AttachmentPages = new();
        foreach (var gun in PlayerLoadout.Guns)
            BuildAttachmentPages(gun.Value);
    }

    public void BuildAttachmentPages(LoadoutGun gun)
    {
        if (AttachmentPages.ContainsKey(gun.Gun.GunID))
            _ = AttachmentPages.Remove(gun.Gun.GunID);

        AttachmentPages.Add(gun.Gun.GunID, new());

        for (var i = 0; i <= 3; i++)
        {
            var attachmentType = (EAttachment)i;
            var index = 0;
            var page = 1;
            Dictionary<int, LoadoutAttachment> attachments = new();
            AttachmentPages[gun.Gun.GunID].Add(attachmentType, new());
            foreach (var attachment in gun.Attachments.Values.Where(k => k.Attachment.AttachmentType == attachmentType).OrderBy(k => k.LevelRequirement))
            {
                attachments.Add(index, attachment);
                if (index == MAX_ITEMS_PER_PAGE)
                {
                    AttachmentPages[gun.Gun.GunID][attachmentType].Add(page, new(page, attachments));
                    attachments = new();
                    index = 0;
                    page++;
                    continue;
                }

                index++;
            }

            if (attachments.Count != 0)
                AttachmentPages[gun.Gun.GunID][attachmentType].Add(page, new(page, attachments));
        }
    }

    public void BuildGunCharmPages()
    {
        GunCharmPages = new();
        var index = 0;
        var page = 1;
        Dictionary<int, LoadoutGunCharm> gunCharms = new();
        foreach (var gunCharm in PlayerLoadout.GunCharms.Values.OrderByDescending(k => (byte)k.GunCharm.CharmRarity))
        {
            gunCharms.Add(index, gunCharm);
            if (index == MAX_ITEMS_PER_GRID)
            {
                GunCharmPages.Add(page, new(page, gunCharms));
                gunCharms = new();
                index = 0;
                page++;
                continue;
            }

            index++;
        }

        if (gunCharms.Count != 0)
            GunCharmPages.Add(page, new(page, gunCharms));
    }

    public void BuildKnifePages()
    {
        KnifePages = new();
        var index = 0;
        var page = 1;
        Dictionary<int, LoadoutKnife> knives = new();
        foreach (var knife in PlayerLoadout.Knives.Values.OrderByDescending(k => k.Knife.KnifeName.Count(c => c == STAR)))
        {
            knives.Add(index, knife);
            if (index == MAX_ITEMS_PER_GRID)
            {
                KnifePages.Add(page, new(page, knives));
                knives = new();
                index = 0;
                page++;
                continue;
            }

            index++;
        }

        if (knives.Count != 0)
            KnifePages.Add(page, new(page, knives));
    }

    public void BuildPerkPages()
    {
        PerkPages = new();
        for (var i = 1; i <= 3; i++)
        {
            PerkPages.Add(i, new());
            var index = 0;
            var page = 1;
            Dictionary<int, LoadoutPerk> perks = new();
            foreach (var perk in PlayerLoadout.Perks.Values.Where(k => k.Perk.PerkType == i).OrderBy(k => k.Perk.LevelRequirement))
            {
                perks.Add(index, perk);
                if (index == MAX_ITEMS_PER_PAGE)
                {
                    PerkPages[i].Add(page, new(page, perks));
                    perks = new();
                    index = 0;
                    page++;
                    continue;
                }

                index++;
            }

            if (perks.Count != 0)
                PerkPages[i].Add(page, new(page, perks));
        }
    }

    public void BuildTacticalPages()
    {
        TacticalPages = new();
        var gadgets = PlayerLoadout.Gadgets.Values.Where(k => k.Gadget.IsTactical).OrderBy(k => k.Gadget.LevelRequirement).ToList();
        var index = 0;
        var page = 1;
        Dictionary<int, LoadoutGadget> gadgetItems = new();
        foreach (var gadget in gadgets)
        {
            gadgetItems.Add(index, gadget);
            if (index == MAX_ITEMS_PER_PAGE)
            {
                TacticalPages.Add(page, new(page, gadgetItems));
                gadgetItems = new();
                index = 0;
                page++;
                continue;
            }

            index++;
        }

        if (gadgetItems.Count != 0)
            TacticalPages.Add(page, new(page, gadgetItems));
    }

    public void BuildLethalPages()
    {
        LethalPages = new();
        var gadgets = PlayerLoadout.Gadgets.Values.Where(k => !k.Gadget.IsTactical).OrderBy(k => k.Gadget.LevelRequirement).ToList();
        var index = 0;
        var page = 1;
        Dictionary<int, LoadoutGadget> gadgetItems = new();
        foreach (var gadget in gadgets)
        {
            gadgetItems.Add(index, gadget);
            if (index == MAX_ITEMS_PER_PAGE)
            {
                LethalPages.Add(page, new(page, gadgetItems));
                gadgetItems = new();
                index = 0;
                page++;
                continue;
            }

            index++;
        }

        if (gadgetItems.Count != 0)
            LethalPages.Add(page, new(page, gadgetItems));
    }

    public void BuildCardPages()
    {
        CardPages = new();
        var index = 0;
        var page = 1;
        Dictionary<int, LoadoutCard> cards = new();
        foreach (var card in PlayerLoadout.Cards.Values.Where(k => k.Card.LevelRequirement >= 0 || k.IsBought).OrderByDescending(k => (byte)k.Card.CardRarity))
        {
            cards.Add(index, card);
            if (index == MAX_ITEMS_PER_GRID)
            {
                CardPages.Add(page, new(page, cards));
                cards = new();
                index = 0;
                page++;
                continue;
            }

            index++;
        }

        if (cards.Count != 0)
            CardPages.Add(page, new(page, cards));
    }

    public void BuildGlovePages()
    {
        GlovePages = new();
        var index = 0;
        var page = 1;
        Dictionary<int, LoadoutGlove> gloves = new();
        foreach (var glove in PlayerLoadout.Gloves.Values.OrderByDescending(k => k.Glove.GloveName.Count(c => c == STAR)))
        {
            gloves.Add(index, glove);
            if (index == MAX_ITEMS_PER_GRID)
            {
                GlovePages.Add(page, new(page, gloves));
                gloves = new();
                index = 0;
                page++;
                continue;
            }

            index++;
        }

        if (gloves.Count != 0)
            GlovePages.Add(page, new(page, gloves));
    }

    public void BuildKillstreakPages()
    {
        KillstreakPages = new();
        var index = 0;
        var page = 1;
        Dictionary<int, LoadoutKillstreak> killstreaks = new();

        foreach (var killstreak in PlayerLoadout.Killstreaks.Values.OrderBy(k => k.Killstreak.BuyPrice))
        {
            killstreaks.Add(index, killstreak);
            if (index == MAX_ITEMS_PER_GRID)
            {
                KillstreakPages.Add(page, new(page, killstreaks));
                killstreaks = new();
                index = 0;
                page++;
                continue;
            }

            index++;
        }

        if (killstreaks.Count != 0)
            KillstreakPages.Add(page, new(page, killstreaks));
    }

    public void BuildAchievementPages()
    {
        AchievementPages = new();
        for (var i = 1; i <= 5; i++)
        {
            AchievementPages.Add(i, new());
            var index = 0;
            var page = 1;
            Dictionary<int, PlayerAchievement> achievements = new();
            foreach (var achievement in PlayerData.Achievements.Where(k => k.Achievement.PageID == i).OrderByDescending(k => k.CurrentTier).ThenByDescending(k => k.TryGetNextTier(out var nextTier) ? k.Amount * 100 / nextTier.TargetAmount : 100))
            {
                achievements.Add(index, achievement);
                if (index == MAX_ACHIEVEMENTS_PER_PAGE)
                {
                    AchievementPages[i].Add(page, new(page, achievements));
                    achievements = new();
                    index = 0;
                    page++;
                    continue;
                }

                index++;
            }

            if (achievements.Count != 0)
                AchievementPages[i].Add(page, new(page, achievements));
        }
    }

    public void BuildUnboxingCasesPages()
    {
        UnboxCasesPages = new();
        var index = 0;
        var page = 1;
        Dictionary<int, PlayerCase> cases = new();
        foreach (var @case in PlayerData.Cases)
        {
            cases.Add(index, @case);
            if (index == MAX_CASES_PER_CASE_PAGE)
            {
                UnboxCasesPages.Add(page, new(page, cases));
                cases = new();
                index = 0;
                page++;
                continue;
            }

            index++;
        }

        if (cases.Count != 0)
            UnboxCasesPages.Add(page, new(page, cases));
    }

    public void BuildUnboxingStorePages()
    {
        UnboxStorePages = new();
        var index = 0;
        var page = 1;
        Dictionary<int, Case> cases = new();
        foreach (var @case in DB.Cases.Values.Where(k => k.IsBuyable).OrderByDescending(k => k.CaseID))
        {
            cases.Add(index, @case);
            if (index == MAX_CASES_PER_STORE_PAGE)
            {
                UnboxStorePages.Add(page, new(page, cases));
                cases = new();
                index = 0;
                page++;
                continue;
            }

            index++;
        }

        if (cases.Count != 0)
            UnboxStorePages.Add(page, new(page, cases));
    }

    public void BuildUnboxingInventoryPages()
    {
        UnboxInventoryPages = new();
        var index = 0;
        var page = 1;
        Dictionary<int, object> skins = new();
        foreach (var skin in PlayerLoadout.Knives.Values.Where(k => k.IsBought && k.Knife.LevelRequirement != 0).OrderByDescending(k => k.Knife.KnifeName.Count(c => c == STAR)).ThenBy(k => k.Knife.KnifeName))
        {
            skins.Add(index, skin.Knife);
            if (index == MAX_SKINS_PER_INVENTORY_PAGE)
            {
                UnboxInventoryPages.Add(page, new(page, skins));
                skins = new();
                index = 0;
                page++;
                continue;
            }

            index++;
        }

        foreach (var skin in PlayerLoadout.Gloves.Values.Where(k => k.IsBought).OrderByDescending(k => k.Glove.GloveName.Count(c => c == STAR)).ThenBy(k => k.Glove.GloveName))
        {
            skins.Add(index, skin.Glove); 
            
            if (index == MAX_SKINS_PER_INVENTORY_PAGE)
            {
                UnboxInventoryPages.Add(page, new(page, skins));
                skins = new();
                index = 0;
                page++;
                continue;
            }

            index++;
        }
        
        foreach (var skin in PlayerLoadout.GunSkinsSearchByID.Values.OrderByDescending(k => (byte)k.SkinRarity).ThenBy(k => k.Gun.GunName))
        {
            skins.Add(index, skin); 
            
            if (index == MAX_SKINS_PER_INVENTORY_PAGE)
            {
                UnboxInventoryPages.Add(page, new(page, skins));
                skins = new();
                index = 0;
                page++;
                continue;
            }

            index++;
        }
        
        if (skins.Count != 0)
            UnboxInventoryPages.Add(page, new(page, skins));
    }

    public void ShowUI(MatchEndSummary summary = null)
    {
        EffectManager.sendUIEffect(MAIN_MENU_ID, MAIN_MENU_KEY, TransportConnection, true);
        Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);
        SetupMainMenu();

        ShowingStats = false;
        AchievementPageShower.Stop();
        CrateUnboxer.Stop();
        MatchEndSummaryShower.Stop();
        TimerRefresher = Plugin.Instance.StartCoroutine(RefreshTimer());
        ImageScroller = Plugin.Instance.StartCoroutine(ScrollImages());
        
        if (summary != null)
            MatchEndSummaryShower = Plugin.Instance.StartCoroutine(ShowMatchEndSummary(summary));
    }

    public void HideUI()
    {
        EffectManager.askEffectClearByID(MAIN_MENU_ID, TransportConnection);
        Player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
        MainPage = EMainPage.NONE;
        ShowingStats = false;
        
        ImageScroller.Stop();
        TimerRefresher.Stop();
        AchievementPageShower.Stop();
        CrateUnboxer.Stop();
        MatchEndSummaryShower.Stop();
        StatsShower.Stop();
    }

    public void SetupMainMenu()
    {
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"Volume {PlayerData.Volume} Enabler", true);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Options Audio Music Toggler", PlayerData.Music);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Options Audio Flag Toggler", PlayerData.HideFlag);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Player Icon", PlayerData.AvatarLinks[2]);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Player Name", (PlayerData.HasPrime ? UIManager.PRIME_SYMBOL : "") + PlayerData.SteamName);

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Version TEXT", Plugin.Instance.Translate("Version").ToRich());

        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Currency Credits IMAGE", Config.Icons.FileData.PointsSmallIconLink);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Currency Coins IMAGE", Config.Icons.FileData.BlacktagsSmallIconLink);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Currency Scrap IMAGE", Config.Icons.FileData.ScrapSmallIconLink);

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Staff BUTTON", PlayerData.IsStaff);
        
        OnCurrencyUpdated(ECurrency.COIN);
        OnCurrencyUpdated(ECurrency.SCRAP);
        OnCurrencyUpdated(ECurrency.CREDIT);

        ClearChat();
        ShowXP();
        ShowQuestCompletion();
        BuildAchievementPages();
        _ = Plugin.Instance.StartCoroutine(SetupScrollableImages());
    }

    public void ShowXP()
    {
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER XP Num", Plugin.Instance.Translate("Level_Show", PlayerData.Level).ToRich());
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER XP Icon", DB.Levels.TryGetValue(PlayerData.Level, out var level) ? level.IconLinkMedium : "");
        var spaces = 0;
        if (PlayerData.TryGetNeededXP(out var neededXP))
            spaces = Math.Min(176, neededXP == 0 ? 0 : PlayerData.XP * 176 / neededXP);

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER XP Bar Fill", spaces == 0 ? "" : new(UIManager.HAIRSPACE_SYMBOL_CHAR, spaces));
    }

    public void ClearChat()
    {
        var steamPlayer = Player.SteamPlayer();
        for (var i = 0; i <= 10; i++)
            ChatManager.serverSendMessage("", Color.white, toPlayer: steamPlayer);
    }

    public void SendNotEnoughCurrencyModal(ECurrency currency)
    {
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Enough Currency Modal", true);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Enough Currency Modal TEXT", Plugin.Instance.Translate("Not_Enough_Currency", Utility.ToFriendlyName(currency)).ToRich());
    }

    public void ActivateStaffMode()
    {
        var gPlayer = Plugin.Instance.Game.GetGamePlayer(Player);
        if (gPlayer == null)
        {
            Logging.Debug($"Unable to find game player for {Player.CharacterName}");
            return;
        }

        gPlayer.StaffMode = true;
        Player.Player.teleportToLocationUnsafe(new(0, 0, 0), 0);
        Player.VanishMode = true;
        Player.Player.look.sendFreecamAllowed(true);
        Player.Player.look.sendSpecStatsAllowed(true);
        Player.Player.movement.sendPluginSpeedMultiplier(0f);
        HideUI();
        Utility.Say(Player, "<color=green>Staff Mode Activated</color>");
    }
    
    public void ShowPlayPage()
    {
        MainPage = EMainPage.PLAY;
        ShowPlayPage(EPlayPage.GAMES);
    }

    public void ShowPlayPage(EPlayPage playPage)
    {
        SelectedGameID = 0;
        if (playPage == EPlayPage.GAMES)
            ShowGames();
        else if (playPage == EPlayPage.SERVERS)
            ShowServers();
    }

    public void SelectedPlayButton(int selected)
    {
        var games = Plugin.Instance.Game.Games;
        var servers = DB.Servers;
        if (PlayPage == EPlayPage.GAMES)
        {
            if (selected + 1 > games.Count)
                return;

            ShowGame(games[selected]);
        }
        else if (PlayPage == EPlayPage.SERVERS)
        {
            if (selected + 1 > servers.Count)
                return;

            ShowServer(servers[selected]);
        }

        SelectedGameID = selected;
    }

    public void ClickedJoinButton()
    {
        if (PlayPage == EPlayPage.GAMES)
            Plugin.Instance.Game.AddPlayerToGame(Player, SelectedGameID);
        else if (PlayPage == EPlayPage.SERVERS)
        {
            var server = DB.Servers[SelectedGameID];
            if (server.IsOnline)
                Player.Player.sendRelayToServer(server.IPNo, server.PortNo, "", false);
        }
    }

    public void ShowGames()
    {
        var games = Plugin.Instance.Game.Games;
        PlayPage = EPlayPage.GAMES;

        for (var i = 0; i <= 13; i++)
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Play BUTTON {i}", false);

        var maxCount = Math.Min(14, games.Count);

        for (var index = 0; index < maxCount; index++)
        {
            var game = games[index];
            var gameMode = Config.Gamemode.FileData.GamemodeOptions.FirstOrDefault(k => k.GameType == game.GameMode);
            if (gameMode == null)
                return;

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Play BUTTON {index}", true);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Play Map TEXT {index}", game.Location.LocationName);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Play Mode TEXT {index}",
                (game.IsHardcore ? $"<color={Config.Base.FileData.HardcoreColor}>Hardcore</color> " : "") + $"<color={gameMode.GamemodeColor}>{Plugin.Instance.Translate($"{game.GameMode}_Name_Full")}</color>");

            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Play Players TEXT {index}", $"{game.GetPlayerCount()}/{game.Location.GetMaxPlayers(game.GameMode)}");
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Play Status TEXT {index}", game.GamePhase.ToFriendlyName());
        }

        SelectedPlayButton(SelectedGameID);
    }

    public void ShowGame(Game game)
    {
        var gameMode = Config.Gamemode.FileData.GamemodeOptions.FirstOrDefault(k => k.GameType == game.GameMode);
        if (gameMode == null)
            return;

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Play Server TEXT", "");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Play Mode TEXT", (game.IsHardcore ? $"<color={Config.Base.FileData.HardcoreColor}>Hardcore</color> " : "") + $"<color={gameMode.GamemodeColor}>{Plugin.Instance.Translate($"{game.GameMode}_Name_Full")}</color>");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Play Map {game.Location.LocationName} Enabler", true);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Play Map TEXT", game.Location.LocationName);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Play Description TEXT", Plugin.Instance.Translate($"{game.GameMode}_Description_Full"));
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Play Join BUTTON", game.GamePhase != EGamePhase.ENDING && game.GamePhase != EGamePhase.ENDED);
    }

    public void UpdateGamePlayerCount(Game game)
    {
        var index = Plugin.Instance.Game.Games.IndexOf(game);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Play Players TEXT {index}", $"{game.GetPlayerCount()}/{game.Location.GetMaxPlayers(game.GameMode)}");
    }

    public void ShowServers()
    {
        PlayPage = EPlayPage.SERVERS;
        for (var i = 0; i <= 13; i++)
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Play BUTTON {i}", false);

        var servers = DB.Servers;
        var maxCount = Math.Min(14, servers.Count);

        for (var index = 0; index < maxCount; index++)
        {
            var server = servers[index];
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Play BUTTON {index}", true);
            var name = string.IsNullOrEmpty(server.Name) ? server.ServerName : server.Name;
            if (server.IsCurrentServer)
                name = $"<color=#FFFF00>{name}</color>";

            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Play Server TEXT {index}", name);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Play Status TEXT {index}",
                server.IsOnline ? "<color=#36ff3c>Online</color>" : (DateTime.UtcNow - server.LastOnline).TotalSeconds < 120 ? "<color=#f5fa73>Restarting</color>" : "<color=#ed2626>Offline</color>");

            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Play Players TEXT {index}", server.IsOnline ? $"{server.Players}/{server.MaxPlayers}" : "0/0");
        }

        SelectedPlayButton(SelectedGameID);
    }

    public void ShowServer(Server server)
    {
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Play Server TEXT", string.IsNullOrEmpty(server.Name) ? server.ServerName : server.Name);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Play Mode TEXT", " ");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Play Server {server.FriendlyIP.Split('.')[0].ToUpper()} Enabler", true);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Play Map TEXT", " ");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Play Description TEXT", server.ServerDesc);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Play Join BUTTON", server.IsOnline && !server.IsCurrentServer);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Play Ping TEXT", " ");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Play IP TEXT", $"{server.FriendlyIP}:{server.Port}");
    }

    public void ShowOptions()
    {
        for (var i = 0; i <= 4; i++)
            SetHotkeyInput((EHotkey)i);
    }

    public void MusicButtonPressed()
    {
        PlayerData.Music = !PlayerData.Music;

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Options Audio Music Toggler", PlayerData.Music);

        DB.ChangePlayerMusic(SteamID, PlayerData.Music);
    }

    public void FlagButtonPressed()
    {
        PlayerData.HideFlag = !PlayerData.HideFlag;

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Options Audio Flag Toggler", PlayerData.HideFlag);

        DB.ChangePlayerHideFlag(SteamID, PlayerData.HideFlag);
    }

    public void VolumeButtonPressed(int volume)
    {
        DB.SetPlayerVolume(SteamID, volume);
    }

    public void SetHotkey(EHotkey hotkeyType, string hotkeyInput)
    {
        // Check if the input is a number, and also check if they aren't setting 1 or 2 as that's primary and secondary
        if (!int.TryParse(hotkeyInput, out var newHotkey) || newHotkey is 1 or 2)
        {
            SetHotkeyInput(hotkeyType);
            return;
        }

        // Check if the new hotkey is the same previous one, if so ignore
        var currentHotkey = PlayerData.Hotkeys[(int)hotkeyType];
        if (currentHotkey == newHotkey)
            return;

        // Check if the set hotkey is set somewhere else for some other thing, if so, swap both the values
        var index = PlayerData.Hotkeys.IndexOf(newHotkey);
        if (index != -1)
        {
            PlayerData.Hotkeys[index] = currentHotkey;
            SetHotkeyInput((EHotkey)index);
        }

        // Set the new hotkey and update DB side
        PlayerData.Hotkeys[(int)hotkeyType] = newHotkey;
        DB.SetPlayerHotkeys(SteamID, PlayerData.Hotkeys);
    }

    private void SetHotkeyInput(EHotkey hotkey)
    {
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{hotkey.ToFriendlyName()} Hotkey INPUT", PlayerData.Hotkeys[(int)hotkey].ToString());
    }

    public void ShowLoadouts()
    {
        MainPage = EMainPage.LOADOUT;

        if (!LoadoutPages.TryGetValue(1, out var firstPage))
        {
            Logging.Debug($"Error finding first page of loadouts for {Player.CharacterName}");
            LoadoutPageID = 0;
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Next BUTTON", false);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Previous Button", false);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Page TEXT", "");
            return;
        }

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Next BUTTON", true);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Previous Button", true);
        ShowLoadoutPage(firstPage);
        SelectedLoadout(0);
    }

    public void ForwardLoadoutPage()
    {
        if (LoadoutPageID == 0)
            return;

        if (!LoadoutPages.TryGetValue(LoadoutPageID + 1, out var nextPage) && !LoadoutPages.TryGetValue(1, out nextPage))
        {
            ShowLoadouts();
            return;
        }

        ShowLoadoutPage(nextPage);
    }

    public void BackwardLoadoutPage()
    {
        if (LoadoutPageID == 0)
            return;

        if (!LoadoutPages.TryGetValue(LoadoutPageID - 1, out var prevPage) && !LoadoutPages.TryGetValue(LoadoutPages.Keys.Max(), out prevPage))
        {
            ShowLoadouts();
            return;
        }

        ShowLoadoutPage(prevPage);
    }

    public void ReloadLoadoutPage()
    {
        if (!LoadoutPages.TryGetValue(LoadoutPageID, out var page))
        {
            Logging.Debug($"Error finding current loadout page with page id {LoadoutPageID} for {Player.CharacterName}");
            return;
        }

        ShowLoadoutPage(page);
    }

    public void ShowLoadoutPage(PageLoadout page)
    {
        LoadoutPageID = page.PageID;

        for (var i = 0; i <= MAX_LOADOUTS_PER_PAGE; i++)
        {
            if (!page.Loadouts.TryGetValue(i, out var loadout))
            {
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout BUTTON {i}", false);
                continue;
            }

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout BUTTON {i}", true);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout TEXT {i}", loadout.LoadoutName);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Equipped {i}", loadout.IsActive);
        }
    }

    public void ReloadLoadout()
    {
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var currentLoadout))
        {
            Logging.Debug($"Couldnt find the current selected loadout");
            return;
        }

        ShowLoadout(currentLoadout);
    }

    public void SelectedLoadout(int selected)
    {
        if (!LoadoutPages.TryGetValue(LoadoutPageID, out var currentPage))
        {
            Logging.Debug($"Couldnt find the current selected page at {LoadoutPageID}");
            return;
        }

        if (!currentPage.Loadouts.TryGetValue(selected, out var currentLoadout))
        {
            Logging.Debug($"Couldnt find the selected loadout at {selected}");
            return;
        }

        ShowLoadout(currentLoadout);
    }

    public void ShowLoadout(Loadout loadout)
    {
        LoadoutID = loadout.LoadoutID;

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Equip BUTTON", !loadout.IsActive);
        // Primary
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Primary IMAGE", loadout.PrimarySkin?.IconLink ?? loadout.Primary?.Gun?.IconLink ?? "");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Primary TEXT", loadout.Primary?.Gun?.GunName ?? "");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Primary Level TEXT", loadout.Primary?.Level.ToString() ?? "");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Primary {loadout.Primary?.Gun?.GunRarity.ToString() ?? "DEFAULT"}", true);
        for (var i = 0; i <= 3; i++)
        {
            var attachmentType = (EAttachment)i;
            _ = loadout.PrimaryAttachments.TryGetValue(attachmentType, out var attachment);
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Primary {attachmentType.ToUIName()} IMAGE", attachment?.Attachment?.IconLink ?? Utility.GetDefaultAttachmentImage(attachmentType.ToString()));
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Primary {attachmentType.ToUIName()} {attachment?.Attachment?.AttachmentRarity.ToString() ?? "COMMON"}", true);
        }

        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Primary Charm IMAGE", loadout.PrimaryGunCharm?.GunCharm?.IconLink ?? Utility.GetDefaultAttachmentImage("charm"));
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Primary Charm {loadout.PrimaryGunCharm?.GunCharm?.CharmRarity.ToString() ?? "COMMON"}", true);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Primary Skin IMAGE", loadout.PrimarySkin?.PatternLink ?? Utility.GetDefaultAttachmentImage("skin"));
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Primary Skin {loadout.PrimarySkin?.SkinRarity.ToString() ?? "COMMON"}", true);
        
        // Secondary
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Secondary IMAGE", loadout.SecondarySkin?.IconLink ?? loadout.Secondary?.Gun?.IconLink ?? "");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Secondary TEXT", loadout.Secondary?.Gun?.GunName ?? "");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Secondary Level TEXT", loadout.Secondary?.Level.ToString() ?? "");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Secondary {loadout.Secondary?.Gun?.GunRarity.ToString() ?? "DEFAULT"}", true);
        for (var i = 0; i <= 3; i++)
        {
            var attachmentType = (EAttachment)i;
            _ = loadout.SecondaryAttachments.TryGetValue(attachmentType, out var attachment);
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Secondary {attachmentType.ToUIName()} IMAGE", attachment?.Attachment?.IconLink ?? Utility.GetDefaultAttachmentImage(attachmentType.ToString()));
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Secondary {attachmentType.ToUIName()} {attachment?.Attachment?.AttachmentRarity.ToString() ?? "COMMON"}", true);
        }

        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Secondary Charm IMAGE", loadout.SecondaryGunCharm?.GunCharm?.IconLink ?? Utility.GetDefaultAttachmentImage("charm"));
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Secondary Charm {loadout.SecondaryGunCharm?.GunCharm?.CharmRarity.ToString() ?? "COMMON"}", true);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Secondary Skin IMAGE", loadout.SecondarySkin?.PatternLink ?? Utility.GetDefaultAttachmentImage("skin"));
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Secondary Skin {loadout.SecondarySkin?.SkinRarity.ToString() ?? "COMMON"}", true);
        
        // Knife
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Knife IMAGE", loadout.Knife?.Knife?.IconLink ?? "");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Knife TEXT", loadout.Knife?.Knife?.KnifeName ?? "");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Knife {loadout.Knife?.Knife?.KnifeRarity.ToString() ?? "DEFAULT"}", true);

        // Tactical
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Tactical IMAGE", loadout.Tactical?.Gadget?.IconLink ?? "");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Tactical TEXT", loadout.Tactical?.Gadget?.GadgetName ?? "");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Tactical {loadout.Tactical?.Gadget?.GadgetRarity.ToString() ?? "DEFAULT"}", true);

        // Lethal
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Lethal IMAGE", loadout.Lethal?.Gadget?.IconLink ?? "");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Lethal TEXT", loadout.Lethal?.Gadget?.IconLink ?? "");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Lethal {loadout.Lethal?.Gadget?.GadgetRarity.ToString() ?? "DEFAULT"}", true);

        // Perk
        for (var i = 1; i <= 3; i++)
        {
            var gotPerk = loadout.Perks.TryGetValue(i, out var perk);
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Perk IMAGE {i}", gotPerk ? perk.Perk.IconLink : "");
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Perk TEXT {i}", gotPerk ? perk.Perk.PerkName : "");
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Perk {i} {(gotPerk ? perk.Perk.PerkRarity.ToString() : "DEFAULT")}", true);
        }

        // Killstreak
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Loadout Killstreak DEFAULT", true);
        for (var i = 0; i <= 2; i++)
        {
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Killstreak IMAGE {i}", loadout.Killstreaks.Count < i + 1 ? "" : loadout.Killstreaks[i].Killstreak.IconLink);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Killstreak TEXT {i}", loadout.Killstreaks.Count < i + 1 ? "" : loadout.Killstreaks[i].Killstreak.KillstreakRequired.ToString());

        }

        // Card
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Card IMAGE", loadout.Card?.Card?.IconLink ?? "");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Card TEXT", loadout.Card?.Card?.IconLink ?? "");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Card {loadout.Card?.Card?.CardRarity.ToString() ?? "DEFAULT"}", true);

        // Glove
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Glove IMAGE", loadout.Glove?.Glove?.IconLink ?? "");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Glove TEXT", loadout.Glove?.Glove?.GloveName ?? "");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Loadout Glove {loadout.Glove?.Glove?.GloveRarity.ToString() ?? "DEFAULT"}", true);
    }

    public void EquipLoadout()
    {
        foreach (var activeLoadout in PlayerLoadout.Loadouts.Values.Where(k => k.IsActive))
            DB.UpdatePlayerLoadoutActive(Player.CSteamID, activeLoadout.LoadoutID, false);

        DB.UpdatePlayerLoadoutActive(Player.CSteamID, LoadoutID, true);
        ReloadLoadoutPage();
        ReloadLoadout();
    }

    public void SendLoadoutName(string name) => LoadoutNameText = name;

    public void RenameLoadout()
    {
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
        {
            Logging.Debug($"Error getting current loadout with id {LoadoutID} for {Player.CharacterName}");
            return;
        }

        if (string.IsNullOrEmpty(LoadoutNameText) || LoadoutNameText.Length > 40)
            return;

        var updatedLoadoutName = LoadoutNameText.Replace("SELECT", "").Replace("UPDATE", "").Replace("INSERT", "").Replace("DELETE", "").Replace("TRUNCATE", "").Replace(";", "");
        if (string.IsNullOrEmpty(updatedLoadoutName))
            return;
        
        loadout.LoadoutName = updatedLoadoutName;
        DB.UpdatePlayerLoadout(Player.CSteamID, LoadoutID);
        ReloadLoadoutPage();
    }

    public void ExitRenameLoadout() => LoadoutNameText = "";

    public void ShowMidgameLoadouts()
    {
        MainPage = EMainPage.LOADOUT;

        Player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);
        EffectManager.sendUIEffect(MIDGAME_LOADOUT_ID, MIDGAME_LOADOUT_KEY, TransportConnection, true);
        if (!LoadoutPages.TryGetValue(1, out var firstPage))
        {
            Logging.Debug($"Error finding first page of loadouts midgame for {Player.CharacterName}");
            LoadoutPageID = 0;
            EffectManager.sendUIEffectVisibility(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Next BUTTON", false);
            EffectManager.sendUIEffectVisibility(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Previous Button", false);
            EffectManager.sendUIEffectText(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Page TEXT", "");
            return;
        }

        EffectManager.sendUIEffectVisibility(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Next BUTTON", true);
        EffectManager.sendUIEffectVisibility(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Previous Button", true);
        ShowMidgameLoadoutPage(firstPage);
        SelectedMidgameLoadout(0);
    }

    public void ForwardMidgameLoadoutPage()
    {
        if (LoadoutPageID == 0)
            return;

        if (!LoadoutPages.TryGetValue(LoadoutPageID + 1, out var nextPage) && !LoadoutPages.TryGetValue(1, out nextPage))
        {
            ShowLoadouts();
            return;
        }

        ShowMidgameLoadoutPage(nextPage);
    }

    public void BackwardMidgameLoadoutPage()
    {
        if (LoadoutPageID == 0)
            return;

        if (!LoadoutPages.TryGetValue(LoadoutPageID - 1, out var prevPage) && !LoadoutPages.TryGetValue(LoadoutPages.Keys.Max(), out prevPage))
        {
            ShowLoadouts();
            return;
        }

        ShowMidgameLoadoutPage(prevPage);
    }

    public void ShowMidgameLoadoutPage(PageLoadout page)
    {
        LoadoutPageID = page.PageID;

        for (var i = 0; i <= 9; i++)
        {
            if (!page.Loadouts.TryGetValue(i, out var loadout))
            {
                EffectManager.sendUIEffectVisibility(MIDGAME_LOADOUT_KEY, TransportConnection, true, $"SERVER Loadout BUTTON {i}", false);
                continue;
            }

            EffectManager.sendUIEffectVisibility(MIDGAME_LOADOUT_KEY, TransportConnection, true, $"SERVER Loadout BUTTON {i}", true);
            EffectManager.sendUIEffectText(MIDGAME_LOADOUT_KEY, TransportConnection, true, $"SERVER Loadout TEXT {i}", loadout.LoadoutName);
            EffectManager.sendUIEffectVisibility(MIDGAME_LOADOUT_KEY, TransportConnection, true, $"SERVER Loadout Equipped {i}", loadout.IsActive);
        }
    }

    public void SelectedMidgameLoadout(int selected)
    {
        if (!LoadoutPages.TryGetValue(LoadoutPageID, out var currentPage))
        {
            Logging.Debug($"Couldnt find the current selected page at {LoadoutPageID}");
            return;
        }

        if (!currentPage.Loadouts.TryGetValue(selected, out var currentLoadout))
        {
            Logging.Debug($"Couldnt find the selected loadout at {selected}");
            return;
        }

        ShowMidgameLoadout(currentLoadout);
    }

    public void ShowMidgameLoadout(Loadout loadout)
    {
        LoadoutID = loadout.LoadoutID;

        EffectManager.sendUIEffectVisibility(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Equip BUTTON", !loadout.IsActive);
        // Primary
        EffectManager.sendUIEffectImageURL(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Primary IMAGE", loadout.PrimarySkin == null ? loadout.Primary == null ? "" : loadout.Primary.Gun.IconLink : loadout.PrimarySkin.IconLink);
        EffectManager.sendUIEffectText(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Primary TEXT", loadout.Primary == null ? "" : loadout.Primary.Gun.GunName);
        EffectManager.sendUIEffectText(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Primary Level TEXT", loadout.Primary == null ? "" : loadout.Primary.Level.ToString());
        for (var i = 0; i <= 3; i++)
        {
            var attachmentType = (EAttachment)i;
            _ = loadout.PrimaryAttachments.TryGetValue(attachmentType, out var attachment);
            EffectManager.sendUIEffectImageURL(MIDGAME_LOADOUT_KEY, TransportConnection, true, $"SERVER Loadout Primary {attachmentType.ToUIName()} IMAGE", attachment == null ? Utility.GetDefaultAttachmentImage(attachmentType.ToString()) : attachment.Attachment.IconLink);
        }

        EffectManager.sendUIEffectImageURL(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Primary Charm IMAGE", loadout.PrimaryGunCharm == null ? Utility.GetDefaultAttachmentImage("charm") : loadout.PrimaryGunCharm.GunCharm.IconLink);
        EffectManager.sendUIEffectImageURL(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Primary Skin IMAGE", loadout.PrimarySkin == null ? Utility.GetDefaultAttachmentImage("skin") : loadout.PrimarySkin.PatternLink);

        // Secondary
        EffectManager.sendUIEffectImageURL(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Secondary IMAGE", loadout.SecondarySkin == null ? loadout.Secondary == null ? "" : loadout.Secondary.Gun.IconLink : loadout.SecondarySkin.IconLink);
        EffectManager.sendUIEffectText(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Secondary TEXT", loadout.Secondary == null ? "" : loadout.Secondary.Gun.GunName);
        EffectManager.sendUIEffectText(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Secondary Level TEXT", loadout.Secondary == null ? "" : loadout.Secondary.Level.ToString());
        for (var i = 0; i <= 3; i++)
        {
            var attachmentType = (EAttachment)i;
            _ = loadout.SecondaryAttachments.TryGetValue(attachmentType, out var attachment);
            EffectManager.sendUIEffectImageURL(MIDGAME_LOADOUT_KEY, TransportConnection, true, $"SERVER Loadout Secondary {attachmentType.ToUIName()} IMAGE", attachment == null ? Utility.GetDefaultAttachmentImage(attachmentType.ToString()) : attachment.Attachment.IconLink);
        }

        EffectManager.sendUIEffectImageURL(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Secondary Charm IMAGE", loadout.SecondaryGunCharm == null ? Utility.GetDefaultAttachmentImage("charm") : loadout.SecondaryGunCharm.GunCharm.IconLink);
        EffectManager.sendUIEffectImageURL(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Secondary Skin IMAGE", loadout.SecondarySkin == null ? Utility.GetDefaultAttachmentImage("skin") : loadout.SecondarySkin.PatternLink);

        // Knife
        EffectManager.sendUIEffectImageURL(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Knife IMAGE", loadout.Knife == null ? "" : loadout.Knife.Knife.IconLink);
        EffectManager.sendUIEffectText(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Knife TEXT", loadout.Knife == null ? "" : loadout.Knife.Knife.KnifeName);

        // Tactical
        EffectManager.sendUIEffectImageURL(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Tactical IMAGE", loadout.Tactical == null ? "" : loadout.Tactical.Gadget.IconLink);
        EffectManager.sendUIEffectText(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Tactical TEXT", loadout.Tactical == null ? "" : loadout.Tactical.Gadget.GadgetName);

        // Lethal
        EffectManager.sendUIEffectImageURL(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Lethal IMAGE", loadout.Lethal == null ? "" : loadout.Lethal.Gadget.IconLink);
        EffectManager.sendUIEffectText(MIDGAME_LOADOUT_KEY, TransportConnection, true, "SERVER Loadout Lethal TEXT", loadout.Lethal == null ? "" : loadout.Lethal.Gadget.GadgetName);

        // Perk
        for (var i = 1; i <= 3; i++)
        {
            _ = loadout.Perks.TryGetValue(i, out var perk);
            EffectManager.sendUIEffectImageURL(MIDGAME_LOADOUT_KEY, TransportConnection, true, $"SERVER Loadout Perk IMAGE {i}", perk == null ? "" : loadout.Perks[i].Perk.IconLink);
            EffectManager.sendUIEffectText(MIDGAME_LOADOUT_KEY, TransportConnection, true, $"SERVER Loadout Perk TEXT {i}", perk == null ? "" : loadout.Perks[i].Perk.PerkName);
        }

        // Killstreak
        for (var i = 0; i <= 2; i++)
        {
            EffectManager.sendUIEffectImageURL(MIDGAME_LOADOUT_KEY, TransportConnection, true, $"SERVER Loadout Killstreak IMAGE {i}", loadout.Killstreaks.Count < i + 1 ? "" : loadout.Killstreaks[i].Killstreak.IconLink);
            EffectManager.sendUIEffectText(MIDGAME_LOADOUT_KEY, TransportConnection, true, $"SERVER Loadout Killstreak TEXT {i}", loadout.Killstreaks.Count < i + 1 ? "" : loadout.Killstreaks[i].Killstreak.KillstreakRequired.ToString());
        }

        // Card
        EffectManager.sendUIEffectImageURL(MIDGAME_LOADOUT_KEY, TransportConnection, true, $"SERVER Loadout Card IMAGE", loadout.Card == null ? "" : loadout.Card.Card.IconLink);
        EffectManager.sendUIEffectText(MIDGAME_LOADOUT_KEY, TransportConnection, true, $"SERVER Loadout Card TEXT", loadout.Card == null ? "" : loadout.Card.Card.CardName);

        // Glove
        EffectManager.sendUIEffectImageURL(MIDGAME_LOADOUT_KEY, TransportConnection, true, $"SERVER Loadout Glove IMAGE", loadout.Glove == null ? "" : loadout.Glove.Glove.IconLink);
        EffectManager.sendUIEffectText(MIDGAME_LOADOUT_KEY, TransportConnection, true, $"SERVER Loadout Glove TEXT", loadout.Glove == null ? "" : loadout.Glove.Glove.GloveName);
    }

    public void EquipMidgameLoadout()
    {
        foreach (var activeLoadout in PlayerLoadout.Loadouts.Values.Where(k => k.IsActive))
            DB.UpdatePlayerLoadoutActive(Player.CSteamID, activeLoadout.LoadoutID, false);

        DB.UpdatePlayerLoadoutActive(Player.CSteamID, LoadoutID, true);
        ClearMidgameLoadouts();
        var gPlayer = Plugin.Instance.Game.GetGamePlayer(Player);
        if (gPlayer != null)
            gPlayer.IsPendingLoadoutChange = true;
    }

    public void ClearMidgameLoadouts()
    {
        var gPlayer = Plugin.Instance.Game.GetGamePlayer(Player);
        if (gPlayer != null)
            gPlayer.HasMidgameLoadout = false;

        EffectManager.askEffectClearByID(MIDGAME_LOADOUT_ID, TransportConnection);
        Player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
    }

    public void ShowLoadoutSubPage(ELoadoutPage page)
    {
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Type TEXT", page.ToFriendlyName());
        LoadoutPage = page;

        switch (LoadoutPage)
        {
            case ELoadoutPage.PRIMARY:
                ShowLoadoutTab(ELoadoutTab.ASSAULT_RIFLES);
                break;
            case ELoadoutPage.SECONDARY:
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

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Next BUTTON", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Previous BUTTON", false);

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Page TEXT", "");
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
        {
            Logging.Debug($"Error finding current loadout with id {LoadoutID} for {Player.CharacterName}");
            return;
        }

        for (var i = 0; i <= MAX_ITEMS_PER_PAGE; i++)
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item BUTTON {i}", false);

        for (var i = 0; i <= MAX_ITEMS_PER_GRID; i++)
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", false);

        switch (LoadoutTab)
        {
            case ELoadoutTab.ALL:
            {
                switch (LoadoutPage)
                {
                    case ELoadoutPage.PRIMARY_SKIN:
                    {
                        if (!GunSkinPages.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out var gunSkinPages))
                        {
                            Logging.Debug($"Error getting gun skin pages for gun with id {loadout.Primary?.Gun?.GunID ?? 0}");
                            return;
                        }

                        if (!gunSkinPages.TryGetValue(1, out var firstPage))
                        {
                            Logging.Debug($"Error finding the first page for gun skins for gun with id {loadout.Primary.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Next BUTTON", gunSkinPages.Count > 1);
                        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Previous BUTTON", gunSkinPages.Count > 1);
                        ShowGunSkinPage(firstPage);
                        break;
                    }

                    case ELoadoutPage.SECONDARY_SKIN:
                    {
                        if (!GunSkinPages.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out var gunSkinPages))
                        {
                            Logging.Debug($"Error getting gun skin pages for gun with id {loadout.Secondary?.Gun?.GunID ?? 0}");
                            return;
                        }

                        if (!gunSkinPages.TryGetValue(1, out var firstPage))
                        {
                            Logging.Debug($"Error finding the first page for gun skins for gun with id {loadout.Secondary.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Next BUTTON", gunSkinPages.Count > 1);
                        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Previous BUTTON", gunSkinPages.Count > 1);
                        ShowGunSkinPage(firstPage);
                        break;
                    }

                    case ELoadoutPage.ATTACHMENT_PRIMARY_BARREL:
                    case ELoadoutPage.ATTACHMENT_PRIMARY_GRIP:
                    case ELoadoutPage.ATTACHMENT_PRIMARY_MAGAZINE:
                    case ELoadoutPage.ATTACHMENT_PRIMARY_SIGHTS:
                    {
                        if (!Enum.TryParse(GetAttachmentPage(), false, out EAttachment attachmentType))
                        {
                            Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                            return;
                        }

                        if (!PlayerLoadout.Guns.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out var gun))
                        {
                            Logging.Debug($"Error finding primary that has been selected with id {loadout.Primary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                            return;
                        }

                        if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                        {
                            Logging.Debug($"Error finding primary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentTypePages.TryGetValue(attachmentType, out var attachmentPages))
                        {
                            Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentPages.TryGetValue(1, out var firstPage))
                        {
                            Logging.Debug($"Error finding first page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        ShowAttachmentPage(firstPage, gun);
                        break;
                    }

                    case ELoadoutPage.ATTACHMENT_PRIMARY_CHARM:
                    case ELoadoutPage.ATTACHMENT_SECONDARY_CHARM:
                    {
                        if (!GunCharmPages.TryGetValue(1, out var firstPage))
                        {
                            Logging.Debug($"Error getting first page for gun charms for {Player.CharacterName}");
                            return;
                        }

                        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Next BUTTON", GunCharmPages.Count > 1);
                        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Previous BUTTON", GunCharmPages.Count > 1);

                        ShowGunCharmPage(firstPage);
                        break;
                    }

                    case ELoadoutPage.ATTACHMENT_SECONDARY_BARREL:
                    case ELoadoutPage.ATTACHMENT_SECONDARY_MAGAZINE:
                    case ELoadoutPage.ATTACHMENT_SECONDARY_SIGHTS:
                    {
                        if (!Enum.TryParse(GetAttachmentPage(), false, out EAttachment attachmentType))
                        {
                            Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                            return;
                        }

                        if (!PlayerLoadout.Guns.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out var gun))
                        {
                            Logging.Debug($"Error finding secondary that has been selected with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                            return;
                        }

                        if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                        {
                            Logging.Debug($"Error finding secondary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentTypePages.TryGetValue(attachmentType, out var attachmentPages))
                        {
                            Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentPages.TryGetValue(1, out var firstPage))
                        {
                            Logging.Debug($"Error finding first page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        ShowAttachmentPage(firstPage, gun);
                        break;
                    }

                    case ELoadoutPage.PERK1:
                    case ELoadoutPage.PERK2:
                    case ELoadoutPage.PERK3:
                    {
                        if (!int.TryParse(GetPerkInt(), out var perkType))
                        {
                            Logging.Debug($"Error getting perk type from {LoadoutPage}");
                            return;
                        }

                        if (!PerkPages[perkType].TryGetValue(1, out var firstPage))
                        {
                            Logging.Debug($"Error getting first page for perks for {Player.CharacterName}");
                            return;
                        }

                        ShowPerkPage(firstPage);
                        break;
                    }

                    case ELoadoutPage.LETHAL:
                    {
                        if (!LethalPages.TryGetValue(1, out var firstPage))
                        {
                            Logging.Debug($"Error finding the first page for lethals for {Player.CharacterName}");
                            return;
                        }

                        ShowGadgetPage(firstPage);
                        break;
                    }

                    case ELoadoutPage.TACTICAL:
                    {
                        if (!TacticalPages.TryGetValue(1, out var firstPage))
                        {
                            Logging.Debug($"Error finding the first page for tacticals for {Player.CharacterName}");
                            return;
                        }

                        ShowGadgetPage(firstPage);
                        break;
                    }

                    case ELoadoutPage.KNIFE:
                    {
                        if (!KnifePages.TryGetValue(1, out var firstPage))
                        {
                            Logging.Debug($"Error finding the first page for knives for {Player.CharacterName}");
                            return;
                        }

                        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Next BUTTON", KnifePages.Count > 1);
                        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Previous BUTTON", KnifePages.Count > 1);
                        ShowKnifePage(firstPage);
                        break;
                    }

                    case ELoadoutPage.KILLSTREAK:
                    {
                        if (!KillstreakPages.TryGetValue(1, out var firstPage))
                        {
                            Logging.Debug($"Error finding the first page for killstreaks for {Player.CharacterName}");
                            return;
                        }

                        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Next BUTTON", KillstreakPages.Count > 1);
                        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Previous BUTTON", KillstreakPages.Count > 1);

                        ShowKillstreakPage(firstPage);
                        break;
                    }

                    case ELoadoutPage.GLOVE:
                    {
                        if (!GlovePages.TryGetValue(1, out var firstPage))
                        {
                            Logging.Debug($"Error finding the first page for gloves for {Player.CharacterName}");
                            return;
                        }

                        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Next BUTTON", GlovePages.Count > 1);
                        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Previous BUTTON", GlovePages.Count > 1);
                        ShowGlovePage(firstPage);
                        break;
                    }

                    case ELoadoutPage.CARD:
                    {
                        if (!CardPages.TryGetValue(1, out var firstPage))
                        {
                            Logging.Debug($"Error finding the first page for cards for {Player.CharacterName}");
                            return;
                        }

                        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Next BUTTON", CardPages.Count > 1);
                        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Previous BUTTON", CardPages.Count > 1);
                        ShowCardPage(firstPage);
                        break;
                    }
                }

                break;
            }

            case ELoadoutTab.PISTOLS:
            {
                if (!PistolPages.TryGetValue(1, out var firstPage))
                {
                    Logging.Debug($"Error finding first page for pistols for {Player.CharacterName}");
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Page TEXT", "");
                    return;
                }

                ShowGunPage(firstPage);
                break;
            }

            case ELoadoutTab.SUBMACHINE_GUNS:
            {
                if (!SMGPages.TryGetValue(1, out var firstPage))
                {
                    Logging.Debug($"Error finding first page for smgs for {Player.CharacterName}");
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Page TEXT", "");
                    return;
                }

                ShowGunPage(firstPage);
                break;
            }

            case ELoadoutTab.SHOTGUNS:
            {
                if (!ShotgunPages.TryGetValue(1, out var firstPage))
                {
                    Logging.Debug($"Error finding first page for shotguns for {Player.CharacterName}");
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Page TEXT", "");
                    return;
                }

                ShowGunPage(firstPage);
                break;
            }

            case ELoadoutTab.LIGHT_MACHINE_GUNS:
            {
                if (!LMGPages.TryGetValue(1, out var firstPage))
                {
                    Logging.Debug($"Error finding first page for lmgs for {Player.CharacterName}");
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Page TEXT", "");
                    return;
                }

                ShowGunPage(firstPage);
                break;
            }

            case ELoadoutTab.ASSAULT_RIFLES:
            {
                if (!ARPages.TryGetValue(1, out var firstPage))
                {
                    Logging.Debug($"Error finding first page for ARs for {Player.CharacterName}");
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Page TEXT", "");
                    return;
                }

                ShowGunPage(firstPage);
                break;
            }

            case ELoadoutTab.SNIPER_RIFLES:
            {
                if (!SniperPages.TryGetValue(1, out var firstPage))
                {
                    Logging.Debug($"Error finding first page for snipers for {Player.CharacterName}");
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Page TEXT", "");
                    return;
                }

                ShowGunPage(firstPage);
                break;
            }

            case ELoadoutTab.CARBINES:
            {
                if (!CarbinePages.TryGetValue(1, out var firstPage))
                {
                    Logging.Debug($"Error finding first page for carbines for {Player.CharacterName}");
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Page TEXT", "");
                    return;
                }

                ShowGunPage(firstPage);
                break;
            }
        }
    }

    public void ForwardLoadoutTab()
    {
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
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
                    case ELoadoutPage.PRIMARY_SKIN:
                    {
                        if (!GunSkinPages.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out var gunSkinPages))
                        {
                            Logging.Debug($"Error getting gun skin pages for gun with id {loadout.Primary?.Gun?.GunID ?? 0}");
                            return;
                        }

                        if (!gunSkinPages.TryGetValue(LoadoutTabPageID + 1, out var nextPage) && !gunSkinPages.TryGetValue(1, out nextPage))
                        {
                            Logging.Debug($"Error finding the next page for gun skins for gun with id {loadout.Primary.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGunSkinPage(nextPage);
                        break;
                    }

                    case ELoadoutPage.SECONDARY_SKIN:
                    {
                        if (!GunSkinPages.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out var gunSkinPages))
                        {
                            Logging.Debug($"Error getting gun skin pages for gun with id {loadout.Secondary?.Gun?.GunID ?? 0}");
                            return;
                        }

                        if (!gunSkinPages.TryGetValue(LoadoutTabPageID + 1, out var nextPage) && !gunSkinPages.TryGetValue(1, out nextPage))
                        {
                            Logging.Debug($"Error finding the next page for gun skins for gun with id {loadout.Secondary.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGunSkinPage(nextPage);
                        break;
                    }

                    case ELoadoutPage.ATTACHMENT_PRIMARY_BARREL:
                    case ELoadoutPage.ATTACHMENT_PRIMARY_GRIP:
                    case ELoadoutPage.ATTACHMENT_PRIMARY_MAGAZINE:
                    case ELoadoutPage.ATTACHMENT_PRIMARY_SIGHTS:
                    {
                        if (!Enum.TryParse(GetAttachmentPage(), false, out EAttachment attachmentType))
                        {
                            Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                            return;
                        }

                        if (!PlayerLoadout.Guns.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out var gun))
                        {
                            Logging.Debug($"Error finding primary that has been selected with id {loadout.Primary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                            return;
                        }

                        if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                        {
                            Logging.Debug($"Error finding primary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentTypePages.TryGetValue(attachmentType, out var attachmentPages))
                        {
                            Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentPages.TryGetValue(LoadoutTabPageID + 1, out var nextPage) && !attachmentPages.TryGetValue(1, out nextPage))
                        {
                            Logging.Debug($"Error finding next page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        ShowAttachmentPage(nextPage, gun);
                        break;
                    }

                    case ELoadoutPage.ATTACHMENT_PRIMARY_CHARM:
                    case ELoadoutPage.ATTACHMENT_SECONDARY_CHARM:
                    {
                        if (!GunCharmPages.TryGetValue(LoadoutTabPageID + 1, out var nextPage) && !GunCharmPages.TryGetValue(1, out nextPage))
                        {
                            Logging.Debug($"Error getting next page for gun charms for {Player.CharacterName}");
                            return;
                        }

                        ShowGunCharmPage(nextPage);
                        break;
                    }

                    case ELoadoutPage.ATTACHMENT_SECONDARY_BARREL:
                    case ELoadoutPage.ATTACHMENT_SECONDARY_MAGAZINE:
                    case ELoadoutPage.ATTACHMENT_SECONDARY_SIGHTS:
                    {
                        if (!Enum.TryParse(GetAttachmentPage(), false, out EAttachment attachmentType))
                        {
                            Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                            return;
                        }

                        if (!PlayerLoadout.Guns.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out var gun))
                        {
                            Logging.Debug($"Error finding secondary that has been selected with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                            return;
                        }

                        if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                        {
                            Logging.Debug($"Error finding secondary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentTypePages.TryGetValue(attachmentType, out var attachmentPages))
                        {
                            Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentPages.TryGetValue(LoadoutTabPageID + 1, out var nextPage) && !attachmentPages.TryGetValue(1, out nextPage))
                        {
                            Logging.Debug($"Error finding next page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        ShowAttachmentPage(nextPage, gun);
                        break;
                    }

                    case ELoadoutPage.PERK1:
                    case ELoadoutPage.PERK2:
                    case ELoadoutPage.PERK3:
                    {
                        if (!int.TryParse(GetPerkInt(), out var perkType))
                        {
                            Logging.Debug($"Error getting perk type from {LoadoutPage}");
                            return;
                        }

                        if (!PerkPages[perkType].TryGetValue(LoadoutTabPageID + 1, out var nextPage) && !PerkPages[perkType].TryGetValue(1, out nextPage))
                        {
                            Logging.Debug($"Error getting next page for perks for {Player.CharacterName}");
                            return;
                        }

                        ShowPerkPage(nextPage);
                        break;
                    }

                    case ELoadoutPage.LETHAL:
                    {
                        if (!LethalPages.TryGetValue(LoadoutTabPageID + 1, out var nextPage) && !LethalPages.TryGetValue(1, out nextPage))
                        {
                            Logging.Debug($"Error finding the next page for lethals for {Player.CharacterName}");
                            return;
                        }

                        ShowGadgetPage(nextPage);
                        break;
                    }

                    case ELoadoutPage.TACTICAL:
                    {
                        if (!TacticalPages.TryGetValue(LoadoutTabPageID + 1, out var nextPage) && !TacticalPages.TryGetValue(1, out nextPage))
                        {
                            Logging.Debug($"Error finding the next page for tacticals for {Player.CharacterName}");
                            return;
                        }

                        ShowGadgetPage(nextPage);
                        break;
                    }

                    case ELoadoutPage.KNIFE:
                    {
                        if (!KnifePages.TryGetValue(LoadoutTabPageID + 1, out var nextPage) && !KnifePages.TryGetValue(1, out nextPage))
                        {
                            Logging.Debug($"Error finding the next page for knives for {Player.CharacterName}");
                            return;
                        }

                        ShowKnifePage(nextPage);
                        break;
                    }

                    case ELoadoutPage.KILLSTREAK:
                    {
                        if (!KillstreakPages.TryGetValue(LoadoutTabPageID + 1, out var nextPage) && !KillstreakPages.TryGetValue(1, out nextPage))
                        {
                            Logging.Debug($"Error finding the next page for killstreaks for {Player.CharacterName}");
                            return;
                        }

                        ShowKillstreakPage(nextPage);
                        break;
                    }

                    case ELoadoutPage.GLOVE:
                    {
                        if (!GlovePages.TryGetValue(LoadoutTabPageID + 1, out var nextPage) && !GlovePages.TryGetValue(1, out nextPage))
                        {
                            Logging.Debug($"Error finding the next page for gloves for {Player.CharacterName}");
                            return;
                        }

                        ShowGlovePage(nextPage);
                        break;
                    }

                    case ELoadoutPage.CARD:
                    {
                        if (!CardPages.TryGetValue(LoadoutTabPageID + 1, out var nextPage) && !CardPages.TryGetValue(1, out nextPage))
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
                if (!PistolPages.TryGetValue(LoadoutTabPageID + 1, out var nextPage) && !PistolPages.TryGetValue(1, out nextPage))
                {
                    Logging.Debug($"Error finding next page for pistols for {Player.CharacterName}");
                    return;
                }

                ShowGunPage(nextPage);
                break;
            }

            case ELoadoutTab.SUBMACHINE_GUNS:
            {
                if (!SMGPages.TryGetValue(LoadoutTabPageID + 1, out var nextPage) && !SMGPages.TryGetValue(1, out nextPage))
                {
                    Logging.Debug($"Error finding next page for smgs for {Player.CharacterName}");
                    return;
                }

                ShowGunPage(nextPage);
                break;
            }

            case ELoadoutTab.SHOTGUNS:
            {
                if (!ShotgunPages.TryGetValue(LoadoutTabPageID + 1, out var nextPage) && !ShotgunPages.TryGetValue(1, out nextPage))
                {
                    Logging.Debug($"Error finding next page for shotguns for {Player.CharacterName}");
                    return;
                }

                ShowGunPage(nextPage);
                break;
            }

            case ELoadoutTab.LIGHT_MACHINE_GUNS:
            {
                if (!LMGPages.TryGetValue(LoadoutTabPageID + 1, out var nextPage) && !LMGPages.TryGetValue(1, out nextPage))
                {
                    Logging.Debug($"Error finding next page for lmgs for {Player.CharacterName}");
                    return;
                }

                ShowGunPage(nextPage);
                break;
            }

            case ELoadoutTab.ASSAULT_RIFLES:
            {
                if (!ARPages.TryGetValue(LoadoutTabPageID + 1, out var nextPage) && !ARPages.TryGetValue(1, out nextPage))
                {
                    Logging.Debug($"Error finding next page for ARs for {Player.CharacterName}");
                    return;
                }

                ShowGunPage(nextPage);
                break;
            }

            case ELoadoutTab.SNIPER_RIFLES:
            {
                if (!SniperPages.TryGetValue(LoadoutTabPageID + 1, out var nextPage) && !SniperPages.TryGetValue(1, out nextPage))
                {
                    Logging.Debug($"Error finding next page for snipers for {Player.CharacterName}");
                    return;
                }

                ShowGunPage(nextPage);
                break;
            }

            case ELoadoutTab.CARBINES:
            {
                if (!CarbinePages.TryGetValue(LoadoutTabPageID + 1, out var nextPage) && !CarbinePages.TryGetValue(1, out nextPage))
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
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
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
                    case ELoadoutPage.PRIMARY_SKIN:
                    {
                        if (!GunSkinPages.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out var gunSkinPages))
                        {
                            Logging.Debug($"Error getting gun skin pages for gun with id {loadout.Primary?.Gun?.GunID ?? 0}");
                            return;
                        }

                        if (!gunSkinPages.TryGetValue(LoadoutTabPageID - 1, out var prevPage) && !gunSkinPages.TryGetValue(gunSkinPages.Keys.Max(), out prevPage))
                        {
                            Logging.Debug($"Error finding the prev page for gun skins for gun with id {loadout.Primary.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGunSkinPage(prevPage);
                        break;
                    }

                    case ELoadoutPage.SECONDARY_SKIN:
                    {
                        if (!GunSkinPages.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out var gunSkinPages))
                        {
                            Logging.Debug($"Error getting gun skin pages for gun with id {loadout.Secondary?.Gun?.GunID ?? 0}");
                            return;
                        }

                        if (!gunSkinPages.TryGetValue(LoadoutTabPageID - 1, out var prevPage) && !gunSkinPages.TryGetValue(gunSkinPages.Keys.Max(), out prevPage))
                        {
                            Logging.Debug($"Error finding the prev page for gun skins for gun with id {loadout.Secondary.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGunSkinPage(prevPage);
                        break;
                    }

                    case ELoadoutPage.ATTACHMENT_PRIMARY_BARREL:
                    case ELoadoutPage.ATTACHMENT_PRIMARY_GRIP:
                    case ELoadoutPage.ATTACHMENT_PRIMARY_MAGAZINE:
                    case ELoadoutPage.ATTACHMENT_PRIMARY_SIGHTS:
                    {
                        if (!Enum.TryParse(GetAttachmentPage(), false, out EAttachment attachmentType))
                        {
                            Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                            return;
                        }

                        if (!PlayerLoadout.Guns.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out var gun))
                        {
                            Logging.Debug($"Error finding primary that has been selected with id {loadout.Primary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                            return;
                        }

                        if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                        {
                            Logging.Debug($"Error finding primary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentTypePages.TryGetValue(attachmentType, out var attachmentPages))
                        {
                            Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentPages.TryGetValue(LoadoutTabPageID - 1, out var prevPage) && !attachmentPages.TryGetValue(attachmentPages.Keys.Max(), out prevPage))
                        {
                            Logging.Debug($"Error finding previous page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        ShowAttachmentPage(prevPage, gun);
                        break;
                    }

                    case ELoadoutPage.ATTACHMENT_PRIMARY_CHARM:
                    case ELoadoutPage.ATTACHMENT_SECONDARY_CHARM:
                    {
                        if (!GunCharmPages.TryGetValue(LoadoutTabPageID - 1, out var prevPage) && !GunCharmPages.TryGetValue(GunCharmPages.Keys.Max(), out prevPage))
                        {
                            Logging.Debug($"Error getting prev page for gun charms for {Player.CharacterName}");
                            return;
                        }

                        ShowGunCharmPage(prevPage);
                        break;
                    }

                    case ELoadoutPage.ATTACHMENT_SECONDARY_BARREL:
                    case ELoadoutPage.ATTACHMENT_SECONDARY_MAGAZINE:
                    case ELoadoutPage.ATTACHMENT_SECONDARY_SIGHTS:
                    {
                        if (!Enum.TryParse(GetAttachmentPage(), false, out EAttachment attachmentType))
                        {
                            Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                            return;
                        }

                        if (!PlayerLoadout.Guns.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out var gun))
                        {
                            Logging.Debug($"Error finding Secondary that has been selected with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                            return;
                        }

                        if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                        {
                            Logging.Debug($"Error finding Secondary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentTypePages.TryGetValue(attachmentType, out var attachmentPages))
                        {
                            Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentPages.TryGetValue(LoadoutTabPageID - 1, out var prevPage) && !attachmentPages.TryGetValue(attachmentPages.Keys.Max(), out prevPage))
                        {
                            Logging.Debug($"Error finding previous page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        ShowAttachmentPage(prevPage, gun);
                        break;
                    }

                    case ELoadoutPage.PERK1:
                    case ELoadoutPage.PERK2:
                    case ELoadoutPage.PERK3:
                    {
                        if (!int.TryParse(GetPerkInt(), out var perkType))
                        {
                            Logging.Debug($"Error getting perk type from {LoadoutPage}");
                            return;
                        }

                        if (!PerkPages[perkType].TryGetValue(LoadoutTabPageID - 1, out var prevPage) && !PerkPages[perkType].TryGetValue(PerkPages.Keys.Max(), out prevPage))
                        {
                            Logging.Debug($"Error getting prev page for perks for {Player.CharacterName}");
                            return;
                        }

                        ShowPerkPage(prevPage);
                        break;
                    }

                    case ELoadoutPage.LETHAL:
                    {
                        if (!LethalPages.TryGetValue(LoadoutTabPageID - 1, out var prevPage) && !LethalPages.TryGetValue(LethalPages.Keys.Max(), out prevPage))
                        {
                            Logging.Debug($"Error finding the prev page for lethals for {Player.CharacterName}");
                            return;
                        }

                        ShowGadgetPage(prevPage);
                        break;
                    }

                    case ELoadoutPage.TACTICAL:
                    {
                        if (!TacticalPages.TryGetValue(LoadoutTabPageID - 1, out var prevPage) && !TacticalPages.TryGetValue(TacticalPages.Keys.Max(), out prevPage))
                        {
                            Logging.Debug($"Error finding the prev page for tacticals for {Player.CharacterName}");
                            return;
                        }

                        ShowGadgetPage(prevPage);
                        break;
                    }

                    case ELoadoutPage.KNIFE:
                    {
                        if (!KnifePages.TryGetValue(LoadoutTabPageID - 1, out var prevPage) && !KnifePages.TryGetValue(KnifePages.Keys.Max(), out prevPage))
                        {
                            Logging.Debug($"Error finding the prev page for knives for {Player.CharacterName}");
                            return;
                        }

                        ShowKnifePage(prevPage);
                        break;
                    }

                    case ELoadoutPage.KILLSTREAK:
                    {
                        if (!KillstreakPages.TryGetValue(LoadoutTabPageID - 1, out var prevPage) && !KillstreakPages.TryGetValue(KillstreakPages.Keys.Max(), out prevPage))
                        {
                            Logging.Debug($"Error finding the prev page for killstreaks for {Player.CharacterName}");
                            return;
                        }

                        ShowKillstreakPage(prevPage);
                        break;
                    }

                    case ELoadoutPage.GLOVE:
                    {
                        if (!GlovePages.TryGetValue(LoadoutTabPageID - 1, out var prevPage) && !GlovePages.TryGetValue(GlovePages.Keys.Max(), out prevPage))
                        {
                            Logging.Debug($"Error finding the prev page for gloves for {Player.CharacterName}");
                            return;
                        }

                        ShowGlovePage(prevPage);
                        break;
                    }

                    case ELoadoutPage.CARD:
                    {
                        if (!CardPages.TryGetValue(LoadoutTabPageID - 1, out var prevPage) && !CardPages.TryGetValue(CardPages.Keys.Max(), out prevPage))
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
                if (!PistolPages.TryGetValue(LoadoutTabPageID - 1, out var prevPage) && !PistolPages.TryGetValue(PistolPages.Keys.Max(), out prevPage))
                {
                    Logging.Debug($"Error finding next page for pistols for {Player.CharacterName}");
                    return;
                }

                ShowGunPage(prevPage);
                break;
            }

            case ELoadoutTab.SUBMACHINE_GUNS:
            {
                if (!SMGPages.TryGetValue(LoadoutTabPageID - 1, out var prevPage) && !SMGPages.TryGetValue(SMGPages.Keys.Max(), out prevPage))
                {
                    Logging.Debug($"Error finding next page for smgs for {Player.CharacterName}");
                    return;
                }

                ShowGunPage(prevPage);
                break;
            }

            case ELoadoutTab.SHOTGUNS:
            {
                if (!ShotgunPages.TryGetValue(LoadoutTabPageID - 1, out var prevPage) && !ShotgunPages.TryGetValue(ShotgunPages.Keys.Max(), out prevPage))
                {
                    Logging.Debug($"Error finding next page for shotguns for {Player.CharacterName}");
                    return;
                }

                ShowGunPage(prevPage);
                break;
            }

            case ELoadoutTab.LIGHT_MACHINE_GUNS:
            {
                if (!LMGPages.TryGetValue(LoadoutTabPageID - 1, out var prevPage) && !LMGPages.TryGetValue(LMGPages.Keys.Max(), out prevPage))
                {
                    Logging.Debug($"Error finding next page for lmgs for {Player.CharacterName}");
                    return;
                }

                ShowGunPage(prevPage);
                break;
            }

            case ELoadoutTab.ASSAULT_RIFLES:
            {
                if (!ARPages.TryGetValue(LoadoutTabPageID - 1, out var prevPage) && !ARPages.TryGetValue(ARPages.Keys.Max(), out prevPage))
                {
                    Logging.Debug($"Error finding next page for ARs for {Player.CharacterName}");
                    return;
                }

                ShowGunPage(prevPage);
                break;
            }

            case ELoadoutTab.SNIPER_RIFLES:
            {
                if (!SniperPages.TryGetValue(LoadoutTabPageID - 1, out var prevPage) && !SniperPages.TryGetValue(SniperPages.Keys.Max(), out prevPage))
                {
                    Logging.Debug($"Error finding next page for snipers for {Player.CharacterName}");
                    return;
                }

                ShowGunPage(prevPage);
                break;
            }

            case ELoadoutTab.CARBINES:
            {
                if (!CarbinePages.TryGetValue(LoadoutTabPageID - 1, out var prevPage) && !CarbinePages.TryGetValue(CarbinePages.Keys.Max(), out prevPage))
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
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
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
                    case ELoadoutPage.PRIMARY_SKIN:
                    {
                        if (!GunSkinPages.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out var gunSkinPages))
                        {
                            Logging.Debug($"Error getting gun skin pages for gun with id {loadout.Primary?.Gun?.GunID ?? 0}");
                            return;
                        }

                        if (!gunSkinPages.TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error finding the current page for gun skins for gun with id {loadout.Primary.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGunSkinPage(page);
                        break;
                    }

                    case ELoadoutPage.SECONDARY_SKIN:
                    {
                        if (!GunSkinPages.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out var gunSkinPages))
                        {
                            Logging.Debug($"Error getting gun skin pages for gun with id {loadout.Secondary?.Gun?.GunID ?? 0}");
                            return;
                        }

                        if (!gunSkinPages.TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error finding the current page for gun skins for gun with id {loadout.Secondary.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGunSkinPage(page);
                        break;
                    }
                    case ELoadoutPage.ATTACHMENT_PRIMARY_BARREL:
                    case ELoadoutPage.ATTACHMENT_PRIMARY_GRIP:
                    case ELoadoutPage.ATTACHMENT_PRIMARY_MAGAZINE:
                    case ELoadoutPage.ATTACHMENT_PRIMARY_SIGHTS:
                    {
                        if (!Enum.TryParse(GetAttachmentPage(), false, out EAttachment attachmentType))
                        {
                            Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                            return;
                        }

                        if (!PlayerLoadout.Guns.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out var gun))
                        {
                            Logging.Debug($"Error finding primary that has been selected with id {loadout.Primary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                            return;
                        }

                        if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                        {
                            Logging.Debug($"Error finding primary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentTypePages.TryGetValue(attachmentType, out var attachmentPages))
                        {
                            Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentPages.TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error finding current page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        ShowAttachmentPage(page, gun);
                        break;
                    }

                    case ELoadoutPage.ATTACHMENT_PRIMARY_CHARM:
                    case ELoadoutPage.ATTACHMENT_SECONDARY_CHARM:
                    {
                        if (!GunCharmPages.TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error getting current page for gun charms for {Player.CharacterName}");
                            return;
                        }

                        ShowGunCharmPage(page);
                        break;
                    }

                    case ELoadoutPage.ATTACHMENT_SECONDARY_BARREL:
                    case ELoadoutPage.ATTACHMENT_SECONDARY_MAGAZINE:
                    case ELoadoutPage.ATTACHMENT_SECONDARY_SIGHTS:
                    {
                        if (!Enum.TryParse(GetAttachmentPage(), false, out EAttachment attachmentType))
                        {
                            Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                            return;
                        }

                        if (!PlayerLoadout.Guns.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out var gun))
                        {
                            Logging.Debug($"Error finding secondary that has been selected with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                            return;
                        }

                        if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                        {
                            Logging.Debug($"Error finding secondary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentTypePages.TryGetValue(attachmentType, out var attachmentPages))
                        {
                            Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentPages.TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error finding current page of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        ShowAttachmentPage(page, gun);
                        break;
                    }

                    case ELoadoutPage.PERK1:
                    case ELoadoutPage.PERK2:
                    case ELoadoutPage.PERK3:
                    {
                        if (!int.TryParse(GetPerkInt(), out var perkType))
                        {
                            Logging.Debug($"Error getting perk type from {LoadoutPage}");
                            return;
                        }

                        if (!PerkPages[perkType].TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error getting current page for perks for {Player.CharacterName}");
                            return;
                        }

                        ShowPerkPage(page);
                        break;
                    }

                    case ELoadoutPage.LETHAL:
                    {
                        if (!LethalPages.TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error finding the current page for lethals for {Player.CharacterName}");
                            return;
                        }

                        ShowGadgetPage(page);
                        break;
                    }

                    case ELoadoutPage.TACTICAL:
                    {
                        if (!TacticalPages.TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error finding the current page for tacticals for {Player.CharacterName}");
                            return;
                        }

                        ShowGadgetPage(page);
                        break;
                    }

                    case ELoadoutPage.KNIFE:
                    {
                        if (!KnifePages.TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error finding the current page for knives for {Player.CharacterName}");
                            return;
                        }

                        ShowKnifePage(page);
                        break;
                    }

                    case ELoadoutPage.KILLSTREAK:
                    {
                        if (!KillstreakPages.TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error finding the current page for killstreaks for {Player.CharacterName}");
                            return;
                        }

                        ShowKillstreakPage(page);
                        break;
                    }

                    case ELoadoutPage.GLOVE:
                    {
                        if (!GlovePages.TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error finding the current page for gloves for {Player.CharacterName}");
                            return;
                        }

                        ShowGlovePage(page);
                        break;
                    }

                    case ELoadoutPage.CARD:
                    {
                        if (!CardPages.TryGetValue(LoadoutTabPageID, out var page))
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
                if (!PistolPages.TryGetValue(LoadoutTabPageID, out var page))
                {
                    Logging.Debug($"Error finding current page for pistols for {Player.CharacterName}");
                    return;
                }

                ShowGunPage(page);
                break;
            }

            case ELoadoutTab.SUBMACHINE_GUNS:
            {
                if (!SMGPages.TryGetValue(LoadoutTabPageID, out var page))
                {
                    Logging.Debug($"Error finding current page for smgs for {Player.CharacterName}");
                    return;
                }

                ShowGunPage(page);
                break;
            }

            case ELoadoutTab.SHOTGUNS:
            {
                if (!ShotgunPages.TryGetValue(LoadoutTabPageID, out var page))
                {
                    Logging.Debug($"Error finding current page for shotguns for {Player.CharacterName}");
                    return;
                }

                ShowGunPage(page);
                break;
            }

            case ELoadoutTab.LIGHT_MACHINE_GUNS:
            {
                if (!LMGPages.TryGetValue(LoadoutTabPageID, out var page))
                {
                    Logging.Debug($"Error finding current page for lmgs for {Player.CharacterName}");
                    return;
                }

                ShowGunPage(page);
                break;
            }

            case ELoadoutTab.ASSAULT_RIFLES:
            {
                if (!ARPages.TryGetValue(LoadoutTabPageID, out var page))
                {
                    Logging.Debug($"Error finding current page for ARs for {Player.CharacterName}");
                    return;
                }

                ShowGunPage(page);
                break;
            }

            case ELoadoutTab.SNIPER_RIFLES:
            {
                if (!SniperPages.TryGetValue(LoadoutTabPageID, out var page))
                {
                    Logging.Debug($"Error finding current page for snipers for {Player.CharacterName}");
                    return;
                }

                ShowGunPage(page);
                break;
            }

            case ELoadoutTab.CARBINES:
            {
                if (!CarbinePages.TryGetValue(LoadoutTabPageID, out var page))
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

        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var currentLoadout))
        {
            Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
            return;
        }

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
        for (var i = 0; i <= MAX_ITEMS_PER_PAGE; i++)
        {
            if (!page.Guns.TryGetValue(i, out var gun))
            {
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item BUTTON {i}", false);
                continue;
            }

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item BUTTON {i}", true);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Equipped {i}", (LoadoutPage == ELoadoutPage.PRIMARY && currentLoadout.Primary == gun) || (LoadoutPage == ELoadoutPage.SECONDARY && currentLoadout.Secondary == gun));
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item IMAGE {i}", gun.Gun.IconLink);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item TEXT {i}", gun.Gun.GunName);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Lock Overlay {i}", !gun.IsBought);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Lock Overlay TEXT {i}",
                gun.Gun.LevelRequirement > PlayerData.Level && !gun.IsUnlocked ? Plugin.Instance.Translate("Unlock_Level", gun.Gun.LevelRequirement) :
                    $"{Utility.GetCurrencySymbol(ECurrency.CREDIT)} <color={(PlayerData.Credits >= gun.Gun.BuyPrice ? "#9CFF84" : "#FF6E6E")}>{gun.Gun.BuyPrice}</color>");

            SendRarity("SERVER Item", gun.Gun.GunRarity, i);
        }
    }

    public void ShowAttachmentPage(PageAttachment page, LoadoutGun gun)
    {
        LoadoutTabPageID = page.PageID;

        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var currentLoadout))
        {
            Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
            return;
        }

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
        for (var i = 0; i <= MAX_ITEMS_PER_PAGE; i++)
        {
            if (!page.Attachments.TryGetValue(i, out var attachment))
            {
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item BUTTON {i}", false);
                continue;
            }

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item BUTTON {i}", true);
            var isAttachmentPrimary = IsAttachmentPagePrimary();
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Equipped {i}", (isAttachmentPrimary && currentLoadout.PrimaryAttachments.ContainsValue(attachment)) || (!isAttachmentPrimary && currentLoadout.SecondaryAttachments.ContainsValue(attachment)));
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item IMAGE {i}", attachment.Attachment.IconLink);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item TEXT {i}", attachment.Attachment.AttachmentName);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Lock Overlay {i}", !attachment.IsBought);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Lock Overlay TEXT {i}",
                attachment.LevelRequirement > gun.Level && !attachment.IsUnlocked ? Plugin.Instance.Translate("Unlock_Gun_Level", attachment.LevelRequirement) :
                    $"{Utility.GetCurrencySymbol(ECurrency.CREDIT)} <color={(PlayerData.Credits >= attachment.Attachment.BuyPrice ? "#9CFF84" : "#FF6E6E")}>{attachment.Attachment.BuyPrice}</color>");

            SendRarity("SERVER Item", attachment.Attachment.AttachmentRarity, i);
        }
    }

    public void ShowGunCharmPage(PageGunCharm page)
    {
        LoadoutTabPageID = page.PageID;

        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var currentLoadout))
        {
            Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
            return;
        }

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
        for (var i = 0; i <= MAX_ITEMS_PER_GRID; i++)
        {
            if (!page.GunCharms.TryGetValue(i, out var gunCharm))
            {
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", false);
                continue;
            }

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", true);
            var isAttachmentPrimary = IsAttachmentPagePrimary();
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid Equipped {i}", (isAttachmentPrimary && currentLoadout.PrimaryGunCharm == gunCharm) || (!isAttachmentPrimary && currentLoadout.SecondaryGunCharm == gunCharm));
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid IMAGE {i}", gunCharm.GunCharm.IconLink);
            SendRarity("SERVER Item Grid", gunCharm.GunCharm.CharmRarity, i);
        }
    }

    public void ShowGunSkinPage(PageGunSkin page)
    {
        LoadoutTabPageID = page.PageID;

        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var currentLoadout))
        {
            Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
            return;
        }

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
        for (var i = 0; i <= MAX_ITEMS_PER_GRID; i++)
        {
            if (!page.GunSkins.TryGetValue(i, out var skin))
            {
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", false);
                continue;
            }

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", true);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid Equipped {i}", (LoadoutPage == ELoadoutPage.PRIMARY_SKIN && currentLoadout.PrimarySkin == skin) || (LoadoutPage == ELoadoutPage.SECONDARY_SKIN && currentLoadout.SecondarySkin == skin));
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid IMAGE {i}", skin.IconLink);
            SendRarity("SERVER Item Grid", skin.SkinRarity, i);
        }
    }

    public void ShowKnifePage(PageKnife page)
    {
        LoadoutTabPageID = page.PageID;

        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var currentLoadout))
        {
            Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
            return;
        }

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
        for (var i = 0; i <= MAX_ITEMS_PER_GRID; i++)
        {
            if (!page.Knives.TryGetValue(i, out var knife))
            {
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", false);
                continue;
            }

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", true);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid Equipped {i}", currentLoadout.Knife == knife);
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid IMAGE {i}", knife.Knife.IconLink);
            SendRarity("SERVER Item Grid", knife.Knife.KnifeRarity, i);
        }
    }

    public void ShowPerkPage(PagePerk page)
    {
        LoadoutTabPageID = page.PageID;

        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var currentLoadout))
        {
            Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
            return;
        }

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
        for (var i = 0; i <= MAX_ITEMS_PER_PAGE; i++)
        {
            if (!page.Perks.TryGetValue(i, out var perk))
            {
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item BUTTON {i}", false);
                continue;
            }

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item BUTTON {i}", true);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Equipped {i}", currentLoadout.Perks.ContainsValue(perk));
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item IMAGE {i}", perk.Perk.IconLink);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item TEXT {i}", perk.Perk.PerkName);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Lock Overlay {i}", !perk.IsBought);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Lock Overlay TEXT {i}",
                perk.Perk.LevelRequirement > PlayerData.Level && !perk.IsUnlocked ? Plugin.Instance.Translate("Unlock_Level", perk.Perk.LevelRequirement) :
                    $"{Utility.GetCurrencySymbol(ECurrency.CREDIT)} <color={(PlayerData.Credits >= perk.Perk.BuyPrice ? "#9CFF84" : "#FF6E6E")}>{perk.Perk.BuyPrice}</color>");
            
            SendRarity("SERVER Item", perk.Perk.PerkRarity, i);
        }
    }

    public void ShowGadgetPage(PageGadget page)
    {
        LoadoutTabPageID = page.PageID;

        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var currentLoadout))
        {
            Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
            return;
        }

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
        for (var i = 0; i <= MAX_ITEMS_PER_PAGE; i++)
        {
            if (!page.Gadgets.TryGetValue(i, out var gadget))
            {
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item BUTTON {i}", false);
                continue;
            }

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item BUTTON {i}", true);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Equipped {i}", (LoadoutPage == ELoadoutPage.TACTICAL && currentLoadout.Tactical == gadget) || (LoadoutPage == ELoadoutPage.LETHAL && currentLoadout.Lethal == gadget));
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item IMAGE {i}", gadget.Gadget.IconLink);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item TEXT {i}", gadget.Gadget.GadgetName);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Lock Overlay {i}", !gadget.IsBought);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Lock Overlay TEXT {i}",
                gadget.Gadget.LevelRequirement > PlayerData.Level && !gadget.IsUnlocked ? Plugin.Instance.Translate("Unlock_Level", gadget.Gadget.LevelRequirement) :
                    $"{Utility.GetCurrencySymbol(ECurrency.CREDIT)} <color={(PlayerData.Credits >= gadget.Gadget.BuyPrice ? "#9CFF84" : "#FF6E6E")}>{gadget.Gadget.BuyPrice}</color>");

            SendRarity("SERVER Item", gadget.Gadget.GadgetRarity, i);
        }
    }

    public void ShowCardPage(PageCard page)
    {
        LoadoutTabPageID = page.PageID;

        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var currentLoadout))
        {
            Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
            return;
        }

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
        for (var i = 0; i <= MAX_ITEMS_PER_GRID; i++)
        {
            if (!page.Cards.TryGetValue(i, out var card))
            {
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", false);
                continue;
            }

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", true);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid Equipped {i}", currentLoadout.Card == card);
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid IMAGE {i}", card.Card.IconLink);
            SendRarity("SERVER Item Grid", card.Card.CardRarity, i);
        }
    }

    public void ShowGlovePage(PageGlove page)
    {
        LoadoutTabPageID = page.PageID;

        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var currentLoadout))
        {
            Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
            return;
        }

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
        for (var i = 0; i <= MAX_ITEMS_PER_GRID; i++)
        {
            if (!page.Gloves.TryGetValue(i, out var glove))
            {
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", false);
                continue;
            }

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", true);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid Equipped {i}", currentLoadout.Glove == glove);
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid IMAGE {i}", glove.Glove.IconLink);
            SendRarity("SERVER Item Grid", glove.Glove.GloveRarity, i);
        }
    }

    public void ShowKillstreakPage(PageKillstreak page)
    {
        LoadoutTabPageID = page.PageID;

        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var currentLoadout))
        {
            Logging.Debug($"Error finding current loadout for {Player.CharacterName} with id {LoadoutID}");
            return;
        }

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Page TEXT", $"Page {page.PageID}");
        for (var i = 0; i <= MAX_ITEMS_PER_GRID; i++)
        {
            if (!page.Killstreaks.TryGetValue(i, out var killstreak))
            {
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item BUTTON {i}", false);
                continue;
            }

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid BUTTON {i}", true);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid Equipped {i}", currentLoadout.Killstreaks.Contains(killstreak));
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Grid IMAGE {i}", killstreak.Killstreak.IconLink);
            SendRarity("SERVER Item Grid", killstreak.Killstreak.KillstreakRarity, i);
        }
    }

    public void ReloadSelectedItem()
    {
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
        {
            Logging.Debug($"Error finding current loadout with id {LoadoutID} for {Player.CharacterName}");
            return;
        }

        switch (LoadoutPage)
        {
            case ELoadoutPage.PRIMARY:
            {
                if (!PlayerLoadout.Guns.TryGetValue((ushort)SelectedItemID, out var gun))
                {
                    Logging.Debug($"Error finding gun at {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                ShowGun(gun);
                break;
            }

            case ELoadoutPage.SECONDARY:
            {
                if (!PlayerLoadout.Guns.TryGetValue((ushort)SelectedItemID, out var gun))
                {
                    Logging.Debug($"Error finding gun at {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                ShowGun(gun);
                break;
            }

            case ELoadoutPage.ATTACHMENT_PRIMARY_BARREL:
            case ELoadoutPage.ATTACHMENT_PRIMARY_GRIP:
            case ELoadoutPage.ATTACHMENT_PRIMARY_MAGAZINE:
            case ELoadoutPage.ATTACHMENT_PRIMARY_SIGHTS:
            {
                if (!PlayerLoadout.Guns.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out var gun))
                {
                    Logging.Debug($"Error finding primary that has been selected with id {loadout.Primary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                    return;
                }

                if (!gun.Attachments.TryGetValue((ushort)SelectedItemID, out var attachment))
                {
                    Logging.Debug($"Error finding attachment with attachment id {SelectedItemID} for gun {gun.Gun.GunName}");
                    return;
                }

                ShowAttachment(attachment, gun);
                break;
            }

            case ELoadoutPage.ATTACHMENT_SECONDARY_BARREL:
            case ELoadoutPage.ATTACHMENT_SECONDARY_MAGAZINE:
            case ELoadoutPage.ATTACHMENT_SECONDARY_SIGHTS:
            {
                if (!PlayerLoadout.Guns.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out var gun))
                {
                    Logging.Debug($"Error finding secondary that has been selected with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                    return;
                }

                if (!gun.Attachments.TryGetValue((ushort)SelectedItemID, out var attachment))
                {
                    Logging.Debug($"Error finding attachment with attachment id {SelectedItemID} for gun {gun.Gun.GunName}");
                    return;
                }

                ShowAttachment(attachment, gun);
                break;
            }

            case ELoadoutPage.ATTACHMENT_PRIMARY_CHARM:
            case ELoadoutPage.ATTACHMENT_SECONDARY_CHARM:
            {
                if (!PlayerLoadout.GunCharms.TryGetValue((ushort)SelectedItemID, out var gunCharm))
                {
                    Logging.Debug($"Error finding gun charm with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                ShowGunCharm(gunCharm);
                break;
            }

            case ELoadoutPage.LETHAL:
            {
                if (!PlayerLoadout.Gadgets.TryGetValue((ushort)SelectedItemID, out var gadget))
                {
                    Logging.Debug($"Error finding gadget with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                ShowGadget(gadget);
                break;
            }

            case ELoadoutPage.TACTICAL:
            {
                if (!PlayerLoadout.Gadgets.TryGetValue((ushort)SelectedItemID, out var gadget))
                {
                    Logging.Debug($"Error finding gadget with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                ShowGadget(gadget);
                break;
            }

            case ELoadoutPage.PERK1:
            case ELoadoutPage.PERK2:
            case ELoadoutPage.PERK3:
            {
                if (!PlayerLoadout.Perks.TryGetValue((int)SelectedItemID, out var perk))
                {
                    Logging.Debug($"Error finding perk with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                ShowPerk(perk);
                break;
            }

            case ELoadoutPage.KNIFE:
            {
                if (!PlayerLoadout.Knives.TryGetValue((ushort)SelectedItemID, out var knife))
                {
                    Logging.Debug($"Error finding knife with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                ShowKnife(knife);
                break;
            }

            case ELoadoutPage.KILLSTREAK:
            {
                if (!PlayerLoadout.Killstreaks.TryGetValue((int)SelectedItemID, out var killstreak))
                {
                    Logging.Debug($"Error finding kilsltreak with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                ShowKillstreak(killstreak);
                break;
            }

            case ELoadoutPage.GLOVE:
            {
                if (!PlayerLoadout.Gloves.TryGetValue((int)SelectedItemID, out var glove))
                {
                    Logging.Debug($"Error finding glove with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                ShowGlove(glove);
                break;
            }

            case ELoadoutPage.CARD:
            {
                if (!PlayerLoadout.Cards.TryGetValue((int)SelectedItemID, out var card))
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
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
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
                    case ELoadoutPage.PRIMARY_SKIN:
                    {
                        if (!GunSkinPages.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out var skinsPage))
                        {
                            Logging.Debug($"Error finding gun skin pages for primary with id {loadout.Primary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                            return;
                        }

                        if (!skinsPage.TryGetValue(LoadoutTabPageID, out var pageSkin))
                        {
                            Logging.Debug($"Error finding gun skin page at id {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        if (!pageSkin.GunSkins.TryGetValue(selected, out var skin))
                        {
                            Logging.Debug($"Error finding skin at {selected} at page with id {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGunSkin(skin);
                        break;
                    }

                    case ELoadoutPage.SECONDARY_SKIN:
                    {
                        if (!GunSkinPages.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out var skinsPage))
                        {
                            Logging.Debug($"Error finding gun skin pages for secondary with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                            return;
                        }

                        if (!skinsPage.TryGetValue(LoadoutTabPageID, out var pageSkin))
                        {
                            Logging.Debug($"Error finding gun skin page at id {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        if (!pageSkin.GunSkins.TryGetValue(selected, out var skin))
                        {
                            Logging.Debug($"Error finding skin at {selected} at page with id {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGunSkin(skin);
                        break;
                    }

                    case ELoadoutPage.ATTACHMENT_PRIMARY_BARREL:
                    case ELoadoutPage.ATTACHMENT_PRIMARY_GRIP:
                    case ELoadoutPage.ATTACHMENT_PRIMARY_MAGAZINE:
                    case ELoadoutPage.ATTACHMENT_PRIMARY_SIGHTS:
                    {
                        if (!Enum.TryParse(GetAttachmentPage(), false, out EAttachment attachmentType))
                        {
                            Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                            return;
                        }

                        if (!PlayerLoadout.Guns.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out var gun))
                        {
                            Logging.Debug($"Error finding primary that has been selected with id {loadout.Primary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                            return;
                        }

                        if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                        {
                            Logging.Debug($"Error finding primary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentTypePages.TryGetValue(attachmentType, out var attachmentPages))
                        {
                            Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentPages.TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error finding page {LoadoutTabPageID} of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        if (!page.Attachments.TryGetValue(selected, out var attachment))
                        {
                            Logging.Debug($"Error finding attachment at page id {LoadoutTabPageID} with position {selected} for {Player.CharacterName}");
                            return;
                        }

                        ShowAttachment(attachment, gun);
                        break;
                    }

                    case ELoadoutPage.ATTACHMENT_PRIMARY_CHARM:
                    case ELoadoutPage.ATTACHMENT_SECONDARY_CHARM:
                    {
                        if (!GunCharmPages.TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error finding gun charm page with id {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        if (!page.GunCharms.TryGetValue(selected, out var gunCharm))
                        {
                            Logging.Debug($"Error finding gun charm at {selected} at page {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGunCharm(gunCharm);
                        break;
                    }

                    case ELoadoutPage.ATTACHMENT_SECONDARY_BARREL:
                    case ELoadoutPage.ATTACHMENT_SECONDARY_MAGAZINE:
                    case ELoadoutPage.ATTACHMENT_SECONDARY_SIGHTS:
                    {
                        if (!Enum.TryParse(GetAttachmentPage(), false, out EAttachment attachmentType))
                        {
                            Logging.Debug($"Error finding attachment type that {Player.CharacterName} has selected");
                            return;
                        }

                        if (!PlayerLoadout.Guns.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out var gun))
                        {
                            Logging.Debug($"Error finding Secondary that has been selected with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                            return;
                        }

                        if (!AttachmentPages.TryGetValue(gun.Gun.GunID, out var attachmentTypePages))
                        {
                            Logging.Debug($"Error finding Secondary attachments for {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentTypePages.TryGetValue(attachmentType, out var attachmentPages))
                        {
                            Logging.Debug($"Error finding attachments with type {attachmentType} for {Player.CharacterName}");
                            return;
                        }

                        if (!attachmentPages.TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error finding page {LoadoutTabPageID} of attachment with type {attachmentType} for gun with id {gun.Gun.GunID} for {Player.CharacterName}");
                            return;
                        }

                        if (!page.Attachments.TryGetValue(selected, out var attachment))
                        {
                            Logging.Debug($"Error finding attachment at page id {LoadoutTabPageID} with position {selected} for {Player.CharacterName}");
                            return;
                        }

                        ShowAttachment(attachment, gun);
                        break;
                    }

                    case ELoadoutPage.PERK1:
                    case ELoadoutPage.PERK2:
                    case ELoadoutPage.PERK3:
                    {
                        if (!int.TryParse(GetPerkInt(), out var perkType))
                        {
                            Logging.Debug($"Error getting perk type from {LoadoutPage}");
                            return;
                        }

                        if (!PerkPages[perkType].TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error finding perk page with id {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        if (!page.Perks.TryGetValue(selected, out var perk))
                        {
                            Logging.Debug($"Error finding perk at {selected} at page {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        ShowPerk(perk);
                        break;
                    }

                    case ELoadoutPage.LETHAL:
                    {
                        if (!LethalPages.TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error finding lethal page with id {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        if (!page.Gadgets.TryGetValue(selected, out var gadget))
                        {
                            Logging.Debug($"Error finding lethal at {selected} at page {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGadget(gadget);
                        break;
                    }

                    case ELoadoutPage.TACTICAL:
                    {
                        if (!TacticalPages.TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error finding tactical page with id {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        if (!page.Gadgets.TryGetValue(selected, out var gadget))
                        {
                            Logging.Debug($"Error finding tactical at {selected} at page {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGadget(gadget);
                        break;
                    }

                    case ELoadoutPage.KNIFE:
                    {
                        if (!KnifePages.TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error finding knife page with id {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        if (!page.Knives.TryGetValue(selected, out var knife))
                        {
                            Logging.Debug($"Error finding knife at {selected} at page {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        ShowKnife(knife);
                        break;
                    }

                    case ELoadoutPage.KILLSTREAK:
                    {
                        if (!KillstreakPages.TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error finding killstreak page with id {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        if (!page.Killstreaks.TryGetValue(selected, out var killstreak))
                        {
                            Logging.Debug($"Error finding killstreak at {selected} at page {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        ShowKillstreak(killstreak);
                        break;
                    }

                    case ELoadoutPage.GLOVE:
                    {
                        if (!GlovePages.TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error finding glove page with id {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        if (!page.Gloves.TryGetValue(selected, out var glove))
                        {
                            Logging.Debug($"Error finding glove at {selected} at page {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        ShowGlove(glove);
                        break;
                    }

                    case ELoadoutPage.CARD:
                    {
                        if (!CardPages.TryGetValue(LoadoutTabPageID, out var page))
                        {
                            Logging.Debug($"Error finding card page with id {LoadoutTabPageID} for {Player.CharacterName}");
                            return;
                        }

                        if (!page.Cards.TryGetValue(selected, out var card))
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
                if (!PistolPages.TryGetValue(LoadoutTabPageID, out var page))
                {
                    Logging.Debug($"Error finding pistol page {LoadoutTabPageID} for {Player.CharacterName}");
                    return;
                }

                if (!page.Guns.TryGetValue(selected, out var gun))
                {
                    Logging.Debug($"Error finding gun at {selected} for page with id {LoadoutTabPageID} for {Player.CharacterName}");
                    return;
                }

                ShowGun(gun);
                break;
            }

            case ELoadoutTab.SUBMACHINE_GUNS:
            {
                if (!SMGPages.TryGetValue(LoadoutTabPageID, out var page))
                {
                    Logging.Debug($"Error finding smg page {LoadoutTabPageID} for {Player.CharacterName}");
                    return;
                }

                if (!page.Guns.TryGetValue(selected, out var gun))
                {
                    Logging.Debug($"Error finding gun at {selected} for page with id {LoadoutTabPageID} for {Player.CharacterName}");
                    return;
                }

                ShowGun(gun);
                break;
            }

            case ELoadoutTab.SHOTGUNS:
            {
                if (!ShotgunPages.TryGetValue(LoadoutTabPageID, out var page))
                {
                    Logging.Debug($"Error finding shotgun page {LoadoutTabPageID} for {Player.CharacterName}");
                    return;
                }

                if (!page.Guns.TryGetValue(selected, out var gun))
                {
                    Logging.Debug($"Error finding gun at {selected} for page with id {LoadoutTabPageID} for {Player.CharacterName}");
                    return;
                }

                ShowGun(gun);
                break;
            }

            case ELoadoutTab.SNIPER_RIFLES:
            {
                if (!SniperPages.TryGetValue(LoadoutTabPageID, out var page))
                {
                    Logging.Debug($"Error finding sniper page {LoadoutTabPageID} for {Player.CharacterName}");
                    return;
                }

                if (!page.Guns.TryGetValue(selected, out var gun))
                {
                    Logging.Debug($"Error finding gun at {selected} for page with id {LoadoutTabPageID} for {Player.CharacterName}");
                    return;
                }

                ShowGun(gun);
                break;
            }

            case ELoadoutTab.LIGHT_MACHINE_GUNS:
            {
                if (!LMGPages.TryGetValue(LoadoutTabPageID, out var page))
                {
                    Logging.Debug($"Error finding lmg page {LoadoutTabPageID} for {Player.CharacterName}");
                    return;
                }

                if (!page.Guns.TryGetValue(selected, out var gun))
                {
                    Logging.Debug($"Error finding gun at {selected} for page with id {LoadoutTabPageID} for {Player.CharacterName}");
                    return;
                }

                ShowGun(gun);
                break;
            }

            case ELoadoutTab.ASSAULT_RIFLES:
            {
                if (!ARPages.TryGetValue(LoadoutTabPageID, out var page))
                {
                    Logging.Debug($"Error finding ar page {LoadoutTabPageID} for {Player.CharacterName}");
                    return;
                }

                if (!page.Guns.TryGetValue(selected, out var gun))
                {
                    Logging.Debug($"Error finding gun at {selected} for page with id {LoadoutTabPageID} for {Player.CharacterName}");
                    return;
                }

                ShowGun(gun);
                break;
            }

            case ELoadoutTab.CARBINES:
            {
                if (!CarbinePages.TryGetValue(LoadoutTabPageID, out var page))
                {
                    Logging.Debug($"Error finding carbine page {LoadoutTabPageID} for {Player.CharacterName}");
                    return;
                }

                if (!page.Guns.TryGetValue(selected, out var gun))
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
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
        {
            Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
            return;
        }

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy BUTTON", !gun.IsBought);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy Locked", !gun.IsUnlocked && gun.Gun.LevelRequirement > PlayerData.Level);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy TEXT", $"BUY {Utility.GetCurrencySymbol(ECurrency.CREDIT)} <color={(PlayerData.Credits >= gun.Gun.BuyPrice ? "#9CFF84" : "#FF6E6E")}>{gun.Gun.BuyPrice}</color>");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Unlock BUTTON", !gun.IsBought && !gun.IsUnlocked && gun.Gun.LevelRequirement > PlayerData.Level);
        var coins = gun.Gun.GetCoins(PlayerData.Level);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Unlock TEXT", $"UNLOCK {Utility.GetCurrencySymbol(ECurrency.COIN)} <color={(PlayerData.Coins >= coins ? "#9CFF84" : "#FF6E6E")}>{coins}</color>");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Equip BUTTON", gun.IsBought && ((LoadoutPage == ELoadoutPage.PRIMARY && loadout.Primary != gun) || (LoadoutPage == ELoadoutPage.SECONDARY && loadout.Secondary != gun)));
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Dequip BUTTON", false);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Description TEXT", gun.Gun.GunDesc);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Weapon IMAGE", gun.Gun.IconLink);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item TEXT", gun.Gun.GunName);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Level TEXT", gun.Level.ToString());
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item ProsCons", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Description TEXT", true);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Credits", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Owned", false);
        
        _ = gun.TryGetNeededXP(out var neededXP);
        var spaces = neededXP != 0 ? gun.XP * 188 / neededXP : 0;
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item XP Bar Fill", spaces == 0 ? UIManager.HAIRSPACE_SYMBOL_STRING : new(UIManager.HAIRSPACE_SYMBOL_CHAR, spaces));
        SendRarityName("SERVER Item Rarity TEXT", gun.Gun.GunRarity);
        
        // Stats
        if (ShowingStats)
        {
            Logging.Debug($"Stats shower already doing it's work, returning");
            return;
        }

        StatsShower.Stop();
        ShowingStats = true;
        if ((LoadoutPage == ELoadoutPage.PRIMARY && loadout.Primary == gun) || (LoadoutPage == ELoadoutPage.SECONDARY && loadout.Secondary == gun))
            StatsShower = Plugin.Instance.StartCoroutine(ShowEquippedGunStats(loadout, gun));
        else
        {
            var currentGun = LoadoutPage == ELoadoutPage.PRIMARY ? loadout.Primary : loadout.Secondary;
            StatsShower = Plugin.Instance.StartCoroutine(ShowComparisonGunStats(currentGun, gun));
        }
    }

    public IEnumerator ShowEquippedGunStats(Loadout loadout, LoadoutGun gun)
    {
        yield return new WaitForSeconds(0.1f);

        gun.GetCurrentStats(loadout, out var defaultStats, out var finalStats, out var attachmentsCompare, out var perksCompare);
        foreach (var stat in defaultStats)
        {
            var uiName = stat.Key.ToUIName();
            var maxAmount = stat.Key.GetMaxAmount();
            var initialStat = stat.Value;
            var finalStat = finalStats.TryGetValue(stat.Key, out var finalStatValue) ? finalStatValue : initialStat;
            var attachmentCompare = attachmentsCompare.TryGetValue(stat.Key, out var attachmentStatValue) ? attachmentStatValue : 0;
            var perkCompare = perksCompare.TryGetValue(stat.Key, out var perkCompareValue) ? perkCompareValue : 0;
            var bracketText = $"[{initialStat}{(attachmentCompare != 0 ? $" {(attachmentCompare > 0 ? "+" : "-")} <color=#e8b843>{Math.Abs(attachmentCompare)}</color>" : "")}{(perkCompare != 0 ? $" {(perkCompare > 0 ? "+" : "-")} <color=#579fdb>{Math.Abs(perkCompare)}</color>" : "")}]";
            if (finalStat == initialStat)
            {
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Colored Fill Enabler 0", true);
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Normal Fill Enabler {Math.Min(100, initialStat * 100 / maxAmount)}", true);
                EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Number Text", initialStat.ToString());
                EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Bracket Text", " ");
            }
            else if (finalStat > initialStat)
            {
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Colored Fill Enabler {Math.Min(100, finalStat * 100 / maxAmount)}", true);
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Normal Fill Enabler {Math.Min(100, initialStat * 100 / maxAmount)}", true);
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Slider Green Enabler", true);
                EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Number Text", finalStat.ToString());
                EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Bracket Text", bracketText);
            }
            else
            {
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Colored Fill Enabler {Math.Min(100, initialStat * 100 / maxAmount)}", true);
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Normal Fill Enabler {Math.Min(100, finalStat * 100 / maxAmount)}", true);
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Slider Red Enabler", true);
                EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Number Text", finalStat.ToString());
                EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Bracket Text", bracketText);
            }
        }

        ShowingStats = false;
    }

    public IEnumerator ShowComparisonGunStats(LoadoutGun currentGun, LoadoutGun newGun)
    {
        yield return new WaitForSeconds(0.1f);
        var equippedStats = currentGun.GetDefaultStats();
        var newStats = newGun.GetDefaultStats();
        foreach (var stat in newStats)
        {
            var uiName = stat.Key.ToUIName();
            var maxAmount = stat.Key.GetMaxAmount();
            var currentStat = equippedStats.TryGetValue(stat.Key, out var currentStatValue) ? currentStatValue : 0;
            var newStat = stat.Value;
            var compareStat = newStat - currentStat;
            Logging.Debug($"Stat: {stat.Key}, Current Stat: {currentStat}, New Stat: {newStat}, Compare Stat: {compareStat}, UI Name: {uiName}, Max Amount: {maxAmount}");
            switch (compareStat)
            {
                case 0:
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Colored Fill Enabler 0", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Normal Fill Enabler {Math.Min(100, currentStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Number Text", currentStat.ToString());
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Bracket Text", " ");
                    break;
                case > 0:
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Colored Fill Enabler {Math.Min(100, newStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Normal Fill Enabler {Math.Min(100, currentStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Slider Green Enabler", true);
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Number Text", newStat.ToString());
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Bracket Text", $"[{currentStat} + <color=#31AB40>{compareStat}</color>]");
                    break;
                default:
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Colored Fill Enabler {Math.Min(100, currentStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Normal Fill Enabler {Math.Min(100, newStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Slider Red Enabler", true);
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Number Text", newStat.ToString());
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Bracket Text", $"[{currentStat} - <color=#CE3036>{Math.Abs(compareStat)}</color>]");
                    break;
            }
        }

        ShowingStats = false;
    }
    
    public void ShowAttachment(LoadoutAttachment attachment, LoadoutGun gun)
    {
        SelectedItemID = attachment.Attachment.AttachmentID;
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
        {
            Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
            return;
        }

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy BUTTON", !attachment.IsBought);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy Locked", !attachment.IsUnlocked && attachment.LevelRequirement > gun.Level);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy TEXT", $"BUY {Utility.GetCurrencySymbol(ECurrency.CREDIT)} <color={(PlayerData.Credits >= attachment.Attachment.BuyPrice ? "#9CFF84" : "#FF6E6E")}>{attachment.Attachment.BuyPrice}</color>");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Unlock BUTTON", !attachment.IsBought && !attachment.IsUnlocked && attachment.LevelRequirement > gun.Level);
        var coins = attachment.GetCoins(gun.Level);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Unlock TEXT", $"UNLOCK {Utility.GetCurrencySymbol(ECurrency.COIN)} <color={(PlayerData.Coins >= coins ? "#9CFF84" : "#FF6E6E")}>{coins}</color>");
        var isAttachmentPrimary = IsAttachmentPagePrimary();
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Equip BUTTON",
            attachment.IsBought && ((isAttachmentPrimary && !loadout.PrimaryAttachments.ContainsValue(attachment)) || (!isAttachmentPrimary && !loadout.SecondaryAttachments.ContainsValue(attachment))));

        if (attachment.Attachment.AttachmentType != EAttachment.MAGAZINE)
        {
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Dequip BUTTON",
                attachment.IsBought && ((isAttachmentPrimary && loadout.PrimaryAttachments.ContainsValue(attachment)) || (!isAttachmentPrimary && loadout.SecondaryAttachments.ContainsValue(attachment))));
        }
        else
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Dequip BUTTON", false);

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Description TEXT", false);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Attachment IMAGE", attachment.Attachment.IconLink);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item TEXT", attachment.Attachment.AttachmentName);
        SendRarityName("SERVER Item Rarity TEXT", attachment.Attachment.AttachmentRarity);

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item ProsCons", !(attachment.Attachment.AttachmentPros.Count == 0 && attachment.Attachment.AttachmentCons.Count == 0));
        for (var i = 0; i <= 2; i++)
        {
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Pro {i}", attachment.Attachment.AttachmentPros.Count > i);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Pro TEXT {i}", attachment.Attachment.AttachmentPros.Count > i ? attachment.Attachment.AttachmentPros[i].Trim() : "");

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Con {i}", attachment.Attachment.AttachmentCons.Count > i);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Item Con TEXT {i}", attachment.Attachment.AttachmentCons.Count > i ? attachment.Attachment.AttachmentCons[i].Trim() : "");
        }

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Credits", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Owned", false);

        Logging.Debug($"Attachment: {attachment.Attachment.AttachmentName}, ID: {attachment.Attachment.AttachmentID}");
        if (ShowingStats)
        {
            Logging.Debug("Stats shower already doing it's work, returning");
            return;
        }

        StatsShower.Stop();
        ShowingStats = true;
        if (attachment.IsBought && ((isAttachmentPrimary && loadout.PrimaryAttachments.ContainsValue(attachment)) || (!isAttachmentPrimary && loadout.SecondaryAttachments.ContainsValue(attachment))))
            StatsShower = Plugin.Instance.StartCoroutine(ShowEquippedAttachmentStats(loadout, gun, attachment));
        else
            StatsShower = Plugin.Instance.StartCoroutine(ShowComparisonAttachmentStats(loadout, gun, attachment));
    }

    public IEnumerator ShowEquippedAttachmentStats(Loadout loadout, LoadoutGun gun, LoadoutAttachment attachment)
    {
        yield return new WaitForSeconds(0.1f);
        Logging.Debug("Attachment is equipped, get the stats while ignore the attachment type");
        gun.GetCurrentStats(loadout, attachment.Attachment.AttachmentType, out var finalStats);
        foreach (var finalStat in finalStats)
        {
            var stat = finalStat.Key;
            var uiName = stat.ToUIName();
            var maxAmount = stat.GetMaxAmount();
            var currentStat = finalStat.Value;
            int newStat;
            int compareStat;
            switch (stat)
            {
                case EStat.AMMO:
                {
                    newStat = attachment.Attachment.StatMultipliers.TryGetValue(EStat.AMMO, out var newAmmoStatValue) ? Mathf.RoundToInt(newAmmoStatValue) : currentStat;
                    compareStat = newStat - currentStat;
                    Logging.Debug($"Stat: {stat}, Current Stat: {currentStat}, New Stat: {newStat}, Compare Stat: {compareStat}, UI Name: {uiName}, Max Amount: {maxAmount}");
                    break;
                }
                case EStat.RELOAD_SPEED:
                {
                    var multiplier = attachment.Attachment.StatMultipliers.TryGetValue(stat, out var multiplierValue) ? multiplierValue : 0f;
                    var tempStat = gun.Gun.Stats[EStat.RELOAD_SPEED];
                    newStat = multiplier != 0f ? tempStat + Mathf.RoundToInt(multiplier * tempStat) : currentStat;
                    compareStat = newStat - currentStat;
                    Logging.Debug($"Stat: {stat}, Current Stat: {currentStat}, Temp Stat: {tempStat} multiplier: {multiplier}, new stat: {newStat}, compare stat {compareStat}, UI Name: {uiName}, Max Amount: {maxAmount}");
                    break;
                }
                default:
                {
                    var multiplier = attachment.Attachment.StatMultipliers.TryGetValue(stat, out var multiplierValue) ? multiplierValue : 0f;
                    newStat = currentStat + Mathf.RoundToInt(multiplier * currentStat);
                    compareStat = newStat - currentStat;
                    Logging.Debug($"Stat: {stat}, Current Stat: {currentStat}, multiplier: {multiplier}, New Stat: {newStat}, Compare Stat: {compareStat}, UI Name: {uiName}, Max Amount: {maxAmount}");
                    break;
                }
            }

            switch (compareStat)
            {
                case 0:
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Colored Fill Enabler 0", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Normal Fill Enabler {Math.Min(100, currentStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Number Text", currentStat.ToString());
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Bracket Text", " ");
                    break;
                case > 0:
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Colored Fill Enabler {Math.Min(100, newStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Normal Fill Enabler {Math.Min(100, currentStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Slider Green Enabler", true);
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Number Text", newStat.ToString());
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Bracket Text", $"[{currentStat} + <color=#31AB40>{compareStat}</color>]");
                    break;
                default:
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Colored Fill Enabler {Math.Min(100, currentStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Normal Fill Enabler {Math.Min(100, newStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Slider Red Enabler", true);
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Number Text", newStat.ToString());
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Bracket Text", $"[{currentStat} - <color=#CE3036>{Math.Abs(compareStat)}</color>]");
                    break;
            }
        }

        ShowingStats = false;
    }

    public IEnumerator ShowComparisonAttachmentStats(Loadout loadout, LoadoutGun gun, LoadoutAttachment attachment)
    {
        yield return new WaitForSeconds(0.1f);
        Logging.Debug("Attachment is not equipped, getting the stats for the current gun");
        gun.GetCurrentStats(loadout, out var _, out var statsWithCurrentAttachment, out var _, out var _);
        Logging.Debug("Getting the stats for the gun while ignoring the current attachment type");
        gun.GetCurrentStats(loadout, attachment.Attachment.AttachmentType, out var statsWithoutCurrentAttachment);
        Logging.Debug("Adding the stat multipliers of the current attachment to the stats without current attachment, and computing the comparison");

        foreach (var finalStat in statsWithCurrentAttachment)
        {
            var stat = finalStat.Key;
            var uiName = stat.ToUIName();
            var maxAmount = stat.GetMaxAmount();
            var currentStat = finalStat.Value;
            int newStat;
            int compareStat;
            switch (stat)
            {
                case EStat.AMMO:
                {
                    newStat = attachment.Attachment.StatMultipliers.TryGetValue(EStat.AMMO, out var newAmmoStatValue) ? Mathf.RoundToInt(newAmmoStatValue) : currentStat;
                    compareStat = newStat - currentStat;
                    Logging.Debug($"Stat: {stat}, Current Stat: {currentStat}, New Stat: {newStat}, Compare Stat: {compareStat}, UI Name: {uiName}, Max Amount: {maxAmount}");
                    break;
                }
                case EStat.RELOAD_SPEED:
                {
                    var multiplier = attachment.Attachment.StatMultipliers.TryGetValue(stat, out var multiplierValue) ? multiplierValue : 0f;
                    var tempStat = gun.Gun.Stats[EStat.RELOAD_SPEED];
                    newStat = multiplier != 0f ? tempStat + Mathf.RoundToInt(multiplier * tempStat) : currentStat;
                    compareStat = newStat - currentStat;
                    Logging.Debug($"Stat: {stat}, Current Stat: {currentStat}, Temp Stat: {tempStat}, multiplier: {multiplier}, New Stat: {newStat}, Compare Stat: {compareStat}, UI Name: {uiName}, Max Amount: {maxAmount}");
                    break;
                }
                default:
                {
                    var multiplier = attachment.Attachment.StatMultipliers.TryGetValue(stat, out var multiplierValue) ? multiplierValue : 0f;
                    var tempStat = statsWithoutCurrentAttachment.TryGetValue(stat, out var tempStatValue) ? tempStatValue : currentStat;
                    newStat = tempStat + Mathf.RoundToInt(multiplier * tempStat);
                    compareStat = newStat - currentStat;
                    Logging.Debug($"Stat: {stat}, Current Stat: {currentStat}, Temp Stat: {tempStat}, multiplier: {multiplier}, New Stat: {newStat}, Compare Stat: {compareStat}, UI Name: {uiName}, Max Amount: {maxAmount}");
                    break;
                }
            }

            switch (compareStat)
            {
                case 0:
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Colored Fill Enabler 0", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Normal Fill Enabler {Math.Min(100, currentStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Number Text", currentStat.ToString());
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Bracket Text", " ");
                    break;
                case > 0:
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Colored Fill Enabler {Math.Min(100, newStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Normal Fill Enabler {Math.Min(100, currentStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Slider Green Enabler", true);
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Number Text", newStat.ToString());
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Bracket Text", $"[{currentStat} + <color=#31AB40>{compareStat}</color>]");
                    break;
                default:
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Colored Fill Enabler {Math.Min(100, currentStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Normal Fill Enabler {Math.Min(100, newStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Slider Red Enabler", true);
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Number Text", newStat.ToString());
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Bracket Text", $"[{currentStat} - <color=#CE3036>{Math.Abs(compareStat)}</color>]");
                    break;
            }
        }

        ShowingStats = false;
    }
    
    public void ShowGunCharm(LoadoutGunCharm gunCharm)
    {
        SelectedItemID = gunCharm.GunCharm.CharmID;
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
        {
            Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
            return;
        }

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy BUTTON", !gunCharm.IsBought);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy Locked", !gunCharm.IsUnlocked && gunCharm.GunCharm.LevelRequirement > PlayerData.Level);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy TEXT", $"BUY {Utility.GetCurrencySymbol(ECurrency.CREDIT)} <color={(PlayerData.Credits >= gunCharm.GunCharm.BuyPrice ? "#9CFF84" : "#FF6E6E")}>{gunCharm.GunCharm.BuyPrice}</color>");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Unlock BUTTON", !gunCharm.IsBought && !gunCharm.IsUnlocked && gunCharm.GunCharm.LevelRequirement > PlayerData.Level);
        var coins = gunCharm.GunCharm.GetCoins(PlayerData.Level);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Unlock TEXT", $"UNLOCK {Utility.GetCurrencySymbol(ECurrency.COIN)} <color={(PlayerData.Coins >= coins ? "#9CFF84" : "#FF6E6E")}>{coins}</color>");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Unlock TEXT", $"UNLOCK {Utility.GetCurrencySymbol(ECurrency.COIN)} {gunCharm.GunCharm.GetCoins(PlayerData.Level)}");
        var isAttachmentPrimary = IsAttachmentPagePrimary();
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Equip BUTTON", gunCharm.IsBought && ((isAttachmentPrimary && loadout.PrimaryGunCharm != gunCharm) || (!isAttachmentPrimary && loadout.SecondaryGunCharm != gunCharm)));
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Dequip BUTTON", gunCharm.IsBought && ((isAttachmentPrimary && loadout.PrimaryGunCharm == gunCharm) || (!isAttachmentPrimary && loadout.SecondaryGunCharm == gunCharm)));
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Description TEXT", gunCharm.GunCharm.CharmDesc);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Attachment IMAGE", gunCharm.GunCharm.IconLink);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item TEXT", gunCharm.GunCharm.CharmName);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item ProsCons", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Description TEXT", true);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Credits", !string.IsNullOrEmpty(gunCharm.GunCharm.AuthorCredits));
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Credits TEXT", gunCharm.GunCharm.AuthorCredits);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Owned", gunCharm.GunCharm.UnboxedAmount > 0);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Owned TEXT", $"Charm owned by {gunCharm.GunCharm.UnboxedAmount} players");
        
        SendRarityName("SERVER Item Rarity TEXT", gunCharm.GunCharm.CharmRarity);
    }

    public void ShowGunSkin(GunSkin skin)
    {
        SelectedItemID = skin.ID;
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
        {
            Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
            return;
        }

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy BUTTON", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Unlock BUTTON", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Equip BUTTON", (LoadoutPage == ELoadoutPage.PRIMARY_SKIN && loadout.PrimarySkin != skin) || (LoadoutPage == ELoadoutPage.SECONDARY_SKIN && loadout.SecondarySkin != skin));
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Dequip BUTTON", (LoadoutPage == ELoadoutPage.PRIMARY_SKIN && loadout.PrimarySkin == skin) || (LoadoutPage == ELoadoutPage.SECONDARY_SKIN && loadout.SecondarySkin == skin));
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Description TEXT", skin.SkinDesc);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Weapon IMAGE", skin.IconLink);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item TEXT", skin.SkinName);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item ProsCons", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Description TEXT", true);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Credits", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Owned", skin.UnboxedAmount > 0);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Owned TEXT", $"Skin owned by {skin.UnboxedAmount} players");
        SendRarityName("SERVER Item Rarity TEXT", skin.SkinRarity);
    }

    public void ShowKnife(LoadoutKnife knife)
    {
        SelectedItemID = knife.Knife.KnifeID;
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
        {
            Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
            return;
        }

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy BUTTON", !knife.IsBought);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy Locked", !knife.IsUnlocked && knife.Knife.LevelRequirement > PlayerData.Level);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy TEXT", $"BUY {Utility.GetCurrencySymbol(ECurrency.CREDIT)} <color={(PlayerData.Credits >= knife.Knife.BuyPrice ? "#9CFF84" : "#FF6E6E")}>{knife.Knife.BuyPrice}</color>");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Unlock BUTTON", !knife.IsBought && !knife.IsUnlocked && knife.Knife.LevelRequirement > PlayerData.Level);
        var coins = knife.Knife.GetCoins(PlayerData.Level);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Unlock TEXT", $"UNLOCK {Utility.GetCurrencySymbol(ECurrency.COIN)} <color={(PlayerData.Coins >= coins ? "#9CFF84" : "#FF6E6E")}>{coins}</color>");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Equip BUTTON", knife.IsBought && loadout.Knife != knife);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Dequip BUTTON", false);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Description TEXT", knife.Knife.KnifeDesc);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item IMAGE", knife.Knife.IconLink);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item TEXT", knife.Knife.KnifeName);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item ProsCons", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Description TEXT", true);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Credits", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Owned", knife.Knife.UnboxedAmount > 0);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Owned TEXT", $"Knife owned by {knife.Knife.UnboxedAmount} players");
        
        SendRarityName("SERVER Item Rarity TEXT", knife.Knife.KnifeRarity);
    }

    public void ShowPerk(LoadoutPerk perk)
    {
        SelectedItemID = perk.Perk.PerkID;
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
        {
            Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
            return;
        }

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy BUTTON", !perk.IsBought);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy Locked", !perk.IsUnlocked && perk.Perk.LevelRequirement > PlayerData.Level);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy TEXT", $"BUY {Utility.GetCurrencySymbol(ECurrency.CREDIT)} <color={(PlayerData.Credits >= perk.Perk.BuyPrice ? "#9CFF84" : "#FF6E6E")}>{perk.Perk.BuyPrice}</color>");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Unlock BUTTON", !perk.IsBought && !perk.IsUnlocked && perk.Perk.LevelRequirement > PlayerData.Level);
        var coins = perk.Perk.GetCoins(PlayerData.Level);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Unlock TEXT", $"UNLOCK {Utility.GetCurrencySymbol(ECurrency.COIN)} <color={(PlayerData.Coins >= coins ? "#9CFF84" : "#FF6E6E")}>{coins}</color>");
        var isEquipped = loadout.Perks.TryGetValue(perk.Perk.PerkType, out var equippedPerk) && equippedPerk == perk;
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Equip BUTTON", perk.IsBought && !isEquipped);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Dequip BUTTON", perk.IsBought && isEquipped);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Description TEXT", perk.Perk.PerkDesc);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item IMAGE", perk.Perk.IconLink);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item TEXT", perk.Perk.PerkName);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item ProsCons", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Description TEXT", true);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Credits", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Owned", false);
        switch (perk.Perk.PerkType)
        {
            case 1:
                EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Rarity TEXT", $"<color={Utility.GetRarityColor(ERarity.CYAN)}>PERK 1</color>");
                break;
            case 2:
                EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Rarity TEXT", $"<color={Utility.GetRarityColor(ERarity.MYTHICAL)}>PERK 2</color>");
                break;
            case 3:
                EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Rarity TEXT", $"<color={Utility.GetRarityColor(ERarity.YELLOW)}>PERK 3</color>");
                break;
        }

        if (ShowingStats)
        {
            Logging.Debug("Stat shower already doing it's work, returning");
            return;
        }

        StatsShower.Stop();
        ShowingStats = true;
        
        StatsShower = Plugin.Instance.StartCoroutine(isEquipped ? ShowEquippedPerkStats(loadout, perk) : ShowComparisonPerkStats(loadout, perk));
    }

    public IEnumerator ShowEquippedPerkStats(Loadout loadout, LoadoutPerk perk)
    {
        yield return new WaitForSeconds(0.1f);

        var gun = loadout.Primary;
        Logging.Debug("Perk is equipped, get the stats while ignore the perk type");
        gun.GetCurrentStats(loadout, perk.Perk.PerkType, out var finalStats);
        foreach (var finalStat in finalStats)
        {
            var stat = finalStat.Key;
            var uiName = stat.ToUIName();
            var maxAmount = stat.GetMaxAmount();
            var currentStat = finalStat.Value;
            int newStat;
            int compareStat;
            if (stat == EStat.AMMO)
            {
                newStat = currentStat;
                compareStat = newStat - currentStat;
                Logging.Debug($"Stat: {stat}, Current Stat: {currentStat}, New Stat: {newStat}, Compare Stat: {compareStat}, UI Name: {uiName}, Max Amount: {maxAmount}");
            }
            else
            {
                var multiplier = perk.Perk.StatMultipliers.TryGetValue(stat, out var multiplierValue) ? multiplierValue : 0f;
                newStat = currentStat + Mathf.RoundToInt(multiplier * currentStat);
                compareStat = newStat - currentStat;
                Logging.Debug($"Stat: {stat}, Current Stat: {currentStat}, multiplier: {multiplier}, New Stat: {newStat}, Compare Stat: {compareStat}, UI Name: {uiName}, Max Amount: {maxAmount}");
            }

            switch (compareStat)
            {
                case 0:
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Colored Fill Enabler 0", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Normal Fill Enabler {Math.Min(100, currentStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Number Text", currentStat.ToString());
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Bracket Text", " ");
                    break;
                case > 0:
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Colored Fill Enabler {Math.Min(100, newStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Normal Fill Enabler {Math.Min(100, currentStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Slider Green Enabler", true);
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Number Text", newStat.ToString());
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Bracket Text", $"[{currentStat} + <color=#31AB40>{compareStat}</color>]");
                    break;
                default:
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Colored Fill Enabler {Math.Min(100, currentStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Normal Fill Enabler {Math.Min(100, newStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Slider Red Enabler", true);
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Number Text", newStat.ToString());
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Bracket Text", $"[{currentStat} - <color=#CE3036>{Math.Abs(compareStat)}</color>]");
                    break;
            }
        }

        ShowingStats = false;
    }

    public IEnumerator ShowComparisonPerkStats(Loadout loadout, LoadoutPerk perk)
    {
        yield return new WaitForSeconds(0.1f);

        var gun = loadout.Primary;
        Logging.Debug("Perk is not equipped, getting the stats for the current gun");
        gun.GetCurrentStats(loadout, out var _, out var statsWithCurrentAttachment, out var _, out var _);
        Logging.Debug("Getting the stats for the gun while ignoring the current perk type");
        gun.GetCurrentStats(loadout, perk.Perk.PerkType, out var statsWithoutCurrentAttachment);
        Logging.Debug("Adding the stat multipliers of the current perk to the stats without current perk, and computing the comparison");

        foreach (var finalStat in statsWithCurrentAttachment)
        {
            var stat = finalStat.Key;
            var uiName = stat.ToUIName();
            var maxAmount = stat.GetMaxAmount();
            var currentStat = finalStat.Value;
            int newStat;
            int compareStat;
            if (stat == EStat.AMMO)
            {
                newStat = currentStat;
                compareStat = newStat - currentStat;
                Logging.Debug($"Stat: {stat}, Current Stat: {currentStat}, New Stat: {newStat}, Compare Stat: {compareStat}, UI Name: {uiName}, Max Amount: {maxAmount}");
            }
            else
            {
                var multiplier = perk.Perk.StatMultipliers.TryGetValue(stat, out var multiplierValue) ? multiplierValue : 0f;
                var tempStat = statsWithoutCurrentAttachment.TryGetValue(stat, out var tempStatValue) ? tempStatValue : currentStat;
                newStat = tempStat + Mathf.RoundToInt(multiplier * tempStat);
                compareStat = newStat - currentStat;
                Logging.Debug($"Stat: {stat}, Current Stat: {currentStat}, Temp Stat: {tempStat}, multiplier: {multiplier}, New Stat: {newStat}, Compare Stat: {compareStat}, UI Name: {uiName}, Max Amount: {maxAmount}");
            }

            switch (compareStat)
            {
                case 0:
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Colored Fill Enabler 0", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Normal Fill Enabler {Math.Min(100, currentStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Number Text", currentStat.ToString());
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Bracket Text", " ");
                    break;
                case > 0:
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Colored Fill Enabler {Math.Min(100, newStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Normal Fill Enabler {Math.Min(100, currentStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Slider Green Enabler", true);
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Number Text", newStat.ToString());
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Bracket Text", $"[{currentStat} + <color=#31AB40>{compareStat}</color>]");
                    break;
                default:
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Colored Fill Enabler {Math.Min(100, currentStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Normal Fill Enabler {Math.Min(100, newStat * 100 / maxAmount)}", true);
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Slider Red Enabler", true);
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Number Text", newStat.ToString());
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"{uiName} Bracket Text", $"[{currentStat} - <color=#CE3036>{Math.Abs(compareStat)}</color>]");
                    break;
            }
        }

        ShowingStats = false;
    }

    public void ShowGadget(LoadoutGadget gadget)
    {
        SelectedItemID = gadget.Gadget.GadgetID;
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
        {
            Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
            return;
        }

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy BUTTON", !gadget.IsBought);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy Locked", !gadget.IsUnlocked && gadget.Gadget.LevelRequirement > PlayerData.Level);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy TEXT", $"BUY {Utility.GetCurrencySymbol(ECurrency.CREDIT)} <color={(PlayerData.Credits >= gadget.Gadget.BuyPrice ? "#9CFF84" : "#FF6E6E")}>{gadget.Gadget.BuyPrice}</color>");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Unlock BUTTON", !gadget.IsBought && !gadget.IsUnlocked && gadget.Gadget.LevelRequirement > PlayerData.Level);
        var coins = gadget.Gadget.GetCoins(PlayerData.Level);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Unlock TEXT", $"UNLOCK {Utility.GetCurrencySymbol(ECurrency.COIN)} <color={(PlayerData.Coins >= coins ? "#9CFF84" : "#FF6E6E")}>{coins}</color>");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Equip BUTTON", gadget.IsBought && ((LoadoutPage == ELoadoutPage.TACTICAL && loadout.Tactical != gadget) || (LoadoutPage == ELoadoutPage.LETHAL && loadout.Lethal != gadget)));
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Dequip BUTTON", gadget.IsBought && ((LoadoutPage == ELoadoutPage.TACTICAL && loadout.Tactical == gadget) || (LoadoutPage == ELoadoutPage.LETHAL && loadout.Lethal == gadget)));
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Description TEXT", gadget.Gadget.GadgetDesc);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item IMAGE", gadget.Gadget.IconLink);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item TEXT", gadget.Gadget.GadgetName);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item ProsCons", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Description TEXT", true);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Credits", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Owned", false);
        SendRarityName("SERVER Item Rarity TEXT", gadget.Gadget.GadgetRarity);
    }

    public void ShowCard(LoadoutCard card)
    {
        SelectedItemID = card.Card.CardID;
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
        {
            Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
            return;
        }

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy BUTTON", !card.IsBought);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy Locked", !card.IsUnlocked && card.Card.LevelRequirement > PlayerData.Level);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy TEXT", $"BUY {Utility.GetCurrencySymbol(ECurrency.CREDIT)} <color={(PlayerData.Credits >= card.Card.BuyPrice ? "#9CFF84" : "#FF6E6E")}>{card.Card.BuyPrice}</color>");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Unlock BUTTON", !card.IsBought && !card.IsUnlocked && card.Card.LevelRequirement > PlayerData.Level);
        var coins = card.Card.GetCoins(PlayerData.Level);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Unlock TEXT", $"UNLOCK {Utility.GetCurrencySymbol(ECurrency.COIN)} <color={(PlayerData.Coins >= coins ? "#9CFF84" : "#FF6E6E")}>{coins}</color>");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Equip BUTTON", card.IsBought && loadout.Card != card);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Dequip BUTTON", card.IsBought && loadout.Card == card);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Description TEXT", card.Card.CardDesc);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Card IMAGE", card.Card.IconLink);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item TEXT", card.Card.CardName);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item ProsCons", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Description TEXT", true);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Credits", !string.IsNullOrEmpty(card.Card.AuthorCredits));
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Credits TEXT", card.Card.AuthorCredits);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Owned", card.Card.UnboxedAmount > 0);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Owned TEXT", $"Card owned by {card.Card.UnboxedAmount} players");
        SendRarityName("SERVER Item Rarity TEXT", card.Card.CardRarity);
    }

    public void ShowGlove(LoadoutGlove glove)
    {
        SelectedItemID = glove.Glove.GloveID;
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
        {
            Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
            return;
        }

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy BUTTON", !glove.IsBought);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy Locked", !glove.IsUnlocked && glove.Glove.LevelRequirement > PlayerData.Level);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy TEXT", $"BUY {Utility.GetCurrencySymbol(ECurrency.CREDIT)} <color={(PlayerData.Credits >= glove.Glove.BuyPrice ? "#9CFF84" : "#FF6E6E")}>{glove.Glove.BuyPrice}</color>");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Unlock BUTTON", !glove.IsBought && !glove.IsUnlocked && glove.Glove.LevelRequirement > PlayerData.Level);
        var coins = glove.Glove.GetCoins(PlayerData.Level);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Unlock TEXT", $"UNLOCK {Utility.GetCurrencySymbol(ECurrency.COIN)} <color={(PlayerData.Coins >= coins ? "#9CFF84" : "#FF6E6E")}>{coins}</color>");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Equip BUTTON", glove.IsBought && loadout.Glove != glove);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Dequip BUTTON", glove.IsBought && loadout.Glove == glove);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Description TEXT", glove.Glove.GloveDesc);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item IMAGE", glove.Glove.IconLink);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item TEXT", glove.Glove.GloveName);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item ProsCons", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Description TEXT", true);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Credits", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Owned", glove.Glove.UnboxedAmount > 0);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Owned TEXT", $"Glove owned by {glove.Glove.UnboxedAmount} players");
        SendRarityName("SERVER Item Rarity TEXT", glove.Glove.GloveRarity);
    }

    public void ShowKillstreak(LoadoutKillstreak killstreak)
    {
        SelectedItemID = killstreak.Killstreak.KillstreakID;
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
        {
            Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
            return;
        }

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy BUTTON", !killstreak.IsBought);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy Locked", !killstreak.IsUnlocked && killstreak.Killstreak.LevelRequirement > PlayerData.Level);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Buy TEXT", $"BUY {Utility.GetCurrencySymbol(ECurrency.CREDIT)} <color={(PlayerData.Credits >= killstreak.Killstreak.BuyPrice ? "#9CFF84" : "#FF6E6E")}>{killstreak.Killstreak.BuyPrice}</color>");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Unlock BUTTON", !killstreak.IsBought && !killstreak.IsUnlocked && killstreak.Killstreak.LevelRequirement > PlayerData.Level);
        var coins = killstreak.Killstreak.GetCoins(PlayerData.Level);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Unlock TEXT", $"UNLOCK {Utility.GetCurrencySymbol(ECurrency.COIN)} <color={(PlayerData.Coins >= coins ? "#9CFF84" : "#FF6E6E")}>{coins}</color>");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Equip BUTTON", killstreak.IsBought && !loadout.Killstreaks.Contains(killstreak));
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Dequip BUTTON", killstreak.IsBought && loadout.Killstreaks.Contains(killstreak));
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Description TEXT", killstreak.Killstreak.KillstreakDesc);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item IMAGE", killstreak.Killstreak.IconLink);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item TEXT", killstreak.Killstreak.KillstreakName);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item ProsCons", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Description TEXT", true);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Credits", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Item Owned", false);
        
        SendRarityName("SERVER Item Rarity TEXT", killstreak.Killstreak.KillstreakRarity);
    }

    public void SendRarity(string objectName, ERarity rarity, int selected) => EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"{objectName} {rarity} {selected}", true);

    public void SendRarityName(string objectName, ERarity rarity)
    {
        var rarityName = rarity switch
        {
            ERarity.GREEN => "ACHIEVEMENT",
            ERarity.YELLOW => "SPECIAL",
            ERarity.CYAN => "LIMITED",
            _ => rarity.ToString()
        };

        // SERVER Item Rarity TEXT
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, objectName, $"<color={Utility.GetRarityColor(rarity)}>{rarityName}</color>");
    }

    public void BuySelectedItem()
    {
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
        {
            Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
            return;
        }

        switch (LoadoutPage)
        {
            case ELoadoutPage.PRIMARY:
            case ELoadoutPage.SECONDARY:
            {
                if (!PlayerLoadout.Guns.TryGetValue((ushort)SelectedItemID, out var gun))
                {
                    Logging.Debug($"Error finding gun with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                if (PlayerData.Credits >= gun.Gun.BuyPrice && !gun.IsBought)
                {
                    if (DB.UpdatePlayerGunBought(Player.CSteamID, gun.Gun.GunID, true))
                    {
                        DB.DecreasePlayerCredits(Player.CSteamID, gun.Gun.BuyPrice);
                        EquipSelectedItem();
                        BackToLoadout();
                    }
                }
                else
                    SendNotEnoughCurrencyModal(ECurrency.CREDIT);

                break;
            }

            case ELoadoutPage.ATTACHMENT_PRIMARY_BARREL:
            case ELoadoutPage.ATTACHMENT_PRIMARY_GRIP:
            case ELoadoutPage.ATTACHMENT_PRIMARY_MAGAZINE:
            case ELoadoutPage.ATTACHMENT_PRIMARY_SIGHTS:
            {
                if (!PlayerLoadout.Guns.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out var gun))
                {
                    Logging.Debug($"Error finding primary with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                if (!gun.Attachments.TryGetValue((ushort)SelectedItemID, out var attachment))
                {
                    Logging.Debug($"Error finding attachment with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                if (PlayerData.Credits >= attachment.Attachment.BuyPrice && !attachment.IsBought)
                {
                    if (DB.UpdatePlayerGunAttachmentBought(Player.CSteamID, gun.Gun.GunID, attachment.Attachment.AttachmentID, true))
                    {
                        DB.DecreasePlayerCredits(Player.CSteamID, attachment.Attachment.BuyPrice);
                        EquipSelectedItem();
                        BackToLoadout();
                    }
                }
                else
                    SendNotEnoughCurrencyModal(ECurrency.CREDIT);

                break;
            }

            case ELoadoutPage.ATTACHMENT_SECONDARY_BARREL:
            case ELoadoutPage.ATTACHMENT_SECONDARY_MAGAZINE:
            case ELoadoutPage.ATTACHMENT_SECONDARY_SIGHTS:
            {
                if (!PlayerLoadout.Guns.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out var gun))
                {
                    Logging.Debug($"Error finding secondary with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                if (!gun.Attachments.TryGetValue((ushort)SelectedItemID, out var attachment))
                {
                    Logging.Debug($"Error finding attachment with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                if (PlayerData.Credits >= attachment.Attachment.BuyPrice && !attachment.IsBought)
                {
                    if (DB.UpdatePlayerGunAttachmentBought(Player.CSteamID, gun.Gun.GunID, attachment.Attachment.AttachmentID, true))
                    {
                        DB.DecreasePlayerCredits(Player.CSteamID, attachment.Attachment.BuyPrice);
                        EquipSelectedItem();
                        BackToLoadout();
                    }
                }
                else
                    SendNotEnoughCurrencyModal(ECurrency.CREDIT);

                break;
            }

            case ELoadoutPage.ATTACHMENT_PRIMARY_CHARM:
            case ELoadoutPage.ATTACHMENT_SECONDARY_CHARM:
            {
                if (!PlayerLoadout.GunCharms.TryGetValue((ushort)SelectedItemID, out var gunCharm))
                {
                    Logging.Debug($"Error finding gun charm with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                // buy gun charm
                if (PlayerData.Credits >= gunCharm.GunCharm.BuyPrice && !gunCharm.IsBought)
                {
                    if (DB.UpdatePlayerGunCharmBought(Player.CSteamID, gunCharm.GunCharm.CharmID, true))
                    {
                        DB.DecreasePlayerCredits(Player.CSteamID, gunCharm.GunCharm.BuyPrice);
                        EquipSelectedItem();
                        BackToLoadout();
                    }
                }
                else
                    SendNotEnoughCurrencyModal(ECurrency.CREDIT);

                break;
            }

            case ELoadoutPage.KNIFE:
            {
                if (!PlayerLoadout.Knives.TryGetValue((ushort)SelectedItemID, out var knife))
                {
                    Logging.Debug($"Error finding knife with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                // buy knife
                if (PlayerData.Credits >= knife.Knife.BuyPrice && !knife.IsBought)
                {
                    if (DB.UpdatePlayerKnifeBought(Player.CSteamID, knife.Knife.KnifeID, true))
                    {
                        DB.DecreasePlayerCredits(Player.CSteamID, knife.Knife.BuyPrice);
                        EquipSelectedItem();
                        BackToLoadout();
                    }
                }
                else
                    SendNotEnoughCurrencyModal(ECurrency.CREDIT);

                break;
            }

            case ELoadoutPage.TACTICAL:
            case ELoadoutPage.LETHAL:
            {
                if (!PlayerLoadout.Gadgets.TryGetValue((ushort)SelectedItemID, out var gadget))
                {
                    Logging.Debug($"Error finding gadget with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                if (PlayerData.Credits >= gadget.Gadget.BuyPrice && !gadget.IsBought)
                {
                    if (DB.UpdatePlayerGadgetBought(Player.CSteamID, gadget.Gadget.GadgetID, true))
                    {
                        DB.DecreasePlayerCredits(Player.CSteamID, gadget.Gadget.BuyPrice);
                        EquipSelectedItem();
                        BackToLoadout();
                    }
                }
                else
                    SendNotEnoughCurrencyModal(ECurrency.CREDIT);

                break;
            }

            case ELoadoutPage.PERK1:
            case ELoadoutPage.PERK2:
            case ELoadoutPage.PERK3:
            {
                if (!PlayerLoadout.Perks.TryGetValue((int)SelectedItemID, out var perk))
                {
                    Logging.Debug($"Error finding perk with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                // buy perk
                if (PlayerData.Credits >= perk.Perk.BuyPrice && !perk.IsBought)
                {
                    if (DB.UpdatePlayerPerkBought(Player.CSteamID, perk.Perk.PerkID, true))
                    {
                        DB.DecreasePlayerCredits(Player.CSteamID, perk.Perk.BuyPrice);
                        EquipSelectedItem();
                        BackToLoadout();
                    }
                }
                else
                    SendNotEnoughCurrencyModal(ECurrency.CREDIT);

                break;
            }

            case ELoadoutPage.KILLSTREAK:
            {
                if (!PlayerLoadout.Killstreaks.TryGetValue((int)SelectedItemID, out var killstreak))
                {
                    Logging.Debug($"Error finding killstreak with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                // buy killstreak
                if (PlayerData.Credits >= killstreak.Killstreak.BuyPrice && !killstreak.IsBought)
                {
                    if (DB.UpdatePlayerKillstreakBought(Player.CSteamID, killstreak.Killstreak.KillstreakID, true))
                    {
                        DB.DecreasePlayerCredits(Player.CSteamID, killstreak.Killstreak.BuyPrice);
                        EquipSelectedItem();
                        BackToLoadout();
                    }
                }
                else
                    SendNotEnoughCurrencyModal(ECurrency.CREDIT);

                break;
            }

            case ELoadoutPage.CARD:
            {
                if (!PlayerLoadout.Cards.TryGetValue((int)SelectedItemID, out var card))
                {
                    Logging.Debug($"Error finding card with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                // buy card
                if (PlayerData.Credits >= card.Card.BuyPrice && !card.IsBought)
                {
                    if (DB.UpdatePlayerCardBought(Player.CSteamID, card.Card.CardID, true))
                    {
                        DB.DecreasePlayerCredits(Player.CSteamID, card.Card.BuyPrice);
                        EquipSelectedItem();
                        BackToLoadout();
                    }
                }
                else
                    SendNotEnoughCurrencyModal(ECurrency.CREDIT);

                break;
            }

            case ELoadoutPage.GLOVE:
            {
                if (!PlayerLoadout.Gloves.TryGetValue((int)SelectedItemID, out var glove))
                {
                    Logging.Debug($"Error finding glove with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                // buy glove
                if (PlayerData.Credits >= glove.Glove.BuyPrice && !glove.IsBought)
                {
                    if (DB.UpdatePlayerGloveBought(Player.CSteamID, glove.Glove.GloveID, true))
                    {
                        DB.DecreasePlayerCredits(Player.CSteamID, glove.Glove.BuyPrice);
                        EquipSelectedItem();
                        BackToLoadout();
                    }
                }
                else
                    SendNotEnoughCurrencyModal(ECurrency.CREDIT);

                break;
            }
        }
    }

    public void UnlockSelectedItem()
    {
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
        {
            Logging.Debug($"Error finding loadout with id {LoadoutID} for {Player.CharacterName}");
            return;
        }

        switch (LoadoutPage)
        {
            case ELoadoutPage.PRIMARY:
            case ELoadoutPage.SECONDARY:
            {
                if (!PlayerLoadout.Guns.TryGetValue((ushort)SelectedItemID, out var gun))
                {
                    Logging.Debug($"Error finding gun with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                var cost = gun.Gun.GetCoins(PlayerData.Level);
                if (PlayerData.Coins >= cost && !gun.IsBought && !gun.IsUnlocked && gun.Gun.LevelRequirement > PlayerData.Level)
                {
                    if (DB.UpdatePlayerGunUnlocked(Player.CSteamID, gun.Gun.GunID, true))
                    {
                        DB.DecreasePlayerCoins(Player.CSteamID, cost);
                        ReloadSelectedItem();
                        ReloadLoadoutTab();
                    }
                }
                else
                    SendNotEnoughCurrencyModal(ECurrency.COIN);

                break;
            }

            case ELoadoutPage.ATTACHMENT_PRIMARY_BARREL:
            case ELoadoutPage.ATTACHMENT_PRIMARY_GRIP:
            case ELoadoutPage.ATTACHMENT_PRIMARY_MAGAZINE:
            case ELoadoutPage.ATTACHMENT_PRIMARY_SIGHTS:
            {
                if (!PlayerLoadout.Guns.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out var gun))
                {
                    Logging.Debug($"Error finding primary with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                if (!gun.Attachments.TryGetValue((ushort)SelectedItemID, out var attachment))
                {
                    Logging.Debug($"Error finding attachment with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                var cost = attachment.GetCoins(gun.Level);
                if (PlayerData.Coins >= cost && !attachment.IsBought && !attachment.IsUnlocked && attachment.LevelRequirement > gun.Level)
                {
                    if (DB.UpdatePlayerGunAttachmentUnlocked(Player.CSteamID, gun.Gun.GunID, attachment.Attachment.AttachmentID, true))
                    {
                        DB.DecreasePlayerCoins(Player.CSteamID, cost);
                        ReloadSelectedItem();
                        ReloadLoadoutTab();
                    }
                }
                else
                    SendNotEnoughCurrencyModal(ECurrency.COIN);

                break;
            }

            case ELoadoutPage.ATTACHMENT_SECONDARY_BARREL:
            case ELoadoutPage.ATTACHMENT_SECONDARY_MAGAZINE:
            case ELoadoutPage.ATTACHMENT_SECONDARY_SIGHTS:
            {
                if (!PlayerLoadout.Guns.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out var gun))
                {
                    Logging.Debug($"Error finding secondary with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                if (!gun.Attachments.TryGetValue((ushort)SelectedItemID, out var attachment))
                {
                    Logging.Debug($"Error finding attachment with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                var cost = attachment.GetCoins(gun.Level);
                if (PlayerData.Coins >= cost && !attachment.IsBought && !attachment.IsUnlocked && attachment.LevelRequirement > gun.Level)
                {
                    if (DB.UpdatePlayerGunAttachmentUnlocked(Player.CSteamID, gun.Gun.GunID, attachment.Attachment.AttachmentID, true))
                    {
                        DB.DecreasePlayerCoins(Player.CSteamID, cost);
                        ReloadSelectedItem();
                        ReloadLoadoutTab();
                    }
                }
                else
                    SendNotEnoughCurrencyModal(ECurrency.COIN);

                break;
            }

            case ELoadoutPage.ATTACHMENT_PRIMARY_CHARM:
            case ELoadoutPage.ATTACHMENT_SECONDARY_CHARM:
            {
                if (!PlayerLoadout.GunCharms.TryGetValue((ushort)SelectedItemID, out var gunCharm))
                {
                    Logging.Debug($"Error finding gun charm with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                var cost = gunCharm.GunCharm.GetCoins(PlayerData.Level);
                if (PlayerData.Coins >= cost && !gunCharm.IsBought && !gunCharm.IsUnlocked && gunCharm.GunCharm.LevelRequirement > PlayerData.Level)
                {
                    if (DB.UpdatePlayerGunCharmUnlocked(Player.CSteamID, gunCharm.GunCharm.CharmID, true))
                    {
                        DB.DecreasePlayerCoins(Player.CSteamID, cost);
                        ReloadSelectedItem();
                        ReloadLoadoutTab();
                    }
                }
                else
                    SendNotEnoughCurrencyModal(ECurrency.COIN);

                break;
            }

            case ELoadoutPage.KNIFE:
            {
                if (!PlayerLoadout.Knives.TryGetValue((ushort)SelectedItemID, out var knife))
                {
                    Logging.Debug($"Error finding knife with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                var cost = knife.Knife.GetCoins(PlayerData.Level);
                if (PlayerData.Coins >= cost && !knife.IsBought && !knife.IsUnlocked && knife.Knife.LevelRequirement > PlayerData.Level)
                {
                    if (DB.UpdatePlayerKnifeUnlocked(Player.CSteamID, knife.Knife.KnifeID, true))
                    {
                        DB.DecreasePlayerCoins(Player.CSteamID, cost);
                        ReloadSelectedItem();
                        ReloadLoadoutTab();
                    }
                }
                else
                    SendNotEnoughCurrencyModal(ECurrency.COIN);

                break;
            }

            case ELoadoutPage.TACTICAL:
            case ELoadoutPage.LETHAL:
            {
                if (!PlayerLoadout.Gadgets.TryGetValue((ushort)SelectedItemID, out var gadget))
                {
                    Logging.Debug($"Error finding gadget with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                var cost = gadget.Gadget.GetCoins(PlayerData.Level);
                if (PlayerData.Coins >= cost && !gadget.IsBought && !gadget.IsUnlocked && gadget.Gadget.LevelRequirement > PlayerData.Level)
                {
                    if (DB.UpdatePlayerGadgetUnlocked(Player.CSteamID, gadget.Gadget.GadgetID, true))
                    {
                        DB.DecreasePlayerCoins(Player.CSteamID, cost);
                        ReloadSelectedItem();
                        ReloadLoadoutTab();
                    }
                }
                else
                    SendNotEnoughCurrencyModal(ECurrency.COIN);

                break;
            }

            case ELoadoutPage.PERK1:
            case ELoadoutPage.PERK2:
            case ELoadoutPage.PERK3:
            {
                if (!PlayerLoadout.Perks.TryGetValue((int)SelectedItemID, out var perk))
                {
                    Logging.Debug($"Error finding perk with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                var cost = perk.Perk.GetCoins(PlayerData.Level);
                if (PlayerData.Coins >= cost && !perk.IsBought && !perk.IsUnlocked && perk.Perk.LevelRequirement > PlayerData.Level)
                {
                    if (DB.UpdatePlayerPerkUnlocked(Player.CSteamID, perk.Perk.PerkID, true))
                    {
                        DB.DecreasePlayerCoins(Player.CSteamID, cost);
                        ReloadSelectedItem();
                        ReloadLoadoutTab();
                    }
                }
                else
                    SendNotEnoughCurrencyModal(ECurrency.COIN);

                break;
            }

            case ELoadoutPage.KILLSTREAK:
            {
                if (!PlayerLoadout.Killstreaks.TryGetValue((int)SelectedItemID, out var killstreak))
                {
                    Logging.Debug($"Error finding killstreak with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                var cost = killstreak.Killstreak.GetCoins(PlayerData.Level);
                if (PlayerData.Coins >= cost && !killstreak.IsBought && !killstreak.IsUnlocked && killstreak.Killstreak.LevelRequirement > PlayerData.Level)
                {
                    if (DB.UpdatePlayerKillstreakUnlocked(Player.CSteamID, killstreak.Killstreak.KillstreakID, true))
                    {
                        DB.DecreasePlayerCoins(Player.CSteamID, cost);
                        ReloadSelectedItem();
                        ReloadLoadoutTab();
                    }
                }
                else
                    SendNotEnoughCurrencyModal(ECurrency.COIN);

                break;
            }

            case ELoadoutPage.CARD:
            {
                if (!PlayerLoadout.Cards.TryGetValue((int)SelectedItemID, out var card))
                {
                    Logging.Debug($"Error finding card with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                var cost = card.Card.GetCoins(PlayerData.Level);
                if (PlayerData.Coins >= cost && !card.IsBought && !card.IsUnlocked && card.Card.LevelRequirement > PlayerData.Level)
                {
                    if (DB.UpdatePlayerCardUnlocked(Player.CSteamID, card.Card.CardID, true))
                    {
                        DB.DecreasePlayerCoins(Player.CSteamID, cost);
                        ReloadSelectedItem();
                        ReloadLoadoutTab();
                    }
                }
                else
                    SendNotEnoughCurrencyModal(ECurrency.COIN);

                break;
            }

            case ELoadoutPage.GLOVE:
            {
                if (!PlayerLoadout.Gloves.TryGetValue((int)SelectedItemID, out var glove))
                {
                    Logging.Debug($"Error finding glove with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                var cost = glove.Glove.GetCoins(PlayerData.Level);
                if (PlayerData.Coins >= cost && !glove.IsBought && !glove.IsUnlocked && glove.Glove.LevelRequirement > PlayerData.Level)
                {
                    if (DB.UpdatePlayerGloveUnlocked(Player.CSteamID, glove.Glove.GloveID, true))
                    {
                        DB.DecreasePlayerCoins(Player.CSteamID, cost);
                        ReloadSelectedItem();
                        ReloadLoadoutTab();
                    }
                }
                else
                    SendNotEnoughCurrencyModal(ECurrency.COIN);

                break;
            }
        }
    }

    public void EquipSelectedItem()
    {
        var loadoutManager = Plugin.Instance.Loadout;
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
        {
            Logging.Debug($"Error finding selected loadout with id {LoadoutID} for {Player.CharacterName}");
            return;
        }

        switch (LoadoutPage)
        {
            case ELoadoutPage.PRIMARY_SKIN:
            {
                if (!PlayerLoadout.GunSkinsSearchByID.TryGetValue((int)SelectedItemID, out var skin))
                {
                    Logging.Debug($"Error finding gun skin with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                loadoutManager.EquipGunSkin(Player, LoadoutID, skin.ID, true);
                BackToLoadout();
                break;
            }

            case ELoadoutPage.SECONDARY_SKIN:
            {
                if (!PlayerLoadout.GunSkinsSearchByID.TryGetValue((int)SelectedItemID, out var skin))
                {
                    Logging.Debug($"Error finding gun skin with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                loadoutManager.EquipGunSkin(Player, LoadoutID, skin.ID, false);
                BackToLoadout();
                break;
            }

            case ELoadoutPage.PRIMARY:
            {
                if (!PlayerLoadout.Guns.TryGetValue((ushort)SelectedItemID, out var gun))
                {
                    Logging.Debug($"Error finding gun with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                if (gun.IsBought)
                {
                    loadoutManager.EquipGun(Player, LoadoutID, gun.Gun.GunID, true);
                    ReloadLoadoutTab();
                    ReloadSelectedItem();
                }

                break;
            }

            case ELoadoutPage.ATTACHMENT_PRIMARY_BARREL:
            case ELoadoutPage.ATTACHMENT_PRIMARY_GRIP:
            case ELoadoutPage.ATTACHMENT_PRIMARY_MAGAZINE:
            case ELoadoutPage.ATTACHMENT_PRIMARY_SIGHTS:
            {
                if (!PlayerLoadout.Guns.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out var gun))
                {
                    Logging.Debug($"Error finding gun with id {loadout.Primary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                    return;
                }

                if (!gun.Attachments.TryGetValue((ushort)SelectedItemID, out var attachment))
                {
                    Logging.Debug($"Error finding attachment with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                if (attachment.IsBought)
                {
                    loadoutManager.EquipAttachment(Player, attachment.Attachment.AttachmentID, LoadoutID, true);
                    ReloadLoadoutTab();
                    ReloadSelectedItem();
                }

                break;
            }

            case ELoadoutPage.SECONDARY:
            {
                if (!PlayerLoadout.Guns.TryGetValue((ushort)SelectedItemID, out var gun))
                {
                    Logging.Debug($"Error finding primary with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                if (gun.IsBought)
                {
                    loadoutManager.EquipGun(Player, LoadoutID, gun.Gun.GunID, false);
                    ReloadLoadoutTab();
                    ReloadSelectedItem();
                }

                break;
            }

            case ELoadoutPage.ATTACHMENT_SECONDARY_BARREL:
            case ELoadoutPage.ATTACHMENT_SECONDARY_MAGAZINE:
            case ELoadoutPage.ATTACHMENT_SECONDARY_SIGHTS:
            {
                if (!PlayerLoadout.Guns.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out var gun))
                {
                    Logging.Debug($"Error finding secondary with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                    return;
                }

                if (!gun.Attachments.TryGetValue((ushort)SelectedItemID, out var attachment))
                {
                    Logging.Debug($"Error finding attachment with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                if (attachment.IsBought)
                {
                    loadoutManager.EquipAttachment(Player, attachment.Attachment.AttachmentID, LoadoutID, false);
                    ReloadLoadoutTab();
                    ReloadSelectedItem();
                }

                break;
            }

            case ELoadoutPage.ATTACHMENT_PRIMARY_CHARM:
            case ELoadoutPage.ATTACHMENT_SECONDARY_CHARM:
            {
                if (!PlayerLoadout.GunCharms.TryGetValue((ushort)SelectedItemID, out var gunCharm))
                {
                    Logging.Debug($"Error finding gun charm with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                if (gunCharm.IsBought)
                {
                    loadoutManager.EquipGunCharm(Player, LoadoutID, gunCharm.GunCharm.CharmID, IsAttachmentPagePrimary());
                    BackToLoadout();
                }

                break;
            }

            case ELoadoutPage.TACTICAL:
            {
                if (!PlayerLoadout.Gadgets.TryGetValue((ushort)SelectedItemID, out var gadget))
                {
                    Logging.Debug($"Error finding tactical with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                if (gadget.IsBought)
                {
                    loadoutManager.EquipTactical(Player, LoadoutID, gadget.Gadget.GadgetID);
                    BackToLoadout();
                }

                break;
            }

            case ELoadoutPage.LETHAL:
            {
                if (!PlayerLoadout.Gadgets.TryGetValue((ushort)SelectedItemID, out var gadget))
                {
                    Logging.Debug($"Error finding lethal with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                if (gadget.IsBought)
                {
                    loadoutManager.EquipLethal(Player, LoadoutID, gadget.Gadget.GadgetID);
                    BackToLoadout();
                }

                break;
            }

            case ELoadoutPage.PERK1:
            case ELoadoutPage.PERK2:
            case ELoadoutPage.PERK3:
            {
                if (!PlayerLoadout.Perks.TryGetValue((int)SelectedItemID, out var perk))
                {
                    Logging.Debug($"Error finding perk with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                if (perk.IsBought)
                {
                    loadoutManager.EquipPerk(Player, LoadoutID, perk.Perk.PerkID);
                    BackToLoadout();
                }

                break;
            }

            case ELoadoutPage.KNIFE:
            {
                if (!PlayerLoadout.Knives.TryGetValue((ushort)SelectedItemID, out var knife))
                {
                    Logging.Debug($"Error finding knife with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                if (knife.IsBought)
                {
                    loadoutManager.EquipKnife(Player, LoadoutID, knife.Knife.KnifeID);
                    BackToLoadout();
                }

                break;
            }

            case ELoadoutPage.KILLSTREAK:
            {
                if (!PlayerLoadout.Killstreaks.TryGetValue((int)SelectedItemID, out var killstreak))
                {
                    Logging.Debug($"Error finding killstreak with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                if (killstreak.IsBought)
                {
                    loadoutManager.EquipKillstreak(Player, LoadoutID, killstreak.Killstreak.KillstreakID);
                    ReloadLoadoutTab();
                    ReloadSelectedItem();
                }

                break;
            }

            case ELoadoutPage.GLOVE:
            {
                if (!PlayerLoadout.Gloves.TryGetValue((int)SelectedItemID, out var glove))
                {
                    Logging.Debug($"Error finding glove with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                if (glove.IsBought)
                {
                    loadoutManager.EquipGlove(Player, LoadoutID, glove.Glove.GloveID);
                    BackToLoadout();
                }

                break;
            }

            case ELoadoutPage.CARD:
            {
                if (!PlayerLoadout.Cards.TryGetValue((int)SelectedItemID, out var card))
                {
                    Logging.Debug($"Error finding card with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                if (card.IsBought)
                {
                    loadoutManager.EquipCard(Player, LoadoutID, card.Card.CardID);
                    BackToLoadout();
                }

                break;
            }
        }

        ReloadLoadout();
    }

    public void DequipSelectedItem()
    {
        var loadoutManager = Plugin.Instance.Loadout;
        if (!PlayerLoadout.Loadouts.TryGetValue(LoadoutID, out var loadout))
        {
            Logging.Debug($"Error finding selected loadout with id {LoadoutID} for {Player.CharacterName}");
            return;
        }

        switch (LoadoutPage)
        {
            case ELoadoutPage.PRIMARY_SKIN:
            {
                loadoutManager.DequipGunSkin(Player, LoadoutID, true);
                ReloadLoadoutTab();
                ReloadSelectedItem();
                break;
            }

            case ELoadoutPage.SECONDARY_SKIN:
            {
                loadoutManager.DequipGunSkin(Player, LoadoutID, false);
                ReloadLoadoutTab();
                ReloadSelectedItem();
                break;
            }

            case ELoadoutPage.PRIMARY:
            {
                loadoutManager.EquipGun(Player, LoadoutID, 0, true);
                break;
            }

            case ELoadoutPage.ATTACHMENT_PRIMARY_BARREL:
            case ELoadoutPage.ATTACHMENT_PRIMARY_GRIP:
            case ELoadoutPage.ATTACHMENT_PRIMARY_MAGAZINE:
            case ELoadoutPage.ATTACHMENT_PRIMARY_SIGHTS:
            {
                if (!PlayerLoadout.Guns.TryGetValue(loadout.Primary?.Gun?.GunID ?? 0, out var gun))
                {
                    Logging.Debug($"Error finding gun with id {loadout.Primary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                    return;
                }

                if (!gun.Attachments.TryGetValue((ushort)SelectedItemID, out var attachment))
                {
                    Logging.Debug($"Error finding attachment with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                loadoutManager.DequipAttachment(Player, attachment.Attachment.AttachmentID, LoadoutID, true);
                ReloadLoadoutTab();
                ReloadSelectedItem();
                break;
            }

            case ELoadoutPage.SECONDARY:
            {
                loadoutManager.EquipGun(Player, LoadoutID, 0, false);
                break;
            }

            case ELoadoutPage.ATTACHMENT_SECONDARY_BARREL:
            case ELoadoutPage.ATTACHMENT_SECONDARY_MAGAZINE:
            case ELoadoutPage.ATTACHMENT_SECONDARY_SIGHTS:
            {
                if (!PlayerLoadout.Guns.TryGetValue(loadout.Secondary?.Gun?.GunID ?? 0, out var gun))
                {
                    Logging.Debug($"Error finding secondary with id {loadout.Secondary?.Gun?.GunID ?? 0} for {Player.CharacterName}");
                    return;
                }

                if (!gun.Attachments.TryGetValue((ushort)SelectedItemID, out var attachment))
                {
                    Logging.Debug($"Error finding attachment with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                loadoutManager.DequipAttachment(Player, attachment.Attachment.AttachmentID, LoadoutID, false);
                ReloadLoadoutTab();
                ReloadSelectedItem();
                break;
            }

            case ELoadoutPage.ATTACHMENT_PRIMARY_CHARM:
            case ELoadoutPage.ATTACHMENT_SECONDARY_CHARM:
            {
                loadoutManager.EquipGunCharm(Player, LoadoutID, 0, IsAttachmentPagePrimary());
                ReloadLoadoutTab();
                ReloadSelectedItem();
                break;
            }

            case ELoadoutPage.TACTICAL:
            {
                loadoutManager.EquipTactical(Player, LoadoutID, 0);
                ReloadLoadoutTab();
                ReloadSelectedItem();
                break;
            }

            case ELoadoutPage.LETHAL:
            {
                loadoutManager.EquipLethal(Player, LoadoutID, 0);
                ReloadLoadoutTab();
                ReloadSelectedItem();
                break;
            }

            case ELoadoutPage.PERK1:
            case ELoadoutPage.PERK2:
            case ELoadoutPage.PERK3:
            {
                if (!PlayerLoadout.Perks.TryGetValue((int)SelectedItemID, out var perk))
                {
                    Logging.Debug($"Error finding perk with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                loadoutManager.DequipPerk(Player, LoadoutID, perk.Perk.PerkID);
                ReloadLoadoutTab();
                ReloadSelectedItem();
                break;
            }

            case ELoadoutPage.KNIFE:
            {
                loadoutManager.EquipKnife(Player, LoadoutID, 0);
                ReloadLoadoutTab();
                ReloadSelectedItem();
                break;
            }

            case ELoadoutPage.KILLSTREAK:
            {
                if (!PlayerLoadout.Killstreaks.TryGetValue((int)SelectedItemID, out var killstreak))
                {
                    Logging.Debug($"Error finding killstreak with id {SelectedItemID} for {Player.CharacterName}");
                    return;
                }

                loadoutManager.DequipKillstreak(Player, LoadoutID, killstreak.Killstreak.KillstreakID);
                ReloadLoadoutTab();
                ReloadSelectedItem();
                break;
            }

            case ELoadoutPage.GLOVE:
            {
                loadoutManager.EquipGlove(Player, LoadoutID, 0);
                ReloadLoadoutTab();
                ReloadSelectedItem();
                break;
            }

            case ELoadoutPage.CARD:
            {
                loadoutManager.EquipCard(Player, LoadoutID, 0);
                ReloadLoadoutTab();
                ReloadSelectedItem();
                break;
            }
        }

        ReloadLoadout();
    }

    public string GetPerkInt() => LoadoutPage.ToString().Replace("PERK", "");
    public string GetAttachmentPage() => LoadoutPage.ToString().Replace("ATTACHMENT_PRIMARY_", "").Replace("ATTACHMENT_SECONDARY_", "");
    public bool IsAttachmentPagePrimary() => (int)LoadoutPage >= MINIMUM_LOADOUT_PAGE_ATTACHMENT_PRIMARY && (int)LoadoutPage <= MAXIMUM_LOADOUT_PAGE_ATTACHMENT_PRIMARY;
    public void BackToLoadout() => EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Item Page Disabler", true);

    public void ShowLeaderboards()
    {
        MainPage = EMainPage.LEADERBOARD;

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Leaderboards Level TEXT 10", PlayerData.Level.ToString());
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Leaderboards Level IMAGE 10", DB.Levels.TryGetValue(PlayerData.Level, out var level) ? level.IconLinkMedium : "");
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Leaderboards Flag IMAGE 10", PlayerData.HideFlag ? Config.Icons.FileData.HiddenFlagIconLink : Utility.GetFlag(PlayerData.CountryCode));
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Leaderboards Name TEXT 10", (PlayerData.HasPrime ? UIManager.PRIME_SYMBOL : "") + PlayerData.SteamName);

        SelectLeaderboardPage(ELeaderboardPage.DAILY);
    }

    public void SelectLeaderboardPage(ELeaderboardPage page)
    {
        LeaderboardPage = page;
        SelectLeaderboardTab(page == ELeaderboardPage.ALL ? ELeaderboardTab.LEVEL : ELeaderboardTab.KILL);
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

        for (var i = 0; i <= 9; i++)
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards BUTTON {i}", false);

        if (data.Count == 0)
            return;

        if (dataLookup.TryGetValue(SteamID, out var playerData))
        {
            decimal kills = playerData.Kills + playerData.HeadshotKills;
            decimal deaths = playerData.Deaths;

            var ratio = playerData.Deaths == 0 ? string.Format("{0:n}", kills) : string.Format("{0:n}", Math.Round(kills / deaths, 2));

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards BUTTON 10", true);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Leaderboards Rank TEXT 10", $"#{data.IndexOf(playerData) + 1}");
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Leaderboards Kills TEXT 10", (playerData.Kills + playerData.HeadshotKills).ToString());
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Leaderboards Deaths TEXT 10", playerData.Deaths.ToString());
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Leaderboards KDR TEXT 10", ratio);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards Skins TEXT 10", playerData.Skins.ToString());
        }
        else
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards BUTTON 10", false);

        ShowLeaderboardPage(1);
    }

    public void ShowLeaderboardPage(int pageNum)
    {
        LeaderboardPageID = pageNum;
        var data = GetLeaderboardData();

        for (var i = 0; i <= 9; i++)
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards BUTTON {i}", false);

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Leaderboards Page TEXT", $"Page {pageNum}");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Leaderboards Reset TEXT", GetLeaderboardRefreshTime());

        var lowerIndex = 10 * (pageNum - 1);
        var upperIndex = Math.Min(lowerIndex + 9, data.Count - 1);

        var index = 0;
        for (var i = lowerIndex; i <= upperIndex; i++)
        {
            var playerData = data[i];
            decimal kills = playerData.Kills + playerData.HeadshotKills;
            decimal deaths = playerData.Deaths;

            var ratio = playerData.Deaths == 0 ? string.Format("{0:n}", kills) : string.Format("{0:n}", Math.Round(kills / deaths, 2));

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards BUTTON {index}", true);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards Rank TEXT {index}", $"#{i + 1}");
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards Level TEXT {index}", playerData.Level.ToString());
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards Level IMAGE {index}", DB.Levels.TryGetValue(playerData.Level, out var level) ? level.IconLinkMedium : "");
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards Flag IMAGE {index}", playerData.HideFlag ? Config.Icons.FileData.HiddenFlagIconLink : Utility.GetFlag(playerData.CountryCode));
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards Name TEXT {index}", (playerData.HasPrime ? UIManager.PRIME_SYMBOL : "") + playerData.SteamName);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards Kills TEXT {index}", (playerData.Kills + playerData.HeadshotKills).ToString());
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards Deaths TEXT {index}", playerData.Deaths.ToString());
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards KDR TEXT {index}", ratio);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards Skins TEXT {index}", playerData.Skins.ToString());
            index++;
        }
    }

    public void ForwardLeaderboardPage()
    {
        var data = GetLeaderboardData();

        if (Mathf.CeilToInt(data.Count / 10f) == LeaderboardPageID)
        {
            ShowLeaderboardPage(1);
            return;
        }

        ShowLeaderboardPage(LeaderboardPageID + 1);
    }

    public void BackwardLeaderboardPage()
    {
        var data = GetLeaderboardData();
        if (LeaderboardPageID == 1)
        {
            ShowLeaderboardPage(Mathf.CeilToInt(data.Count / 10f));
            return;
        }

        ShowLeaderboardPage(LeaderboardPageID - 1);
    }

    public void ForwardLeaderboardPageFast()
    {
        var data = GetLeaderboardData();
        var maxPage = Mathf.CeilToInt(data.Count / 10f);
        if (LeaderboardPageID + 10 >= maxPage)
        {
            ShowLeaderboardPage(maxPage);
            return;
        }
        
        ShowLeaderboardPage(LeaderboardPageID + 10);
    }

    public void BackwardLeaderboardPageFast()
    {
        var data = GetLeaderboardData();
        if (LeaderboardPageID - 10 <= 1)
        {
            ShowLeaderboardPage(1);
            return;
        }
        
        ShowLeaderboardPage(LeaderboardPageID - 10);
    }

    public void ForwardLeaderboardPageEnd()
    {
        var data = GetLeaderboardData();
        ShowLeaderboardPage(Mathf.CeilToInt(data.Count / 10f));
    }

    public void BackwardLeaderboardPageEnd()
    {
        ShowLeaderboardPage(1);
    }

    public void SearchLeaderboardPlayer(string input)
    {
        var data = GetLeaderboardData();
        for (var i = 0; i <= 9; i++)
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards BUTTON {i}", false);

        var inputLower = input.ToLower();
        var searchPlayers = data.Where(k => k.SteamName.ToLower().Contains(inputLower)).Take(10).ToList();

        var maxCount = Math.Min(10, searchPlayers.Count);
        for (var i = 0; i < maxCount; i++)
        {
            var playerData = searchPlayers[i];
            decimal kills = playerData.Kills + playerData.HeadshotKills;
            decimal deaths = playerData.Deaths;

            var ratio = playerData.Deaths == 0 ? $"{kills:n}" : $"{Math.Round(kills / deaths, 2):n}";

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards BUTTON {i}", true);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards Rank TEXT {i}", $"#{data.IndexOf(playerData) + 1}");
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards Level TEXT {i}", playerData.Level.ToString());
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards Level IMAGE {i}", DB.Levels.TryGetValue(playerData.Level, out var level) ? level.IconLinkMedium : "");
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards Flag IMAGE {i}", playerData.HideFlag ? Config.Icons.FileData.HiddenFlagIconLink : Utility.GetFlag(playerData.CountryCode));
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards Name TEXT {i}", playerData.SteamName);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards Kills TEXT {i}", (playerData.Kills + playerData.HeadshotKills).ToString());
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards Deaths TEXT {i}", playerData.Deaths.ToString());
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards KDR TEXT {i}", ratio);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Leaderboards Skins TEXT {i}", playerData.Skins.ToString());
        }
    }

    private List<LeaderboardData> GetLeaderboardData() => LeaderboardPage switch
    {
        ELeaderboardPage.DAILY => DB.PlayerDailyLeaderboard,
        ELeaderboardPage.WEEKLY => DB.PlayerWeeklyLeaderboard,
        ELeaderboardPage.SEASONAL => DB.PlayerSeasonalLeaderboard,
        ELeaderboardPage.ALL => LeaderboardTab switch
        {
            ELeaderboardTab.KILL => DB.PlayerAllTimeKill,
            ELeaderboardTab.SKINS => DB.PlayerAllTimeSkins,
            ELeaderboardTab.LEVEL => DB.PlayerAllTimeLevel,
            _ => throw new ArgumentOutOfRangeException()
        },
        var _ => throw new ArgumentOutOfRangeException(nameof(LeaderboardPage), "Value is not as expected")
    };

    private Dictionary<CSteamID, LeaderboardData> GetLeaderboardDataLookup() => LeaderboardPage switch
    {
        ELeaderboardPage.DAILY => DB.PlayerDailyLeaderboardLookup,
        ELeaderboardPage.WEEKLY => DB.PlayerWeeklyLeaderboardLookup,
        ELeaderboardPage.SEASONAL => DB.PlayerSeasonalLeaderboardLookup,
        ELeaderboardPage.ALL => DB.PlayerAllTimeLeaderboardLookup,
        var _ => throw new ArgumentOutOfRangeException(nameof(LeaderboardPage), "Value is not as expected")
    };

    private string GetLeaderboardRefreshTime() => LeaderboardPage switch
    {
        ELeaderboardPage.DAILY => DB.ServerOptions.DailyLeaderboardWipe.UtcDateTime > DateTime.UtcNow ? (DB.ServerOptions.DailyLeaderboardWipe.UtcDateTime - DateTime.UtcNow).ToString(@"hh\:mm\:ss") : "Soon...",
        ELeaderboardPage.WEEKLY => DB.ServerOptions.WeeklyLeaderboardWipe.UtcDateTime > DateTime.UtcNow ? (DB.ServerOptions.WeeklyLeaderboardWipe.UtcDateTime - DateTime.UtcNow).ToString(@"dd\:hh\:mm\:ss") : "Soon...",
        var _ => "00:00:00"
    };

    public void ShowQuests()
    {
        var quests = PlayerData.Quests.OrderBy(k => (int)k.Quest.QuestTier).ToList();
        var maxCount = Math.Min(6, quests.Count);
        for (var i = 0; i < maxCount; i++)
        {
            var quest = quests[i];
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Quest Complete {i} Toggler", quest.Amount >= quest.Quest.TargetAmount);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Quest Description TEXT {i}", quest.Quest.QuestDesc);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Quest Title TEXT {i}", quest.Quest.QuestTitle);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Quest Target TEXT {i}", $"{quest.Amount}/{quest.Quest.TargetAmount}");
            var updatedXP = (int)Math.Floor(quest.Quest.XP * (1f + PlayerData.BPBooster + DB.ServerOptions.BPBooster + (PlayerData.HasPrime ? Config.WinningValues.FileData.PrimeBPXPBooster : 0f) + (PlayerData.HasBattlepass ? Config.WinningValues.FileData.PremiumBattlepassBooster : 0f)));
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Quest Reward TEXT {i}", $"+{updatedXP}★");
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Quest Bar Fill {i}", quest.Amount == 0 ? UIManager.HAIRSPACE_SYMBOL_STRING : new(UIManager.HAIRSPACE_SYMBOL_CHAR, Math.Min(256, quest.Amount * 256 / quest.Quest.TargetAmount)));
        }
        
        ShowQuestCompletion();
    }

    public void ShowQuestCompletion()
    {
        var completedQuests = PlayerData.Quests.Count(k => k.Amount >= k.Quest.TargetAmount);
        var totalQuests = PlayerData.Quests.Count;

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Quest Complete", completedQuests == totalQuests);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Quest Complete Count TEXT", $"{completedQuests}/{totalQuests}");
    }

    public void ShowAchievements()
    {
        MainPage = EMainPage.ACHIEVEMENTS;
        SelectedAchievementMainPage(1);

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Achievements Page 1 TEXT", "Weapons");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Achievements Page 2 TEXT", "Other");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Achievements Page 3 BUTTON", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Achievements Page 4 BUTTON", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Achievements Page 5 BUTTON", false);
    }

    public void SelectedAchievementMainPage(int mainPage)
    {
        AchievementMainPage = mainPage;
        AchievementPageShower.Stop();
        
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Achievements Previous BUTTON", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Achievements Next BUTTON", false);

        if (!AchievementPages.TryGetValue(mainPage, out var achievementPages))
        {
            Logging.Debug($"Error finding achievement pages for main page {mainPage}");
            return;
        }

        if (!achievementPages.TryGetValue(1, out var firstPage))
        {
            Logging.Debug($"Error finding first page of achievement of main page {mainPage}");
            return;
        }

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Achievements Previous BUTTON", achievementPages.Count > 1);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Achievements Next BUTTON", achievementPages.Count > 1);

        AchievementPageShower = Plugin.Instance.StartCoroutine(ShowAchievementSubPage(firstPage));
    }

    public IEnumerator ShowAchievementSubPage(PageAchievement page)
    {
        AchievementSubPage = page.PageID;
        Logging.Debug($"Showing achievement page to {Player.CharacterName} with main page {AchievementMainPage} and sub page {AchievementSubPage}");

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Achievements Page TEXT", $"Page {page.PageID}");

        for (var i = 0; i <= 48; i++)
        {
            yield return new WaitForSeconds(0.01f);

            if (!page.Achievements.TryGetValue(i, out var achievement))
                break;

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Achievements BUTTON {i}", true);
            var tier = achievement.GetCurrentTier();
            if (tier != null)
                EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Achievements IMAGE {i}", tier.TierPrevLarge);

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Achievements Basic {i}", achievement.CurrentTier == 0);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Achievements Bronze {i}", achievement.CurrentTier == 1);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Achievements Silver {i}", achievement.CurrentTier == 2);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Achievements Gold {i}", achievement.CurrentTier == 3);

            if (achievement.TryGetNextTier(out var nextTier))
            {
                var fillTxt = achievement.Amount == 0 ? UIManager.VERY_SMALL_SQUARE : new(UIManager.HAIRSPACE_SYMBOL_CHAR, Math.Min(68, achievement.Amount * 68 / nextTier.TargetAmount));

                switch (achievement.CurrentTier)
                {
                    case 0:
                        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Achievements Basic Fill {i}", fillTxt);
                        break;
                    case 1:
                        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Achievements Bronze Fill {i}", fillTxt);
                        break;
                    case 2:
                        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Achievements Silver Fill {i}", fillTxt);
                        break;
                    case 3:
                        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Achievements Gold Fill {i}", fillTxt);
                        break;
                }
            }

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Achievements Claimable {i}", nextTier != null && achievement.Amount >= nextTier.TargetAmount);
        }
    }

    public void ForwardAchievementSubPage()
    {
        if (!AchievementPages.TryGetValue(AchievementMainPage, out var achievementPages))
        {
            Logging.Debug($"Error finding achievement pages for main page {AchievementMainPage}");
            ShowAchievements();
            return;
        }

        if (!achievementPages.TryGetValue(AchievementSubPage + 1, out var nextPage) && !achievementPages.TryGetValue(1, out nextPage))
        {
            Logging.Debug($"Error finding next achievement page");
            SelectedAchievementMainPage(AchievementMainPage);
            return;
        }

        AchievementPageShower.Stop();
        AchievementPageShower = Plugin.Instance.StartCoroutine(ShowAchievementSubPage(nextPage));
    }

    public void BackwardAchievementSubPage()
    {
        if (!AchievementPages.TryGetValue(AchievementMainPage, out var achievementPages))
        {
            Logging.Debug($"Error finding achievement pages for main page {AchievementMainPage}");
            ShowAchievements();
            return;
        }

        if (!achievementPages.TryGetValue(AchievementSubPage - 1, out var nextPage) && !achievementPages.TryGetValue(achievementPages.Keys.Max(), out nextPage))
        {
            Logging.Debug("Error finding next achievement page");
            SelectedAchievementMainPage(AchievementMainPage);
            return;
        }

        AchievementPageShower.Stop();
        AchievementPageShower = Plugin.Instance.StartCoroutine(ShowAchievementSubPage(nextPage));
    }

    public void ReloadAchievementSubPage()
    {
        if (!AchievementPages.TryGetValue(AchievementMainPage, out var achievementPages) || !achievementPages.TryGetValue(AchievementSubPage, out var page))
        {
            Logging.Debug($"Unable to find selected page with main page {AchievementMainPage} and sub page {AchievementSubPage}");
            return;
        }

        AchievementPageShower.Stop();
        AchievementPageShower = Plugin.Instance.StartCoroutine(ShowAchievementSubPage(page));
    }

    public void SelectedAchievement(int selected)
    {
        if (!AchievementPages.TryGetValue(AchievementMainPage, out var achievementPages) || !achievementPages.TryGetValue(AchievementSubPage, out var page))
        {
            Logging.Debug($"Error getting current page of achievement for {Player.CharacterName}");
            return;
        }

        if (!page.Achievements.TryGetValue(selected, out var achievement))
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
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Achievements IMAGE", tier.TierPrevLarge);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Achievements TEXT", tier.TierTitle);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Achievements Description TEXT", tier.TierDesc);

            var targetAmount = tier.TargetAmount;
            if (achievement.TryGetNextTier(out var nextTier))
            {
                targetAmount = nextTier.TargetAmount;
                if (nextTier.Rewards.Count >= 1 && TryGetAchievementRewardInfo(nextTier.Rewards[0], out var rewardName, out var _, out var _))
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Achievements Item TEXT", rewardName);
                else
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Achievements Item TEXT", "None");
            }
            else
                EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Achievements Item TEXT", "None");

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Achievements Claim BUTTON", nextTier != null && achievement.Amount >= targetAmount);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Achievements Target TEXT", $"{achievement.Amount}/{targetAmount}");

            var fill = achievement.Amount == 0 ? UIManager.HAIRSPACE_SYMBOL_STRING : new(UIManager.HAIRSPACE_SYMBOL_CHAR, Math.Min(291, achievement.Amount * 291 / targetAmount));
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Achievements Fill 0", fill);
        }

        for (var i = 1; i <= 4; i++)
        {
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Achievements Reward Claimed {i}", achievement.CurrentTier >= i);

            if (!achievement.Achievement.TiersLookup.TryGetValue(i, out var rewardTier) || rewardTier.Rewards.Count == 0 || !TryGetAchievementRewardInfo(rewardTier.Rewards[0], out var _, out var rewardImage, out var _))
            {
                EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Achievements Reward IMAGE {i}", "");
                continue;
            }

            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Achievements Reward IMAGE {i}", rewardImage);
        }
    }

    public void ReloadSelectedAchievement()
    {
        if (!PlayerData.AchievementsSearchByID.TryGetValue(SelectedAchievementID, out var achievement))
        {
            Logging.Debug($"Unable to find selected achievement with id {SelectedAchievementID} for {Player.CharacterName}");
            return;
        }

        ShowAchievement(achievement);
    }

    public bool TryGetAchievementRewardInfo(Reward reward, out string rewardName, out string rewardImage, out ERarity rewardRarity)
    {
        rewardName = "";
        rewardImage = "";
        rewardRarity = ERarity.NONE;

        switch (reward.RewardType)
        {
            case ERewardType.CARD:
                if (!DB.Cards.TryGetValue(Convert.ToInt32(reward.RewardValue), out var card))
                    return false;

                rewardName = $"<color={Utility.GetRarityColor(card.CardRarity)}>{card.CardName}</color>";
                rewardImage = card.IconLink;
                rewardRarity = card.CardRarity;
                return true;
            case ERewardType.GUN_SKIN:
                if (!DB.GunSkinsSearchByID.TryGetValue(Convert.ToInt32(reward.RewardValue), out var skin))
                    return false;

                rewardName = $"<color={Utility.GetRarityColor(skin.SkinRarity)}>{skin.SkinName}</color>";
                rewardImage = skin.IconLink;
                rewardRarity = skin.SkinRarity;
                return true;
            case ERewardType.GLOVE:
                if (!DB.Gloves.TryGetValue(Convert.ToUInt16(reward.RewardValue), out var glove))
                    return false;

                rewardName = $"<color={Utility.GetRarityColor(glove.GloveRarity)}>{glove.GloveName}</color>";
                rewardImage = glove.IconLink;
                rewardRarity = glove.GloveRarity;
                return true;
            case ERewardType.GUN:
                if (!DB.Guns.TryGetValue(Convert.ToUInt16(reward.RewardValue), out var gun))
                    return false;

                rewardName = $"<color={Utility.GetRarityColor(gun.GunRarity)}>{gun.GunName}</color>";
                rewardImage = gun.IconLink;
                rewardRarity = gun.GunRarity;
                return true;
            case ERewardType.GUN_CHARM:
                if (!DB.GunCharms.TryGetValue(Convert.ToUInt16(reward.RewardValue), out var gunCharm))
                    return false;

                rewardName = $"<color={Utility.GetRarityColor(gunCharm.CharmRarity)}>{gunCharm.CharmName}</color>";
                rewardImage = gunCharm.IconLink;
                rewardRarity = gunCharm.CharmRarity;
                return true;
            case ERewardType.BP_BOOSTER:
                rewardName = $"<color=white>{string.Format("{0:0.##}", Convert.ToDecimal(reward.RewardValue) * 100)}% Battlepass Stars Boost</color>";
                rewardImage = Config.Icons.FileData.BPXPBoostIconLink;
                return true;
            case ERewardType.XP_BOOSTER:
                rewardName = $"<color=white>{string.Format("{0:0.##}", Convert.ToDecimal(reward.RewardValue) * 100)}% XP Boost</color>";
                rewardImage = Config.Icons.FileData.XPBoostIconLink;
                return true;
            case ERewardType.GUN_XP_BOOSTER:
                rewardName = $"<color=white>{string.Format("{0:0.##}", Convert.ToDecimal(reward.RewardValue) * 100)}% Gun XP Boost</color>";
                rewardImage = Config.Icons.FileData.GunXPBoostIconLink;
                return true;
            case ERewardType.COIN:
                rewardName = $"<color=white>{reward.RewardValue} Blacktags</color>";
                rewardImage = Config.Icons.FileData.BlacktagsSmallIconLink;
                return true;
            case ERewardType.CREDIT:
                rewardName = $"<color=white>{reward.RewardValue} Points</color>";
                rewardImage = Config.Icons.FileData.PointsSmallIconLink;
                return true;
            case ERewardType.LEVEL_XP:
                rewardName = $"<color=white>{reward.RewardValue} XP</color>";
                rewardImage = Config.Icons.FileData.XPIconLink;
                return true;
            case ERewardType.SCRAP:
                rewardName = $"<color=white>{reward.RewardValue} Scrap</color>";
                rewardImage = Config.Icons.FileData.ScrapSmallIconLink;
                return true;
            default:
                return false;
        }
    }

    public IEnumerator SetupBattlepass()
    {
        MainPage = EMainPage.BATTLEPASS;
        ShowBattlepass();
        // Setup all 50 objects
        for (var i = 1; i <= 50; i++)
        {
            yield return new WaitForSeconds(0.1f);
            
            ShowBattlepassTier(i);
        }

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Battlepass Claim BUTTON", false);
    }

    public void ShowBattlepass()
    {
        var bp = PlayerData.Battlepass;

        if (!DB.BattlepassTiersSearchByID.TryGetValue(bp.CurrentTier, out var currentTier))
        {
            Logging.Debug($"Error finding current battlepass tier for {Player.CharacterName}, returning");
            return;
        }

        var isBattlePassCompleted = !DB.BattlepassTiersSearchByID.TryGetValue(bp.CurrentTier + 1, out var nextTier);

        // Setup the XP bar
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Battlepass Tier Target TEXT", $"{bp.XP}/{(isBattlePassCompleted ? currentTier.XP : nextTier.XP)}★");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Battlepass Tier TEXT", $"{bp.CurrentTier}");
        var fill = bp.XP == 0 ? UIManager.VERY_SMALL_SQUARE : new(' ', Math.Min(72, bp.XP * 72 / (isBattlePassCompleted ? currentTier.XP : nextTier.XP)));
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Battlepass Tier XP Fill", fill);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Battlepass IMAGE", "");

        // Setup the preview section
        var bpExpiry = DB.ServerOptions.BattlepassExpiry;
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Battlepass Expire Timer", bpExpiry > DateTimeOffset.UtcNow);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Battlepass Expire TEXT", $"{(int)(bpExpiry - DateTimeOffset.UtcNow).TotalDays} Days");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Battlepass Buy Pass BUTTON", !PlayerData.HasBattlepass);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Battlepass Tier Skip", !isBattlePassCompleted);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Battlepass Tier Skip TEXT",
            $"{Utility.GetCurrencySymbol(ECurrency.COIN)} <color={(PlayerData.Coins >= Config.Base.FileData.BattlepassTierSkipCost ? "#9CFF84" : "#FF6E6E")}>{Config.Base.FileData.BattlepassTierSkipCost}</color>");

        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Battlepass Tier Skip IMAGE", "");
    }

    public void ShowBattlepassTier(int tierID)
    {
        var bp = PlayerData.Battlepass;
        if (!DB.BattlepassTiersSearchByID.TryGetValue(tierID, out var tier))
            return;

        var isTierUnlocked = bp.CurrentTier >= tierID;
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Battlepass Tier Completed Toggler {tierID}", isTierUnlocked);
        var spaces = bp.CurrentTier > tierID ? 70 : bp.CurrentTier == tierID ? Math.Min(70, bp.XP * 70 / (DB.BattlepassTiersSearchByID.TryGetValue(tierID + 1, out var nextTier) ? nextTier.XP : tier.XP)) : 0;
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Battlepass Tier Fill {tierID}", spaces == 0 ? UIManager.VERY_SMALL_SQUARE : new(UIManager.HAIRSPACE_SYMBOL_CHAR, spaces));

        // Setup top reward (free reward)
        var isRewardClaimed = bp.ClaimedFreeRewards.Contains(tierID);
        if (tier.FreeReward != null && TryGetBattlepassRewardInfo(tier.FreeReward, out var topRewardName, out var topRewardImage, out var topRewardRarity))
        {
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Battlepass T IMAGE {tierID}", topRewardImage);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Battlepass T TEXT {tierID}", topRewardName);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Battlepass T Locked {tierID}", !isTierUnlocked || isRewardClaimed);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Battlepass T Claimed {tierID}", isRewardClaimed);
            SendRarity("SERVER Battlepass T", topRewardRarity, tierID);
        }
        else
        {
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Battlepass T IMAGE {tierID}", "");
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Battlepass T TEXT {tierID}", " ");
            if (isTierUnlocked)
            {
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Battlepass T Locked {tierID}", true);
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Battlepass T Claimed {tierID}", true);
            }
        }

        // Setup bottom reward (premium reward)
        isRewardClaimed = bp.ClaimedPremiumRewards.Contains(tierID);
        if (tier.PremiumReward != null && TryGetBattlepassRewardInfo(tier.PremiumReward, out var bottomRewardName, out var bottomRewardImage, out var bottomRewardRarity))
        {
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Battlepass B IMAGE {tierID}", bottomRewardImage);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Battlepass B TEXT {tierID}", bottomRewardName);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Battlepass B Locked {tierID}", !PlayerData.HasBattlepass || !isTierUnlocked || isRewardClaimed);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Battlepass B Claimed {tierID}", isRewardClaimed);
            SendRarity("SERVER Battlepass B", bottomRewardRarity, tierID);
        }
        else
        {
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Battlepass B IMAGE {tierID}", "");
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Battlepass B TEXT {tierID}", " ");

            if (isTierUnlocked)
            {
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Battlepass B Locked {tierID}", true);
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Battlepass B Claimed {tierID}", true);
            }
        }
    }

    public void SelectedBattlepassTier(bool isTop, int tierID)
    {
        SelectedBattlepassTierID = (isTop, tierID);

        if (!DB.BattlepassTiersSearchByID.TryGetValue(tierID, out var tier))
        {
            Logging.Debug($"Error finding selected battlepass tier for {Player.CharacterName} with selected {tierID}");
            return;
        }

        var reward = isTop ? tier.FreeReward : tier.PremiumReward;
        if (reward == null || !TryGetBattlepassRewardInfo(reward, out var _, out var rewardImage, out var _))
            return;

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Battlepass IMAGE", reward.RewardType != ERewardType.CARD);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Battlepass Card IMAGE", reward.RewardType == ERewardType.CARD);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Battlepass IMAGE", rewardImage);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Battlepass Card IMAGE", rewardImage);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Battlepass Claim BUTTON", true);
    }

    public bool TryGetBattlepassRewardInfo(Reward reward, out string rewardName, out string rewardImage, out ERarity rewardRarity)
    {
        rewardName = " ";
        rewardImage = "";
        rewardRarity = ERarity.NONE;

        switch (reward.RewardType)
        {
            case ERewardType.CARD:
                if (!DB.Cards.TryGetValue(Convert.ToInt32(reward.RewardValue), out var card))
                    return false;

                rewardImage = card.IconLink;
                rewardRarity = card.CardRarity;
                return true;
            case ERewardType.GUN_SKIN:
                if (!DB.GunSkinsSearchByID.TryGetValue(Convert.ToInt32(reward.RewardValue), out var skin))
                    return false;

                rewardImage = skin.IconLink;
                rewardRarity = skin.SkinRarity;
                return true;
            case ERewardType.GLOVE:
                if (!DB.Gloves.TryGetValue(Convert.ToUInt16(reward.RewardValue), out var glove))
                    return false;

                rewardImage = glove.IconLink;
                rewardRarity = glove.GloveRarity;
                return true;
            case ERewardType.GUN:
                if (!DB.Guns.TryGetValue(Convert.ToUInt16(reward.RewardValue), out var gun))
                    return false;

                rewardImage = gun.IconLink;
                rewardRarity = gun.GunRarity;
                return true;
            case ERewardType.GUN_CHARM:
                if (!DB.GunCharms.TryGetValue(Convert.ToUInt16(reward.RewardValue), out var gunCharm))
                    return false;

                rewardImage = gunCharm.IconLink;
                rewardRarity = gunCharm.CharmRarity;
                return true;
            case ERewardType.KNIFE:
                if (!DB.Knives.TryGetValue(Convert.ToUInt16(reward.RewardValue), out var knife))
                    return false;

                rewardImage = knife.IconLink;
                rewardRarity = knife.KnifeRarity;
                return true;
            case ERewardType.CASE:
                if (!DB.Cases.TryGetValue(Convert.ToInt32(reward.RewardValue), out var @case))
                    return false;

                rewardImage = @case.IconLink;
                rewardRarity = @case.CaseRarity;
                return true;
            case ERewardType.BP_BOOSTER:
                rewardName = $"<color=white>{string.Format("{0:0.##}", Convert.ToDecimal(reward.RewardValue) * 100)}%</color>";
                rewardImage = Config.Icons.FileData.BPXPBoostIconLink;
                return true;
            case ERewardType.XP_BOOSTER:
                rewardName = $"<color=white>{string.Format("{0:0.##}", Convert.ToDecimal(reward.RewardValue) * 100)}%</color>";
                rewardImage = Config.Icons.FileData.XPBoostIconLink;
                return true;
            case ERewardType.GUN_XP_BOOSTER:
                rewardName = $"<color=white>{string.Format("{0:0.##}", Convert.ToDecimal(reward.RewardValue) * 100)}%</color>";
                rewardImage = Config.Icons.FileData.GunXPBoostIconLink;
                return true;
            case ERewardType.COIN:
                rewardName = $"<color=white>{reward.RewardValue}</color>";
                rewardImage = Config.Icons.FileData.BlacktagsLargeIconLink;
                return true;
            case ERewardType.CREDIT:
                rewardName = $"<color=white>{reward.RewardValue}</color>";
                rewardImage = Config.Icons.FileData.PointsLargeIconLink;
                return true;
            case ERewardType.LEVEL_XP:
                rewardName = $"<color=white>{reward.RewardValue}</color>";
                rewardImage = Config.Icons.FileData.XPIconLink;
                return true;
            case ERewardType.SCRAP:
                rewardName = $"<color=white>{reward.RewardValue}</color>";
                rewardImage = Config.Icons.FileData.ScrapLargeIconLink;
                return true;
            default:
                return false;
        }
    }

    public void ShowUnboxingPage(EUnboxingPage unboxingPage, int selectedCase = -1)
    {
        UnboxingPage = unboxingPage;

        switch (unboxingPage)
        {
            case EUnboxingPage.CASES:
                ShowUnboxingCasePage();
                break;
            case EUnboxingPage.BUY:
                ShowUnboxingStorePage();
                break;
            case EUnboxingPage.OPEN:
                UnboxInventoryCase(selectedCase);
                break;
            case EUnboxingPage.INVENTORY:
                ShowUnboxingInventoryPage();
                break;
        }
    }

    public void ShowUnboxingCasePage()
    {
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Cases Next BUTTON", UnboxCasesPages.Count > 1);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Cases Previous BUTTON", UnboxCasesPages.Count > 1);

        if (!UnboxCasesPages.TryGetValue(1, out var firstPage))
        {
            Logging.Debug($"Unable to find first page of unboxing inventory for {Player.CharacterName}");
            for (var i = 0; i <= MAX_CASES_PER_CASE_PAGE; i++)
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Crate BUTTON {i}", false);

            return;
        }

        ShowUnboxingCasePage(firstPage);
    }

    public void ShowUnboxingCasePage(PageUnboxCase page)
    {
        UnboxingPageID = page.PageID;

        for (var i = 0; i <= MAX_CASES_PER_CASE_PAGE; i++)
        {
            if (!page.Cases.TryGetValue(i, out var @case))
            {
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Crate BUTTON {i}", false);
                continue;
            }

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Crate BUTTON {i}", true);
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Crate IMAGE {i}", @case.Case.IconLink);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Crate Count TEXT {i}", $"x{@case.Amount}");
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Crate TEXT {i}", @case.Case.CaseName);

            SendRarity("SERVER Unbox Crate", @case.Case.CaseRarity, i);
        }
    }

    public void ForwardUnboxingCasePage()
    {
        if (!UnboxCasesPages.TryGetValue(UnboxingPageID + 1, out var nextPage) && !UnboxCasesPages.TryGetValue(1, out nextPage))
        {
            Logging.Debug($"Unable to find the next or first unboxing inventory page for {Player.CharacterName}");
            ShowUnboxingCasePage();
            return;
        }

        ShowUnboxingCasePage(nextPage);
    }

    public void BackwardUnboxingCasePage()
    {
        if (!UnboxCasesPages.TryGetValue(UnboxingPageID - 1, out var nextPage) && !UnboxCasesPages.TryGetValue(UnboxCasesPages.Keys.Max(), out nextPage))
        {
            Logging.Debug($"Unable to find the previous or max unboxing inventory page for {Player.CharacterName}");
            ShowUnboxingCasePage();
            return;
        }

        ShowUnboxingCasePage(nextPage);
    }

    public void UnboxInventoryCase(int selected)
    {
        if (!UnboxCasesPages.TryGetValue(UnboxingPageID, out var page))
        {
            Logging.Debug($"Error finding the unbox inventory page with id {UnboxingPageID} for {Player.CharacterName}");
            return;
        }

        if (!page.Cases.TryGetValue(selected, out var @case))
        {
            Logging.Debug($"Error finding the selected case at id {selected} for page with id {UnboxingPage} for {Player.CharacterName}");
            return;
        }

        SelectedCaseID = @case.Case.CaseID;
        PreviewUnboxingStoreCase();

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Unbox Content Unbox Button Toggler", @case.Amount > 0);
    }

    private ECaseRarity CalculateCaseRarity(List<(ECaseRarity, int)> weights, int poolSize)
    {
        var randInt = UnityEngine.Random.Range(0, poolSize) + 1;

        var accumulatedProbability = 0;
        for (var i = 0; i < weights.Count; i++)
        {
            var weight = weights[i];
            accumulatedProbability += weight.Item2;
            if (randInt <= accumulatedProbability)
                return weight.Item1;
        }

        return weights[UnityEngine.Random.Range(0, weights.Count)].Item1;
    }

    public IEnumerator UnboxCase()
    {
        IsUnboxing = true;
        if (!PlayerData.CasesSearchByID.TryGetValue(SelectedCaseID, out var @case))
        {
            Logging.Debug($"Error finding selected case with id {SelectedCaseID} for unboxing for {Player.CharacterName}");
            IsUnboxing = false;
            yield break;
        }

        DB.IncreaseCaseUnboxedAmount(@case.Case.CaseID, 1);
        if (!Plugin.Instance.Unbox.TryCalculateReward(@case.Case, Player, out var reward, out var rewardImage, out var rewardName, out var rewardDesc, out var rewardRarity, out var isDuplicate, out var duplicateScrapAmount, out var cRarity, out var updatedWeights))
        {
            Logging.Debug($"Unable to calculate reward for unboxing case {SelectedCaseID} for {Player.CharacterName}");
            IsUnboxing = false;
            yield break;
        }
        
        var embed = new Embed(null, null, null, Utility.GetDiscordColorCode(rewardRarity), DateTime.UtcNow.ToString("s"), new(Provider.serverName, Provider.configData.Browser.Icon), new(PlayerData.SteamName, $"https://steamcommunity.com/profiles/{PlayerData.SteamID}", PlayerData.AvatarLinks[0]), new Field[] { new($"[{rewardRarity.ToFriendlyName()}] {rewardName}", @case.Case.CaseName, true) }, new(rewardImage), null);
        switch (cRarity)
        {
            case ECaseRarity.GLOVE or ECaseRarity.LIMITED_GLOVE or ECaseRarity.KNIFE or ECaseRarity.LIMITED_KNIFE or ECaseRarity.LIMITED_SKIN or ECaseRarity.SPECIAL_SKIN:
                Plugin.Instance.Discord.SendEmbed(embed, "Black Market", Config.Webhooks.FileData.SpecialUnboxedWebhookLink);
                break;
            default:
                Plugin.Instance.Discord.SendEmbed(embed, "Black Market", Config.Webhooks.FileData.UnboxedWebhookLink);
                break;
        }
        
        switch (rewardRarity)
        {
            case ERarity.COMMON or ERarity.UNCOMMON or ERarity.RARE:
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Crate Result Common Uncommon Rare", true);
                break;
            case ERarity.EPIC:
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Crate Result Epic", true);
                break;
            case ERarity.LEGENDARY:
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Crate Result Legendary", true);
                break;
            case ERarity.MYTHICAL or ERarity.YELLOW or ERarity.CYAN or ERarity.GREEN:
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Crate Result Mythical Special", true);
                break;
        }
        
        var poolSize = updatedWeights.Sum(k => k.Item2);

        for (var i = 0; i <= MAX_ROLLING_CONTENT_PER_CASE; i++)
        {
            if (i == 20)
            {
                EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content Rolling IMAGE {i}",
                    cRarity switch
                    {
                        ECaseRarity.KNIFE or ECaseRarity.LIMITED_KNIFE => Config.Icons.FileData.KnifeUnboxingIconLink,
                        ECaseRarity.GLOVE or ECaseRarity.LIMITED_GLOVE => Config.Icons.FileData.GloveUnboxingIconLink,
                        ECaseRarity.LIMITED_SKIN => Config.Icons.FileData.LimitedSkinUnboxingIconLink,
                        ECaseRarity.SPECIAL_SKIN => Config.Icons.FileData.SpecialSkinUnboxingIconLink,
                        var _ => rewardImage
                    });

                SendRarity("SERVER Unbox Content Rolling", reward.RewardType is ERewardType.KNIFE or ERewardType.GLOVE ? ERarity.YELLOW : rewardRarity, i);
                continue;
            }

            var caseRarity = CalculateCaseRarity(updatedWeights, poolSize);
            switch (caseRarity)
            {
                case ECaseRarity.KNIFE or ECaseRarity.LIMITED_KNIFE:
                    EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content Rolling IMAGE {i}", Config.Icons.FileData.KnifeUnboxingIconLink);
                    SendRarity("SERVER Unbox Content Rolling", ERarity.YELLOW, i);
                    continue;
                case ECaseRarity.GLOVE or ECaseRarity.LIMITED_GLOVE:
                    EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content Rolling IMAGE {i}", Config.Icons.FileData.GloveUnboxingIconLink);
                    SendRarity("SERVER Unbox Content Rolling", ERarity.YELLOW, i);
                    continue;
                case ECaseRarity.SPECIAL_SKIN:
                    EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content Rolling IMAGE {i}", Config.Icons.FileData.SpecialSkinUnboxingIconLink);
                    SendRarity("SERVER Unbox Content Rolling", ERarity.YELLOW, i);
                    continue;
                case ECaseRarity.LIMITED_SKIN:
                    EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content Rolling IMAGE {i}", Config.Icons.FileData.LimitedSkinUnboxingIconLink);
                    SendRarity("SERVER Unbox Content Rolling", ERarity.CYAN, i);
                    continue;
                default:
                    if (!Enum.TryParse(caseRarity.ToString(), true, out ERarity skinRarity))
                    {
                        Logging.Debug($"Error parsing {caseRarity} to a specified skin rarity for rolling for case with id {SelectedCaseID}");
                        break;
                    }

                    if (!@case.Case.AvailableSkinsSearchByRarity.TryGetValue(skinRarity, out var raritySkins))
                    {
                        Logging.Debug($"Error getting skins with {skinRarity} for rolling for case with id {SelectedCaseID}");
                        break;
                    }

                    var randomSkin = raritySkins[UnityEngine.Random.Range(0, raritySkins.Count)];
                    EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content Rolling IMAGE {i}", randomSkin.IconLink);
                    SendRarity("SERVER Unbox Content Rolling", randomSkin.SkinRarity, i);
                    continue;
            }

            var randomS = @case.Case.AvailableSkins[UnityEngine.Random.Range(0, @case.Case.AvailableSkins.Count)];

            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content Rolling IMAGE {i}", randomS.IconLink);
            SendRarity("SERVER Unbox Content Rolling", randomS.SkinRarity, i);
        }

        DB.DecreasePlayerCase(SteamID, @case.Case.CaseID, 1);

        if (isDuplicate)
            DB.IncreasePlayerScrap(SteamID, duplicateScrapAmount);
        else
            Plugin.Instance.Reward.GiveRewards(SteamID, new() { reward });

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "Scene Unbox Content Description TITLE", $"Unboxing {@case.Case.CaseName}");
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "Scene Unbox Content Result IMAGE", rewardImage);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "Scene Unbox Content Result TEXT", $"<color={Utility.GetRarityColor(rewardRarity)}>{rewardName}</color>");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Unbox Content Duplicate", isDuplicate);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "Scene Unbox Content Duplicate TEXT", $"+{duplicateScrapAmount}");
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "Scene Unbox Content Duplicate IMAGE", Config.Icons.FileData.ScrapSmallIconLink);
        
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"Crate Rolling ANIM {UnityEngine.Random.Range(1, 6)}", true);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Crate EXAMPLE Open ANIM", true);

        yield return new WaitForSeconds(1.36f);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Rolling Starter", true);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Wave Starter", true);
        yield return new WaitForSeconds(6.01f);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Result Starter", true);
        yield return new WaitForSeconds(Player.Ping + 0.2f);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content Result {rewardRarity}", true);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Unbox Content Unbox Button Toggler", @case.Amount > 0);
        IsUnboxing = false;
    }

    public void ShowUnboxingStorePage()
    {
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Buy Next BUTTON", UnboxStorePages.Count > 1);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Buy Previous BUTTON", UnboxStorePages.Count > 1);

        if (!UnboxStorePages.TryGetValue(1, out var firstPage))
        {
            Logging.Debug($"Unable to find the first unboxing store page for {Player.CharacterName}");
            for (var i = 0; i <= MAX_CASES_PER_STORE_PAGE; i++)
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Buy BUTTON {i}", false);

            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Unbox Buy IMAGE", "");
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Unbox Buy TEXT", " ");
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Unbox Buy Credits BUTTON", false);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Unbox Buy Coins BUTTON", false);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Unbox Buy Scrap BUTTON", false);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Unbox Buy Preview BUTTON", false);
            SendRarityName("SERVER Unbox Buy RarityType TEXT", ERarity.NONE);

            return;
        }

        ShowUnboxingStorePage(firstPage);
        SelectedUnboxingStoreCase(0);
    }

    public void ShowUnboxingStorePage(PageUnboxStore page)
    {
        UnboxingPageID = page.PageID;

        for (var i = 0; i <= MAX_CASES_PER_STORE_PAGE; i++)
        {
            if (!page.Cases.TryGetValue(i, out var @case))
            {
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Buy BUTTON {i}", false);
                continue;
            }

            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Buy BUTTON {i}", true);
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Buy IMAGE {i}", @case.IconLink);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Buy TEXT {i}", @case.CaseName);

            SendRarity("SERVER Unbox Buy", @case.CaseRarity, i);
        }
    }

    public void ForwardUnboxingStorePage()
    {
        if (!UnboxStorePages.TryGetValue(UnboxingPageID + 1, out var nextPage) && !UnboxStorePages.TryGetValue(1, out nextPage))
        {
            Logging.Debug($"Unable to find the next or first page for unboxing store for {Player.CharacterName}");
            ShowUnboxingStorePage();
            return;
        }

        ShowUnboxingStorePage(nextPage);
    }

    public void BackwardUnboxingStorePage()
    {
        if (!UnboxStorePages.TryGetValue(UnboxingPageID - 1, out var nextPage) && !UnboxStorePages.TryGetValue(UnboxStorePages.Keys.Max(), out nextPage))
        {
            Logging.Debug($"Unable to find previous or max page for unboxing store for {Player.CharacterName}");
            ShowUnboxingStorePage();
            return;
        }

        ShowUnboxingStorePage(nextPage);
    }

    public void SelectedUnboxingStoreCase(int selected)
    {
        if (!UnboxStorePages.TryGetValue(UnboxingPageID, out var page))
        {
            Logging.Debug($"Unable to find the selected page with id {UnboxingPageID} for {Player.CharacterName}");
            return;
        }

        if (!page.Cases.TryGetValue(selected, out var @case))
        {
            Logging.Debug($"Unable to find the case at the selected position {selected} for page with id {UnboxingPageID} for {Player.CharacterName}");
            return;
        }

        SelectedCaseID = @case.CaseID;
        SelectedCaseBuyAmount = 1;
        
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Unbox Buy IMAGE", @case.IconLink);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Unbox Buy TEXT", @case.CaseName);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Unbox Buy Credits BUTTON", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Unbox Buy Coins BUTTON", @case.CoinPrice != 0);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Unbox Buy Scrap BUTTON", @case.ScrapPrice != 0);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Unbox Buy Preview BUTTON", true);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Preview Coins TEXT", $"{Utility.GetCurrencySymbol(ECurrency.COIN)} <color={(PlayerData.Coins >= @case.CoinPrice ? "#9CFF84" : "#FF6E6E")}>{@case.CoinPrice}</color>");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Preview Scrap TEXT", $"{Utility.GetCurrencySymbol(ECurrency.SCRAP)} <color={(PlayerData.Scrap >= @case.ScrapPrice ? "#9CFF84" : "#FF6E6E")}>{@case.ScrapPrice}</color>");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Unbox Buy Amount INPUT", "1");
        
        SendRarityName("SERVER Unbox Buy RarityType TEXT", @case.CaseRarity);
    }

    public void SetUnboxingStoreBuyAmount(string input)
    {
        if (!int.TryParse(input, out var amount))
        {
            Logging.Debug($"{Player.CharacterName} inputted a non-integer in case buy amount");
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Unbox Buy Amount INPUT", "1");
            return;
        }

        if (amount <= 0 || !DB.Cases.TryGetValue(SelectedCaseID, out var @case))
        {
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Unbox Buy Amount INPUT", "1");
            return;
        }

        SelectedCaseBuyAmount = amount;
        
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Preview Coins TEXT", $"{Utility.GetCurrencySymbol(ECurrency.COIN)} <color={(PlayerData.Coins >= @case.CoinPrice * amount ? "#9CFF84" : "#FF6E6E")}>{@case.CoinPrice * amount}</color>");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Preview Scrap TEXT", $"{Utility.GetCurrencySymbol(ECurrency.SCRAP)} <color={(PlayerData.Scrap >= @case.ScrapPrice * amount ? "#9CFF84" : "#FF6E6E")}>{@case.ScrapPrice * amount}</color>");
    }
    
    public void PreviewUnboxingStoreCase()
    {
        if (!DB.Cases.TryGetValue(SelectedCaseID, out var @case))
        {
            Logging.Debug($"Could'nt find selected case id with id {SelectedCaseID} for preview for {Player.CharacterName}");
            return;
        }

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Unbox Content Unbox Button Toggler", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Crate EXAMPLE Drop ANIM", true);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "Scene Unbox Content Description TEXT", @case.CaseName);
        
        var skins = @case.ShowLimiteds ? @case.AvailableSkins.Where(k => k.MaxAmount > 0).OrderBy(k => k.ID).ToList() : @case.AvailableSkins.Where(k => k.MaxAmount == 0).OrderBy(k => k.SkinRarity).ThenBy(k => k.ID).ToList();
        for (var i = 0; i <= MAX_PREVIEW_CONTENT_PER_CASE; i++)
        {
            switch (i)
            {
                case 17 when @case.Weights.Exists(k => k.Item1 is ECaseRarity.GLOVE or ECaseRarity.LIMITED_GLOVE):
                {
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content BUTTON {i}", true);
                    EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content IMAGE {i}", Config.Icons.FileData.GloveUnboxingIconLink);
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content Name TEXT {i}", "Special Gloves");
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content Extra TEXT {i}", " ");
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content BOUGHT {i}", false);
                    SendRarity("SERVER Unbox Content", ERarity.YELLOW, i);
                    continue;
                }
                case 18 when @case.Weights.Exists(k => k.Item1 is ECaseRarity.KNIFE or ECaseRarity.LIMITED_KNIFE):
                {
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content BUTTON {i}", true);
                    EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content IMAGE {i}", Config.Icons.FileData.KnifeUnboxingIconLink);
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content Name TEXT {i}", "Special Melee");
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content Extra TEXT {i}", " ");
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content BOUGHT {i}", false);
                    SendRarity("SERVER Unbox Content", ERarity.YELLOW, i);
                    continue;
                }
                case 19 when @case.Weights.Exists(k => k.Item1 is ECaseRarity.SPECIAL_SKIN):
                {
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content BUTTON {i}", true);
                    EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content IMAGE {i}", Config.Icons.FileData.SpecialSkinUnboxingIconLink);
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content Name TEXT {i}", "Special Skin");
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content Extra TEXT {i}", " ");
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content BOUGHT {i}", false);
                    SendRarity("SERVER Unbox Content", ERarity.YELLOW, i);
                    continue;
                }
                default:
                {
                    if (skins.Count < i + 1)
                    {
                        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content BUTTON {i}", false);
                        continue;
                    }

                    var skin = skins[i];
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content BUTTON {i}", true);
                    EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content IMAGE {i}", skin.IconLink);
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content Name TEXT {i}", $"{skin.Gun.GunName} | {skin.SkinName}");
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content Extra TEXT {i}", " ");
                    EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "Scene Unbox Content Description TITLE", "Unbox Case");
                    EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Unbox Content BOUGHT {i}", PlayerLoadout.GunSkinsSearchByID.ContainsKey(skin.ID));
                    SendRarity("SERVER Unbox Content", skin.SkinRarity, i);
                    continue;
                }
            }
        }
    }

    public void BuyUnboxingStoreCase(ECurrency currency)
    {
        if (!DB.Cases.TryGetValue(SelectedCaseID, out var @case))
        {
            Logging.Debug($"Could'nt find selected case id with id {SelectedCaseID} for buying case for {Player.CharacterName}");
            return;
        }

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Unbox Buy Modal Description TEXT", $"{SelectedCaseBuyAmount}x {@case.CaseName}");
        SelectedCaseBuyMethod = currency;
    }

    public void ConfirmUnboxingStoreCase()
    {
        if (!DB.Cases.TryGetValue(SelectedCaseID, out var @case))
        {
            Logging.Debug($"Could'nt find selected case id with id {SelectedCaseID} for buying case for {Player.CharacterName}");
            return;
        }

        var buyPrice = @case.GetBuyPrice(SelectedCaseBuyMethod) * SelectedCaseBuyAmount;
        if (buyPrice > PlayerData.GetCurrency(SelectedCaseBuyMethod))
        {
            SendNotEnoughCurrencyModal(SelectedCaseBuyMethod);
            return;
        }

        switch (SelectedCaseBuyMethod)
        {
            case ECurrency.COIN:
                DB.DecreasePlayerCoins(SteamID, buyPrice);
                break;
            case ECurrency.SCRAP:
                DB.DecreasePlayerScrap(SteamID, buyPrice);
                break;
            default:
                return;
        }

        DB.IncreasePlayerCase(SteamID, @case.CaseID, SelectedCaseBuyAmount);
    }

    public void ShowUnboxingInventoryPage()
    {
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Inventory Next BUTTON", UnboxInventoryPages.Count > 1);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Inventory Previous BUTTON", UnboxInventoryPages.Count > 1);

        if (!UnboxInventoryPages.TryGetValue(1, out var firstPage))
        {
            Logging.Debug($"Unable to find the first unboxing inventory page for {Player.CharacterName}");
            for (var i = 0; i <= MAX_SKINS_PER_INVENTORY_PAGE; i++)
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Inventory Skin BUTTON {i}", false);
            return;
        }

        ShowUnboxingInventoryPage(firstPage);
    }

    public void ShowUnboxingInventoryPage(PageUnboxInventory page)
    {
        UnboxingPageID = page.PageID;
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Inventory Page TEXT", $"Page {page.PageID}");
        for (var i = 0; i <= MAX_SKINS_PER_INVENTORY_PAGE; i++)
        {
            if (!page.Skins.TryGetValue(i, out var skin) || !TryGetUnboxInventorySkinInfo(skin, out var name, out var iconLink, out var rarity))
            {
                EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Inventory Skin BUTTON {i}", false);
                continue;
            }
            
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Inventory Skin BUTTON {i}", true);
            SendRarity("SERVER Inventory Skin", rarity, i);
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Inventory Skin IMAGE {i}", iconLink);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Inventory Skin TEXT {i}", name);
        }
    }
    
    public void ForwardUnboxingInventoryPage()
    {
        if (!UnboxInventoryPages.TryGetValue(UnboxingPageID + 1, out var page) && !UnboxInventoryPages.TryGetValue(1, out page))
        {
            Logging.Debug($"Unable to find the next or first unboxing inventory page for {Player.CharacterName}");
            return;
        }
        
        ShowUnboxingInventoryPage(page);
    }

    public void BackwardUnboxingInventoryPage()
    {
        if (!UnboxInventoryPages.TryGetValue(UnboxingPageID - 1, out var page) && !UnboxInventoryPages.TryGetValue(UnboxInventoryPages.Count, out page))
        {
            Logging.Debug($"Unable to find the previous or last unboxing inventory page for {Player.CharacterName}");
            return;
        }
        
        ShowUnboxingInventoryPage(page);
    }

    public void SearchUnboxingInventoryPage(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            ShowUnboxingInventoryPage();
            return;
        }

        var updatedInput = input.ToLower();
        var skins = new Dictionary<int, object>();
        var index = 0;
        foreach (var skin in PlayerLoadout.Knives.Values.Where(k => k.Knife.KnifeName.ToLower().Contains(updatedInput)))
        {
            skins.Add(index, skin.Knife);
            if (index == MAX_SKINS_PER_INVENTORY_PAGE)
                break;

            index++;
        }

        if (index < 20)
        {
            foreach (var skin in PlayerLoadout.Gloves.Values.Where(k => k.Glove.GloveName.ToLower().Contains(updatedInput)))
            {
                skins.Add(index, skin.Glove);
                if (index == MAX_SKINS_PER_INVENTORY_PAGE)
                    break;

                index++;
            }
        }

        if (index < 20)
        {
            foreach (var skin in PlayerLoadout.GunSkinsSearchByID.Values.Where(k => k.SkinName.ToLower().Contains(updatedInput) || k.Gun.GunName.ToLower().Contains(updatedInput)))
            {
                skins.Add(index, skin);
                if (index == MAX_SKINS_PER_INVENTORY_PAGE)
                    break;

                index++;
            }
        }

        if (skins.Count == 0)
            return;
        
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Inventory Next BUTTON", false);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Inventory Previous BUTTON", false);
        
        ShowUnboxingInventoryPage(new(1, skins));
    }

    public bool TryGetUnboxInventorySkinInfo(object skin, out string name, out string iconLink, out ERarity rarity)
    {
        name = "";
        iconLink = "";
        rarity = ERarity.NONE;
        
        switch (skin)
        {
            case Knife knife:
                rarity = knife.KnifeRarity;
                name = knife.KnifeName;
                iconLink = knife.IconLink;
                return true;
            case Glove glove:
                rarity = glove.GloveRarity;
                name = glove.GloveName;
                iconLink = glove.IconLink;
                return true;
            case GunSkin gunSkin:
                rarity = gunSkin.SkinRarity;
                name = $"{gunSkin.Gun.GunName} | {gunSkin.SkinName}";
                iconLink = gunSkin.IconLink;
                return true;
            default:
                Logging.Debug($"Unable to find unbox inventory skin info for this type");
                break;
        }

        return false;
    }
    
    public IEnumerator SetupScrollableImages()
    {
        var images = Config.Base.FileData.ScrollableImages;
        var scrollableImage = images[0];
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Scrollable IMAGE", scrollableImage.Image);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Scrollable Previous IMAGE", scrollableImage.Image);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Scrollable Shift Image Enabler 0", true);
        CurrentScrollableImage = 0;
        for (var i = images.Count; i <= 9; i++)
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Scrollable Dot {i}", false);

        for (var i = 1; i <= images.Count - 1; i++)
        {
            var image = images[i];
            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Scrollable Setup IMAGE", image.Image);
            yield return new WaitForSeconds(0.2f);
        }
    }

    public void StartScrollableImages()
    {
        ImageScroller.Stop();
        ImageScroller = Plugin.Instance.StartCoroutine(ScrollImages());
    }

    public void StopScrollableImages()
    {
        ImageScroller.Stop();
    }
    
    public void SendScrollableImageLink()
    {
        var scrollableImage = Config.Base.FileData.ScrollableImages[CurrentScrollableImage];
        if (string.IsNullOrEmpty(scrollableImage.Link))
            return;
        
        Player.Player.sendBrowserRequest(scrollableImage.LinkMessage, scrollableImage.Link);
    }

    public void ChangeScrollableImage(int index)
    {
        if (CurrentScrollableImage == index)
            return;

        var scrollableImages = Config.Base.FileData.ScrollableImages;
        if (scrollableImages.Count < index + 1)
            return;

        var newImage = scrollableImages[index];
        ChangeScrollableImage(newImage, index);
    }

    public void ChangeScrollableImage(ScrollableImage newImage, int index)
    {
        var scrollableImages = Config.Base.FileData.ScrollableImages;
        var currentScrollableImage = scrollableImages[CurrentScrollableImage];
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "SERVER Scrollable Previous IMAGE", true);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Scrollable Previous IMAGE", currentScrollableImage.Image);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Scrollable IMAGE", newImage.Image);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Scrollable Shift Image Enabler {index}", true);
        CurrentScrollableImage = index;
        ImageScroller.Stop();
        ImageScroller = Plugin.Instance.StartCoroutine(ScrollImages());
    }
    
    public IEnumerator ScrollImages()
    {
        var scrollableImages = Config.Base.FileData.ScrollableImages;
        yield return new WaitForSeconds(Config.Base.FileData.ScrollableImageTimer);

        var newIndex = CurrentScrollableImage + 1;
        if (scrollableImages.Count < newIndex + 1)
            newIndex = 0;

        var newImage = scrollableImages[newIndex];
        ChangeScrollableImage(newImage, newIndex);
    }

    public IEnumerator ShowMatchEndSummary(MatchEndSummary summary)
    {
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary", true);
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary XP Toggle", true);

        // Send the match end info

        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Banner IMAGE", summary.Player.ActiveLoadout?.Card?.Card?.CardLink ?? "");
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Level IMAGE", DB.Levels.TryGetValue(summary.Player.Data.Level, out var level) ? level.IconLinkLarge : "");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Level TEXT", summary.Player.Data.Level.ToString());
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Player TEXT", Player.CharacterName);
        EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Player IMAGE", summary.Player.Data.AvatarLinks[2]);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Kills TEXT", summary.Kills.ToString());
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Deaths TEXT", summary.Deaths.ToString());
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary KD TEXT", $"{summary.KD:n}");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Assists TEXT", summary.Assists.ToString());
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Killstreak TEXT", summary.HighestKillstreak.ToString());
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Multikill TEXT", summary.HighestMK.ToString());
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Points TEXT", $"+{summary.PendingCredits}");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Total XP TEXT", $"<color=#fcee6a>+{summary.TotalXP}</color>");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Match XP", $"MATCH <color=#fcee6a>XP</color>");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Match XP TEXT", $"<color=#fcee6a>+{summary.MatchXP}</color>");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Match Bonus", $"MATCH <color=#fcee6a>XP</color> BONUS");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Match Bonus TEXT", $"<color=#fcee6a>+{summary.MatchXPBonus}</color>");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Achievements XP", $"ACHIEVEMENTS XP BONUS <color=#ffb566>({summary.Player.Data.AchievementXPBooster * 100:0.##}%)</color>");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Achievements XP TEXT", $"<color=#ffb566>+{summary.AchievementXPBonus}</color>");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Other XP", $"OTHER <color=#fcee6a>XP</color> BONUSES");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Other XP TEXT", $"<color=#fcee6a>+{summary.OtherXPBonus}</color>");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Battlepass XP", $"BATTLEPASS ★");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Battlepass XP TEXT", $"<color=#be69ff>+{summary.BattlepassXP}</color>");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Battlepass Bonus", $"BATTLEPASS ★ BONUS");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Battlepass Bonus TEXT", $"<color=#be69ff>+{summary.BattlepassBonusXP}</color>");

        // Set the current level, xp and next level xp to animate the bar
        var currentLevel = summary.StartingLevel;
        var currentXP = summary.StartingXP;
        var nextLevelXP = DB.Levels.TryGetValue(currentLevel + 1, out level) ? level.XPNeeded : 0;

        // Send the filled amount of bar and set the toggle to true and animate the text
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP 1 TEXT", $"Match <color=#AD6816>{summary.MatchXP}</color> XP");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP 2 TEXT", $"Match <color=#AD6816>{summary.MatchXPBonus}</color> Bonus XP");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP 3 TEXT", $"Achievement <color=#AD6816>{summary.AchievementXPBonus}</color> Bonus XP");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP 4 TEXT", $"Other <color=#AD6816>{summary.OtherXPBonus}</color> Bonus XP");

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Level 0 TEXT", $"<color=#AD6816>{currentLevel:D3}</color>");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Level 1 TEXT", nextLevelXP == 0 ? "MAX" : (currentLevel + 1).ToString("D3"));
        // Animate Match XP

        var boldSpaces = currentXP == 0 ? 1 : Math.Max(1, Math.Min(MAX_SPACES_MATCH_END_SUMMARY, currentXP * MAX_SPACES_MATCH_END_SUMMARY / (nextLevelXP == 0 ? 1 : nextLevelXP)));
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 1", UIManager.HAIRSPACE_SYMBOL_STRING);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 0", new(' ', boldSpaces));
        yield return new WaitForSeconds(0.7f);

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Type TEXT", "Match XP");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP 0 TEXT", $"+{summary.MatchXP} XP");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary XP 0 Toggle", true);

        var b = summary.MatchXP;
        while (nextLevelXP != 0 && currentXP + b >= nextLevelXP)
        {
            // Level has changed
            b = currentXP + b - nextLevelXP;
            currentLevel += 1;
            currentXP = 0;
            nextLevelXP = DB.Levels.TryGetValue(currentLevel + 1, out level) ? level.XPNeeded : 0;

            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary LevelUp IMAGE", DB.Levels.TryGetValue(currentLevel, out level) ? level.IconLinkLarge : "");
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary LevelUp Toggle", true);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 1", new(' ', MAX_SPACES_MATCH_END_SUMMARY - boldSpaces));
            yield return new WaitForSeconds(0.5f);

            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 0", UIManager.HAIRSPACE_SYMBOL_STRING);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 1", UIManager.HAIRSPACE_SYMBOL_STRING);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary LevelUp Toggle", false);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Level 0 TEXT", $"<color=#AD6816>{currentLevel:D3}</color>");
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Level 1 TEXT", nextLevelXP == 0 ? "MAX" : (currentLevel + 1).ToString("D3"));

            boldSpaces = 0;
        }

        var highlightedSpaces = Math.Max(1, Math.Min(MAX_SPACES_MATCH_END_SUMMARY - boldSpaces, b * (MAX_SPACES_MATCH_END_SUMMARY - boldSpaces) / (nextLevelXP - currentXP)));
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 1", new(' ', highlightedSpaces));
        currentXP += b;
        yield return new WaitForSeconds(0.7f);

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary XP 0 Toggle", false);
        yield return new WaitForSeconds(0.18f);

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary XP 1 Toggle", true);

        // --------------------------

        // Animate Match Bonus XP

        boldSpaces = currentXP == 0 ? 1 : Math.Max(1, Math.Min(MAX_SPACES_MATCH_END_SUMMARY, currentXP * MAX_SPACES_MATCH_END_SUMMARY / (nextLevelXP == 0 ? 1 : nextLevelXP)));
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 1", UIManager.HAIRSPACE_SYMBOL_STRING);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 0", new(' ', boldSpaces));
        yield return new WaitForSeconds(0.7f);

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Type TEXT", "Match Bonus XP");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP 0 TEXT", $"+{summary.MatchXPBonus} XP");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary XP 0 Toggle", true);

        b = summary.MatchXPBonus;
        while (nextLevelXP != 0 && currentXP + b >= nextLevelXP)
        {
            // Level has changed
            b = currentXP + b - nextLevelXP;
            currentLevel += 1;
            currentXP = 0;
            nextLevelXP = DB.Levels.TryGetValue(currentLevel + 1, out level) ? level.XPNeeded : 0;

            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary LevelUp Toggle", DB.Levels.TryGetValue(currentLevel, out level) ? level.IconLinkLarge : "");
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary LevelUp Toggle", true);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 1", new(' ', MAX_SPACES_MATCH_END_SUMMARY - boldSpaces));
            yield return new WaitForSeconds(0.5f);

            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 0", UIManager.HAIRSPACE_SYMBOL_STRING);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 1", UIManager.HAIRSPACE_SYMBOL_STRING);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary LevelUp Toggle", false);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Level 0 TEXT", $"<color=#AD6816>{currentLevel:D3}</color>");
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Level 1 TEXT", nextLevelXP == 0 ? "MAX" : (currentLevel + 1).ToString("D3"));

            boldSpaces = 0;
        }

        highlightedSpaces = Math.Max(1, Math.Min(MAX_SPACES_MATCH_END_SUMMARY - boldSpaces, b * (MAX_SPACES_MATCH_END_SUMMARY - boldSpaces) / (nextLevelXP - currentXP)));
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 1", new(' ', highlightedSpaces));
        currentXP += b;
        yield return new WaitForSeconds(0.7f);

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary XP 0 Toggle", false);
        yield return new WaitForSeconds(0.18f);

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary XP 2 Toggle", true);

        // --------------------------

        // Animate Achievement Bonus XP

        boldSpaces = currentXP == 0 ? 1 : Math.Max(1, Math.Min(MAX_SPACES_MATCH_END_SUMMARY, currentXP * MAX_SPACES_MATCH_END_SUMMARY / (nextLevelXP == 0 ? 1 : nextLevelXP)));
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 1", UIManager.HAIRSPACE_SYMBOL_STRING);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 0", new(' ', boldSpaces));
        yield return new WaitForSeconds(0.7f);

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Type TEXT", "Achievement Bonus XP");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP 0 TEXT", $"+{summary.AchievementXPBonus} XP");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary XP 0 Toggle", true);

        b = summary.AchievementXPBonus;
        while (nextLevelXP != 0 && currentXP + b >= nextLevelXP)
        {
            // Level has changed
            b = currentXP + b - nextLevelXP;
            currentLevel += 1;
            currentXP = 0;
            nextLevelXP = DB.Levels.TryGetValue(currentLevel + 1, out level) ? level.XPNeeded : 0;

            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary LevelUp Toggle", DB.Levels.TryGetValue(currentLevel, out level) ? level.IconLinkLarge : "");
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary LevelUp Toggle", true);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 1", new(' ', MAX_SPACES_MATCH_END_SUMMARY - boldSpaces));
            yield return new WaitForSeconds(0.5f);

            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 0", UIManager.HAIRSPACE_SYMBOL_STRING);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 1", UIManager.HAIRSPACE_SYMBOL_STRING);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary LevelUp Toggle", false);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Level 0 TEXT", $"<color=#AD6816>{currentLevel:D3}</color>");
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Level 1 TEXT", nextLevelXP == 0 ? "MAX" : (currentLevel + 1).ToString("D3"));

            boldSpaces = 0;
        }

        highlightedSpaces = Math.Max(1, Math.Min(MAX_SPACES_MATCH_END_SUMMARY - boldSpaces, b * (MAX_SPACES_MATCH_END_SUMMARY - boldSpaces) / (nextLevelXP - currentXP)));
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 1", new(' ', highlightedSpaces));
        currentXP += b;
        yield return new WaitForSeconds(0.7f);

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary XP 0 Toggle", false);
        yield return new WaitForSeconds(0.18f);

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary XP 3 Toggle", true);

        // --------------------------

        // Animate Other Bonus XP

        boldSpaces = currentXP == 0 ? 1 : Math.Max(1, Math.Min(MAX_SPACES_MATCH_END_SUMMARY, currentXP * MAX_SPACES_MATCH_END_SUMMARY / (nextLevelXP == 0 ? 1 : nextLevelXP)));
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 1", UIManager.HAIRSPACE_SYMBOL_STRING);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 0", new(' ', boldSpaces));
        yield return new WaitForSeconds(0.7f);

        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Type TEXT", "Other Bonus XP");
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP 0 TEXT", $"+{summary.OtherXPBonus} XP");
        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary XP 0 Toggle", true);

        b = summary.OtherXPBonus;
        while (nextLevelXP != 0 && currentXP + b >= nextLevelXP)
        {
            // Level has changed
            b = currentXP + b - nextLevelXP;
            currentLevel += 1;
            currentXP = 0;
            nextLevelXP = DB.Levels.TryGetValue(currentLevel + 1, out level) ? level.XPNeeded : 0;

            EffectManager.sendUIEffectImageURL(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary LevelUp Toggle", DB.Levels.TryGetValue(currentLevel, out level) ? level.IconLinkLarge : "");
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary LevelUp Toggle", true);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 1", new(' ', MAX_SPACES_MATCH_END_SUMMARY - boldSpaces));
            yield return new WaitForSeconds(0.5f);

            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 0", UIManager.HAIRSPACE_SYMBOL_STRING);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 1", UIManager.HAIRSPACE_SYMBOL_STRING);
            EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary LevelUp Toggle", false);
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Level 0 TEXT", $"<color=#AD6816>{currentLevel:D3}</color>");
            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary Level 1 TEXT", nextLevelXP == 0 ? "MAX" : (currentLevel + 1).ToString("D3"));

            boldSpaces = 0;
        }

        highlightedSpaces = Math.Max(1, Math.Min(MAX_SPACES_MATCH_END_SUMMARY - boldSpaces, b * (MAX_SPACES_MATCH_END_SUMMARY - boldSpaces) / (nextLevelXP - currentXP)));
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 1", new(' ', highlightedSpaces));
        currentXP += b;
        yield return new WaitForSeconds(0.7f);

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary XP 0 Toggle", false);
        yield return new WaitForSeconds(0.18f);

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary XP 4 Toggle", true);

        // --------------------------

        // Finish up Animation

        boldSpaces = currentXP == 0 ? 1 : Math.Max(1, Math.Min(MAX_SPACES_MATCH_END_SUMMARY, currentXP * MAX_SPACES_MATCH_END_SUMMARY / (nextLevelXP == 0 ? 1 : nextLevelXP)));
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 1", UIManager.HAIRSPACE_SYMBOL_STRING);
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Summary XP Bar Fill 0", new(' ', boldSpaces));

        // --------------------------

        yield return new WaitForSeconds(2f);

        EffectManager.sendUIEffectVisibility(MAIN_MENU_KEY, TransportConnection, true, "Scene Summary Stats Toggle", true);
    }

    public void OnCurrencyUpdated(ECurrency currency)
    {
        EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, $"SERVER Currency {currency.ToUIName()} TEXT", PlayerData.GetCurrency(currency).ToString());
    }

    public IEnumerator RefreshTimer()
    {
        while (PlayerData != null)
        {
            yield return new WaitForSeconds(1f);

            if (MainPage == EMainPage.LEADERBOARD)
                EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Leaderboards Reset TEXT", GetLeaderboardRefreshTime());

            EffectManager.sendUIEffectText(MAIN_MENU_KEY, TransportConnection, true, "SERVER Quest Expire TEXT", $"NEW QUESTS IN: {(DateTimeOffset.UtcNow > PlayerData.Quests[0].QuestEnd ? "00:00:00" : (DateTimeOffset.UtcNow - PlayerData.Quests[0].QuestEnd).ToString(@"hh\:mm\:ss"))}");
        }
    }
}