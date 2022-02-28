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

namespace UnturnedBlackout.Managers
{
    public class DatabaseManager
    {
        public string ConnectionString { get; set; }
        public Config Config { get; set; }

        public Dictionary<CSteamID, PlayerData> PlayerData { get; set; }

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

        // Players Data
        // MAIN
        public const string PlayersTableName = "UB_Players";

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

                    // BASE DATA
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GunsTableName}` ( `GunID` SMALLINT UNSIGNED NOT NULL , `GunName` VARCHAR(255) NOT NULL , `GunDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `MagAmount` TINYINT UNSIGNED NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , `IsPrimary` BOOLEAN NOT NULL , `DefaultAttachments` TEXT NOT NULL , `MaxLevel` INT UNSIGNED NOT NULL , `LevelXPNeeded` TEXT NOT NULL , `LevelRewards` TEXT NOT NULL , PRIMARY KEY (`GunID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{AttachmentsTableName}` ( `AttachmentID` SMALLINT UNSIGNED NOT NULL , `AttachmentName` VARCHAR(255) NOT NULL , `AttachmentDesc` TEXT NOT NULL , `AttachmentType` ENUM('Sights','Grip','Tactical','Barrel','Magazine') NOT NULL , `IconLink` TEXT NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , PRIMARY KEY (`AttachmentID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GunsSkinsTableName}` ( `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT , `GunID` SMALLINT UNSIGNED NOT NULL , `SkinID` SMALLINT UNSIGNED NOT NULL , `SkinName` VARCHAR(255) NOT NULL , `SkinDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , CONSTRAINT `ub_gun_id` FOREIGN KEY (`GunID`) REFERENCES `{GunsTableName}` (`GunID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`ID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{KnivesTableName}` ( `KnifeID` SMALLINT UNSIGNED NOT NULL , `KnifeName` VARCHAR(255) NOT NULL , `KnifeDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , PRIMARY KEY (`KnifeID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{KnivesSkinsTableName}` ( `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT , `KnifeID` SMALLINT UNSIGNED NOT NULL , `SkinID` SMALLINT UNSIGNED NOT NULL , `SkinName` VARCHAR(255) NOT NULL , `SkinDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , CONSTRAINT `ub_knife_id` FOREIGN KEY (`KnifeID`) REFERENCES `{KnivesTableName}` (`KnifeID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`ID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PerksTableName}` ( `PerkID` INT UNSIGNED NOT NULL , `PerkName` VARCHAR(255) NOT NULL , `PerkDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `SkillType` ENUM('OVERKILL','SHARPSHOOTER','DEXTERITY','CARDIO','EXERCISE','DIVING','PARKOUR','SNEAKYBEAKY','TOUGHNESS') NOT NULL , `SkillLevel` INT UNSIGNED NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , PRIMARY KEY (`PerkID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GadgetsTableName}` ( `GadgetID` SMALLINT UNSIGNED NOT NULL , `GadgetName` VARCHAR(255) NOT NULL , `GadgetDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `GiveSeconds` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , PRIMARY KEY (`GadgetID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{KillstreaksTableName}` ( `KillstreakID` INT UNSIGNED NOT NULL , `KillstreakName` VARCHAR(255) NOT NULL , `KillstreakDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `KillstreakRequired` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , PRIMARY KEY (`KillstreakID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{CardsTableName}` ( `CardID` INT UNSIGNED NOT NULL , `CardName` VARCHAR(255) NOT NULL , `CardDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `CardLink` TEXT NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , PRIMARY KEY (`CardID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GlovesTableName}` ( `GloveID` SMALLINT UNSIGNED NOT NULL , `GloveName` VARCHAR(255) NOT NULL , `GloveDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , PRIMARY KEY (`GloveID`));", Conn).ExecuteScalarAsync();
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

        public async Task AddOrUpdatePlayerAsync(CSteamID steamID, string steamName, string avatarLink)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    var cmd = new MySqlCommand($"INSERT INTO `{PlayersTableName}` ( `SteamID` , `SteamName` , `AvatarLink` ) VALUES ({steamID}, @name, '{avatarLink}') ON DUPLICATE KEY UPDATE `SteamName` = @name, `AvatarLink` = '{avatarLink}';", Conn);
                    cmd.Parameters.AddWithValue("@name", steamName);
                    await cmd.ExecuteScalarAsync();
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error adding player with Steam ID {steamID}, Steam Name {steamName}, avatar link {avatarLink}");
                    Logger.Log(ex);
                }
                finally
                {
                    await Conn.CloseAsync();
                }
            }
        }

        public async Task GetPlayerDataAsync(CSteamID steamID)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    var rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteReaderAsync();

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

                            if (PlayerData.ContainsKey(steamID))
                            {
                                PlayerData.Remove(steamID);
                            }

                            PlayerData.Add(steamID, new PlayerData(steamID, steamName, avatarLink, xp, level, credits, kills, headshotKills, highestKillstreak, highestMultiKills, killsConfirmed, killsDenied, flagsCaptured, flagsSaved, areasTaken, deaths, music));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Error reading player data with Steam ID {steamID}");
                        Logger.Log(ex);
                    }
                    finally
                    {
                        rdr.Close();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error getting player data with Steam ID {steamID}");
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

        public async Task GetBaseDataAsync()
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();

                    Utility.Debug("Getting base data");
                    Utility.Debug("Reading attachments from the base data");
                    var rdr = (MySqlDataReader)await new MySqlCommand($"SELECT `AttachmentID`, `AttachmentName`, `AttachmentDesc`, `AttachmentType`-1, `IconLink`, `BuyPrice` FROM `{AttachmentsTableName}`;", Conn).ExecuteReaderAsync();
                    try
                    {
                        var gunAttachments = new Dictionary<ushort, GunAttachment>();
                        while (rdr.Read())
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
                            } else
                            {
                                Utility.Debug($"Found a duplicate attachment with id {attachmentID}, ignoring this");
                            }
                        }

                        Utility.Debug($"Successfully read {gunAttachments.Count} attachments from the table");
                        GunAttachments = gunAttachments;
                    } catch (Exception ex)
                    {
                        Logger.Log("Error reading data from attachments table");
                        Logger.Log(ex);
                    } finally
                    {
                        rdr.Close();
                    }

                    Utility.Debug("Reading guns from the base data");
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
                                } else
                                {
                                    Utility.Debug($"Could'nt find default attachment with id {id} for gun {gunID} with name {gunName}");
                                }
                            }
                            if (!int.TryParse(rdr[10].ToString(), out int maxLevel)) continue;
                            var levelXPNeeded = rdr[11].GetIntListFromReaderResult();
                            var levelRewards = rdr[12].GetIntListFromReaderResult();
                            if (!guns.ContainsKey(gunID))
                            {
                                guns.Add(gunID, new Gun(gunID, gunName, gunDesc, iconLink, magAmount, scrapAmount, buyPrice, isDefault, isPrimary, attachments, maxLevel, levelXPNeeded, levelRewards));
                            } else
                            {
                                Utility.Debug($"Found a duplicate with id {gunID}, ignoring this");
                            }
                        }

                        Utility.Debug($"Successfully read {guns.Count} guns from the table");
                        Guns = guns;
                    } catch (Exception ex)
                    {
                        Logger.Log("Error reading data from guns table");
                        Logger.Log(ex);
                    } finally
                    {
                        rdr.Close();
                    }

                    Utility.Debug("Reading gun skins from the base table");
                    rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{GunsSkinsTableName}`;", Conn).ExecuteReaderAsync();
                    try
                    {
                        var gunSkinsSearchByID = new Dictionary<int, GunSkin>();
                        var gunSkinsSearchByGunID = new Dictionary<ushort, List<GunSkin>>();
                        var gunSkinsSearchBySkinID = new Dictionary<ushort, GunSkin>();

                        while (rdr.Read())
                        {
                            if (!int.TryParse(rdr[0].ToString(), out int id)) continue;
                            if (!ushort.TryParse(rdr[1].ToString(), out ushort gunID)) continue;
                            if (!Guns.TryGetValue(gunID, out Gun gun))
                            {
                                Utility.Debug($"Could'nt find gun id with {gunID} for skin with id {id}");
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
                                Utility.Debug($"Found a duplicate skin with id {id}, ignoring this");
                                continue;
                            } else
                            {
                                gunSkinsSearchByID.Add(id, skin);
                            }

                            if (gunSkinsSearchByGunID.TryGetValue(gunID, out List<GunSkin> skins))
                            {
                                if (skins.Exists(k => k.ID == id))
                                {
                                    Utility.Debug($"Found a duplicate skin with id {id}, ignoring this");
                                    continue;
                                } else
                                {
                                    skins.Add(skin);
                                }
                            } else
                            {
                                gunSkinsSearchByGunID.Add(gunID, new List<GunSkin> { skin });
                            }

                            if (gunSkinsSearchBySkinID.ContainsKey(skinID))
                            {
                                Utility.Debug($"Found a duplicate skin with id {id}, ignoring this");
                                continue;
                            } else
                            {
                                gunSkinsSearchBySkinID.Add(skinID, skin);
                            }
                        }

                        Utility.Debug($"Successfully read {gunSkinsSearchByID.Count} gun skins from the table");
                        GunSkinsSearchByID = gunSkinsSearchByID;
                        GunSkinsSearchByGunID = gunSkinsSearchByGunID;
                        GunSkinsSearchBySkinID = gunSkinsSearchBySkinID;
                    } catch (Exception ex)
                    {
                        Logger.Log("Error reading data from the guns skins table");
                        Logger.Log(ex);
                    } finally
                    {
                        rdr.Close();
                    }

                    Utility.Debug("Reading knives from the base data");
                    rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{KnivesTableName}`;", Conn).ExecuteReaderAsync();
                    try
                    {
                        var knives = new Dictionary<ushort, Knife>();
                        while (rdr.Read())
                        {
                            if (!ushort.TryParse(rdr[0].ToString(), out ushort knifeID)) continue;
                            var knifeName = rdr[1].ToString();
                            var knifeDesc = rdr[2].ToString();
                            var iconLink = rdr[3].ToString();
                            if (!int.TryParse(rdr[4].ToString(), out int scrapAmount)) continue;
                            if (!int.TryParse(rdr[5].ToString(), out int buyPrice)) continue;
                            if (!bool.TryParse(rdr[6].ToString(), out bool isDefault)) continue;

                            if (!knives.ContainsKey(knifeID))
                            {
                                knives.Add(knifeID, new Knife(knifeID, knifeName, knifeDesc, iconLink, scrapAmount, buyPrice, isDefault));
                            } else
                            {
                                Utility.Debug($"Found a duplicate knife with id {knifeID}, ignoring this");
                            }
                        }

                        Utility.Debug($"Successfully read {knives.Count} knives from the table");
                        Knives = knives;
                    } catch (Exception ex)
                    {
                        Logger.Log("Error reading data from the knives table");
                        Logger.Log(ex);
                    } finally
                    {
                        rdr.Close();
                    }

                    Utility.Debug("Reading knife skins from the base data");
                    rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{KnivesSkinsTableName}`;", Conn).ExecuteReaderAsync();
                    try
                    {
                        var knifeSkinsSearchByID = new Dictionary<int, KnifeSkin>();
                        var knifeSkinsSearchByKnifeID = new Dictionary<ushort, List<KnifeSkin>>();
                        var knifeSkinsSearchBySkinID = new Dictionary<ushort, KnifeSkin>();

                        while (rdr.Read())
                        {
                            if (!int.TryParse(rdr[0].ToString(), out int id)) continue;
                            if (!ushort.TryParse(rdr[1].ToString(), out ushort knifeID)) continue;
                            if (!Knives.TryGetValue(knifeID, out Knife knife))
                            {
                                Utility.Debug($"Could'nt find knife id with {knifeID} for skin with id {id}");
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
                                Utility.Debug($"Found a duplicate skin with id {id}, ignoring this");
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
                                    Utility.Debug($"Found a duplicate skin with id {id}, ignoring this");
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
                                Utility.Debug($"Found a duplicate skin with id {id}, ignoring this");
                                continue;
                            }
                            else
                            {
                                knifeSkinsSearchBySkinID.Add(skinID, skin);
                            }
                        }

                        Utility.Debug($"Successfully read {knifeSkinsSearchByID.Count} knife skins from the table");
                        KnifeSkinsSearchByID = knifeSkinsSearchByID;
                        KnifeSkinsSearchByKnifeID = knifeSkinsSearchByKnifeID;
                        KnifeSkinsSearchBySkinID = knifeSkinsSearchBySkinID;
                    } catch (Exception ex)
                    {
                        Logger.Log("Error reading data from knife skins table");
                        Logger.Log(ex);
                    } finally
                    {
                        rdr.Close();
                    }

                    Utility.Debug("Reading gadgets from base data");
                    rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{GadgetsTableName}`;", Conn).ExecuteReaderAsync();
                    try
                    {
                        var gadgets = new Dictionary<ushort, Gadget>();
                        while (rdr.Read())
                        {
                            if (!ushort.TryParse(rdr[0].ToString(), out ushort gadgetID)) continue;
                            var gadgetName = rdr[1].ToString();
                            var gadgetDesc = rdr[2].ToString();
                            var iconLink = rdr[3].ToString();
                            if (!int.TryParse(rdr[4].ToString(), out int scrapAmount)) continue;
                            if (!int.TryParse(rdr[5].ToString(), out int buyPrice)) continue;
                            if (!int.TryParse(rdr[6].ToString(), out int giveSeconds)) continue;
                            if (!bool.TryParse(rdr[7].ToString(), out bool isDefault)) continue;

                            if (!gadgets.ContainsKey(gadgetID))
                            {
                                gadgets.Add(gadgetID, new Gadget(gadgetID, gadgetName, gadgetDesc, iconLink, scrapAmount, buyPrice, giveSeconds, isDefault));
                            } else
                            {
                                Utility.Debug($"Found a duplicate gadget with id {gadgetID}, ignoring this");
                            }
                        }

                        Utility.Debug($"Successfully read {gadgets.Count} gadgets from the table");
                        Gadgets = gadgets;
                    } catch (Exception ex)
                    {
                        Logger.Log("Error reading data from gadgets table");
                        Logger.Log(ex);
                    } finally
                    {
                        rdr.Close();
                    }

                    Utility.Debug("Reading killstreaks from base data");
                    rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{KillstreaksTableName}`;", Conn).ExecuteReaderAsync();
                    try
                    {
                        var killstreaks = new Dictionary<int, Killstreak>();
                        while (rdr.Read())
                        {
                            if (!int.TryParse(rdr[0].ToString(), out int killstreakID)) continue;
                            var killstreakName = rdr[1].ToString();
                            var killstreakDesc = rdr[2].ToString();
                            var iconLink = rdr[3].ToString();
                            if (!int.TryParse(rdr[4].ToString(), out int killstreakRequired)) continue;
                            if (!int.TryParse(rdr[5].ToString(), out int buyPrice)) continue;
                            if (!int.TryParse(rdr[6].ToString(), out int scrapAmount)) continue;
                            if (!bool.TryParse(rdr[7].ToString(), out bool isDefault)) continue;

                            if (!killstreaks.ContainsKey(killstreakID))
                            {
                                killstreaks.Add(killstreakID, new Killstreak(killstreakID, killstreakName, iconLink, killstreakRequired, buyPrice, scrapAmount, isDefault));
                            } else
                            {
                                Utility.Debug($"Found a duplicate killstrea with id {killstreakID}, ignoring it");
                            }
                        }

                        Utility.Debug($"Successfully read {killstreaks.Count} killstreaks from table");
                        Killstreaks = killstreaks;
                    } catch (Exception ex)
                    {
                        Logger.Log("Error reading data from gadgets table");
                        Logger.Log(ex);
                    } finally
                    {
                        rdr.Close();
                    }

                    Utility.Debug("Reading perks from base data");
                    rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{PerksTableName}`;", Conn).ExecuteReaderAsync();
                    try
                    {
                        var perks = new Dictionary<int, Perk>();
                        while (rdr.Read())
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

                            if (!perks.ContainsKey(perkID))
                            {
                                perks.Add(perkID, new Perk(perkID, perkName, perkDesc, iconLink, skillType, skillLevel, scrapAmount, buyPrice, isDefault));
                            } else
                            {
                                Utility.Debug($"Found a duplicate perk with id {perkID}, ignoring this");
                            }
                        }

                        Utility.Debug($"Successfully read {perks.Count} perks from the table");
                        Perks = perks;
                    } catch (Exception ex)
                    {
                        Logger.Log("Error reading from perks table");
                        Logger.Log(ex);
                    } finally
                    {
                        rdr.Close();
                    }

                    Utility.Debug("Reading gloves from base data");
                    rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{GlovesTableName}`;", Conn).ExecuteReaderAsync();
                    try
                    {
                        var gloves = new Dictionary<ushort, Glove>();
                        while (rdr.Read())
                        {
                            if (!ushort.TryParse(rdr[0].ToString(), out ushort gloveID)) continue;
                            var gloveName = rdr[1].ToString();
                            var gloveDesc = rdr[2].ToString();
                            var iconLink = rdr[3].ToString();
                            if (!int.TryParse(rdr[4].ToString(), out int buyPrice)) continue;
                            if (!int.TryParse(rdr[5].ToString(), out int scrapAmount)) continue;
                            if (!bool.TryParse(rdr[6].ToString(), out bool isDefault)) continue;

                            if (!gloves.ContainsKey(gloveID))
                            {
                                gloves.Add(gloveID, new Glove(gloveID, gloveName, iconLink, buyPrice, scrapAmount, isDefault));
                            } else
                            {
                                Utility.Debug($"Found a duplicate glove with id {gloveID}");
                            }
                        }

                        Utility.Debug($"Successfully read {gloves.Count} gloves from the table");
                        Gloves = gloves;
                    } catch (Exception ex)
                    {
                        Logger.Log("Error reading from gloves table");
                        Logger.Log(ex);
                    } finally
                    {
                        rdr.Close();
                    }

                    Utility.Debug("Reading cards from base data");
                    rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{CardsTableName}`;", Conn).ExecuteReaderAsync();
                    try
                    {
                        var cards = new Dictionary<int, Card>();
                        while (rdr.Read())
                        {
                            if (!int.TryParse(rdr[0].ToString(), out int cardID)) continue;
                            var cardName = rdr[1].ToString();
                            var cardDesc = rdr[2].ToString();
                            var iconLink = rdr[3].ToString();
                            var cardLink = rdr[4].ToString();
                            if (!int.TryParse(rdr[5].ToString(), out int buyPrice)) continue;
                            if (!int.TryParse(rdr[6].ToString(), out int scrapAmount)) continue;
                            if (!bool.TryParse(rdr[7].ToString(), out bool isDefault)) continue;

                            if (!cards.ContainsKey(cardID))
                            {
                                cards.Add(cardID, new Card(cardID, cardName, cardDesc, iconLink, cardLink, buyPrice, scrapAmount, isDefault));
                            } else
                            {
                                Utility.Debug($"Found a duplicate card with id {cardID}, ignoring this");
                            }
                        }

                        Utility.Debug($"Successfully read {cards.Count} cards from the table");
                        Cards = cards;
                    } catch (Exception ex)
                    {
                        Logger.Log("Error reading data from cards table");
                        Logger.Log(ex);
                    } finally
                    {
                        rdr.Close();
                    }
                } catch (Exception ex)
                {
                    Logger.Log("Error getting base data");
                    Logger.Log(ex);
                } finally
                {
                    await Conn.CloseAsync();
                }
            }
        }
    }
}
