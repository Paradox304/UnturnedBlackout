using MySql.Data.MySqlClient;
using Rocket.Core.Logging;
using Rocket.Core.Steam;
using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Enums;
using UnturnedBlackout.Models.Data;
using UnturnedBlackout.Models.Webhook;
using Timer = System.Timers.Timer;

namespace UnturnedBlackout.Managers
{
    public class DatabaseManager
    {
        public string ConnectionString { get; set; }
        public Config Config { get; set; }

        public Timer m_MuteChecker { get; set; }


        // Players Data
        public Dictionary<CSteamID, PlayerData> PlayerData { get; set; }
        public Dictionary<CSteamID, PlayerLoadout> PlayerLoadouts { get; set; }

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
        public Dictionary<ushort, Glove> Gloves { get; set; }
        public Dictionary<int, Card> Cards { get; set; }
        public Dictionary<int, XPLevel> Levels { get; set; }

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

        // REGIONS
        public const string RegionsTableName = "UB_Regions";

        // SERVER OPTIONS
        public const string OptionsTableName = "UB_Options";

        public DatabaseManager()
        {
            Config = Plugin.Instance.Configuration.Instance;
            ConnectionString = $"server={Config.DatabaseHost};user={Config.DatabaseUsername};database={Config.DatabaseName};port={Config.DatabasePort};password={Config.DatabasePassword}";
            m_MuteChecker = new Timer(10 * 1000);
            m_MuteChecker.Elapsed += CheckMutedPlayers;

            PlayerData = new Dictionary<CSteamID, PlayerData>();
            PlayerLoadouts = new Dictionary<CSteamID, PlayerLoadout>();

            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await LoadDatabaseAsync();
                await GetBaseDataAsync();

                Plugin.Instance.LoadoutManager = new LoadoutManager();
                m_MuteChecker.Start();
            });
        }

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

                            Embed embed = new Embed(null, $"**{profile.SteamID}** was unmuted", null, "15105570", DateTime.UtcNow.ToString("s"),
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

        public async Task LoadDatabaseAsync()
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    // BASE DATA
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GunsTableName}` ( `GunID` SMALLINT UNSIGNED NOT NULL , `GunName` VARCHAR(255) NOT NULL , `GunDesc` TEXT NOT NULL , `GunType` ENUM('Pistol','SMG','Shotgun','LMG','AR','SNIPER') NOT NULL , `GunRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `MovementChange` DECIMAL(4,3) NOT NULL , `MovementChangeADS` DECIMAL(4,3) NOT NULL , `IconLink` TEXT NOT NULL , `MagAmount` TINYINT UNSIGNED NOT NULL , `Coins` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL ,  `ScrapAmount` INT UNSIGNED NOT NULL , `LevelRequirement` INT NOT NULL , `IsPrimary` BOOLEAN NOT NULL , `DefaultAttachments` TEXT NOT NULL , `LevelXPNeeded` TEXT NOT NULL , `LevelRewards` TEXT NOT NULL , PRIMARY KEY (`GunID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{AttachmentsTableName}` ( `AttachmentID` SMALLINT UNSIGNED NOT NULL , `AttachmentName` VARCHAR(255) NOT NULL , `AttachmentDesc` TEXT NOT NULL , `AttachmentType` ENUM('Sights','Grip','Barrel','Magazine') NOT NULL , `AttachmentRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `MovementChange` DECIMAL(4,3) NOT NULL , `MovementChangeADS` DECIMAL (4,3) NOT NULL , `IconLink` TEXT NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `Coins` INT UNSIGNED NOT NULL , PRIMARY KEY (`AttachmentID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GunsSkinsTableName}` ( `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT , `GunID` SMALLINT UNSIGNED NOT NULL , `SkinID` SMALLINT UNSIGNED NOT NULL , `SkinName` VARCHAR(255) NOT NULL , `SkinDesc` TEXT NOT NULL , `SkinRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `PatternLink` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , CONSTRAINT `ub_gun_id` FOREIGN KEY (`GunID`) REFERENCES `{GunsTableName}` (`GunID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`ID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GunsCharmsTableName}` ( `CharmID` SMALLINT UNSIGNED NOT NULL , `CharmName` VARCHAR(255) NOT NULL , `CharmDesc` TEXT NOT NULL , `CharmRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `IconLink` TEXT NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `Coins` INT UNSIGNED NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `LevelRequirement` INT NOT NULL , PRIMARY KEY (`CharmID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{KnivesTableName}` ( `KnifeID` SMALLINT UNSIGNED NOT NULL , `KnifeName` VARCHAR(255) NOT NULL , `KnifeDesc` TEXT NOT NULL , `KnifeRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `MovementChange` DECIMAL(4,3) NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `Coins` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `LevelRequirement` INT NOT NULL , PRIMARY KEY (`KnifeID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PerksTableName}` ( `PerkID` INT UNSIGNED NOT NULL , `PerkName` VARCHAR(255) NOT NULL , `PerkDesc` TEXT NOT NULL , `PerkType` ENUM('1','2','3') NOT NULL , `PerkRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `IconLink` TEXT NOT NULL , `SkillType` TEXT NOT NULL , `SkillLevel` INT UNSIGNED NOT NULL , `Coins` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `LevelRequirement` INT NOT NULL , PRIMARY KEY (`PerkID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GadgetsTableName}` ( `GadgetID` SMALLINT UNSIGNED NOT NULL , `GadgetName` VARCHAR(255) NOT NULL , `GadgetDesc` TEXT NOT NULL , `GadgetRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `IconLink` TEXT NOT NULL , `Coins` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `GiveSeconds` INT UNSIGNED NOT NULL , `LevelRequirement` INT NOT NULL , `IsTactical` BOOLEAN NOT NULL , PRIMARY KEY (`GadgetID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{KillstreaksTableName}` ( `KillstreakID` INT UNSIGNED NOT NULL , `KillstreakName` VARCHAR(255) NOT NULL , `KillstreakDesc` TEXT NOT NULL , `KillstreakRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `IconLink` TEXT NOT NULL , `KillstreakRequired` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `Coins` INT UNSIGNED NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `LevelRequirement` INT NOT NULL , PRIMARY KEY (`KillstreakID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{CardsTableName}` ( `CardID` INT UNSIGNED NOT NULL , `CardName` VARCHAR(255) NOT NULL , `CardDesc` TEXT NOT NULL , `CardRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `IconLink` TEXT NOT NULL , `CardLink` TEXT NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `Coins` INT UNSIGNED NOT NULL , `LevelRequirement` INT NOT NULL , PRIMARY KEY (`CardID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GlovesTableName}` ( `GloveID` SMALLINT UNSIGNED NOT NULL , `GloveName` VARCHAR(255) NOT NULL , `GloveDesc` TEXT NOT NULL , `GloveRarity` ENUM('NONE','COMMON','UNCOMMON','RARE','EPIC','LEGENDARY','MYTHICAL','YELLOW','ORANGE','CYAN','GREEN') NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `Coins` INT UNSIGNED NOT NULL , `LevelRequirement` INT NOT NULL , PRIMARY KEY (`GloveID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{LevelsTableName}` ( `Level` INT UNSIGNED NOT NULL , `XPNeeded` INT UNSIGNED NOT NULL , `IconLinkLarge` TEXT NOT NULL , `IconLinkMedium` TEXT NOT NULL , `IconLinkSmall` TEXT NOT NULL , PRIMARY KEY (`Level`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{RegionsTableName}` ( `RegionIdentifier` VARCHAR(10) NOT NULL , PRIMARY KEY (`RegionIdentifier`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{OptionsTableName}` ( `DailyLeaderboardWipe` BIGINT NOT NULL , `WeeklyLeaderboardWipe` BIGINT NOT NULL , `DailyLeaderboardRankedRewards` TEXT NOT NULL , `DailyLeaderboardPercentileRewards` TEXT NOT NULL , `WeeklyLeaderboardPercentileRewards` TEXT NOT NULL);", Conn).ExecuteScalarAsync();


                    // PLAYERS DATA
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `SteamName` TEXT NOT NULL , `AvatarLink` VARCHAR(200) NOT NULL , `XP` INT UNSIGNED NOT NULL DEFAULT '0' , `Level` INT UNSIGNED NOT NULL DEFAULT '1' , `Credits` INT UNSIGNED NOT NULL DEFAULT '0' , `Scrap` INT UNSIGNED NOT NULL DEFAULT '0' , `Coins` INT UNSIGNED NOT NULL DEFAULT '0' , `Kills` INT UNSIGNED NOT NULL DEFAULT '0' , `HeadshotKills` INT UNSIGNED NOT NULL DEFAULT '0' , `HighestKillstreak` INT UNSIGNED NOT NULL DEFAULT '0' , `HighestMultiKills` INT UNSIGNED NOT NULL DEFAULT '0' , `KillsConfirmed` INT UNSIGNED NOT NULL DEFAULT '0' , `KillsDenied` INT UNSIGNED NOT NULL DEFAULT '0' , `FlagsCaptured` INT UNSIGNED NOT NULL DEFAULT '0' , `FlagsSaved` INT UNSIGNED NOT NULL DEFAULT '0' , `AreasTaken` INT UNSIGNED NOT NULL DEFAULT '0' , `Deaths` INT UNSIGNED NOT NULL DEFAULT '0' , `Music` BOOLEAN NOT NULL DEFAULT TRUE , `IsMuted` BOOLEAN NOT NULL DEFAULT FALSE , `MuteExpiry` BIGINT NOT NULL , PRIMARY KEY (`SteamID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersGunsTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `GunID` SMALLINT UNSIGNED NOT NULL , `Level` INT UNSIGNED NOT NULL , `XP` INT UNSIGNED NOT NULL , `GunKills` INT UNSIGNED NOT NULL , `IsBought` BOOLEAN NOT NULL , `Attachments` TEXT NOT NULL , CONSTRAINT `ub_steam_id` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_gun_id_1` FOREIGN KEY (`GunID`) REFERENCES `{GunsTableName}` (`GunID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `GunID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersGunsSkinsTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `SkinIDs` TEXT NOT NULL , CONSTRAINT `ub_steam_id_1` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersGunsCharmsTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `CharmID` SMALLINT UNSIGNED NOT NULL , `IsBought` BOOLEAN NOT NULL , CONSTRAINT `ub_steam_id_10` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_charm_id` FOREIGN KEY (`CharmID`) REFERENCES `{GunsCharmsTableName}` (`CharmID`) ON DELETE CASCADE ON UPDATE CASCADE , Primary Key (`SteamID`, `CharmID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersKnivesTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `KnifeID` SMALLINT UNSIGNED NOT NULL , `KnifeKills` INT UNSIGNED NOT NULL , `IsBought` BOOLEAN NOT NULL , CONSTRAINT `ub_steam_id_2` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_knife_id` FOREIGN KEY (`KnifeID`) REFERENCES `{KnivesTableName}` (`KnifeID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `KnifeID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersPerksTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `PerkID` INT UNSIGNED NOT NULL , `IsBought` BOOLEAN NOT NULL , CONSTRAINT `ub_steam_id_4` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_perk_id` FOREIGN KEY (`PerkID`) REFERENCES `{PerksTableName}` (`PerkID`) ON DELETE CASCADE ON UPDATE CASCADE, PRIMARY KEY (`SteamID` , `PerkID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersGadgetsTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `GadgetID` SMALLINT UNSIGNED NOT NULL , `GadgetKills` INT UNSIGNED NOT NULL , `IsBought` BOOLEAN NOT NULL , CONSTRAINT `ub_steam_id_5` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_gadget_id` FOREIGN KEY (`GadgetID`) REFERENCES `{GadgetsTableName}` (`GadgetID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `GadgetID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersKillstreaksTableName}` (`SteamID` BIGINT UNSIGNED NOT NULL , `KillstreakID` INT UNSIGNED NOT NULl , `KillstreakKills` INT UNSIGNED NOT NULL , `IsBought` BOOLEAN NOT NULL , CONSTRAINT `ub_steam_id_6` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_killstreak_id` FOREIGN KEY (`KillstreakID`) REFERENCES `{KillstreaksTableName}` (`KillstreakID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `KillstreakID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersCardsTableName}` (`SteamID` BIGINT UNSIGNED NOT NULL , `CardID` INT UNSIGNED NOT NULL , `IsBought` BOOLEAN NOT NULL , CONSTRAINT `ub_steam_id_7` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_card_id` FOREIGN KEY (`CardID`) REFERENCES `{CardsTableName}` (`CardID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `CardID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersGlovesTableName}` (`SteamID` BIGINT UNSIGNED NOT NULL , `GloveID` SMALLINT UNSIGNED NOT NULL , `IsBought` BOOLEAN NOT NULl , CONSTRAINT `ub_steam_id_8` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_glove_id` FOREIGN KEY (`GloveID`) REFERENCES `{GlovesTableName}` (`GloveID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `GloveID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersLoadoutsTableName}` (`SteamID` BIGINT UNSIGNED NOT NULL , `LoadoutID` INT UNSIGNED NOT NULL , `IsActive` BOOLEAN NOT NULL , `Loadout` TEXT NOT NULL , CONSTRAINT `ub_steam_id_9` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID`, `LoadoutID`));", Conn).ExecuteScalarAsync();
                
                
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
        }

        public async Task GetBaseDataAsync()
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
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

                    var rdr = (MySqlDataReader)await new MySqlCommand($"SELECT `AttachmentID`, `AttachmentName`, `AttachmentDesc`, `AttachmentType`-1, `AttachmentRarity`, `MovementChange`, `MovementChangeADS`, `IconLink`, `BuyPrice`, `Coins` FROM `{AttachmentsTableName}`;", Conn).ExecuteReaderAsync();
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
                            if (!int.TryParse(rdr[3].ToString(), out int attachmentTypeInt))
                            {
                                continue;
                            }

                            var attachmentType = (EAttachment)attachmentTypeInt;
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
                            if (!int.TryParse(rdr[8].ToString(), out int buyPrice))
                            {
                                continue;
                            }
                            if (!int.TryParse(rdr[9].ToString(), out int coins))
                            {
                                continue;
                            }

                            if (!gunAttachments.ContainsKey(attachmentID))
                            {
                                gunAttachments.Add(attachmentID, new GunAttachment(attachmentID, attachmentName, attachmentDesc, attachmentType, rarity, movementChange, movementChangeADS, iconLink, buyPrice, coins));
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
                            var gun = new Gun(gunID, gunName, gunDesc, gunType, rarity, movementChange, movementChangeADS, iconLink, magAmount, coins, buyPrice, scrapAmount, levelRequirement, isPrimary, defaultAttachments, rewardAttachments, rewardAttachmentsInverse, levelXPNeeded);
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
                        var gloves = new Dictionary<ushort, Glove>();
                        while (await rdr.ReadAsync())
                        {
                            if (!ushort.TryParse(rdr[0].ToString(), out ushort gloveID))
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
        }

        public async Task AddPlayerAsync(UnturnedPlayer player, string steamName, string avatarLink)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    var regex = new Regex("<[^>]*>");
                    Logging.Debug($"Adding {steamName} to the DB");
                    await Conn.OpenAsync();
                    var cmd = new MySqlCommand($"INSERT INTO `{PlayersTableName}` ( `SteamID` , `SteamName` , `AvatarLink` , `MuteExpiry` ) VALUES ({player.CSteamID}, @name, '{avatarLink}' , {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}) ON DUPLICATE KEY UPDATE `AvatarLink` = '{avatarLink}', `SteamName` = @name;", Conn);
                    cmd.Parameters.AddWithValue("@name", steamName.ToUnrich());
                    await cmd.ExecuteScalarAsync();

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
        }

        public async Task GetPlayerDataAsync(UnturnedPlayer player)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
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
                            if (!uint.TryParse(rdr[3].ToString(), out uint xp))
                            {
                                continue;
                            }

                            if (!uint.TryParse(rdr[4].ToString(), out uint level))
                            {
                                continue;
                            }

                            if (!uint.TryParse(rdr[5].ToString(), out uint credits))
                            {
                                continue;
                            }

                            if (!uint.TryParse(rdr[6].ToString(), out uint scrap))
                            {
                                continue;
                            }

                            if (!uint.TryParse(rdr[7].ToString(), out uint coins))
                            {
                                continue;
                            }

                            if (!uint.TryParse(rdr[8].ToString(), out uint kills))
                            {
                                continue;
                            }

                            if (!uint.TryParse(rdr[9].ToString(), out uint headshotKills))
                            {
                                continue;
                            }

                            if (!uint.TryParse(rdr[10].ToString(), out uint highestKillstreak))
                            {
                                continue;
                            }

                            if (!uint.TryParse(rdr[11].ToString(), out uint highestMultiKills))
                            {
                                continue;
                            }

                            if (!uint.TryParse(rdr[12].ToString(), out uint killsConfirmed))
                            {
                                continue;
                            }

                            if (!uint.TryParse(rdr[13].ToString(), out uint killsDenied))
                            {
                                continue;
                            }

                            if (!uint.TryParse(rdr[14].ToString(), out uint flagsCaptured))
                            {
                                continue;
                            }

                            if (!uint.TryParse(rdr[15].ToString(), out uint flagsSaved))
                            {
                                continue;
                            }

                            if (!uint.TryParse(rdr[16].ToString(), out uint areasTaken))
                            {
                                continue;
                            }

                            if (!uint.TryParse(rdr[17].ToString(), out uint deaths))
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

                            if (PlayerData.ContainsKey(player.CSteamID))
                            {
                                PlayerData.Remove(player.CSteamID);
                            }

                            PlayerData.Add(player.CSteamID, new PlayerData(player.CSteamID, steamName, avatarLink, xp, level, credits, scrap, coins, kills, headshotKills, highestKillstreak, highestMultiKills, killsConfirmed, killsDenied, flagsCaptured, flagsSaved, areasTaken, deaths, music, isMuted, muteExpiry));
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

                    if (!PlayerData.ContainsKey(player.CSteamID))
                    {
                        Logging.Debug("Error finding player data, returning");
                        return;
                    }

                    Dictionary<ushort, LoadoutGun> guns = new Dictionary<ushort, LoadoutGun>();
                    Dictionary<ushort, LoadoutGunCharm> gunCharms = new Dictionary<ushort, LoadoutGunCharm>();
                    Dictionary<ushort, LoadoutKnife> knives = new Dictionary<ushort, LoadoutKnife>();
                    Dictionary<int, GunSkin> gunSkinsSearchByID = new Dictionary<int, GunSkin>();
                    Dictionary<ushort, List<GunSkin>> gunSkinsSearchByGunID = new Dictionary<ushort, List<GunSkin>>();
                    Dictionary<ushort, GunSkin> gunSkinsSearchBySkinID = new Dictionary<ushort, GunSkin>();
                    Dictionary<int, LoadoutPerk> perks = new Dictionary<int, LoadoutPerk>();
                    Dictionary<ushort, LoadoutGadget> gadgets = new Dictionary<ushort, LoadoutGadget>();
                    Dictionary<int, LoadoutKillstreak> killstreaks = new Dictionary<int, LoadoutKillstreak>();
                    Dictionary<int, LoadoutCard> cards = new Dictionary<int, LoadoutCard>();
                    Dictionary<ushort, LoadoutGlove> gloves = new Dictionary<ushort, LoadoutGlove>();
                    Dictionary<int, Loadout> loadouts = new Dictionary<int, Loadout>();

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
                            if (!ushort.TryParse(rdr[1].ToString(), out ushort gloveID))
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
        }

        public async Task IncreasePlayerXPAsync(CSteamID steamID, uint xp)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();

                    await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `XP` = `XP` + {xp} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    var obj = await new MySqlCommand($"Select `XP` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();

                    if (PlayerData.TryGetValue(steamID, out PlayerData data))
                    {
                        if (obj is uint newXp)
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
                                if (obj is uint level)
                                {
                                    data.Level = level;
                                    TaskDispatcher.QueueOnMainThread(() =>
                                    {
                                        var player = Plugin.Instance.GameManager.GetGamePlayer(data.SteamID);
                                        if (player != null)
                                        {
                                            Plugin.Instance.UIManager.SendLevelUpAnimation(player, level);
                                        }
                                    });
                                }
                                data.XP = (uint)newXP;
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
        }

        public async Task IncreasePlayerCreditsAsync(CSteamID steamID, uint credits)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();

                    await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `Credits` = `Credits` + {credits} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    var obj = await new MySqlCommand($"Select `Credits` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    if (PlayerData.TryGetValue(steamID, out PlayerData data))
                    {
                        if (obj is uint newCredits)
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
        }

        public async Task DecreasePlayerCreditsAsync(CSteamID steamID, uint credits)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();

                    await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `Credits` = `Credits` - {credits} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    var obj = await new MySqlCommand($"Select `Credits` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    if (PlayerData.TryGetValue(steamID, out PlayerData data))
                    {
                        if (obj is uint newCredits)
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
        }

        public async Task IncreasePlayerScrapAsync(CSteamID steamID, uint scrap)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();

                    await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `Scrap` = `Scrap` + {scrap} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    var obj = await new MySqlCommand($"Select `Scrap` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    if (PlayerData.TryGetValue(steamID, out PlayerData data))
                    {
                        if (obj is uint newScrap)
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
        }

        public async Task DecreasePlayerScrapAsync(CSteamID steamID, uint scrap)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();

                    await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `Scrap` = `Scrap` - {scrap} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    var obj = await new MySqlCommand($"Select `Scrap` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    if (PlayerData.TryGetValue(steamID, out PlayerData data))
                    {
                        if (obj is uint newScrap)
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
        }

        public async Task IncreasePlayerCoinsAsync(CSteamID steamID, uint coins)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();

                    await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `Coins` = `Coins` + {coins} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    var obj = await new MySqlCommand($"Select `Coins` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    if (PlayerData.TryGetValue(steamID, out PlayerData data))
                    {
                        if (obj is uint newCoins)
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
        }

        public async Task DecreasePlayerCoinsAsync(CSteamID steamID, uint coins)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();

                    await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `Coins` = `Coins` - {coins} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    var obj = await new MySqlCommand($"Select `Coins` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    if (PlayerData.TryGetValue(steamID, out PlayerData data))
                    {
                        if (obj is uint newCoins)
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
        }

        public async Task IncreasePlayerKillsAsync(CSteamID steamID, uint kills)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();

                    await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `Kills` = `Kills` + {kills} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    var obj = await new MySqlCommand($"Select `Kills` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();

                    if (PlayerData.TryGetValue(steamID, out PlayerData data))
                    {
                        if (obj is uint newKills)
                        {
                            data.Kills = newKills;
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
        }

        public async Task IncreasePlayerHeadshotKillsAsync(CSteamID steamID, uint headshotKills)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();

                    await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `HeadshotKills` = `HeadshotKills` + {headshotKills} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    var obj = await new MySqlCommand($"Select `HeadshotKills` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    if (PlayerData.TryGetValue(steamID, out PlayerData data))
                    {
                        if (obj is uint newHeadshotKills)
                        {
                            data.HeadshotKills = newHeadshotKills;
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
        }

        public async Task UpdatePlayerHighestKillStreakAsync(CSteamID steamID, uint killStreak)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
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
        }

        public async Task UpdatePlayerHighestMultiKillsAsync(CSteamID steamID, uint multiKills)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
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
        }

        public async Task IncreasePlayerKillsConfirmedAsync(CSteamID steamID, uint killsConfirmed)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();

                    await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `KillsConfirmed` = `KillsConfirmed` + {killsConfirmed} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    var obj = await new MySqlCommand($"Select `KillsConfirmed` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    if (PlayerData.TryGetValue(steamID, out PlayerData data))
                    {
                        if (obj is uint newKillsConfirmed)
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
        }

        public async Task IncreasePlayerKillsDeniedAsync(CSteamID steamID, uint killsDenied)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();

                    await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `KillsDenied` = `KillsDenied` + {killsDenied} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    var obj = await new MySqlCommand($"Select `KillsDenied` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    if (PlayerData.TryGetValue(steamID, out PlayerData data))
                    {
                        if (obj is uint newKillsDenied)
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
        }

        public async Task IncreasePlayerFlagsCapturedAsync(CSteamID steamID, uint flagsCaptured)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();

                    await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `FlagsCaptured` = `FlagsCaptured` + {flagsCaptured} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    var obj = await new MySqlCommand($"Select `FlagsCaptured` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    if (PlayerData.TryGetValue(steamID, out PlayerData data))
                    {
                        if (obj is uint newFlagsCaptured)
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
        }

        public async Task IncreasePlayerFlagsSavedAsync(CSteamID steamID, uint flagsSaved)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();

                    await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `FlagsSaved` = `FlagsSaved` + {flagsSaved} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    var obj = await new MySqlCommand($"Select `FlagsSaved` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    if (PlayerData.TryGetValue(steamID, out PlayerData data))
                    {
                        if (obj is uint newFlagsSaved)
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
        }

        public async Task IncreasePlayerAreasTakenAsync(CSteamID steamID, uint areasTaken)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();

                    await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `AreasTaken` = `AreasTaken` + {areasTaken} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    var obj = await new MySqlCommand($"Select `AreasTaken` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    if (PlayerData.TryGetValue(steamID, out PlayerData data))
                    {
                        if (obj is uint newArenasTaken)
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
        }

        public async Task IncreasePlayerDeathsAsync(CSteamID steamID, uint deaths)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();

                    await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `Deaths` = `Deaths` + {deaths} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    var obj = await new MySqlCommand($"Select `Deaths` FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    if (PlayerData.TryGetValue(steamID, out PlayerData data))
                    {
                        if (obj is uint newDeaths)
                        {
                            data.Deaths = newDeaths;
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
        }

        public async Task ChangePlayerMusicAsync(CSteamID steamID, bool isMusic)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
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
        }

        public async Task ChangePlayerMutedAsync(CSteamID steamID, bool isMuted)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
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
        }

        public async Task ChangePlayerMuteExpiryAsync(CSteamID steamID, DateTimeOffset muteExpiry)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
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
        }

        // Player Guns

        public async Task AddPlayerGunAsync(CSteamID steamID, ushort gunID, bool isBought)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!Guns.TryGetValue(gunID, out Gun gun))
                    {
                        Logging.Debug($"Error finding gun with id {gunID} to add to {steamID}");
                        throw new Exception();
                    }

                    await new MySqlCommand($"INSERT INTO `{PlayersGunsTableName}` (`SteamID` , `GunID` , `Level` , `XP` , `GunKills` , `IsBought` , `Attachments`) VALUES ({steamID} , {gunID} , 1 , 0 , 0 , {isBought} , '{Utility.CreateStringFromDefaultAttachments(gun.DefaultAttachments) + Utility.CreateStringFromRewardAttachments(gun.RewardAttachments.Values.ToList())}');", Conn).ExecuteScalarAsync();
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

                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (loadout.Guns.ContainsKey(loadoutGun.Gun.GunID))
                    {
                        Logging.Debug($"{steamID} has already gun with id {gunID} registered, ignoring it");
                        throw new Exception();
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
        }

        public async Task IncreasePlayerGunXPAsync(CSteamID steamID, ushort gunID, int xp)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    await new MySqlCommand($"UPDATE `{PlayersGunsTableName}` SET `XP` = `XP` + {xp} WHERE `SteamID` = {steamID} AND `GunID` = {gunID};", Conn).ExecuteScalarAsync();
                    var obj = await new MySqlCommand($"SELECT `XP` FROM `{PlayersGunsTableName}` WHERE `SteamID` = {steamID} AND `GunID` = {gunID};", Conn).ExecuteScalarAsync();
                    if (obj is uint newXP)
                    {
                        if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                        {
                            Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                            throw new Exception();
                        }
                        if (!loadout.Guns.TryGetValue(gunID, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding loadout gun with id {gunID} for player with steam id {steamID}");
                            throw new Exception();
                        }

                        gun.XP = (int)newXP;
                        if (gun.TryGetNeededXP(out int neededXP))
                        {
                            if (gun.XP >= neededXP)
                            {
                                var updatedXP = gun.XP - neededXP;
                                await new MySqlCommand($"UPDATE `{PlayersGunsTableName}` SET `XP` = {updatedXP}, `Level` = `Level` + 1 WHERE `SteamID` = {steamID} AND `GunID` = {gunID};", Conn).ExecuteScalarAsync();
                                obj = await new MySqlCommand($"SELECT `Level` FROM `{PlayersGunsTableName}` WHERE `SteamID` = {steamID} AND `GunID` = {gunID};", Conn).ExecuteScalarAsync();
                                if (obj is uint newLevel)
                                {
                                    gun.Level = (int)newLevel;
                                }
                                gun.XP = updatedXP;

                                TaskDispatcher.QueueOnMainThread(() =>
                                {
                                    var player = Plugin.Instance.GameManager.GetGamePlayer(steamID);
                                    if (player != null)
                                    {
                                        Plugin.Instance.UIManager.SendGunLevelUpAnimation(player, gun);
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
        }

        public async Task IncreasePlayerGunKillsAsync(CSteamID steamID, ushort gunID, int kills)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
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
                            throw new Exception();
                        }

                        if (!loadout.Guns.TryGetValue(gunID, out LoadoutGun gun))
                        {
                            Logging.Debug($"Error finding loadout gun with id {gunID} for player with steam id {steamID}");
                            throw new Exception();
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
        }

        public async Task UpdatePlayerGunBoughtAsync(CSteamID steamID, ushort gunID, bool isBought)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    await new MySqlCommand($"UPDATE `{PlayersGunsTableName}` SET `IsBought` = {isBought} WHERE `SteamID` = {steamID} AND `GunID` = {gunID};", Conn).ExecuteScalarAsync();

                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!loadout.Guns.TryGetValue(gunID, out LoadoutGun gun))
                    {
                        Logging.Debug($"Error finding loadout gun with id {gunID} for player with steam id {steamID}");
                        throw new Exception();
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
        }

        // Player Guns Attachments

        public async Task UpdatePlayerGunAttachmentBoughtAsync(CSteamID steamID, ushort gunID, ushort attachmentID, bool isBought)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();

                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!loadout.Guns.TryGetValue(gunID, out LoadoutGun gun))
                    {
                        Logging.Debug($"Error finding loadout gun with id {gunID} for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!gun.Attachments.TryGetValue(attachmentID, out LoadoutAttachment attachment))
                    {
                        Logging.Debug($"Error finding loadout attachment with id {attachmentID} for loadout gun with id {gunID} for player with steam id {steamID}");
                        throw new Exception();
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
        }

        // Player Guns Skins

        public async Task AddPlayerGunSkinAsync(CSteamID steamID, int id)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!GunSkinsSearchByID.TryGetValue(id, out GunSkin skin))
                    {
                        Logging.Debug($"Error finding gun skin with id {id}");
                        throw new Exception();
                    }

                    if (loadout.GunSkinsSearchByID.ContainsKey(id))
                    {
                        Logging.Debug($"Found gun skin with id {id} already registered to player with steam id {steamID}");
                        throw new Exception();
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
        }

        // Player Guns Charms

        public async Task AddPlayerGunCharmAsync(CSteamID steamID, ushort gunCharmID, bool isBought)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!GunCharms.TryGetValue(gunCharmID, out GunCharm gunCharm))
                    {
                        Logging.Debug($"Error finding gun charm with id {gunCharmID}");
                        throw new Exception();
                    }

                    if (loadout.GunCharms.ContainsKey(gunCharmID))
                    {
                        Logging.Debug($"Gun charm with id {gunCharmID} is already registered to player with steam id {steamID}");
                        throw new Exception();
                    }

                    var loadoutGunCharm = new LoadoutGunCharm(gunCharm, isBought);
                    loadout.GunCharms.Add(gunCharmID, loadoutGunCharm);

                    await new MySqlCommand($"INSERT INTO `{PlayersGunsCharmsTableName}` (`SteamID` , `CharmID` , `IsBought`) VALUES ({steamID} , {gunCharmID} , {isBought});", Conn).ExecuteScalarAsync();
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
        }

        public async Task UpdatePlayerGunCharmBoughtAsync(CSteamID steamID, ushort gunCharmID, bool isBought)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!loadout.GunCharms.TryGetValue(gunCharmID, out LoadoutGunCharm gunCharm))
                    {
                        Logging.Debug($"Error finding loadout gun charm with id {gunCharmID} for player with steam id {steamID}");
                        throw new Exception();
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
        }

        // Player Knives

        public async Task AddPlayerKnifeAsync(CSteamID steamID, ushort knifeID, bool isBought)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!Knives.TryGetValue(knifeID, out Knife knife))
                    {
                        Logging.Debug($"Error finding knife with id {knifeID}");
                        throw new Exception();
                    }

                    await new MySqlCommand($"INSERT INTO `{PlayersKnivesTableName}` (`SteamID` , `KnifeID` , `KnifeKills` , `IsBought`) VALUES ({steamID} , {knifeID} , {isBought});", Conn).ExecuteScalarAsync();

                    var loadoutKnife = new LoadoutKnife(knife, 0, isBought);
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (loadout.Knives.ContainsKey(knifeID))
                    {
                        Logging.Debug($"Knife with id {knifeID} is already registered for player with steam id {steamID}");
                        throw new Exception();
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
        }

        public async Task IncreasePlayerKnifeKillsAsync(CSteamID steamID, ushort knifeID, int kills)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!loadout.Knives.TryGetValue(knifeID, out LoadoutKnife knife))
                    {
                        Logging.Debug($"Error finding loadout knife with id {knifeID} for player with steam id {steamID}");
                        throw new Exception();
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
        }

        public async Task UpdatePlayerKnifeBoughtAsync(CSteamID steamID, ushort knifeID, bool isBought)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!loadout.Knives.TryGetValue(knifeID, out LoadoutKnife knife))
                    {
                        Logging.Debug($"Error finding loadout knife with id {knifeID} for player with steam id {steamID}");
                        throw new Exception();
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
        }

        // Player Perks

        public async Task AddPlayerPerkAsync(CSteamID steamID, int perkID, bool isBought)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!Perks.TryGetValue(perkID, out Perk perk))
                    {
                        Logging.Debug($"Error finding perk with id {perkID}");
                        throw new Exception();
                    }

                    if (loadout.Perks.ContainsKey(perkID))
                    {
                        Logging.Debug($"Already found perk with id {perkID} registered to player with steam id {steamID}");
                        throw new Exception();
                    }

                    var loadoutPerk = new LoadoutPerk(perk, isBought);
                    loadout.Perks.Add(perkID, loadoutPerk);
                    await new MySqlCommand($"INSERT INTO `{PlayersPerksTableName}` (`SteamID` , `PerkID` , `IsBought`) VALUES ({steamID} , {perkID} , {isBought});", Conn).ExecuteScalarAsync();
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
        }

        public async Task UpdatePlayerPerkBoughtAsync(CSteamID steamID, int perkID, bool isBought)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!loadout.Perks.TryGetValue(perkID, out LoadoutPerk perk))
                    {
                        Logging.Debug($"Error finding loadout perk with id {perkID} for player with steam id {steamID}");
                        throw new Exception();
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
        }

        // Player Gadgets

        public async Task AddPlayerGadgetAsync(CSteamID steamID, ushort gadgetID, bool isBought)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!Gadgets.TryGetValue(gadgetID, out Gadget gadget))
                    {
                        Logging.Debug($"Error finding gadget with id {gadgetID}");
                        throw new Exception();
                    }

                    var loadoutGadget = new LoadoutGadget(gadget, 0, isBought);
                    loadout.Gadgets.Add(gadgetID, loadoutGadget);

                    await new MySqlCommand($"INSERT INTO `{PlayersGadgetsTableName}` (`SteamID` , `GadgetID` , `GadgetKills` , `IsBought) VALUES ({steamID} , {gadgetID} , 0 , {(isBought ? 0 : 1)});", Conn).ExecuteScalarAsync();
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
        }

        public async Task IncreasePlayerGadgetKillsAsync(CSteamID steamID, ushort gadgetID, int kills)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!loadout.Gadgets.TryGetValue(gadgetID, out LoadoutGadget gadget))
                    {
                        Logging.Debug($"Error finding loadout gadget with id {gadgetID} for player with steam id {steamID}");
                        throw new Exception();
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
        }

        public async Task UpdatePlayerGadgetBoughtAsync(CSteamID steamID, ushort gadgetID, bool isBought)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!loadout.Gadgets.TryGetValue(gadgetID, out LoadoutGadget gadget))
                    {
                        Logging.Debug($"Error finding loadout gadget with id {gadgetID} for player with steam id {steamID}");
                        throw new Exception();
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
        }

        // Player Killstreaks

        public async Task AddPlayerKillstreakAsync(CSteamID steamID, int killstreakID, bool isBought)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!Killstreaks.TryGetValue(killstreakID, out Killstreak killstreak))
                    {
                        Logging.Debug($"Error finding killstreak with id {killstreakID}");
                        throw new Exception();
                    }

                    if (loadout.Killstreaks.ContainsKey(killstreakID))
                    {
                        Logging.Debug($"Found killstreak with id {killstreakID} already registered to player with steam id {steamID}");
                        throw new Exception();
                    }

                    var loadoutKillstreak = new LoadoutKillstreak(killstreak, 0, isBought);
                    loadout.Killstreaks.Add(killstreakID, loadoutKillstreak);
                    await new MySqlCommand($"INSERT INTO `{PlayersKillstreaksTableName}` (`SteamID` , `KillstreakID` , `KillstreakKills` , `IsBought) VALUES ({steamID} , {killstreakID} , 0 , {isBought});", Conn).ExecuteScalarAsync();
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
        }

        public async Task IncreasePlayerKillstreakKillsAsync(CSteamID steamID, int killstreakID, int kills)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!loadout.Killstreaks.TryGetValue(killstreakID, out LoadoutKillstreak killstreak))
                    {
                        Logging.Debug($"Error finding loadout killstreak with id {killstreakID} for player with steam id {steamID}");
                        throw new Exception();
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
        }

        public async Task UpdatePlayerKillstreakBoughtAsync(CSteamID steamID, int killstreakID, bool isBought)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!loadout.Killstreaks.TryGetValue(killstreakID, out LoadoutKillstreak killstreak))
                    {
                        Logging.Debug($"Error finding loadout killstreak with id {killstreakID} for player with steam id {steamID}");
                        throw new Exception();
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
        }

        // Player Cards

        public async Task AddPlayerCardAsync(CSteamID steamID, int cardID, bool isBought)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!Cards.TryGetValue(cardID, out Card card))
                    {
                        Logging.Debug($"Error finding card with id {cardID}");
                        throw new Exception();
                    }

                    if (loadout.Cards.ContainsKey(cardID))
                    {
                        Logging.Debug($"Card with id {cardID} is already registered to player with steam id {steamID}");
                        throw new Exception();
                    }

                    var loadoutCard = new LoadoutCard(card, isBought);
                    loadout.Cards.Add(cardID, loadoutCard);

                    await new MySqlCommand($"INSERT INTO `{PlayersCardsTableName}` (`SteamID` , `CardID` , `IsBought`) VALUES ({steamID} , {cardID} , {isBought});", Conn).ExecuteScalarAsync();
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
        }

        public async Task UpdatePlayerCardBoughtAsync(CSteamID steamID, int cardID, bool isBought)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!loadout.Cards.TryGetValue(cardID, out LoadoutCard card))
                    {
                        Logging.Debug($"Error finding loadout card with id {cardID} for player with steam id {steamID}");
                        throw new Exception();
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
        }

        // Player Gloves

        public async Task AddPlayerGloveAsync(CSteamID steamID, ushort gloveID, bool isBought)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!Gloves.TryGetValue(gloveID, out Glove glove))
                    {
                        Logging.Debug($"Error finding glove with id {gloveID}");
                        throw new Exception();
                    }

                    if (loadout.Gloves.ContainsKey(gloveID))
                    {
                        Logging.Debug($"Glove with id {gloveID} is already registered to player with steam id {steamID}");
                        throw new Exception();
                    }

                    var loadoutGlove = new LoadoutGlove(glove, isBought);
                    loadout.Gloves.Add(gloveID, loadoutGlove);

                    await new MySqlCommand($"INSERT INTO `{PlayersGlovesTableName}` (`SteamID` , `GloveID` , `IsBought`) VALUES ({steamID} , {gloveID} , {isBought});", Conn).ExecuteScalarAsync();
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
        }

        public async Task UpdatePlayerGloveBoughtAsync(CSteamID steamID, ushort gloveID, bool isBought)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!loadout.Gloves.TryGetValue(gloveID, out LoadoutGlove glove))
                    {
                        Logging.Debug($"Error finding loadout glove with id {gloveID} for player with steam id {steamID}");
                        throw new Exception();
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
        }

        // Player Loadouts

        public async Task UpdatePlayerLoadoutAsync(CSteamID steamID, int loadoutID)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!loadout.Loadouts.TryGetValue(loadoutID, out Loadout playerLoadout))
                    {
                        Logging.Debug($"Error finding loadout with id {loadoutID} for player with steam id {steamID}");
                        throw new Exception();
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
        }

        public async Task UpdatePlayerLoadoutActiveAsync(CSteamID steamID, int loadoutID, bool isActive)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    if (!PlayerLoadouts.TryGetValue(steamID, out PlayerLoadout loadout))
                    {
                        Logging.Debug($"Error finding loadout for player with steam id {steamID}");
                        throw new Exception();
                    }

                    if (!loadout.Loadouts.TryGetValue(loadoutID, out Loadout playerLoadout))
                    {
                        Logging.Debug($"Error finding loadout for player with id {loadoutID}");
                        throw new Exception();
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
        }
    }
}
