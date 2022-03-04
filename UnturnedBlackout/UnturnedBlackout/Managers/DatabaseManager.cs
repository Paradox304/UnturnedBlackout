using MySql.Data.MySqlClient;
using Rocket.Core.Logging;
using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Data;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Enums;
using System.Linq;
using UnturnedBlackout.Models.Data;

namespace UnturnedBlackout.Managers
{
    public class DatabaseManager
    {
        public string ConnectionString { get; set; }
        public Config Config { get; set; }

        // Players Data
        public Dictionary<CSteamID, PlayerData> PlayerData { get; set; }
        public Dictionary<CSteamID, PlayerLoadout> PlayerLoadouts { get; set; }

        // Base Data
        public Dictionary<ushort, Gun> Guns { get; set; }
        public Dictionary<ushort, GunAttachment> GunAttachments { get; set; }
        public Dictionary<int, GunSkin> GunSkinsSearchByID { get; set; }
        public Dictionary<ushort, List<GunSkin>> GunSkinsSearchByGunID { get; set; }
        public Dictionary<ushort, GunSkin> GunSkinsSearchBySkinID { get; set; }
        
        public Dictionary<ushort, Knife> Knives { get; set; }
        public Dictionary<int, KnifeSkin> KnifeSkinsSearchByID { get; set; }
        public Dictionary<ushort, List<KnifeSkin>> KnifeSkinsSearchByKnifeID { get; set; }
        public Dictionary<ushort, KnifeSkin> KnifeSkinsSearchBySkinID { get; set; }

        public Dictionary<ushort, Gadget> Gadgets { get; set; }
        public Dictionary<int, Killstreak> Killstreaks { get; set; }
        public Dictionary<int, Perk> Perks { get; set; }
        public Dictionary<ushort, Glove> Gloves { get; set; }
        public Dictionary<int, Card> Cards { get; set; }

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

        // KNIVES
        public const string PlayersKnivesTableName = "UB_Players_Knives";
        public const string PlayersKnivesSkinsTableName = "UB_Players_Knives_Skins";

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

        // KNIVES
        public const string KnivesTableName = "UB_Knives";
        public const string KnivesSkinsTableName = "UB_Knives_Skins";

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

        public DatabaseManager()
        {
            Config = Plugin.Instance.Configuration.Instance;
            ConnectionString = $"server={Config.DatabaseHost};user={Config.DatabaseUsername};database={Config.DatabaseName};port={Config.DatabasePort};password={Config.DatabasePassword}";

            PlayerData = new Dictionary<CSteamID, PlayerData>();
            PlayerLoadouts = new Dictionary<CSteamID, PlayerLoadout>();

            ThreadPool.QueueUserWorkItem(async (o) =>
            {
                await LoadDatabaseAsync();
                await GetBaseDataAsync();
            });
        }

        public async Task LoadDatabaseAsync()
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    // BASE DATA
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GunsTableName}` ( `GunID` SMALLINT UNSIGNED NOT NULL , `GunName` VARCHAR(255) NOT NULL , `GunDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `MagAmount` TINYINT UNSIGNED NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , `IsPrimary` BOOLEAN NOT NULL , `DefaultAttachments` TEXT NOT NULL , `MaxLevel` INT UNSIGNED NOT NULL , `LevelXPNeeded` TEXT NOT NULL , `LevelRewards` TEXT NOT NULL , PRIMARY KEY (`GunID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{AttachmentsTableName}` ( `AttachmentID` SMALLINT UNSIGNED NOT NULL , `AttachmentName` VARCHAR(255) NOT NULL , `AttachmentDesc` TEXT NOT NULL , `AttachmentType` ENUM('Sights','Grip','Tactical','Barrel','Magazine') NOT NULL , `IconLink` TEXT NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , PRIMARY KEY (`AttachmentID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GunsSkinsTableName}` ( `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT , `GunID` SMALLINT UNSIGNED NOT NULL , `SkinID` SMALLINT UNSIGNED NOT NULL , `SkinName` VARCHAR(255) NOT NULL , `SkinDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , CONSTRAINT `ub_gun_id` FOREIGN KEY (`GunID`) REFERENCES `{GunsTableName}` (`GunID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`ID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{KnivesTableName}` ( `KnifeID` SMALLINT UNSIGNED NOT NULL , `KnifeName` VARCHAR(255) NOT NULL , `KnifeDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , PRIMARY KEY (`KnifeID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{KnivesSkinsTableName}` ( `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT , `KnifeID` SMALLINT UNSIGNED NOT NULL , `SkinID` SMALLINT UNSIGNED NOT NULL , `SkinName` VARCHAR(255) NOT NULL , `SkinDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , CONSTRAINT `ub_knife_id` FOREIGN KEY (`KnifeID`) REFERENCES `{KnivesTableName}` (`KnifeID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`ID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PerksTableName}` ( `PerkID` INT UNSIGNED NOT NULL , `PerkName` VARCHAR(255) NOT NULL , `PerkDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `SkillType` ENUM('OVERKILL','SHARPSHOOTER','DEXTERITY','CARDIO','EXERCISE','DIVING','PARKOUR','SNEAKYBEAKY','TOUGHNESS') NOT NULL , `SkillLevel` INT UNSIGNED NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , PRIMARY KEY (`PerkID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GadgetsTableName}` ( `GadgetID` SMALLINT UNSIGNED NOT NULL , `GadgetName` VARCHAR(255) NOT NULL , `GadgetDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `GiveSeconds` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , `IsTactical` BOOLEAN NOT NULL , PRIMARY KEY (`GadgetID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{KillstreaksTableName}` ( `KillstreakID` INT UNSIGNED NOT NULL , `KillstreakName` VARCHAR(255) NOT NULL , `KillstreakDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `KillstreakRequired` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , PRIMARY KEY (`KillstreakID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{CardsTableName}` ( `CardID` INT UNSIGNED NOT NULL , `CardName` VARCHAR(255) NOT NULL , `CardDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `CardLink` TEXT NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , PRIMARY KEY (`CardID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GlovesTableName}` ( `GloveID` SMALLINT UNSIGNED NOT NULL , `GloveName` VARCHAR(255) NOT NULL , `GloveDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , PRIMARY KEY (`GloveID`));", Conn).ExecuteScalarAsync();

                    // PLAYERS DATA
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `SteamName` TEXT NOT NULL , `AvatarLink` VARCHAR(200) NOT NULL , `XP` INT UNSIGNED NOT NULL DEFAULT '0' , `Level` INT UNSIGNED NOT NULL DEFAULT '1' , `Credits` INT UNSIGNED NOT NULL DEFAULT '0' , `Kills` INT UNSIGNED NOT NULL DEFAULT '0' , `HeadshotKills` INT UNSIGNED NOT NULL DEFAULT '0' , `HighestKillstreak` INT UNSIGNED NOT NULL DEFAULT '0' , `HighestMultiKills` INT UNSIGNED NOT NULL DEFAULT '0' , `KillsConfirmed` INT UNSIGNED NOT NULL DEFAULT '0' , `KillsDenied` INT UNSIGNED NOT NULL DEFAULT '0' , `FlagsCaptured` INT UNSIGNED NOT NULL DEFAULT '0' , `FlagsSaved` INT UNSIGNED NOT NULL DEFAULT '0' , `AreasTaken` INT UNSIGNED NOT NULL DEFAULT '0' , `Deaths` INT UNSIGNED NOT NULL DEFAULT '0' , `Music` BOOLEAN NOT NULL DEFAULT TRUE , PRIMARY KEY (`SteamID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersGunsTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `GunID` SMALLINT UNSIGNED NOT NULL , `Level` INT UNSIGNED NOT NULL , `XP` INT UNSIGNED NOT NULL , `GunKills` INT UNSIGNED NOT NULL , `IsBought` BOOLEAN NOT NULL , `Attachments` TEXT NOT NULL , CONSTRAINT `ub_steam_id` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_gun_id_1` FOREIGN KEY (`GunID`) REFERENCES `{GunsTableName}` (`GunID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `GunID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersGunsSkinsTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `SkinIDs` TEXT NOT NULL , CONSTRAINT `ub_steam_id_1` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersKnivesTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `KnifeID` SMALLINT UNSIGNED NOT NULL , `KnifeKills` INT UNSIGNED NOT NULL , `IsBought` BOOLEAN NOT NULL , CONSTRAINT `ub_steam_id_2` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , CONSTRAINT `ub_knife_id_1` FOREIGN KEY (`KnifeID`) REFERENCES `{KnivesTableName}` (`KnifeID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID` , `KnifeID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersKnivesSkinsTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `SkinIDs` TEXT NOT NULL , CONSTRAINT `ub_steam_id_3` FOREIGN KEY (`SteamID`) REFERENCES `{PlayersTableName}` (`SteamID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`SteamID`));", Conn).ExecuteScalarAsync();
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

                    var rdr = (MySqlDataReader)await new MySqlCommand($"SELECT `AttachmentID`, `AttachmentName`, `AttachmentDesc`, `AttachmentType`-1, `IconLink`, `BuyPrice` FROM `{AttachmentsTableName}`;", Conn).ExecuteReaderAsync();
                    try
                    {
                        var gunAttachments = new Dictionary<ushort, GunAttachment>();
                        while (await rdr.ReadAsync())
                        {
                            if (!ushort.TryParse(rdr[0].ToString(), out ushort attachmentID)) continue;
                            var attachmentName = rdr[1].ToString();
                            var attachmentDesc = rdr[2].ToString();
                            if (!int.TryParse(rdr[3].ToString(), out int attachmentTypeInt)) continue;
                            var attachmentType = (EAttachment)attachmentTypeInt;
                            var iconLink = rdr[4].ToString();
                            if (!int.TryParse(rdr[5].ToString(), out int buyPrice)) continue;
                            if (!gunAttachments.ContainsKey(attachmentID))
                            {
                                gunAttachments.Add(attachmentID, new GunAttachment(attachmentID, attachmentName, attachmentDesc, attachmentType, iconLink, buyPrice));
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
                    rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{GunsTableName}`;", Conn).ExecuteReaderAsync();
                    try
                    {
                        var guns = new Dictionary<ushort, Gun>();
                        while (await rdr.ReadAsync())
                        {
                            if (!ushort.TryParse(rdr[0].ToString(), out ushort gunID)) continue;
                            var gunName = rdr[1].ToString();
                            var gunDesc = rdr[2].ToString();
                            var iconLink = rdr[3].ToString();
                            if (!int.TryParse(rdr[4].ToString(), out int magAmount)) continue;
                            if (!int.TryParse(rdr[5].ToString(), out int scrapAmount)) continue;
                            if (!int.TryParse(rdr[6].ToString(), out int buyPrice)) continue;
                            if (!bool.TryParse(rdr[7].ToString(), out bool isDefault)) continue;
                            if (!bool.TryParse(rdr[8].ToString(), out bool isPrimary)) continue;
                            var attachments = new List<GunAttachment>();
                            foreach (var id in rdr[9].GetIntListFromReaderResult())
                            {
                                if (GunAttachments.TryGetValue((ushort)id, out GunAttachment gunAttachment))
                                {
                                    attachments.Add(gunAttachment);
                                }
                                else
                                {
                                    Logging.Debug($"Could'nt find default attachment with id {id} for gun {gunID} with name {gunName}");
                                }
                            }
                            if (!int.TryParse(rdr[10].ToString(), out int maxLevel)) continue;
                            var levelXPNeeded = rdr[11].GetIntListFromReaderResult();
                            var levelRewards = rdr[12].GetIntListFromReaderResult();
                            var gun = new Gun(gunID, gunName, gunDesc, iconLink, magAmount, scrapAmount, buyPrice, isDefault, isPrimary, attachments, maxLevel, levelXPNeeded, levelRewards);
                            if (!guns.ContainsKey(gunID))
                            {
                                guns.Add(gunID, gun);
                            }
                            else
                            {
                                Logging.Debug($"Found a duplicate with id {gunID}, ignoring this");
                            }
                            if (isDefault)
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
                            if (!int.TryParse(rdr[0].ToString(), out int id)) continue;
                            if (!ushort.TryParse(rdr[1].ToString(), out ushort gunID)) continue;
                            if (!Guns.TryGetValue(gunID, out Gun gun))
                            {
                                Logging.Debug($"Could'nt find gun id with {gunID} for skin with id {id}");
                                continue;
                            }
                            if (!ushort.TryParse(rdr[2].ToString(), out ushort skinID)) continue;
                            var skinName = rdr[3].ToString();
                            var skinDesc = rdr[4].ToString();
                            var iconLink = rdr[5].ToString();
                            if (!int.TryParse(rdr[6].ToString(), out int scrapAmount)) continue;

                            var skin = new GunSkin(id, gun, skinID, skinName, skinDesc, iconLink, scrapAmount);
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

                    Logging.Debug("Reading knives from the base data");
                    rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{KnivesTableName}`;", Conn).ExecuteReaderAsync();
                    try
                    {
                        var knives = new Dictionary<ushort, Knife>();
                        while (await rdr.ReadAsync())
                        {
                            if (!ushort.TryParse(rdr[0].ToString(), out ushort knifeID)) continue;
                            var knifeName = rdr[1].ToString();
                            var knifeDesc = rdr[2].ToString();
                            var iconLink = rdr[3].ToString();
                            if (!int.TryParse(rdr[4].ToString(), out int scrapAmount)) continue;
                            if (!int.TryParse(rdr[5].ToString(), out int buyPrice)) continue;
                            if (!bool.TryParse(rdr[6].ToString(), out bool isDefault)) continue;

                            var knife = new Knife(knifeID, knifeName, knifeDesc, iconLink, scrapAmount, buyPrice, isDefault);
                            if (!knives.ContainsKey(knifeID))
                            {
                                knives.Add(knifeID, knife);
                            }
                            else
                            {
                                Logging.Debug($"Found a duplicate knife with id {knifeID}, ignoring this");
                            }
                            if (isDefault)
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

                    Logging.Debug("Reading knife skins from the base data");
                    rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{KnivesSkinsTableName}`;", Conn).ExecuteReaderAsync();
                    try
                    {
                        var knifeSkinsSearchByID = new Dictionary<int, KnifeSkin>();
                        var knifeSkinsSearchByKnifeID = new Dictionary<ushort, List<KnifeSkin>>();
                        var knifeSkinsSearchBySkinID = new Dictionary<ushort, KnifeSkin>();

                        while (await rdr.ReadAsync())
                        {
                            if (!int.TryParse(rdr[0].ToString(), out int id)) continue;
                            if (!ushort.TryParse(rdr[1].ToString(), out ushort knifeID)) continue;
                            if (!Knives.TryGetValue(knifeID, out Knife knife))
                            {
                                Logging.Debug($"Could'nt find knife id with {knifeID} for skin with id {id}");
                                continue;
                            }
                            if (!ushort.TryParse(rdr[2].ToString(), out ushort skinID)) continue;
                            var skinName = rdr[3].ToString();
                            var skinDesc = rdr[4].ToString();
                            var iconLink = rdr[5].ToString();
                            if (!int.TryParse(rdr[6].ToString(), out int scrapAmount)) continue;

                            var skin = new KnifeSkin(id, knife, skinID, skinName, skinDesc, iconLink, scrapAmount);
                            if (knifeSkinsSearchByID.ContainsKey(id))
                            {
                                Logging.Debug($"Found a duplicate skin with id {id}, ignoring this");
                                continue;
                            }
                            else
                            {
                                knifeSkinsSearchByID.Add(id, skin);
                            }

                            if (knifeSkinsSearchByKnifeID.TryGetValue(knifeID, out List<KnifeSkin> skins))
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
                                knifeSkinsSearchByKnifeID.Add(knifeID, new List<KnifeSkin> { skin });
                            }

                            if (knifeSkinsSearchBySkinID.ContainsKey(skinID))
                            {
                                Logging.Debug($"Found a duplicate skin with id {id}, ignoring this");
                                continue;
                            }
                            else
                            {
                                knifeSkinsSearchBySkinID.Add(skinID, skin);
                            }
                        }

                        Logging.Debug($"Successfully read {knifeSkinsSearchByID.Count} knife skins from the table");
                        KnifeSkinsSearchByID = knifeSkinsSearchByID;
                        KnifeSkinsSearchByKnifeID = knifeSkinsSearchByKnifeID;
                        KnifeSkinsSearchBySkinID = knifeSkinsSearchBySkinID;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("Error reading data from knife skins table");
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
                            if (!ushort.TryParse(rdr[0].ToString(), out ushort gadgetID)) continue;
                            var gadgetName = rdr[1].ToString();
                            var gadgetDesc = rdr[2].ToString();
                            var iconLink = rdr[3].ToString();
                            if (!int.TryParse(rdr[4].ToString(), out int scrapAmount)) continue;
                            if (!int.TryParse(rdr[5].ToString(), out int buyPrice)) continue;
                            if (!int.TryParse(rdr[6].ToString(), out int giveSeconds)) continue;
                            if (!bool.TryParse(rdr[7].ToString(), out bool isDefault)) continue;
                            if (!bool.TryParse(rdr[8].ToString(), out bool isTactical)) continue;

                            var gadget = new Gadget(gadgetID, gadgetName, gadgetDesc, iconLink, scrapAmount, buyPrice, giveSeconds, isDefault, isTactical);
                            if (!gadgets.ContainsKey(gadgetID))
                            {
                                gadgets.Add(gadgetID, gadget);
                            }
                            else
                            {
                                Logging.Debug($"Found a duplicate gadget with id {gadgetID}, ignoring this");
                            }
                            if (isDefault)
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
                            if (!int.TryParse(rdr[0].ToString(), out int killstreakID)) continue;
                            var killstreakName = rdr[1].ToString();
                            var killstreakDesc = rdr[2].ToString();
                            var iconLink = rdr[3].ToString();
                            if (!int.TryParse(rdr[4].ToString(), out int killstreakRequired)) continue;
                            if (!int.TryParse(rdr[5].ToString(), out int buyPrice)) continue;
                            if (!int.TryParse(rdr[6].ToString(), out int scrapAmount)) continue;
                            if (!bool.TryParse(rdr[7].ToString(), out bool isDefault)) continue;

                            var killstreak = new Killstreak(killstreakID, killstreakName, iconLink, killstreakRequired, buyPrice, scrapAmount, isDefault);
                            if (!killstreaks.ContainsKey(killstreakID))
                            {
                                killstreaks.Add(killstreakID, killstreak);
                            }
                            else
                            {
                                Logging.Debug($"Found a duplicate killstrea with id {killstreakID}, ignoring it");
                            }
                            if (isDefault)
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
                            if (!int.TryParse(rdr[0].ToString(), out int perkID)) continue;
                            var perkName = rdr[1].ToString();
                            var perkDesc = rdr[2].ToString();
                            var iconLink = rdr[3].ToString();
                            var skillType = rdr[4].ToString();
                            if (!int.TryParse(rdr[5].ToString(), out int skillLevel)) continue;
                            if (!int.TryParse(rdr[6].ToString(), out int scrapAmount)) continue;
                            if (!int.TryParse(rdr[7].ToString(), out int buyPrice)) continue;
                            if (!bool.TryParse(rdr[8].ToString(), out bool isDefault)) continue;

                            var perk = new Perk(perkID, perkName, perkDesc, iconLink, skillType, skillLevel, scrapAmount, buyPrice, isDefault);
                            if (!perks.ContainsKey(perkID))
                            {
                                perks.Add(perkID, perk);
                            }
                            else
                            {
                                Logging.Debug($"Found a duplicate perk with id {perkID}, ignoring this");
                            }
                            if (isDefault)
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
                            if (!ushort.TryParse(rdr[0].ToString(), out ushort gloveID)) continue;
                            var gloveName = rdr[1].ToString();
                            var gloveDesc = rdr[2].ToString();
                            var iconLink = rdr[3].ToString();
                            if (!int.TryParse(rdr[4].ToString(), out int buyPrice)) continue;
                            if (!int.TryParse(rdr[5].ToString(), out int scrapAmount)) continue;
                            if (!bool.TryParse(rdr[6].ToString(), out bool isDefault)) continue;

                            var glove = new Glove(gloveID, gloveName, iconLink, buyPrice, scrapAmount, isDefault);
                            if (!gloves.ContainsKey(gloveID))
                            {
                                gloves.Add(gloveID, glove);
                            }
                            else
                            {
                                Logging.Debug($"Found a duplicate glove with id {gloveID}");
                            }
                            if (isDefault)
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
                            if (!int.TryParse(rdr[0].ToString(), out int cardID)) continue;
                            var cardName = rdr[1].ToString();
                            var cardDesc = rdr[2].ToString();
                            var iconLink = rdr[3].ToString();
                            var cardLink = rdr[4].ToString();
                            if (!int.TryParse(rdr[5].ToString(), out int buyPrice)) continue;
                            if (!int.TryParse(rdr[6].ToString(), out int scrapAmount)) continue;
                            if (!bool.TryParse(rdr[7].ToString(), out bool isDefault)) continue;

                            var card = new Card(cardID, cardName, cardDesc, iconLink, cardLink, buyPrice, scrapAmount, isDefault);
                            if (!cards.ContainsKey(cardID))
                            {
                                cards.Add(cardID, card);
                            }
                            else
                            {
                                Logging.Debug($"Found a duplicate card with id {cardID}, ignoring this");
                            }
                            if (isDefault)
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

                    Logging.Debug("Building a default loadout for new players");
                    try
                    {
                        var defaultPrimary = defaultGuns.FirstOrDefault(k => k.IsPrimary);
                        Logging.Debug($"Found default primary with id {defaultPrimary?.GunID ?? 0}");
                        var defaultPrimaryAttachments = defaultPrimary?.DefaultAttachments.Select(k => k.AttachmentID).ToList() ?? new List<ushort>();
                        Logging.Debug($"Found {defaultPrimaryAttachments.Count} default primary attachments");
                        var defaultSecondary = defaultGuns.FirstOrDefault(k => !k.IsPrimary);
                        Logging.Debug($"Found default secondary with id {defaultSecondary?.GunID ?? 0}");
                        var defaultSecondaryAttachments = defaultSecondary?.DefaultAttachments.Select(k => k.AttachmentID).ToList() ?? new List<ushort>();
                        Logging.Debug($"Found {defaultSecondaryAttachments.Count} default secondary attachments");
                        var defaultPerk = new List<int>();
                        foreach (var perk in defaultPerks)
                        {
                            defaultPerk.Add(perk.PerkID);
                            if (defaultPerk.Count == 3) break;
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
                            if (defaultKillstreaks.Count == 3) break;
                        }
                        Logging.Debug($"Found {defaultKillstreak.Count} default killstreaks");
                        var defaultGlove = defaultGloves.FirstOrDefault();
                        Logging.Debug($"Found default glove with id {defaultGlove?.GloveID ?? 0}");
                        var defaultCard = defaultCards.FirstOrDefault();
                        Logging.Debug($"Found default card with id {defaultCard?.CardID ?? 0}");
                        DefaultLoadout = new LoadoutData("DEFAULT LOADOUT", defaultPrimary?.GunID ?? 0, defaultPrimaryAttachments, defaultSecondary?.GunID ?? 0, defaultSecondaryAttachments, defaultKnife?.KnifeID ?? 0, defaultTactical?.GadgetID ?? 0, defaultLethal?.GadgetID ?? 0, defaultKillstreak, defaultPerk, defaultGlove?.GloveID ?? 0, defaultCard?.CardID ?? 0);
                        Logging.Debug("Built a default loadout to give to the players when they join");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("Error building default loadout for players");
                        Logger.Log(ex);
                    } finally
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
                    Logging.Debug($"Adding {steamName} to the DB");
                    await Conn.OpenAsync();
                    var cmd = new MySqlCommand($"INSERT INTO `{PlayersTableName}` ( `SteamID` , `SteamName` , `AvatarLink` ) VALUES ({player.CSteamID}, @name, '{avatarLink}');", Conn);
                    cmd.Parameters.AddWithValue("@name", steamName);
                    await cmd.ExecuteScalarAsync();

                    Logging.Debug($"Giving {steamName} the default guns");
                    foreach (var gun in DefaultGuns)
                    {
                        Logging.Debug($"Adding gun with id {gun.GunID}");
                        await new MySqlCommand($"INSERT INTO `{PlayersGunsTableName}` (`SteamID` , `GunID` , `Level` , `XP` , `GunKills` , `IsBought` , `Attachments`) VALUES ({player.CSteamID} , {gun.GunID} , 1 , 0 , 0 , 1 , '{Utility.CreateStringFromDefaultAttachments(gun.DefaultAttachments)}');", Conn).ExecuteScalarAsync();
                    }
                    await new MySqlCommand($"INSERT INTO `{PlayersGunsSkinsTableName}` (`SteamID` , `SkinIDs`) VALUES ({player.CSteamID}, '');", Conn).ExecuteScalarAsync();

                    Logging.Debug($"Giving {steamName} the default knives");
                    foreach (var knife in DefaultKnives)
                    {
                        Logging.Debug($"Adding knife with id {knife.KnifeID}");
                        await new MySqlCommand($"INSERT INTO `{PlayersKnivesTableName}` (`SteamID` , `KnifeID` , `KnifeKills` , `IsBought`) VALUES ({player.CSteamID} , {knife.KnifeID} , 0 , 1);", Conn).ExecuteScalarAsync();
                    }
                    await new MySqlCommand($"INSERT INTO `{PlayersKnivesSkinsTableName}` (`SteamID` , `SkinIDs`) VALUES ({player.CSteamID}, '');", Conn).ExecuteScalarAsync();

                    Logging.Debug($"Giving {steamName} the default gadgets");
                    foreach (var gadget in DefaultGadgets)
                    {
                        Logging.Debug($"Adding gadget with id {gadget.GadgetID}");
                        await new MySqlCommand($"INSERT INTO `{PlayersGadgetsTableName}` (`SteamID` , `GadgetID` , `GadgetKills` , `IsBought`) VALUES ({player.CSteamID} , {gadget.GadgetID} , 0 , 1);", Conn).ExecuteScalarAsync();
                    }

                    Logging.Debug($"Giving {steamName} the default killstreaks");
                    foreach (var killstreak in DefaultKillstreaks)
                    {
                        Logging.Debug($"Adding killstreak with id {killstreak.KillstreakID}");
                        await new MySqlCommand($"INSERT INTO `{PlayersKillstreaksTableName}` (`SteamID` , `KillstreakID` , `KillstreakKills` , `IsBought`) VALUES ({player.CSteamID} , {killstreak.KillstreakID} , 0 , 1);", Conn).ExecuteScalarAsync();
                    }

                    Logging.Debug($"Giving {steamName} the default perks");
                    foreach (var perk in DefaultPerks)
                    {
                        Logging.Debug($"Adding perk with id {perk.PerkID}");
                        await new MySqlCommand($"INSERT INTO `{PlayersPerksTableName}` (`SteamID` , `PerkID` , `IsBought`) VALUES ({player.CSteamID} , {perk.PerkID} , 1);", Conn).ExecuteScalarAsync();
                    }

                    Logging.Debug($"Giving {steamName} the default gloves");
                    foreach (var glove in DefaultGloves)
                    {
                        Logging.Debug($"Adding glove with id {glove.GloveID}");
                        await new MySqlCommand($"INSERT INTO `{PlayersGlovesTableName}` (`SteamID` , `GloveID` , `IsBought`) VALUES ({player.CSteamID} , {glove.GloveID} , 1);", Conn).ExecuteScalarAsync();
                    }

                    Logging.Debug($"Giving {steamName} the default cards");
                    foreach (var card in DefaultCards)
                    {
                        Logging.Debug($"Adding card with id {card.CardID}");
                        await new MySqlCommand($"INSERT INTO `{PlayersCardsTableName}` (`SteamID` , `CardID` , `IsBought`) VALUES ({player.CSteamID} , {card.CardID} , 1);", Conn).ExecuteScalarAsync();
                    }

                    var loadoutAmount = Utility.GetLoadoutAmount(player);
                    Logging.Debug($"{steamName} should have {loadoutAmount} loadouts, adding them");
                    var data = Plugin.Instance.DataManager.ConvertLoadoutToJson(DefaultLoadout);
                    for (int i = 1; i <= loadoutAmount; i++)
                    {
                        Logging.Debug($"Adding loadout with id {i} for {steamName}");
                        await new MySqlCommand($"INSERT INTO `{PlayersLoadoutsTableName}` (`SteamID` , `LoadoutID` , `IsActive` , `Loadout`) VALUES ({player.CSteamID}, {i}, {(i == 1 ? 1 : 0)}, '{data}');", Conn).ExecuteScalarAsync();
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
        
        public async Task UpdatePlayerAsync(CSteamID steamID, string steamName, string avatarLink)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    var cmd = new MySqlCommand($"UPDATE `{PlayersTableName}` SET `SteamName` = @name, AvatarLink = '{avatarLink}';", Conn);
                    cmd.Parameters.AddWithValue("@name", steamName);
                    await cmd.ExecuteScalarAsync();
                } catch (Exception ex)
                {
                    Logger.Log($"Error updating player with steam id {steamID}, steam name {steamName}, avatar link {avatarLink}");
                    Logger.Log(ex);
                } finally
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
                            if (!uint.TryParse(rdr[3].ToString(), out uint xp)) continue;
                            if (!uint.TryParse(rdr[4].ToString(), out uint level)) continue;
                            if (!uint.TryParse(rdr[5].ToString(), out uint credits)) continue;
                            if (!uint.TryParse(rdr[6].ToString(), out uint kills)) continue;
                            if (!uint.TryParse(rdr[7].ToString(), out uint headshotKills)) continue;
                            if (!uint.TryParse(rdr[8].ToString(), out uint highestKillstreak)) continue;
                            if (!uint.TryParse(rdr[9].ToString(), out uint highestMultiKills)) continue;
                            if (!uint.TryParse(rdr[10].ToString(), out uint killsConfirmed)) continue;
                            if (!uint.TryParse(rdr[11].ToString(), out uint killsDenied)) continue;
                            if (!uint.TryParse(rdr[12].ToString(), out uint flagsCaptured)) continue;
                            if (!uint.TryParse(rdr[13].ToString(), out uint flagsSaved)) continue;
                            if (!uint.TryParse(rdr[14].ToString(), out uint areasTaken)) continue;
                            if (!uint.TryParse(rdr[15].ToString(), out uint deaths)) continue;
                            if (!bool.TryParse(rdr[16].ToString(), out bool music)) continue;

                            if (PlayerData.ContainsKey(player.CSteamID))
                            {
                                PlayerData.Remove(player.CSteamID);
                            }

                            PlayerData.Add(player.CSteamID, new PlayerData(player.CSteamID, steamName, avatarLink, xp, level, credits, kills, headshotKills, highestKillstreak, highestMultiKills, killsConfirmed, killsDenied, flagsCaptured, flagsSaved, areasTaken, deaths, music));
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
                    Dictionary<ushort, LoadoutKnife> knives = new Dictionary<ushort, LoadoutKnife>();
                    Dictionary<int, LoadoutGunSkin> gunSkinsSearchByID = new Dictionary<int, LoadoutGunSkin>();
                    Dictionary<int, LoadoutKnifeSkin> knifeSkinsSearchByID = new Dictionary<int, LoadoutKnifeSkin>();
                    Dictionary<ushort, List<LoadoutGunSkin>> gunSkinsSearchByGunID = new Dictionary<ushort, List<LoadoutGunSkin>>();
                    Dictionary<ushort, List<LoadoutKnifeSkin>> knifeSkinsSearchByKnifeID = new Dictionary<ushort, List<LoadoutKnifeSkin>>();
                    Dictionary<ushort, LoadoutGunSkin> gunSkinsSearchBySkinID = new Dictionary<ushort, LoadoutGunSkin>();
                    Dictionary<ushort, LoadoutKnifeSkin> knifeSkinsSearchBySkinID = new Dictionary<ushort, LoadoutKnifeSkin>();
                    Dictionary<int, LoadoutPerk> perks = new Dictionary<int, LoadoutPerk>();
                    Dictionary<int, LoadoutGadget> gadgets = new Dictionary<int, LoadoutGadget>();
                    Dictionary<int, LoadoutKillstreak> killstreaks = new Dictionary<int, LoadoutKillstreak>();
                    Dictionary<int, LoadoutCard> cards = new Dictionary<int, LoadoutCard>();
                    Dictionary<int, LoadoutGlove> gloves = new Dictionary<int, LoadoutGlove>();
                    Dictionary<int, Loadout> loadouts = new Dictionary<int, Loadout>();

                    Logging.Debug($"Getting guns for {player.CharacterName}");
                    rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersGunsTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                    try
                    {
                        while (await rdr.ReadAsync())
                        {
                            if (!ushort.TryParse(rdr[1].ToString(), out ushort gunID)) continue;
                            if (!Guns.TryGetValue(gunID, out Gun gun))
                            {
                                Logging.Debug($"Error finding gun with id {gunID}, ignoring it");
                                continue;
                            }

                            if (!int.TryParse(rdr[2].ToString(), out int level)) continue;
                            if (!int.TryParse(rdr[3].ToString(), out int xp)) continue;
                            if (!int.TryParse(rdr[4].ToString(), out int gunKills)) continue;
                            if (!bool.TryParse(rdr[5].ToString(), out bool isBought)) continue;
                            var attachments = Utility.GetAttachmentsFromString(rdr[6].ToString());
                            if (!guns.ContainsKey(gunID))
                            {
                                guns.Add(gunID, new LoadoutGun(gun, level, xp, gunKills, isBought, attachments));
                            } else
                            {
                                Logging.Debug($"Found a duplicate gun with id {gunID} registered for the player, ignoring it");
                            }
                        }
                        Logging.Debug($"Successfully got {guns.Count} guns registered to the player");
                    } catch (Exception ex)
                    {
                        Logger.Log($"Error reading gun data for {player.CharacterName}");
                        Logger.Log(ex);
                    } finally
                    {
                        rdr.Close();
                    }

                    Logging.Debug($"Getting knives for {player.CharacterName}");
                    rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersKnivesTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                    try
                    {
                        while (await rdr.ReadAsync())
                        {
                            if (!ushort.TryParse(rdr[1].ToString(), out ushort knifeID)) continue;
                            if (!Knives.TryGetValue(knifeID, out Knife knife))
                            {
                                Logging.Debug($"Error finding knife with id {knifeID}, ignoring it");
                                continue;
                            }
                            if (!int.TryParse(rdr[2].ToString(), out int knifeKills)) continue;
                            if (!bool.TryParse(rdr[3].ToString(), out bool isBought)) continue;
                            
                            if (!knives.ContainsKey(knifeID))
                            {
                                knives.Add(knifeID, new LoadoutKnife(knife, knifeKills, isBought));
                            } else
                            {
                                Logging.Debug($"Found a duplicate knife with id {knifeID} registered for {player.CharacterName}, ignoring it");
                            }
                        }
                        Logging.Debug($"Successfully got {knives.Count} knives for {player.CharacterName}");
                    } catch (Exception ex)
                    {
                        Logger.Log($"Error reading knife data for {player.CharacterName}");
                        Logger.Log(ex);
                    } finally
                    {
                        rdr.Close();
                    }

                    Logging.Debug($"Getting gun skins for {player.CharacterName}");
                    var gunSkinsTxt = await new MySqlCommand($"SELECT `SkinIDs` FROM `{PlayersGunsSkinsTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteScalarAsync();
                    if (gunSkinsTxt is string gunSkinsText)
                    {
                        foreach (var gunSkinText in gunSkinsText.Split(','))
                        {
                            var isEquipped = false;
                            var newText = "";
                            if (gunSkinText.StartsWith("E."))
                            {
                                isEquipped = true;
                                newText = gunSkinText.Replace("E.", "");
                            } else if (gunSkinText.StartsWith("UE."))
                            {
                                isEquipped = false;
                                newText = gunSkinText.Replace("UE.", "");
                            }

                            if (!int.TryParse(newText, out int skinID)) continue;
                            if (!GunSkinsSearchByID.TryGetValue(skinID, out GunSkin skin))
                            {
                                Logging.Debug($"Error finding gun skin with id {skinID}, ignoring it");
                                continue;
                            }

                            if (gunSkinsSearchByID.ContainsKey(skinID))
                            {
                                Logging.Debug($"Found a duplicate gun skin with id {skinID} registered for {player.CharacterName}, ignoring this");
                                continue;
                            }

                            var gunSkin = new LoadoutGunSkin(skin, isEquipped);
                            gunSkinsSearchByID.Add(skinID, gunSkin);
                            if (gunSkinsSearchByGunID.TryGetValue(skin.Gun.GunID, out List<LoadoutGunSkin> gunSkins))
                            {
                                gunSkins.Add(gunSkin);
                            }
                            else
                            {
                                gunSkinsSearchByGunID.Add(skin.Gun.GunID, new List<LoadoutGunSkin> { gunSkin });
                            }
                            gunSkinsSearchBySkinID.Add(skin.SkinID, gunSkin);
                        }
                        Logging.Debug($"Successfully got {gunSkinsSearchByID.Count} gun skins for {player.CharacterName}");
                    }

                    Logging.Debug($"Getting knife skins for {player.CharacterName}");
                    var knifeSkinsTxt = await new MySqlCommand($"SELECT `SkinIDs` FROM `{PlayersKnivesSkinsTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteScalarAsync();
                    if (knifeSkinsTxt is string knifeSkinsText)
                    {
                        foreach (var knifeSkinText in knifeSkinsText.Split(','))
                        {
                            var isEquipped = false;
                            var newText = "";
                            if (knifeSkinText.StartsWith("E."))
                            {
                                isEquipped = true;
                                newText = knifeSkinText.Replace("E.", "");
                            }
                            else if (knifeSkinText.StartsWith("UE."))
                            {
                                isEquipped = false;
                                newText = knifeSkinText.Replace("UE.", "");
                            }

                            if (!int.TryParse(newText, out int skinID)) continue;
                            if (!KnifeSkinsSearchByID.TryGetValue(skinID, out KnifeSkin skin))
                            {
                                Logging.Debug($"Error finding knife skin with id {skinID}, ignoring it");
                                continue;
                            }

                            if (knifeSkinsSearchByID.ContainsKey(skinID))
                            {
                                Logging.Debug($"Found a duplicate knife skin with id {skinID} registered for {player.CharacterName}, ignoring this");
                                continue;
                            }

                            var knifeSkin = new LoadoutKnifeSkin(skin, isEquipped);
                            knifeSkinsSearchByID.Add(skinID, knifeSkin);
                            if (knifeSkinsSearchByKnifeID.TryGetValue(skin.Knife.KnifeID, out List<LoadoutKnifeSkin> knifeSkins))
                            {
                                knifeSkins.Add(knifeSkin);
                            }
                            else
                            {
                                knifeSkinsSearchByKnifeID.Add(skin.Knife.KnifeID, new List<LoadoutKnifeSkin> { knifeSkin });
                            }
                            knifeSkinsSearchBySkinID.Add(skin.SkinID, knifeSkin);
                        }
                        Logging.Debug($"Successfully got {knifeSkinsSearchByID.Count} knife skins for {player.CharacterName}");
                    }

                    Logging.Debug($"Getting perks for {player.CharacterName}");
                    rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersPerksTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                    try
                    {
                        while (await rdr.ReadAsync())
                        {
                            if (!int.TryParse(rdr[1].ToString(), out int perkID)) continue;
                            if (!Perks.TryGetValue(perkID, out Perk perk))
                            {
                                Logging.Debug($"Error finding perk with id {perkID}, ignoring this");
                                continue;
                            }
                            if (!bool.TryParse(rdr[2].ToString(), out bool isBought)) continue;
                            if (!perks.ContainsKey(perkID))
                            {
                                perks.Add(perkID, new LoadoutPerk(perk, isBought));
                            } else
                            {
                                Logging.Debug($"Found a duplicate perk with id {perkID} registered for {player.CharacterName}, ignoring it");
                            }
                        }
                        Logging.Debug($"Successfully got {perks.Count} perks for {player.CharacterName}");
                    } catch (Exception ex)
                    {
                        Logger.Log($"Error reading perk data for {player.CharacterName}");
                        Logger.Log(ex);
                    } finally
                    {
                        rdr.Close();
                    }

                    Logging.Debug($"Getting gadgets for {player.CharacterName}");
                    rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersPerksTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                    try
                    {
                        while (await rdr.ReadAsync())
                        {
                            if (!ushort.TryParse(rdr[1].ToString(), out ushort gadgetID)) continue;
                            if (!Gadgets.TryGetValue(gadgetID, out Gadget gadget))
                            {
                                Logging.Debug($"Error finding gadget with id {gadgetID} for {player.CharacterName}, ignoring it");
                                continue;
                            }
                            if (!int.TryParse(rdr[2].ToString(), out int gadgetKills)) continue;
                            if (!bool.TryParse(rdr[3].ToString(), out bool isBought)) continue;
                            if (!gadgets.ContainsKey(gadgetID))
                            {
                                gadgets.Add(gadgetID, new LoadoutGadget(gadget, gadgetKills, isBought));
                            } else
                            {
                                Logging.Debug($"Found duplicate gadget with id {gadgetID} registered for {player.CharacterName}, ignoring it");
                            }
                        }
                        Logging.Debug($"Successfully got {gadgets.Count} for {player.CharacterName}");
                    } catch (Exception ex)
                    {
                        Logger.Log($"Error reading gadgets data for {player.CharacterName}");
                        Logger.Log(ex);
                    } finally
                    {
                        rdr.Close();
                    }

                    Logging.Debug($"Getting killstreaks for {player.CharacterName}");
                    rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersKillstreaksTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                    try
                    {
                        while (await rdr.ReadAsync())
                        {
                            if (!int.TryParse(rdr[1].ToString(), out int killstreakID)) continue;
                            if (!Killstreaks.TryGetValue(killstreakID, out Killstreak killstreak))
                            {
                                Logging.Debug($"Error finding killstreak with id {killstreakID} for {player.CharacterName}, ignoring it");
                                continue;
                            }
                            if (!int.TryParse(rdr[2].ToString(), out int killstreakKills)) continue;
                            if (!bool.TryParse(rdr[3].ToString(), out bool isBought)) continue;

                            if (!killstreaks.ContainsKey(killstreakID))
                            {
                                killstreaks.Add(killstreakID, new LoadoutKillstreak(killstreak, killstreakKills, isBought));
                            } else
                            {
                                Logging.Debug($"Found a duplicate killstreak with id {killstreakID} for {player.CharacterName}, ignoring it");
                            }
                        }
                        Logging.Debug($"Successfully got {killstreaks.Count} for {player.CharacterName}");
                    } catch (Exception ex)
                    {
                        Logger.Log($"Error reading killstreaks data for {player.CharacterName}");
                        Logger.Log(ex);
                    } finally
                    {
                        rdr.Close();
                    }

                    Logging.Debug($"Getting cards for {player.CharacterName}");
                    rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersCardsTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                    try
                    {
                        while (await rdr.ReadAsync())
                        {
                            if (!int.TryParse(rdr[1].ToString(), out int cardID)) continue;
                            if (!Cards.TryGetValue(cardID, out Card card))
                            {
                                Logging.Debug($"Error finding card with id {cardID} for {player.CharacterName}, ignoring it");
                                continue;
                            }

                            if (!bool.TryParse(rdr[2].ToString(), out bool isBought)) continue;
                            if (!cards.ContainsKey(cardID))
                            {
                                cards.Add(cardID, new LoadoutCard(card, isBought));
                            } else
                            {
                                Logging.Debug($"Found duplicate card with id {cardID} for {player.CharacterName}, ignoring it");
                            }
                        }
                        Logging.Debug($"Successfully got {cards.Count} for {player.CharacterName}");
                    } catch (Exception ex)
                    {
                        Logger.Log($"Error reading cards data for {player.CharacterName}");
                        Logger.Log(ex);
                    } finally
                    {
                        rdr.Close();
                    }

                    Logging.Debug($"Getting gloves for {player.CharacterName}");
                    rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersGlovesTableName}` WHERE `SteamID` = {player.CSteamID};", Conn).ExecuteReaderAsync();
                    try
                    {
                        while (await rdr.ReadAsync())
                        {
                            if (!ushort.TryParse(rdr[1].ToString(), out ushort gloveID)) continue;
                            if (!Gloves.TryGetValue(gloveID, out Glove glove))
                            {
                                Logging.Debug($"Error finding glove with id {gloveID} for {player.CharacterName}, ignoring it");
                                continue;
                            }
                            if (!bool.TryParse(rdr[2].ToString(), out bool isBought)) continue;
                            if (!gloves.ContainsKey(gloveID))
                            {
                                gloves.Add(gloveID, new LoadoutGlove(glove, isBought));
                            } else
                            {
                                Logging.Debug($"Found duplicate glove with id {gloveID} for {player.CharacterName}, ignoring it");
                            }
                        }
                        Logging.Debug($"Successfully got {gloves.Count} gloves for {player.CharacterName}");
                    } catch (Exception ex)
                    {
                        Logger.Log($"Error reading gloves data for {player.CharacterName}");
                        Logger.Log(ex);
                    } finally
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
                            if (!int.TryParse(rdr[1].ToString(), out int loadoutID)) continue;
                            if (!bool.TryParse(rdr[2].ToString(), out bool isActive)) continue;

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

                            var primaryAttachments = new Dictionary<EAttachment, LoadoutAttachment>();
                            foreach (var primaryAttachment in loadoutData.PrimaryAttachments)
                            {
                                if (primary.Attachments.TryGetValue(primaryAttachment, out LoadoutAttachment attachment))
                                {
                                    primaryAttachments.Add(attachment.Attachment.AttachmentType, attachment);
                                } else
                                {
                                    Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a primary attachment id with {primaryAttachment} which is not owned by the player, not counting it");
                                }
                            }

                            if (!guns.TryGetValue(loadoutData.Secondary, out LoadoutGun secondary) && loadoutData.Secondary != 0)
                            {
                                Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a secondary with id {loadoutData.Secondary} which is not owned by the player, not counting this loadout");
                                continue;
                            }

                            var secondaryAttachments = new Dictionary<EAttachment, LoadoutAttachment>();
                            foreach (var secondaryAttachment in loadoutData.SecondaryAttachments)
                            {
                                if (secondary.Attachments.TryGetValue(secondaryAttachment, out LoadoutAttachment attachment))
                                {
                                    secondaryAttachments.Add(attachment.Attachment.AttachmentType, attachment);
                                } else
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
                                } else
                                {
                                    Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a killstreak with id {killstreakID} which is not owned by the player, not counting this loadout");
                                    shouldContinue = false;
                                    break;
                                }
                            }
                            if (!shouldContinue) continue;

                            var loadoutPerks = new List<LoadoutPerk>();
                            foreach (var perkID in loadoutData.Perks)
                            {
                                if (perks.TryGetValue(perkID, out LoadoutPerk perk))
                                {
                                    loadoutPerks.Add(perk);
                                } else
                                {
                                    Logging.Debug($"Loadout with id {loadoutID} for {player.CharacterName} has a perk with id {perkID} which is not owned by the player, not counting this loadout");
                                    shouldContinue = false;
                                    break;
                                }
                            }
                            if (!shouldContinue) continue;

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

                            loadouts.Add(loadoutID, new Loadout(loadoutID, loadoutData.LoadoutName, isActive, primary, primaryAttachments, secondary, secondaryAttachments, knife, tactical, lethal, loadoutKillstreaks, loadoutPerks, glove, card));
                        }
                        Logging.Debug($"Successfully got {loadouts.Count} loadouts for {player.CharacterName}");
                    } catch (Exception ex)
                    {
                        Logger.Log($"Error reading loadouts data for {player.CharacterName}");
                        Logger.Log(ex);
                    } finally
                    {
                        rdr.Close();
                    }

                    Logging.Debug($"Checking loadouts for {player.CharacterName}");
                    try
                    {
                        var loadoutAmount = Utility.GetLoadoutAmount(player);
                        Logging.Debug($"{player.CharacterName} should have {loadoutAmount} loadouts, he has {loadouts.Count} registered");
                        var data = Plugin.Instance.DataManager.ConvertLoadoutToJson(DefaultLoadout);
                        if (loadoutAmount > loadouts.Count)
                        {
                            if (!guns.TryGetValue(DefaultLoadout.Primary, out LoadoutGun primary) && DefaultLoadout.Primary != 0)
                            {
                                Logging.Debug($"Default Loadout for {player.CharacterName} has a primary with id {DefaultLoadout.Primary} which is not owned by the player, not counting this loadout");
                                return;
                            }

                            var primaryAttachments = new Dictionary<EAttachment, LoadoutAttachment>();
                            foreach (var primaryAttachment in DefaultLoadout.PrimaryAttachments)
                            {
                                if (primary.Attachments.TryGetValue(primaryAttachment, out LoadoutAttachment attachment))
                                {
                                    primaryAttachments.Add(attachment.Attachment.AttachmentType, attachment);
                                }
                                else
                                {
                                    Logging.Debug($"Default loadout for {player.CharacterName} has a primary attachment id with {primaryAttachment} which is not owned by the player, not counting it");
                                    return;
                                }
                            }

                            if (!guns.TryGetValue(DefaultLoadout.Secondary, out LoadoutGun secondary) && DefaultLoadout.Secondary != 0)
                            {
                                Logging.Debug($"Default Loadout for {player.CharacterName} has a secondary with id {DefaultLoadout.Secondary} which is not owned by the player, not counting this loadout");
                                return;
                            }

                            var secondaryAttachments = new Dictionary<EAttachment, LoadoutAttachment>();
                            foreach (var secondaryAttachment in DefaultLoadout.SecondaryAttachments)
                            {
                                if (secondary.Attachments.TryGetValue(secondaryAttachment, out LoadoutAttachment attachment))
                                {
                                    secondaryAttachments.Add(attachment.Attachment.AttachmentType, attachment);
                                }
                                else
                                {
                                    Logging.Debug($"Default Loadout for {player.CharacterName} has a secondary attachment id with {secondaryAttachment} which is not owned by the player, not counting it");
                                    return;
                                }
                            }

                            if (!knives.TryGetValue(DefaultLoadout.Knife, out LoadoutKnife knife) && DefaultLoadout.Knife != 0)
                            {
                                Logging.Debug($"Default Loadout for {player.CharacterName} has a knife with id {DefaultLoadout.Knife} which is not owned by the player, not counting this loadout");
                                return;
                            }

                            if (!gadgets.TryGetValue(DefaultLoadout.Tactical, out LoadoutGadget tactical) && DefaultLoadout.Tactical != 0)
                            {
                                Logging.Debug($"Default Loadout for {player.CharacterName} has a tactical with id {DefaultLoadout.Tactical} which is not owned by the player, not counting this loadout");
                                return;
                            }

                            if (!gadgets.TryGetValue(DefaultLoadout.Lethal, out LoadoutGadget lethal) && DefaultLoadout.Lethal != 0)
                            {
                                Logging.Debug($"Default Loadout for {player.CharacterName} has a lethal with id {DefaultLoadout.Lethal} which is not owned by the player, not counting this loadout");
                                return;
                            }

                            var loadoutKillstreaks = new List<LoadoutKillstreak>();
                            foreach (var killstreakID in DefaultLoadout.Killstreaks)
                            {
                                if (killstreaks.TryGetValue(killstreakID, out LoadoutKillstreak killstreak))
                                {
                                    loadoutKillstreaks.Add(killstreak);
                                }
                                else
                                {
                                    Logging.Debug($"Default Loadout for {player.CharacterName} has a killstreak with id {killstreakID} which is not owned by the player, not counting this loadout");
                                    return;
                                }
                            }

                            var loadoutPerks = new List<LoadoutPerk>();
                            foreach (var perkID in DefaultLoadout.Perks)
                            {
                                if (perks.TryGetValue(perkID, out LoadoutPerk perk))
                                {
                                    loadoutPerks.Add(perk);
                                }
                                else
                                {
                                    Logging.Debug($"Default Loadout for {player.CharacterName} has a perk with id {perkID} which is not owned by the player, not counting this loadout");
                                    return;
                                }
                            }

                            if (!gloves.TryGetValue(DefaultLoadout.Glove, out LoadoutGlove glove) && DefaultLoadout.Glove != 0)
                            {
                                Logging.Debug($"Default Loadout for {player.CharacterName} has a glove with id {DefaultLoadout.Glove} which is not owned by the player, not counting this loadout");
                                return;
                            }

                            if (!cards.TryGetValue(DefaultLoadout.Card, out LoadoutCard card) && DefaultLoadout.Card != 0)
                            {
                                Logging.Debug($"Default Loadout has a card with id {DefaultLoadout.Card} which is not owned by the player, not counting this loadout");
                                return;
                            }

                            Logging.Debug($"{player.CharacterName} has less loadouts than he should have, creating more");
                            var defaultLoadout = new Loadout(0, DefaultLoadout.LoadoutName, false, primary, primaryAttachments, secondary, secondaryAttachments, knife, tactical, lethal, loadoutKillstreaks, loadoutPerks, glove, card);
                            for (int i = loadouts.Count + 1; i <= loadoutAmount; i++)
                            {
                                Logging.Debug($"Adding loadout with id {i} for {player.CharacterName}");
                                await new MySqlCommand($"INSERT INTO `{PlayersLoadoutsTableName}` (`SteamID` , `LoadoutID` , `IsActive` , `Loadout`) VALUES ({player.CSteamID}, {i}, {(i == 1 ? 1 : 0)}, '{data}');", Conn).ExecuteScalarAsync();
                                loadouts.Add(i, new Loadout(i, defaultLoadout.LoadoutName, i == 1, defaultLoadout.Primary, defaultLoadout.PrimaryAttachments, defaultLoadout.Secondary, defaultLoadout.SecondaryAttachments, defaultLoadout.Knife, defaultLoadout.Tactical, defaultLoadout.Lethal, defaultLoadout.Killstreaks, defaultLoadout.Perks, defaultLoadout.Glove, defaultLoadout.Card));
                            }
                        }
                        else if (loadoutAmount < loadouts.Count)
                        {
                            Logging.Debug($"{player.CharacterName} has more loadouts than he should have, deleting the last ones");
                            for (int i = loadouts.Count; i >= 1; i--)
                            {
                                if (loadouts.Count == loadoutAmount) break;
                                Logging.Debug($"Removing loadout with id {i} for {player.CharacterName}");
                                await new MySqlCommand($"DELETE FROM `{PlayersLoadoutsTableName}` WHERE `SteamID` = {player.CSteamID} AND `LoadoutID` = {i}", Conn).ExecuteScalarAsync();
                                loadouts.Remove(i);
                            }
                        }
                    } catch (Exception ex)
                    {
                        Logger.Log("Error checking the loadout amounts for player");
                        Logger.Log(ex);
                    }

                    if (!PlayerLoadouts.ContainsKey(player.CSteamID))
                    {
                        PlayerLoadouts.Remove(player.CSteamID);
                    }
                    PlayerLoadouts.Add(player.CSteamID, new PlayerLoadout(guns, knives, gunSkinsSearchByID, knifeSkinsSearchByID, gunSkinsSearchByGunID, knifeSkinsSearchByKnifeID, gunSkinsSearchBySkinID, knifeSkinsSearchBySkinID, perks, gadgets, killstreaks, cards, gloves, loadouts));
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

                    TaskDispatcher.QueueOnMainThread(() =>
                    {
                        var player = UnturnedPlayer.FromCSteamID(steamID);
                        if (player != null)
                        {
                            Plugin.Instance.HUDManager.OnXPChanged(player);
                        }
                    });
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

                    await new MySqlCommand($"UPDATE `{PlayersTableName}` SET `Music` = {(isMusic ? 1 : 0)} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
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

                    await new MySqlCommand($"INSERT INTO `{PlayersGunsTableName}` (`SteamID` , `GunID` , `Level` , `XP` , `GunKills` , `IsBought` , `Attachments`) VALUES ({steamID} , {gunID} , 1 , 0 , 0 , {(isBought ? 1 : 0)} , '{Utility.CreateStringFromDefaultAttachments(gun.DefaultAttachments)}');", Conn).ExecuteScalarAsync();
                    var loadoutAttachments = new Dictionary<ushort, LoadoutAttachment>();
                    foreach (var attachment in gun.DefaultAttachments)
                    {
                        if (loadoutAttachments.ContainsKey(attachment.AttachmentID))
                        {
                            Logging.Debug($"Duplicate default attachment found for gun {gunID} with id {attachment.AttachmentID}, ignoring it");
                            continue;
                        }
                        loadoutAttachments.Add(attachment.AttachmentID, new LoadoutAttachment(attachment, true));
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
                } catch (Exception ex)
                {
                    Logger.Log($"Error adding gun to {steamID} with gun id {gunID} and is bought {isBought}");
                    Logger.Log(ex);
                } finally
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
                    if (obj is int newXP)
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
                                    // Give reward
                                }
                                gun.XP = updatedXP;
                            }
                        }
                    }
                } catch (Exception ex)
                {
                    Logger.Log($"Error adding {xp} xp to gun with id {gunID} for {steamID}");
                    Logger.Log(ex);
                } finally
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
                } catch (Exception ex)
                {
                    Logger.Log($"Error adding kills {kills} to gun with id {gunID} for steam id {steamID}");
                    Logger.Log(ex);
                } finally
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
                    await new MySqlCommand($"UPDATE `{PlayersGunsTableName}` SET `IsBought` = {(isBought ? 1 : 0)} WHERE `SteamID` = {steamID} AND `GunID` = {gunID};", Conn).ExecuteScalarAsync();

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
                } catch (Exception ex)
                {
                    Logger.Log($"Error changing bought to {isBought} for gun with id {gunID} for steam id {steamID}");
                    Logger.Log(ex);
                } finally
                {
                    await Conn.CloseAsync();
                }
            }
        }

        public async Task AddPlayerGunAttachmentAsync(CSteamID steamID, ushort gunID, ushort attachmentID, bool isBought)
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

                    if (!GunAttachments.TryGetValue(attachmentID, out GunAttachment attachment))
                    {
                        Logging.Debug($"Error finding attachment with id {attachmentID}");
                        throw new Exception();
                    }

                    var loadoutAttachment = new LoadoutAttachment(attachment, isBought);
                    if (!gun.Attachments.ContainsKey(attachmentID))
                    {
                        Logging.Debug($"Attachment already registered to gun with id {gunID} for player with steam id {steamID}");
                        throw new Exception();
                    }


                } catch (Exception ex)
                {
                    Logger.Log($"Error adding gun attachment with id {attachmentID} to gun with id {gunID}, is bought {isBought} for player with steam id {steamID}");
                    Logger.Log(ex);
                } finally
                {
                    await Conn.CloseAsync();
                }
            }
        }
    }
}
