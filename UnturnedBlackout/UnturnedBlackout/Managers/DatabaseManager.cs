using MySql.Data.MySqlClient;
using Rocket.API;
using Rocket.Core;
using Rocket.Core.Steam;
using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using SDG.Unturned;
using SteamServerQuery;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using UnityEngine;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Extensions;
using UnturnedBlackout.Models.Animation;
using UnturnedBlackout.Models.Bot;
using UnturnedBlackout.Models.Data;
using UnturnedBlackout.Models.Webhook;
using Achievement = UnturnedBlackout.Database.Base.Achievement;
using Logger = Rocket.Core.Logging.Logger;
using PlayerQuest = UnturnedBlackout.Database.Data.PlayerQuest;
using Timer = System.Timers.Timer;

// ReSharper disable NotResolvedInText

namespace UnturnedBlackout.Managers;

public class DatabaseManager
{
    public MySqlConnectionStringBuilder Builder { get; set; }
    public string ConnectionString => Builder.ConnectionString;
    public Config Config { get; set; }

    public bool ForcedShutdown { get; set; }
    public int ConnectionThreshold { get; set; }
    private Timer CacheRefresher { get; set; }
    private Timer BatchQueryCleaner { get; set; }

    // Pending Queries
    public List<string> PendingQueries { get; set; }

    // Server Data
    public Options ServerOptions { get; set; }
    public bool IsPendingSeasonalWipe { get; set; }

    // Players Data
    public Dictionary<CSteamID, PlayerData> PlayerData { get; set; }
    public Dictionary<CSteamID, PlayerLoadout> PlayerLoadouts { get; set; }

    // Leaderboard Data
    public Dictionary<CSteamID, LeaderboardData> PlayerDailyLeaderboardLookup { get; set; }
    public List<LeaderboardData> PlayerDailyLeaderboard { get; set; }

    public Dictionary<CSteamID, LeaderboardData> PlayerWeeklyLeaderboardLookup { get; set; }
    public List<LeaderboardData> PlayerWeeklyLeaderboard { get; set; }

    public Dictionary<CSteamID, LeaderboardData> PlayerSeasonalLeaderboardLookup { get; set; }
    public List<LeaderboardData> PlayerSeasonalLeaderboard { get; set; }

    public Dictionary<CSteamID, LeaderboardData> PlayerAllTimeLeaderboardLookup { get; set; }
    public List<LeaderboardData> PlayerAllTimeKill { get; set; }
    public List<LeaderboardData> PlayerAllTimeLevel { get; set; }
    public List<LeaderboardData> PlayerAllTimeSkins { get; set; }

    // Base Data
    public Dictionary<ushort, Gun> Guns { get; set; }
    public Dictionary<ushort, GunAttachment> GunAttachments { get; set; }
    public Dictionary<ushort, GunCharm> GunCharms { get; set; }
    public Dictionary<int, GunSkin> GunSkinsSearchByID { get; set; }
    public Dictionary<ushort, List<GunSkin>> GunSkinsSearchByGunID { get; set; }
    public Dictionary<ushort, GunSkin> GunSkinsSearchBySkinID { get; set; }

    public Dictionary<ushort, Knife> Knives { get; set; }

    public Dictionary<ushort, Gadget> Gadgets { get; set; }
    public Dictionary<int, Killstreak> Killstreaks { get; set; }
    public Dictionary<int, Perk> Perks { get; set; }
    public Dictionary<int, Glove> Gloves { get; set; }
    public Dictionary<int, Card> Cards { get; set; }
    public Dictionary<int, XPLevel> Levels { get; set; }

    public Dictionary<int, Quest> QuestsSearchByID { get; set; }
    public List<Quest> Quests { get; set; }

    public Dictionary<int, Achievement> AchievementsSearchByID { get; set; }
    public List<Achievement> Achievements { get; set; }

    public Dictionary<int, BattlepassTier> BattlepassTiersSearchByID { get; set; }
    public List<BattlepassTier> BattlepassTiers { get; set; }

    public Dictionary<int, Case> Cases { get; set; }

    public Dictionary<int, List<AnimationItemUnlock>> ItemsSearchByLevel { get; set; }

    public List<Server> Servers { get; set; }

    // Default Data
    public LoadoutData DefaultLoadout { get; set; }
    public List<Gun> DefaultGuns { get; set; }
    public List<Knife> DefaultKnives { get; set; }
    public List<Gadget> DefaultGadgets { get; set; }
    public List<Killstreak> DefaultKillstreaks { get; set; }
    public List<Perk> DefaultPerks { get; set; }
    public List<Glove> DefaultGloves { get; set; }
    public List<Card> DefaultCards { get; set; }

    // Players Data
    // MAIN
    public const string PLAYERS = "UB_Players";
    public const string PLAYERS_LEADERBOARD_DAILY = "UB_Players_Leaderboard_Daily";
    public const string PLAYERS_LEADERBOARD_WEEKLY = "UB_Players_Leaderboard_Weekly";
    public const string PLAYERS_LEADERBOARD_SEASONAL = "UB_Players_Leaderboard_Seasonal";
    public const string PLAYERS_LOADOUTS = "UB_Players_Loadouts";

    // GUNS
    public const string PLAYERS_GUNS = "UB_Players_Guns";
    public const string PLAYERS_GUNS_SKINS = "UB_Players_Guns_Skins";
    public const string PLAYERS_GUNS_CHARMS = "UB_Players_Guns_Charms";

    // KNIVES
    public const string PLAYERS_KNIVES = "UB_Players_Knives";

    // PERKS
    public const string PLAYERS_PERKS = "UB_Players_Perks";

    // GADGETS
    public const string PLAYERS_GADGETS = "UB_Players_Gadgets";

    // KILLSTREAKS
    public const string PLAYERS_KILLSTREAKS = "UB_Players_Killstreaks";

    // CARDS
    public const string PLAYERS_CARDS = "UB_Players_Cards";

    // GLOVES
    public const string PLAYERS_GLOVES = "UB_Players_Gloves";

    // QUESTS
    public const string PLAYERS_QUESTS = "UB_Players_Quests";

    // ACHIEVEMENTS
    public const string PLAYERS_ACHIEVEMENTS = "UB_Players_Achievements";

    // BATTLEPASS
    public const string PLAYERS_BATTLEPASS = "UB_Players_Battlepass";

    // CASES
    public const string PLAYERS_CASES = "UB_Players_Cases";

    // BOOSTERS
    public const string PLAYERS_BOOSTERS = "UB_Players_Boosters";

    // Base Data
    // GUNS
    public const string GUNS = "UB_Guns";
    public const string ATTACHMENTS = "UB_Guns_Attachments";
    public const string GUNS_SKINS = "UB_Guns_Skins";
    public const string GUNS_CHARMS = "UB_Guns_Charms";

    // KNIVES
    public const string KNIVES = "UB_Knives";

    // PERKS
    public const string PERKS = "UB_Perks";

    // GADGETS
    public const string GADGETS = "UB_Gadgets";

    // KILLSTREAKS
    public const string KILLSTREAKS = "UB_Killstreaks";

    // CARDS
    public const string CARDS = "UB_Cards";

    // GLOVES
    public const string GLOVES = "UB_Gloves";

    // LEVELS
    public const string LEVELS = "UB_Levels";

    // SERVER OPTIONS
    public const string OPTIONS = "UB_Options";
    public const string SERVERS = "UB_Servers";

    // Quests
    public const string QUESTS = "UB_Quests";

    // ACHIEVEMENTS
    public const string ACHIEVEMENTS = "UB_Achievements";
    public const string ACHIEVEMENTS_TIERS = "UB_Achievements_Tiers";

    // BATTLEPASS
    public const string BATTLEPASS = "UB_Battlepass";

    // CASES
    public const string CASES = "UB_Cases";
   
    public const int LOADING_SPACES = 96;

    public DatabaseManager()
    {
        Config = Plugin.Instance.Configuration.Instance;
        Builder = new()
        {
            Server = Config.DatabaseHost,
            Port = Convert.ToUInt32(Config.DatabasePort),
            Database = Config.DatabaseName,
            UserID = Config.DatabaseUsername,
            Password = Config.DatabasePassword,
            MaximumPoolSize = 50,
            ConnectionTimeout = 5
        };

        ConnectionThreshold = 0;

        CacheRefresher = new(120 * 1000);
        CacheRefresher.Elapsed += RefreshData;

        BatchQueryCleaner = new(10 * 1000);
        BatchQueryCleaner.Elapsed += CleanQueries;
        PendingQueries = new();

        PlayerData = new();
        PlayerLoadouts = new();

        IsPendingSeasonalWipe = false;

        Task.Run(async () =>
        {
            await LoadDatabaseAsync();
            await GetBaseDataAsync();
        }).Wait();

        RefreshData(null, null);
        CacheRefresher.Start();
        BatchQueryCleaner.Start();
    }

    public void Destroy()
    {
        CacheRefresher.Stop();
        BatchQueryCleaner.Stop();
        if (ForcedShutdown)
            return;

        CleanQueries(null, null);
        Logger.Log("Cleared all queries", ConsoleColor.Green);
    }

    public async Task LoadDatabaseAsync()
    {
        using MySqlConnection conn = new(ConnectionString);
        try
        {
            await conn.OpenAsync();

            // BASE DATA
            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{GUNS}` ( `GunID` SMALLINT UNSIGNED NOT NULL , `GunName` VARCHAR(255) NOT NULL , `GunDesc` TEXT NOT NULL , `GunType` ENUM('Pistol','SMG','Shotgun','LMG','AR','SNIPER','CARBINE') NOT NULL , `GunRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `MovementChange` DECIMAL(4,3) NOT NULL , `MovementChangeADS` DECIMAL(4,3) NOT NULL , `IconLink` TEXT NOT NULL , `MagAmount` TINYINT NOT NULL , `Coins` INT NOT NULL , `BuyPrice` INT NOT NULL ,  `ScrapAmount` INT NOT NULL , `LevelRequirement` INT NOT NULL , `IsPrimary` BOOLEAN NOT NULL , `DefaultAttachments` TEXT NOT NULL , `LevelXPNeeded` TEXT NOT NULL , `LevelRewards` TEXT NOT NULL , `OverrideStats` TEXT NOT NULL , PRIMARY KEY (`GunID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{ATTACHMENTS}` ( `AttachmentID` SMALLINT UNSIGNED NOT NULL , `AttachmentName` VARCHAR(255) NOT NULL , `AttachmentDesc` TEXT NOT NULL , `AttachmentPros` TEXT NOT NULL , `AttachmentCons` TEXT NOT NULL , `AttachmentType` ENUM('Sights','Grip','Barrel','Magazine') NOT NULL , `AttachmentRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `MovementChange` DECIMAL(4,3) NOT NULL , `MovementChangeADS` DECIMAL (4,3) NOT NULL , `IconLink` TEXT NOT NULL , `BuyPrice` INT NOT NULL , `Coins` INT NOT NULL , `OverrideStats` TEXT NOT NULL , PRIMARY KEY (`AttachmentID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{GUNS_SKINS}` ( `ID` INT NOT NULL AUTO_INCREMENT , `GunID` SMALLINT UNSIGNED NOT NULL , `SkinID` SMALLINT UNSIGNED NOT NULL , `SkinName` VARCHAR(255) NOT NULL , `SkinDesc` TEXT NOT NULL , `SkinRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `PatternLink` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT  NOT NULL , `MaxAmount` INT NOT NULL , `UnboxedAmount` INT NOT NULL , CONSTRAINT `ub_gun_id` FOREIGN KEY (`GunID`) REFERENCES `{GUNS}` (`GunID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`ID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{GUNS_CHARMS}` ( `CharmID` SMALLINT UNSIGNED NOT NULL , `CharmName` VARCHAR(255) NOT NULL , `CharmDesc` TEXT NOT NULL , `CharmRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `IconLink` TEXT NOT NULL , `BuyPrice` INT NOT NULL , `Coins` INT NOT NULL , `ScrapAmount` INT  NOT NULL , `LevelRequirement` INT NOT NULL , `AuthorCredits` TEXT NOT NULL , `UnboxedAmount` INT NOT NULL , PRIMARY KEY (`CharmID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{KNIVES}` ( `KnifeID` SMALLINT UNSIGNED NOT NULL , `KnifeName` VARCHAR(255) NOT NULL , `KnifeDesc` TEXT NOT NULL , `KnifeRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `MovementChange` DECIMAL(4,3) NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT NOT NULL , `Coins` INT NOT NULL , `BuyPrice` INT NOT NULL , `LevelRequirement` INT NOT NULL , `KnifeWeight` INT NOT NULL , `MaxAmount` INT NOT NULL , `UnboxedAmount` INT NOT NULL , PRIMARY KEY (`KnifeID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{PERKS}` ( `PerkID` INT NOT NULL , `PerkName` VARCHAR(255) NOT NULL , `PerkDesc` TEXT NOT NULL , `PerkType` ENUM('1','2','3') NOT NULL , `PerkRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `IconLink` TEXT NOT NULL , `SkillType` TEXT NOT NULL , `SkillLevel` INT NOT NULL , `Coins` INT NOT NULL , `BuyPrice` INT NOT NULL , `ScrapAmount` INT  NOT NULL , `LevelRequirement` INT NOT NULL , `OverrideStats` TEXT NOT NULL , PRIMARY KEY (`PerkID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{GADGETS}` ( `GadgetID` SMALLINT UNSIGNED NOT NULL , `GadgetName` VARCHAR(255) NOT NULL , `GadgetDesc` TEXT NOT NULL , `GadgetRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `IconLink` TEXT NOT NULL , `Coins` INT NOT NULL , `BuyPrice` INT NOT NULL , `ScrapAmount` INT NOT NULL , `GiveSeconds` INT  NOT NULL , `LevelRequirement` INT NOT NULL , `IsTactical` BOOLEAN NOT NULL , PRIMARY KEY (`GadgetID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{KILLSTREAKS}` ( `KillstreakID` INT NOT NULL , `KillstreakName` VARCHAR(255) NOT NULL , `KillstreakDesc` TEXT NOT NULL , `KillstreakRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `IconLink` TEXT NOT NULL , `KillstreakRequired` INT NOT NULL , `BuyPrice` INT NOT NULL , `Coins` INT  NOT NULL , `ScrapAmount` INT NOT NULL , `LevelRequirement` INT NOT NULL , PRIMARY KEY (`KillstreakID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{CARDS}` ( `CardID` INT NOT NULL , `CardName` VARCHAR(255) NOT NULL , `CardDesc` TEXT NOT NULL , `CardRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `IconLink` TEXT NOT NULL , `CardLink` TEXT NOT NULL , `ScrapAmount` INT NOT NULL , `BuyPrice` INT NOT NULL , `Coins` INT NOT NULL , `LevelRequirement` INT NOT NULL , `AuthorCredits` TEXT NOT NULL , `UnboxedAmount` INT NOT NULL , PRIMARY KEY (`CardID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{GLOVES}` ( `GloveID` INT NOT NULL , `GloveName` VARCHAR(255) NOT NULL , `GloveDesc` TEXT NOT NULL , `GloveRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT NOT NULL , `BuyPrice` INT NOT NULL , `Coins` INT NOT NULL , `LevelRequirement` INT NOT NULL , `GloveWeight` INT NOT NULL , `MaxAmount` INT NOT NULL , `UnboxedAmount` INT NOT NULL , PRIMARY KEY (`GloveID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{LEVELS}` ( `Level` INT NOT NULL , `XPNeeded` INT NOT NULL , `IconLinkLarge` TEXT NOT NULL , `IconLinkMedium` TEXT NOT NULL , `IconLinkSmall` TEXT NOT NULL , PRIMARY KEY (`Level`));", conn).ExecuteScalarAsync();
            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{OPTIONS}` ( `DailyLeaderboardWipe` BIGINT NOT NULL , `WeeklyLeaderboardWipe` BIGINT NOT NULL , `DailyLeaderboardRankedRewards` TEXT NOT NULL , `DailyLeaderboardPercentileRewards` TEXT NOT NULL , `WeeklyLeaderboardRankedRewards` TEXT NOT NULL , `WeeklyLeaderboardPercentileRewards` TEXT NOT NULL, `SeasonalLeaderboardRankedRewards` TEXT NOT NULL , `SeasonalLeaderboardPercentileRewards` TEXT NOT NULL , `XPBooster` DECIMAL(6,3) NOT NULL , `BPBooster` DECIMAL(6,3) NOT NULL , `GunXPBooster` DECIMAL(6,3) NOT NULL , `XPBoosterWipe` BIGINT NOT NULL , `BPBoosterWipe` BIGINT NOT NULL , `GunXPBoosterWipe` BIGINT NOT NULL , `GameTips` TEXT NOT NULL , `PrimeRewards` TEXT NOT NULL , `PrimeDailyRewards` TEXT NOT NULL);",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{SERVERS}`  ( `IP` TEXT NOT NULL , `Port` TEXT NOT NULL , `ServerName` TEXT NOT NULL , `FriendlyIP` TEXT NOT NULL , `ServerBanner` TEXT NOT NULL , `ServerDesc` TEXT NOT NULL );", conn).ExecuteScalarAsync();
            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{QUESTS}` ( `QuestID` INT NOT NULL AUTO_INCREMENT , `QuestTitle` TEXT NOT NULL , `QuestDesc` TEXT NOT NULL , QuestType ENUM('Kill', 'Death', 'Win', 'FinishMatch', 'MultiKill', 'Killstreak', 'Headshots', 'GadgetsUsed', 'FlagsCaptured', 'FlagsSaved', 'Dogtags', 'Shutdown', 'Domination', 'FlagKiller', 'FlagDenied', 'Revenge', 'FirstKill', 'Longshot', 'Survivor', 'Collector') NOT NULL , `QuestTier` ENUM('Easy1', 'Easy2', 'Easy3', 'Medium1', 'Medium2', 'Hard1') NOT NULL , `QuestConditions` TEXT NOT NULL , `TargetAmount` INT NOT NULL , `XP` INT NOT NULL , PRIMARY KEY (`QuestID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{ACHIEVEMENTS}` ( `AchievementID` INT NOT NULL AUTO_INCREMENT , `AchievementType` ENUM('Kill', 'Death', 'Win', 'FinishMatch', 'MultiKill', 'Killstreak', 'Headshots', 'GadgetsUsed', 'FlagsCaptured', 'FlagsSaved', 'Dogtags', 'Shutdown', 'Domination', 'FlagKiller', 'FlagDenied', 'Revenge', 'FirstKill', 'Longshot', 'Survivor', 'Collector') NOT NULL , `AchievementConditions` TEXT NOT NULL , `PageID` INT NOT NULL , PRIMARY KEY (`AchievementID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{ACHIEVEMENTS_TIERS}` ( `AchievementID` INT NOT NULL , `TierID` INT NOT NULL , `TierTitle` TEXT NOT NULL , `TierDesc` TEXT NOT NULL , `TierPrevSmall` TEXT NOT NULL , `TierPrevLarge` TEXT NOT NULL , `TargetAmount` INT NOT NULL , `Rewards` TEXT NOT NULL , `RemoveRewards` TEXT NOT NULL , CONSTRAINT `ub_achievement_id` FOREIGN KEY (`AchievementID`) REFERENCES `{ACHIEVEMENTS}` (`AchievementID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`AchievementID`, `TierID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{BATTLEPASS}` ( `TierID` INT NOT NULL , `FreeReward` TEXT NOT NULL , `PremiumReward` TEXT NOT NULL , `XP` INT NOT NULL , PRIMARY KEY (`TierID`));", conn).ExecuteScalarAsync();
            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{CASES}` ( `CaseID` INT NOT NULL , `CaseName` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `CaseRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `IsBuyable` BOOLEAN NOT NULL , `ScrapPrice` INT NOT NULL , `CoinPrice` INT NOT NULL , `CommonWeight` INT NOT NULL , `UncommonWeight` INT NOT NULL , `RareWeight` INT NOT NULL , `EpicWeight` INT NOT NULL , `LegendaryWeight` INT NOT NULL , `MythicalWeight` INT NOT NULL , `KnifeWeight` INT NOT NULL , `GloveWeight` INT NOT NULL , `LimitedKnifeWeight` INT NOT NULL , `LimitedGloveWeight` INT NOT NULL , `LimitedSkinWeight` INT NOT NULL , `SpecialSkinWeight` INT NOT NULL , `AvailableSkins` TEXT NOT NULL, `UnboxedAmount` INT NOT NULL, PRIMARY KEY (`CaseID`))",
                conn).ExecuteScalarAsync();

            // PLAYERS DATA
            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{PLAYERS}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `SteamName` TEXT NOT NULL , `AvatarLink` TEXT NOT NULL , `CountryCode` TEXT NOT NULL , `HideFlag` BOOLEAN NOT NULL DEFAULT FALSE , `XP` INT NOT NULL DEFAULT '0' , `Level` INT NOT NULL DEFAULT '1' , `Credits` INT NOT NULL DEFAULT '0' , `Scrap` INT NOT NULL DEFAULT '0' , `Coins` INT NOT NULL DEFAULT '0' , `Kills` INT NOT NULL DEFAULT '0' , `HeadshotKills` INT NOT NULL DEFAULT '0' , `HighestKillstreak` INT NOT NULL DEFAULT '0' , `HighestMultiKills` INT NOT NULL DEFAULT '0' , `KillsConfirmed` INT NOT NULL DEFAULT '0' , `KillsDenied` INT NOT NULL DEFAULT '0' , `FlagsCaptured` INT NOT NULL DEFAULT '0' , `FlagsSaved` INT NOT NULL DEFAULT '0' , `AreasTaken` INT NOT NULL DEFAULT '0' , `Deaths` INT NOT NULL DEFAULT '0' , `Music` BOOLEAN NOT NULL DEFAULT TRUE , `IsMuted` BOOLEAN NOT NULL DEFAULT FALSE , `MuteExpiry` BIGINT NOT NULL DEFAULT '1' , `MuteReason` TEXT NOT NULL , `HasBattlepass` BOOLEAN NOT NULL DEFAULT FALSE , `XPBooster` DECIMAL(6,3) NOT NULL DEFAULT '0' , `BPBooster` DECIMAL(6,3) NOT NULL DEFAULT '0' , `GunXPBooster` DECIMAL(6,3) NOT NULL DEFAULT '0' , `HasPrime` BOOLEAN NOT NULL DEFAULT FALSE , `PrimeExpiry` BIGINT NOT NULL DEFAULT '1' , `PrimeLastDailyReward` BIGINT NOT NULL DEFAULT '1' , `Volume` INT NOT NULL DEFAULT '20' , `Hotkeys` TEXT NOT NULL , `IsStaff` BOOLEAN NOT NULL DEFAULT FALSE ,  PRIMARY KEY (`SteamID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{PLAYERS_LEADERBOARD_DAILY}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `Kills` INT NOT NULL DEFAULT '0' , `HeadshotKills` INT NOT NULL DEFAULT '0' , `Deaths` INT NOT NULL DEFAULT '0' , CONSTRAINT `ub_steam_id_11` FOREIGN KEY (`SteamID`) REFERENCES `{PLAYERS}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{PLAYERS_LEADERBOARD_WEEKLY}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `Kills` INT NOT NULL DEFAULT '0' , `HeadshotKills` INT NOT NULL DEFAULT '0' , `Deaths` INT NOT NULL DEFAULT '0' , CONSTRAINT `ub_steam_id_12` FOREIGN KEY (`SteamID`) REFERENCES `{PLAYERS}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{PLAYERS_LEADERBOARD_SEASONAL}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `Kills` INT NOT NULL DEFAULT '0' , `HeadshotKills` INT NOT NULL DEFAULT '0' , `Deaths` INT NOT NULL DEFAULT '0' , CONSTRAINT `ub_steam_id_13` FOREIGN KEY (`SteamID`) REFERENCES `{PLAYERS}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{PLAYERS_GUNS}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `GunID` SMALLINT UNSIGNED NOT NULL , `Level` INT NOT NULL , `XP` INT NOT NULL , `GunKills` INT NOT NULL , `IsBought` BOOLEAN NOT NULL , `IsUnlocked` BOOLEAN NOT NULL DEFAULT False , `Attachments` TEXT NOT NULL , CONSTRAINT `ub_steam_id` FOREIGN KEY (`SteamID`) REFERENCES `{PLAYERS}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_gun_id_1` FOREIGN KEY (`GunID`) REFERENCES `{GUNS}` (`GunID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `GunID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                    $"CREATE TABLE IF NOT EXISTS `{PLAYERS_GUNS_SKINS}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `SkinIDs` TEXT NOT NULL , CONSTRAINT `ub_steam_id_1` FOREIGN KEY (`SteamID`) REFERENCES `{PLAYERS}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID`));", conn)
                .ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{PLAYERS_GUNS_CHARMS}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `CharmID` SMALLINT UNSIGNED NOT NULL , `IsBought` BOOLEAN NOT NULL , `IsUnlocked` BOOLEAN NOT NULL DEFAULT False , CONSTRAINT `ub_steam_id_10` FOREIGN KEY (`SteamID`) REFERENCES `{PLAYERS}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_charm_id` FOREIGN KEY (`CharmID`) REFERENCES `{GUNS_CHARMS}` (`CharmID`) ON DELETE CASCADE ON UPDATE CASCADE , Primary Key (`SteamID`, `CharmID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{PLAYERS_KNIVES}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `KnifeID` SMALLINT UNSIGNED NOT NULL , `KnifeKills` INT NOT NULL , `IsBought` BOOLEAN NOT NULL , `IsUnlocked` BOOLEAN NOT NULL DEFAULT False , CONSTRAINT `ub_steam_id_2` FOREIGN KEY (`SteamID`) REFERENCES `{PLAYERS}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_knife_id` FOREIGN KEY (`KnifeID`) REFERENCES `{KNIVES}` (`KnifeID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `KnifeID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{PLAYERS_PERKS}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `PerkID` INT NOT NULL , `IsBought` BOOLEAN NOT NULL , `IsUnlocked` BOOLEAN NOT NULL DEFAULT False , CONSTRAINT `ub_steam_id_4` FOREIGN KEY (`SteamID`) REFERENCES `{PLAYERS}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_perk_id` FOREIGN KEY (`PerkID`) REFERENCES `{PERKS}` (`PerkID`) ON DELETE CASCADE ON UPDATE CASCADE, PRIMARY KEY (`SteamID` , `PerkID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{PLAYERS_GADGETS}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `GadgetID` SMALLINT UNSIGNED NOT NULL , `GadgetKills` INT NOT NULL , `IsBought` BOOLEAN NOT NULL , `IsUnlocked` BOOLEAN NOT NULL DEFAULT False , CONSTRAINT `ub_steam_id_5` FOREIGN KEY (`SteamID`) REFERENCES `{PLAYERS}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_gadget_id` FOREIGN KEY (`GadgetID`) REFERENCES `{GADGETS}` (`GadgetID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `GadgetID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{PLAYERS_KILLSTREAKS}` (`SteamID` BIGINT UNSIGNED NOT NULL , `KillstreakID` INT NOT NULl , `KillstreakKills` INT NOT NULL , `IsBought` BOOLEAN NOT NULL , `IsUnlocked` BOOLEAN NOT NULL DEFAULT False , CONSTRAINT `ub_steam_id_6` FOREIGN KEY (`SteamID`) REFERENCES `{PLAYERS}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_killstreak_id` FOREIGN KEY (`KillstreakID`) REFERENCES `{KILLSTREAKS}` (`KillstreakID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `KillstreakID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{PLAYERS_CARDS}` (`SteamID` BIGINT UNSIGNED NOT NULL , `CardID` INT NOT NULL , `IsBought` BOOLEAN NOT NULL , `IsUnlocked` BOOLEAN NOT NULL DEFAULT False , CONSTRAINT `ub_steam_id_7` FOREIGN KEY (`SteamID`) REFERENCES `{PLAYERS}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_card_id` FOREIGN KEY (`CardID`) REFERENCES `{CARDS}` (`CardID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `CardID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{PLAYERS_GLOVES}` (`SteamID` BIGINT UNSIGNED NOT NULL , `GloveID` INT NOT NULL , `IsBought` BOOLEAN NOT NULl , `IsUnlocked` BOOLEAN NOT NULL DEFAULT False , CONSTRAINT `ub_steam_id_8` FOREIGN KEY (`SteamID`) REFERENCES `{PLAYERS}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_glove_id` FOREIGN KEY (`GloveID`) REFERENCES `{GLOVES}` (`GloveID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `GloveID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{PLAYERS_LOADOUTS}` (`SteamID` BIGINT UNSIGNED NOT NULL , `LoadoutID` INT NOT NULL , `IsActive` BOOLEAN NOT NULL , `Loadout` TEXT NOT NULL , CONSTRAINT `ub_steam_id_9` FOREIGN KEY (`SteamID`) REFERENCES `{PLAYERS}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID`, `LoadoutID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{PLAYERS_QUESTS}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `QuestID` INT NOT NULL , `Amount` INT NOT NULL , `QuestEnd` BIGINT NOT NULL , CONSTRAINT `ub_steam_id_14` FOREIGN KEY (`SteamID`) REFERENCES `{PLAYERS}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_quest_id` FOREIGN KEY (`QuestID`) REFERENCES `{QUESTS}` (`QuestID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `QuestID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{PLAYERS_ACHIEVEMENTS}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `AchievementID` INT NOT NULL , `CurrentTier` INT NOT NULL DEFAULT '0' , `Amount` INT NOT NULL DEFAULT '0' , CONSTRAINT `ub_steam_id_15` FOREIGN KEY (`SteamID`) REFERENCES `{PLAYERS}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_achievement_id_2` FOREIGN KEY (`AchievementID`) REFERENCES `{ACHIEVEMENTS}` (`AchievementID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID`, `AchievementID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{PLAYERS_BATTLEPASS}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `CurrentTier` INT NOT NULL DEFAULT '1' , `XP` INT NOT NULL DEFAULT '0', `ClaimedFreeRewards` TEXT NOT NULL , `ClaimedPremiumRewards` TEXT NOT NULL , CONSTRAINT `ub_steam_id_16` FOREIGN KEY (`SteamID`) REFERENCES `{PLAYERS}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{PLAYERS_CASES}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `CaseID` INT NOT NULL , `Amount` INT NOT NULL , CONSTRAINT `ub_steam_id_17` FOREIGN KEY (`SteamID`) REFERENCES `{PLAYERS}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_case_id` FOREIGN KEY (`CaseID`) REFERENCES `{CASES}` (`CaseID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `CaseID`));",
                conn).ExecuteScalarAsync();

            _ = await new MySqlCommand(
                $"CREATE TABLE IF NOT EXISTS `{PLAYERS_BOOSTERS}` (`SteamID` BIGINT UNSIGNED NOT NULL , `BoosterType` ENUM('XP','BPXP','GUNXP') NOT NULL , `BoosterValue` DECIMAL(6,3) NOT NULL , `BoosterExpiration` BIGINT NOT NULL , CONSTRAINT `ub_steam_id_18` FOREIGN KEY (`SteamID`) REFERENCES `{PLAYERS}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `BoosterType` , `BoosterValue`));",
                conn).ExecuteScalarAsync();
        }
        catch (Exception ex)
        {
            Logger.Log("Error loading database");
            Logger.Log(ex);
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    public async Task GetBaseDataAsync()
    {
        using MySqlConnection conn = new(ConnectionString);
        try
        {
            await conn.OpenAsync();

            Logging.Debug("Getting base data");
            Logging.Debug("Reading attachments from the base data");
            List<Gun> defaultGuns = new();
            List<Knife> defaultKnives = new();
            List<Gadget> defaultGadgets = new();
            List<Killstreak> defaultKillstreaks = new();
            List<Perk> defaultPerks = new();
            List<Glove> defaultGloves = new();
            List<Card> defaultCards = new();

            Dictionary<int, List<AnimationItemUnlock>> itemsSearchByLevel = new();

            var rdr = (MySqlDataReader)await new MySqlCommand($"SELECT `AttachmentID`, `AttachmentName`, `AttachmentDesc`, `AttachmentPros` , `AttachmentCons` , `AttachmentType`-1, `AttachmentRarity`, `MovementChange`, `MovementChangeADS`, `IconLink`, `BuyPrice`, `Coins`, `OverrideStats` FROM `{ATTACHMENTS}`;",
                conn).ExecuteReaderAsync();

            try
            {
                Dictionary<ushort, GunAttachment> gunAttachments = new();
                while (await rdr.ReadAsync())
                {
                    if (!ushort.TryParse(rdr[0].ToString(), out var attachmentID))
                        continue;

                    var attachmentName = rdr[1].ToString();
                    var attachmentDesc = rdr[2].ToString();
                    var attachmentPros = rdr[3].ToString().Split(',').Where(k => !string.IsNullOrEmpty(k)).ToList();
                    var attachmentCons = rdr[4].ToString().Split(',').Where(k => !string.IsNullOrEmpty(k)).ToList();
                    if (!int.TryParse(rdr[5].ToString(), out var attachmentTypeInt))
                        continue;

                    var attachmentType = (EAttachment)attachmentTypeInt;
                    if (!Enum.TryParse(rdr[6].ToString(), true, out ERarity rarity))
                        continue;

                    if (!float.TryParse(rdr[7].ToString(), out var movementChange))
                        continue;

                    if (!float.TryParse(rdr[8].ToString(), out var movementChangeADS))
                        continue;

                    var iconLink = rdr[9].ToString();
                    if (!int.TryParse(rdr[10].ToString(), out var buyPrice))
                        continue;

                    if (!int.TryParse(rdr[11].ToString(), out var coins))
                        continue;
                    
                    var overrideStats = Utility.GetStatsFromString(rdr[12].ToString());
                    var stats = new Dictionary<EStat, int>();
                    
                    
                    if (!gunAttachments.ContainsKey(attachmentID))
                        gunAttachments.Add(attachmentID, new(attachmentID, attachmentName, attachmentDesc, attachmentPros, attachmentCons, attachmentType, rarity, movementChange, movementChangeADS, iconLink, buyPrice, coins, new()));
                    else
                        Logging.Debug($"Found a duplicate attachment with id {attachmentID}, ignoring this");
                }

                Logging.Debug($"Successfully read {gunAttachments.Count} attachments from the table");
                GunAttachments = gunAttachments;
            }
            catch (Exception ex)
            {
                Logger.Log("Error reading data from attachments table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug("Reading guns from the base data");
            rdr = (MySqlDataReader)await new MySqlCommand(
                    $"SELECT `GunID`, `GunName`, `GunDesc`, `GunType`-1, `GunRarity`, `MovementChange`, `MovementChangeADS`, `IconLink`, `MagAmount`, `Coins`, `BuyPrice`, `ScrapAmount`, `LevelRequirement`, `IsPrimary`, `DefaultAttachments`, `LevelXPNeeded`, `LevelRewards`, `OverrideStats` FROM `{GUNS}`;", conn)
                .ExecuteReaderAsync();

            try
            {
                Dictionary<ushort, Gun> guns = new();
                while (await rdr.ReadAsync())
                {
                    if (!ushort.TryParse(rdr[0].ToString(), out var gunID))
                        continue;

                    var gunName = rdr[1].ToString();
                    var gunDesc = rdr[2].ToString();
                    if (!byte.TryParse(rdr[3].ToString(), out var gunTypeInt))
                        continue;

                    var gunType = (EGun)gunTypeInt;
                    if (!Enum.TryParse(rdr[4].ToString(), true, out ERarity rarity))
                        continue;

                    if (!float.TryParse(rdr[5].ToString(), out var movementChange))
                        continue;

                    if (!float.TryParse(rdr[6].ToString(), out var movementChangeADS))
                        continue;

                    var iconLink = rdr[7].ToString();
                    if (!int.TryParse(rdr[8].ToString(), out var magAmount))
                        continue;

                    if (!int.TryParse(rdr[9].ToString(), out var coins))
                        continue;

                    if (!int.TryParse(rdr[10].ToString(), out var buyPrice))
                        continue;

                    if (!int.TryParse(rdr[11].ToString(), out var scrapAmount))
                        continue;

                    if (!int.TryParse(rdr[12].ToString(), out var levelRequirement))
                        continue;

                    if (!bool.TryParse(rdr[13].ToString(), out var isPrimary))
                        continue;

                    List<GunAttachment> defaultAttachments = new();
                    foreach (var id in rdr[14].GetIntListFromReaderResult())
                    {
                        if (GunAttachments.TryGetValue((ushort)id, out var gunAttachment))
                            defaultAttachments.Add(gunAttachment);
                        else
                        {
                            if (id != 0)
                                Logging.Debug($"Could'nt find default attachment with id {id} for gun {gunID} with name {gunName}");
                        }
                    }

                    var levelXPNeeded = rdr[15].GetIntListFromReaderResult();
                    var levelRewards = rdr[16].GetIntListFromReaderResult();
                    Dictionary<int, GunAttachment> rewardAttachments = new();
                    Dictionary<GunAttachment, int> rewardAttachmentsInverse = new();
                    for (var i = 0; i < levelRewards.Count; i++)
                    {
                        var id = levelRewards[i];
                        var levelNeededReward = i + 2;

                        if (id == 0)
                            continue;

                        if (!GunAttachments.TryGetValue((ushort)id, out var gunAttachment))
                        {
                            Logging.Debug($"Could'nt find reward attachment with id {id} at level {levelNeededReward} for gun {gunID} with name {gunName}");
                            continue;
                        }

                        if (rewardAttachmentsInverse.TryGetValue(gunAttachment, out var alreadyRegisteredLevel))
                        {
                            Logging.Debug($"This reward attachment with id {id} was already registered at level {alreadyRegisteredLevel} (trying to register it again at {levelNeededReward}) --- for gun {gunID} with name {gunName}");
                            continue;
                        }

                        rewardAttachments.Add(levelNeededReward, gunAttachment);
                        rewardAttachmentsInverse.Add(gunAttachment, levelNeededReward);
                    }

                    if (Assets.find(EAssetType.ITEM, gunID) is not ItemGunAsset gunAsset)
                    {
                        Logging.Debug($"Error finding gun asset of the gun with id {gunID} and name {gunName} ignoring the gun");
                        continue;
                    }

                    var longshotRange = Mathf.Pow(gunAsset.damageFalloffRange * 100, 2) * 1.2f;
                    var overrideStats = Utility.GetStatsFromString(rdr[17].ToString());

                    var stats = new Dictionary<EStat, int>
                    {
                            { EStat.RANGE, overrideStats.TryGetValue(EStat.RANGE, out var range) ? range : Mathf.RoundToInt(gunAsset.damageFalloffRange * 100) },
                            { EStat.MOBILITY, overrideStats.TryGetValue(EStat.MOBILITY, out var mobility) ? mobility : Mathf.RoundToInt(gunAsset.equipableMovementSpeedMultiplier * 50) }
                    };

                    switch (gunType)
                    {
                        case EGun.ASSAULT_RIFLES:
                        case EGun.SUBMACHINE_GUNS:
                        case EGun.LIGHT_MACHINE_GUNS:
                        case EGun.CARBINES:
                        {
                            stats.Add(EStat.DAMAGE, overrideStats.TryGetValue(EStat.DAMAGE, out var damage) ? damage : (int)gunAsset.playerDamageMultiplier.damage);
                            stats.Add(EStat.FIRE_RATE, overrideStats.TryGetValue(EStat.FIRE_RATE, out var fireRate) ? fireRate : Mathf.RoundToInt(100 - gunAsset.firerate * 10));
                            stats.Add(EStat.RECOIL_CONTROL, overrideStats.TryGetValue(EStat.RECOIL_CONTROL, out var recoilControl) ? recoilControl : Mathf.RoundToInt(100 - (gunAsset.recoilMin_x + gunAsset.recoilMin_y + gunAsset.recoilMax_y) * 10));
                            stats.Add(EStat.HIPFIRE_ACCURACY, overrideStats.TryGetValue(EStat.HIPFIRE_ACCURACY, out var accuracy) ? accuracy : Mathf.RoundToInt(100 - gunAsset.spreadHip * 4));
                            break;
                        }
                        case EGun.SNIPER_RIFLES:
                        {
                            stats.Add(EStat.DAMAGE, overrideStats.TryGetValue(EStat.DAMAGE, out var damage) ? damage : (int)gunAsset.playerDamageMultiplier.damage);
                            stats.Add(EStat.FIRE_RATE, overrideStats.TryGetValue(EStat.FIRE_RATE, out var fireRate) ? fireRate : Mathf.RoundToInt(100 - gunAsset.firerate));
                            stats.Add(EStat.RECOIL_CONTROL, overrideStats.TryGetValue(EStat.RECOIL_CONTROL, out var recoilControl) ? recoilControl : Mathf.RoundToInt(100 - (gunAsset.recoilMin_x + gunAsset.recoilMin_y + gunAsset.recoilMax_y) * 3));
                            stats.Add(EStat.HIPFIRE_ACCURACY, overrideStats.TryGetValue(EStat.HIPFIRE_ACCURACY, out var accuracy) ? accuracy : Mathf.RoundToInt(100 - gunAsset.spreadHip * 4));
                            break;
                        }
                        case EGun.SHOTGUNS:
                        { 
                            stats.Add(EStat.DAMAGE, overrideStats.TryGetValue(EStat.DAMAGE, out var damage) ? damage : (int)(gunAsset.playerDamageMultiplier.damage * 8));
                            stats.Add(EStat.FIRE_RATE, overrideStats.TryGetValue(EStat.FIRE_RATE, out var fireRate) ? fireRate : Mathf.RoundToInt(100 - gunAsset.firerate * 10));
                            stats.Add(EStat.RECOIL_CONTROL, overrideStats.TryGetValue(EStat.RECOIL_CONTROL, out var recoilControl) ? recoilControl : Mathf.RoundToInt(100 - (gunAsset.recoilMin_x + gunAsset.recoilMin_y + gunAsset.recoilMax_y) * 3));
                            stats.Add(EStat.HIPFIRE_ACCURACY, overrideStats.TryGetValue(EStat.HIPFIRE_ACCURACY, out var accuracy) ? accuracy : Mathf.RoundToInt(100 - gunAsset.spreadHip * 5));
                            break;
                        }
                        case EGun.PISTOL:
                        {
                            stats.Add(EStat.DAMAGE, overrideStats.TryGetValue(EStat.DAMAGE, out var damage) ? damage : (int)gunAsset.playerDamageMultiplier.damage);
                            stats.Add(EStat.FIRE_RATE, overrideStats.TryGetValue(EStat.FIRE_RATE, out var fireRate) ? fireRate : Mathf.RoundToInt(100 - gunAsset.firerate * 10));
                            stats.Add(EStat.RECOIL_CONTROL, overrideStats.TryGetValue(EStat.RECOIL_CONTROL, out var recoilControl) ? recoilControl : Mathf.RoundToInt(100 - (gunAsset.recoilMin_x + gunAsset.recoilMin_y + gunAsset.recoilMax_y) * 5));
                            stats.Add(EStat.HIPFIRE_ACCURACY, overrideStats.TryGetValue(EStat.HIPFIRE_ACCURACY, out var accuracy) ? accuracy : Mathf.RoundToInt(100 - gunAsset.spreadHip * 4));
                            break;
                        }
                    }
                    
                    var statText = stats.Aggregate("", (current, stat) => current + $"Stat: {stat.Key} Amount: {stat.Value}, ");

                    Logging.Debug($"Gun: {gunName}, {statText}");

                    Gun gun = new(gunID, gunName, gunDesc, gunType, rarity, movementChange, movementChangeADS, iconLink, magAmount, coins, buyPrice, scrapAmount, levelRequirement, isPrimary, defaultAttachments, rewardAttachments, rewardAttachmentsInverse, levelXPNeeded, longshotRange, stats);
                    if (!guns.ContainsKey(gunID))
                        guns.Add(gunID, gun);
                    else
                    {
                        Logging.Debug($"Found a duplicate with id {gunID}, ignoring this");
                        break;
                    }

                    if (levelRequirement == 0)
                        defaultGuns.Add(gun);
                    else if (levelRequirement > 0)
                    {
                        if (!itemsSearchByLevel.ContainsKey(levelRequirement))
                            itemsSearchByLevel.Add(levelRequirement, new());

                        itemsSearchByLevel[levelRequirement].Add(new(gun.IconLink, "GUN", gun.GunName));
                    }
                }

                Logging.Debug($"Successfully read {guns.Count} guns from the table");
                Guns = guns;
            }
            catch (Exception ex)
            {
                Logger.Log("Error reading data from guns table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug("Reading gun skins from the base table");
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{GUNS_SKINS}`;", conn).ExecuteReaderAsync();
            try
            {
                Dictionary<int, GunSkin> gunSkinsSearchByID = new();
                Dictionary<ushort, List<GunSkin>> gunSkinsSearchByGunID = new();
                Dictionary<ushort, GunSkin> gunSkinsSearchBySkinID = new();

                while (await rdr.ReadAsync())
                {
                    if (!int.TryParse(rdr[0].ToString(), out var id))
                        continue;

                    if (!ushort.TryParse(rdr[1].ToString(), out var gunID))
                        continue;

                    if (!Guns.TryGetValue(gunID, out var gun))
                    {
                        Logging.Debug($"Could'nt find gun id with {gunID} for skin with id {id}");
                        continue;
                    }

                    if (!ushort.TryParse(rdr[2].ToString(), out var skinID))
                        continue;

                    var skinName = rdr[3].ToString();
                    var skinDesc = rdr[4].ToString();
                    if (!Enum.TryParse(rdr[5].ToString(), true, out ERarity rarity))
                        continue;

                    var patternLink = rdr[6].ToString();
                    var iconLink = rdr[7].ToString();
                    if (!int.TryParse(rdr[8].ToString(), out var scrapAmount))
                        continue;

                    if (!int.TryParse(rdr[9].ToString(), out var maxAmount))
                        continue;

                    if (!int.TryParse(rdr[10].ToString(), out var unboxedAmount))
                        continue;

                    GunSkin skin = new(id, gun, skinID, skinName, skinDesc, rarity, patternLink, iconLink, scrapAmount, maxAmount, unboxedAmount);
                    if (gunSkinsSearchByID.ContainsKey(id))
                    {
                        Logging.Debug($"Found a duplicate skin with id {id}, ignoring this");
                        continue;
                    }

                    gunSkinsSearchByID.Add(id, skin);

                    if (gunSkinsSearchByGunID.TryGetValue(gunID, out var skins))
                    {
                        if (skins.Exists(k => k.ID == id))
                        {
                            Logging.Debug($"Found a duplicate skin with id {id}, ignoring this");
                            continue;
                        }

                        skins.Add(skin);
                    }
                    else
                        gunSkinsSearchByGunID.Add(gunID, new() { skin });

                    if (gunSkinsSearchBySkinID.ContainsKey(skinID))
                        Logging.Debug($"Found a duplicate skin with id {id}, ignoring this");
                    else
                        gunSkinsSearchBySkinID.Add(skinID, skin);
                }

                Logging.Debug($"Successfully read {gunSkinsSearchByID.Count} gun skins from the table");
                GunSkinsSearchByID = gunSkinsSearchByID;
                GunSkinsSearchByGunID = gunSkinsSearchByGunID;
                GunSkinsSearchBySkinID = gunSkinsSearchBySkinID;
            }
            catch (Exception ex)
            {
                Logger.Log("Error reading data from the guns skins table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug("Reading guns charms from the base table");
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{GUNS_CHARMS}`;", conn).ExecuteReaderAsync();
            try
            {
                Dictionary<ushort, GunCharm> gunCharms = new();
                while (await rdr.ReadAsync())
                {
                    if (!ushort.TryParse(rdr[0].ToString(), out var charmID))
                        continue;

                    var charmName = rdr[1].ToString();
                    var charmDesc = rdr[2].ToString();
                    if (!Enum.TryParse(rdr[3].ToString(), true, out ERarity rarity))
                        continue;

                    var iconLink = rdr[4].ToString();
                    if (!int.TryParse(rdr[5].ToString(), out var buyPrice))
                        continue;

                    if (!int.TryParse(rdr[6].ToString(), out var coins))
                        continue;

                    if (!int.TryParse(rdr[7].ToString(), out var scrapAmount))
                        continue;

                    if (!int.TryParse(rdr[8].ToString(), out var levelRequirement))
                        continue;

                    var authorCredits = rdr[9].ToString();

                    if (!int.TryParse(rdr[10].ToString(), out var unboxedAmount))
                        continue;
                    
                    if (gunCharms.ContainsKey(charmID))
                    {
                        Logging.Debug($"Found a duplicate charm with id {charmID} registered");
                        continue;
                    }

                    gunCharms.Add(charmID, new(charmID, charmName, charmDesc, rarity, iconLink, buyPrice, coins, scrapAmount, levelRequirement, authorCredits, unboxedAmount));
                }

                Logging.Debug($"Successfully read {gunCharms.Count} gun charms");
                GunCharms = gunCharms;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error finding data from guns charms table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug("Reading knives from the base data");
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{KNIVES}`;", conn).ExecuteReaderAsync();
            try
            {
                Dictionary<ushort, Knife> knives = new();
                while (await rdr.ReadAsync())
                {
                    if (!ushort.TryParse(rdr[0].ToString(), out var knifeID))
                        continue;

                    var knifeName = rdr[1].ToString();
                    var knifeDesc = rdr[2].ToString();
                    if (!Enum.TryParse(rdr[3].ToString(), true, out ERarity rarity))
                        continue;

                    if (!float.TryParse(rdr[4].ToString(), out var movementChange))
                        continue;

                    var iconLink = rdr[5].ToString();
                    if (!int.TryParse(rdr[6].ToString(), out var scrapAmount))
                        continue;

                    if (!int.TryParse(rdr[7].ToString(), out var coins))
                        continue;

                    if (!int.TryParse(rdr[8].ToString(), out var buyPrice))
                        continue;

                    if (!int.TryParse(rdr[9].ToString(), out var levelRequirement))
                        continue;

                    if (!int.TryParse(rdr[10].ToString(), out var knifeWeight))
                        continue;

                    if (!int.TryParse(rdr[11].ToString(), out var maxAmount))
                        continue;

                    if (!int.TryParse(rdr[12].ToString(), out var unboxedAmount))
                        continue;

                    Knife knife = new(knifeID, knifeName, knifeDesc, rarity, movementChange, iconLink, scrapAmount, coins, buyPrice, levelRequirement, knifeWeight, maxAmount, unboxedAmount);
                    if (!knives.ContainsKey(knifeID))
                        knives.Add(knifeID, knife);
                    else
                    {
                        Logging.Debug($"Found a duplicate knife with id {knifeID}, ignoring this");
                        break;
                    }

                    if (levelRequirement == 0)
                        defaultKnives.Add(knife);
                }

                Logging.Debug($"Successfully read {knives.Count} knives from the table");
                Knives = knives;
            }
            catch (Exception ex)
            {
                Logger.Log("Error reading data from the knives table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug("Reading gadgets from base data");
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{GADGETS}`;", conn).ExecuteReaderAsync();
            try
            {
                Dictionary<ushort, Gadget> gadgets = new();
                while (await rdr.ReadAsync())
                {
                    if (!ushort.TryParse(rdr[0].ToString(), out var gadgetID))
                        continue;

                    var gadgetName = rdr[1].ToString();
                    var gadgetDesc = rdr[2].ToString();
                    if (!Enum.TryParse(rdr[3].ToString(), true, out ERarity rarity))
                        continue;

                    var iconLink = rdr[4].ToString();
                    if (!int.TryParse(rdr[5].ToString(), out var coins))
                        continue;

                    if (!int.TryParse(rdr[6].ToString(), out var buyPrice))
                        continue;

                    if (!int.TryParse(rdr[7].ToString(), out var scrapAmount))
                        continue;

                    if (!int.TryParse(rdr[8].ToString(), out var giveSeconds))
                        continue;

                    if (!int.TryParse(rdr[9].ToString(), out var levelRequirement))
                        continue;

                    if (!bool.TryParse(rdr[10].ToString(), out var isTactical))
                        continue;

                    Gadget gadget = new(gadgetID, gadgetName, gadgetDesc, rarity, iconLink, coins, buyPrice, scrapAmount, giveSeconds, levelRequirement, isTactical);
                    if (!gadgets.ContainsKey(gadgetID))
                        gadgets.Add(gadgetID, gadget);
                    else
                    {
                        Logging.Debug($"Found a duplicate gadget with id {gadgetID}, ignoring this");
                        break;
                    }

                    if (levelRequirement == 0)
                        defaultGadgets.Add(gadget);
                    else if (levelRequirement > 0)
                    {
                        if (!itemsSearchByLevel.ContainsKey(levelRequirement))
                            itemsSearchByLevel.Add(levelRequirement, new());

                        itemsSearchByLevel[levelRequirement].Add(new(gadget.IconLink, isTactical ? "TACTICAL" : "LETHAL", gadget.GadgetName));
                    }
                }

                Logging.Debug($"Successfully read {gadgets.Count} gadgets from the table");
                Gadgets = gadgets;
            }
            catch (Exception ex)
            {
                Logger.Log("Error reading data from gadgets table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug("Reading killstreaks from base data");
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{KILLSTREAKS}`;", conn).ExecuteReaderAsync();
            try
            {
                Dictionary<int, Killstreak> killstreaks = new();
                while (await rdr.ReadAsync())
                {
                    if (!int.TryParse(rdr[0].ToString(), out var killstreakID))
                        continue;

                    var killstreakName = rdr[1].ToString();
                    var killstreakDesc = rdr[2].ToString();
                    if (!Enum.TryParse(rdr[3].ToString(), true, out ERarity rarity))
                        continue;

                    var iconLink = rdr[4].ToString();
                    if (!int.TryParse(rdr[5].ToString(), out var killstreakRequired))
                        continue;

                    if (!int.TryParse(rdr[6].ToString(), out var buyPrice))
                        continue;

                    if (!int.TryParse(rdr[7].ToString(), out var coins))
                        continue;

                    if (!int.TryParse(rdr[8].ToString(), out var scrapAmount))
                        continue;

                    if (!int.TryParse(rdr[9].ToString(), out var levelRequirement))
                        continue;

                    var killstreakInfo = Plugin.Instance.Config.Killstreaks.FileData.KillstreaksData.FirstOrDefault(k => k.KillstreakID == killstreakID);
                    if (killstreakInfo == null)
                    {
                        Logging.Debug($"Error finding killstreak info for killstreak with id {killstreakID}, ignoring");
                        continue;
                    }

                    Killstreak killstreak = new(killstreakID, killstreakName, killstreakDesc, rarity, iconLink, killstreakRequired, buyPrice, coins, scrapAmount, levelRequirement, killstreakInfo);
                    if (!killstreaks.ContainsKey(killstreakID))
                        killstreaks.Add(killstreakID, killstreak);
                    else
                    {
                        Logging.Debug($"Found a duplicate killstrea with id {killstreakID}, ignoring it");
                        break;
                    }

                    if (levelRequirement == 0)
                        defaultKillstreaks.Add(killstreak);
                    else if (levelRequirement > 0)
                    {
                        if (!itemsSearchByLevel.ContainsKey(levelRequirement))
                            itemsSearchByLevel.Add(levelRequirement, new());

                        itemsSearchByLevel[levelRequirement].Add(new(killstreak.IconLink, "KILLSTREAK", killstreak.KillstreakName));
                    }
                }

                Logging.Debug($"Successfully read {killstreaks.Count} killstreaks from table");
                Killstreaks = killstreaks;
            }
            catch (Exception ex)
            {
                Logger.Log("Error reading data from gadgets table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug("Reading perks from base data");
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PERKS}`;", conn).ExecuteReaderAsync();
            try
            {
                Dictionary<int, Perk> perks = new();
                while (await rdr.ReadAsync())
                {
                    if (!int.TryParse(rdr[0].ToString(), out var perkID))
                        continue;

                    var perkName = rdr[1].ToString();
                    var perkDesc = rdr[2].ToString();
                    if (!int.TryParse(rdr[3].ToString(), out var perkType))
                        continue;

                    if (!Enum.TryParse(rdr[4].ToString(), true, out ERarity rarity))
                        continue;

                    var iconLink = rdr[5].ToString();
                    var skillType = rdr[6].ToString();
                    if (!int.TryParse(rdr[7].ToString(), out var skillLevel))
                        continue;

                    if (!int.TryParse(rdr[8].ToString(), out var coins))
                        continue;

                    if (!int.TryParse(rdr[9].ToString(), out var buyPrice))
                        continue;

                    if (!int.TryParse(rdr[10].ToString(), out var scrapAmount))
                        continue;

                    if (!int.TryParse(rdr[11].ToString(), out var levelRequirement))
                        continue;

                    var stats = Utility.GetStatsFromString(rdr[12].ToString());
                    Perk perk = new(perkID, perkName, perkDesc, perkType, rarity, iconLink, skillType, skillLevel, coins, buyPrice, scrapAmount, levelRequirement, stats);
                    if (!perks.ContainsKey(perkID))
                        perks.Add(perkID, perk);
                    else
                    {
                        Logging.Debug($"Found a duplicate perk with id {perkID}, ignoring this");
                        break;
                    }

                    if (levelRequirement == 0)
                        defaultPerks.Add(perk);
                    else if (levelRequirement > 0)
                    {
                        if (!itemsSearchByLevel.ContainsKey(levelRequirement))
                            itemsSearchByLevel.Add(levelRequirement, new());

                        itemsSearchByLevel[levelRequirement].Add(new(perk.IconLink, "PERK", perk.PerkName));
                    }
                }

                Logging.Debug($"Successfully read {perks.Count} perks from the table");
                Perks = perks;
            }
            catch (Exception ex)
            {
                Logger.Log("Error reading from perks table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug("Reading gloves from base data");
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{GLOVES}`;", conn).ExecuteReaderAsync();
            try
            {
                Dictionary<int, Glove> gloves = new();
                while (await rdr.ReadAsync())
                {
                    if (!int.TryParse(rdr[0].ToString(), out var gloveID))
                        continue;

                    var gloveName = rdr[1].ToString();
                    var gloveDesc = rdr[2].ToString();
                    if (!Enum.TryParse(rdr[3].ToString(), true, out ERarity rarity))
                        continue;

                    var iconLink = rdr[4].ToString();
                    if (!int.TryParse(rdr[5].ToString(), out var scrapAmount))
                        continue;

                    if (!int.TryParse(rdr[6].ToString(), out var buyPrice))
                        continue;

                    if (!int.TryParse(rdr[7].ToString(), out var coins))
                        continue;

                    if (!int.TryParse(rdr[8].ToString(), out var levelRequirement))
                        continue;

                    if (!int.TryParse(rdr[9].ToString(), out var gloveWeight))
                        continue;

                    if (!int.TryParse(rdr[10].ToString(), out var maxAmount))
                        continue;

                    if (!int.TryParse(rdr[11].ToString(), out var unboxedAmount))
                        continue;

                    Glove glove = new(gloveID, gloveName, gloveDesc, rarity, iconLink, scrapAmount, buyPrice, coins, levelRequirement, gloveWeight, maxAmount, unboxedAmount);
                    if (!gloves.ContainsKey(gloveID))
                        gloves.Add(gloveID, glove);
                    else
                    {
                        Logging.Debug($"Found a duplicate glove with id {gloveID}");
                        break;
                    }

                    if (levelRequirement == 0)
                        defaultGloves.Add(glove);
                    else if (levelRequirement > 0)
                    {
                        if (!itemsSearchByLevel.ContainsKey(levelRequirement))
                            itemsSearchByLevel.Add(levelRequirement, new());

                        itemsSearchByLevel[levelRequirement].Add(new(glove.IconLink, "GLOVE", glove.GloveName));
                    }
                }

                Logging.Debug($"Successfully read {gloves.Count} gloves from the table");
                Gloves = gloves;
            }
            catch (Exception ex)
            {
                Logger.Log("Error reading from gloves table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug("Reading cards from base data");
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{CARDS}`;", conn).ExecuteReaderAsync();
            try
            {
                Dictionary<int, Card> cards = new();
                while (await rdr.ReadAsync())
                {
                    if (!int.TryParse(rdr[0].ToString(), out var cardID))
                        continue;

                    var cardName = rdr[1].ToString();
                    var cardDesc = rdr[2].ToString();
                    if (!Enum.TryParse(rdr[3].ToString(), true, out ERarity rarity))
                        continue;

                    var iconLink = rdr[4].ToString();
                    var cardLink = rdr[5].ToString();
                    if (!int.TryParse(rdr[6].ToString(), out var scrapAmount))
                        continue;

                    if (!int.TryParse(rdr[7].ToString(), out var buyPrice))
                        continue;

                    if (!int.TryParse(rdr[8].ToString(), out var coins))
                        continue;

                    if (!int.TryParse(rdr[9].ToString(), out var levelRequirement))
                        continue;

                    var authorCredits = rdr[10].ToString();

                    if (!int.TryParse(rdr[11].ToString(), out var unboxedAmount))
                        continue;
                    
                    Card card = new(cardID, cardName, cardDesc, rarity, iconLink, cardLink, scrapAmount, buyPrice, coins, levelRequirement, authorCredits, unboxedAmount);
                    if (!cards.ContainsKey(cardID))
                        cards.Add(cardID, card);
                    else
                    {
                        Logging.Debug($"Found a duplicate card with id {cardID}, ignoring this");
                        break;
                    }

                    if (levelRequirement == 0)
                        defaultCards.Add(card);
                    else if (levelRequirement > 0)
                    {
                        if (!itemsSearchByLevel.ContainsKey(levelRequirement))
                            itemsSearchByLevel.Add(levelRequirement, new());

                        itemsSearchByLevel[levelRequirement].Add(new(card.IconLink, "CARD", card.CardName));
                    }
                }

                Logging.Debug($"Successfully read {cards.Count} cards from the table");
                Cards = cards;
            }
            catch (Exception ex)
            {
                Logger.Log("Error reading data from cards table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            ItemsSearchByLevel = itemsSearchByLevel;

            Logging.Debug("Reading levels from base data");
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{LEVELS}`;", conn).ExecuteReaderAsync();
            try
            {
                Dictionary<int, XPLevel> levels = new();
                while (await rdr.ReadAsync())
                {
                    if (!int.TryParse(rdr[0].ToString(), out var level))
                        continue;

                    if (!int.TryParse(rdr[1].ToString(), out var xpNeeded))
                        continue;

                    var iconLinkLarge = rdr[2].ToString();
                    var iconLinkMedium = rdr[3].ToString();
                    var iconLinkSmall = rdr[4].ToString();

                    if (!levels.ContainsKey(level))
                        levels.Add(level, new(level, xpNeeded, iconLinkLarge, iconLinkMedium, iconLinkSmall));
                }

                Logging.Debug($"Successfully read {levels.Count} levels from the table");
                Levels = levels;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading data from levels table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug("Reading quests from base data");
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT `QuestID`, `QuestTitle`, `QuestDesc`, `QuestType`-1, `QuestTier`-1, `QuestConditions`, `TargetAmount`, `XP` FROM `{QUESTS}`;", conn).ExecuteReaderAsync();
            try
            {
                Dictionary<int, Quest> questsSearchByID = new();
                List<Quest> quests = new();

                while (await rdr.ReadAsync())
                {
                    if (!int.TryParse(rdr[0].ToString(), out var questID))
                        continue;

                    var questTitle = rdr[1].ToString();
                    var questDesc = rdr[2].ToString();

                    if (!int.TryParse(rdr[3].ToString(), out var questTypeInt))
                        continue;

                    var questType = (EQuestType)questTypeInt;

                    if (!int.TryParse(rdr[4].ToString(), out var questTierInt))
                        continue;

                    var questTier = (EQuestTier)questTierInt;

                    var questConditions = rdr[5].ToString();
                    var conditions = Utility.GetQuestConditionsFromString(questConditions);

                    if (!int.TryParse(rdr[6].ToString(), out var targetAmount))
                        continue;

                    if (!int.TryParse(rdr[7].ToString(), out var xp))
                        continue;

                    Quest quest = new(questID, questTitle, questDesc, questType, questTier, conditions, targetAmount, xp);
                    if (!questsSearchByID.ContainsKey(questID))
                    {
                        questsSearchByID.Add(questID, quest);
                        quests.Add(quest);
                    }
                    else
                        Logging.Debug($"Found a duplicate quest with id {questID}, ignoring this");
                }

                QuestsSearchByID = questsSearchByID;
                Quests = quests;

                Logging.Debug($"Successfully read {quests.Count} quests from the table");
            }
            catch (Exception ex)
            {
                Logger.Log("Error reading data from quests table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug("Reading achievements for base data");
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT `AchievementID`, `AchievementType`-1, `AchievementConditions`, `PageID` FROM `{ACHIEVEMENTS}`;", conn).ExecuteReaderAsync();
            try
            {
                List<Achievement> achievements = new();
                Dictionary<int, Achievement> achievementsSearchByID = new();
                while (await rdr.ReadAsync())
                {
                    if (!int.TryParse(rdr[0].ToString(), out var achievementID))
                        continue;

                    if (!int.TryParse(rdr[1].ToString(), out var achievementTypeInt))
                        continue;

                    var achievementType = (EQuestType)achievementTypeInt;
                    var achievementConditions = rdr[2].ToString();
                    var conditions = Utility.GetQuestConditionsFromString(achievementConditions);
                    if (!int.TryParse(rdr[3].ToString(), out var pageID))
                        continue;

                    Achievement achievement = new(achievementID, achievementType, conditions, new(), new(), pageID);
                    if (!achievementsSearchByID.ContainsKey(achievementID))
                    {
                        achievementsSearchByID.Add(achievementID, achievement);
                        achievements.Add(achievement);
                    }
                    else
                        Logging.Debug($"Found a duplicate achievement with id {achievementID}, ignoring this");
                }

                AchievementsSearchByID = achievementsSearchByID;
                Achievements = achievements;

                Logging.Debug($"Successfully read {achievements.Count} achievements from the table");
            }
            catch (Exception ex)
            {
                Logger.Log("Error reading data from achievements table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug("Reading achievements tiers for base data");
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{ACHIEVEMENTS_TIERS}`;", conn).ExecuteReaderAsync();
            try
            {
                while (await rdr.ReadAsync())
                {
                    if (!int.TryParse(rdr[0].ToString(), out var achievementID))
                        continue;

                    if (!AchievementsSearchByID.TryGetValue(achievementID, out var achievement))
                        continue;

                    if (!int.TryParse(rdr[1].ToString(), out var tierID))
                        continue;

                    var tierTitle = rdr[2].ToString();
                    var tierDesc = rdr[3].ToString();
                    var tierColor = rdr[4].ToString();
                    var tierPrevSmall = rdr[5].ToString();
                    var tierPrevLarge = rdr[6].ToString();
                    if (!int.TryParse(rdr[7].ToString(), out var targetAmount))
                        continue;

                    var rewards = Utility.GetRewardsFromString(rdr[8].ToString());
                    var removeRewards = Utility.GetRewardsFromString(rdr[9].ToString());
                    AchievementTier achievementTier = new(achievement, tierID, tierTitle, tierDesc, tierColor, tierPrevSmall, tierPrevLarge, targetAmount, rewards, removeRewards);

                    if (!achievement.TiersLookup.ContainsKey(tierID))
                    {
                        achievement.TiersLookup.Add(tierID, achievementTier);
                        achievement.Tiers.Add(achievementTier);
                    }
                    else
                        Logging.Debug($"Found a duplicate achievement tier with id {tierID} for achievement with id {achievementID}, ignoring this");
                }

                Logging.Debug($"Loaded total {Achievements.Sum(k => k.Tiers.Count)} for {Achievements.Count} achievements");
            }
            catch (Exception ex)
            {
                Logger.Log("Error reading data from achievements table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug("Reading battlepass tiers for base data");
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{BATTLEPASS}`;", conn).ExecuteReaderAsync();
            try
            {
                Dictionary<int, BattlepassTier> battlepassTiersSearchByID = new();
                List<BattlepassTier> battlepassTiers = new();
                while (await rdr.ReadAsync())
                {
                    if (!int.TryParse(rdr[0].ToString(), out var tierID))
                        continue;

                    var freeReward = Utility.GetRewardFromString(rdr[1].ToString());
                    var premiumReward = Utility.GetRewardFromString(rdr[2].ToString());

                    if (!int.TryParse(rdr[3].ToString(), out var xp))
                        continue;

                    BattlepassTier battlepass = new(tierID, freeReward, premiumReward, xp);

                    if (battlepassTiersSearchByID.ContainsKey(tierID))
                    {
                        Logging.Debug($"Found a duplicate battlepass tier with id {tierID}, ignoring");
                        continue;
                    }

                    battlepassTiersSearchByID.Add(tierID, battlepass);
                    battlepassTiers.Add(battlepass);
                }

                Logging.Debug($"Successfully read {battlepassTiers.Count} battlepass tiers from the table");
                BattlepassTiersSearchByID = battlepassTiersSearchByID;
                BattlepassTiers = battlepassTiers;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading battlepass tiers from battlepass table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug("Reading cases for base data");
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{CASES}`;", conn).ExecuteReaderAsync();
            try
            {
                Dictionary<int, Case> cases = new();
                while (await rdr.ReadAsync())
                {
                    if (!int.TryParse(rdr[0].ToString(), out var caseID))
                        continue;

                    var caseName = rdr[1].ToString();
                    var iconLink = rdr[2].ToString();
                    if (!Enum.TryParse(rdr[3].ToString(), true, out ERarity caseRarity))
                        continue;

                    if (!bool.TryParse(rdr[4].ToString(), out var isBuyable))
                        continue;

                    if (!int.TryParse(rdr[5].ToString(), out var scrapPrice))
                        continue;

                    if (!int.TryParse(rdr[6].ToString(), out var coinPrice))
                        continue;

                    List<(ECaseRarity, int)> caseRarities = new();

                    var shouldContinue = true;
                    for (var i = 7; i <= 18; i++)
                    {
                        var rarity = (ECaseRarity)(i - 7);
                        if (!int.TryParse(rdr[i].ToString(), out var weight))
                        {
                            shouldContinue = false;
                            break;
                        }

                        if (weight > 0)
                            caseRarities.Add((rarity, weight));
                    }

                    if (!shouldContinue)
                        continue;

                    var availableSkinIDs = rdr[19].GetIntListFromReaderResult();
                    List<GunSkin> availableSkins = new();
                    Dictionary<ERarity, List<GunSkin>> availableSkinsSearchByRarity = new();

                    foreach (var skinID in availableSkinIDs)
                    {
                        if (!GunSkinsSearchByID.TryGetValue(skinID, out var skin))
                        {
                            Logging.Debug($"Case with id {caseID} has a skin with id {skinID} which is not a valid skin registered in the database");
                            continue;
                        }

                        if (availableSkins.Contains(skin))
                        {
                            Logging.Debug($"Case with id {caseID} has a skin with id {skinID} which is a duplicate, the same skin is already added to the case");
                            continue;
                        }

                        if (!availableSkinsSearchByRarity.ContainsKey(skin.SkinRarity))
                            availableSkinsSearchByRarity.Add(skin.SkinRarity, new());

                        availableSkinsSearchByRarity[skin.SkinRarity].Add(skin);
                        availableSkins.Add(skin);
                    }

                    if (!int.TryParse(rdr[20].ToString(), out var unboxedAmount))
                        continue;

                    if (!bool.TryParse(rdr[21].ToString(), out var showLimiteds))
                        continue;
                    
                    if (cases.ContainsKey(caseID))
                    {
                        Logging.Debug($"Found a case with id {caseID} already registered, ignoring");
                        continue;
                    }

                    cases.Add(caseID, new(caseID, caseName, iconLink, caseRarity, isBuyable, scrapPrice, coinPrice, caseRarities, availableSkins, availableSkinsSearchByRarity, unboxedAmount, showLimiteds));
                }

                Logging.Debug($"Successfully read {cases.Count} cases from base data");
                Cases = cases;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading cases from cases table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug("Reading servers for base data");
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{SERVERS}`;", conn).ExecuteReaderAsync();
            try
            {
                List<Server> servers = new();
                while (await rdr.ReadAsync())
                {
                    var ip = rdr[0].ToString();
                    var port = rdr[1].ToString();
                    var serverName = rdr[2].ToString();
                    var friendlyIP = rdr[3].ToString();
                    var serverBanner = rdr[4].ToString();
                    var serverDesc = rdr[5].ToString();

                    Server server = new(ip, port, serverName, friendlyIP, serverBanner, serverDesc);

                    servers.Add(server);
                }

                Logging.Debug($"Successfully read {servers.Count} servers from the table");
                Servers = servers;
            }
            catch (Exception ex)
            {
                Logger.Log("Error reading data from servers table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug("Building a default loadout for new players");
            try
            {
                var defaultPrimary = defaultGuns.FirstOrDefault(k => k.IsPrimary);
                Logging.Debug($"Found default primary with id {defaultPrimary?.GunID ?? 0}");
                List<ushort> defaultPrimaryAttachments = new();
                if (defaultPrimary != null)
                {
                    Dictionary<EAttachment, GunAttachment> defaultAttachments = new();
                    foreach (var defaultAttachment in defaultPrimary.DefaultAttachments)
                    {
                        if (!defaultAttachments.ContainsKey(defaultAttachment.AttachmentType))
                            defaultAttachments.Add(defaultAttachment.AttachmentType, defaultAttachment);
                    }

                    defaultPrimaryAttachments = defaultAttachments.Values.Select(k => k.AttachmentID).ToList();
                }

                Logging.Debug($"Found {defaultPrimaryAttachments.Count} default primary attachments");
                var defaultSecondary = defaultGuns.FirstOrDefault(k => !k.IsPrimary);
                Logging.Debug($"Found default secondary with id {defaultSecondary?.GunID ?? 0}");
                List<ushort> defaultSecondaryAttachments = new();
                if (defaultSecondary != null)
                {
                    Dictionary<EAttachment, GunAttachment> defaultAttachments = new();
                    foreach (var defaultAttachment in defaultSecondary.DefaultAttachments)
                    {
                        if (!defaultAttachments.ContainsKey(defaultAttachment.AttachmentType))
                            defaultAttachments.Add(defaultAttachment.AttachmentType, defaultAttachment);
                    }

                    defaultSecondaryAttachments = defaultAttachments.Values.Select(k => k.AttachmentID).ToList();
                }

                Logging.Debug($"Found {defaultSecondaryAttachments.Count} default secondary attachments");
                List<int> defaultPerk = new();
                for (var i = 1; i <= 3; i++)
                {
                    var randomPerks = defaultPerks.Where(k => k.PerkType == i).ToList();
                    if (randomPerks.Count == 0)
                        continue;

                    var randomPerk = randomPerks[UnityEngine.Random.Range(0, randomPerks.Count)];
                    defaultPerk.Add(randomPerk.PerkID);
                }

                Logging.Debug($"Found {defaultPerk.Count} default perks");
                var defaultKnife = defaultKnives.Count > 0 ? defaultKnives[0] : null;
                Logging.Debug($"Found default knife with id {defaultKnife?.KnifeID ?? 0}");
                var defaultTactical = defaultGadgets.FirstOrDefault(k => k.IsTactical);
                Logging.Debug($"Found default tactical with id {defaultTactical?.GadgetID ?? 0}");
                var defaultLethal = defaultGadgets.FirstOrDefault(k => !k.IsTactical);
                Logging.Debug($"Found default lethal with id {defaultLethal?.GadgetID ?? 0}");
                List<int> defaultKillstreak = new();
                foreach (var killstreak in defaultKillstreaks)
                {
                    defaultKillstreak.Add(killstreak.KillstreakID);
                    if (defaultKillstreaks.Count == 3)
                        break;
                }

                Logging.Debug($"Found {defaultKillstreak.Count} default killstreaks");
                var defaultGlove = defaultGloves.FirstOrDefault();
                Logging.Debug($"Found default glove with id {defaultGlove?.GloveID ?? 0}");
                var defaultCard = defaultCards.FirstOrDefault();
                Logging.Debug($"Found default card with id {defaultCard?.CardID ?? 0}");
                DefaultLoadout = new("DEFAULT LOADOUT", defaultPrimary?.GunID ?? 0, 0, 0, defaultPrimaryAttachments, defaultSecondary?.GunID ?? 0, 0, 0, defaultSecondaryAttachments, defaultKnife?.KnifeID ?? 0, defaultTactical?.GadgetID ?? 0, defaultLethal?.GadgetID ?? 0, defaultKillstreak,
                    defaultPerk, defaultGlove?.GloveID ?? 0, defaultCard?.CardID ?? 0);

                Logging.Debug("Built a default loadout to give to the players when they join");
            }
            catch (Exception ex)
            {
                Logger.Log("Error building default loadout for players");
                Logger.Log(ex);
            }
            finally
            {
                DefaultGuns = defaultGuns;
                DefaultKnives = defaultKnives;
                DefaultGadgets = defaultGadgets;
                DefaultKillstreaks = defaultKillstreaks;
                DefaultPerks = defaultPerks;
                DefaultGloves = defaultGloves;
                DefaultCards = defaultCards;
            }
        }
        catch (Exception ex)
        {
            Logger.Log("Error getting base data");
            Logger.Log(ex);
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    public async Task AddPlayerAsync(UnturnedPlayer player, string steamName, string avatarLink, string countryCode)
    {
        using MySqlConnection conn = new(ConnectionString);
        try
        {
            Logging.Debug($"Adding {steamName} to the DB");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.1f)), "LOADING PLAYER DATA..."));
            await conn.OpenAsync();
            MySqlCommand cmd =
                new(
                    $"INSERT INTO `{PLAYERS}` ( `SteamID` , `SteamName` , `AvatarLink` , `CountryCode` , `MuteExpiry`, `MuteReason`, `Coins` , `Hotkeys` ) VALUES ({player.CSteamID}, @name, '{avatarLink}' , '{countryCode}' , {DateTimeOffset.UtcNow.ToUnixTimeSeconds()} , ' ', {(Plugin.Instance.Configuration.Instance.UnlockAllItems ? 10000000 : 0)} , '4,3,5,6,7' ) ON DUPLICATE KEY UPDATE `AvatarLink` = '{avatarLink}', `SteamName` = @name, `CountryCode` = '{countryCode}';",
                    conn);

            _ = cmd.Parameters.AddWithValue("@name", steamName.ToUnrich());
            _ = await cmd.ExecuteScalarAsync();

            _ = await new MySqlCommand($"INSERT IGNORE INTO `{PLAYERS_LEADERBOARD_DAILY}` ( `SteamID` ) VALUES ({player.CSteamID});", conn).ExecuteScalarAsync();
            _ = await new MySqlCommand($"INSERT IGNORE INTO `{PLAYERS_LEADERBOARD_WEEKLY}` ( `SteamID` ) VALUES ({player.CSteamID});", conn).ExecuteScalarAsync();
            _ = await new MySqlCommand($"INSERT IGNORE INTO `{PLAYERS_LEADERBOARD_SEASONAL}` ( `SteamID` ) VALUES ({player.CSteamID});", conn).ExecuteScalarAsync();

            Logging.Debug($"Giving {steamName} the guns");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.15f)), "LOADING GUNS..."));
            foreach (var gun in Guns.Values)
            {
                if (gun.LevelRequirement < 0)
                    continue;

                _ = await new MySqlCommand(
                    $"INSERT IGNORE INTO `{PLAYERS_GUNS}` (`SteamID` , `GunID` , `Level` , `XP` , `GunKills` , `IsBought` , `Attachments`) VALUES ({player.CSteamID} , {gun.GunID} , 1 , 0 , 0 , {gun.LevelRequirement == 0} , '{Utility.CreateStringFromDefaultAttachments(gun.DefaultAttachments) + Utility.CreateStringFromRewardAttachments(gun.RewardAttachments.Values.ToList())}');",
                    conn).ExecuteScalarAsync();
            }

            _ = await new MySqlCommand($"INSERT IGNORE INTO `{PLAYERS_GUNS_SKINS}` (`SteamID` , `SkinIDs`) VALUES ({player.CSteamID}, '');", conn).ExecuteScalarAsync();

            Logging.Debug($"Giving {steamName} the gun charms");
            foreach (var gunCharm in GunCharms.Values)
            {
                if (gunCharm.LevelRequirement < 0)
                    continue;

                _ = await new MySqlCommand($"INSERT IGNORE INTO `{PLAYERS_GUNS_CHARMS}` (`SteamID` , `CharmID` , `IsBought`) VALUES ({player.CSteamID} , {gunCharm.CharmID} , {gunCharm.LevelRequirement == 0});", conn).ExecuteScalarAsync();
            }

            Logging.Debug($"Giving {steamName} the knives");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.2f)), "LOADING KNIVES..."));
            foreach (var knife in Knives.Values)
            {
                if (knife.LevelRequirement < 0)
                    continue;

                _ = await new MySqlCommand($"INSERT IGNORE INTO `{PLAYERS_KNIVES}` (`SteamID` , `KnifeID` , `KnifeKills` , `IsBought`) VALUES ({player.CSteamID} , {knife.KnifeID} , 0 , {knife.LevelRequirement == 0});", conn).ExecuteScalarAsync();
            }

            Logging.Debug($"Giving {steamName} the gadgets");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.25f)), "LOADING GADGETS..."));
            foreach (var gadget in Gadgets.Values)
            {
                if (gadget.LevelRequirement < 0)
                    continue;

                _ = await new MySqlCommand($"INSERT IGNORE INTO  `{PLAYERS_GADGETS}` (`SteamID` , `GadgetID` , `GadgetKills` , `IsBought`) VALUES ({player.CSteamID} , {gadget.GadgetID} , 0 , {gadget.LevelRequirement == 0});", conn).ExecuteScalarAsync();
            }

            Logging.Debug($"Giving {steamName} the killstreaks");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.3f)), "LOADING KILLSTREAKS..."));
            foreach (var killstreak in Killstreaks.Values)
            {
                if (killstreak.LevelRequirement < 0)
                    continue;

                _ = await new MySqlCommand($"INSERT IGNORE INTO `{PLAYERS_KILLSTREAKS}` (`SteamID` , `KillstreakID` , `KillstreakKills` , `IsBought`) VALUES ({player.CSteamID} , {killstreak.KillstreakID} , 0 ,  {killstreak.LevelRequirement == 0});", conn).ExecuteScalarAsync();
            }

            Logging.Debug($"Giving {steamName} the perks");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.35f)), "LOADING PERKS..."));
            foreach (var perk in Perks.Values)
            {
                if (perk.LevelRequirement < 0)
                    continue;

                _ = await new MySqlCommand($"INSERT IGNORE INTO `{PLAYERS_PERKS}` (`SteamID` , `PerkID` , `IsBought`) VALUES ({player.CSteamID} , {perk.PerkID} , {perk.LevelRequirement == 0});", conn).ExecuteScalarAsync();
            }

            Logging.Debug($"Giving {steamName} the gloves");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.4f)), "LOADING GLOVES..."));
            foreach (var glove in Gloves.Values)
            {
                if (glove.LevelRequirement < 0)
                    continue;

                _ = await new MySqlCommand($"INSERT IGNORE INTO `{PLAYERS_GLOVES}` (`SteamID` , `GloveID` , `IsBought`) VALUES ({player.CSteamID} , {glove.GloveID} , {glove.LevelRequirement == 0});", conn).ExecuteScalarAsync();
            }

            Logging.Debug($"Giving {steamName} the cards");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.45f)), "LOADING CARDS..."));
            foreach (var card in Cards.Values)
            {
                if (card.LevelRequirement < 0)
                    continue;

                _ = await new MySqlCommand($"INSERT IGNORE INTO `{PLAYERS_CARDS}` (`SteamID` , `CardID` , `IsBought`) VALUES ({player.CSteamID} , {card.CardID} ,  {card.LevelRequirement == 0});", conn).ExecuteScalarAsync();
            }

            Logging.Debug($"Giving {steamName} the battlepass");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.47f)), "LOADING BATTLEPASS..."));
            _ = await new MySqlCommand($"INSERT IGNORE INTO `{PLAYERS_BATTLEPASS}` (`SteamID` , `ClaimedFreeRewards` , `ClaimedPremiumRewards`) VALUES ({player.CSteamID} , '' , '');", conn).ExecuteScalarAsync();

            Logging.Debug($"Giving {steamName} the achievements");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.5f)), "LOADING ACHIEVEMENTS..."));
            foreach (var achievement in Achievements)
                _ = await new MySqlCommand($"INSERT IGNORE INTO `{PLAYERS_ACHIEVEMENTS}` (`SteamID`, `AchievementID`) VALUES ({player.CSteamID}, {achievement.AchievementID});", conn).ExecuteScalarAsync();

            var obj = await new MySqlCommand($"SELECT `HasPrime` FROM `{PLAYERS}` WHERE `SteamID` = {player.CSteamID} AND `PrimeExpiry` > {DateTimeOffset.UtcNow.ToUnixTimeSeconds()};", conn).ExecuteScalarAsync();
            var loadoutAmount = obj != null && bool.TryParse(obj.ToString(), out var hasPrime) && hasPrime ? Plugin.Instance.Config.Base.FileData.PrimeLoadoutAmount : Plugin.Instance.Config.Base.FileData.DefaultLoadoutAmount;
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.51f)), "LOADING LOADOUTS..."));
            Logging.Debug($"{steamName} should have {loadoutAmount} loadouts, adding them");
            var data = Plugin.Instance.Data.ConvertLoadoutToJson(DefaultLoadout);
            for (var i = 1; i <= loadoutAmount; i++)
                _ = await new MySqlCommand($"INSERT IGNORE INTO `{PLAYERS_LOADOUTS}` (`SteamID` , `LoadoutID` , `IsActive` , `Loadout`) VALUES ({player.CSteamID}, {i}, {i == 1}, '{data}');", conn).ExecuteScalarAsync();
        }
        catch (Exception ex)
        {
            Logger.Log($"Error adding player with Steam ID {player.CSteamID}, Steam Name {steamName}, avatar link {avatarLink}");
            Logger.Log(ex);
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    public async Task GetPlayerDataAsync(UnturnedPlayer player)
    {
        using MySqlConnection conn = new(ConnectionString);
        try
        {
            Logging.Debug($"Getting data for {player.CharacterName} from the main table");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.6f)), "PREPARING PLAYER DATA..."));
            await conn.OpenAsync();
            var rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PLAYERS}` WHERE `SteamID` = {player.CSteamID};", conn).ExecuteReaderAsync();
            try
            {
                while (await rdr.ReadAsync())
                {
                    var steamName = rdr[1].ToString();
                    var avatarLinks = rdr[2].ToString().Split(',').ToList();
                    var countryCode = rdr[3].ToString();

                    if (!bool.TryParse(rdr[4].ToString(), out var hideFlag))
                        continue;

                    if (!int.TryParse(rdr[5].ToString(), out var xp))
                        continue;

                    if (!int.TryParse(rdr[6].ToString(), out var level))
                        continue;

                    if (!int.TryParse(rdr[7].ToString(), out var credits))
                        continue;

                    if (!int.TryParse(rdr[8].ToString(), out var scrap))
                        continue;

                    if (!int.TryParse(rdr[9].ToString(), out var coins))
                        continue;

                    if (!int.TryParse(rdr[10].ToString(), out var kills))
                        continue;

                    if (!int.TryParse(rdr[11].ToString(), out var headshotKills))
                        continue;

                    if (!int.TryParse(rdr[12].ToString(), out var highestKillstreak))
                        continue;

                    if (!int.TryParse(rdr[13].ToString(), out var highestMultiKills))
                        continue;

                    if (!int.TryParse(rdr[14].ToString(), out var killsConfirmed))
                        continue;

                    if (!int.TryParse(rdr[15].ToString(), out var killsDenied))
                        continue;

                    if (!int.TryParse(rdr[16].ToString(), out var flagsCaptured))
                        continue;

                    if (!int.TryParse(rdr[17].ToString(), out var flagsSaved))
                        continue;

                    if (!int.TryParse(rdr[18].ToString(), out var areasTaken))
                        continue;

                    if (!int.TryParse(rdr[19].ToString(), out var deaths))
                        continue;

                    if (!bool.TryParse(rdr[20].ToString(), out var music))
                        continue;

                    if (!bool.TryParse(rdr[21].ToString(), out var isMuted))
                        continue;

                    if (!long.TryParse(rdr[22].ToString(), out var muteUnixSeconds))
                        continue;

                    var muteExpiry = DateTimeOffset.FromUnixTimeSeconds(muteUnixSeconds);

                    var muteReason = rdr[23].ToString();
                    if (!bool.TryParse(rdr[24].ToString(), out var hasBattlepass))
                        continue;

                    if (!float.TryParse(rdr[25].ToString(), out var xpBooster))
                        continue;

                    if (!float.TryParse(rdr[26].ToString(), out var bpBooster))
                        continue;

                    if (!float.TryParse(rdr[27].ToString(), out var gunXPBooster))
                        continue;

                    if (!bool.TryParse(rdr[28].ToString(), out var hasPrime))
                        continue;

                    if (!long.TryParse(rdr[29].ToString(), out var primeExpiryUnixSeconds))
                        continue;

                    var primeExpiry = DateTimeOffset.FromUnixTimeSeconds(primeExpiryUnixSeconds);

                    if (!long.TryParse(rdr[30].ToString(), out var primeLastDailyRewardUnixSeconds))
                        continue;

                    var primeLastDailyReward = DateTimeOffset.FromUnixTimeSeconds(primeLastDailyRewardUnixSeconds);

                    if (!int.TryParse(rdr[31].ToString(), out var volume))
                        continue;

                    var hotkeys = rdr[32].GetIntListFromReaderResult();

                    if (!bool.TryParse(rdr[33].ToString(), out var isStaff))
                        continue;
                    
                    if (PlayerData.ContainsKey(player.CSteamID))
                        _ = PlayerData.Remove(player.CSteamID);

                    PlayerData.Add(player.CSteamID,
                        new(player.CSteamID, steamName, avatarLinks, countryCode, hideFlag, xp, level, credits, scrap, coins, kills, headshotKills, highestKillstreak, highestMultiKills, killsConfirmed, killsDenied, flagsCaptured, flagsSaved, areasTaken, deaths, music, isMuted, muteExpiry, muteReason,
                            hasBattlepass, xpBooster, bpBooster, gunXPBooster, hasPrime, primeExpiry, primeLastDailyReward, volume, hotkeys, isStaff));
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading player data for {player.CharacterName}");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            if (!PlayerData.TryGetValue(player.CSteamID, out var playerData))
            {
                Logging.Debug($"Player {player.CharacterName} does not exist in the main table after getting the data, don't know what happened here");
                return;
            }
            
            /*Logging.Debug($"Getting all time data for {player.CharacterName} from the all time table");
            if (PlayerData.TryGetValue(player.CSteamID, out var playerData))
            {
                LeaderboardData leaderboardData = new(player.CSteamID, playerData.SteamName, playerData.CountryCode, playerData.HideFlag, playerData.Level, playerData.HasPrime, playerData.Kills, playerData.HeadshotKills, playerData.Deaths);
                if (!PlayerAllTimeLeaderboardLookup.ContainsKey(player.CSteamID))
                {
                    PlayerAllTimeLeaderboardLookup.Add(player.CSteamID, leaderboardData);
                    PlayerAllTimeKill.Add(leaderboardData);
                    PlayerAllTimeLevel.Add(leaderboardData);
                }
            }

            Logging.Debug($"Getting leaderboard daily data for {player.CharacterName} from the daily table");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.65f)), "PREPARING LEADERBOARD DATA..."));
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PLAYERS_LEADERBOARD_DAILY}` WHERE `SteamID` = {player.CSteamID};", conn).ExecuteReaderAsync();
            try
            {
                while (await rdr.ReadAsync())
                {
                    if (!PlayerData.TryGetValue(player.CSteamID, out var data))
                        continue;

                    if (!int.TryParse(rdr[1].ToString(), out var kills))
                        continue;

                    if (!int.TryParse(rdr[2].ToString(), out var headshotKills))
                        continue;

                    if (!int.TryParse(rdr[3].ToString(), out var deaths))
                        continue;

                    LeaderboardData leaderboardData = new(player.CSteamID, data.SteamName, data.CountryCode, data.HideFlag, data.Level, data.HasPrime, kills, headshotKills, deaths);
                    if (!PlayerDailyLeaderboardLookup.ContainsKey(player.CSteamID))
                    {
                        PlayerDailyLeaderboardLookup.Add(player.CSteamID, leaderboardData);
                        PlayerDailyLeaderboard.Add(leaderboardData);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Debug($"Error reading player daily data for {player.CharacterName}");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug($"Getting leaderboard weekly data for {player.CharacterName} from the weekly table");
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PLAYERS_LEADERBOARD_WEEKLY}` WHERE `SteamID` = {player.CSteamID};", conn).ExecuteReaderAsync();
            try
            {
                while (await rdr.ReadAsync())
                {
                    if (!PlayerData.TryGetValue(player.CSteamID, out var data))
                        continue;

                    if (!int.TryParse(rdr[1].ToString(), out var kills))
                        continue;

                    if (!int.TryParse(rdr[2].ToString(), out var headshotKills))
                        continue;

                    if (!int.TryParse(rdr[3].ToString(), out var deaths))
                        continue;

                    LeaderboardData leaderboardData = new(player.CSteamID, data.SteamName, data.CountryCode, data.HideFlag, data.Level, data.HasPrime, kills, headshotKills, deaths);
                    if (!PlayerWeeklyLeaderboardLookup.ContainsKey(player.CSteamID))
                    {
                        PlayerWeeklyLeaderboardLookup.Add(player.CSteamID, leaderboardData);
                        PlayerWeeklyLeaderboard.Add(leaderboardData);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Debug($"Error reading player weekly data for {player.CharacterName}");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug($"Getting leaderboard seasonal data for {player.CharacterName} from the seasonal table");
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PLAYERS_LEADERBOARD_SEASONAL}` WHERE `SteamID` = {player.CSteamID};", conn).ExecuteReaderAsync();
            try
            {
                while (await rdr.ReadAsync())
                {
                    if (!PlayerData.TryGetValue(player.CSteamID, out var data))
                        continue;

                    if (!int.TryParse(rdr[1].ToString(), out var kills))
                        continue;

                    if (!int.TryParse(rdr[2].ToString(), out var headshotKills))
                        continue;

                    if (!int.TryParse(rdr[3].ToString(), out var deaths))
                        continue;

                    LeaderboardData leaderboardData = new(player.CSteamID, data.SteamName, data.CountryCode, data.HideFlag, data.Level, data.HasPrime, kills, headshotKills, deaths);
                    if (!PlayerSeasonalLeaderboardLookup.ContainsKey(player.CSteamID))
                    {
                        PlayerSeasonalLeaderboardLookup.Add(player.CSteamID, leaderboardData);
                        PlayerSeasonalLeaderboard.Add(leaderboardData);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Debug($"Error reading player seasonal data for {player.CharacterName}");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }*/

            Logging.Debug($"Getting quests for {player.CharacterName}");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.7f)), "PREPARING QUESTS..."));
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PLAYERS_QUESTS}` WHERE `SteamID` = {player.CSteamID};", conn).ExecuteReaderAsync();
            try
            {
                List<PlayerQuest> playerQuests = new();
                Dictionary<EQuestType, List<PlayerQuest>> playerQuestsSearchByType = new();

                while (await rdr.ReadAsync())
                {
                    if (!int.TryParse(rdr[1].ToString(), out var questID))
                        continue;

                    if (!QuestsSearchByID.TryGetValue(questID, out var quest))
                    {
                        Logging.Debug($"Error finding quest with id {questID} for {player.CharacterName}, ignoring it");
                        continue;
                    }

                    if (!int.TryParse(rdr[2].ToString(), out var amount))
                        continue;

                    if (!long.TryParse(rdr[3].ToString(), out var questEndDate))
                        continue;

                    var questEndDateTime = DateTimeOffset.FromUnixTimeSeconds(questEndDate);

                    PlayerQuest playerQuest = new(player.CSteamID, quest, amount, questEndDateTime);
                    playerQuests.Add(playerQuest);
                    if (!playerQuestsSearchByType.ContainsKey(quest.QuestType))
                        playerQuestsSearchByType.Add(quest.QuestType, new());

                    playerQuestsSearchByType[quest.QuestType].Add(playerQuest);
                }

                Logging.Debug($"Got {playerQuests.Count} quests registered to player");

                rdr.Close();
                if (playerQuests.Count == 0 || playerQuests[0].QuestEnd.UtcDateTime < DateTime.UtcNow)
                {
                    Logging.Debug("Quests have expired, generate different quests");

                    playerQuests.Clear();
                    playerQuestsSearchByType.Clear();

                    _ = await new MySqlCommand($"DELETE FROM `{PLAYERS_QUESTS}` WHERE `SteamID` = {player.CSteamID};", conn).ExecuteScalarAsync();
                    var expiryDate = ServerOptions.DailyLeaderboardWipe;
                    List<Quest> questsToAdd = new();
                    for (var i = 0; i < 6; i++)
                    {
                        var randomQuests = Quests.Where(k => (int)k.QuestTier == i).ToList();
                        var randomQuest = randomQuests[UnityEngine.Random.Range(0, randomQuests.Count)];
                        questsToAdd.Add(randomQuest);
                    }

                    foreach (var quest in questsToAdd)
                    {
                        PlayerQuest playerQuest = new(player.CSteamID, quest, 0, expiryDate);
                        playerQuests.Add(playerQuest);
                        if (!playerQuestsSearchByType.ContainsKey(quest.QuestType))
                            playerQuestsSearchByType.Add(quest.QuestType, new());

                        playerQuestsSearchByType[quest.QuestType].Add(playerQuest);
                        _ = await new MySqlCommand($"INSERT INTO `{PLAYERS_QUESTS}` (`SteamID` , `QuestID`, `Amount`, `QuestEnd`) VALUES ({player.CSteamID}, {quest.QuestID}, 0, {expiryDate.ToUnixTimeSeconds()});", conn).ExecuteScalarAsync();
                    }

                    Logging.Debug($"Generated {playerQuests.Count} quests for player");
                }

                playerData.Quests = playerQuests;
                playerData.QuestsSearchByType = playerQuestsSearchByType;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading quests data for {player.CharacterName}");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.73f)), "PREPARING ACHIEVEMENTS..."));
            Logging.Debug($"Getting achievements for {player.CharacterName}");
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PLAYERS_ACHIEVEMENTS}` WHERE `SteamID` = {player.CSteamID};", conn).ExecuteReaderAsync();
            try
            {
                List<PlayerAchievement> achievements = new();
                Dictionary<EQuestType, List<PlayerAchievement>> achievementsSearchByType = new();
                Dictionary<int, PlayerAchievement> achievementsSearchByID = new();

                while (await rdr.ReadAsync())
                {
                    if (!int.TryParse(rdr[1].ToString(), out var achievementID))
                        continue;

                    if (!AchievementsSearchByID.TryGetValue(achievementID, out var achievement))
                    {
                        Logging.Debug($"Error finding achievement with id {achievementID} for {player.CharacterName}, ignoring");
                        continue;
                    }

                    if (!int.TryParse(rdr[2].ToString(), out var currentTier))
                        continue;

                    if (!int.TryParse(rdr[3].ToString(), out var amount))
                        continue;

                    PlayerAchievement playerAchievement = new(player.CSteamID, achievement, currentTier, amount);
                    if (!achievementsSearchByID.ContainsKey(achievementID))
                    {
                        achievementsSearchByID.Add(achievementID, playerAchievement);
                        if (!achievementsSearchByType.ContainsKey(achievement.AchievementType))
                            achievementsSearchByType.Add(achievement.AchievementType, new());

                        achievementsSearchByType[achievement.AchievementType].Add(playerAchievement);
                        achievements.Add(playerAchievement);
                    }
                    else
                        Logging.Debug($"Error, achievement {achievementID} already exists for {player.CharacterName}, ignoring");
                }

                Logging.Debug($"Got {achievements.Count} achievements registered to player");
                playerData.Achievements = achievements;
                playerData.AchievementsSearchByType = achievementsSearchByType;
                playerData.AchievementsSearchByID = achievementsSearchByID;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading achievements data for {player.CharacterName}");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            playerData.SetAchievementXPBooster();
            Logging.Debug($"Getting battlepass for {player.CharacterName}");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.8f)), "PREPARING BATTLEPASS..."));
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PLAYERS_BATTLEPASS}` WHERE `SteamID` = {player.CSteamID};", conn).ExecuteReaderAsync();
            try
            {
                while (await rdr.ReadAsync())
                {
                    if (!int.TryParse(rdr[1].ToString(), out var currentTier))
                        continue;

                    if (!int.TryParse(rdr[2].ToString(), out var xp))
                        continue;

                    var claimedFreeRewards = rdr[3].GetHashSetIntFromReaderResult();
                    var claimedPremiumRewards = rdr[4].GetHashSetIntFromReaderResult();

                    Logging.Debug($"Got battlepass with current tier {currentTier}, xp {xp} and claimed free rewards {claimedFreeRewards.Count} and claimed premium rewards {claimedPremiumRewards.Count} registered to the player");
                    playerData.Battlepass = new(player.CSteamID, currentTier, xp, claimedFreeRewards, claimedPremiumRewards);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading battlepass data for {player.CharacterName}");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            if (!PlayerData.ContainsKey(player.CSteamID))
            {
                Logging.Debug("Error finding player data, returning");
                return;
            }

            Dictionary<ushort, LoadoutGun> guns = new();
            Dictionary<ushort, LoadoutGunCharm> gunCharms = new();
            Dictionary<ushort, LoadoutKnife> knives = new();
            Dictionary<int, GunSkin> gunSkinsSearchByID = new();
            Dictionary<ushort, List<GunSkin>> gunSkinsSearchByGunID = new();
            Dictionary<ushort, GunSkin> gunSkinsSearchBySkinID = new();
            Dictionary<int, LoadoutPerk> perks = new();
            Dictionary<ushort, LoadoutGadget> gadgets = new();
            Dictionary<int, LoadoutKillstreak> killstreaks = new();
            Dictionary<int, LoadoutCard> cards = new();
            Dictionary<int, LoadoutGlove> gloves = new();
            Dictionary<int, Loadout> loadouts = new();

            Logging.Debug($"Getting guns for {player.CharacterName}");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.82f)), "PREPARING GUNS..."));
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PLAYERS_GUNS}` WHERE `SteamID` = {player.CSteamID};", conn).ExecuteReaderAsync();
            try
            {
                while (await rdr.ReadAsync())
                {
                    if (!ushort.TryParse(rdr[1].ToString(), out var gunID))
                        continue;

                    if (!Guns.TryGetValue(gunID, out var gun))
                    {
                        Logging.Debug($"Error finding gun with id {gunID}, ignoring it");
                        continue;
                    }

                    if (!int.TryParse(rdr[2].ToString(), out var level))
                        continue;

                    if (!int.TryParse(rdr[3].ToString(), out var xp))
                        continue;

                    if (!int.TryParse(rdr[4].ToString(), out var gunKills))
                        continue;

                    if (!bool.TryParse(rdr[5].ToString(), out var isBought))
                        continue;

                    if (!bool.TryParse(rdr[6].ToString(), out var isUnlocked))
                        continue;

                    var attachments = Utility.GetAttachmentsFromString(rdr[7].ToString(), gun, player);
                    if (!guns.ContainsKey(gunID))
                        guns.Add(gunID, new(gun, level, xp, gunKills, isBought, isUnlocked, attachments));
                    else
                        Logging.Debug($"Found a duplicate gun with id {gunID} registered for the player, ignoring it");
                }

                Logging.Debug($"Successfully got {guns.Count} guns registered to the player");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading gun data for {player.CharacterName}");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug($"Checking gun attachments for {player.CharacterName}");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.84f)), "PREPARING ATTACHMENTS..."));
            try
            {
                foreach (var gun in guns.Values)
                {
                    foreach (var rewardAttachment in gun.Gun.RewardAttachments)
                    {
                        if (!gun.Attachments.ContainsKey(rewardAttachment.Value.AttachmentID))
                        {
                            gun.Attachments.Add(rewardAttachment.Value.AttachmentID, new(rewardAttachment.Value, rewardAttachment.Key, false, false));
                            Logging.Debug($"Gun with name {gun.Gun.GunName} doesn't have a reward attachment with id {rewardAttachment.Value.AttachmentID} that comes with the gun, adding it for {player.CharacterName}");
                        }
                    }

                    _ = await new MySqlCommand($"UPDATE `{PLAYERS_GUNS}` SET `Attachments` = '{Utility.GetStringFromAttachments(gun.Attachments.Values.ToList())}' WHERE `SteamID` = {player.CSteamID} AND `GunID` = {gun.Gun.GunID};", conn).ExecuteScalarAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error while checking gun attachments");
                Logger.Log(ex);
            }

            Logging.Debug($"Getting gun skins for {player.CharacterName}");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.86f)), "PREPARING SKINS..."));
            var gunSkinsTxt = await new MySqlCommand($"SELECT `SkinIDs` FROM `{PLAYERS_GUNS_SKINS}` WHERE `SteamID` = {player.CSteamID};", conn).ExecuteScalarAsync();
            if (gunSkinsTxt is string gunSkinsText)
            {
                foreach (var id in gunSkinsText.GetIntListFromReaderResult())
                {
                    if (!GunSkinsSearchByID.TryGetValue(id, out var skin))
                    {
                        Logging.Debug($"Error finding gun skin with id {id}, ignoring it");
                        continue;
                    }

                    if (gunSkinsSearchByID.ContainsKey(id))
                    {
                        Logging.Debug($"Found a duplicate gun skin with id {id} registered for {player.CharacterName}, ignoring this");
                        continue;
                    }

                    gunSkinsSearchByID.Add(id, skin);
                    if (gunSkinsSearchByGunID.TryGetValue(skin.Gun.GunID, out var gunSkins))
                        gunSkins.Add(skin);
                    else
                        gunSkinsSearchByGunID.Add(skin.Gun.GunID, new() { skin });

                    gunSkinsSearchBySkinID.Add(skin.SkinID, skin);
                }

                Logging.Debug($"Successfully got {gunSkinsSearchByID.Count} gun skins for {player.CharacterName}");
            }

            Logging.Debug($"Getting gun charms for {player.CharacterName}");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.88f)), "PREPARING CHARMS..."));
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PLAYERS_GUNS_CHARMS}` WHERE `SteamID` = {player.CSteamID};", conn).ExecuteReaderAsync();
            try
            {
                while (await rdr.ReadAsync())
                {
                    if (!ushort.TryParse(rdr[1].ToString(), out var charmID))
                        continue;

                    if (!GunCharms.TryGetValue(charmID, out var gunCharm))
                    {
                        Logging.Debug($"Error finding gun charm with id {charmID} for {player.CharacterName}, ignoring it");
                        continue;
                    }

                    if (!bool.TryParse(rdr[2].ToString(), out var isBought))
                        continue;

                    if (!bool.TryParse(rdr[3].ToString(), out var isUnlocked))
                        continue;

                    if (!gunCharms.ContainsKey(charmID))
                        gunCharms.Add(charmID, new(gunCharm, isBought, isUnlocked));
                    else
                        Logging.Debug($"Found duplicate gun charm with id {charmID} for {player.CharacterName}, ignoring it");
                }

                Logging.Debug($"Successfully got {gunCharms.Count} for {player.CharacterName}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading gun charms data for {player.CharacterName}");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug($"Getting knives for {player.CharacterName}");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.9f)), "PREPARING KNIVES..."));
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PLAYERS_KNIVES}` WHERE `SteamID` = {player.CSteamID};", conn).ExecuteReaderAsync();
            try
            {
                while (await rdr.ReadAsync())
                {
                    if (!ushort.TryParse(rdr[1].ToString(), out var knifeID))
                        continue;

                    if (!Knives.TryGetValue(knifeID, out var knife))
                    {
                        Logging.Debug($"Error finding knife with id {knifeID}, ignoring it");
                        continue;
                    }

                    if (!int.TryParse(rdr[2].ToString(), out var knifeKills))
                        continue;

                    if (!bool.TryParse(rdr[3].ToString(), out var isBought))
                        continue;

                    if (!bool.TryParse(rdr[4].ToString(), out var isUnlocked))
                        continue;

                    if (!knives.ContainsKey(knifeID))
                        knives.Add(knifeID, new(knife, knifeKills, isBought, isUnlocked));
                    else
                        Logging.Debug($"Found a duplicate knife with id {knifeID} registered for {player.CharacterName}, ignoring it");
                }

                Logging.Debug($"Successfully got {knives.Count} knives for {player.CharacterName}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading knife data for {player.CharacterName}");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug($"Getting perks for {player.CharacterName}");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.91f)), "PREPARING PERKS..."));
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PLAYERS_PERKS}` WHERE `SteamID` = {player.CSteamID};", conn).ExecuteReaderAsync();
            try
            {
                while (await rdr.ReadAsync())
                {
                    if (!int.TryParse(rdr[1].ToString(), out var perkID))
                        continue;

                    if (!Perks.TryGetValue(perkID, out var perk))
                    {
                        Logging.Debug($"Error finding perk with id {perkID}, ignoring this");
                        continue;
                    }

                    if (!bool.TryParse(rdr[2].ToString(), out var isBought))
                        continue;

                    if (!bool.TryParse(rdr[3].ToString(), out var isUnlocked))
                        continue;

                    if (!perks.ContainsKey(perkID))
                        perks.Add(perkID, new(perk, isBought, isUnlocked));
                    else
                        Logging.Debug($"Found a duplicate perk with id {perkID} registered for {player.CharacterName}, ignoring it");
                }

                Logging.Debug($"Successfully got {perks.Count} perks for {player.CharacterName}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading perk data for {player.CharacterName}");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug($"Getting gadgets for {player.CharacterName}");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.93f)), "PREPARING GADGETS..."));
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PLAYERS_GADGETS}` WHERE `SteamID` = {player.CSteamID};", conn).ExecuteReaderAsync();
            try
            {
                while (await rdr.ReadAsync())
                {
                    if (!ushort.TryParse(rdr[1].ToString(), out var gadgetID))
                        continue;

                    if (!Gadgets.TryGetValue(gadgetID, out var gadget))
                    {
                        Logging.Debug($"Error finding gadget with id {gadgetID} for {player.CharacterName}, ignoring it");
                        continue;
                    }

                    if (!int.TryParse(rdr[2].ToString(), out var gadgetKills))
                        continue;

                    if (!bool.TryParse(rdr[3].ToString(), out var isBought))
                        continue;

                    if (!bool.TryParse(rdr[4].ToString(), out var isUnlocked))
                        continue;

                    if (!gadgets.ContainsKey(gadgetID))
                        gadgets.Add(gadgetID, new(gadget, gadgetKills, isBought, isUnlocked));
                    else
                        Logging.Debug($"Found duplicate gadget with id {gadgetID} registered for {player.CharacterName}, ignoring it");
                }

                Logging.Debug($"Successfully got {gadgets.Count} for {player.CharacterName}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading gadgets data for {player.CharacterName}");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug($"Getting killstreaks for {player.CharacterName}");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.94f)), "PREPARING KILLSTREAKS..."));
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PLAYERS_KILLSTREAKS}` WHERE `SteamID` = {player.CSteamID};", conn).ExecuteReaderAsync();
            try
            {
                while (await rdr.ReadAsync())
                {
                    if (!int.TryParse(rdr[1].ToString(), out var killstreakID))
                        continue;

                    if (!Killstreaks.TryGetValue(killstreakID, out var killstreak))
                    {
                        Logging.Debug($"Error finding killstreak with id {killstreakID} for {player.CharacterName}, ignoring it");
                        continue;
                    }

                    if (!int.TryParse(rdr[2].ToString(), out var killstreakKills))
                        continue;

                    if (!bool.TryParse(rdr[3].ToString(), out var isBought))
                        continue;

                    if (!bool.TryParse(rdr[4].ToString(), out var isUnlocked))
                        continue;

                    if (!killstreaks.ContainsKey(killstreakID))
                        killstreaks.Add(killstreakID, new(killstreak, killstreakKills, isBought, isUnlocked));
                    else
                        Logging.Debug($"Found a duplicate killstreak with id {killstreakID} for {player.CharacterName}, ignoring it");
                }

                Logging.Debug($"Successfully got {killstreaks.Count} for {player.CharacterName}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading killstreaks data for {player.CharacterName}");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug($"Getting cards for {player.CharacterName}");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.97f)), "PREPARING CARDS..."));
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PLAYERS_CARDS}` WHERE `SteamID` = {player.CSteamID};", conn).ExecuteReaderAsync();
            try
            {
                while (await rdr.ReadAsync())
                {
                    if (!int.TryParse(rdr[1].ToString(), out var cardID))
                        continue;

                    if (!Cards.TryGetValue(cardID, out var card))
                    {
                        Logging.Debug($"Error finding card with id {cardID} for {player.CharacterName}, ignoring it");
                        continue;
                    }

                    if (!bool.TryParse(rdr[2].ToString(), out var isBought))
                        continue;

                    if (!bool.TryParse(rdr[3].ToString(), out var isUnlocked))
                        continue;

                    if (!cards.ContainsKey(cardID))
                        cards.Add(cardID, new(card, isBought, isUnlocked));
                    else
                        Logging.Debug($"Found duplicate card with id {cardID} for {player.CharacterName}, ignoring it");
                }

                Logging.Debug($"Successfully got {cards.Count} for {player.CharacterName}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading cards data for {player.CharacterName}");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug($"Getting gloves for {player.CharacterName}");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.98f)), "PREPARING GLOVES..."));
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PLAYERS_GLOVES}` WHERE `SteamID` = {player.CSteamID};", conn).ExecuteReaderAsync();
            try
            {
                while (await rdr.ReadAsync())
                {
                    if (!int.TryParse(rdr[1].ToString(), out var gloveID))
                        continue;

                    if (!Gloves.TryGetValue(gloveID, out var glove))
                    {
                        Logging.Debug($"Error finding glove with id {gloveID} for {player.CharacterName}, ignoring it");
                        continue;
                    }

                    if (!bool.TryParse(rdr[2].ToString(), out var isBought))
                        continue;

                    if (!bool.TryParse(rdr[3].ToString(), out var isUnlocked))
                        continue;

                    if (!gloves.ContainsKey(gloveID))
                        gloves.Add(gloveID, new(glove, isBought, isUnlocked));
                    else
                        Logging.Debug($"Found duplicate glove with id {gloveID} for {player.CharacterName}, ignoring it");
                }

                Logging.Debug($"Successfully got {gloves.Count} gloves for {player.CharacterName}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading gloves data for {player.CharacterName}");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug($"Getting loadouts for {player.CharacterName}");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.99f)), "PREPARING LOADOUTS..."));
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PLAYERS_LOADOUTS}` WHERE `SteamID` = {player.CSteamID};", conn).ExecuteReaderAsync();
            List<int> updateLoadouts = new();
            try
            {
                while (await rdr.ReadAsync())
                {
                    if (!int.TryParse(rdr[1].ToString(), out var loadoutID))
                        continue;

                    if (!bool.TryParse(rdr[2].ToString(), out var isActive))
                        continue;

                    if (loadouts.ContainsKey(loadoutID))
                    {
                        Logging.Debug($"Found a duplicate loadout with id {loadoutID} for {player.CharacterName}, ignoring it");
                        continue;
                    }

                    var loadoutData = Plugin.Instance.Data.ConvertLoadoutFromJson(rdr[3].ToString());
                    if (!guns.TryGetValue(loadoutData.Primary, out var primary) && loadoutData.Primary != 0)
                    {
                        Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a primary with id {loadoutData.Primary} which is not owned by the player, removing primary");
                        if (!updateLoadouts.Contains(loadoutID))
                            updateLoadouts.Add(loadoutID);
                    }

                    if (!gunCharms.TryGetValue(loadoutData.PrimaryGunCharm, out var primaryGunCharm) && loadoutData.PrimaryGunCharm != 0)
                    {
                        Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a primary gun charm with id {loadoutData.PrimaryGunCharm} which is not owned by the player, not counting this loadout");
                        if (!updateLoadouts.Contains(loadoutID))
                            updateLoadouts.Add(loadoutID);
                    }

                    if (!gunSkinsSearchByID.TryGetValue(loadoutData.PrimarySkin, out var primarySkin) && loadoutData.PrimarySkin != 0)
                    {
                        Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a primary skin with id {loadoutData.PrimarySkin} which is not owned by the player, not counting this loadout");
                        if (!updateLoadouts.Contains(loadoutID))
                            updateLoadouts.Add(loadoutID);
                    }

                    Dictionary<EAttachment, LoadoutAttachment> primaryAttachments = new();
                    foreach (var primaryAttachment in loadoutData.PrimaryAttachments)
                    {
                        if (primary.Attachments.TryGetValue(primaryAttachment, out var attachment))
                            primaryAttachments.Add(attachment.Attachment.AttachmentType, attachment);
                        else
                            Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a primary attachment id with {primaryAttachment} which is not owned by the player, not counting it");
                    }

                    if (!guns.TryGetValue(loadoutData.Secondary, out var secondary) && loadoutData.Secondary != 0)
                    {
                        Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a secondary with id {loadoutData.Secondary} which is not owned by the player, not counting this loadout");
                        if (!updateLoadouts.Contains(loadoutID))
                            updateLoadouts.Add(loadoutID);
                    }

                    if (!gunCharms.TryGetValue(loadoutData.SecondaryGunCharm, out var secondaryGunCharm) && loadoutData.SecondaryGunCharm != 0)
                    {
                        Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a secondary gun charm with id {loadoutData.SecondaryGunCharm} which is not owned by the player, not counting this loadout");
                        if (!updateLoadouts.Contains(loadoutID))
                            updateLoadouts.Add(loadoutID);
                    }

                    if (!gunSkinsSearchByID.TryGetValue(loadoutData.SecondarySkin, out var secondarySkin) && loadoutData.SecondarySkin != 0)
                    {
                        Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a secondary skin with id {loadoutData.SecondarySkin} which is not owned by the player, not counting this loadout");
                        if (!updateLoadouts.Contains(loadoutID))
                            updateLoadouts.Add(loadoutID);
                    }

                    Dictionary<EAttachment, LoadoutAttachment> secondaryAttachments = new();
                    foreach (var secondaryAttachment in loadoutData.SecondaryAttachments)
                    {
                        if (secondary.Attachments.TryGetValue(secondaryAttachment, out var attachment))
                            secondaryAttachments.Add(attachment.Attachment.AttachmentType, attachment);
                        else
                            Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a secondary attachment id with {secondaryAttachment} which is not owned by the player, not counting it");
                    }

                    if (!knives.TryGetValue(loadoutData.Knife, out var knife) && loadoutData.Knife != 0)
                    {
                        Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a knife with id {loadoutData.Knife} which is not owned by the player, not counting this loadout");
                        if (!updateLoadouts.Contains(loadoutID))
                            updateLoadouts.Add(loadoutID);
                    }

                    if (!gadgets.TryGetValue(loadoutData.Tactical, out var tactical) && loadoutData.Tactical != 0)
                    {
                        Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a tactical with id {loadoutData.Tactical} which is not owned by the player, not counting this loadout");
                        if (!updateLoadouts.Contains(loadoutID))
                            updateLoadouts.Add(loadoutID);
                    }

                    if (!gadgets.TryGetValue(loadoutData.Lethal, out var lethal) && loadoutData.Lethal != 0)
                    {
                        Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a lethal with id {loadoutData.Lethal} which is not owned by the player, not counting this loadout");
                        if (!updateLoadouts.Contains(loadoutID))
                            updateLoadouts.Add(loadoutID);
                    }

                    List<LoadoutKillstreak> loadoutKillstreaks = new();
                    foreach (var killstreakID in loadoutData.Killstreaks)
                    {
                        if (killstreaks.TryGetValue(killstreakID, out var killstreak))
                            loadoutKillstreaks.Add(killstreak);
                        else
                        {
                            Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a killstreak with id {killstreakID} which is not owned by the player, not counting this loadout");
                            if (!updateLoadouts.Contains(loadoutID))
                                updateLoadouts.Add(loadoutID);
                        }
                    }

                    Dictionary<int, LoadoutPerk> loadoutPerks = new();
                    foreach (var perkID in loadoutData.Perks)
                    {
                        if (perks.TryGetValue(perkID, out var perk))
                            loadoutPerks.Add(perk.Perk.PerkType, perk);
                        else
                        {
                            Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a perk with id {perkID} which is not owned by the player, not counting this loadout");
                            if (!updateLoadouts.Contains(loadoutID))
                                updateLoadouts.Add(loadoutID);
                        }
                    }

                    Dictionary<string, LoadoutPerk> perksSearchByType = new(StringComparer.OrdinalIgnoreCase);
                    foreach (var perk in loadoutPerks.Values)
                    {
                        if (perksSearchByType.ContainsKey(perk.Perk.SkillType))
                            Logging.Debug($"There is perk with type {perk.Perk.SkillType} already in the loadout, ignoring");
                        else
                            perksSearchByType.Add(perk.Perk.SkillType, perk);
                    }

                    if (!gloves.TryGetValue(loadoutData.Glove, out var glove) && loadoutData.Glove != 0)
                    {
                        Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a glove with id {loadoutData.Glove} which is not owned by the player, not counting this loadout");
                        if (!updateLoadouts.Contains(loadoutID))
                            updateLoadouts.Add(loadoutID);
                    }

                    if (!cards.TryGetValue(loadoutData.Card, out var card) && loadoutData.Card != 0)
                    {
                        Logging.Debug($"Loadout with id {loadoutID} has a card with id {loadoutData.Card} which is not owned by the player, not counting this loadout");
                        if (!updateLoadouts.Contains(loadoutID))
                            updateLoadouts.Add(loadoutID);
                    }

                    loadouts.Add(loadoutID,
                        new(loadoutID, loadoutData.LoadoutName, isActive, primary, primarySkin, primaryGunCharm, primaryAttachments, secondary, secondarySkin, secondaryGunCharm, secondaryAttachments, knife, tactical, lethal, loadoutKillstreaks, loadoutPerks, perksSearchByType, glove, card));
                }

                Logging.Debug($"Successfully got {loadouts.Count} loadouts for {player.CharacterName}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading loadouts data for {player.CharacterName}");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug($"Fixing broken loadouts for {player.CharacterName}, found {updateLoadouts.Count} broken loadouts");
            foreach (var updateLoadout in updateLoadouts)
            {
                if (!loadouts.TryGetValue(updateLoadout, out var playerLoadout))
                {
                    Logging.Debug($"Error finding loadout with id {updateLoadout} for player with steam id {player.CSteamID}");
                    continue;
                }

                LoadoutData loadoutData = new(playerLoadout);
                _ = await new MySqlCommand($"UPDATE `{PLAYERS_LOADOUTS}` SET `Loadout` = '{Plugin.Instance.Data.ConvertLoadoutToJson(loadoutData)}' WHERE `SteamID` = {player.CSteamID} AND `LoadoutID` = {updateLoadout};", conn).ExecuteScalarAsync();
            }

            Logging.Debug($"Getting boosters for {player.CharacterName}");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', (int)(LOADING_SPACES * 0.99f)), "PREPARING BOOSTERS..."));
            _ = await new MySqlCommand($"DELETE FROM `{PLAYERS_BOOSTERS}` WHERE `SteamID` = {player.CSteamID} AND `BoosterExpiration` < {DateTimeOffset.UtcNow.ToUnixTimeSeconds()};", conn).ExecuteScalarAsync();
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PLAYERS_BOOSTERS}` WHERE `SteamID` = {player.CSteamID};", conn).ExecuteReaderAsync();
            try
            {
                List<PlayerBooster> boosters = new();
                while (await rdr.ReadAsync())
                {
                    if (!Enum.TryParse(rdr[1].ToString(), true, out EBoosterType boosterType))
                        return;

                    if (!float.TryParse(rdr[2].ToString(), out var boosterValue))
                        return;

                    if (!long.TryParse(rdr[3].ToString(), out var boosterExpirationUnix))
                        return;

                    var boosterExpiration = DateTimeOffset.FromUnixTimeSeconds(boosterExpirationUnix);
                    PlayerBooster booster = new(player.CSteamID, boosterType, boosterValue, boosterExpiration);

                    boosters.Add(booster);
                }

                playerData.ActiveBoosters = boosters;

                Logging.Debug($"Successfully got {boosters.Count} active boosters registered for {player.CharacterName}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading player boosters for {player.CharacterName}");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug($"Getting cases for {player.CharacterName}");
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new(' ', (int)(LOADING_SPACES * 0.99f)), "PREPARING CASES..."));
            rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PLAYERS_CASES}` WHERE `SteamID` = {player.CSteamID} ORDER BY `CaseID`;", conn).ExecuteReaderAsync();
            try
            {
                List<PlayerCase> playerCases = new();
                Dictionary<int, PlayerCase> playerCasesSearchByID = new();
                while (await rdr.ReadAsync())
                {
                    if (!int.TryParse(rdr[1].ToString(), out var caseID))
                        continue;

                    if (!Cases.TryGetValue(caseID, out var @case))
                    {
                        Logging.Debug($"Error finding case with id {caseID} for {player.CharacterName}, ignoring it");
                        continue;
                    }

                    if (!int.TryParse(rdr[2].ToString(), out var amount))
                        continue;

                    PlayerCase playerCase = new(player.CSteamID, @case, amount);
                    if (playerCasesSearchByID.ContainsKey(caseID))
                    {
                        Logging.Debug($"Case with id {caseID} already registered for player, ignoring");
                        continue;
                    }

                    playerCases.Add(playerCase);
                    playerCasesSearchByID.Add(caseID, playerCase);
                }

                Logging.Debug($"Successfully got {playerCases.Count} cases registered for {player.CharacterName}");

                playerData.Cases = playerCases;
                playerData.CasesSearchByID = playerCasesSearchByID;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading player cases for {player.CharacterName}");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            playerData.SetPersonalBooster(EBoosterType.XP, playerData.XPBooster);
            playerData.SetPersonalBooster(EBoosterType.BPXP, playerData.BPBooster);
            playerData.SetPersonalBooster(EBoosterType.GUNXP, playerData.GunXPBooster);

            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.UpdateLoadingBar(player, new('　', LOADING_SPACES), "FINALISING..."));
            Logging.Debug($"Checking if player has more loadouts for {player.CharacterName}");
            try
            {
                var loadoutAmount = playerData.HasPrime && playerData.PrimeExpiry > DateTimeOffset.UtcNow ? Plugin.Instance.Config.Base.FileData.PrimeLoadoutAmount : Plugin.Instance.Config.Base.FileData.DefaultLoadoutAmount;
                Logging.Debug($"{player.CharacterName} should have {loadoutAmount} loadouts, he has {loadouts.Count} registered");
                if (loadoutAmount < loadouts.Count)
                {
                    Logging.Debug($"{player.CharacterName} has more loadouts than he should have, deleting the last ones");
                    for (var i = loadouts.Count; i > loadoutAmount; i--)
                    {
                        Logging.Debug($"Removing loadout with id {i} for {player.CharacterName}");

                        if (!loadouts.TryGetValue(i, out var loadout))
                            continue;

                        if (loadout.IsActive)
                        {
                            loadouts[1].IsActive = true;
                            _ = await new MySqlCommand($"UPDATE `{PLAYERS_LOADOUTS}` SET `IsActive` = true WHERE `SteamID` = {player.CSteamID} AND `LoadoutID` = 1;", conn).ExecuteScalarAsync();
                        }

                        _ = await new MySqlCommand($"DELETE FROM `{PLAYERS_LOADOUTS}` WHERE `SteamID` = {player.CSteamID} AND `LoadoutID` = {i}", conn).ExecuteScalarAsync();
                        _ = loadouts.Remove(i);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error checking the loadout amounts for player");
                Logger.Log(ex);
            }

            if (PlayerLoadouts.ContainsKey(player.CSteamID))
                _ = PlayerLoadouts.Remove(player.CSteamID);

            PlayerLoadouts.Add(player.CSteamID, new(guns, gunCharms, knives, gunSkinsSearchByID, gunSkinsSearchByGunID, gunSkinsSearchBySkinID, perks, gadgets, killstreaks, cards, gloves, loadouts));
        }
        catch (Exception ex)
        {
            Logger.Log($"Error getting player data with Steam ID {player.CSteamID}");
            Logger.Log(ex);
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    // Pending Query
    private void AddQuery(string query)
    {
        if (string.IsNullOrEmpty(query))
            return;

        lock (PendingQueries)
            PendingQueries.Add(query);
    }

    private void AddQueries(List<string> queries)
    {
        if (queries == null || queries.Count == 0)
            return;

        lock (PendingQueries)
            PendingQueries.AddRange(queries);
    }

    // Timers
    private void CleanQueries(object sender, ElapsedEventArgs e)
    {
        lock (PendingQueries)
        {
            if (PendingQueries.Count == 0)
                return;
        }

        var stopWatch = new Stopwatch();
        stopWatch.Start();
        using MySqlConnection conn = new(ConnectionString);
        try
        {
            conn.Open();
            stopWatch.Stop();
            Logging.Debug($"Cleaning queries");
            Logging.Debug($"Connection established, took {stopWatch.ElapsedMilliseconds}ms");
            stopWatch.Reset();
            List<string> pendingQueries;
            lock (PendingQueries)
            {
                pendingQueries = PendingQueries.ToList();
                PendingQueries.Clear();
            }

            Logging.Debug($"Found {pendingQueries.Count} queries to go through");
            stopWatch.Start();
            var cmdText = string.Join("", pendingQueries);
            try
            {
                new MySqlCommand(cmdText, conn).ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error executing query: \n{cmdText}");
                Logger.Log(ex);
            }

            stopWatch.Stop();
            Logging.Debug($"Pending queries: {pendingQueries.Count}, Time Elapsed = {stopWatch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            Logger.Log("Error cleaning the queries");
            Logger.Log(ex);
            ConnectionThreshold++;
            if (ConnectionThreshold >= 2)
            {
                Logger.Log("Connection threshold reached, shutting down the server");
                Plugin.Instance.Logger.Dump(PendingQueries);
                Plugin.Instance.Logger.Warn("Connection threshold reached, shutting down the server");
                ForcedShutdown = true;
                Provider.shutdown();
                return;
            }

            Plugin.Instance.Logger.Warn("Failed to run the batch queries, trying once more after 10 seconds");
        }
        finally
        {
            conn.Close();
        }
    }

    private void RefreshData(object sender, ElapsedEventArgs e)
    {
        using MySqlConnection conn = new(ConnectionString);

        try
        {
            conn.Open();
            Logging.Debug($"Refreshing data");
            Logging.Debug("Getting server options");
            var rdr = new MySqlCommand($"SELECT * FROM `{OPTIONS}`;", conn).ExecuteReader();
            try
            {
                while (rdr.Read())
                {
                    if (!long.TryParse(rdr[0].ToString(), out var dailyLeaderboardWipeUnix))
                        continue;

                    var dailyLeaderboardWipe = DateTimeOffset.FromUnixTimeSeconds(dailyLeaderboardWipeUnix);

                    if (!long.TryParse(rdr[1].ToString(), out var weeklyLeaderboardWipeUnix))
                        continue;

                    var weeklyLeaderboardWipe = DateTimeOffset.FromUnixTimeSeconds(weeklyLeaderboardWipeUnix);

                    var dailyRanked = Utility.GetRankedRewardsFromString(rdr[2].ToString());
                    var dailyPercentile = Utility.GetPercentileRewardsFromString(rdr[3].ToString());

                    var weeklyRanked = Utility.GetRankedRewardsFromString(rdr[4].ToString());
                    var weeklyPercentile = Utility.GetPercentileRewardsFromString(rdr[5].ToString());

                    var seasonalRanked = Utility.GetRankedRewardsFromString(rdr[6].ToString());
                    var seasonalPercentile = Utility.GetPercentileRewardsFromString(rdr[7].ToString());

                    if (!float.TryParse(rdr[8].ToString(), out var xpBooster))
                        continue;

                    if (!float.TryParse(rdr[9].ToString(), out var bpBooster))
                        continue;

                    if (!float.TryParse(rdr[10].ToString(), out var gunXPBooster))
                        continue;

                    if (!long.TryParse(rdr[11].ToString(), out var xpBoosterWipeUnix))
                        continue;

                    var xpBoosterWipe = DateTimeOffset.FromUnixTimeSeconds(xpBoosterWipeUnix);
                    if (!long.TryParse(rdr[12].ToString(), out var bpBoosterWipeUnix))
                        continue;

                    var bpBoosterWipe = DateTimeOffset.FromUnixTimeSeconds(bpBoosterWipeUnix);
                    if (!long.TryParse(rdr[13].ToString(), out var gunXPBoosterWipeUnix))
                        continue;

                    var gunXPBoosterWipe = DateTimeOffset.FromUnixTimeSeconds(gunXPBoosterWipeUnix);

                    var gameTips = rdr[14].ToString().Split(',').ToList();
                    var primeRewards = Utility.GetRewardsFromString(rdr[15].ToString());
                    var primeDailyRewards = Utility.GetRewardsFromString(rdr[16].ToString());

                    ServerOptions = new(dailyLeaderboardWipe, weeklyLeaderboardWipe, dailyRanked, dailyPercentile, weeklyRanked, weeklyPercentile, seasonalRanked, seasonalPercentile, xpBooster, bpBooster, gunXPBooster, xpBoosterWipe, bpBoosterWipe, gunXPBoosterWipe, gameTips, primeRewards,
                        primeDailyRewards);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error getting data from options table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }
            
            rdr = new MySqlCommand($"SELECT `{PLAYERS}`.`SteamID`, `{PLAYERS}`.`SteamName`, `{PLAYERS}`.`CountryCode`, `{PLAYERS}`.`HideFlag`, `{PLAYERS}`.`Level`, `{PLAYERS}`.`HasPrime`, `{PLAYERS}`.`Kills`, `{PLAYERS}`.`HeadshotKills`, `{PLAYERS}`.`Deaths`, ((SELECT CHAR_LENGTH(`{PLAYERS_GUNS_SKINS}`.`SkinIDs`) - CHAR_LENGTH(REPLACE(`{PLAYERS_GUNS_SKINS}`.`SkinIDs`, ',', ''))) + (SELECT COUNT(*) FROM `{PLAYERS_KNIVES}` WHERE `{PLAYERS_KNIVES}`.`SteamID` = `{PLAYERS}`.`SteamID` AND `{PLAYERS_KNIVES}`.`IsBought` = 1 AND `{PLAYERS_KNIVES}`.`KnifeID` != 12767) + (SELECT COUNT(*) FROM `{PLAYERS_GLOVES}`  WHERE `{PLAYERS_GLOVES}`.`SteamID` = `{PLAYERS}`.`SteamID` AND `{PLAYERS_GLOVES}`.`IsBought` = 1)) AS `TotalSkins` FROM `{PLAYERS}` INNER JOIN `{PLAYERS_GUNS_SKINS}` ON `{PLAYERS}`.`SteamID` = `{PLAYERS_GUNS_SKINS}`.`SteamID` ORDER BY (`Kills` + `HeadshotKills`) DESC;", conn).ExecuteReader();
            Logging.Debug("Getting all time leaderboard data");
            try
            {
                Dictionary<CSteamID, LeaderboardData> playerAllTimeLeaderboardLookup = new();
                List<LeaderboardData> playerAllTimeKill = new();
                List<LeaderboardData> playerAllTimeLevel = new();
                List<LeaderboardData> playerAllTimeSkins = new();
                
                while (rdr.Read())
                {
                    if (!ulong.TryParse(rdr[0].ToString(), out var steamid))
                        continue;

                    CSteamID steamID = new(steamid);
                    var steamName = rdr[1].ToString();
                    var countryCode = rdr[2].ToString();

                    if (!bool.TryParse(rdr[3].ToString(), out var hideFlag))
                        continue;

                    if (!int.TryParse(rdr[4].ToString(), out var level))
                        continue;

                    if (!bool.TryParse(rdr[5].ToString(), out var hasPrime))
                        continue;

                    if (!int.TryParse(rdr[6].ToString(), out var kills))
                        continue;

                    if (!int.TryParse(rdr[7].ToString(), out var headshotKills))
                        continue;

                    if (!int.TryParse(rdr[8].ToString(), out var deaths))
                        continue;
                    
                    if (!int.TryParse(rdr[9].ToString(), out var totalSkins))
                        return;
                    
                    LeaderboardData leaderboardData = new(steamID, steamName, countryCode, hideFlag, level, hasPrime, kills, headshotKills, deaths, totalSkins);
                    playerAllTimeLeaderboardLookup.Add(steamID, leaderboardData);
                    playerAllTimeKill.Add(leaderboardData);
                    playerAllTimeLevel.Add(leaderboardData);
                    playerAllTimeSkins.Add(leaderboardData);
                }

                PlayerAllTimeLeaderboardLookup = playerAllTimeLeaderboardLookup;
                PlayerAllTimeKill = playerAllTimeKill;
                PlayerAllTimeLevel = playerAllTimeLevel;
                PlayerAllTimeSkins = playerAllTimeSkins;
                PlayerAllTimeLevel.Sort((x, y) => y.Level.CompareTo(x.Level));
                PlayerAllTimeSkins.Sort((x, y) => y.Skins.CompareTo(x.Skins));
            }
            catch (Exception ex)
            {
                Logger.Log("Error reading data from players table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }
            
            rdr = new MySqlCommand($"SELECT `SteamID`, `Kills`, `HeadshotKills`, `Deaths` FROM `{PLAYERS_LEADERBOARD_DAILY}` ORDER BY (`Kills` + `HeadshotKills`) DESC;", conn).ExecuteReader();
            Logging.Debug("Getting daily leaderboard data");
            try
            {
                List<LeaderboardData> playerDailyLeaderboard = new();
                Dictionary<CSteamID, LeaderboardData> playerDailyLeaderboardLookup = new();

                while (rdr.Read())
                {
                    if (!ulong.TryParse(rdr[0].ToString(), out var steamid))
                        continue;
                    
                    CSteamID steamID = new(steamid);
                    if (!int.TryParse(rdr[1].ToString(), out var kills))
                        continue;

                    if (!int.TryParse(rdr[2].ToString(), out var headshotKills))
                        continue;

                    if (!int.TryParse(rdr[3].ToString(), out var deaths))
                        continue;

                    if (!PlayerAllTimeLeaderboardLookup.TryGetValue(steamID, out var allTimeData))
                    {
                        Logging.Debug($"Player with steam id {steamID} has no all time data but has daily leaderboard data??");
                        continue;
                    }
                    
                    LeaderboardData leaderboardData = new(steamID, kills, headshotKills, deaths, allTimeData);

                    playerDailyLeaderboard.Add(leaderboardData);
                    playerDailyLeaderboardLookup.Add(steamID, leaderboardData);
                }

                PlayerDailyLeaderboard = playerDailyLeaderboard;
                PlayerDailyLeaderboardLookup = playerDailyLeaderboardLookup;
            }
            catch (Exception ex)
            {
                Logger.Log("Error reading data from daily leaderboard table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            rdr = new MySqlCommand($"SELECT `SteamID`, `Kills`, `HeadshotKills`, `Deaths` FROM `{PLAYERS_LEADERBOARD_WEEKLY}` ORDER BY (`Kills` + `HeadshotKills`) DESC;", conn).ExecuteReader();
            Logging.Debug("Getting weekly leaderboard data");
            try
            {
                List<LeaderboardData> playerWeeklyLeaderboard = new();
                Dictionary<CSteamID, LeaderboardData> playerWeeklyLeaderboardLookup = new();

                while (rdr.Read())
                {
                    if (!ulong.TryParse(rdr[0].ToString(), out var steamid))
                        continue;
                    
                    CSteamID steamID = new(steamid);
                    if (!int.TryParse(rdr[1].ToString(), out var kills))
                        continue;

                    if (!int.TryParse(rdr[2].ToString(), out var headshotKills))
                        continue;

                    if (!int.TryParse(rdr[3].ToString(), out var deaths))
                        continue;

                    if (!PlayerAllTimeLeaderboardLookup.TryGetValue(steamID, out var allTimeData))
                    {
                        Logging.Debug($"Player with steam id {steamID} has no all time data but has weekly leaderboard data??");
                        continue;
                    }
                    
                    LeaderboardData leaderboardData = new(steamID, kills, headshotKills, deaths, allTimeData);

                    playerWeeklyLeaderboard.Add(leaderboardData);
                    playerWeeklyLeaderboardLookup.Add(steamID, leaderboardData);
                }

                PlayerWeeklyLeaderboard = playerWeeklyLeaderboard;
                PlayerWeeklyLeaderboardLookup = playerWeeklyLeaderboardLookup;
            }
            catch (Exception ex)
            {
                Logger.Log("Error reading data from weekly leaderboard table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug("Adding any players that are not added in daily or weekly leaderboards");
            foreach (var data in PlayerData.Values)
            {
                if (!PlayerAllTimeLeaderboardLookup.TryGetValue(data.SteamID, out var allTimeData))
                    continue;
                
                if (!PlayerDailyLeaderboardLookup.ContainsKey(data.SteamID))
                {
                    LeaderboardData dailyLeaderboardData = new(data.SteamID, 0, 0, 0, allTimeData);
                    PlayerDailyLeaderboard.Add(dailyLeaderboardData);
                    PlayerDailyLeaderboardLookup.Add(data.SteamID, dailyLeaderboardData);
                    _ = new MySqlCommand($"INSERT INTO `{PLAYERS_LEADERBOARD_DAILY}` ( `SteamID` ) VALUES ( {data.SteamID} );", conn).ExecuteScalar();
                }

                if (!PlayerWeeklyLeaderboardLookup.ContainsKey(data.SteamID))
                {
                    LeaderboardData weeklyLeaderboardData = new(data.SteamID, 0, 0, 0, allTimeData);
                    PlayerWeeklyLeaderboard.Add(weeklyLeaderboardData);
                    PlayerWeeklyLeaderboardLookup.Add(data.SteamID, weeklyLeaderboardData);
                    _ = new MySqlCommand($"INSERT INTO `{PLAYERS_LEADERBOARD_WEEKLY}` ( `SteamID` ) VALUES ( {data.SteamID} );", conn).ExecuteScalar();
                }
            }

            rdr = new MySqlCommand($"SELECT `SteamID`, `Kills`, `HeadshotKills`, `Deaths` FROM `{PLAYERS_LEADERBOARD_SEASONAL}` ORDER BY (`Kills` + `HeadshotKills`) DESC;", conn).ExecuteReader();
            Logging.Debug("Getting seasonal leaderboard data");
            try
            {
                List<LeaderboardData> playerSeasonalLeaderboard = new();
                Dictionary<CSteamID, LeaderboardData> playerSeasonalLeaderboardLookup = new();

                while (rdr.Read())
                {
                    if (!ulong.TryParse(rdr[0].ToString(), out var steamid))
                        continue;
                    
                    CSteamID steamID = new(steamid);
                    if (!int.TryParse(rdr[1].ToString(), out var kills))
                        continue;

                    if (!int.TryParse(rdr[2].ToString(), out var headshotKills))
                        continue;

                    if (!int.TryParse(rdr[3].ToString(), out var deaths))
                        continue;

                    if (!PlayerAllTimeLeaderboardLookup.TryGetValue(steamID, out var allTimeData))
                    {
                        Logging.Debug($"Player with steam id {steamID} has no all time data but has seasonal leaderboard data??");
                        continue;
                    }
                    
                    LeaderboardData leaderboardData = new(steamID, kills, headshotKills, deaths, allTimeData);

                    playerSeasonalLeaderboard.Add(leaderboardData);
                    playerSeasonalLeaderboardLookup.Add(steamID, leaderboardData);
                }

                PlayerSeasonalLeaderboard = playerSeasonalLeaderboard;
                PlayerSeasonalLeaderboardLookup = playerSeasonalLeaderboardLookup;
            }
            catch (Exception ex)
            {
                Logger.Log("Error reading data from weekly leaderboard table");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug(
                $"Loaded {PlayerDailyLeaderboardLookup.Count} daily leaderboard entries, {PlayerWeeklyLeaderboardLookup.Count} weekly leaderboard entries, {PlayerSeasonalLeaderboardLookup.Count} seasonal leaderboard entries, {PlayerAllTimeLeaderboardLookup.Count} all time leaderboard entries");

            List<(CSteamID, List<Reward>)> bulkRewards = new();

            Logging.Debug("Checking if daily leaderboard needs to be wiped");
            if (ServerOptions.DailyLeaderboardWipe < DateTimeOffset.UtcNow)
            {
                var botRewards = new List<BotReward>();
                
                // Give all ranked rewards
                Embed embed = new(null, $"Last Playtest Rankings ({PlayerDailyLeaderboard.Count} Players)", "", "15105570", DateTime.UtcNow.ToString("s"), new(Provider.serverName, Provider.configData.Browser.Icon), new(Provider.serverName, "", Provider.configData.Browser.Icon),
                    new Field[] { new($"Ranked:", "", false), new("Percentile:", "", false) }, null, null);

                foreach (var rankedReward in ServerOptions.DailyRankedRewards)
                {
                    if (PlayerDailyLeaderboard.Count < rankedReward.Key + 1)
                        break;

                    var leaderboardData = PlayerDailyLeaderboard[rankedReward.Key];
                    bulkRewards.Add(new(leaderboardData.SteamID, rankedReward.Value));
                    embed.fields[0].value += $"{Utility.GetDiscordEmoji(rankedReward.Key + 1)} [{leaderboardData.SteamName}](https://steamcommunity.com/profiles/{leaderboardData.SteamID}/) | {leaderboardData.Kills + leaderboardData.HeadshotKills} Kills \n";
                    if (rankedReward.Key == 2)
                        embed.fields[0].value += $"\n";
                    
                    botRewards.Add(new(leaderboardData.SteamID.ToString(), rankedReward.Key + 1, 0));
                }

                // Give all percentile rewards
                foreach (var percentileReward in ServerOptions.DailyPercentileRewards)
                {
                    var lowerIndex = percentileReward.LowerPercentile == 0 ? 0 : percentileReward.LowerPercentile * PlayerDailyLeaderboard.Count / 100;
                    var upperIndex = percentileReward.UpperPercentile * PlayerDailyLeaderboard.Count / 100;

                    for (var i = lowerIndex; i < upperIndex; i++)
                    {
                        if (PlayerDailyLeaderboard.Count < i + 1)
                            break;

                        var leaderboardData = PlayerDailyLeaderboard[i];
                        bulkRewards.Add(new(leaderboardData.SteamID, percentileReward.Rewards));
                        var botReward = botRewards.FirstOrDefault(k => k.steam_id == leaderboardData.SteamID.ToString());
                        if (botReward != null)
                            botReward.percentile = percentileReward.UpperPercentile;
                        else
                            botRewards.Add(new(leaderboardData.SteamID.ToString(), PlayerDailyLeaderboard.IndexOf(leaderboardData) + 1, percentileReward.UpperPercentile));
                    }

                    embed.fields[1].value += $"**Top {percentileReward.UpperPercentile}%:** {upperIndex - lowerIndex} players \n";
                }

                try
                {
                    var leaderboardReward = new BotLeaderboardReward("daily", PlayerDailyLeaderboard.Count, botRewards.ToArray());
                    var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(leaderboardReward));
                    using WebClient webClient = new();
                    var headers = webClient.Headers;
                    headers.Set(HttpRequestHeader.ContentType, "application/json");
                    _ = webClient.UploadData($"http://213.32.6.3:27116/leaderboard", bytes);
                }
                catch (Exception ex)
                {
                    Logger.Log("Error sending bot leaderboard reward info");
                    Logger.Log(ex);
                }
                
                try
                {
                    Plugin.Instance.Discord.SendEmbed(embed, "Leaderboard", Plugin.Instance.Config.Webhooks.FileData.LeaderboardWebhookLink);
                }
                catch (Exception ex)
                {
                    Logger.Log("Error sending embed");
                    Logger.Log(ex);
                }

                // Wipe the Daily leaderboard data
                _ = new MySqlCommand($"DELETE FROM `{PLAYERS_LEADERBOARD_DAILY}`;", conn).ExecuteScalar();

                PlayerDailyLeaderboard.Clear();
                PlayerDailyLeaderboardLookup.Clear();

                foreach (var data in PlayerData.Values)
                {
                    if (!PlayerAllTimeLeaderboardLookup.TryGetValue(data.SteamID, out var allTimeData))
                        continue;
                    
                    LeaderboardData leaderboardData = new(data.SteamID,0, 0, 0, allTimeData);
                    PlayerDailyLeaderboard.Add(leaderboardData);
                    PlayerDailyLeaderboardLookup.Add(data.SteamID, leaderboardData);
                    _ = new MySqlCommand($"INSERT INTO `{PLAYERS_LEADERBOARD_DAILY}` ( `SteamID` ) VALUES ( {data.SteamID} );", conn).ExecuteScalar();
                }

                // Change the wipe date
                var hourTarget = ServerOptions.DailyLeaderboardWipe.Hour;
                var now = DateTime.UtcNow;
                DateTimeOffset newWipeDate = new(now.Year, now.Month, now.Day, hourTarget, 0, 0, new(0));
                if (now.Hour >= hourTarget)
                    newWipeDate = newWipeDate.AddDays(1);

                _ = new MySqlCommand($"UPDATE `{OPTIONS}` SET `DailyLeaderboardWipe` = {newWipeDate.ToUnixTimeSeconds()};", conn).ExecuteScalar();
                ServerOptions.DailyLeaderboardWipe = newWipeDate;
            }

            Logging.Debug("Checking if weekly leaderboard needs to be wiped");
            if (ServerOptions.WeeklyLeaderboardWipe < DateTimeOffset.UtcNow)
            {
                var botRewards = new List<BotReward>();
                
                // Give all ranked rewards
                Embed embed = new("", $"Last Playtest Rankings ({PlayerWeeklyLeaderboard.Count} Players)", "", "15105570", DateTime.UtcNow.ToString("s"), new(Provider.serverName, Provider.configData.Browser.Icon), new(Provider.serverName, "", Provider.configData.Browser.Icon),
                    new Field[] { new($"Ranked:", "", false), new("Percentile:", "", false) }, null, null);

                foreach (var rankedReward in ServerOptions.WeeklyRankedRewards)
                {
                    if (PlayerWeeklyLeaderboard.Count < rankedReward.Key + 1)
                        break;

                    var leaderboardData = PlayerWeeklyLeaderboard[rankedReward.Key];
                    bulkRewards.Add(new(leaderboardData.SteamID, rankedReward.Value));
                    embed.fields[0].value += $"{Utility.GetDiscordEmoji(rankedReward.Key + 1)} [{leaderboardData.SteamName}](https://steamcommunity.com/profiles/{leaderboardData.SteamID}/) | {leaderboardData.Kills + leaderboardData.HeadshotKills} Kills \n";
                    if (rankedReward.Key == 2)
                        embed.fields[0].value += $"\n";
                    
                    botRewards.Add(new(leaderboardData.SteamID.ToString(), rankedReward.Key + 1, 0));
                }

                // Give all percentile rewards
                foreach (var percentileReward in ServerOptions.WeeklyPercentileRewards)
                {
                    var lowerIndex = percentileReward.LowerPercentile == 0 ? 0 : percentileReward.LowerPercentile * PlayerWeeklyLeaderboard.Count / 100;
                    var upperIndex = percentileReward.UpperPercentile * PlayerWeeklyLeaderboard.Count / 100;

                    for (var i = lowerIndex; i < upperIndex; i++)
                    {
                        if (PlayerWeeklyLeaderboard.Count < i + 1)
                            break;

                        var leaderboardData = PlayerWeeklyLeaderboard[i];
                        bulkRewards.Add(new(leaderboardData.SteamID, percentileReward.Rewards));
                    
                        var botReward = botRewards.FirstOrDefault(k => k.steam_id == leaderboardData.SteamID.ToString());
                        if (botReward != null)
                            botReward.percentile = percentileReward.UpperPercentile;
                        else
                            botRewards.Add(new(leaderboardData.SteamID.ToString(), PlayerWeeklyLeaderboard.IndexOf(leaderboardData) + 1, percentileReward.UpperPercentile));
                    }

                    embed.fields[1].value += $"**Top {percentileReward.UpperPercentile}%:** {upperIndex - lowerIndex} players \n";
                }

                try
                {
                    var leaderboardReward = new BotLeaderboardReward("weekly", PlayerWeeklyLeaderboard.Count, botRewards.ToArray());
                    var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(leaderboardReward));
                    using WebClient webClient = new();
                    var headers = webClient.Headers;
                    headers.Set(HttpRequestHeader.ContentType, "application/json");
                    _ = webClient.UploadData($"http://213.32.6.3:27116/leaderboard", bytes);
                }
                catch (Exception ex)
                {
                    Logger.Log("Error sending bot leaderboard reward info");
                    Logger.Log(ex);
                }
                
                try
                {
                    Plugin.Instance.Discord.SendEmbed(embed, "Leaderboard", Plugin.Instance.Config.Webhooks.FileData.LeaderboardWebhookLink);
                }
                catch (Exception ex)
                {
                    Logger.Log("Error sending embed");
                    Logger.Log(ex);
                }

                // Wipe the Weekly leaderboard data
                _ = new MySqlCommand($"DELETE FROM `{PLAYERS_LEADERBOARD_WEEKLY}`;", conn).ExecuteScalar();

                PlayerWeeklyLeaderboard.Clear();
                PlayerWeeklyLeaderboardLookup.Clear();

                foreach (var data in PlayerData.Values)
                {
                    if (!PlayerAllTimeLeaderboardLookup.TryGetValue(data.SteamID, out var allTimeData))
                        continue;
                    
                    LeaderboardData leaderboardData = new(data.SteamID, 0, 0, 0, allTimeData);
                    PlayerWeeklyLeaderboard.Add(leaderboardData);
                    PlayerWeeklyLeaderboardLookup.Add(data.SteamID, leaderboardData);
                    _ = new MySqlCommand($"INSERT INTO `{PLAYERS_LEADERBOARD_WEEKLY}` ( `SteamID` ) VALUES ( {data.SteamID} );", conn).ExecuteScalar();
                }

                // Change the wipe date
                var newWipeDate = DateTimeOffset.UtcNow.AddDays(7);
                newWipeDate = new(newWipeDate.Year, newWipeDate.Month, newWipeDate.Day, ServerOptions.WeeklyLeaderboardWipe.Hour, 0, 0, new(0));
                _ = new MySqlCommand($"UPDATE `{OPTIONS}` SET `WeeklyLeaderboardWipe` = {newWipeDate.ToUnixTimeSeconds()};", conn).ExecuteScalar();
                ServerOptions.WeeklyLeaderboardWipe = newWipeDate;
            }

            Logging.Debug("Checking if seasonal leaderboard needs to be wiped");
            if (IsPendingSeasonalWipe)
            {
                IsPendingSeasonalWipe = false;

                var botRewards = new List<BotReward>();
                
                // Give all ranked rewards
                Embed embed = new(null, $"Last Playtest Rankings ({PlayerSeasonalLeaderboard.Count} Players)", null, "15105570", DateTime.UtcNow.ToString("s"), new(Provider.serverName, Provider.configData.Browser.Icon), new(Provider.serverName, "", Provider.configData.Browser.Icon),
                    new Field[] { new($"Ranked:", "", false), new("Percentile:", "", false) }, null, null);

                foreach (var rankedReward in ServerOptions.SeasonalRankedRewards)
                {
                    if (PlayerSeasonalLeaderboard.Count < rankedReward.Key + 1)
                        break;

                    var leaderboardData = PlayerSeasonalLeaderboard[rankedReward.Key];
                    bulkRewards.Add(new(leaderboardData.SteamID, rankedReward.Value));
                    embed.fields[0].value += $"{Utility.GetDiscordEmoji(rankedReward.Key + 1)} [{leaderboardData.SteamName}](https://steamcommunity.com/profiles/{leaderboardData.SteamID}/) | {leaderboardData.Kills + leaderboardData.HeadshotKills} Kills \n";
                    if (rankedReward.Key == 2)
                        embed.fields[0].value += $"\n";
                    
                    botRewards.Add(new(leaderboardData.SteamID.ToString(), rankedReward.Key + 1, 0));
                }

                // Give all percentile rewards
                foreach (var percentileReward in ServerOptions.SeasonalPercentileRewards)
                {
                    var lowerIndex = percentileReward.LowerPercentile == 0 ? 0 : percentileReward.LowerPercentile * PlayerSeasonalLeaderboard.Count / 100;
                    var upperIndex = percentileReward.UpperPercentile * PlayerSeasonalLeaderboard.Count / 100;

                    for (var i = lowerIndex; i < upperIndex; i++)
                    {
                        if (PlayerSeasonalLeaderboard.Count < i + 1)
                            break;

                        var leaderboardData = PlayerSeasonalLeaderboard[i];
                        bulkRewards.Add(new(leaderboardData.SteamID, percentileReward.Rewards));
                        
                        var botReward = botRewards.FirstOrDefault(k => k.steam_id == leaderboardData.SteamID.ToString());
                        if (botReward != null)
                            botReward.percentile = percentileReward.UpperPercentile;
                        else
                            botRewards.Add(new(leaderboardData.SteamID.ToString(), PlayerSeasonalLeaderboard.IndexOf(leaderboardData) + 1, percentileReward.UpperPercentile));
                    }

                    embed.fields[1].value += $"**Top {percentileReward.UpperPercentile}%:** {upperIndex - lowerIndex} players \n";
                }

                try
                {
                    var leaderboardReward = new BotLeaderboardReward("seasonal", PlayerSeasonalLeaderboard.Count, botRewards.ToArray());
                    var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(leaderboardReward));
                    using WebClient webClient = new();
                    var headers = webClient.Headers;
                    headers.Set(HttpRequestHeader.ContentType, "application/json");
                    _ = webClient.UploadData($"http://213.32.6.3:27116/leaderboard", bytes);
                }
                catch (Exception ex)
                {
                    Logger.Log("Error sending bot leaderboard reward info");
                    Logger.Log(ex);
                }
                
                try
                {
                    Plugin.Instance.Discord.SendEmbed(embed, "Leaderboard", Plugin.Instance.Config.Webhooks.FileData.LeaderboardWebhookLink);
                }
                catch (Exception ex)
                {
                    Logger.Log("Error sending embed");
                    Logger.Log(ex);
                }
            }

            Logging.Debug($"Giving bulk rewards, rewards: {bulkRewards.Count}");
            if (bulkRewards.Count > 0)
                TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.Reward.GiveBulkRewards(bulkRewards));

            Logging.Debug("Checking if quests need to be wiped");
            foreach (var data in PlayerData.Values)
            {
                if (data.Quests[0].QuestEnd > DateTimeOffset.UtcNow)
                    continue;

                List<PlayerQuest> playerQuests = new();
                Dictionary<EQuestType, List<PlayerQuest>> playerQuestsSearchByType = new();

                _ = new MySqlCommand($"DELETE FROM `{PLAYERS_QUESTS}` WHERE `SteamID` = {data.SteamID};", conn).ExecuteScalar();
                var expiryDate = ServerOptions.DailyLeaderboardWipe;
                List<Quest> questsToAdd = new();
                for (var i = 0; i < 6; i++)
                {
                    var randomQuests = Quests.Where(k => (int)k.QuestTier == i).ToList();
                    var randomQuest = randomQuests[UnityEngine.Random.Range(0, randomQuests.Count)];
                    questsToAdd.Add(randomQuest);
                }

                foreach (var quest in questsToAdd)
                {
                    PlayerQuest playerQuest = new(data.SteamID, quest, 0, expiryDate);
                    playerQuests.Add(playerQuest);
                    if (!playerQuestsSearchByType.ContainsKey(quest.QuestType))
                        playerQuestsSearchByType.Add(quest.QuestType, new());

                    playerQuestsSearchByType[quest.QuestType].Add(playerQuest);
                    _ = new MySqlCommand($"INSERT INTO `{PLAYERS_QUESTS}` (`SteamID` , `QuestID`, `Amount`, `QuestEnd`) VALUES ({data.SteamID}, {quest.QuestID}, 0, {expiryDate.ToUnixTimeSeconds()});", conn).ExecuteScalar();
                }

                data.Quests = playerQuests;
                data.QuestsSearchByType = playerQuestsSearchByType;
            }

            Logging.Debug("Checking if global xp booster needs to be wiped");
            if (ServerOptions.XPBoosterWipe < DateTimeOffset.UtcNow && ServerOptions.XPBooster != 0f)
            {
                ServerOptions.XPBooster = 0f;
                _ = new MySqlCommand($"UPDATE `{OPTIONS}` SET `XPBooster` = 0;", conn).ExecuteScalar();
            }

            Logging.Debug("Checking if global bp booster needs to be wiped");
            if (ServerOptions.BPBoosterWipe < DateTimeOffset.UtcNow && ServerOptions.BPBooster != 0f)
            {
                ServerOptions.BPBooster = 0f;
                _ = new MySqlCommand($"UPDATE `{OPTIONS}` SET `BPBooster` = 0;", conn).ExecuteScalar();
            }

            Logging.Debug("Checking if global gunxp booster needs to be wiped");
            if (ServerOptions.GunXPBoosterWipe < DateTimeOffset.UtcNow && ServerOptions.GunXPBooster != 0f)
            {
                ServerOptions.GunXPBooster = 0f;
                _ = new MySqlCommand($"UPDATE `{OPTIONS}` SET `GunXPBooster` = 0;", conn).ExecuteScalar();
            }

            Logging.Debug("Reloading boosters, checking prime, mute for all players");
            foreach (var data in PlayerData.Values)
            {
                _ = new MySqlCommand($"DELETE FROM `{PLAYERS_BOOSTERS}` WHERE `SteamID` = {data.SteamID} AND `BoosterExpiration` < {DateTimeOffset.UtcNow.ToUnixTimeSeconds()};", conn).ExecuteScalar();
                rdr = new MySqlCommand($"SELECT * FROM `{PLAYERS_BOOSTERS}` WHERE `SteamID` = {data.SteamID};", conn).ExecuteReader();
                try
                {
                    List<PlayerBooster> boosters = new();
                    while (rdr.Read())
                    {
                        if (!Enum.TryParse(rdr[1].ToString(), true, out EBoosterType boosterType))
                            return;

                        if (!float.TryParse(rdr[2].ToString(), out var boosterValue))
                            return;

                        if (!long.TryParse(rdr[3].ToString(), out var boosterExpirationUnix))
                            return;

                        var boosterExpiration = DateTimeOffset.FromUnixTimeSeconds(boosterExpirationUnix);
                        PlayerBooster booster = new(data.SteamID, boosterType, boosterValue, boosterExpiration);

                        boosters.Add(booster);
                    }

                    data.ActiveBoosters = boosters;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error reading player boosters for {data.SteamName}");
                    Logger.Log(ex);
                }
                finally
                {
                    rdr.Close();
                }

                data.SetPersonalBooster(EBoosterType.XP, data.XPBooster);
                data.SetPersonalBooster(EBoosterType.BPXP, data.BPBooster);
                data.SetPersonalBooster(EBoosterType.GUNXP, data.GunXPBooster);

                if (data.HasPrime)
                {
                    Logging.Debug($"{data.SteamName} has prime, checking if it needs rewards");
                    var maxRewardDate = DateTime.UtcNow;
                    if (DateTime.UtcNow > data.PrimeExpiry.UtcDateTime)
                    {
                        maxRewardDate = data.PrimeExpiry.UtcDateTime;
                        Logging.Debug($"Prime has expired for the player, setting the max reward date to expiry date which is {data.PrimeExpiry.UtcDateTime}");
                        data.HasPrime = false;
                        TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.Reward.RemoveRewards(data.SteamID, ServerOptions.PrimeRewards));
                        _ = new MySqlCommand($"UPDATE `{PLAYERS}` SET `HasPrime` = false WHERE `SteamID` = {data.SteamID};", conn).ExecuteScalar();
                        Logging.Debug("Removed prime for the player");
                    }

                    var daysWorthReward = (int)(maxRewardDate - data.PrimeLastDailyReward.UtcDateTime).TotalDays;
                    Logging.Debug($"Max reward date: {maxRewardDate}, last daily reward {data.PrimeLastDailyReward.UtcDateTime}, days worth reward: {daysWorthReward}");
                    if (daysWorthReward == 0)
                        continue;
                    
                    Logging.Debug("Need to give some reward to player, getting the list of daily rewards");
                    var dailyRewards = ServerOptions.PrimeDailyRewards.ToList();
                    if (daysWorthReward > 1)
                        Plugin.Instance.Reward.MultiplyRewards(dailyRewards, daysWorthReward);

                    TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.Reward.GiveRewards(data.SteamID, dailyRewards));
                    var lastDailyRewardDate = data.PrimeLastDailyReward.AddDays(daysWorthReward);
                    data.PrimeLastDailyReward = lastDailyRewardDate;
                    _ = new MySqlCommand($"UPDATE `{PLAYERS}` SET `PrimeLastDailyReward` = {lastDailyRewardDate.ToUnixTimeSeconds()} WHERE `SteamID` = {data.SteamID};", conn).ExecuteScalar();
                }

                if (data.IsMuted && DateTime.UtcNow > data.MuteExpiry.UtcDateTime)
                {
                    ChangePlayerMuted(data.SteamID, false);
                    try
                    {
                        Profile profile = new(data.SteamID.m_SteamID);

                        Embed embed = new(null, $"**{profile.SteamID}** was unmuted", null, "15105570", DateTime.UtcNow.ToString("s"), new(Provider.serverName, Provider.configData.Browser.Icon),
                            new(profile.SteamID, $"https://steamcommunity.com/profiles/{profile.SteamID64}/", profile.AvatarIcon.ToString()), new Field[] { new("**Unmuter:**", $"**Mute Expired**", true), new("**Time:**", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture), true) }, null, null);
                        
                        Plugin.Instance.Discord.SendEmbed(embed, "Player Unmuted", Plugin.Instance.Config.Webhooks.FileData.UnmuteWebhookLink);
                        TaskDispatcher.QueueOnMainThread(() => Utility.Say(UnturnedPlayer.FromCSteamID(data.SteamID), Plugin.Instance.Translate("Unmuted").ToRich()));
                    }
                    catch (Exception)
                    {
                        Logger.Log($"Error sending discord webhook for {data.SteamID}");
                    }
                }
            }

            Logging.Debug("Reloading unboxed amount for skins");
            rdr = new MySqlCommand($"SELECT `ID`,`UnboxedAmount` FROM `{GUNS_SKINS}`;", conn).ExecuteReader();
            try
            {
                while (rdr.Read())
                {
                    if (!int.TryParse(rdr[0].ToString(), out var id))
                        continue;

                    if (!int.TryParse(rdr[1].ToString(), out var unboxedAmount))
                        continue;

                    if (!GunSkinsSearchByID.TryGetValue(id, out var skin))
                        continue;

                    skin.UnboxedAmount = unboxedAmount;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading unboxed amounts for skins");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug("Reloading unboxed amount for knives");
            rdr = new MySqlCommand($"SELECT `KnifeID`,`UnboxedAmount` FROM `{KNIVES}`;", conn).ExecuteReader();
            try
            {
                while (rdr.Read())
                {
                    if (!ushort.TryParse(rdr[0].ToString(), out var knifeID))
                        continue;

                    if (!int.TryParse(rdr[1].ToString(), out var unboxedAmount))
                        continue;

                    if (!Knives.TryGetValue(knifeID, out var knife))
                        continue;

                    knife.UnboxedAmount = unboxedAmount;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading unboxed amounts for knives");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug("Reloading unboxed amount for gloves");
            rdr = new MySqlCommand($"SELECT `GloveID`,`UnboxedAmount` FROM `{GLOVES}`;", conn).ExecuteReader();
            try
            {
                while (rdr.Read())
                {
                    if (!int.TryParse(rdr[0].ToString(), out var gloveID))
                        continue;

                    if (!int.TryParse(rdr[1].ToString(), out var unboxedAmount))
                        continue;

                    if (!Gloves.TryGetValue(gloveID, out var glove))
                        continue;

                    glove.UnboxedAmount = unboxedAmount;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading unboxed amounts for gloves");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }

            Logging.Debug("Reloading unboxed amount for cards");
            rdr = new MySqlCommand($"SELECT `CardID`,`UnboxedAmount` FROM `{CARDS}`;", conn).ExecuteReader();
            try
            {
                while (rdr.Read())
                {
                    if (!int.TryParse(rdr[0].ToString(), out var cardID))
                        continue;

                    if (!int.TryParse(rdr[1].ToString(), out var unboxedAmount))
                        continue;

                    if (!Cards.TryGetValue(cardID, out var card))
                        continue;

                    card.UnboxedAmount = unboxedAmount;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading unboxed amounts for cards");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }
            
            Logging.Debug("Reloading unboxed amount for gun charms");
            rdr = new MySqlCommand($"SELECT `CharmID`,`UnboxedAmount` FROM `{GUNS_CHARMS}`;", conn).ExecuteReader();
            try
            {
                while (rdr.Read())
                {
                    if (!ushort.TryParse(rdr[0].ToString(), out var gunCharmID))
                        continue;

                    if (!int.TryParse(rdr[1].ToString(), out var unboxedAmount))
                        continue;

                    if (!GunCharms.TryGetValue(gunCharmID, out var gunCharm))
                        continue;

                    gunCharm.UnboxedAmount = unboxedAmount;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading unboxed amounts for gun charms");
                Logger.Log(ex);
            }
            finally
            {
                rdr.Close();
            }
            
            Logging.Debug($"Querying servers to get their info");
            foreach (var server in Servers)
            {
                Logging.Debug($"Querying server with IP: {server.FriendlyIP}, Port: {server.Port}");
                try
                {
                    var info = SteamServer.QueryServer(server.IP, server.PortNo, 1000);
                    server.Players = info.Players;
                    server.MaxPlayers = info.MaxPlayers;
                    server.Name = info.Name;
                    server.IsOnline = true;
                    Logging.Debug($"Found info, players: {info.Players}, max players: {info.MaxPlayers}, name: {info.Name}");
                }
                catch
                {
                    Logging.Debug($"Failed to get info, server is probably offline");
                    if (server.IsOnline)
                    {
                        server.LastOnline = DateTime.UtcNow;
                        server.IsOnline = false;
                    }
                }
            }
            
            Servers.Sort((a, b) =>
            {
                if (a.IsCurrentServer)
                    return -1;

                if (b.IsCurrentServer)
                    return 1;

                if (a.IsOnline && !b.IsOnline)
                    return -1;
                
                if (!a.IsOnline && b.IsOnline)
                    return 1;

                return Utility.ServerNameSort(a, b);
            });
            TaskDispatcher.QueueOnMainThread(() => Plugin.Instance.UI.OnServersUpdated());
        }
        catch (Exception ex)
        {
            Logger.Log("Error refreshing leaderboard data");
            Logger.Log(ex);
        }
        finally
        {
            conn.Close();
        }
    }

    // Player Data

    public void IncreasePlayerXP(CSteamID steamID, int xp)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `XP` = `XP` + {xp} WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.XP += xp;

        var updatedLevel = false;
        var player = Plugin.Instance.Game.GetGamePlayer(data.SteamID);
        if (player == null)
            return;

        while (data.TryGetNeededXP(out var neededXP) && data.XP >= neededXP)
        {
            updatedLevel = true;
            data.XP -= neededXP;
            data.Level++;

            Plugin.Instance.UI.SendAnimation(player, new(EAnimationType.LEVEL_UP, data.Level));
            if (ItemsSearchByLevel.TryGetValue(data.Level, out var unlocks))
            {
                foreach (var unlock in unlocks)
                    Plugin.Instance.UI.SendAnimation(player, new(EAnimationType.ITEM_UNLOCK, unlock));
            }
        }

        if (!updatedLevel)
            return;

        AddQuery($"UPDATE `{PLAYERS}` SET `Level` = {data.Level}, `XP` = {data.XP} WHERE `SteamID` = {steamID};");
        if (PlayerDailyLeaderboardLookup.TryGetValue(steamID, out var dailyLeaderboard))
            dailyLeaderboard.Level = data.Level;

        if (PlayerWeeklyLeaderboardLookup.TryGetValue(steamID, out var weeklyLeaderboard))
            weeklyLeaderboard.Level = data.Level;

        if (PlayerSeasonalLeaderboardLookup.TryGetValue(steamID, out var seasonalLeaderboard))
            seasonalLeaderboard.Level = data.Level;

        if (PlayerAllTimeLeaderboardLookup.TryGetValue(steamID, out var allTimeLeaderboard))
            allTimeLeaderboard.Level = data.Level;
    }

    public void IncreasePlayerCredits(CSteamID steamID, int credits)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `Credits` = `Credits` + {credits} WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.Credits += credits;
        Plugin.Instance.UI.OnCurrencyUpdated(steamID, ECurrency.CREDIT);
    }

    public void DecreasePlayerCredits(CSteamID steamID, int credits)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `Credits` = `Credits` - {credits} WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.Credits -= credits;
        Plugin.Instance.UI.OnCurrencyUpdated(steamID, ECurrency.CREDIT);
    }

    public void IncreasePlayerScrap(CSteamID steamID, int scrap)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `Scrap` = `Scrap` + {scrap} WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.Scrap += scrap;
        Plugin.Instance.UI.OnCurrencyUpdated(steamID, ECurrency.SCRAP);
    }

    public void DecreasePlayerScrap(CSteamID steamID, int scrap)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `Scrap` = `Scrap` - {scrap} WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.Scrap -= scrap;
        Plugin.Instance.UI.OnCurrencyUpdated(steamID, ECurrency.SCRAP);
    }

    public void IncreasePlayerCoins(CSteamID steamID, int coins)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `Coins` = `Coins` + {coins} WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.Coins += coins;
        Plugin.Instance.UI.OnCurrencyUpdated(steamID, ECurrency.COIN);
    }

    public void DecreasePlayerCoins(CSteamID steamID, int coins)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `Coins` = `Coins` - {coins} WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.Coins -= coins;
        Plugin.Instance.UI.OnCurrencyUpdated(steamID, ECurrency.COIN);
    }

    public void IncreasePlayerKills(CSteamID steamID, int kills)
    {
        AddQueries(new()
        {
            $"UPDATE `{PLAYERS}` SET `Kills` = `Kills` + {kills} WHERE `SteamID` = {steamID};",
            $"UPDATE `{PLAYERS_LEADERBOARD_DAILY}` SET `Kills` = `Kills` + {kills} WHERE `SteamID` = {steamID};",
            $"UPDATE `{PLAYERS_LEADERBOARD_WEEKLY}` SET `Kills` = `Kills` + {kills} WHERE `SteamID` = {steamID};",
            $"UPDATE `{PLAYERS_LEADERBOARD_SEASONAL}` SET `Kills` = `Kills` + {kills} WHERE `SteamID` = {steamID};"
        });

        if (PlayerData.TryGetValue(steamID, out var data))
            data.Kills += kills;

        if (PlayerDailyLeaderboardLookup.TryGetValue(steamID, out var dailyLeaderboard))
            dailyLeaderboard.Kills += kills;

        if (PlayerWeeklyLeaderboardLookup.TryGetValue(steamID, out var weeklyLeaderboard))
            weeklyLeaderboard.Kills += kills;

        if (PlayerSeasonalLeaderboardLookup.TryGetValue(steamID, out var seasonalLeaderboard))
            seasonalLeaderboard.Kills += kills;

        if (PlayerAllTimeLeaderboardLookup.TryGetValue(steamID, out var allTimeLeaderboard))
            allTimeLeaderboard.Kills += kills;
    }

    public void IncreasePlayerHeadshotKills(CSteamID steamID, int headshotKills)
    {
        AddQueries(new()
        {
            $"UPDATE `{PLAYERS}` SET `HeadshotKills` = `HeadshotKills` + {headshotKills} WHERE `SteamID` = {steamID};",
            $"UPDATE `{PLAYERS_LEADERBOARD_DAILY}` SET `HeadshotKills` = `HeadshotKills` + {headshotKills} WHERE `SteamID` = {steamID};",
            $"UPDATE `{PLAYERS_LEADERBOARD_WEEKLY}` SET `HeadshotKills` = `HeadshotKills` + {headshotKills} WHERE `SteamID` = {steamID};",
            $"UPDATE `{PLAYERS_LEADERBOARD_SEASONAL}` SET `HeadshotKills` = `HeadshotKills` + {headshotKills} WHERE `SteamID` = {steamID};"
        });

        if (PlayerData.TryGetValue(steamID, out var data))
            data.HeadshotKills += headshotKills;

        if (PlayerDailyLeaderboardLookup.TryGetValue(steamID, out var dailyLeaderboard))
            dailyLeaderboard.HeadshotKills += headshotKills;

        if (PlayerWeeklyLeaderboardLookup.TryGetValue(steamID, out var weeklyLeaderboard))
            weeklyLeaderboard.HeadshotKills += headshotKills;

        if (PlayerSeasonalLeaderboardLookup.TryGetValue(steamID, out var seasonalLeaderboard))
            seasonalLeaderboard.HeadshotKills += headshotKills;

        if (PlayerAllTimeLeaderboardLookup.TryGetValue(steamID, out var allTimeLeaderboard))
            allTimeLeaderboard.HeadshotKills += headshotKills;
    }

    public void UpdatePlayerHighestKillstreak(CSteamID steamID, int killstreak)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `HighestKillstreak` = {killstreak} WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.HighestKillstreak = killstreak;
    }

    public void UpdatePlayerHighestMultikills(CSteamID steamID, int multikills)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `HighestMultiKills` = {multikills} WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.HighestMultiKills = multikills;
    }

    public void IncreasePlayerKillsConfirmed(CSteamID steamID, int killsConfirmed)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `KillsConfirmed` = `KillsConfirmed` + {killsConfirmed} WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.KillsConfirmed += killsConfirmed;
    }

    public void IncreasePlayerKillsDenied(CSteamID steamID, int killsDenied)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `KillsDenied` = `KillsDenied` + {killsDenied} WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.KillsDenied += killsDenied;
    }

    public void IncreasePlayerFlagsCaptured(CSteamID steamID, int flagsCaptured)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `FlagsCaptured` = `FlagsCaptured` + {flagsCaptured} WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.FlagsCaptured += flagsCaptured;
    }

    public void IncreasePlayerFlagsSaved(CSteamID steamID, int flagsSaved)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `FlagsSaved` = `FlagsSaved` + {flagsSaved} WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.FlagsSaved += flagsSaved;
    }

    public void IncreasePlayerAreasTaken(CSteamID steamID, int areasTaken)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `AreasTaken` = `AreasTaken` + {areasTaken} WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.AreasTaken += areasTaken;
    }

    public void IncreasePlayerDeaths(CSteamID steamID, int deaths)
    {
        AddQueries(new()
        {
            $"UPDATE `{PLAYERS}` SET `Deaths` = `Deaths` + {deaths} WHERE `SteamID` = {steamID};",
            $"UPDATE `{PLAYERS_LEADERBOARD_DAILY}` SET `Deaths` = `Deaths` + {deaths} WHERE `SteamID` = {steamID};",
            $"UPDATE `{PLAYERS_LEADERBOARD_WEEKLY}` SET `Deaths` = `Deaths` + {deaths} WHERE `SteamID` = {steamID};",
            $"UPDATE `{PLAYERS_LEADERBOARD_SEASONAL}` SET `Deaths` = `Deaths` + {deaths} WHERE `SteamID` = {steamID};"
        });

        if (PlayerData.TryGetValue(steamID, out var data))
            data.Deaths += deaths;

        if (PlayerDailyLeaderboardLookup.TryGetValue(steamID, out var dailyLeaderboard))
            dailyLeaderboard.Deaths += deaths;

        if (PlayerWeeklyLeaderboardLookup.TryGetValue(steamID, out var weeklyLeaderboard))
            weeklyLeaderboard.Deaths += deaths;

        if (PlayerSeasonalLeaderboardLookup.TryGetValue(steamID, out var seasonalLeaderboard))
            seasonalLeaderboard.Deaths += deaths;

        if (PlayerAllTimeLeaderboardLookup.TryGetValue(steamID, out var allTimeLeaderboard))
            allTimeLeaderboard.Deaths += deaths;
    }

    public void ChangePlayerMusic(CSteamID steamID, bool music)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `Music` = {music} WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.Music = music;
    }

    public void UpdatePlayerCountryCode(CSteamID steamID, string countryCode)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `CountryCode` = '{countryCode}' WHERE `SteamID` = {steamID};");
        if (PlayerData.TryGetValue(steamID, out var data))
            data.CountryCode = countryCode;

        if (PlayerDailyLeaderboardLookup.TryGetValue(steamID, out var dailyLeaderboard))
            dailyLeaderboard.CountryCode = countryCode;

        if (PlayerWeeklyLeaderboardLookup.TryGetValue(steamID, out var weeklyLeaderboard))
            weeklyLeaderboard.CountryCode = countryCode;

        if (PlayerSeasonalLeaderboardLookup.TryGetValue(steamID, out var seasonalLeaderboard))
            seasonalLeaderboard.CountryCode = countryCode;

        if (PlayerAllTimeLeaderboardLookup.TryGetValue(steamID, out var allTimeLeaderboard))
            allTimeLeaderboard.CountryCode = countryCode;
    }

    public void ChangePlayerHideFlag(CSteamID steamID, bool hideFlag)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `HideFlag` = {hideFlag} WHERE `SteamID` = {steamID};");
        if (PlayerData.TryGetValue(steamID, out var data))
            data.HideFlag = hideFlag;

        if (PlayerDailyLeaderboardLookup.TryGetValue(steamID, out var dailyLeaderboard))
            dailyLeaderboard.HideFlag = hideFlag;

        if (PlayerWeeklyLeaderboardLookup.TryGetValue(steamID, out var weeklyLeaderboard))
            weeklyLeaderboard.HideFlag = hideFlag;

        if (PlayerSeasonalLeaderboardLookup.TryGetValue(steamID, out var seasonalLeaderboard))
            seasonalLeaderboard.HideFlag = hideFlag;

        if (PlayerAllTimeLeaderboardLookup.TryGetValue(steamID, out var allTimeLeaderboard))
            allTimeLeaderboard.HideFlag = hideFlag;
    }

    public void ChangePlayerMuted(CSteamID steamID, bool muted)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `IsMuted` = {muted} WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.IsMuted = muted;
    }

    public void ChangePlayerMuteExpiry(CSteamID steamID, DateTimeOffset muteExpiry)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `MuteExpiry` = {muteExpiry.ToUnixTimeSeconds()} WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.MuteExpiry = muteExpiry;
    }

    public void ChangePlayerMuteReason(CSteamID steamID, string muteReason)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `MuteReason` = '{muteReason}' WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;
        
        data.MuteReason = muteReason;
    }
    
    public void IncreasePlayerBooster(CSteamID steamID, EBoosterType boosterType, float increaseBooster)
    {
        var coloumnName = "None";
        switch (boosterType)
        {
            case EBoosterType.XP:
                coloumnName = "XPBooster";
                break;
            case EBoosterType.BPXP:
                coloumnName = "BPBooster";
                break;
            case EBoosterType.GUNXP:
                coloumnName = "GunXPBooster";
                break;
        }

        AddQuery($"UPDATE `{PLAYERS}` SET `{coloumnName}` = `{coloumnName}` + {increaseBooster} WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.SetPersonalBooster(boosterType, data.GetPersonalBooster(boosterType) + increaseBooster);
    }

    public void AddPlayerPrime(CSteamID steamID, int days)
    {
        if (!PlayerData.TryGetValue(steamID, out var data))
            throw new ArgumentNullException("data", $"Player data for {steamID} is not found, please make sure prime is only added when the player is online");

        if (data.HasPrime)
        {
            data.PrimeExpiry = data.PrimeExpiry.AddDays(days);
            AddQuery($"UPDATE `{PLAYERS}` SET `PrimeExpiry` = {data.PrimeExpiry.ToUnixTimeSeconds()} WHERE `SteamID` = {steamID};");
        }
        else
        {
            data.HasPrime = true;
            data.PrimeExpiry = DateTimeOffset.UtcNow.AddDays(days);
            data.PrimeLastDailyReward = DateTimeOffset.UtcNow;
            Plugin.Instance.Reward.GiveRewards(steamID, ServerOptions.PrimeRewards);
            AddQuery($"UPDATE `{PLAYERS}` SET `HasPrime` = {data.HasPrime}, `PrimeExpiry` = {data.PrimeExpiry.ToUnixTimeSeconds()} , `PrimeLastDailyReward` = {data.PrimeLastDailyReward.ToUnixTimeSeconds()} WHERE `SteamID` = {steamID};");
        }

        if (PlayerDailyLeaderboardLookup.TryGetValue(steamID, out var dailyLeaderboard))
            dailyLeaderboard.HasPrime = true;

        if (PlayerWeeklyLeaderboardLookup.TryGetValue(steamID, out var weeklyLeaderboard))
            weeklyLeaderboard.HasPrime = true;

        if (PlayerSeasonalLeaderboardLookup.TryGetValue(steamID, out var seasonalLeaderboard))
            seasonalLeaderboard.HasPrime = true;

        if (PlayerAllTimeLeaderboardLookup.TryGetValue(steamID, out var allTimeLeaderboard))
            allTimeLeaderboard.HasPrime = true;
    }

    public void SetPlayerVolume(CSteamID id, int volume)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `Volume` = {volume} WHERE `SteamID` = {id};");
        if (!PlayerData.TryGetValue(id, out var data))
            return;

        data.Volume = volume;
    }

    public void SetPlayerHotkeys(CSteamID id, List<int> hotkeys)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `Hotkeys` = '{hotkeys.GetStringFromIntList()}' WHERE `SteamID` = {id};");
    }

    public void AddPlayerBattlepass(CSteamID steamID)
    {
        AddQuery($"UPDATE `{PLAYERS}` SET `HasBattlepass` = true WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.HasBattlepass = true;
    }

    // Player Guns

    public void AddPlayerGunBought(CSteamID steamID, ushort gunID)
    {
        if (!Guns.TryGetValue(gunID, out var gun))
            throw new ArgumentNullException("gun", $"Gun {gunID} is not found, adding gun to player with steam id {steamID}");

        AddQuery(
            $"INSERT INTO `{PLAYERS_GUNS}` (`SteamID` , `GunID` , `Level` , `XP` , `GunKills` , `IsBought` , `Attachments`) VALUES ({steamID} , {gunID} , 1 , 0 , 0 , true , '{Utility.CreateStringFromDefaultAttachments(gun.DefaultAttachments) + Utility.CreateStringFromRewardAttachments(gun.RewardAttachments.Values.ToList())}') ON DUPLICATE KEY UPDATE `IsBought` = true;");

        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return;

        Dictionary<ushort, LoadoutAttachment> loadoutAttachments = new();
        foreach (var attachment in gun.DefaultAttachments)
        {
            if (loadoutAttachments.ContainsKey(attachment.AttachmentID))
            {
                Logging.Debug($"Duplicate default attachment found for gun {gunID} with id {attachment.AttachmentID}, ignoring it");
                continue;
            }

            loadoutAttachments.Add(attachment.AttachmentID, new(attachment, 0, true, false));
        }

        foreach (var attachment in gun.RewardAttachments)
        {
            if (loadoutAttachments.ContainsKey(attachment.Value.AttachmentID))
            {
                Logging.Debug($"Duplicate reward attachment found for gun {gunID} with id {attachment.Value.AttachmentID}, ignoring it");
                continue;
            }

            loadoutAttachments.Add(attachment.Value.AttachmentID, new(attachment.Value, attachment.Key, true, false));
        }

        LoadoutGun loadoutGun = new(gun, 1, 0, 0, true, false, loadoutAttachments);
        if (loadout.Guns.ContainsKey(loadoutGun.Gun.GunID))
        {
            loadout.Guns[loadoutGun.Gun.GunID].IsBought = true;
            return;
        }

        loadout.Guns.Add(loadoutGun.Gun.GunID, loadoutGun);
        Plugin.Instance.UI.OnUIUpdated(steamID, (EUIPage)(byte)loadoutGun.Gun.GunType);
    }

    public void IncreasePlayerGunXP(CSteamID steamID, ushort gunID, int increaseXP)
    {
        AddQuery($"UPDATE `{PLAYERS_GUNS}` SET `XP` = `XP` + {increaseXP} WHERE `SteamID` = {steamID} AND `GunID` = {gunID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return;

        if (!loadout.Guns.TryGetValue(gunID, out var gun))
            return;

        gun.XP += increaseXP;
        var updatedLevel = false;
        var player = Plugin.Instance.Game.GetGamePlayer(data.SteamID);
        if (player == null)
            return;

        while (gun.TryGetNeededXP(out var neededXP) && gun.XP >= neededXP)
        {
            updatedLevel = true;
            gun.XP -= neededXP;
            gun.Level++;

            var icon = gun.Gun.IconLink;
            if ((player.ActiveLoadout?.Primary?.Gun?.GunID ?? 0) == gun.Gun.GunID && (player.ActiveLoadout?.PrimarySkin?.Gun?.GunID ?? 0) == gun.Gun.GunID)
                icon = player.ActiveLoadout?.PrimarySkin?.IconLink ?? string.Empty;
            else if ((player.ActiveLoadout?.Secondary?.Gun?.GunID ?? 0) == gun.Gun.GunID && (player.ActiveLoadout?.SecondarySkin?.Gun?.GunID ?? 0) == gun.Gun.GunID)
                icon = player.ActiveLoadout?.SecondarySkin?.IconLink ?? string.Empty;

            Plugin.Instance.UI.SendAnimation(player, new(EAnimationType.GUN_LEVEL_UP, new AnimationItemUnlock(icon, gun.Level.ToString(), gun.Gun.GunName)));
            if (gun.Gun.RewardAttachments.TryGetValue(gun.Level, out var attachment))
                Plugin.Instance.UI.SendAnimation(player, new(EAnimationType.ITEM_UNLOCK, new AnimationItemUnlock(attachment.IconLink, "", $"{attachment.AttachmentName} [{gun.Gun.GunName}]")));
        }

        if (updatedLevel)
            AddQuery($"UPDATE `{PLAYERS_GUNS}` SET `Level` = {gun.Level}, `XP` = {gun.XP} WHERE `SteamID` = {steamID} AND `GunID` = {gunID};");
    }

    public void IncreasePlayerGunKills(CSteamID steamID, ushort gunID, int kills)
    {
        AddQuery($"UPDATE `{PLAYERS_GUNS}` SET `GunKills` = `GunKills` + {kills} WHERE `SteamID` = {steamID} AND `GunID` = {gunID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return;

        if (!loadout.Guns.TryGetValue(gunID, out var gun))
            return;

        gun.GunKills += kills;
    }

    public bool UpdatePlayerGunBought(CSteamID steamID, ushort gunID, bool isBought)
    {
        AddQuery($"UPDATE `{PLAYERS_GUNS}` SET `IsBought` = {isBought} WHERE `SteamID` = {steamID} AND `GunID` = {gunID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return false;

        if (!loadout.Guns.TryGetValue(gunID, out var gun))
            return false;

        gun.IsBought = isBought;
        return true;
    }

    public bool UpdatePlayerGunUnlocked(CSteamID steamID, ushort gunID, bool isUnlocked)
    {
        AddQuery($"UPDATE `{PLAYERS_GUNS}` SET `IsUnlocked` = {isUnlocked} WHERE `SteamID` = {steamID} AND `GunID` = {gunID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return false;

        if (!loadout.Guns.TryGetValue(gunID, out var gun))
            return false;

        gun.IsUnlocked = isUnlocked;
        return true;
    }

    // Player Guns Attachments

    public bool UpdatePlayerGunAttachmentBought(CSteamID steamID, ushort gunID, ushort attachmentID, bool isBought)
    {
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            throw new ArgumentNullException("loadout", $"Could'nt find loadout for {steamID} while adding gun attachment bought to {attachmentID} for gun with id {gunID}");

        if (!loadout.Guns.TryGetValue(gunID, out var gun))
            throw new ArgumentNullException("gun", $"Could'nt find loadout gun with id {gunID} in player ({steamID}) loadout");

        if (!gun.Attachments.TryGetValue(attachmentID, out var attachment))
            throw new ArgumentNullException("attachment", $"Could'nt find attachment with id {attachmentID} for gun with id {gunID} for player ({steamID})");

        attachment.IsBought = isBought;
        AddQuery($"UPDATE `{PLAYERS_GUNS}` SET `Attachments` = '{Utility.GetStringFromAttachments(gun.Attachments.Values.ToList())}' WHERE `SteamID` = {steamID} AND `GunID` = {gunID};");
        return true;
    }

    public bool UpdatePlayerGunAttachmentUnlocked(CSteamID steamID, ushort gunID, ushort attachmentID, bool isUnlocked)
    {
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            throw new ArgumentNullException("loadout", $"Could'nt find loadout for {steamID} while adding gun attachment unlocked to {attachmentID} for gun with id {gunID}");

        if (!loadout.Guns.TryGetValue(gunID, out var gun))
            throw new ArgumentNullException("gun", $"Could'nt find loadout gun with id {gunID} in player ({steamID}) loadout");

        if (!gun.Attachments.TryGetValue(attachmentID, out var attachment))
            throw new ArgumentNullException("attachment", $"Could'nt find attachment with id {attachmentID} for gun with id {gunID} for player ({steamID})");

        attachment.IsUnlocked = isUnlocked;
        AddQuery($"UPDATE `{PLAYERS_GUNS}` SET `Attachments` = '{Utility.GetStringFromAttachments(gun.Attachments.Values.ToList())}' WHERE `SteamID` = {steamID} AND `GunID` = {gunID};");
        return true;
    }

    // Player Guns Skins

    public void AddPlayerGunSkin(CSteamID steamID, int id)
    {
        if (!GunSkinsSearchByID.TryGetValue(id, out var skin))
            throw new ArgumentNullException("id", $"Skin with id {id} doesn't exist in the database, while adding to {steamID}");

        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            throw new ArgumentNullException("loadout", $"Loadout for player with steam id {steamID} is not found, please ensure that the player is online");

        if (loadout.GunSkinsSearchByID.ContainsKey(id))
            return;

        loadout.GunSkinsSearchByID.Add(id, skin);
        if (!loadout.GunSkinsSearchByGunID.ContainsKey(skin.Gun.GunID))
            loadout.GunSkinsSearchByGunID.Add(skin.Gun.GunID, new());

        loadout.GunSkinsSearchByGunID[skin.Gun.GunID].Add(skin);
        loadout.GunSkinsSearchBySkinID.Add(skin.SkinID, skin);

        var skinsString = loadout.GunSkinsSearchByID.Keys.ToList().GetStringFromIntList();
        AddQuery($"UPDATE `{PLAYERS_GUNS_SKINS}` SET `SkinIDs` = '{skinsString}' WHERE `SteamID` = {steamID};");
        IncreasePlayerGunSkinUnboxedAmount(id, 1);
        Plugin.Instance.UI.OnUIUpdated(steamID, EUIPage.GUN_SKIN);
    }

    public void IncreasePlayerGunSkinUnboxedAmount(int id, int amount)
    {
        if (!GunSkinsSearchByID.TryGetValue(id, out var skin))
            throw new ArgumentNullException("id", $"Skin with id {id} is not found");

        AddQuery($"UPDATE `{GUNS_SKINS}` SET `UnboxedAmount` = `UnboxedAmount` + {amount} WHERE `ID` = {id};");
        skin.UnboxedAmount += amount;
    }

    // Player Guns Charms

    public void AddPlayerGunCharmBought(CSteamID steamID, ushort gunCharmID)
    {
        if (!GunCharms.TryGetValue(gunCharmID, out var charm))
            throw new ArgumentNullException("id", $"Charm with id {gunCharmID} doesn't exist in the database, while adding to {steamID}");

        AddQuery($"INSERT INTO `{PLAYERS_GUNS_CHARMS}` (`SteamID` , `CharmID` , `IsBought`) VALUES ({steamID} , {gunCharmID} , true) ON DUPLICATE KEY UPDATE `IsBought` = true;");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            throw new ArgumentNullException("loadout", $"Loadout for player with steam id {steamID} is not found, please ensure that the player is online");

        if (loadout.GunCharms.TryGetValue(gunCharmID, out var gunCharm))
        {
            gunCharm.IsBought = true;
            return;
        }

        loadout.GunCharms.Add(gunCharmID, new(charm, true, false));
        IncreasePlayerGunSkinUnboxedAmount(gunCharmID, 1);
        Plugin.Instance.UI.OnUIUpdated(steamID, EUIPage.GUN_CHARM);
    }

    public void RemovePlayerGunCharm(CSteamID steamID, ushort gunCharmID)
    {
        if (!GunCharms.ContainsKey(gunCharmID))
            throw new ArgumentNullException("id", $"Charm with id {gunCharmID} doesn't exist in the database, while removing from {steamID}");

        AddQuery($"DELETE FROM `{PLAYERS_GUNS_CHARMS}` WHERE `SteamID` = {steamID} AND `CharmID` = {gunCharmID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return;

        if (!loadout.GunCharms.ContainsKey(gunCharmID))
            return;

        loadout.GunCharms.Remove(gunCharmID);
        Plugin.Instance.UI.OnUIUpdated(steamID, EUIPage.GUN_CHARM);
    }

    public bool UpdatePlayerGunCharmBought(CSteamID steamID, ushort gunCharmID, bool isBought)
    {
        if (!GunCharms.ContainsKey(gunCharmID))
            throw new ArgumentNullException("id", $"Charm with id {gunCharmID} doesn't exist in the database, while updating bought for {steamID}");

        AddQuery($"UPDATE `{PLAYERS_GUNS_CHARMS}` SET `IsBought` = {isBought} WHERE `SteamID` = {steamID} AND `CharmID` = {gunCharmID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return false;

        if (!loadout.GunCharms.TryGetValue(gunCharmID, out var gunCharm))
            return false;

        gunCharm.IsBought = isBought;
        if (isBought)
            IncreasePlayerGunSkinUnboxedAmount(gunCharmID, 1);
        
        return true;
    }

    public bool UpdatePlayerGunCharmUnlocked(CSteamID steamID, ushort gunCharmID, bool isUnlocked)
    {
        if (!GunCharms.ContainsKey(gunCharmID))
            throw new ArgumentNullException("id", $"Charm with id {gunCharmID} doesn't exist in the database, while updating unlocked for {steamID}");

        AddQuery($"UPDATE `{PLAYERS_GUNS_CHARMS}` SET `IsUnlocked` = {isUnlocked} WHERE `SteamID` = {steamID} AND `CharmID` = {gunCharmID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return false;

        if (!loadout.GunCharms.TryGetValue(gunCharmID, out var gunCharm))
            return false;

        gunCharm.IsUnlocked = isUnlocked;
        return true;
    }

    public void IncreasePlayerGunCharmUnboxedAmount(ushort id, int amount)
    {
        if (!GunCharms.TryGetValue(id, out var charm))
            throw new ArgumentNullException(nameof(id), $"Charm with id {id} is not found");

        AddQuery($"UPDATE `{GUNS_CHARMS}` SET `UnboxedAmount` = `UnboxedAmount` + {amount} WHERE `CharmID` = {id};");
        charm.UnboxedAmount += amount;
    }
    
    // Player Knives

    public void AddPlayerKnifeBought(CSteamID steamID, ushort knifeID)
    {
        if (!Knives.TryGetValue(knifeID, out var knife))
            throw new ArgumentNullException("id", $"Knife with id {knifeID} doesn't exist in the database, while adding to {steamID}");

        AddQuery($"INSERT INTO `{PLAYERS_KNIVES}` (`SteamID` , `KnifeID` , `KnifeKills` , `IsBought`) VALUES ({steamID} , {knifeID} , 0 , true) ON DUPLICATE KEY UPDATE `IsBought` = true;");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return;

        if (loadout.Knives.TryGetValue(knifeID, out var playerKnife))
        {
            playerKnife.IsBought = true;
            return;
        }

        loadout.Knives.Add(knifeID, new(knife, 0, true, false));
        IncreasePlayerKnifeUnboxedAmount(knifeID, 1);
        Plugin.Instance.UI.OnUIUpdated(steamID, EUIPage.KNIFE);
    }

    public void IncreasePlayerKnifeKills(CSteamID steamID, ushort knifeID, int amount)
    {
        if (!Knives.TryGetValue(knifeID, out var knife))
            throw new ArgumentNullException("id", $"Knife with id {knifeID} doesn't exist in the database, while increasing kills for {steamID}");

        AddQuery($"UPDATE `{PLAYERS_KNIVES}` SET `KnifeKills` = `KnifeKills` + {amount} WHERE `SteamID` = {steamID} AND `KnifeID` = {knifeID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return;

        if (!loadout.Knives.TryGetValue(knifeID, out var playerKnife))
            return;

        playerKnife.KnifeKills += amount;
    }

    public bool UpdatePlayerKnifeBought(CSteamID steamID, ushort knifeID, bool isBought)
    {
        if (!Knives.TryGetValue(knifeID, out var knife))
            throw new ArgumentNullException("id", $"Knife with id {knifeID} doesn't exist in the database, while updating bought for {steamID}");

        AddQuery($"UPDATE `{PLAYERS_KNIVES}` SET `IsBought` = {isBought} WHERE `SteamID` = {steamID} AND `KnifeID` = {knifeID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return false;

        if (!loadout.Knives.TryGetValue(knifeID, out var playerKnife))
            return false;

        playerKnife.IsBought = isBought;
        if (isBought)
            IncreasePlayerKnifeUnboxedAmount(knifeID, 1);
        
        return true;
    }

    public bool UpdatePlayerKnifeUnlocked(CSteamID steamID, ushort knifeID, bool isUnlocked)
    {
        if (!Knives.TryGetValue(knifeID, out var knife))
            throw new ArgumentNullException("id", $"Knife with id {knifeID} doesn't exist in the database, while updating unlocked for {steamID}");

        AddQuery($"UPDATE `{PLAYERS_KNIVES}` SET `IsUnlocked` = {isUnlocked} WHERE `SteamID` = {steamID} AND `KnifeID` = {knifeID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return false;

        if (!loadout.Knives.TryGetValue(knifeID, out var playerKnife))
            return false;

        playerKnife.IsUnlocked = isUnlocked;
        return true;
    }

    public void IncreasePlayerKnifeUnboxedAmount(ushort knifeID, int amount)
    {
        if (!Knives.TryGetValue(knifeID, out var knife))
            throw new ArgumentNullException("id", $"Knife with id {knifeID} doesn't exist in the database, while updating unboxed");

        AddQuery($"UPDATE `{KNIVES}` SET `UnboxedAmount` = `UnboxedAmount` + {amount} WHERE `KnifeID` = {knifeID};");
        knife.UnboxedAmount += amount;
    }

    // Player Perks

    public void AddPlayerPerkBought(CSteamID steamID, int perkID)
    {
        if (!Perks.TryGetValue(perkID, out var perk))
            throw new ArgumentNullException("id", $"Perk with id {perkID} doesn't exist in the database, while adding to {steamID}");

        AddQuery($"INSERT INTO `{PLAYERS_PERKS}` (`SteamID` , `PerkID` , `IsBought`) VALUES ({steamID} , {perkID} , true) ON DUPLICATE KEY UPDATE `IsBought` = true;");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return;

        if (loadout.Perks.TryGetValue(perkID, out var playerPerk))
        {
            playerPerk.IsBought = true;
            return;
        }

        loadout.Perks.Add(perkID, new(perk, true, false));
        Plugin.Instance.UI.OnUIUpdated(steamID, EUIPage.PERK);
    }

    public bool UpdatePlayerPerkBought(CSteamID steamID, int perkID, bool isBought)
    {
        if (!Perks.TryGetValue(perkID, out var perk))
            throw new ArgumentNullException("id", $"Perk with id {perkID} doesn't exist in the database, while updating bought for {steamID}");

        AddQuery($"UPDATE `{PLAYERS_PERKS}` SET `IsBought` = {isBought} WHERE `SteamID` = {steamID} AND `PerkID` = {perkID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return false;

        if (!loadout.Perks.TryGetValue(perkID, out var playerPerk))
            return false;

        playerPerk.IsBought = isBought;
        return true;
    }

    public bool UpdatePlayerPerkUnlocked(CSteamID steamID, int perkID, bool isUnlocked)
    {
        if (!Perks.TryGetValue(perkID, out var perk))
            throw new ArgumentNullException("id", $"Perk with id {perkID} doesn't exist in the database, while updating unlocked for {steamID}");

        AddQuery($"UPDATE `{PLAYERS_PERKS}` SET `IsUnlocked` = {isUnlocked} WHERE `SteamID` = {steamID} AND `PerkID` = {perkID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return false;

        if (!loadout.Perks.TryGetValue(perkID, out var playerPerk))
            return false;

        playerPerk.IsUnlocked = isUnlocked;
        return true;
    }

    // Player Gadgets

    public void AddPlayerGadgetBought(CSteamID steamID, ushort gadgetID)
    {
        if (!Gadgets.TryGetValue(gadgetID, out var gadget))
            throw new ArgumentNullException("id", $"Gadget with id {gadgetID} doesn't exist in the database, while adding to {steamID}");

        AddQuery($"INSERT INTO `{PLAYERS_GADGETS}` (`SteamID` , `GadgetID` , `GadgetKills` , `IsBought`) VALUES ({steamID} , {gadgetID} , 0 , true) ON DUPLICATE KEY UPDATE `IsBought` = true;");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return;

        if (loadout.Gadgets.TryGetValue(gadgetID, out var playerGadget))
        {
            playerGadget.IsBought = true;
            return;
        }

        loadout.Gadgets.Add(gadgetID, new(gadget, 0, true, false));
        Plugin.Instance.UI.OnUIUpdated(steamID, gadget.IsTactical ? EUIPage.TACTICAL : EUIPage.LETHAL);
    }

    public void IncreasePlayerGadgetKills(CSteamID steamID, ushort gadgetID, int amount)
    {
        if (!Gadgets.TryGetValue(gadgetID, out var gadget))
            throw new ArgumentNullException("id", $"Gadget with id {gadgetID} doesn't exist in the database, while increasing kills for {steamID}");

        AddQuery($"UPDATE `{PLAYERS_GADGETS}` SET `GadgetKills` = `GadgetKills` + {amount} WHERE `SteamID` = {steamID} AND `GadgetID` = {gadgetID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return;

        if (!loadout.Gadgets.TryGetValue(gadgetID, out var playerGadget))
            return;

        playerGadget.GadgetKills += amount;
    }

    public bool UpdatePlayerGadgetBought(CSteamID steamID, ushort gadgetID, bool isBought)
    {
        if (!Gadgets.TryGetValue(gadgetID, out var gadget))
            throw new ArgumentNullException("id", $"Gadget with id {gadgetID} doesn't exist in the database, while updating bought for {steamID}");

        AddQuery($"UPDATE `{PLAYERS_GADGETS}` SET `IsBought` = {isBought} WHERE `SteamID` = {steamID} AND `GadgetID` = {gadgetID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return false;

        if (!loadout.Gadgets.TryGetValue(gadgetID, out var playerGadget))
            return false;

        playerGadget.IsBought = isBought;
        return true;
    }

    public bool UpdatePlayerGadgetUnlocked(CSteamID steamID, ushort gadgetID, bool isUnlocked)
    {
        if (!Gadgets.TryGetValue(gadgetID, out var gadget))
            throw new ArgumentNullException("id", $"Gadget with id {gadgetID} doesn't exist in the database, while updating unlocked for {steamID}");

        AddQuery($"UPDATE `{PLAYERS_GADGETS}` SET `IsUnlocked` = {isUnlocked} WHERE `SteamID` = {steamID} AND `GadgetID` = {gadgetID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return false;

        if (!loadout.Gadgets.TryGetValue(gadgetID, out var playerGadget))
            return false;

        playerGadget.IsUnlocked = isUnlocked;
        return true;
    }

    // Player Killstreaks

    public void AddPlayerKillstreakBought(CSteamID steamID, int killstreakID)
    {
        if (!Killstreaks.TryGetValue(killstreakID, out var killstreak))
            throw new ArgumentNullException("id", $"Killstreak with id {killstreakID} doesn't exist in the database, while adding to {steamID}");

        AddQuery($"INSERT INTO `{PLAYERS_KILLSTREAKS}` (`SteamID` , `KillstreakID` , `KillstreakKills` , `IsBought`) VALUES ({steamID} , {killstreakID} , 0 , true) ON DUPLICATE KEY UPDATE `IsBought` = true;");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return;

        if (loadout.Killstreaks.TryGetValue(killstreakID, out var playerKillstreak))
        {
            playerKillstreak.IsBought = true;
            return;
        }

        loadout.Killstreaks.Add(killstreakID, new(killstreak, 0, true, false));
        Plugin.Instance.UI.OnUIUpdated(steamID, EUIPage.KILLSTREAK);
    }

    public void IncreasePlayerKillstreakKills(CSteamID steamID, int killstreakID, int amount)
    {
        if (!Killstreaks.TryGetValue(killstreakID, out var killstreak))
            throw new ArgumentNullException("id", $"Killstreak with id {killstreakID} doesn't exist in the database, while increasing kills for {steamID}");

        AddQuery($"UPDATE `{PLAYERS_KILLSTREAKS}` SET `KillstreakKills` = `KillstreakKills` + {amount} WHERE `SteamID` = {steamID} AND `KillstreakID` = {killstreakID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return;

        if (!loadout.Killstreaks.TryGetValue(killstreakID, out var playerKillstreak))
            return;

        playerKillstreak.KillstreakKills += amount;
    }

    public bool UpdatePlayerKillstreakBought(CSteamID steamID, int killstreakID, bool isBought)
    {
        if (!Killstreaks.TryGetValue(killstreakID, out var killstreak))
            throw new ArgumentNullException("id", $"Killstreak with id {killstreakID} doesn't exist in the database, while updating bought for {steamID}");

        AddQuery($"UPDATE `{PLAYERS_KILLSTREAKS}` SET `IsBought` = {isBought} WHERE `SteamID` = {steamID} AND `KillstreakID` = {killstreakID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return false;

        if (!loadout.Killstreaks.TryGetValue(killstreakID, out var playerKillstreak))
            return false;

        playerKillstreak.IsBought = isBought;
        return true;
    }

    public bool UpdatePlayerKillstreakUnlocked(CSteamID steamID, int killstreakID, bool isUnlocked)
    {
        if (!Killstreaks.TryGetValue(killstreakID, out var killstreak))
            throw new ArgumentNullException("id", $"Killstreak with id {killstreakID} doesn't exist in the database, while updating unlocked for {steamID}");

        AddQuery($"UPDATE `{PLAYERS_KILLSTREAKS}` SET `IsUnlocked` = {isUnlocked} WHERE `SteamID` = {steamID} AND `KillstreakID` = {killstreakID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return false;

        if (!loadout.Killstreaks.TryGetValue(killstreakID, out var playerKillstreak))
            return false;

        playerKillstreak.IsUnlocked = isUnlocked;
        return true;
    }

    // Player Cards

    public void AddPlayerCardBought(CSteamID steamID, int cardID)
    {
        if (!Cards.TryGetValue(cardID, out var card))
            throw new ArgumentNullException("id", $"Card with id {cardID} doesn't exist in the database, while adding to {steamID}");

        AddQuery($"INSERT INTO `{PLAYERS_CARDS}` (`SteamID` , `CardID` , `IsBought`) VALUES ({steamID} , {cardID} , true) ON DUPLICATE KEY UPDATE `IsBought` = true;");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return;

        if (loadout.Cards.TryGetValue(cardID, out var playerCard))
        {
            playerCard.IsBought = true;
            return;
        }

        loadout.Cards.Add(cardID, new(card, true, false));
        IncreasePlayerCardUnboxedAmount(cardID, 1);
        Plugin.Instance.UI.OnUIUpdated(steamID, EUIPage.CARD);
    }

    public void RemovePlayerCard(CSteamID steamID, int cardID)
    {
        if (!Cards.TryGetValue(cardID, out var card))
            throw new ArgumentNullException("id", $"Card with id {cardID} doesn't exist in the database, while removing from {steamID}");

        AddQuery($"DELETE FROM `{PLAYERS_CARDS}` WHERE `SteamID` = {steamID} AND `CardID` = {cardID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return;

        loadout.Cards.Remove(cardID);
    }

    public bool UpdatePlayerCardBought(CSteamID steamID, int cardID, bool isBought)
    {
        if (!Cards.TryGetValue(cardID, out var card))
            throw new ArgumentNullException("id", $"Card with id {cardID} doesn't exist in the database, while updating bought for {steamID}");

        AddQuery($"UPDATE `{PLAYERS_CARDS}` SET `IsBought` = {isBought} WHERE `SteamID` = {steamID} AND `CardID` = {cardID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return false;

        if (!loadout.Cards.TryGetValue(cardID, out var playerCard))
            return false;

        playerCard.IsBought = isBought;
        if (isBought)
            IncreasePlayerCardUnboxedAmount(cardID, 1);
        
        return true;
    }

    public bool UpdatePlayerCardUnlocked(CSteamID steamID, int cardID, bool isUnlocked)
    {
        if (!Cards.TryGetValue(cardID, out var card))
            throw new ArgumentNullException("id", $"Card with id {cardID} doesn't exist in the database, while updating unlocked for {steamID}");

        AddQuery($"UPDATE `{PLAYERS_CARDS}` SET `IsUnlocked` = {isUnlocked} WHERE `SteamID` = {steamID} AND `CardID` = {cardID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return false;

        if (!loadout.Cards.TryGetValue(cardID, out var playerCard))
            return false;

        playerCard.IsUnlocked = isUnlocked;
        return true;
    }

    public void IncreasePlayerCardUnboxedAmount(int id, int amount)
    {
        if (!Cards.TryGetValue(id, out var card))
            throw new ArgumentNullException(nameof(id), $"Card with id {id} doesn't exist in the database, while increasing unboxed amount");

        AddQuery($"UPDATE `{CARDS}` SET `UnboxedAmount` = `UnboxedAmount` + {amount} WHERE `CardID` = {id};");
        card.UnboxedAmount += amount;
    }

    // Player Gloves

    public void AddPlayerGloveBought(CSteamID steamID, int gloveID)
    {
        if (!Gloves.TryGetValue(gloveID, out var glove))
            throw new ArgumentNullException("id", $"Glove with id {gloveID} doesn't exist in the database, while adding to {steamID}");

        AddQuery($"INSERT INTO `{PLAYERS_GLOVES}` (`SteamID` , `GloveID` , `IsBought`) VALUES ({steamID} , {gloveID} , true) ON DUPLICATE KEY UPDATE `IsBought` = true;");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return;

        if (loadout.Gloves.TryGetValue(gloveID, out var playerGlove))
        {
            playerGlove.IsBought = true;
            return;
        }

        loadout.Gloves.Add(gloveID, new(glove, true, false));
        IncreasePlayerGloveUnboxedAmount(gloveID, 1);
        Plugin.Instance.UI.OnUIUpdated(steamID, EUIPage.GLOVE);
    }

    public bool UpdatePlayerGloveBought(CSteamID steamID, int gloveID, bool isBought)
    {
        if (!Gloves.TryGetValue(gloveID, out var glove))
            throw new ArgumentNullException("id", $"Glove with id {gloveID} doesn't exist in the database, while updating bought for {steamID}");

        AddQuery($"UPDATE `{PLAYERS_GLOVES}` SET `IsBought` = {isBought} WHERE `SteamID` = {steamID} AND `GloveID` = {gloveID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return false;

        if (!loadout.Gloves.TryGetValue(gloveID, out var playerGlove))
            return false;

        playerGlove.IsBought = isBought;
        if (isBought)
            IncreasePlayerGloveUnboxedAmount(gloveID, 1);
        
        return true;
    }

    public bool UpdatePlayerGloveUnlocked(CSteamID steamID, int gloveID, bool isUnlocked)
    {
        if (!Gloves.TryGetValue(gloveID, out var glove))
            throw new ArgumentNullException("id", $"Glove with id {gloveID} doesn't exist in the database, while updating unlocked for {steamID}");

        AddQuery($"UPDATE `{PLAYERS_GLOVES}` SET `IsUnlocked` = {isUnlocked} WHERE `SteamID` = {steamID} AND `GloveID` = {gloveID};");
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            return false;

        if (!loadout.Gloves.TryGetValue(gloveID, out var playerGlove))
            return false;

        playerGlove.IsUnlocked = isUnlocked;
        return true;
    }

    public void IncreasePlayerGloveUnboxedAmount(int gloveID, int amount)
    {
        // get glove from Gloves, throw an error if not
        if (!Gloves.TryGetValue(gloveID, out var glove))
            throw new ArgumentNullException("id", $"Glove with id {gloveID} doesn't exist in the database, while updating unboxed amount");

        AddQuery($"UPDATE `{GLOVES}` SET `UnboxedAmount` = `UnboxedAmount` + {amount} WHERE `GloveID` = {gloveID};");
        glove.UnboxedAmount += amount;
    }

    // Player Loadouts

    public void UpdatePlayerLoadout(CSteamID steamID, int loadoutID)
    {
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            throw new ArgumentNullException("steamID", $"Player with steamID {steamID} doesn't exist in the database, while updating loadout");

        if (!loadout.Loadouts.TryGetValue(loadoutID, out var playerLoadout))
            throw new ArgumentNullException("loadoutID", $"Loadout with id {loadoutID} doesn't exist in the database, while updating loadout for {steamID}");

        LoadoutData loadoutData = new(playerLoadout);
        AddQuery($"UPDATE `{PLAYERS_LOADOUTS}` SET `Loadout` = '{Plugin.Instance.Data.ConvertLoadoutToJson(loadoutData)}' WHERE `SteamID` = {steamID} AND `LoadoutID` = {loadoutID};");
    }

    public void UpdatePlayerLoadoutActive(CSteamID steamID, int loadoutID, bool isActive)
    {
        if (!PlayerLoadouts.TryGetValue(steamID, out var loadout))
            throw new ArgumentNullException("steamID", $"Player with steamID {steamID} doesn't exist in the database, while updating loadout");

        if (!loadout.Loadouts.TryGetValue(loadoutID, out var playerLoadout))
            throw new ArgumentNullException("loadoutID", $"Loadout with id {loadoutID} doesn't exist in the database, while updating loadout for {steamID}");

        AddQuery($"UPDATE `{PLAYERS_LOADOUTS}` SET `IsActive` = {isActive} WHERE `SteamID` = {steamID} AND `LoadoutID` = {loadoutID};");
        playerLoadout.IsActive = isActive;
    }

    // Player Quest

    public void IncreasePlayerQuestAmount(CSteamID steamID, int questID, int amount)
    {
        if (!QuestsSearchByID.ContainsKey(questID))
            throw new ArgumentNullException("id", $"Quest with id {questID} doesn't exist in the database, while updating amount for {steamID}");

        AddQuery($"UPDATE `{PLAYERS_QUESTS}` SET `Amount` = `Amount` + {amount} WHERE `SteamID` = {steamID} AND `QuestID` = {questID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        var quest = data.Quests.FirstOrDefault(k => k.Quest.QuestID == questID);
        if (quest == null)
            return;

        quest.Amount += amount;
    }

    // Player Achievement
    public void UpdatePlayerAchievementTier(CSteamID steamID, int achievementID, int currentTier)
    {
        AddQuery($"UPDATE `{PLAYERS_ACHIEVEMENTS}` SET `CurrentTier` = {currentTier} WHERE `SteamID` = {steamID} AND `AchievementID` = {achievementID};");
    }

    // increase player achievement amount
    public void IncreasePlayerAchievementAmount(CSteamID steamID, int achievementID, int amount)
    {
        AddQuery($"UPDATE `{PLAYERS_ACHIEVEMENTS}` SET `Amount` = `Amount` + {amount} WHERE `SteamID` = {steamID} AND `AchievementID` = {achievementID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        if (!data.AchievementsSearchByID.TryGetValue(achievementID, out var achievement))
            return;

        achievement.Amount += amount;
    }

    public async Task GenerateAchievementTiersAsync(int achievementID, string tierTitle)
    {
        using MySqlConnection conn = new(ConnectionString);
        try
        {
            await conn.OpenAsync();
            for (var i = 0; i <= 4; i++)
            {
                var targetAmount = 0;
                var color = "";

                switch (i)
                {
                    case 1:
                        targetAmount = 100;
                        color = "<color=#a8723d>Bronze</color>";
                        break;
                    case 2:
                        targetAmount = 250;
                        color = "<color=#bfbebd>Silver</color>";
                        break;
                    case 3:
                        targetAmount = 1000;
                        color = "<color=#e6de49>Gold</color>";
                        break;
                    case 4:
                        targetAmount = 2500;
                        color = "<color=#60f7cd>Diamond</color>";
                        break;
                }

                _ = await new MySqlCommand(
                    $"INSERT INTO `{ACHIEVEMENTS_TIERS}` (`AchievementID` , `TierID` , `TierTitle` , `TierDesc` , `TierColor` , `TierPrevSmall` , `TierPrevLarge` , `TargetAmount` , `Rewards` , `RemoveRewards`) VALUES ({achievementID} , {i} , '{tierTitle}' , ' ', '{color}' , ' ', ' ', {targetAmount} , ' ' , ' ' );",
                    conn).ExecuteScalarAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error generating 5 tiers for achievement with id {achievementID}");
            Logger.Log(ex);
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    // Player Battle-pass

    public void IncreasePlayerBPXP(CSteamID steamID, int xp)
    {
        AddQuery($"UPDATE `{PLAYERS_BATTLEPASS}` SET `XP` = `XP` + {xp} WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.Battlepass.XP += xp;
        var tierUp = false;
        var player = Plugin.Instance.Game.GetGamePlayer(data.SteamID);
        if (player == null)
            return;

        while (data.Battlepass.TryGetNeededXP(out var neededXP) && data.Battlepass.XP >= neededXP)
        {
            data.Battlepass.XP -= neededXP;
            data.Battlepass.CurrentTier++;
            tierUp = true;

            if (BattlepassTiersSearchByID.TryGetValue(data.Battlepass.CurrentTier, out var currentTier))
                Plugin.Instance.UI.SendAnimation(player, new(EAnimationType.BATTLEPASS_TIER_COMPLETION, currentTier));
        }

        if (tierUp)
            AddQuery($"UPDATE `{PLAYERS_BATTLEPASS}` SET `CurrentTier` = {data.Battlepass.CurrentTier}, `XP` = {data.Battlepass.XP} WHERE `SteamID` = {steamID};");
    }

    public void UpdatePlayerBPTier(CSteamID steamID, int tier)
    {
        AddQuery($"UPDATE `{PLAYERS_BATTLEPASS}` SET `CurrentTier` = {tier} WHERE `SteamID` = {steamID};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        data.Battlepass.CurrentTier = tier;
    }

    public void UpdatePlayerBPClaimedFreeRewards(CSteamID steamID)
    {
        if (!PlayerData.TryGetValue(steamID, out var data))
            throw new ArgumentNullException(nameof(steamID), $"Player with steamID {steamID} doesn't exist in the database, while updating BP claimed free rewards");

        AddQuery($"UPDATE `{PLAYERS_BATTLEPASS}` SET `ClaimedFreeRewards` = '{data.Battlepass.ClaimedFreeRewards.GetStringFromHashSetInt()}' WHERE `SteamID` = {steamID};");
    }

    public void UpdatePlayerBPClaimedPremiumRewards(CSteamID steamID)
    {
        if (!PlayerData.TryGetValue(steamID, out var data))
            throw new ArgumentNullException(nameof(steamID), $"Player with steamID {steamID} doesn't exist in the database, while updating BP claimed premium rewards");

        AddQuery($"UPDATE `{PLAYERS_BATTLEPASS}` SET `ClaimedPremiumRewards` = '{data.Battlepass.ClaimedPremiumRewards.GetStringFromHashSetInt()}' WHERE `SteamID` = {steamID};");
    }

    // Player Cases
    public void IncreasePlayerCase(CSteamID steamID, int caseID, int amount)
    {
        AddQuery($"INSERT INTO `{PLAYERS_CASES}` ( `SteamID` , `CaseID` , `Amount` ) VALUES ({steamID}, {caseID}, {amount}) ON DUPLICATE KEY UPDATE `Amount` = `Amount` + {amount};");
        if (!PlayerData.TryGetValue(steamID, out var data))
            return;

        if (!Cases.TryGetValue(caseID, out var @case))
            return;

        if (data.CasesSearchByID.TryGetValue(caseID, out var playerCase))
        {
            playerCase.Amount += amount;
            return;
        }

        playerCase = new(steamID, @case, amount);

        data.Cases.Add(playerCase);
        data.CasesSearchByID.Add(caseID, playerCase);
        Plugin.Instance.UI.OnUIUpdated(steamID, EUIPage.CASE);
    }

    public void DecreasePlayerCase(CSteamID steamID, int caseID, int amount)
    {
        if (!PlayerData.TryGetValue(steamID, out var data))
            throw new ArgumentNullException(nameof(steamID), $"Player with steamID {steamID} doesn't exist in the database, while decreasing case");

        if (!data.CasesSearchByID.TryGetValue(caseID, out var playerCase))
            throw new ArgumentNullException(nameof(caseID), $"Case with id {caseID} doesn't exist in the database, while decreasing case");

        playerCase.Amount -= amount;
        if (playerCase.Amount > 0)
        {
            AddQuery($"UPDATE `{PLAYERS_CASES}` SET `Amount` = `Amount` - {amount} WHERE `SteamID` = {steamID} AND `CaseID` = {caseID};");
        }
        else
        {
            _ = data.CasesSearchByID.Remove(caseID);
            _ = data.Cases.RemoveAll(k => k.Case.CaseID == caseID);
            Plugin.Instance.UI.OnUIUpdated(steamID, EUIPage.CASE);
            AddQuery($"DELETE FROM `{PLAYERS_CASES}` WHERE `SteamID` = {steamID} AND `CaseID` = {caseID};");
        }
    }

    public void IncreaseCaseUnboxedAmount(int caseID, int amount)
    {
        if (!Cases.TryGetValue(caseID, out var @case))
            throw new ArgumentNullException(nameof(caseID), $"Case with id {caseID} doesn't exist in the database, while increasing case unboxed amount");

        AddQuery($"UPDATE `{CASES}` SET `UnboxedAmount` = `UnboxedAmount` + {amount} WHERE `CaseID` = {caseID};");
        @case.UnboxedAmount += amount;
    }

    // Player Boosters
    public void AddPlayerBooster(CSteamID steamID, EBoosterType boosterType, float boosterValue, int days)
    {
        if (!PlayerData.TryGetValue(steamID, out var data))
            throw new ArgumentNullException(nameof(steamID), $"Player with steamID {steamID} doesn't exist in the database, while adding booster");

        var booster = data.ActiveBoosters.FirstOrDefault(k => k.BoosterType == boosterType && k.BoosterValue == boosterValue);
        if (booster != null)
        {
            booster.BoosterExpiration = booster.BoosterExpiration.AddDays(days);
            AddQuery($"UPDATE `{PLAYERS_BOOSTERS}` SET `BoosterExpiration` = {booster.BoosterExpiration.ToUnixTimeSeconds()} WHERE `SteamID` = {steamID} AND `BoosterType` = '{boosterType}' AND `BoosterValue` = {boosterValue};");
        }
        else
        {
            booster = new(steamID, boosterType, boosterValue, DateTimeOffset.UtcNow.AddDays(days));
            data.ActiveBoosters.Add(booster);
            AddQuery($"INSERT INTO `{PLAYERS_BOOSTERS}` (`SteamID` , `BoosterType` , `BoosterValue` , `BoosterExpiration`) VALUES ({steamID} , '{boosterType}' , {boosterValue} , {booster.BoosterExpiration.ToUnixTimeSeconds()});");
        }
    }
}