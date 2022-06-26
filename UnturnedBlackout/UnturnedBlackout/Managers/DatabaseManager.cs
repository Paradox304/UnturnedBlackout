﻿using MySql.Data.MySqlClient;
using Rocket.Core.Logging;
using Rocket.Core.Steam;
using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Models.Data;
using UnturnedBlackout.Models.Webhook;
using PlayerQuest = UnturnedBlackout.Database.Data.PlayerQuest;
using Timer = System.Timers.Timer;
using Achievement = UnturnedBlackout.Database.Base.Achievement;
using UnturnedBlackout.Models.Global;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace UnturnedBlackout.Managers
{
    public class DatabaseManager
    {
        public string ConnectionString { get; set; }
        public Config Config { get; set; }

        public Timer m_MuteChecker { get; set; }
        public Timer m_LeaderboardChecker { get; set; }

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
        public const string PlayersTableName = "UB_Players";
        public const string PlayersLeaderboardDailyTableName = "UB_Players_Leaderboard_Daily";
        public const string PlayersLeaderboardWeeklyTableName = "UB_Players_Leaderboard_Weekly";
        public const string PlayersLeaderboardSeasonalTableName = "UB_Players_Leaderboard_Seasonal";
        public const string PlayersLoadoutsTableName = "UB_Players_Loadouts";

        // GUNS
        public const string PlayersGunsTableName = "UB_Players_Guns";
        public const string PlayersGunsSkinsTableName = "UB_Players_Guns_Skins";
        public const string PlayersGunsCharmsTableName = "UB_Players_Guns_Charms";

        // KNIVES
        public const string PlayersKnivesTableName = "UB_Players_Knives";

        // PERKS
        public const string PlayersPerksTableName = "UB_Players_Perks";

        // GADGETS
        public const string PlayersGadgetsTableName = "UB_Players_Gadgets";

        // KILLSTREAKS
        public const string PlayersKillstreaksTableName = "UB_Players_Killstreaks";

        // CARDS
        public const string PlayersCardsTableName = "UB_Players_Cards";

        // GLOVES
        public const string PlayersGlovesTableName = "UB_Players_Gloves";

        // QUESTS
        public const string PlayersQuestsTableName = "UB_Players_Quests";

        // ACHIEVEMENTS
        public const string PlayersAchievementsTableName = "UB_Players_Achievements";

        // BATTLEPASS
        public const string PlayersBattlepassTableName = "UB_Players_Battlepass";

        // Base Data
        // GUNS
        public const string GunsTableName = "UB_Guns";
        public const string AttachmentsTableName = "UB_Guns_Attachments";
        public const string GunsSkinsTableName = "UB_Guns_Skins";
        public const string GunsCharmsTableName = "UB_Guns_Charms";

        // KNIVES
        public const string KnivesTableName = "UB_Knives";

        // PERKS
        public const string PerksTableName = "UB_Perks";

        // GADGETS
        public const string GadgetsTableName = "UB_Gadgets";

        // KILLSTREAKS
        public const string KillstreaksTableName = "UB_Killstreaks";

        // CARDS
        public const string CardsTableName = "UB_Cards";

        // GLOVES
        public const string GlovesTableName = "UB_Gloves";

        // LEVELS
        public const string LevelsTableName = "UB_Levels";

        // SERVER OPTIONS
        public const string OptionsTableName = "UB_Options";
        public const string ServersTableName = "UB_Servers";
        
        // Quests
        public const string QuestsTableName = "UB_Quests";

        // ACHIEVEMENTS
        public const string AchievementsTableName = "UB_Achievements";
        public const string AchievementsTiersTableName = "UB_Achievements_Tiers";

        // BATTLEPASS
        public const string BattlepassTableName = "UB_Battlepass";

        public DatabaseManager()
        {
            Config = Plugin.Instance.Configuration.Instance;
            ConnectionString = $"server={Config.DatabaseHost};user={Config.DatabaseUsername};database={Config.DatabaseName};port={Config.DatabasePort};password={Config.DatabasePassword}";
            m_MuteChecker = new Timer(10 * 1000);
            m_MuteChecker.Elapsed += CheckMutedPlayers;

            m_LeaderboardChecker = new Timer(60 * 1000);
            m_LeaderboardChecker.Elapsed += RefreshLeaderboardData;

            PlayerData = new Dictionary<CSteamID, PlayerData>();
            PlayerLoadouts = new Dictionary<CSteamID, PlayerLoadout>();

            IsPendingSeasonalWipe = false;
            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await LoadDatabaseAsync();
                await GetBaseDataAsync();

                Plugin.Instance.LoadoutManager = new LoadoutManager();
                Plugin.Instance.ServerManager = new ServerManager();
                m_MuteChecker.Start();
                RefreshLeaderboardData(null, null);
                m_LeaderboardChecker.Start();
            });
        }

        public async Task LoadDatabaseAsync()
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                // BASE DATA
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GunsTableName}` ( `GunID` SMALLINT UNSIGNED NOT NULL , `GunName` VARCHAR(255) NOT NULL , `GunDesc` TEXT NOT NULL , `GunType` ENUM('Pistol','SMG','Shotgun','LMG','AR','SNIPER','CARBINE') NOT NULL , `GunRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `MovementChange` DECIMAL(4,3) NOT NULL , `MovementChangeADS` DECIMAL(4,3) NOT NULL , `IconLink` TEXT NOT NULL , `MagAmount` TINYINT NOT NULL , `Coins` INT NOT NULL , `BuyPrice` INT NOT NULL ,  `ScrapAmount` INT NOT NULL , `LevelRequirement` INT NOT NULL , `IsPrimary` BOOLEAN NOT NULL , `DefaultAttachments` TEXT NOT NULL , `LevelXPNeeded` TEXT NOT NULL , `LevelRewards` TEXT NOT NULL , PRIMARY KEY (`GunID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{AttachmentsTableName}` ( `AttachmentID` SMALLINT UNSIGNED NOT NULL , `AttachmentName` VARCHAR(255) NOT NULL , `AttachmentDesc` TEXT NOT NULL , `AttachmentPros` TEXT NOT NULL , `AttachmentCons` TEXT NOT NULL , `AttachmentType` ENUM('Sights','Grip','Barrel','Magazine') NOT NULL , `AttachmentRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `MovementChange` DECIMAL(4,3) NOT NULL , `MovementChangeADS` DECIMAL (4,3) NOT NULL , `IconLink` TEXT NOT NULL , `BuyPrice` INT NOT NULL , `Coins` INT NOT NULL , PRIMARY KEY (`AttachmentID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GunsSkinsTableName}` ( `ID` INT NOT NULL AUTO_INCREMENT , `GunID` SMALLINT UNSIGNED NOT NULL , `SkinID` SMALLINT UNSIGNED NOT NULL , `SkinName` VARCHAR(255) NOT NULL , `SkinDesc` TEXT NOT NULL , `SkinRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `PatternLink` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT  NOT NULL , CONSTRAINT `ub_gun_id` FOREIGN KEY (`GunID`) REFERENCES `{GunsTableName}` (`GunID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`ID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GunsCharmsTableName}` ( `CharmID` SMALLINT UNSIGNED NOT NULL , `CharmName` VARCHAR(255) NOT NULL , `CharmDesc` TEXT NOT NULL , `CharmRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `IconLink` TEXT NOT NULL , `BuyPrice` INT NOT NULL , `Coins` INT NOT NULL , `ScrapAmount` INT  NOT NULL , `LevelRequirement` INT NOT NULL , PRIMARY KEY (`CharmID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{KnivesTableName}` ( `KnifeID` SMALLINT UNSIGNED NOT NULL , `KnifeName` VARCHAR(255) NOT NULL , `KnifeDesc` TEXT NOT NULL , `KnifeRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `MovementChange` DECIMAL(4,3) NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT NOT NULL , `Coins` INT NOT NULL , `BuyPrice` INT NOT NULL , `LevelRequirement` INT NOT NULL , PRIMARY KEY (`KnifeID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PerksTableName}` ( `PerkID` INT NOT NULL , `PerkName` VARCHAR(255) NOT NULL , `PerkDesc` TEXT NOT NULL , `PerkType` ENUM('1','2','3') NOT NULL , `PerkRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `IconLink` TEXT NOT NULL , `SkillType` TEXT NOT NULL , `SkillLevel` INT NOT NULL , `Coins` INT NOT NULL , `BuyPrice` INT NOT NULL , `ScrapAmount` INT  NOT NULL , `LevelRequirement` INT NOT NULL , PRIMARY KEY (`PerkID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GadgetsTableName}` ( `GadgetID` SMALLINT UNSIGNED NOT NULL , `GadgetName` VARCHAR(255) NOT NULL , `GadgetDesc` TEXT NOT NULL , `GadgetRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `IconLink` TEXT NOT NULL , `Coins` INT NOT NULL , `BuyPrice` INT NOT NULL , `ScrapAmount` INT NOT NULL , `GiveSeconds` INT  NOT NULL , `LevelRequirement` INT NOT NULL , `IsTactical` BOOLEAN NOT NULL , PRIMARY KEY (`GadgetID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{KillstreaksTableName}` ( `KillstreakID` INT NOT NULL , `KillstreakName` VARCHAR(255) NOT NULL , `KillstreakDesc` TEXT NOT NULL , `KillstreakRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `IconLink` TEXT NOT NULL , `KillstreakRequired` INT NOT NULL , `BuyPrice` INT NOT NULL , `Coins` INT  NOT NULL , `ScrapAmount` INT NOT NULL , `LevelRequirement` INT NOT NULL , PRIMARY KEY (`KillstreakID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{CardsTableName}` ( `CardID` INT NOT NULL , `CardName` VARCHAR(255) NOT NULL , `CardDesc` TEXT NOT NULL , `CardRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `IconLink` TEXT NOT NULL , `CardLink` TEXT NOT NULL , `ScrapAmount` INT NOT NULL , `BuyPrice` INT NOT NULL , `Coins` INT NOT NULL , `LevelRequirement` INT NOT NULL , PRIMARY KEY (`CardID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GlovesTableName}` ( `GloveID` INT NOT NULL , `GloveName` VARCHAR(255) NOT NULL , `GloveDesc` TEXT NOT NULL , `GloveRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT NOT NULL , `BuyPrice` INT NOT NULL , `Coins` INT NOT NULL , `LevelRequirement` INT NOT NULL , PRIMARY KEY (`GloveID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{LevelsTableName}` ( `Level` INT NOT NULL , `XPNeeded` INT NOT NULL , `IconLinkLarge` TEXT NOT NULL , `IconLinkMedium` TEXT NOT NULL , `IconLinkSmall` TEXT NOT NULL , PRIMARY KEY (`Level`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{OptionsTableName}` ( `DailyLeaderboardWipe` BIGINT NOT NULL , `WeeklyLeaderboardWipe` BIGINT NOT NULL , `DailyLeaderboardRankedRewards` TEXT NOT NULL , `DailyLeaderboardPercentileRewards` TEXT NOT NULL , `WeeklyLeaderboardRankedRewards` TEXT NOT NULL , `WeeklyLeaderboardPercentileRewards` TEXT NOT NULL, `SeasonalLeaderboardRankedRewards` TEXT NOT NULL , `SeasonalLeaderboardPercentileRewards` TEXT NOT NULL , `XPBooster` DECIMAL(6,3) NOT NULL , `BPBooster` DECIMAL(6,3) NOT NULL , `XPBoosterWipe` BIGINT NOT NULL , `BPBoosterWipe` BIGINT NOT NULL);", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{ServersTableName}`  ( `IP` TEXT NOT NULL , `Port` TEXT NOT NULL , `ServerName` TEXT NOT NULL , `FriendlyIP` TEXT NOT NULL , `ServerBanner` TEXT NOT NULL , `ServerDesc` TEXT NOT NULL );", Conn).ExecuteScalarAsync(); ;
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{QuestsTableName}` ( `QuestID` INT NOT NULL AUTO_INCREMENT , `QuestTitle` TEXT NOT NULL , `QuestDesc` TEXT NOT NULL , QuestType ENUM('Kill', 'Death', 'Win', 'MultiKill', 'Killstreak', 'Headshots', 'GadgetsUsed', 'FlagsCaptured', 'FlagsSaved', 'Dogtags', 'Shutdown', 'Domination', 'FlagKiller', 'FlagDenied', 'Revenge', 'FirstKill', 'Longshot', 'Survivor', 'Collector') NOT NULL , `QuestTier` ENUM('Easy1', 'Easy2', 'Easy3', 'Medium1', 'Medium2', 'Hard1') NOT NULL , `QuestConditions` TEXT NOT NULL , `TargetAmount` INT NOT NULL , `XP` INT NOT NULL , PRIMARY KEY (`QuestID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{AchievementsTableName}` ( `AchievementID` INT NOT NULL AUTO_INCREMENT , `AchievementType` ENUM('Kill', 'Death', 'Win', 'MultiKill', 'Killstreak', 'Headshots', 'GadgetsUsed', 'FlagsCaptured', 'FlagsSaved', 'Dogtags', 'Shutdown', 'Domination', 'FlagKiller', 'FlagDenied', 'Revenge', 'FirstKill', 'Longshot', 'Survivor', 'Collector') NOT NULL , `AchievementConditions` TEXT NOT NULL , `PageID` INT NOT NULL , PRIMARY KEY (`AchievementID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{AchievementsTiersTableName}` ( `AchievementID` INT NOT NULL , `TierID` INT NOT NULL , `TierTitle` TEXT NOT NULL , `TierDesc` TEXT NOT NULL , `TierPrevSmall` TEXT NOT NULL , `TierPrevLarge` TEXT NOT NULL , `TargetAmount` INT NOT NULL , `Rewards` TEXT NOT NULL , `RemoveRewards` TEXT NOT NULL , CONSTRAINT `ub_achievement_id` FOREIGN KEY (`AchievementID`) REFERENCES `{AchievementsTableName}` (`AchievementID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`AchievementID`, `TierID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{BattlepassTableName}` ( `TierID` INT NOT NULL , `FreeReward` TEXT NOT NULL , `FreeRewardIcon` TEXT NOT NULL , `PremiumReward` TEXT NOT NULL , `PremiumRewardIcon` TEXT NOT NULL , `XP` INT NOT NULL , PRIMARY KEY (`TierID`));", Conn).ExecuteScalarAsync();

                // PLAYERS DATA
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `SteamName` TEXT NOT NULL , `AvatarLink` VARCHAR(200) NOT NULL , `XP` INT NOT NULL DEFAULT '0' , `Level` INT NOT NULL DEFAULT '1' , `Credits` INT NOT NULL DEFAULT '0' , `Scrap` INT NOT NULL DEFAULT '0' , `Coins` INT NOT NULL DEFAULT '0' , `Kills` INT NOT NULL DEFAULT '0' , `HeadshotKills` INT NOT NULL DEFAULT '0' , `HighestKillstreak` INT NOT NULL DEFAULT '0' , `HighestMultiKills` INT NOT NULL DEFAULT '0' , `KillsConfirmed` INT NOT NULL DEFAULT '0' , `KillsDenied` INT NOT NULL DEFAULT '0' , `FlagsCaptured` INT NOT NULL DEFAULT '0' , `FlagsSaved` INT NOT NULL DEFAULT '0' , `AreasTaken` INT NOT NULL DEFAULT '0' , `Deaths` INT NOT NULL DEFAULT '0' , `Music` BOOLEAN NOT NULL DEFAULT TRUE , `IsMuted` BOOLEAN NOT NULL DEFAULT FALSE , `MuteExpiry` BIGINT NOT NULL , `HasBattlepass` BOOLEAN NOT NULL DEFAULT FALSE , `XPBooster` DECIMAL(6,3) NOT NULL DEFAULT '0' , `BPBooster` DECIMAL(6,3) NOT NULL DEFAULT '0' , PRIMARY KEY (`SteamID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersLeaderboardDailyTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `Kills` INT NOT NULL DEFAULT '0' , `HeadshotKills` INT NOT NULL DEFAULT '0' , `Deaths` INT NOT NULL DEFAULT '0' , CONSTRAINT `ub_steam_id_11` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersLeaderboardWeeklyTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `Kills` INT NOT NULL DEFAULT '0' , `HeadshotKills` INT NOT NULL DEFAULT '0' , `Deaths` INT NOT NULL DEFAULT '0' , CONSTRAINT `ub_steam_id_12` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersLeaderboardSeasonalTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `Kills` INT NOT NULL DEFAULT '0' , `HeadshotKills` INT NOT NULL DEFAULT '0' , `Deaths` INT NOT NULL DEFAULT '0' , CONSTRAINT `ub_steam_id_13` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersGunsTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `GunID` SMALLINT UNSIGNED NOT NULL , `Level` INT NOT NULL , `XP` INT NOT NULL , `GunKills` INT NOT NULL , `IsBought` BOOLEAN NOT NULL , `Attachments` TEXT NOT NULL , CONSTRAINT `ub_steam_id` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_gun_id_1` FOREIGN KEY (`GunID`) REFERENCES `{GunsTableName}` (`GunID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `GunID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersGunsSkinsTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `SkinIDs` TEXT NOT NULL , CONSTRAINT `ub_steam_id_1` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersGunsCharmsTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `CharmID` SMALLINT UNSIGNED NOT NULL , `IsBought` BOOLEAN NOT NULL , CONSTRAINT `ub_steam_id_10` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_charm_id` FOREIGN KEY (`CharmID`) REFERENCES `{GunsCharmsTableName}` (`CharmID`) ON DELETE CASCADE ON UPDATE CASCADE , Primary Key (`SteamID`, `CharmID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersKnivesTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `KnifeID` SMALLINT UNSIGNED NOT NULL , `KnifeKills` INT NOT NULL , `IsBought` BOOLEAN NOT NULL , CONSTRAINT `ub_steam_id_2` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_knife_id` FOREIGN KEY (`KnifeID`) REFERENCES `{KnivesTableName}` (`KnifeID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `KnifeID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersPerksTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `PerkID` INT NOT NULL , `IsBought` BOOLEAN NOT NULL , CONSTRAINT `ub_steam_id_4` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_perk_id` FOREIGN KEY (`PerkID`) REFERENCES `{PerksTableName}` (`PerkID`) ON DELETE CASCADE ON UPDATE CASCADE, PRIMARY KEY (`SteamID` , `PerkID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersGadgetsTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `GadgetID` SMALLINT UNSIGNED NOT NULL , `GadgetKills` INT NOT NULL , `IsBought` BOOLEAN NOT NULL , CONSTRAINT `ub_steam_id_5` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_gadget_id` FOREIGN KEY (`GadgetID`) REFERENCES `{GadgetsTableName}` (`GadgetID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `GadgetID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersKillstreaksTableName}` (`SteamID` BIGINT UNSIGNED NOT NULL , `KillstreakID` INT NOT NULl , `KillstreakKills` INT NOT NULL , `IsBought` BOOLEAN NOT NULL , CONSTRAINT `ub_steam_id_6` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_killstreak_id` FOREIGN KEY (`KillstreakID`) REFERENCES `{KillstreaksTableName}` (`KillstreakID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `KillstreakID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersCardsTableName}` (`SteamID` BIGINT UNSIGNED NOT NULL , `CardID` INT NOT NULL , `IsBought` BOOLEAN NOT NULL , CONSTRAINT `ub_steam_id_7` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_card_id` FOREIGN KEY (`CardID`) REFERENCES `{CardsTableName}` (`CardID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `CardID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersGlovesTableName}` (`SteamID` BIGINT UNSIGNED NOT NULL , `GloveID` INT NOT NULL , `IsBought` BOOLEAN NOT NULl , CONSTRAINT `ub_steam_id_8` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_glove_id` FOREIGN KEY (`GloveID`) REFERENCES `{GlovesTableName}` (`GloveID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `GloveID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersLoadoutsTableName}` (`SteamID` BIGINT UNSIGNED NOT NULL , `LoadoutID` INT NOT NULL , `IsActive` BOOLEAN NOT NULL , `Loadout` TEXT NOT NULL , CONSTRAINT `ub_steam_id_9` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID`, `LoadoutID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersQuestsTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `QuestID` INT NOT NULL , `Amount` INT NOT NULL , `QuestEnd` BIGINT NOT NULL , CONSTRAINT `ub_steam_id_14` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_quest_id` FOREIGN KEY (`QuestID`) REFERENCES `{QuestsTableName}` (`QuestID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `QuestID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersAchievementsTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `AchievementID` INT NOT NULL , `CurrentTier` INT NOT NULL DEFAULT '0' , `Amount` INT NOT NULL DEFAULT '0' , CONSTRAINT `ub_steam_id_15` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_achievement_id_2` FOREIGN KEY (`AchievementID`) REFERENCES `{AchievementsTableName}` (`AchievementID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID`, `AchievementID`));", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersBattlepassTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `CurrentTier` INT NOT NULL DEFAULT '1' , `XP` INT NOT NULL DEFAULT '0', `ClaimedFreeRewards` TEXT NOT NULL DEFAULT ' ' , `ClaimedPremiumRewards` TEXT NOT NULL DEFAULT ' ', CONSTRAINT `ub_steam_16` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID`));", Conn).ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                Logger.Log("Error loading database");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task GetBaseDataAsync()
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                Logging.Debug("Getting base data");
                Logging.Debug("Reading attachments from the base data");
                var defaultGuns = new List<Gun>();
                var defaultKnives = new List<Knife>();
                var defaultGadgets = new List<Gadget>();
                var defaultKillstreaks = new List<Killstreak>();
                var defaultPerks = new List<Perk>();
                var defaultGloves = new List<Glove>();
                var defaultCards = new List<Card>();

                var rdr = (MySqlDataReader)await new MySqlCommand($"SELECT `AttachmentID`, `AttachmentName`, `AttachmentDesc`, `AttachmentPros` , `AttachmentCons` , `AttachmentType`-1, `AttachmentRarity`, `MovementChange`, `MovementChangeADS`, `IconLink`, `BuyPrice`, `Coins` FROM `{AttachmentsTableName}`;", Conn).ExecuteReaderAsync();
                try
                {
                    var gunAttachments = new Dictionary<ushort, GunAttachment>();
                    while (await rdr.ReadAsync())
                    {
                        if (!ushort.TryParse(rdr[0].ToString(), out ushort attachmentID))
                        {
                            continue;
                        }

                        var attachmentName = rdr[1].ToString();
                        var attachmentDesc = rdr[2].ToString();
                        var attachmentPros = rdr[3].ToString().Split(',').Where(k => !string.IsNullOrEmpty(k)).ToList();
                        var attachmentCons = rdr[4].ToString().Split(',').Where(k => !string.IsNullOrEmpty(k)).ToList();
                        if (!int.TryParse(rdr[5].ToString(), out int attachmentTypeInt))
                        {
                            continue;
                        }

                        var attachmentType = (EAttachment)attachmentTypeInt;
                        var rarity = rdr[6].ToString();
                        if (!float.TryParse(rdr[7].ToString(), out float movementChange))
                        {
                            continue;
                        }

                        if (!float.TryParse(rdr[8].ToString(), out float movementChangeADS))
                        {
                            continue;
                        }

                        var iconLink = rdr[9].ToString();
                        if (!int.TryParse(rdr[10].ToString(), out int buyPrice))
                        {
                            continue;
                        }
                        if (!int.TryParse(rdr[11].ToString(), out int coins))
                        {
                            continue;
                        }

                        if (!gunAttachments.ContainsKey(attachmentID))
                        {
                            gunAttachments.Add(attachmentID, new GunAttachment(attachmentID, attachmentName, attachmentDesc, attachmentPros, attachmentCons, attachmentType, rarity, movementChange, movementChangeADS, iconLink, buyPrice, coins));
                        }
                        else
                        {
                            Logging.Debug($"Found a duplicate attachment with id {attachmentID}, ignoring this");
                        }
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT `GunID`, `GunName`, `GunDesc`, `GunType`-1, `GunRarity`, `MovementChange`, `MovementChangeADS`, `IconLink`, `MagAmount`, `Coins`, `BuyPrice`, `ScrapAmount`, `LevelRequirement`, `IsPrimary`, `DefaultAttachments`, `LevelXPNeeded`, `LevelRewards` FROM `{GunsTableName}`;", Conn).ExecuteReaderAsync();
                try
                {
                    var guns = new Dictionary<ushort, Gun>();
                    while (await rdr.ReadAsync())
                    {
                        if (!ushort.TryParse(rdr[0].ToString(), out ushort gunID))
                        {
                            continue;
                        }

                        var gunName = rdr[1].ToString();
                        var gunDesc = rdr[2].ToString();
                        if (!byte.TryParse(rdr[3].ToString(), out byte gunTypeInt))
                        {
                            continue;
                        }

                        var gunType = (EGun)gunTypeInt;
                        var rarity = rdr[4].ToString();
                        if (!float.TryParse(rdr[5].ToString(), out float movementChange))
                        {
                            continue;
                        }

                        if (!float.TryParse(rdr[6].ToString(), out float movementChangeADS))
                        {
                            continue;
                        }

                        var iconLink = rdr[7].ToString();
                        if (!int.TryParse(rdr[8].ToString(), out int magAmount))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[9].ToString(), out int coins))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[10].ToString(), out int buyPrice))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[11].ToString(), out int scrapAmount))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[12].ToString(), out int levelRequirement))
                        {
                            continue;
                        }

                        if (!bool.TryParse(rdr[13].ToString(), out bool isPrimary))
                        {
                            continue;
                        }

                        var defaultAttachments = new List<GunAttachment>();
                        foreach (var id in rdr[14].GetIntListFromReaderResult())
                        {
                            if (GunAttachments.TryGetValue((ushort)id, out GunAttachment gunAttachment))
                            {
                                defaultAttachments.Add(gunAttachment);
                            }
                            else
                            {
                                if (id != 0)
                                    Logging.Debug($"Could'nt find default attachment with id {id} for gun {gunID} with name {gunName}");
                            }
                        }

                        var levelXPNeeded = rdr[15].GetIntListFromReaderResult();
                        var levelRewards = rdr[16].GetIntListFromReaderResult();
                        var rewardAttachments = new Dictionary<int, GunAttachment>();
                        var rewardAttachmentsInverse = new Dictionary<GunAttachment, int>();
                        foreach (var id in levelRewards)
                        {
                            if (GunAttachments.TryGetValue((ushort)id, out GunAttachment gunAttachment))
                            {
                                if (!rewardAttachments.ContainsKey(levelRewards.IndexOf(id) + 2))
                                {
                                    rewardAttachments.Add(levelRewards.IndexOf(id) + 2, gunAttachment);
                                }
                                else
                                {
                                    Logging.Debug($"Duplicate reward attachment with id {id} found for gun {gunID} with name {gunName}");
                                }

                                if (!rewardAttachmentsInverse.ContainsKey(gunAttachment))
                                {
                                    rewardAttachmentsInverse.Add(gunAttachment, levelRewards.IndexOf(id) + 2);
                                }
                                else
                                {
                                    Logging.Debug($"Duplicate reward attachment inverse with id {id} found for gun {gunID} with name {gunName}");
                                }
                            }
                            else
                            {
                                if (id != 0)
                                    Logging.Debug($"Could'nt find reward attachment with id {id} for gun {gunID} with name {gunName}");
                            }
                        }

                        if (Assets.find(EAssetType.ITEM, gunID) is not ItemGunAsset gunAsset)
                        {
                            Logging.Debug($"Error finding gun asset of the gun with id {gunID} and name {gunName} ignoring the gun");
                            continue;
                        }

                        Logging.Debug($"Gun: {gunName}, ID: {gunID}, DamageFalloffRange: {gunAsset.damageFalloffRange}");
                        var longshotRange = Mathf.Pow(gunAsset.damageFalloffRange * 100, 2);
                        Logging.Debug($"Converted longshot range {longshotRange}");
                        var gun = new Gun(gunID, gunName, gunDesc, gunType, rarity, movementChange, movementChangeADS, iconLink, magAmount, coins, buyPrice, scrapAmount, levelRequirement, isPrimary, defaultAttachments, rewardAttachments, rewardAttachmentsInverse, levelXPNeeded, longshotRange);
                        if (!guns.ContainsKey(gunID))
                        {
                            guns.Add(gunID, gun);
                        }
                        else
                        {
                            Logging.Debug($"Found a duplicate with id {gunID}, ignoring this");
                        }

                        if (levelRequirement == 0)
                        {
                            defaultGuns.Add(gun);
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{GunsSkinsTableName}`;", Conn).ExecuteReaderAsync();
                try
                {
                    var gunSkinsSearchByID = new Dictionary<int, GunSkin>();
                    var gunSkinsSearchByGunID = new Dictionary<ushort, List<GunSkin>>();
                    var gunSkinsSearchBySkinID = new Dictionary<ushort, GunSkin>();

                    while (await rdr.ReadAsync())
                    {
                        if (!int.TryParse(rdr[0].ToString(), out int id))
                        {
                            continue;
                        }

                        if (!ushort.TryParse(rdr[1].ToString(), out ushort gunID))
                        {
                            continue;
                        }

                        if (!Guns.TryGetValue(gunID, out Gun gun))
                        {
                            Logging.Debug($"Could'nt find gun id with {gunID} for skin with id {id}");
                            continue;
                        }
                        if (!ushort.TryParse(rdr[2].ToString(), out ushort skinID))
                        {
                            continue;
                        }

                        var skinName = rdr[3].ToString();
                        var skinDesc = rdr[4].ToString();
                        var rarity = rdr[5].ToString();
                        var patternLink = rdr[6].ToString();
                        var iconLink = rdr[7].ToString();
                        if (!int.TryParse(rdr[8].ToString(), out int scrapAmount))
                        {
                            continue;
                        }

                        var skin = new GunSkin(id, gun, skinID, skinName, skinDesc, rarity, patternLink, iconLink, scrapAmount);
                        if (gunSkinsSearchByID.ContainsKey(id))
                        {
                            Logging.Debug($"Found a duplicate skin with id {id}, ignoring this");
                            continue;
                        }
                        else
                        {
                            gunSkinsSearchByID.Add(id, skin);
                        }

                        if (gunSkinsSearchByGunID.TryGetValue(gunID, out List<GunSkin> skins))
                        {
                            if (skins.Exists(k => k.ID == id))
                            {
                                Logging.Debug($"Found a duplicate skin with id {id}, ignoring this");
                                continue;
                            }
                            else
                            {
                                skins.Add(skin);
                            }
                        }
                        else
                        {
                            gunSkinsSearchByGunID.Add(gunID, new List<GunSkin> { skin });
                        }

                        if (gunSkinsSearchBySkinID.ContainsKey(skinID))
                        {
                            Logging.Debug($"Found a duplicate skin with id {id}, ignoring this");
                            continue;
                        }
                        else
                        {
                            gunSkinsSearchBySkinID.Add(skinID, skin);
                        }
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{GunsCharmsTableName}`;", Conn).ExecuteReaderAsync();
                try
                {
                    var gunCharms = new Dictionary<ushort, GunCharm>();
                    while (await rdr.ReadAsync())
                    {
                        if (!ushort.TryParse(rdr[0].ToString(), out ushort charmID))
                        {
                            continue;
                        }

                        var charmName = rdr[1].ToString();
                        var charmDesc = rdr[2].ToString();
                        var rarity = rdr[3].ToString();
                        var iconLink = rdr[4].ToString();
                        if (!int.TryParse(rdr[5].ToString(), out int buyPrice))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[6].ToString(), out int coins))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[7].ToString(), out int scrapAmount))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[8].ToString(), out int levelRequirement))
                        {
                            continue;
                        }

                        if (gunCharms.ContainsKey(charmID))
                        {
                            Logging.Debug($"Found a duplicate charm with id {charmID} registered");
                            continue;
                        }
                        gunCharms.Add(charmID, new GunCharm(charmID, charmName, charmDesc, rarity, iconLink, buyPrice, coins, scrapAmount, levelRequirement));
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{KnivesTableName}`;", Conn).ExecuteReaderAsync();
                try
                {
                    var knives = new Dictionary<ushort, Knife>();
                    while (await rdr.ReadAsync())
                    {
                        if (!ushort.TryParse(rdr[0].ToString(), out ushort knifeID))
                        {
                            continue;
                        }

                        var knifeName = rdr[1].ToString();
                        var knifeDesc = rdr[2].ToString();
                        var rarity = rdr[3].ToString();
                        if (!float.TryParse(rdr[4].ToString(), out float movementChange))
                        {
                            continue;
                        }

                        var iconLink = rdr[5].ToString();
                        if (!int.TryParse(rdr[6].ToString(), out int scrapAmount))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[7].ToString(), out int coins))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[8].ToString(), out int buyPrice))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[9].ToString(), out int levelRequirement))
                        {
                            continue;
                        }

                        var knife = new Knife(knifeID, knifeName, knifeDesc, rarity, movementChange, iconLink, scrapAmount, coins, buyPrice, levelRequirement);
                        if (!knives.ContainsKey(knifeID))
                        {
                            knives.Add(knifeID, knife);
                        }
                        else
                        {
                            Logging.Debug($"Found a duplicate knife with id {knifeID}, ignoring this");
                        }
                        if (levelRequirement == 0)
                        {
                            defaultKnives.Add(knife);
                        }
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{GadgetsTableName}`;", Conn).ExecuteReaderAsync();
                try
                {
                    var gadgets = new Dictionary<ushort, Gadget>();
                    while (await rdr.ReadAsync())
                    {
                        if (!ushort.TryParse(rdr[0].ToString(), out ushort gadgetID))
                        {
                            continue;
                        }

                        var gadgetName = rdr[1].ToString();
                        var gadgetDesc = rdr[2].ToString();
                        var rarity = rdr[3].ToString();
                        var iconLink = rdr[4].ToString();
                        if (!int.TryParse(rdr[5].ToString(), out int coins))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[6].ToString(), out int buyPrice))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[7].ToString(), out int scrapAmount))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[8].ToString(), out int giveSeconds))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[9].ToString(), out int levelRequirement))
                        {
                            continue;
                        }

                        if (!bool.TryParse(rdr[10].ToString(), out bool isTactical))
                        {
                            continue;
                        }

                        var gadget = new Gadget(gadgetID, gadgetName, gadgetDesc, rarity, iconLink, coins, buyPrice, scrapAmount, giveSeconds, levelRequirement, isTactical);
                        if (!gadgets.ContainsKey(gadgetID))
                        {
                            gadgets.Add(gadgetID, gadget);
                        }
                        else
                        {
                            Logging.Debug($"Found a duplicate gadget with id {gadgetID}, ignoring this");
                        }
                        if (levelRequirement == 0)
                        {
                            defaultGadgets.Add(gadget);
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{KillstreaksTableName}`;", Conn).ExecuteReaderAsync();
                try
                {
                    var killstreaks = new Dictionary<int, Killstreak>();
                    while (await rdr.ReadAsync())
                    {
                        if (!int.TryParse(rdr[0].ToString(), out int killstreakID))
                        {
                            continue;
                        }

                        var killstreakName = rdr[1].ToString();
                        var killstreakDesc = rdr[2].ToString();
                        var rarity = rdr[3].ToString();
                        var iconLink = rdr[4].ToString();
                        if (!int.TryParse(rdr[5].ToString(), out int killstreakRequired))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[6].ToString(), out int buyPrice))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[7].ToString(), out int coins))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[8].ToString(), out int scrapAmount))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[9].ToString(), out int levelRequirement))
                        {
                            continue;
                        }

                        var killstreak = new Killstreak(killstreakID, killstreakName, killstreakDesc, rarity, iconLink, killstreakRequired, buyPrice, coins, scrapAmount, levelRequirement);
                        if (!killstreaks.ContainsKey(killstreakID))
                        {
                            killstreaks.Add(killstreakID, killstreak);
                        }
                        else
                        {
                            Logging.Debug($"Found a duplicate killstrea with id {killstreakID}, ignoring it");
                        }
                        if (levelRequirement == 0)
                        {
                            defaultKillstreaks.Add(killstreak);
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PerksTableName}`;", Conn).ExecuteReaderAsync();
                try
                {
                    var perks = new Dictionary<int, Perk>();
                    while (await rdr.ReadAsync())
                    {
                        if (!int.TryParse(rdr[0].ToString(), out int perkID))
                        {
                            continue;
                        }

                        var perkName = rdr[1].ToString();
                        var perkDesc = rdr[2].ToString();
                        if (!int.TryParse(rdr[3].ToString(), out int perkType))
                        {
                            continue;
                        }

                        var rarity = rdr[4].ToString();
                        var iconLink = rdr[5].ToString();
                        var skillType = rdr[6].ToString();
                        if (!int.TryParse(rdr[7].ToString(), out int skillLevel))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[8].ToString(), out int coins))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[9].ToString(), out int buyPrice))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[10].ToString(), out int scrapAmount))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[11].ToString(), out int levelRequirement))
                        {
                            continue;
                        }

                        var perk = new Perk(perkID, perkName, perkDesc, perkType, rarity, iconLink, skillType, skillLevel, coins, buyPrice, scrapAmount, levelRequirement);
                        if (!perks.ContainsKey(perkID))
                        {
                            perks.Add(perkID, perk);
                        }
                        else
                        {
                            Logging.Debug($"Found a duplicate perk with id {perkID}, ignoring this");
                        }
                        if (levelRequirement == 0)
                        {
                            defaultPerks.Add(perk);
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{GlovesTableName}`;", Conn).ExecuteReaderAsync();
                try
                {
                    var gloves = new Dictionary<int, Glove>();
                    while (await rdr.ReadAsync())
                    {
                        if (!int.TryParse(rdr[0].ToString(), out int gloveID))
                        {
                            continue;
                        }

                        var gloveName = rdr[1].ToString();
                        var gloveDesc = rdr[2].ToString();
                        var rarity = rdr[3].ToString();
                        var iconLink = rdr[4].ToString();
                        if (!int.TryParse(rdr[5].ToString(), out int scrapAmount))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[6].ToString(), out int buyPrice))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[7].ToString(), out int coins))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[8].ToString(), out int levelRequirement))
                        {
                            continue;
                        }

                        var glove = new Glove(gloveID, gloveName, gloveDesc, rarity, iconLink, scrapAmount, buyPrice, coins, levelRequirement);
                        if (!gloves.ContainsKey(gloveID))
                        {
                            gloves.Add(gloveID, glove);
                        }
                        else
                        {
                            Logging.Debug($"Found a duplicate glove with id {gloveID}");
                        }
                        if (levelRequirement == 0)
                        {
                            defaultGloves.Add(glove);
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{CardsTableName}`;", Conn).ExecuteReaderAsync();
                try
                {
                    var cards = new Dictionary<int, Card>();
                    while (await rdr.ReadAsync())
                    {
                        if (!int.TryParse(rdr[0].ToString(), out int cardID))
                        {
                            continue;
                        }

                        var cardName = rdr[1].ToString();
                        var cardDesc = rdr[2].ToString();
                        var rarity = rdr[3].ToString();
                        var iconLink = rdr[4].ToString();
                        var cardLink = rdr[5].ToString();
                        if (!int.TryParse(rdr[6].ToString(), out int scrapAmount))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[7].ToString(), out int buyPrice))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[8].ToString(), out int coins))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[9].ToString(), out int levelRequirement))
                        {
                            continue;
                        }

                        var card = new Card(cardID, cardName, cardDesc, rarity, iconLink, cardLink, scrapAmount, buyPrice, coins, levelRequirement);
                        if (!cards.ContainsKey(cardID))
                        {
                            cards.Add(cardID, card);
                        }
                        else
                        {
                            Logging.Debug($"Found a duplicate card with id {cardID}, ignoring this");
                        }
                        if (levelRequirement == 0)
                        {
                            defaultCards.Add(card);
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

                Logging.Debug("Reading levels from base data");
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{LevelsTableName}`;", Conn).ExecuteReaderAsync();
                try
                {
                    var levels = new Dictionary<int, XPLevel>();
                    while (await rdr.ReadAsync())
                    {
                        if (!int.TryParse(rdr[0].ToString(), out int level))
                        {
                            continue;
                        }
                        if (!int.TryParse(rdr[1].ToString(), out int xpNeeded))
                        {
                            continue;
                        }
                        var iconLinkLarge = rdr[2].ToString();
                        var iconLinkMedium = rdr[3].ToString();
                        var iconLinkSmall = rdr[4].ToString();

                        if (!levels.ContainsKey(level))
                        {
                            levels.Add(level, new XPLevel(level, xpNeeded, iconLinkLarge, iconLinkMedium, iconLinkSmall));
                        }
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT `QuestID`, `QuestTitle`, `QuestDesc`, `QuestType`-1, `QuestTier`-1, `QuestConditions`, `TargetAmount`, `XP` FROM `{QuestsTableName}`;", Conn).ExecuteReaderAsync();
                try
                {
                    var questsSearchByID = new Dictionary<int, Quest>();
                    var quests = new List<Quest>();

                    while (await rdr.ReadAsync())
                    {
                        if (!int.TryParse(rdr[0].ToString(), out int questID))
                        {
                            continue;
                        }

                        var questTitle = rdr[1].ToString();
                        var questDesc = rdr[2].ToString();

                        if (!int.TryParse(rdr[3].ToString(), out int questTypeInt))
                        {
                            continue;
                        }
                        var questType = (EQuestType)questTypeInt;

                        if (!int.TryParse(rdr[4].ToString(), out int questTierInt))
                        {
                            continue;
                        }
                        var questTier = (EQuestTier)questTierInt;

                        var questConditions = rdr[5].ToString();
                        var conditions = Utility.GetQuestConditionsFromString(questConditions);

                        if (!int.TryParse(rdr[6].ToString(), out int targetAmount))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[7].ToString(), out int xp))
                        {
                            continue;
                        }

                        var quest = new Quest(questID, questTitle, questDesc, questType, questTier, conditions, targetAmount, xp);
                        if (!questsSearchByID.ContainsKey(questID))
                        {
                            questsSearchByID.Add(questID, quest);
                            quests.Add(quest);
                        }
                        else
                        {
                            Logging.Debug($"Found a duplicate quest with id {questID}, ignoring this");
                        }
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT `AchievementID`, `AchievementType`-1, `AchievementConditions`, `PageID` FROM `{AchievementsTableName}`;", Conn).ExecuteReaderAsync();
                try
                {
                    var achievements = new List<Achievement>();
                    var achievementsSearchByID = new Dictionary<int, Achievement>();
                    while (await rdr.ReadAsync())
                    {
                        if (!int.TryParse(rdr[0].ToString(), out int achievementID))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[1].ToString(), out int achievementTypeInt))
                        {
                            continue;
                        }

                        var achievementType = (EQuestType)achievementTypeInt;
                        var achievementConditions = rdr[2].ToString();
                        var conditions = Utility.GetQuestConditionsFromString(achievementConditions);
                        if (!int.TryParse(rdr[3].ToString(), out int pageID))
                        {
                            continue;
                        }
                        
                        var achievement = new Achievement(achievementID, achievementType, conditions, new(), new(), pageID);
                        if (!achievementsSearchByID.ContainsKey(achievementID))
                        {
                            achievementsSearchByID.Add(achievementID, achievement);
                            achievements.Add(achievement);
                        }
                        else
                        {
                            Logging.Debug($"Found a duplicate achievement with id {achievementID}, ignoring this");
                        }
                    }
                    AchievementsSearchByID = achievementsSearchByID;
                    Achievements = achievements;

                    Logging.Debug($"Successfully read {achievements.Count} achievements from the table");
                } catch (Exception ex)
                {
                    Logger.Log("Error reading data from achievements table");
                    Logger.Log(ex);
                } finally
                {
                    rdr.Close();
                }

                Logging.Debug("Reading achievements tiers for base data");
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{AchievementsTiersTableName}`;", Conn).ExecuteReaderAsync();
                try
                {
                    while (await rdr.ReadAsync())
                    {
                        if (!int.TryParse(rdr[0].ToString(), out int achievementID))
                        {
                            continue;
                        }

                        if (!AchievementsSearchByID.TryGetValue(achievementID, out Achievement achievement))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[1].ToString(), out int tierID))
                        {
                            continue;
                        }

                        var tierTitle = rdr[2].ToString();
                        var tierDesc = rdr[3].ToString();
                        var tierPrevSmall = rdr[4].ToString();
                        var tierPrevLarge = rdr[5].ToString();
                        if (!int.TryParse(rdr[6].ToString(), out int targetAmount))
                        {
                            continue;
                        }
                        var rewards = Utility.GetRewardsFromString(rdr[7].ToString());
                        var removeRewards = Utility.GetRewardsFromString(rdr[8].ToString());
                        var achievementTier = new AchievementTier(achievement, tierID, tierTitle, tierDesc, tierPrevSmall, tierPrevLarge, targetAmount, rewards, removeRewards);
                    
                        if (!achievement.TiersLookup.ContainsKey(tierID))
                        {
                            achievement.TiersLookup.Add(tierID, achievementTier);
                            achievement.Tiers.Add(achievementTier);
                        } else
                        {
                            Logging.Debug($"Found a duplicate achievement tier with id {tierID} for achievement with id {achievementID}, ignoring this");
                        }
                    }
                    
                    Logging.Debug($"Loaded total {Achievements.Sum(k => k.Tiers.Count)} for {Achievements.Count} achievements");
                } catch (Exception ex)
                {
                    Logger.Log("Error reading data from achievements table");
                    Logger.Log(ex);
                }
                finally
                {
                    rdr.Close();
                }

                Logging.Debug("Reading battlepass tiers for base data");
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{BattlepassTableName}`;", Conn).ExecuteReaderAsync();
                try
                {
                    var battlepassTiersSearchByID = new Dictionary<int, BattlepassTier>();
                    var battlepassTiers = new List<BattlepassTier>();
                    while (await rdr.ReadAsync())
                    {
                        if (!int.TryParse(rdr[0].ToString(), out int tierID))
                        {
                            continue;
                        }

                        var freeReward = Utility.GetRewardFromString(rdr[1].ToString());
                        var freeRewardIcon = rdr[2].ToString();
                        var premiumReward = Utility.GetRewardFromString(rdr[3].ToString());
                        var premiumRewardIcon = rdr[4].ToString();

                        if (!int.TryParse(rdr[5].ToString(), out int xp))
                        {
                            continue;
                        }

                        var battlepass = new BattlepassTier(tierID, freeReward, freeRewardIcon, premiumReward, premiumRewardIcon, xp);

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
                } catch (Exception ex)
                {
                    Logger.Log($"Error reading battlepass tiers from battlepass table");
                    Logger.Log(ex);
                } finally
                {
                    rdr.Close();
                }

                Logging.Debug("Reading servers for base data");
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{ServersTableName}`;", Conn).ExecuteReaderAsync();
                try
                {
                    var servers = new List<Server>();
                    while (await rdr.ReadAsync())
                    {
                        var ip = rdr[0].ToString();
                        var port = rdr[1].ToString();
                        var serverName = rdr[2].ToString();
                        var friendlyIP = rdr[3].ToString();
                        var serverBanner = rdr[4].ToString();
                        var serverDesc = rdr[5].ToString();
                        
                        var server = new Server(ip, port, serverName, friendlyIP, serverBanner, serverDesc);
                        if (server.IPNo == Provider.ip && server.PortNo == Provider.port)
                        {
                            Logging.Debug($"Server registered with ip {server.IP} and port {server.Port} is the current serer, not adding this");
                            continue;
                        }

                        servers.Add(server);
                    }

                    Logging.Debug($"Successfully read {servers.Count} servers from the table");
                    Servers = servers;
                }
                catch (Exception ex)
                {
                    Logger.Log("Error reading data from servers table");
                    Logger.Log(ex);
                } finally
                {
                    rdr.Close();
                }
                
                Logging.Debug("Building a default loadout for new players");
                try
                {
                    var defaultPrimary = defaultGuns.FirstOrDefault(k => k.IsPrimary);
                    Logging.Debug($"Found default primary with id {defaultPrimary?.GunID ?? 0}");
                    var defaultPrimaryAttachments = new List<ushort>();
                    if (defaultPrimary != null)
                    {
                        var defaultAttachments = new Dictionary<EAttachment, GunAttachment>();
                        foreach (var defaultAttachment in defaultPrimary.DefaultAttachments)
                        {
                            if (!defaultAttachments.ContainsKey(defaultAttachment.AttachmentType))
                            {
                                defaultAttachments.Add(defaultAttachment.AttachmentType, defaultAttachment);
                            }
                        }
                        defaultPrimaryAttachments = defaultAttachments.Values.Select(k => k.AttachmentID).ToList();
                    }
                    Logging.Debug($"Found {defaultPrimaryAttachments.Count} default primary attachments");
                    var defaultSecondary = defaultGuns.FirstOrDefault(k => !k.IsPrimary);
                    Logging.Debug($"Found default secondary with id {defaultSecondary?.GunID ?? 0}");
                    var defaultSecondaryAttachments = new List<ushort>();
                    if (defaultSecondary != null)
                    {
                        var defaultAttachments = new Dictionary<EAttachment, GunAttachment>();
                        foreach (var defaultAttachment in defaultSecondary.DefaultAttachments)
                        {
                            if (!defaultAttachments.ContainsKey(defaultAttachment.AttachmentType))
                            {
                                defaultAttachments.Add(defaultAttachment.AttachmentType, defaultAttachment);
                            }
                        }
                        defaultSecondaryAttachments = defaultAttachments.Values.Select(k => k.AttachmentID).ToList();
                    }
                    Logging.Debug($"Found {defaultSecondaryAttachments.Count} default secondary attachments");
                    var defaultPerk = new List<int>();
                    foreach (var perk in defaultPerks)
                    {
                        defaultPerk.Add(perk.PerkID);
                        if (defaultPerk.Count == 3)
                        {
                            break;
                        }
                    }
                    Logging.Debug($"Found {defaultPerk.Count} default perks");
                    var defaultKnife = defaultKnives.Count > 0 ? defaultKnives[0] : null;
                    Logging.Debug($"Found default knife with id {defaultKnife?.KnifeID ?? 0}");
                    var defaultTactical = defaultGadgets.FirstOrDefault(k => k.IsTactical);
                    Logging.Debug($"Found default tactical with id {defaultTactical?.GadgetID ?? 0}");
                    var defaultLethal = defaultGadgets.FirstOrDefault(k => !k.IsTactical);
                    Logging.Debug($"Found default lethal with id {defaultLethal?.GadgetID ?? 0}");
                    var defaultKillstreak = new List<int>();
                    foreach (var killstreak in defaultKillstreaks)
                    {
                        defaultKillstreak.Add(killstreak.KillstreakID);
                        if (defaultKillstreaks.Count == 3)
                        {
                            break;
                        }
                    }
                    Logging.Debug($"Found {defaultKillstreak.Count} default killstreaks");
                    var defaultGlove = defaultGloves.FirstOrDefault();
                    Logging.Debug($"Found default glove with id {defaultGlove?.GloveID ?? 0}");
                    var defaultCard = defaultCards.FirstOrDefault();
                    Logging.Debug($"Found default card with id {defaultCard?.CardID ?? 0}");
                    DefaultLoadout = new LoadoutData("DEFAULT LOADOUT", defaultPrimary?.GunID ?? 0, 0, 0, defaultPrimaryAttachments, defaultSecondary?.GunID ?? 0, 0, 0, defaultSecondaryAttachments, defaultKnife?.KnifeID ?? 0, defaultTactical?.GadgetID ?? 0, defaultLethal?.GadgetID ?? 0, defaultKillstreak, defaultPerk, defaultGlove?.GloveID ?? 0, defaultCard?.CardID ?? 0);
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
                await Conn.CloseAsync();
            }
        }

        public async Task AddPlayerAsync(UnturnedPlayer player, string steamName, string avatarLink)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                Logging.Debug($"Adding {steamName} to the DB");
                await Conn.OpenAsync();
                var cmd = new MySqlCommand($"INSERT INTO `{PlayersTableName}` ( `SteamID` , `SteamName` , `AvatarLink` , `MuteExpiry`, `Coins` ) VALUES ({player.CSteamID}, @name, '{avatarLink}' , {DateTimeOffset.UtcNow.ToUnixTimeSeconds()} , {(Config.UnlockAllItems ? 10000000 : 0)}) ON DUPLICATE KEY UPDATE `AvatarLink` = '{avatarLink}', `SteamName` = @name;", Conn);
                cmd.Parameters.AddWithValue("@name", steamName.ToUnrich());
                await cmd.ExecuteScalarAsync();

                await new MySqlCommand($"INSERT IGNORE INTO `{PlayersLeaderboardDailyTableName}` ( `SteamID` ) VALUES ({player.CSteamID});", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"INSERT IGNORE INTO `{PlayersLeaderboardWeeklyTableName}` ( `SteamID` ) VALUES ({player.CSteamID});", Conn).ExecuteScalarAsync();
                await new MySqlCommand($"INSERT IGNORE INTO `{PlayersLeaderboardSeasonalTableName}` ( `SteamID` ) VALUES ({player.CSteamID});", Conn).ExecuteScalarAsync();

                Logging.Debug($"Giving {steamName} the guns");
                foreach (var gun in Guns.Values)
                {
                    if (gun.LevelRequirement < 0)
                    {
                        continue;
                    }

                    Logging.Debug($"Adding gun with id {gun.GunID}");
                    await new MySqlCommand($"INSERT IGNORE INTO `{PlayersGunsTableName}` (`SteamID` , `GunID` , `Level` , `XP` , `GunKills` , `IsBought` , `Attachments`) VALUES ({player.CSteamID} , {gun.GunID} , 1 , 0 , 0 , {gun.LevelRequirement == 0} , '{Utility.CreateStringFromDefaultAttachments(gun.DefaultAttachments) + Utility.CreateStringFromRewardAttachments(gun.RewardAttachments.Values.ToList())}');", Conn).ExecuteScalarAsync();
                }
                await new MySqlCommand($"INSERT IGNORE INTO `{PlayersGunsSkinsTableName}` (`SteamID` , `SkinIDs`) VALUES ({player.CSteamID}, '');", Conn).ExecuteScalarAsync();

                Logging.Debug($"Giving {steamName} the gun charms");
                foreach (var gunCharm in GunCharms.Values)
                {
                    if (gunCharm.LevelRequirement < 0)
                    {
                        continue;
                    }

                    Logging.Debug($"Adding gun charm with id {gunCharm.CharmID}");
                    await new MySqlCommand($"INSERT IGNORE INTO `{PlayersGunsCharmsTableName}` (`SteamID` , `CharmID` , `IsBought`) VALUES ({player.CSteamID} , {gunCharm.CharmID} , {gunCharm.LevelRequirement == 0});", Conn).ExecuteScalarAsync();
                }

                Logging.Debug($"Giving {steamName} the knives");
                foreach (var knife in Knives.Values)
                {
                    if (knife.LevelRequirement < 0)
                    {
                        continue;
                    }

                    Logging.Debug($"Adding knife with id {knife.KnifeID}");
                    await new MySqlCommand($"INSERT IGNORE INTO `{PlayersKnivesTableName}` (`SteamID` , `KnifeID` , `KnifeKills` , `IsBought`) VALUES ({player.CSteamID} , {knife.KnifeID} , 0 , {knife.LevelRequirement == 0});", Conn).ExecuteScalarAsync();
                }

                Logging.Debug($"Giving {steamName} the gadgets");
                foreach (var gadget in Gadgets.Values)
                {
                    if (gadget.LevelRequirement < 0)
                    {
                        continue;
                    }

                    Logging.Debug($"Adding gadget with id {gadget.GadgetID}");
                    await new MySqlCommand($"INSERT IGNORE INTO  `{PlayersGadgetsTableName}` (`SteamID` , `GadgetID` , `GadgetKills` , `IsBought`) VALUES ({player.CSteamID} , {gadget.GadgetID} , 0 , {gadget.LevelRequirement == 0});", Conn).ExecuteScalarAsync();
                }

                Logging.Debug($"Giving {steamName} the killstreaks");
                foreach (var killstreak in Killstreaks.Values)
                {
                    if (killstreak.LevelRequirement < 0)
                    {
                        continue;
                    }

                    Logging.Debug($"Adding killstreak with id {killstreak.KillstreakID}");
                    await new MySqlCommand($"INSERT IGNORE INTO `{PlayersKillstreaksTableName}` (`SteamID` , `KillstreakID` , `KillstreakKills` , `IsBought`) VALUES ({player.CSteamID} , {killstreak.KillstreakID} , 0 ,  {killstreak.LevelRequirement == 0});", Conn).ExecuteScalarAsync();
                }

                Logging.Debug($"Giving {steamName} the perks");
                foreach (var perk in Perks.Values)
                {
                    if (perk.LevelRequirement < 0)
                    {
                        continue;
                    }

                    Logging.Debug($"Adding perk with id {perk.PerkID}");
                    await new MySqlCommand($"INSERT IGNORE INTO `{PlayersPerksTableName}` (`SteamID` , `PerkID` , `IsBought`) VALUES ({player.CSteamID} , {perk.PerkID} , {perk.LevelRequirement == 0});", Conn).ExecuteScalarAsync();
                }

                Logging.Debug($"Giving {steamName} the gloves");
                foreach (var glove in Gloves.Values)
                {
                    if (glove.LevelRequirement < 0)
                    {
                        continue;
                    }

                    Logging.Debug($"Adding glove with id {glove.GloveID}");
                    await new MySqlCommand($"INSERT IGNORE INTO `{PlayersGlovesTableName}` (`SteamID` , `GloveID` , `IsBought`) VALUES ({player.CSteamID} , {glove.GloveID} , {glove.LevelRequirement == 0});", Conn).ExecuteScalarAsync();
                }

                Logging.Debug($"Giving {steamName} the cards");
                foreach (var card in Cards.Values)
                {
                    if (card.LevelRequirement < 0)
                    {
                        continue;
                    }

                    Logging.Debug($"Adding card with id {card.CardID}");
                    await new MySqlCommand($"INSERT IGNORE INTO `{PlayersCardsTableName}` (`SteamID` , `CardID` , `IsBought`) VALUES ({player.CSteamID} , {card.CardID} ,  {card.LevelRequirement == 0});", Conn).ExecuteScalarAsync();
                }

                Logging.Debug($"Giving {steamName} the battlepass");
                await new MySqlCommand($"INSERT IGNORE INTO `{PlayersBattlepassTableName}` (`SteamID`) VALUES ({player.CSteamID});", Conn).ExecuteScalarAsync();

                Logging.Debug($"Giving {steamName} the achievements");
                foreach (var achievement in Achievements)
                {
                    Logging.Debug($"Adding achievement with id {achievement.AchievementID}");
                    await new MySqlCommand($"INSERT IGNORE INTO `{PlayersAchievementsTableName}` (`SteamID`, `AchievementID`) VALUES ({player.CSteamID}, {achievement.AchievementID});", Conn).ExecuteScalarAsync();
                }

                var loadoutAmount = Utility.GetLoadoutAmount(player);
                Logging.Debug($"{steamName} should have {loadoutAmount} loadouts, adding them");
                var data = Plugin.Instance.DataManager.ConvertLoadoutToJson(DefaultLoadout);
                for (int i = 1; i <= loadoutAmount; i++)
                {
                    Logging.Debug($"Adding loadout with id {i} for {steamName}");
                    await new MySqlCommand($"INSERT IGNORE INTO `{PlayersLoadoutsTableName}` (`SteamID` , `LoadoutID` , `IsActive` , `Loadout`) VALUES ({player.CSteamID}, {i}, {i == 1}, '{data}');", Conn).ExecuteScalarAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding player with Steam ID {player.CSteamID}, Steam Name {steamName}, avatar link {avatarLink}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        // Player Data

        public async Task GetPlayerDataAsync(UnturnedPlayer player)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                Logging.Debug($"Getting data for {player.CharacterName} from the main table");
                await Conn.OpenAsync();
                var rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                try
                {
                    while (await rdr.ReadAsync())
                    {
                        var steamName = rdr[1].ToString();
                        var avatarLink = rdr[2].ToString();
                        if (!int.TryParse(rdr[3].ToString(), out int xp))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[4].ToString(), out int level))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[5].ToString(), out int credits))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[6].ToString(), out int scrap))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[7].ToString(), out int coins))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[8].ToString(), out int kills))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[9].ToString(), out int headshotKills))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[10].ToString(), out int highestKillstreak))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[11].ToString(), out int highestMultiKills))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[12].ToString(), out int killsConfirmed))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[13].ToString(), out int killsDenied))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[14].ToString(), out int flagsCaptured))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[15].ToString(), out int flagsSaved))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[16].ToString(), out int areasTaken))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[17].ToString(), out int deaths))
                        {
                            continue;
                        }

                        if (!bool.TryParse(rdr[18].ToString(), out bool music))
                        {
                            continue;
                        }

                        if (!bool.TryParse(rdr[19].ToString(), out bool isMuted))
                        {
                            continue;
                        }

                        if (!long.TryParse(rdr[20].ToString(), out long muteUnixSeconds))
                        {
                            continue;
                        }

                        var muteExpiry = DateTimeOffset.FromUnixTimeSeconds(muteUnixSeconds);

                        if (!bool.TryParse(rdr[21].ToString(), out bool hasBattlepass))
                        {
                            continue;
                        }
                         
                        if (!float.TryParse(rdr[22].ToString(), out float xpBooster))
                        {
                            continue;
                        }

                        if (!float.TryParse(rdr[23].ToString(), out float bpBooster))
                        {
                            continue;
                        }

                        if (PlayerData.ContainsKey(player.CSteamID))
                        {
                            PlayerData.Remove(player.CSteamID);
                        }

                        PlayerData.Add(player.CSteamID, new PlayerData(player.CSteamID, steamName, avatarLink, xp, level, credits, scrap, coins, kills, headshotKills, highestKillstreak, highestMultiKills, killsConfirmed, killsDenied, flagsCaptured, flagsSaved, areasTaken, deaths, music, isMuted, muteExpiry, hasBattlepass, xpBooster, bpBooster, new(), new(), new(), new(), new(), new()));
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

                Logging.Debug($"Getting leaderboard daily data for {player.CharacterName} from the daily table");
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersLeaderboardDailyTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                try
                {
                    while (await rdr.ReadAsync())
                    {
                        if (!PlayerData.TryGetValue(player.CSteamID, out PlayerData data))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[1].ToString(), out int kills))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[2].ToString(), out int headshotKills))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[3].ToString(), out int deaths))
                        {
                            continue;
                        }

                        var leaderboardData = new LeaderboardData(player.CSteamID, data.SteamName, data.Level, kills, headshotKills, deaths);
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersLeaderboardWeeklyTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                try
                {
                    while (await rdr.ReadAsync())
                    {
                        if (!PlayerData.TryGetValue(player.CSteamID, out PlayerData data))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[1].ToString(), out int kills))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[2].ToString(), out int headshotKills))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[3].ToString(), out int deaths))
                        {
                            continue;
                        }

                        var leaderboardData = new LeaderboardData(player.CSteamID, data.SteamName, data.Level, kills, headshotKills, deaths);
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersLeaderboardSeasonalTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                try
                {
                    while (await rdr.ReadAsync())
                    {
                        if (!PlayerData.TryGetValue(player.CSteamID, out PlayerData data))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[1].ToString(), out int kills))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[2].ToString(), out int headshotKills))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[3].ToString(), out int deaths))
                        {
                            continue;
                        }

                        var leaderboardData = new LeaderboardData(player.CSteamID, data.SteamName, data.Level, kills, headshotKills, deaths);
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
                }

                Logging.Debug($"Getting all time data for {player.CharacterName} from the all time table");
                if (PlayerData.TryGetValue(player.CSteamID, out PlayerData playerData))
                {
                    var leaderboardData = new LeaderboardData(player.CSteamID, playerData.SteamName, playerData.Level, playerData.Kills, playerData.HeadshotKills, playerData.Deaths);
                    if (!PlayerAllTimeLeaderboardLookup.ContainsKey(player.CSteamID))
                    {
                        PlayerAllTimeLeaderboardLookup.Add(player.CSteamID, leaderboardData);
                        PlayerAllTimeKill.Add(leaderboardData);
                        PlayerAllTimeLevel.Add(leaderboardData);
                    }
                }

                Logging.Debug($"Getting quests for {player.CharacterName}");
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersQuestsTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                try
                {
                    var playerQuests = new List<PlayerQuest>();
                    var playerQuestsSearchByType = new Dictionary<EQuestType, List<PlayerQuest>>();

                    while (await rdr.ReadAsync())
                    {
                        if (!int.TryParse(rdr[1].ToString(), out int questID))
                        {
                            continue;
                        }

                        if (!QuestsSearchByID.TryGetValue(questID, out Quest quest))
                        {
                            Logging.Debug($"Error finding quest with id {questID} for {player.CharacterName}, ignoring it");
                            continue;
                        }

                        if (!int.TryParse(rdr[2].ToString(), out int amount))
                        {
                            continue;
                        }

                        if (!long.TryParse(rdr[3].ToString(), out long questEndDate))
                        {
                            continue;
                        }

                        var questEndDateTime = DateTimeOffset.FromUnixTimeSeconds(questEndDate);

                        var playerQuest = new PlayerQuest(player.CSteamID, quest, amount, questEndDateTime);
                        playerQuests.Add(playerQuest);
                        if (!playerQuestsSearchByType.ContainsKey(quest.QuestType))
                        {
                            playerQuestsSearchByType.Add(quest.QuestType, new List<PlayerQuest>());
                        }
                        playerQuestsSearchByType[quest.QuestType].Add(playerQuest);
                    }
                    Logging.Debug($"Got {playerQuests.Count} quests registered to player");

                    rdr.Close();
                    if (playerQuests.Count == 0 || playerQuests[0].QuestEnd.UtcDateTime < DateTime.UtcNow)
                    {
                        Logging.Debug("Quests have expired, generate different quests");

                        playerQuests.Clear();
                        playerQuestsSearchByType.Clear();

                        await new MySqlCommand($"DELETE FROM `{PlayersQuestsTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteScalarAsync();
                        var expiryDate = ServerOptions.DailyLeaderboardWipe;
                        var questsToAdd = new List<Quest>();
                        for (var i = 0; i < 6; i++)
                        {
                            Logging.Debug($"i: {i}, Tier: {(EQuestTier)i}");
                            if (i == 2) continue;
                            var randomQuests = Quests.Where(k => (int)k.QuestTier == i).ToList();
                            Logging.Debug($"FOund {randomQuests.Count} random quests");
                            var randomQuest = randomQuests[UnityEngine.Random.Range(0, randomQuests.Count)];
                            questsToAdd.Add(randomQuest);
                        }

                        foreach (var quest in questsToAdd)
                        {
                            var playerQuest = new PlayerQuest(player.CSteamID, quest, 0, expiryDate);
                            playerQuests.Add(playerQuest);
                            if (!playerQuestsSearchByType.ContainsKey(quest.QuestType))
                            {
                                playerQuestsSearchByType.Add(quest.QuestType, new List<PlayerQuest>());
                            }
                            playerQuestsSearchByType[quest.QuestType].Add(playerQuest);
                            await new MySqlCommand($"INSERT INTO `{PlayersQuestsTableName}` (`SteamID` , `QuestID`, `Amount`, `QuestEnd`) VALUES ({player.CSteamID}, {quest.QuestID}, 0, {expiryDate.ToUnixTimeSeconds()});", Conn).ExecuteScalarAsync();
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

                Logging.Debug($"Getting achievements for {player.CharacterName}");
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersAchievementsTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                try
                {
                    var achievements = new List<PlayerAchievement>();
                    var achievementsSearchByType = new Dictionary<EQuestType, List<PlayerAchievement>>();
                    var achievementsSearchByID = new Dictionary<int, PlayerAchievement>();

                    while (await rdr.ReadAsync())
                    {
                        if (!int.TryParse(rdr[1].ToString(), out int achievementID))
                        {
                            continue;
                        }

                        if (!AchievementsSearchByID.TryGetValue(achievementID, out Achievement achievement))
                        {
                            Logging.Debug($"Error finding achievement with id {achievementID} for {player.CharacterName}, ignoring");
                            continue;
                        }

                        if (!int.TryParse(rdr[2].ToString(), out int currentTier))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[3].ToString(), out int amount))
                        {
                            continue;
                        }

                        var playerAchievement = new PlayerAchievement(player.CSteamID, achievement, currentTier, amount);
                        if (!achievementsSearchByID.ContainsKey(achievementID))
                        {
                            achievementsSearchByID.Add(achievementID, playerAchievement);
                            if (!achievementsSearchByType.ContainsKey(achievement.AchievementType))
                            {
                                achievementsSearchByType.Add(achievement.AchievementType, new());
                            }
                            achievementsSearchByType[achievement.AchievementType].Add(playerAchievement);
                            achievements.Add(playerAchievement);
                        } else
                        {
                            Logging.Debug($"Error, achievement {achievementID} already exists for {player.CharacterName}, ignoring");
                        }
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

                Logging.Debug($"Getting battlepass for {player.CharacterName}");
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersBattlepassTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                try
                {
                    while (await rdr.ReadAsync())
                    {
                        if (!int.TryParse(rdr[1].ToString(), out int currentTier))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[2].ToString(), out int xp))
                        {
                            continue;
                        }

                        var claimedFreeRewards = rdr[3].GetHashSetIntFromReaderResult();
                        var claimedPremiumRewards = rdr[4].GetHashSetIntFromReaderResult();

                        Logging.Debug($"Got battlepass with current tier {currentTier}, xp {xp} and claimed free rewards {claimedFreeRewards.Count} and claimed premium rewards {claimedPremiumRewards.Count} registered to the player");
                        playerData.Battlepass = new PlayerBattlepass(player.CSteamID, currentTier, xp, claimedFreeRewards, claimedPremiumRewards);
                    }
                } catch (Exception ex)
                {
                    Logger.Log($"Error reading battlepass data for {player.CharacterName}");
                    Logger.Log(ex);
                } finally
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersGunsTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                try
                {
                    while (await rdr.ReadAsync())
                    {
                        if (!ushort.TryParse(rdr[1].ToString(), out ushort gunID))
                        {
                            continue;
                        }

                        if (!Guns.TryGetValue(gunID, out Gun gun))
                        {
                            Logging.Debug($"Error finding gun with id {gunID}, ignoring it");
                            continue;
                        }

                        if (!int.TryParse(rdr[2].ToString(), out int level))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[3].ToString(), out int xp))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[4].ToString(), out int gunKills))
                        {
                            continue;
                        }

                        if (!bool.TryParse(rdr[5].ToString(), out bool isBought))
                        {
                            continue;
                        }

                        var attachments = Utility.GetAttachmentsFromString(rdr[6].ToString(), gun, player);
                        if (!guns.ContainsKey(gunID))
                        {
                            guns.Add(gunID, new LoadoutGun(gun, level, xp, gunKills, isBought, attachments));
                        }
                        else
                        {
                            Logging.Debug($"Found a duplicate gun with id {gunID} registered for the player, ignoring it");
                        }
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
                try
                {
                    foreach (var gun in guns.Values)
                    {
                        foreach (var rewardAttachment in gun.Gun.RewardAttachments)
                        {
                            if (!gun.Attachments.ContainsKey(rewardAttachment.Value.AttachmentID))
                            {
                                gun.Attachments.Add(rewardAttachment.Value.AttachmentID, new LoadoutAttachment(rewardAttachment.Value, rewardAttachment.Key, false));
                                Logging.Debug($"Gun with name {gun.Gun.GunName} doesn't have a reward attachment with id {rewardAttachment.Value.AttachmentID} that comes with the gun, adding it for {player.CharacterName}");
                            }
                        }
                        await new MySqlCommand($"UPDATE `{PlayersGunsTableName}` SET `Attachments` = '{Utility.GetStringFromAttachments(gun.Attachments.Values.ToList())}' WHERE `SteamID` = {player.CSteamID} AND `GunID` = {gun.Gun.GunID};", Conn).ExecuteScalarAsync();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Error while checking gun attachments");
                    Logger.Log(ex);
                }

                Logging.Debug($"Getting gun skins for {player.CharacterName}");
                var gunSkinsTxt = await new MySqlCommand($"SELECT `SkinIDs` FROM `{PlayersGunsSkinsTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteScalarAsync();
                if (gunSkinsTxt is string gunSkinsText)
                {
                    foreach (var id in gunSkinsText.GetIntListFromReaderResult())
                    {
                        if (!GunSkinsSearchByID.TryGetValue(id, out GunSkin skin))
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
                        if (gunSkinsSearchByGunID.TryGetValue(skin.Gun.GunID, out List<GunSkin> gunSkins))
                        {
                            gunSkins.Add(skin);
                        }
                        else
                        {
                            gunSkinsSearchByGunID.Add(skin.Gun.GunID, new List<GunSkin> { skin });
                        }
                        gunSkinsSearchBySkinID.Add(skin.SkinID, skin);
                    }
                    Logging.Debug($"Successfully got {gunSkinsSearchByID.Count} gun skins for {player.CharacterName}");
                }

                Logging.Debug($"Getting gun charms for {player.CharacterName}");
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersGunsCharmsTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                try
                {
                    while (await rdr.ReadAsync())
                    {
                        if (!ushort.TryParse(rdr[1].ToString(), out ushort charmID))
                        {
                            continue;
                        }

                        if (!GunCharms.TryGetValue(charmID, out GunCharm gunCharm))
                        {
                            Logging.Debug($"Error finding gun charm with id {charmID} for {player.CharacterName}, ignoring it");
                            continue;
                        }

                        if (!bool.TryParse(rdr[2].ToString(), out bool isBought))
                        {
                            continue;
                        }

                        if (!gunCharms.ContainsKey(charmID))
                        {
                            gunCharms.Add(charmID, new LoadoutGunCharm(gunCharm, isBought));
                        }
                        else
                        {
                            Logging.Debug($"Found duplicate gun charm with id {charmID} for {player.CharacterName}, ignoring it");
                        }
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersKnivesTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                try
                {
                    while (await rdr.ReadAsync())
                    {
                        if (!ushort.TryParse(rdr[1].ToString(), out ushort knifeID))
                        {
                            continue;
                        }

                        if (!Knives.TryGetValue(knifeID, out Knife knife))
                        {
                            Logging.Debug($"Error finding knife with id {knifeID}, ignoring it");
                            continue;
                        }
                        if (!int.TryParse(rdr[2].ToString(), out int knifeKills))
                        {
                            continue;
                        }

                        if (!bool.TryParse(rdr[3].ToString(), out bool isBought))
                        {
                            continue;
                        }

                        if (!knives.ContainsKey(knifeID))
                        {
                            knives.Add(knifeID, new LoadoutKnife(knife, knifeKills, isBought));
                        }
                        else
                        {
                            Logging.Debug($"Found a duplicate knife with id {knifeID} registered for {player.CharacterName}, ignoring it");
                        }
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersPerksTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                try
                {
                    while (await rdr.ReadAsync())
                    {
                        if (!int.TryParse(rdr[1].ToString(), out int perkID))
                        {
                            continue;
                        }

                        if (!Perks.TryGetValue(perkID, out Perk perk))
                        {
                            Logging.Debug($"Error finding perk with id {perkID}, ignoring this");
                            continue;
                        }
                        if (!bool.TryParse(rdr[2].ToString(), out bool isBought))
                        {
                            continue;
                        }

                        if (!perks.ContainsKey(perkID))
                        {
                            perks.Add(perkID, new LoadoutPerk(perk, isBought));
                        }
                        else
                        {
                            Logging.Debug($"Found a duplicate perk with id {perkID} registered for {player.CharacterName}, ignoring it");
                        }
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersGadgetsTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                try
                {
                    while (await rdr.ReadAsync())
                    {
                        if (!ushort.TryParse(rdr[1].ToString(), out ushort gadgetID))
                        {
                            continue;
                        }

                        if (!Gadgets.TryGetValue(gadgetID, out Gadget gadget))
                        {
                            Logging.Debug($"Error finding gadget with id {gadgetID} for {player.CharacterName}, ignoring it");
                            continue;
                        }
                        if (!int.TryParse(rdr[2].ToString(), out int gadgetKills))
                        {
                            continue;
                        }

                        if (!bool.TryParse(rdr[3].ToString(), out bool isBought))
                        {
                            continue;
                        }

                        if (!gadgets.ContainsKey(gadgetID))
                        {
                            gadgets.Add(gadgetID, new LoadoutGadget(gadget, gadgetKills, isBought));
                        }
                        else
                        {
                            Logging.Debug($"Found duplicate gadget with id {gadgetID} registered for {player.CharacterName}, ignoring it");
                        }
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersKillstreaksTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                try
                {
                    while (await rdr.ReadAsync())
                    {
                        if (!int.TryParse(rdr[1].ToString(), out int killstreakID))
                        {
                            continue;
                        }

                        if (!Killstreaks.TryGetValue(killstreakID, out Killstreak killstreak))
                        {
                            Logging.Debug($"Error finding killstreak with id {killstreakID} for {player.CharacterName}, ignoring it");
                            continue;
                        }
                        if (!int.TryParse(rdr[2].ToString(), out int killstreakKills))
                        {
                            continue;
                        }

                        if (!bool.TryParse(rdr[3].ToString(), out bool isBought))
                        {
                            continue;
                        }

                        if (!killstreaks.ContainsKey(killstreakID))
                        {
                            killstreaks.Add(killstreakID, new LoadoutKillstreak(killstreak, killstreakKills, isBought));
                        }
                        else
                        {
                            Logging.Debug($"Found a duplicate killstreak with id {killstreakID} for {player.CharacterName}, ignoring it");
                        }
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersCardsTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                try
                {
                    while (await rdr.ReadAsync())
                    {
                        if (!int.TryParse(rdr[1].ToString(), out int cardID))
                        {
                            continue;
                        }

                        if (!Cards.TryGetValue(cardID, out Card card))
                        {
                            Logging.Debug($"Error finding card with id {cardID} for {player.CharacterName}, ignoring it");
                            continue;
                        }

                        if (!bool.TryParse(rdr[2].ToString(), out bool isBought))
                        {
                            continue;
                        }

                        if (!cards.ContainsKey(cardID))
                        {
                            cards.Add(cardID, new LoadoutCard(card, isBought));
                        }
                        else
                        {
                            Logging.Debug($"Found duplicate card with id {cardID} for {player.CharacterName}, ignoring it");
                        }
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersGlovesTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                try
                {
                    while (await rdr.ReadAsync())
                    {
                        if (!int.TryParse(rdr[1].ToString(), out int gloveID))
                        {
                            continue;
                        }

                        if (!Gloves.TryGetValue(gloveID, out Glove glove))
                        {
                            Logging.Debug($"Error finding glove with id {gloveID} for {player.CharacterName}, ignoring it");
                            continue;
                        }
                        if (!bool.TryParse(rdr[2].ToString(), out bool isBought))
                        {
                            continue;
                        }

                        if (!gloves.ContainsKey(gloveID))
                        {
                            gloves.Add(gloveID, new LoadoutGlove(glove, isBought));
                        }
                        else
                        {
                            Logging.Debug($"Found duplicate glove with id {gloveID} for {player.CharacterName}, ignoring it");
                        }
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
                rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersLoadoutsTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                try
                {
                    while (await rdr.ReadAsync())
                    {
                        var shouldContinue = true;
                        if (!int.TryParse(rdr[1].ToString(), out int loadoutID))
                        {
                            continue;
                        }

                        if (!bool.TryParse(rdr[2].ToString(), out bool isActive))
                        {
                            continue;
                        }

                        if (loadouts.ContainsKey(loadoutID))
                        {
                            Logging.Debug($"Found a duplicate loadout with id {loadoutID} for {player.CharacterName}, ignoring it");
                            continue;
                        }

                        var loadoutData = Plugin.Instance.DataManager.ConvertLoadoutFromJson(rdr[3].ToString());
                        if (!guns.TryGetValue(loadoutData.Primary, out LoadoutGun primary) && loadoutData.Primary != 0)
                        {
                            Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a primary with id {loadoutData.Primary} which is not owned by the player, not counting this loadout");
                            continue;
                        }

                        if (!gunCharms.TryGetValue(loadoutData.PrimaryGunCharm, out LoadoutGunCharm primaryGunCharm) && loadoutData.PrimaryGunCharm != 0)
                        {
                            Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a primary gun charm with id {loadoutData.PrimaryGunCharm} which is not owned by the player, not counting this loadout");
                            continue;
                        }

                        if (!gunSkinsSearchByID.TryGetValue(loadoutData.PrimarySkin, out GunSkin primarySkin) && loadoutData.PrimarySkin != 0)
                        {
                            Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a primary skin with id {loadoutData.PrimarySkin} which is not owned by the player, not counting this loadout");
                            continue;
                        }

                        var primaryAttachments = new Dictionary<EAttachment, LoadoutAttachment>();
                        foreach (var primaryAttachment in loadoutData.PrimaryAttachments)
                        {
                            if (primary.Attachments.TryGetValue(primaryAttachment, out LoadoutAttachment attachment))
                            {
                                primaryAttachments.Add(attachment.Attachment.AttachmentType, attachment);
                            }
                            else
                            {
                                Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a primary attachment id with {primaryAttachment} which is not owned by the player, not counting it");
                            }
                        }

                        if (!guns.TryGetValue(loadoutData.Secondary, out LoadoutGun secondary) && loadoutData.Secondary != 0)
                        {
                            Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a secondary with id {loadoutData.Secondary} which is not owned by the player, not counting this loadout");
                            continue;
                        }

                        if (!gunCharms.TryGetValue(loadoutData.SecondaryGunCharm, out LoadoutGunCharm secondaryGunCharm) && loadoutData.SecondaryGunCharm != 0)
                        {
                            Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a secondary gun charm with id {loadoutData.SecondaryGunCharm} which is not owned by the player, not counting this loadout");
                            continue;
                        }

                        if (!gunSkinsSearchByID.TryGetValue(loadoutData.SecondarySkin, out GunSkin secondarySkin) && loadoutData.SecondarySkin != 0)
                        {
                            Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a secondary skin with id {loadoutData.SecondarySkin} which is not owned by the player, not counting this loadout");
                            continue;
                        }

                        var secondaryAttachments = new Dictionary<EAttachment, LoadoutAttachment>();
                        foreach (var secondaryAttachment in loadoutData.SecondaryAttachments)
                        {
                            if (secondary.Attachments.TryGetValue(secondaryAttachment, out LoadoutAttachment attachment))
                            {
                                secondaryAttachments.Add(attachment.Attachment.AttachmentType, attachment);
                            }
                            else
                            {
                                Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a secondary attachment id with {secondaryAttachment} which is not owned by the player, not counting it");
                            }
                        }

                        if (!knives.TryGetValue(loadoutData.Knife, out LoadoutKnife knife) && loadoutData.Knife != 0)
                        {
                            Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a knife with id {loadoutData.Knife} which is not owned by the player, not counting this loadout");
                            continue;
                        }

                        if (!gadgets.TryGetValue(loadoutData.Tactical, out LoadoutGadget tactical) && loadoutData.Tactical != 0)
                        {
                            Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a tactical with id {loadoutData.Tactical} which is not owned by the player, not counting this loadout");
                            continue;
                        }

                        if (!gadgets.TryGetValue(loadoutData.Lethal, out LoadoutGadget lethal) && loadoutData.Lethal != 0)
                        {
                            Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a lethal with id {loadoutData.Lethal} which is not owned by the player, not counting this loadout");
                            continue;
                        }

                        var loadoutKillstreaks = new List<LoadoutKillstreak>();
                        foreach (var killstreakID in loadoutData.Killstreaks)
                        {
                            if (killstreaks.TryGetValue(killstreakID, out LoadoutKillstreak killstreak))
                            {
                                loadoutKillstreaks.Add(killstreak);
                            }
                            else
                            {
                                Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a killstreak with id {killstreakID} which is not owned by the player, not counting this loadout");
                                shouldContinue = false;
                                break;
                            }
                        }
                        if (!shouldContinue)
                        {
                            continue;
                        }

                        var loadoutPerks = new Dictionary<int, LoadoutPerk>();
                        foreach (var perkID in loadoutData.Perks)
                        {
                            if (perks.TryGetValue(perkID, out LoadoutPerk perk))
                            {
                                loadoutPerks.Add(perk.Perk.PerkType, perk);
                            }
                            else
                            {
                                Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a perk with id {perkID} which is not owned by the player, not counting this loadout");
                                shouldContinue = false;
                                break;
                            }
                        }
                        if (!shouldContinue)
                        {
                            continue;
                        }

                        if (!gloves.TryGetValue(loadoutData.Glove, out LoadoutGlove glove) && loadoutData.Glove != 0)
                        {
                            Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a glove with id {loadoutData.Glove} which is not owned by the player, not counting this loadout");
                            continue;
                        }

                        if (!cards.TryGetValue(loadoutData.Card, out LoadoutCard card) && loadoutData.Card != 0)
                        {
                            Logging.Debug($"Loadout with id {loadoutID} has a card with id {loadoutData.Card} which is not owned by the player, not counting this loadout");
                            continue;
                        }

                        loadouts.Add(loadoutID, new Loadout(loadoutID, loadoutData.LoadoutName, isActive, primary, primarySkin, primaryGunCharm, primaryAttachments, secondary, secondarySkin, secondaryGunCharm, secondaryAttachments, knife, tactical, lethal, loadoutKillstreaks, loadoutPerks, glove, card));
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

                Logging.Debug($"Checking if player has more loadouts for {player.CharacterName}");
                try
                {
                    var loadoutAmount = Utility.GetLoadoutAmount(player);
                    Logging.Debug($"{player.CharacterName} should have {loadoutAmount} loadouts, he has {loadouts.Count} registered");
                    var data = Plugin.Instance.DataManager.ConvertLoadoutToJson(DefaultLoadout);
                    if (loadoutAmount < loadouts.Count)
                    {
                        Logging.Debug($"{player.CharacterName} has more loadouts than he should have, deleting the last ones");
                        for (int i = loadouts.Count; i >= 1; i--)
                        {
                            if (loadouts.Count == loadoutAmount)
                            {
                                break;
                            }

                            Logging.Debug($"Removing loadout with id {i} for {player.CharacterName}");
                            await new MySqlCommand($"DELETE FROM `{PlayersLoadoutsTableName}` WHERE `SteamID` = {player.CSteamID} AND `LoadoutID` = {i}", Conn).ExecuteScalarAsync();
                            loadouts.Remove(i);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Error checking the loadout amounts for player");
                    Logger.Log(ex);
                }

                if (PlayerLoadouts.ContainsKey(player.CSteamID))
                {
                    PlayerLoadouts.Remove(player.CSteamID);
                }
                PlayerLoadouts.Add(player.CSteamID, new PlayerLoadout(guns, gunCharms, knives, gunSkinsSearchByID, gunSkinsSearchByGunID, gunSkinsSearchBySkinID, perks, gadgets, killstreaks, cards, gloves, loadouts));
            }
            catch (Exception ex)
            {
                Logger.Log($"Error getting player data with Steam ID {player.CSteamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task IncreasePlayerXPAsync(CSteamID steamID, int xp)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `XP` = `XP` + {xp} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"Select `XP` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();

                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    if (obj is int newXp)
                    {
                        data.XP = newXp;
                    }

                    if (data.TryGetNeededXP(out int neededXP))
                    {
                        if (data.XP >= neededXP)
                        {
                            var newXP = data.XP - neededXP;
                            await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `XP` = {newXP}, `Level` = `Level` + 1 WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                            obj = await new MySqlCommand($"Select `Level` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                            if (obj is int level)
                            {
                                data.Level = level;
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    var player = Plugin.Instance.GameManager.GetGamePlayer(data.SteamID);
                                    if (player != null)
                                    {
                                        Plugin.Instance.UIManager.SendAnimation(player, new AnimationInfo(EAnimationType.LevelUp, level));
                                    }
                                });
                            }
                            data.XP = newXP;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding {xp} xp for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task IncreasePlayerCreditsAsync(CSteamID steamID, int credits)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `Credits` = `Credits` + {credits} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"Select `Credits` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    if (obj is int newCredits)
                    {
                        data.Credits = newCredits;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding {credits} credits for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task DecreasePlayerCreditsAsync(CSteamID steamID, int credits)
        {
            if (Config.UnlockAllItems) return;
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `Credits` = `Credits` - {credits} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"Select `Credits` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    if (obj is int newCredits)
                    {
                        data.Credits = newCredits;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error removing {credits} credits for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task IncreasePlayerScrapAsync(CSteamID steamID, int scrap)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `Scrap` = `Scrap` + {scrap} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"Select `Scrap` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    if (obj is int newScrap)
                    {
                        data.Scrap = newScrap;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding {scrap} scrap for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task DecreasePlayerScrapAsync(CSteamID steamID, int scrap)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `Scrap` = `Scrap` - {scrap} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"Select `Scrap` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    if (obj is int newScrap)
                    {
                        data.Scrap = newScrap;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error removing {scrap} scrap for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task IncreasePlayerCoinsAsync(CSteamID steamID, int coins)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `Coins` = `Coins` + {coins} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"Select `Coins` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    if (obj is int newCoins)
                    {
                        data.Coins = newCoins;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding {coins} coins for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task DecreasePlayerCoinsAsync(CSteamID steamID, int coins)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `Coins` = `Coins` - {coins} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"Select `Coins` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    if (obj is int newCoins)
                    {
                        data.Coins = newCoins;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error removing {coins} coins for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task IncreasePlayerKillsAsync(CSteamID steamID, int kills)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `Kills` = `Kills` + {kills} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"Select `Kills` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();

                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    if (obj is int newKills)
                    {
                        data.Kills = newKills;
                    }
                }

                await new MySqlCommand($"UPDATE `{PlayersLeaderboardDailyTableName}` SET `Kills` = `Kills` + {kills} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                obj = await new MySqlCommand($"SELECT `Kills` FROM `{PlayersLeaderboardDailyTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();

                if (PlayerDailyLeaderboardLookup.TryGetValue(steamID, out LeaderboardData lData))
                {
                    if (obj is int newKills)
                    {
                        lData.Kills = newKills;
                    }
                }

                await new MySqlCommand($"UPDATE `{PlayersLeaderboardWeeklyTableName}` SET `Kills` = `Kills` + {kills} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                obj = await new MySqlCommand($"SELECT `Kills` FROM `{PlayersLeaderboardWeeklyTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();

                if (PlayerWeeklyLeaderboardLookup.TryGetValue(steamID, out lData))
                {
                    if (obj is int newKills)
                    {
                        lData.Kills = newKills;
                    }
                }

                await new MySqlCommand($"UPDATE `{PlayersLeaderboardSeasonalTableName}` SET `Kills` = `Kills` + {kills} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                obj = await new MySqlCommand($"SELECT `Kills` FROM `{PlayersLeaderboardSeasonalTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();

                if (PlayerSeasonalLeaderboardLookup.TryGetValue(steamID, out lData))
                {
                    if (obj is int newKills)
                    {
                        lData.Kills = newKills;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding {kills} kills for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task IncreasePlayerHeadshotKillsAsync(CSteamID steamID, int headshotKills)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `HeadshotKills` = `HeadshotKills` + {headshotKills} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"Select `HeadshotKills` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    if (obj is int newHeadshotKills)
                    {
                        data.HeadshotKills = newHeadshotKills;
                    }
                }

                await new MySqlCommand($"UPDATE `{PlayersLeaderboardDailyTableName}` SET `HeadshotKills` = `HeadshotKills` + {headshotKills} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                obj = await new MySqlCommand($"Select `HeadshotKills` FROM `{PlayersLeaderboardDailyTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerDailyLeaderboardLookup.TryGetValue(steamID, out LeaderboardData lData))
                {
                    if (obj is int newHeadshotKills)
                    {
                        lData.HeadshotKills = newHeadshotKills;
                    }
                }

                await new MySqlCommand($"UPDATE `{PlayersLeaderboardWeeklyTableName}` SET `HeadshotKills` = `HeadshotKills` + {headshotKills} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                obj = await new MySqlCommand($"Select `HeadshotKills` FROM `{PlayersLeaderboardWeeklyTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerWeeklyLeaderboardLookup.TryGetValue(steamID, out lData))
                {
                    if (obj is int newHeadshotKills)
                    {
                        lData.HeadshotKills = newHeadshotKills;
                    }
                }

                await new MySqlCommand($"UPDATE `{PlayersLeaderboardSeasonalTableName}` SET `HeadshotKills` = `HeadshotKills` + {headshotKills} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                obj = await new MySqlCommand($"Select `HeadshotKills` FROM `{PlayersLeaderboardSeasonalTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerSeasonalLeaderboardLookup.TryGetValue(steamID, out lData))
                {
                    if (obj is int newHeadshotKills)
                    {
                        lData.HeadshotKills = newHeadshotKills;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding {headshotKills} headshot kills for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task UpdatePlayerHighestKillStreakAsync(CSteamID steamID, int killStreak)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `HighestKillstreak` = {killStreak} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    data.HighestKillstreak = killStreak;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error setting {killStreak} highest killstreak for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task UpdatePlayerHighestMultiKillsAsync(CSteamID steamID, int multiKills)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `HighestMultiKills` = {multiKills} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    data.HighestMultiKills = multiKills;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error setting {multiKills} highest multi kills for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task IncreasePlayerKillsConfirmedAsync(CSteamID steamID, int killsConfirmed)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `KillsConfirmed` = `KillsConfirmed` + {killsConfirmed} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"Select `KillsConfirmed` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    if (obj is int newKillsConfirmed)
                    {
                        data.KillsConfirmed = newKillsConfirmed;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding {killsConfirmed} kills confirmed for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task IncreasePlayerKillsDeniedAsync(CSteamID steamID, int killsDenied)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `KillsDenied` = `KillsDenied` + {killsDenied} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"Select `KillsDenied` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    if (obj is int newKillsDenied)
                    {
                        data.KillsDenied = newKillsDenied;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding {killsDenied} kills denied for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task IncreasePlayerFlagsCapturedAsync(CSteamID steamID, int flagsCaptured)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `FlagsCaptured` = `FlagsCaptured` + {flagsCaptured} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"Select `FlagsCaptured` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    if (obj is int newFlagsCaptured)
                    {
                        data.FlagsCaptured = newFlagsCaptured;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding {flagsCaptured} flags captured for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task IncreasePlayerFlagsSavedAsync(CSteamID steamID, int flagsSaved)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `FlagsSaved` = `FlagsSaved` + {flagsSaved} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"Select `FlagsSaved` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    if (obj is int newFlagsSaved)
                    {
                        data.FlagsSaved = newFlagsSaved;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding {flagsSaved} flags saved for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task IncreasePlayerAreasTakenAsync(CSteamID steamID, int areasTaken)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `AreasTaken` = `AreasTaken` + {areasTaken} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"Select `AreasTaken` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    if (obj is int newArenasTaken)
                    {
                        data.AreasTaken = newArenasTaken;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding {areasTaken} areas taken for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task IncreasePlayerDeathsAsync(CSteamID steamID, int deaths)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `Deaths` = `Deaths` + {deaths} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"Select `Deaths` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    if (obj is int newDeaths)
                    {
                        data.Deaths = newDeaths;
                    }
                }

                await new MySqlCommand($"UPDATE `{PlayersLeaderboardDailyTableName}` SET `Deaths` = `Deaths` + {deaths} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                obj = await new MySqlCommand($"Select `Deaths` FROM `{PlayersLeaderboardDailyTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerDailyLeaderboardLookup.TryGetValue(steamID, out LeaderboardData lData))
                {
                    if (obj is int newDeaths)
                    {
                        lData.Deaths = newDeaths;
                    }
                }

                await new MySqlCommand($"UPDATE `{PlayersLeaderboardWeeklyTableName}` SET `Deaths` = `Deaths` + {deaths} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                obj = await new MySqlCommand($"Select `Deaths` FROM `{PlayersLeaderboardWeeklyTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerWeeklyLeaderboardLookup.TryGetValue(steamID, out lData))
                {
                    if (obj is int newDeaths)
                    {
                        lData.Deaths = newDeaths;
                    }
                }

                await new MySqlCommand($"UPDATE `{PlayersLeaderboardSeasonalTableName}` SET `Deaths` = `Deaths` + {deaths} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                obj = await new MySqlCommand($"Select `Deaths` FROM `{PlayersLeaderboardSeasonalTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerSeasonalLeaderboardLookup.TryGetValue(steamID, out lData))
                {
                    if (obj is int newDeaths)
                    {
                        lData.Deaths = newDeaths;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding {deaths} deaths for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task ChangePlayerMusicAsync(CSteamID steamID, bool isMusic)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `Music` = {isMusic} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    data.Music = isMusic;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error changing music to {isMusic} for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task ChangePlayerMutedAsync(CSteamID steamID, bool isMuted)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `IsMuted` = {isMuted} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    data.IsMuted = isMuted;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error changing is muted to {isMuted} for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task ChangePlayerMuteExpiryAsync(CSteamID steamID, DateTimeOffset muteExpiry)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `MuteExpiry` = {muteExpiry.ToUnixTimeSeconds()} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    data.MuteExpiry = muteExpiry;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error changing mute expiry to {muteExpiry.ToUnixTimeSeconds()} for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        // Player Guns

        public async Task AddPlayerGunAsync(CSteamID steamID, ushort gunID, bool isBought)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                if (!Guns.TryGetValue(gunID, out Gun gun))
                {
                    Logging.Debug($"Error finding gun with id {gunID} to add to {steamID}");
                    return;
                }

                await new MySqlCommand($"INSERT INTO `{PlayersGunsTableName}` (`SteamID` , `GunID` , `Level` , `XP` , `GunKills` , `IsBought` , `Attachments`) VALUES ({steamID} , {gunID} , 1 , 0 , 0 , {isBought} , '{Utility.CreateStringFromDefaultAttachments(gun.DefaultAttachments) + Utility.CreateStringFromRewardAttachments(gun.RewardAttachments.Values.ToList())}') ON DUPLICATE KEY UPDATE `IsBought` = {isBought};", Conn).ExecuteScalarAsync();

                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Couldnt finding loadout for player with steam id {steamID}");
                    return;
                }

                var loadoutAttachments = new Dictionary<ushort, LoadoutAttachment>();
                foreach (var attachment in gun.DefaultAttachments)
                {
                    if (loadoutAttachments.ContainsKey(attachment.AttachmentID))
                    {
                        Logging.Debug($"Duplicate default attachment found for gun {gunID} with id {attachment.AttachmentID}, ignoring it");
                        continue;
                    }
                    loadoutAttachments.Add(attachment.AttachmentID, new LoadoutAttachment(attachment, 0, true));
                }

                foreach (var attachment in gun.RewardAttachments)
                {
                    if (loadoutAttachments.ContainsKey(attachment.Value.AttachmentID))
                    {
                        Logging.Debug($"Duplicate reward attachment found for gun {gunID} with id {attachment.Value.AttachmentID}, ignoring it");
                        continue;
                    }
                    loadoutAttachments.Add(attachment.Value.AttachmentID, new LoadoutAttachment(attachment.Value, attachment.Key, true));
                }

                var loadoutGun = new LoadoutGun(gun, 1, 0, 0, isBought, loadoutAttachments);

                if (loadout.Guns.ContainsKey(loadoutGun.Gun.GunID))
                {
                    Logging.Debug($"{steamID} has already gun with id {gunID} registered, ignoring it");
                    loadout.Guns[loadoutGun.Gun.GunID].IsBought = isBought;
                    return;
                }
                loadout.Guns.Add(loadoutGun.Gun.GunID, loadoutGun);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding gun to {steamID} with gun id {gunID} and is bought {isBought}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task IncreasePlayerGunXPAsync(CSteamID steamID, ushort gunID, int xp)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                await new MySqlCommand($"UPDATE `{PlayersGunsTableName}` SET `XP` = `XP` + {xp} WHERE `SteamID` = {steamID} AND `GunID` = {gunID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"SELECT `XP` FROM `{PlayersGunsTableName}` WHERE `SteamID` = {steamID} AND `GunID` = {gunID};", Conn).ExecuteScalarAsync();
                if (obj is int newXP)
                {
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        return;
                    }
                    if (!loadout.Guns.TryGetValue(gunID, out LoadoutGun gun))
                    {
                        Logging.Debug($"Error finding loadout gun with id {gunID} for player with steam id {steamID}");
                        return;
                    }

                    gun.XP = newXP;
                    if (gun.TryGetNeededXP(out int neededXP))
                    {
                        if (gun.XP >= neededXP)
                        {
                            var updatedXP = gun.XP - neededXP;
                            await new MySqlCommand($"UPDATE `{PlayersGunsTableName}` SET `XP` = {updatedXP}, `Level` = `Level` + 1 WHERE `SteamID` = {steamID} AND `GunID` = {gunID};", Conn).ExecuteScalarAsync();
                            obj = await new MySqlCommand($"SELECT `Level` FROM `{PlayersGunsTableName}` WHERE `SteamID` = {steamID} AND `GunID` = {gunID};", Conn).ExecuteScalarAsync();
                            if (obj is int newLevel)
                            {
                                gun.Level = newLevel;
                            }
                            gun.XP = updatedXP;

                            TaskDispatcher.QueueOnMainThread(() =>
                            {
                                var player = Plugin.Instance.GameManager.GetGamePlayer(steamID);
                                if (player != null)
                                {
                                    Plugin.Instance.UIManager.SendAnimation(player, new AnimationInfo(EAnimationType.GunLevelUp, gun));
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding {xp} xp to gun with id {gunID} for {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task IncreasePlayerGunKillsAsync(CSteamID steamID, ushort gunID, int kills)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                await new MySqlCommand($"UPDATE `{PlayersGunsTableName}` SET `GunKills` = `GunKills` + {kills} WHERE `SteamID` = {steamID} AND `GunID` = {gunID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"SELECT `GunKills` FROM `{PlayersGunsTableName}` WHERE `SteamID` = {steamID} AND `GunID` = {gunID};", Conn).ExecuteScalarAsync();
                if (obj is int newKills)
                {
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        return;
                    }

                    if (!loadout.Guns.TryGetValue(gunID, out LoadoutGun gun))
                    {
                        Logging.Debug($"Error finding loadout gun with id {gunID} for player with steam id {steamID}");
                        return;
                    }

                    gun.GunKills = newKills;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding kills {kills} to gun with id {gunID} for steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task UpdatePlayerGunBoughtAsync(CSteamID steamID, ushort gunID, bool isBought)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                await new MySqlCommand($"UPDATE `{PlayersGunsTableName}` SET `IsBought` = {isBought} WHERE `SteamID` = {steamID} AND `GunID` = {gunID};", Conn).ExecuteScalarAsync();

                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                    return;
                }

                if (!loadout.Guns.TryGetValue(gunID, out LoadoutGun gun))
                {
                    Logging.Debug($"Error finding loadout gun with id {gunID} for player with steam id {steamID}");
                    return;
                }

                gun.IsBought = isBought;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error changing bought to {isBought} for gun with id {gunID} for steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        // Player Guns Attachments

        public async Task UpdatePlayerGunAttachmentBoughtAsync(CSteamID steamID, ushort gunID, ushort attachmentID, bool isBought)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                    return;
                }

                if (!loadout.Guns.TryGetValue(gunID, out LoadoutGun gun))
                {
                    Logging.Debug($"Error finding loadout gun with id {gunID} for player with steam id {steamID}");
                    return;
                }

                if (!gun.Attachments.TryGetValue(attachmentID, out LoadoutAttachment attachment))
                {
                    Logging.Debug($"Error finding loadout attachment with id {attachmentID} for loadout gun with id {gunID} for player with steam id {steamID}");
                    return;
                }

                attachment.IsBought = isBought;
                await new MySqlCommand($"UPDATE `{PlayersGunsTableName}` SET `Attachments` = '{Utility.GetStringFromAttachments(gun.Attachments.Values.ToList())}' WHERE `SteamID` = {steamID} AND `GunID` = {gunID};", Conn).ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error changing is bought to {isBought} for attachment with id {attachmentID} for gun with id {gunID} for player with steam id");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        // Player Guns Skins

        public async Task AddPlayerGunSkinAsync(CSteamID steamID, int id)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Couldnt finding loadout for player with steam id {steamID}");
                    var skins = await new MySqlCommand($"SELECT `SkinIDs` FROM `{PlayersGunsSkinsTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    var ids = skins.GetIntListFromReaderResult();
                    if (!ids.Contains(id))
                    {
                        ids.Add(id);
                        var newSkins = ids.GetStringFromIntList();
                        await new MySqlCommand($"UPDATE `{PlayersGunsSkinsTableName}` SET `SkinIDs` = '{newSkins}' WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    }
                    return;
                }

                if (!GunSkinsSearchByID.TryGetValue(id, out GunSkin skin))
                {
                    Logging.Debug($"Error finding gun skin with id {id}");
                    return;
                }

                if (loadout.GunSkinsSearchByID.ContainsKey(id))
                {
                    Logging.Debug($"Found gun skin with id {id} already registered to player with steam id {steamID}");
                    return;
                }

                loadout.GunSkinsSearchByID.Add(id, skin);
                if (loadout.GunSkinsSearchByGunID.TryGetValue(skin.Gun.GunID, out List<GunSkin> gunSkins))
                {
                    gunSkins.Add(skin);
                }
                else
                {
                    loadout.GunSkinsSearchByGunID.Add(skin.Gun.GunID, new List<GunSkin> { skin });
                }
                loadout.GunSkinsSearchBySkinID.Add(skin.SkinID, skin);

                await new MySqlCommand($"UPDATE `{PlayersGunsSkinsTableName}` SET `SkinIDs` = '{loadout.GunSkinsSearchByID.Keys.ToList().GetStringFromIntList()}' WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding gun skin with id {id} to player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        // Player Guns Charms

        public async Task AddPlayerGunCharmAsync(CSteamID steamID, ushort gunCharmID, bool isBought)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"INSERT INTO `{PlayersGunsCharmsTableName}` (`SteamID` , `CharmID` , `IsBought`) VALUES ({steamID} , {gunCharmID} , {isBought}) ON DUPLICATE KEY UPDATE `IsBought` = {isBought};", Conn).ExecuteScalarAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Couldnt find loadout for player with steam id {steamID}");
                    return;
                }

                if (!GunCharms.TryGetValue(gunCharmID, out GunCharm gunCharm))
                {
                    Logging.Debug($"Error finding gun charm with id {gunCharmID}");
                    return;
                }

                if (loadout.GunCharms.ContainsKey(gunCharmID))
                {
                    Logging.Debug($"Gun charm with id {gunCharmID} is already registered to player with steam id {steamID}");
                    loadout.GunCharms[gunCharmID].IsBought = isBought;
                    return;
                }

                var loadoutGunCharm = new LoadoutGunCharm(gunCharm, isBought);
                loadout.GunCharms.Add(gunCharmID, loadoutGunCharm);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding gun charm with id {gunCharmID} to player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task RemovePlayerGunCharmAsync(CSteamID steamID, ushort gunCharmID)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"DELETE FROM `{PlayersGunsCharmsTableName}` WHERE `SteamID` = {steamID} AND `CharmID` = {gunCharmID};", Conn).ExecuteScalarAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Couldnt find loadout for player with steam id {steamID}");
                    return;
                }

                if (!GunCharms.TryGetValue(gunCharmID, out GunCharm gunCharm))
                {
                    Logging.Debug($"Error finding gun charm with id {gunCharmID}");
                    return;
                }

                loadout.GunCharms.Remove(gunCharmID);
            } catch (Exception ex)
            {
                Logger.Log($"Error removing gun charm with id {gunCharmID} to player with steam id {steamID}");
                Logger.Log(ex);
            } finally
            {
                await Conn.CloseAsync();
            }
        }
        
        public async Task UpdatePlayerGunCharmBoughtAsync(CSteamID steamID, ushort gunCharmID, bool isBought)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                    return;
                }

                if (!loadout.GunCharms.TryGetValue(gunCharmID, out LoadoutGunCharm gunCharm))
                {
                    Logging.Debug($"Error finding loadout gun charm with id {gunCharmID} for player with steam id {steamID}");
                    return;
                }

                gunCharm.IsBought = isBought;
                await new MySqlCommand($"UPDATE `{PlayersGunsCharmsTableName}` SET `IsBought` = {isBought} WHERE `SteamID` = {steamID} AND `CharmID` = {gunCharmID};", Conn).ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error changing is bought to {isBought} for gun charm with id {gunCharmID} for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        // Player Knives

        public async Task AddPlayerKnifeAsync(CSteamID steamID, ushort knifeID, bool isBought)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                if (!Knives.TryGetValue(knifeID, out Knife knife))
                {
                    Logging.Debug($"Error finding knife with id {knifeID}");
                    return;
                }

                await new MySqlCommand($"INSERT INTO `{PlayersKnivesTableName}` (`SteamID` , `KnifeID` , `KnifeKills` , `IsBought`) VALUES ({steamID} , {knifeID} , {isBought}) ON DUPLICATE KEY UPDATE `IsBought` = {isBought};", Conn).ExecuteScalarAsync();

                var loadoutKnife = new LoadoutKnife(knife, 0, isBought);
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Couldnt finding loadout for player with steam id {steamID}");
                    return;
                }

                if (loadout.Knives.ContainsKey(knifeID))
                {
                    Logging.Debug($"Knife with id {knifeID} is already registered for player with steam id {steamID}");
                    loadout.Knives[knifeID].IsBought = isBought;
                    return;
                }

                loadout.Knives.Add(knifeID, loadoutKnife);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding knife with id {knifeID} to player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task IncreasePlayerKnifeKillsAsync(CSteamID steamID, ushort knifeID, int kills)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                    return;
                }

                if (!loadout.Knives.TryGetValue(knifeID, out LoadoutKnife knife))
                {
                    Logging.Debug($"Error finding loadout knife with id {knifeID} for player with steam id {steamID}");
                    return;
                }

                await new MySqlCommand($"UPDATE `{PlayersKnivesTableName}` SET `KnifeKills` = `KnifeKills` + {kills} WHERE `SteamID` = {steamID} AND `KnifeID` = {knifeID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"SELECT `KnifeKills` FROM `{PlayersKnivesTableName}` WHERE `SteamID` = {steamID} AND `KnifeID` = {knifeID};", Conn).ExecuteScalarAsync();
                if (obj is int newKills)
                {
                    knife.KnifeKills = newKills;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding {kills} kills to knife with id {knifeID} for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task UpdatePlayerKnifeBoughtAsync(CSteamID steamID, ushort knifeID, bool isBought)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                    return;
                }

                if (!loadout.Knives.TryGetValue(knifeID, out LoadoutKnife knife))
                {
                    Logging.Debug($"Error finding loadout knife with id {knifeID} for player with steam id {steamID}");
                    return;
                }

                knife.IsBought = isBought;
                await new MySqlCommand($"UPDATE `{PlayersKnivesTableName}` SET `IsBought` = {isBought} WHERE `SteamID` = {steamID} AND `KnifeID` = {knifeID};", Conn).ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error changing is bought to {isBought} for knife with id {knifeID} for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        // Player Perks

        public async Task AddPlayerPerkAsync(CSteamID steamID, int perkID, bool isBought)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                await new MySqlCommand($"INSERT INTO `{PlayersPerksTableName}` (`SteamID` , `PerkID` , `IsBought`) VALUES ({steamID} , {perkID} , {isBought}) ON DUPLICATE KEY UPDATE `IsBought` = {isBought};", Conn).ExecuteScalarAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Couldnt finding loadout for player with steam id {steamID}");
                    return;
                }

                if (!Perks.TryGetValue(perkID, out Perk perk))
                {
                    Logging.Debug($"Error finding perk with id {perkID}");
                    return;
                }

                if (loadout.Perks.ContainsKey(perkID))
                {
                    Logging.Debug($"Already found perk with id {perkID} registered to player with steam id {steamID}");
                    loadout.Perks[perkID].IsBought = isBought;
                    return;
                }

                var loadoutPerk = new LoadoutPerk(perk, isBought);
                loadout.Perks.Add(perkID, loadoutPerk);

            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding perk with id {perkID} to player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task UpdatePlayerPerkBoughtAsync(CSteamID steamID, int perkID, bool isBought)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                    return;
                }

                if (!loadout.Perks.TryGetValue(perkID, out LoadoutPerk perk))
                {
                    Logging.Debug($"Error finding loadout perk with id {perkID} for player with steam id {steamID}");
                    return;
                }

                perk.IsBought = isBought;
                await new MySqlCommand($"UPDATE `{PlayersPerksTableName}` SET `IsBought` = {isBought} WHERE `SteamID` = {steamID} AND `PerkID` = {perkID};", Conn).ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error changing is bought of perk with id {perkID} of player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        // Player Gadgets

        public async Task AddPlayerGadgetAsync(CSteamID steamID, ushort gadgetID, bool isBought)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                await new MySqlCommand($"INSERT INTO `{PlayersGadgetsTableName}` (`SteamID` , `GadgetID` , `GadgetKills` , `IsBought) VALUES ({steamID} , {gadgetID} , 0 , {isBought}) ON DUPLICATE KEY UPDATE `IsBought` = {isBought};", Conn).ExecuteScalarAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Couldnt finding loadout for player with steam id {steamID}");
                    return;
                }

                if (!Gadgets.TryGetValue(gadgetID, out Gadget gadget))
                {
                    Logging.Debug($"Error finding gadget with id {gadgetID}");
                    return;
                }

                if (loadout.Gadgets.ContainsKey(gadgetID))
                {
                    Logging.Debug($"Player already owns the gadget with id {gadgetID}");
                    loadout.Gadgets[gadgetID].IsBought = isBought;
                    return;
                }

                var loadoutGadget = new LoadoutGadget(gadget, 0, isBought);
                loadout.Gadgets.Add(gadgetID, loadoutGadget);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding gadget with id {gadgetID} to player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task IncreasePlayerGadgetKillsAsync(CSteamID steamID, ushort gadgetID, int kills)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                    return;
                }

                if (!loadout.Gadgets.TryGetValue(gadgetID, out LoadoutGadget gadget))
                {
                    Logging.Debug($"Error finding loadout gadget with id {gadgetID} for player with steam id {steamID}");
                    return;
                }

                await new MySqlCommand($"UPDATE `{PlayersGadgetsTableName}` SET `GadgetKills` = `GadgetKills` + {kills} WHERE `SteamID` = {steamID} AND `GadgetID` = {gadgetID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"SELECT `GadgetKills` FROM `{PlayersGadgetsTableName}` WHERE `SteamID` = {steamID} AND `GadgetID` = {gadgetID};", Conn).ExecuteScalarAsync();
                if (obj is int newKills)
                {
                    gadget.GadgetKills = newKills;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error increasing {kills} gadget kills for gadget with id {gadgetID} for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task UpdatePlayerGadgetBoughtAsync(CSteamID steamID, ushort gadgetID, bool isBought)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                    return;
                }

                if (!loadout.Gadgets.TryGetValue(gadgetID, out LoadoutGadget gadget))
                {
                    Logging.Debug($"Error finding loadout gadget with id {gadgetID} for player with steam id {steamID}");
                    return;
                }

                gadget.IsBought = isBought;
                await new MySqlCommand($"UPDATE `{PlayersGadgetsTableName}` SET `IsBought` = {isBought} WHERE `SteamID` = {steamID} AND `GadgetID` = {gadgetID};", Conn).ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error changing is bought to {isBought} for gadget with id {gadgetID} for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        // Player Killstreaks

        public async Task AddPlayerKillstreakAsync(CSteamID steamID, int killstreakID, bool isBought)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                await new MySqlCommand($"INSERT INTO `{PlayersKillstreaksTableName}` (`SteamID` , `KillstreakID` , `KillstreakKills` , `IsBought) VALUES ({steamID} , {killstreakID} , 0 , {isBought}) ON DUPLICATE KEY UPDATE `IsBought` = {isBought};", Conn).ExecuteScalarAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Couldnt finding loadout for player with steam id {steamID}");
                    return;
                }

                if (!Killstreaks.TryGetValue(killstreakID, out Killstreak killstreak))
                {
                    Logging.Debug($"Error finding killstreak with id {killstreakID}");
                    return;
                }

                if (loadout.Killstreaks.ContainsKey(killstreakID))
                {
                    Logging.Debug($"Found killstreak with id {killstreakID} already registered to player with steam id {steamID}");
                    loadout.Killstreaks[killstreakID].IsBought = isBought;
                    return;
                }

                var loadoutKillstreak = new LoadoutKillstreak(killstreak, 0, isBought);
                loadout.Killstreaks.Add(killstreakID, loadoutKillstreak);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding killstreak with id {killstreakID} to player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task IncreasePlayerKillstreakKillsAsync(CSteamID steamID, int killstreakID, int kills)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                    return;
                }

                if (!loadout.Killstreaks.TryGetValue(killstreakID, out LoadoutKillstreak killstreak))
                {
                    Logging.Debug($"Error finding loadout killstreak with id {killstreakID} for player with steam id {steamID}");
                    return;
                }

                await new MySqlCommand($"UPDATE `{PlayersKillstreaksTableName}` SET `KillstreakKills` = `KillstreakKills` + {kills} WHERE `SteamID` = {steamID} AND `KillstreakID` = {killstreakID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"SELECT `KillstreakKills` WHERE `SteamID` = {steamID} AND `KillstreakID` = {killstreakID};", Conn).ExecuteScalarAsync();
                if (obj is int newKills)
                {
                    killstreak.KillstreakKills = newKills;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error increasing {kills} kills of killstreak with id {killstreakID} for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task UpdatePlayerKillstreakBoughtAsync(CSteamID steamID, int killstreakID, bool isBought)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                    return;
                }

                if (!loadout.Killstreaks.TryGetValue(killstreakID, out LoadoutKillstreak killstreak))
                {
                    Logging.Debug($"Error finding loadout killstreak with id {killstreakID} for player with steam id {steamID}");
                    return;
                }

                killstreak.IsBought = isBought;
                await new MySqlCommand($"UPDATE `{PlayersKillstreaksTableName}` SET `IsBought` = {isBought} WHERE `SteamID` = {steamID} AND `KillstreakID` = {killstreakID};", Conn).ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error changing is bought to {isBought} for killstreak with id {killstreakID} for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        // Player Cards

        public async Task AddPlayerCardAsync(CSteamID steamID, int cardID, bool isBought)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                await new MySqlCommand($"INSERT INTO `{PlayersCardsTableName}` (`SteamID` , `CardID` , `IsBought`) VALUES ({steamID} , {cardID} , {isBought}) ON DUPLICATE KEY UPDATE `IsBought` = {isBought};", Conn).ExecuteScalarAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Couldnt finding loadout for player with steam id {steamID}");
                    return;
                }

                if (!Cards.TryGetValue(cardID, out Card card))
                {
                    Logging.Debug($"Error finding card with id {cardID}");
                    return;
                }

                if (loadout.Cards.ContainsKey(cardID))
                {
                    Logging.Debug($"Card with id {cardID} is already registered to player with steam id {steamID}");
                    loadout.Cards[cardID].IsBought = isBought;
                    return;
                }

                var loadoutCard = new LoadoutCard(card, isBought);
                loadout.Cards.Add(cardID, loadoutCard);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding card with id {cardID} to player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task UpdatePlayerCardBoughtAsync(CSteamID steamID, int cardID, bool isBought)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                    return;
                }

                if (!loadout.Cards.TryGetValue(cardID, out LoadoutCard card))
                {
                    Logging.Debug($"Error finding loadout card with id {cardID} for player with steam id {steamID}");
                    return;
                }

                card.IsBought = isBought;
                await new MySqlCommand($"UPDATE `{PlayersCardsTableName}` SET `IsBought` = {isBought} WHERE `SteamID` = {steamID} AND `CardID` = {cardID};", Conn).ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error changing is bought to {isBought} for card with id {cardID} for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task RemovePlayerCardAsync(CSteamID steamID, int cardID)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                await new MySqlCommand($"DELETE FROM `{PlayersCardsTableName}` WHERE `SteamID` = {steamID} AND `CardID` = {cardID};", Conn).ExecuteScalarAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                    return;
                }

                if (!loadout.Cards.TryGetValue(cardID, out LoadoutCard card))
                {
                    Logging.Debug($"Error finding loadout card with id {cardID} for player with steam id {steamID}");
                    return;
                }

                loadout.Cards.Remove(cardID);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error removing card with id {cardID} from player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        // Player Gloves

        public async Task AddPlayerGloveAsync(CSteamID steamID, int gloveID, bool isBought)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                await new MySqlCommand($"INSERT INTO `{PlayersGlovesTableName}` (`SteamID` , `GloveID` , `IsBought`) VALUES ({steamID} , {gloveID} , {isBought}) ON DUPLICATE KEY UPDATE `IsBought` = {isBought};", Conn).ExecuteScalarAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Couldnt finding loadout for player with steam id {steamID}");
                    return;
                }

                if (!Gloves.TryGetValue(gloveID, out Glove glove))
                {
                    Logging.Debug($"Error finding glove with id {gloveID}");
                    return;
                }

                if (loadout.Gloves.ContainsKey(gloveID))
                {
                    Logging.Debug($"Glove with id {gloveID} is already registered to player with steam id {steamID}");
                    loadout.Gloves[gloveID].IsBought = isBought;
                    return;
                }

                var loadoutGlove = new LoadoutGlove(glove, isBought);
                loadout.Gloves.Add(gloveID, loadoutGlove);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding glove with id {gloveID} to player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task UpdatePlayerGloveBoughtAsync(CSteamID steamID, int gloveID, bool isBought)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                    return;
                }

                if (!loadout.Gloves.TryGetValue(gloveID, out LoadoutGlove glove))
                {
                    Logging.Debug($"Error finding loadout glove with id {gloveID} for player with steam id {steamID}");
                    return;
                }

                glove.IsBought = isBought;
                await new MySqlCommand($"UPDATE `{PlayersGlovesTableName}` SET `IsBought` = {isBought} WHERE `SteamID` = {steamID} AND `GloveID` = {gloveID};", Conn).ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error changing is bought to {isBought} for glove with id {gloveID} for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        // Player Loadouts

        public async Task UpdatePlayerLoadoutAsync(CSteamID steamID, int loadoutID)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                    return;
                }

                if (!loadout.Loadouts.TryGetValue(loadoutID, out Loadout playerLoadout))
                {
                    Logging.Debug($"Error finding loadout with id {loadoutID} for player with steam id {steamID}");
                    return;
                }

                var loadoutData = new LoadoutData(playerLoadout);
                await new MySqlCommand($"UPDATE `{PlayersLoadoutsTableName}` SET `Loadout` = '{Plugin.Instance.DataManager.ConvertLoadoutToJson(loadoutData)}' WHERE `SteamID` = {steamID} AND `LoadoutID` = {loadoutID};", Conn).ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error updating loadout with id {loadoutID} for player with id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task UpdatePlayerLoadoutActiveAsync(CSteamID steamID, int loadoutID, bool isActive)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                {
                    Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                    return;
                }

                if (!loadout.Loadouts.TryGetValue(loadoutID, out Loadout playerLoadout))
                {
                    Logging.Debug($"Error finding loadout for player with id {loadoutID}");
                    return;
                }

                playerLoadout.IsActive = isActive;
                await new MySqlCommand($"UPDATE `{PlayersLoadoutsTableName}` SET `IsActive` = {isActive} WHERE `SteamID` = {steamID} AND `LoadoutID` = {loadoutID};", Conn).ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error setting loadout is active to {isActive} for loadout with id {loadoutID} for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        // Player Mute

        private void CheckMutedPlayers(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                foreach (var data in PlayerData.Values.Where(k => k.IsMuted).ToList())
                {
                    if (DateTime.UtcNow > data.MuteExpiry.UtcDateTime)
                    {
                        ChangePlayerMutedAsync(data.SteamID, false);
                        try
                        {
                            var profile = new Profile(data.SteamID.m_SteamID);

                            Embed embed = new(null, $"**{profile.SteamID}** was unmuted", null, "15105570", DateTime.UtcNow.ToString("s"),
                                                    new Footer(Provider.serverName, Provider.configData.Browser.Icon),
                                                    new Author(profile.SteamID, $"https://steamcommunity.com/profiles/{profile.SteamID64}/", profile.AvatarIcon.ToString()),
                                                    new Field[]
                                                    {
                                                            new Field("**Unmuter:**", $"**Mute Expired**", true),
                                                            new Field("**Time:**", DateTime.UtcNow.ToString(), true)
                                                    },
                                                    null, null);
                            if (!string.IsNullOrEmpty(Plugin.Instance.Configuration.Instance.WebhookURL))
                            {
                                DiscordManager.SendEmbed(embed, "Player Unmuted", Plugin.Instance.Configuration.Instance.WebhookURL);
                            }

                            TaskDispatcher.QueueOnMainThread(() => Utility.Say(UnturnedPlayer.FromCSteamID(data.SteamID), Plugin.Instance.Translate("Unmuted").ToRich()));
                        }
                        catch (Exception)
                        {
                            Logger.Log($"Error sending discord webhook for {data.SteamID}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error checking the muted players");
                Logger.Log(ex);
            }
        }

        // Player Leaderboard

        private async void RefreshLeaderboardData(object sender, System.Timers.ElapsedEventArgs e)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                Conn.Open();
                Logging.Debug("Refreshing leaderboard data");
                Logging.Debug("Getting server options");
                var rdr = new MySqlCommand($"SELECT * FROM `{OptionsTableName}`;", Conn).ExecuteReader();
                try
                {
                    while (rdr.Read())
                    {
                        if (!long.TryParse(rdr[0].ToString(), out long dailyLeaderboardWipeUnix))
                        {
                            continue;
                        }

                        var dailyLeaderboardWipe = DateTimeOffset.FromUnixTimeSeconds(dailyLeaderboardWipeUnix);

                        if (!long.TryParse(rdr[1].ToString(), out long weeklyLeaderboardWipeUnix))
                        {
                            continue;
                        }

                        var weeklyLeaderboardWipe = DateTimeOffset.FromUnixTimeSeconds(weeklyLeaderboardWipeUnix);

                        var dailyRanked = Utility.GetRankedRewardsFromString(rdr[2].ToString());
                        var dailyPercentile = Utility.GetPercentileRewardsFromString(rdr[3].ToString());

                        var weeklyRanked = Utility.GetRankedRewardsFromString(rdr[4].ToString());
                        var weeklyPercentile = Utility.GetPercentileRewardsFromString(rdr[5].ToString());

                        var seasonalRanked = Utility.GetRankedRewardsFromString(rdr[6].ToString());
                        var seasonalPercentile = Utility.GetPercentileRewardsFromString(rdr[7].ToString());

                        if (!float.TryParse(rdr[8].ToString(), out float xpBooster))
                        {
                            continue;
                        }

                        if (!float.TryParse(rdr[9].ToString(), out float bpBooster))
                        {
                            continue;
                        }

                        if (!long.TryParse(rdr[10].ToString(), out long xpBoosterWipeUnix))
                        {
                            continue;
                        }

                        var xpBoosterWipe = DateTimeOffset.FromUnixTimeSeconds(xpBoosterWipeUnix);

                        if (!long.TryParse(rdr[11].ToString(), out long bpBoosterWipeUnix))
                        {
                            continue;
                        }

                        var bpBoosterWipe = DateTimeOffset.FromUnixTimeSeconds(bpBoosterWipeUnix);
                        ServerOptions = new Options(dailyLeaderboardWipe, weeklyLeaderboardWipe, dailyRanked, dailyPercentile, weeklyRanked, weeklyPercentile, seasonalRanked, seasonalPercentile, xpBooster, bpBooster, xpBoosterWipe, bpBoosterWipe);
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

                Logging.Debug("Getting daily leaderboard data");
                rdr = new MySqlCommand($"SELECT `{PlayersLeaderboardDailyTableName}`.`SteamID`, `{PlayersTableName}`.`SteamName`, `{PlayersTableName}`.`Level`, `{PlayersLeaderboardDailyTableName}`.`Kills`, `{PlayersLeaderboardDailyTableName}`.`HeadshotKills`, `{PlayersLeaderboardDailyTableName}`.`Deaths` FROM `{PlayersLeaderboardDailyTableName}` INNER JOIN `{PlayersTableName}` ON `{PlayersLeaderboardDailyTableName}`.`SteamID` = `{PlayersTableName}`.`SteamID` ORDER BY (`{PlayersLeaderboardDailyTableName}`.`Kills` + `{PlayersLeaderboardDailyTableName}`.`HeadshotKills`) DESC;", Conn).ExecuteReader();
                try
                {
                    var playerDailyLeaderboard = new List<LeaderboardData>();
                    var playerDailyLeaderboardLookup = new Dictionary<CSteamID, LeaderboardData>();

                    while (rdr.Read())
                    {
                        if (!ulong.TryParse(rdr[0].ToString(), out ulong steamid))
                        {
                            continue;
                        }

                        var steamName = rdr[1].ToString();

                        if (!int.TryParse(rdr[2].ToString(), out int level))
                        {
                            continue;
                        }

                        var steamID = new CSteamID(steamid);
                        if (!int.TryParse(rdr[3].ToString(), out int kills))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[4].ToString(), out int headshotKills))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[5].ToString(), out int deaths))
                        {
                            continue;
                        }

                        var leaderboardData = new LeaderboardData(steamID, steamName, level, kills, headshotKills, deaths);

                        playerDailyLeaderboard.Add(leaderboardData);
                        playerDailyLeaderboardLookup.Add(steamID, leaderboardData);
                    }

                    Logging.Debug($"Got data for {playerDailyLeaderboard.Count} players");
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

                Logging.Debug("Getting weekly leaderboard data");
                rdr = new MySqlCommand($"SELECT `{PlayersLeaderboardWeeklyTableName}`.`SteamID`, `{PlayersTableName}`.`SteamName`, `{PlayersTableName}`.`Level`, `{PlayersLeaderboardWeeklyTableName}`.`Kills`, `{PlayersLeaderboardWeeklyTableName}`.`HeadshotKills`, `{PlayersLeaderboardWeeklyTableName}`.`Deaths` FROM `{PlayersLeaderboardWeeklyTableName}` INNER JOIN `{PlayersTableName}` ON `{PlayersLeaderboardWeeklyTableName}`.`SteamID` = `{PlayersTableName}`.`SteamID` ORDER BY (`{PlayersLeaderboardWeeklyTableName}`.`Kills` + `{PlayersLeaderboardWeeklyTableName}`.`HeadshotKills`) DESC;", Conn).ExecuteReader();
                try
                {
                    var playerWeeklyLeaderboard = new List<LeaderboardData>();
                    var playerWeeklyLeaderboardLookup = new Dictionary<CSteamID, LeaderboardData>();

                    while (rdr.Read())
                    {
                        if (!ulong.TryParse(rdr[0].ToString(), out ulong steamid))
                        {
                            continue;
                        }

                        var steamName = rdr[1].ToString();

                        if (!int.TryParse(rdr[2].ToString(), out int level))
                        {
                            continue;
                        }

                        var steamID = new CSteamID(steamid);
                        if (!int.TryParse(rdr[3].ToString(), out int kills))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[4].ToString(), out int headshotKills))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[5].ToString(), out int deaths))
                        {
                            continue;
                        }

                        var leaderboardData = new LeaderboardData(steamID, steamName, level, kills, headshotKills, deaths);

                        playerWeeklyLeaderboard.Add(leaderboardData);
                        playerWeeklyLeaderboardLookup.Add(steamID, leaderboardData);
                    }

                    Logging.Debug($"Got data for {playerWeeklyLeaderboard.Count} players");
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

                Logging.Debug("Checking if any players dont have weekly or daily leaderboard data");
                foreach (var data in PlayerData.Values)
                {
                    if (!PlayerDailyLeaderboardLookup.ContainsKey(data.SteamID))
                    {
                        var dailyLeaderboardData = new LeaderboardData(data.SteamID, data.SteamName, data.Level, 0, 0, 0);
                        PlayerDailyLeaderboard.Add(dailyLeaderboardData);
                        PlayerDailyLeaderboardLookup.Add(data.SteamID, dailyLeaderboardData);
                        await new MySqlCommand($"INSERT INTO `{PlayersLeaderboardDailyTableName}` ( `SteamID` ) VALUES ( {data.SteamID} );", Conn).ExecuteScalarAsync();
                    }

                    if (!PlayerWeeklyLeaderboardLookup.ContainsKey(data.SteamID))
                    {
                        var weeklyLeaderboardData = new LeaderboardData(data.SteamID, data.SteamName, data.Level, 0, 0, 0);
                        PlayerWeeklyLeaderboard.Add(weeklyLeaderboardData);
                        PlayerWeeklyLeaderboardLookup.Add(data.SteamID, weeklyLeaderboardData);
                        await new MySqlCommand($"INSERT INTO `{PlayersLeaderboardWeeklyTableName}` ( `SteamID` ) VALUES ( {data.SteamID} );", Conn).ExecuteScalarAsync();
                    }
                }

                Logging.Debug("Getting seasonal leaderboard data");
                rdr = new MySqlCommand($"SELECT `{PlayersLeaderboardSeasonalTableName}`.`SteamID`, `{PlayersTableName}`.`SteamName`, `{PlayersTableName}`.`Level`, `{PlayersLeaderboardSeasonalTableName}`.`Kills`, `{PlayersLeaderboardSeasonalTableName}`.`HeadshotKills`, `{PlayersLeaderboardSeasonalTableName}`.`Deaths` FROM `{PlayersLeaderboardSeasonalTableName}` INNER JOIN `{PlayersTableName}` ON `{PlayersLeaderboardSeasonalTableName}`.`SteamID` = `{PlayersTableName}`.`SteamID` ORDER BY (`{PlayersLeaderboardSeasonalTableName}`.`Kills` + `{PlayersLeaderboardSeasonalTableName}`.`HeadshotKills`) DESC;", Conn).ExecuteReader();
                try
                {
                    var playerSeasonalLeaderboard = new List<LeaderboardData>();
                    var playerSeasonalLeaderboardLookup = new Dictionary<CSteamID, LeaderboardData>();

                    while (rdr.Read())
                    {
                        if (!ulong.TryParse(rdr[0].ToString(), out ulong steamid))
                        {
                            continue;
                        }

                        var steamName = rdr[1].ToString();

                        if (!int.TryParse(rdr[2].ToString(), out int level))
                        {
                            continue;
                        }

                        var steamID = new CSteamID(steamid);
                        if (!int.TryParse(rdr[3].ToString(), out int kills))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[4].ToString(), out int headshotKills))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[5].ToString(), out int deaths))
                        {
                            continue;
                        }

                        var leaderboardData = new LeaderboardData(steamID, steamName, level, kills, headshotKills, deaths);

                        playerSeasonalLeaderboard.Add(leaderboardData);
                        playerSeasonalLeaderboardLookup.Add(steamID, leaderboardData);
                    }

                    Logging.Debug($"Got data for {playerSeasonalLeaderboard.Count} players");
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

                Logging.Debug("Getting all time leaderboard data");
                rdr = new MySqlCommand($"SELECT `SteamID`, `SteamName`, `Level`, `Kills`, `HeadshotKills`, `Deaths` FROM `{PlayersTableName}` ORDER BY (`Kills` + `HeadshotKills`) DESC;", Conn).ExecuteReader();
                try
                {
                    var playerAllTimeLeaderboardLookup = new Dictionary<CSteamID, LeaderboardData>();
                    var playerAllTimeKill = new List<LeaderboardData>();
                    var playerAllTimeLevel = new List<LeaderboardData>();
                    while (rdr.Read())
                    {
                        if (!ulong.TryParse(rdr[0].ToString(), out ulong steamid))
                        {
                            continue;
                        }

                        var steamID = new CSteamID(steamid);
                        var steamName = rdr[1].ToString();

                        if (!int.TryParse(rdr[2].ToString(), out int level))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[3].ToString(), out int kills))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[4].ToString(), out int headshotKills))
                        {
                            continue;
                        }

                        if (!int.TryParse(rdr[5].ToString(), out int deaths))
                        {
                            continue;
                        }

                        var leaderboardData = new LeaderboardData(steamID, steamName, level, kills, headshotKills, deaths);
                        playerAllTimeLeaderboardLookup.Add(steamID, leaderboardData);
                        playerAllTimeKill.Add(leaderboardData);
                        playerAllTimeLevel.Add(leaderboardData);
                    }

                    PlayerAllTimeLeaderboardLookup = playerAllTimeLeaderboardLookup;
                    PlayerAllTimeKill = playerAllTimeKill;
                    PlayerAllTimeLevel = playerAllTimeLevel;
                    PlayerAllTimeLevel.Sort((x, y) => y.Level.CompareTo(x.Level));
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

                Logging.Debug("Checking if daily leaderboard is to be wiped");
                var bulkRewards = new List<(CSteamID, List<Reward>)>();
                if (ServerOptions.DailyLeaderboardWipe < DateTimeOffset.UtcNow)
                {
                    Logging.Debug("Daily leaderboard has to be wiped, giving ranked rewards");

                    // Give all ranked rewards
                    var embed = new Embed(null, $"Last Playtest Rankings ({PlayerDailyLeaderboard.Count} Players)", null, "15105570", DateTime.UtcNow.ToString("s"),
                    new Footer(Provider.serverName, Provider.configData.Browser.Icon),
                    new Author(Provider.serverName, "", Provider.configData.Browser.Icon),
                    new Field[] { new Field($"Ranked:", "", false), new Field("Percentile:", "", false) },
                    null, null);

                    foreach (var rankedReward in ServerOptions.DailyRankedRewards)
                    {
                        Logging.Debug($"Giving ranked reward for position {rankedReward.Key}");
                        if (PlayerDailyLeaderboard.Count < (rankedReward.Key + 1))
                        {
                            Logging.Debug($"Daily leaderboard doesn't have any player at that position, breaking");
                            break;
                        }

                        var leaderboardData = PlayerDailyLeaderboard[rankedReward.Key];
                        Logging.Debug($"Giving player with steam id {leaderboardData.SteamID} the reward");
                        bulkRewards.Add(new(leaderboardData.SteamID, rankedReward.Value));
                        embed.fields[0].value += $"{Utility.GetDiscordEmoji(rankedReward.Key + 1)} [{leaderboardData.SteamName}](https://steamcommunity.com/profiles/{leaderboardData.SteamID}/) | {leaderboardData.Kills + leaderboardData.HeadshotKills} Kills \n";
                        if (rankedReward.Key == 2)
                        {
                            embed.fields[0].value += $"\n";
                        }
                    }

                    Logging.Debug("Giving percentile rewards");
                    // Give all percentile rewards
                    foreach (var percentileReward in ServerOptions.DailyPercentileRewards)
                    {
                        Logging.Debug($"Giving percentile reward with lower percentile {percentileReward.LowerPercentile} and upper percentile {percentileReward.UpperPercentile}");
                        var lowerIndex = percentileReward.LowerPercentile == 0 ? 0 : (percentileReward.LowerPercentile * PlayerDailyLeaderboard.Count / 100);
                        var upperIndex = percentileReward.UpperPercentile * PlayerDailyLeaderboard.Count / 100;

                        Logging.Debug($"Lower index {lowerIndex}, Upper Index {upperIndex}");
                        for (int i = lowerIndex; i < upperIndex; i++)
                        {
                            Logging.Debug($"i: {i}");
                            if (ServerOptions.DailyRankedRewards.ContainsKey(i))
                            {
                                Logging.Debug("Player at this position already got ranked reward, continue");
                                continue;
                            }
                            if (PlayerDailyLeaderboard.Count < (i + 1))
                            {
                                Logging.Debug("Could'nt find any player at the position i, breaking");
                                break;
                            }

                            var leaderboardData = PlayerDailyLeaderboard[i];
                            Logging.Debug($"Giving player with steam id {leaderboardData.SteamID} the percentile reward");
                            bulkRewards.Add(new(leaderboardData.SteamID, percentileReward.Rewards));
                        }

                        embed.fields[1].value += $"**Top {percentileReward.UpperPercentile}%:** {upperIndex - lowerIndex} players \n";
                    }

                    ThreadPool.QueueUserWorkItem((o) =>
                    {
                        Logging.Debug($"Sending embed with {embed.fields.Count()} fields");
                        try
                        {
                            DiscordManager.SendEmbed(embed, "Leaderboard", "https://discord.com/api/webhooks/983367340525760542/RfPxBseRKp3kffBEaHovRBRsLpIR4A-pvAXbQWzknDMohxCiawGlsZw6U_ehXukPreb_");
                        }
                        catch (Exception ex)
                        {
                            Logger.Log("Error sending embed");
                            Logger.Log(ex);
                        }
                        Logging.Debug("Sent embed");
                    });

                    // Wipe the Daily leaderboard data
                    new MySqlCommand($"DELETE FROM `{PlayersLeaderboardDailyTableName}`;", Conn).ExecuteScalar();

                    PlayerDailyLeaderboard.Clear();
                    PlayerDailyLeaderboardLookup.Clear();

                    foreach (var data in PlayerData.Values)
                    {
                        var leaderboardData = new LeaderboardData(data.SteamID, data.SteamName, data.Level, 0, 0, 0);
                        PlayerDailyLeaderboard.Add(leaderboardData);
                        PlayerDailyLeaderboardLookup.Add(data.SteamID, leaderboardData);
                    }

                    // Change the wipe date
                    var newWipeDate = DateTimeOffset.UtcNow.AddDays(1);
                    new MySqlCommand($"UPDATE `{OptionsTableName}` SET `DailyLeaderboardWipe` = {newWipeDate.ToUnixTimeSeconds()};", Conn).ExecuteScalar();
                    ServerOptions.DailyLeaderboardWipe = newWipeDate;
                }

                Logging.Debug("Checking if weekly leaderboard is to be wiped");
                if (ServerOptions.WeeklyLeaderboardWipe < DateTimeOffset.UtcNow)
                {
                    Logging.Debug("Weekly leaderboard has to be wiped, giving ranked rewards");

                    // Give all ranked rewards
                    var embed = new Embed(null, $"Last Playtest Rankings ({PlayerWeeklyLeaderboard.Count} Players)", null, "15105570", DateTime.UtcNow.ToString("s"),
                    new Footer(Provider.serverName, Provider.configData.Browser.Icon),
                    new Author(Provider.serverName, "", Provider.configData.Browser.Icon),
                    new Field[] { new Field($"Ranked:", "", false), new Field("Percentile:", "", false) },
                    null, null);

                    foreach (var rankedReward in ServerOptions.WeeklyRankedRewards)
                    {
                        Logging.Debug($"Giving ranked reward for position {rankedReward.Key}");
                        if (PlayerWeeklyLeaderboard.Count < (rankedReward.Key + 1))
                        {
                            Logging.Debug($"Weekly leaderboard doesn't have any player at that position, breaking");
                            break;
                        }

                        var leaderboardData = PlayerWeeklyLeaderboard[rankedReward.Key];
                        Logging.Debug($"Giving player with steam id {leaderboardData.SteamID} the reward");
                        bulkRewards.Add(new(leaderboardData.SteamID, rankedReward.Value));
                        embed.fields[0].value += $"{Utility.GetDiscordEmoji(rankedReward.Key + 1)} [{leaderboardData.SteamName}](https://steamcommunity.com/profiles/{leaderboardData.SteamID}/) | {leaderboardData.Kills + leaderboardData.HeadshotKills} Kills \n";
                        if (rankedReward.Key == 2)
                        {
                            embed.fields[0].value += $"\n";
                        }
                    }

                    Logging.Debug("Giving percentile rewards");
                    // Give all percentile rewards
                    foreach (var percentileReward in ServerOptions.WeeklyPercentileRewards)
                    {
                        Logging.Debug($"Giving percentile reward with lower percentile {percentileReward.LowerPercentile} and upper percentile {percentileReward.UpperPercentile}");
                        var lowerIndex = percentileReward.LowerPercentile == 0 ? 0 : (percentileReward.LowerPercentile * PlayerWeeklyLeaderboard.Count / 100);
                        var upperIndex = percentileReward.UpperPercentile * PlayerWeeklyLeaderboard.Count / 100;

                        Logging.Debug($"Lower index {lowerIndex}, Upper Index {upperIndex}");
                        for (int i = lowerIndex; i < upperIndex; i++)
                        {
                            Logging.Debug($"i: {i}");
                            if (ServerOptions.WeeklyRankedRewards.ContainsKey(i))
                            {
                                Logging.Debug("Player at this position already got ranked reward, continue");
                                continue;
                            }
                            if (PlayerWeeklyLeaderboard.Count < (i + 1))
                            {
                                Logging.Debug("Could'nt find any player at the position i, breaking");
                                break;
                            }

                            var leaderboardData = PlayerWeeklyLeaderboard[i];
                            Logging.Debug($"Giving player with steam id {leaderboardData.SteamID} the percentile reward");
                            bulkRewards.Add(new(leaderboardData.SteamID, percentileReward.Rewards));
                        }

                        embed.fields[1].value += $"**Top {percentileReward.UpperPercentile}%:** {upperIndex - lowerIndex} players \n";
                    }

                    ThreadPool.QueueUserWorkItem((o) =>
                    {
                        Logging.Debug($"Sending embed with {embed.fields.Count()} fields");
                        try
                        {
                            DiscordManager.SendEmbed(embed, "Leaderboard", "https://discord.com/api/webhooks/983367340525760542/RfPxBseRKp3kffBEaHovRBRsLpIR4A-pvAXbQWzknDMohxCiawGlsZw6U_ehXukPreb_");
                        }
                        catch (Exception ex)
                        {
                            Logger.Log("Error sending embed");
                            Logger.Log(ex);
                        }
                        Logging.Debug("Sent embed");
                    });

                    // Wipe the Weekly leaderboard data
                    new MySqlCommand($"DELETE FROM `{PlayersLeaderboardWeeklyTableName}`;", Conn).ExecuteScalar();

                    PlayerWeeklyLeaderboard.Clear();
                    PlayerWeeklyLeaderboardLookup.Clear();

                    foreach (var data in PlayerData.Values)
                    {
                        var leaderboardData = new LeaderboardData(data.SteamID, data.SteamName, data.Level, 0, 0, 0);
                        PlayerWeeklyLeaderboard.Add(leaderboardData);
                        PlayerWeeklyLeaderboardLookup.Add(data.SteamID, leaderboardData);
                    }

                    // Change the wipe date
                    var newWipeDate = DateTimeOffset.UtcNow.AddDays(7);
                    new MySqlCommand($"UPDATE `{OptionsTableName}` SET `WeeklyLeaderboardWipe` = {newWipeDate.ToUnixTimeSeconds()};", Conn).ExecuteScalar();
                    ServerOptions.WeeklyLeaderboardWipe = newWipeDate;
                }

                Logging.Debug("Checking if seasonal leaderboard is to be wiped");
                if (IsPendingSeasonalWipe)
                {
                    IsPendingSeasonalWipe = false;
                    Logging.Debug("Seasonal leaderboard has to be wiped, giving ranked rewards");

                    // Give all ranked rewards
                    var embed = new Embed(null, $"Last Playtest Rankings ({PlayerSeasonalLeaderboard.Count} Players)", null, "15105570", DateTime.UtcNow.ToString("s"),
                    new Footer(Provider.serverName, Provider.configData.Browser.Icon),
                    new Author(Provider.serverName, "", Provider.configData.Browser.Icon),
                    new Field[] { new Field($"Ranked:", "", false), new Field("Percentile:", "", false) },
                    null, null);

                    foreach (var rankedReward in ServerOptions.SeasonalRankedRewards)
                    {
                        Logging.Debug($"Giving ranked reward for position {rankedReward.Key}");
                        if (PlayerSeasonalLeaderboard.Count < (rankedReward.Key + 1))
                        {
                            Logging.Debug($"Seasonal leaderboard doesn't have any player at that position, breaking");
                            break;
                        }

                        var leaderboardData = PlayerSeasonalLeaderboard[rankedReward.Key];
                        Logging.Debug($"Giving player with steam id {leaderboardData.SteamID} the reward");
                        bulkRewards.Add(new(leaderboardData.SteamID, rankedReward.Value));
                        embed.fields[0].value += $"{Utility.GetDiscordEmoji(rankedReward.Key + 1)} [{leaderboardData.SteamName}](https://steamcommunity.com/profiles/{leaderboardData.SteamID}/) | {leaderboardData.Kills + leaderboardData.HeadshotKills} Kills \n";
                        if (rankedReward.Key == 2)
                        {
                            embed.fields[0].value += $"\n";
                        }
                    }

                    Logging.Debug("Giving percentile rewards");
                    // Give all percentile rewards
                    foreach (var percentileReward in ServerOptions.SeasonalPercentileRewards)
                    {
                        Logging.Debug($"Giving percentile reward with lower percentile {percentileReward.LowerPercentile} and upper percentile {percentileReward.UpperPercentile}");
                        var lowerIndex = percentileReward.LowerPercentile == 0 ? 0 : (percentileReward.LowerPercentile * PlayerSeasonalLeaderboard.Count / 100);
                        var upperIndex = percentileReward.UpperPercentile * PlayerSeasonalLeaderboard.Count / 100;

                        Logging.Debug($"Lower index {lowerIndex}, Upper Index {upperIndex}");
                        for (int i = lowerIndex; i < upperIndex; i++)
                        {
                            Logging.Debug($"i: {i}");
                            if (ServerOptions.SeasonalRankedRewards.ContainsKey(i))
                            {
                                Logging.Debug("Player at this position already got ranked reward, continue");
                                continue;
                            }
                            if (PlayerSeasonalLeaderboard.Count < (i + 1))
                            {
                                Logging.Debug("Could'nt find any player at the position i, breaking");
                                break;
                            }

                            var leaderboardData = PlayerSeasonalLeaderboard[i];
                            Logging.Debug($"Giving player with steam id {leaderboardData.SteamID} the percentile reward");
                            bulkRewards.Add(new(leaderboardData.SteamID, percentileReward.Rewards));
                        }

                        embed.fields[1].value += $"**Top {percentileReward.UpperPercentile}%:** {upperIndex - lowerIndex} players \n";
                    }

                    ThreadPool.QueueUserWorkItem((o) =>
                    {
                        Logging.Debug($"Sending embed with {embed.fields.Count()} fields");
                        try
                        {
                            DiscordManager.SendEmbed(embed, "Leaderboard", "https://discord.com/api/webhooks/983367340525760542/RfPxBseRKp3kffBEaHovRBRsLpIR4A-pvAXbQWzknDMohxCiawGlsZw6U_ehXukPreb_");
                        }
                        catch (Exception ex)
                        {
                            Logger.Log("Error sending embed");
                            Logger.Log(ex);
                        }
                        Logging.Debug("Sent embed");
                    });
                }

                Logging.Debug($"Sending {bulkRewards.Count} bulk rewards");
                Plugin.Instance.RewardManager.GiveBulkRewards(bulkRewards);

                Logging.Debug("Checking if quests are to be wiped");
                foreach (var data in PlayerData.Values)
                {
                    if (data.Quests[0].QuestEnd > DateTimeOffset.UtcNow)
                    {
                        continue;
                    }

                    Logging.Debug($"Quests of {data.SteamName} are to be wiped, generating new");

                    var playerQuests = new List<PlayerQuest>();
                    var playerQuestsSearchByType = new Dictionary<EQuestType, List<PlayerQuest>>();

                    await new MySqlCommand($"DELETE FROM `{PlayersQuestsTableName}` WHERE `SteamID` = {data.SteamID};", Conn).ExecuteScalarAsync();
                    var expiryDate = ServerOptions.DailyLeaderboardWipe;
                    var questsToAdd = new List<Quest>();
                    for (var i = 0; i < 6; i++)
                    {
                        Logging.Debug($"i: {i}, Tier: {(EQuestTier)i}");
                        if (i == 2) continue;
                        
                        var randomQuests = Quests.Where(k => (int)k.QuestTier == i).ToList();
                        Logging.Debug($"FOund {randomQuests.Count} random quests");
                        var randomQuest = randomQuests[UnityEngine.Random.Range(0, randomQuests.Count)];
                        questsToAdd.Add(randomQuest);
                    }

                    foreach (var quest in questsToAdd)
                    {
                        var playerQuest = new PlayerQuest(data.SteamID, quest, 0, expiryDate);
                        playerQuests.Add(playerQuest);
                        if (!playerQuestsSearchByType.ContainsKey(quest.QuestType))
                        {
                            playerQuestsSearchByType.Add(quest.QuestType, new List<PlayerQuest>());
                        }
                        playerQuestsSearchByType[quest.QuestType].Add(playerQuest);
                        await new MySqlCommand($"INSERT INTO `{PlayersQuestsTableName}` (`SteamID` , `QuestID`, `Amount`, `QuestEnd`) VALUES ({data.SteamID}, {quest.QuestID}, 0, {expiryDate.ToUnixTimeSeconds()});", Conn).ExecuteScalarAsync();
                    }

                    data.Quests = playerQuests;
                    data.QuestsSearchByType = playerQuestsSearchByType;

                    Logging.Debug($"Generated {playerQuests.Count} quests for player");
                }

                Logging.Debug("Checking if xp booster needs to be wiped");
                if (ServerOptions.XPBoosterWipe < DateTimeOffset.UtcNow && ServerOptions.XPBooster != 0f)
                {
                    ServerOptions.XPBooster = 0f;
                    await new MySqlCommand($"UPDATE `{OptionsTableName}` SET `XPBooster` = 0;", Conn).ExecuteScalarAsync();
                }

                Logging.Debug("Checking if bp booster needs to be wiped");
                if (ServerOptions.BPBoosterWipe < DateTimeOffset.UtcNow && ServerOptions.BPBooster != 0f)
                {
                    ServerOptions.BPBooster = 0f;
                    await new MySqlCommand($"UPDATE `{OptionsTableName}` SET `BPBooster` = 0;", Conn).ExecuteScalarAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error refreshing leaderboard data");
                Logger.Log(ex);
            }
            finally
            {
                Conn.Close();
            }
        }

        // Player Quest

        public async Task IncreasePlayerQuestAmountAsync(CSteamID steamID, int questID, int amount)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                await new MySqlCommand($"UPDATE `{PlayersQuestsTableName}` SET `Amount` = `Amount` + {amount} WHERE `SteamID` = {steamID} AND `QuestID` = {questID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"SELECT `Amount` FROM `{PlayersQuestsTableName}` WHERE `SteamID` = {steamID} AND `QuestID` = {questID};", Conn).ExecuteScalarAsync();

                if (obj is int newAmount)
                {
                    if (!PlayerData.TryGetValue(steamID, out PlayerData data))
                    {
                        Logger.Log($"Error finding player data for player with steam id {steamID}");
                        return;
                    }

                    var quest = data.Quests.FirstOrDefault(k => k.Quest.QuestID == questID);
                    if (quest == null)
                    {
                        Logger.Log($"Error finding quest with id {questID} for player with steam id {steamID}");
                        return;
                    }

                    quest.Amount = newAmount;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error updating player quest amount of {steamID} for quest {questID} by amount {amount}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        // Player Achievement

        public async Task UpdatePlayerAchievementTierAsync(CSteamID steamID, int achievementID, int currentTier)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                await new MySqlCommand($"UPDATE `{PlayersAchievementsTableName}` SET `CurrentTier` = {currentTier} WHERE `SteamID` = {steamID} AND `AchievementID` = {achievementID};", Conn).ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error updating player achievement tier of {steamID} for achievement {achievementID} to {currentTier}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task IncreasePlayerAchievementAmountAsync(CSteamID steamID, int achievementID, int amount)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                await new MySqlCommand($"UPDATE `{PlayersAchievementsTableName}` SET `Amount` = `Amount` + {amount} WHERE `SteamID` = {steamID} AND `AchievementID` = {achievementID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"SELECT `Amount` FROM `{PlayersAchievementsTableName}` WHERE `SteamID` = {steamID} AND `AchievementID` = {achievementID};", Conn).ExecuteScalarAsync();

                if (obj is int newAmount)
                {
                    if (!PlayerData.TryGetValue(steamID, out PlayerData data))
                    {
                        Logger.Log($"Error finding player data for player with steam id {steamID}");
                        return;
                    }

                    if (!data.AchievementsSearchByID.TryGetValue(achievementID, out PlayerAchievement achievement))
                    {
                        Logger.Log($"Error finding achievement with id {achievementID} for player with steam id {steamID}");
                        return;
                    }

                    achievement.Amount = newAmount;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error updating player achievement amount of {steamID} for achievement {achievementID} by amount {amount}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        // Player Battlepass
        public async Task IncreasePlayerBPXPAsync(CSteamID steamID, int xp)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();

                await new MySqlCommand($"UPDATE `{PlayersBattlepassTableName}` SET `XP` = `XP` + {xp} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                var obj = await new MySqlCommand($"Select `XP` FROM `{PlayersBattlepassTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();

                if (PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    if (obj is int newXp)
                    {
                        data.Battlepass.XP = newXp;
                    }

                    if (data.Battlepass.TryGetNeededXP(out int neededXP))
                    {
                        if (data.Battlepass.XP >= neededXP)
                        {
                            var newXP = data.Battlepass.XP - neededXP;
                            await new MySqlCommand($"UPDATE `{PlayersBattlepassTableName}` SET `XP` = {newXP}, `CurrentTier` = `CurrentTier` + 1 WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                            obj = await new MySqlCommand($"Select `CurrentTier` FROM `{PlayersBattlepassTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                            if (obj is int tier)
                            {
                                data.Battlepass.CurrentTier = tier;
                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    var player = Plugin.Instance.GameManager.GetGamePlayer(data.SteamID);
                                    if (player != null)
                                    {
                                        // Code to send battlepass level up
                                    }
                                });
                            }
                            data.Battlepass.XP = newXP;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding {xp} battlepass xp for player with steam id {steamID}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }

        public async Task UpdatePlayerBPClaimedFreeRewardsAsync(CSteamID steamID, int claimedFreeRewardTier)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                if (!PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    Logging.Debug($"Error finding player data for steam id {steamID}");
                    return;
                }

                if (data.Battlepass.ClaimedFreeRewards.Contains(claimedFreeRewardTier))
                {
                    Logging.Debug($"Player with steam id {steamID} has already claimed that free reward tier");
                    return;
                }

                data.Battlepass.ClaimedFreeRewards.Add(claimedFreeRewardTier);
                await new MySqlCommand($"UPDATE `{PlayersBattlepassTableName}` SET `ClaimedFreeRewards` = '{data.Battlepass.ClaimedFreeRewards.GetStringFromHashSetInt()}' WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();

            } catch (Exception ex)
            {
                Logger.Log($"Error updating claimed free rewards for player with steam id {steamID} and adding the tier {claimedFreeRewardTier}");
                Logger.Log(ex);
            } finally
            {
                await Conn.CloseAsync();
            }
        }
        
        public async Task UpdatePlayerBPClaimedPremiumRewardsAsync(CSteamID steamID, int claimedPremiumRewardTier)
        {
            using MySqlConnection Conn = new(ConnectionString);
            try
            {
                await Conn.OpenAsync();
                if (!PlayerData.TryGetValue(steamID, out PlayerData data))
                {
                    Logging.Debug($"Error finding player data for steam id {steamID}");
                    return;
                }

                if (data.Battlepass.ClaimedPremiumRewards.Contains(claimedPremiumRewardTier))
                {
                    Logging.Debug($"Player with steam id {steamID} has already claimed that free reward tier");
                    return;
                }

                data.Battlepass.ClaimedPremiumRewards.Add(claimedPremiumRewardTier);
                await new MySqlCommand($"UPDATE `{PlayersBattlepassTableName}` SET `ClaimedPremiumRewards` = '{data.Battlepass.ClaimedPremiumRewards.GetStringFromHashSetInt()}' WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();

            }
            catch (Exception ex)
            {
                Logger.Log($"Error updating claimed premium rewards for player with steam id {steamID} and adding the tier {claimedPremiumRewardTier}");
                Logger.Log(ex);
            }
            finally
            {
                await Conn.CloseAsync();
            }
        }
    }
}
