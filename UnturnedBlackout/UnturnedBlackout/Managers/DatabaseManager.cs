using MySql.Data.MySqlClient;
using Rocket.Core.Logging;
using Rocket.Core.Utils;
using Rocket.Unturned.Player;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnturnedBlackout.Database;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Managers
{
    public class DatabaseManager
    {
        public string ConnectionString { get; set; }
        public Config Config { get; set; }

        public Dictionary<CSteamID, PlayerData> PlayerCache { get; set; }

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

            PlayerCache = new Dictionary<CSteamID, PlayerData>();

            Guns = new Dictionary<ushort, Gun>();
            GunAttachments = new Dictionary<ushort, GunAttachment>();
            GunSkinsSearchByID = new Dictionary<int, GunSkin>();
            GunSkinsSearchByGunID = new Dictionary<ushort, List<GunSkin>>();
            GunSkinsSearchBySkinID = new Dictionary<ushort, GunSkin>();

            Knives = new Dictionary<ushort, Knife>();
            KnifeSkinsSearchByID = new Dictionary<int, KnifeSkin>();
            KnifeSkinsSearchByKnifeID = new Dictionary<ushort, List<KnifeSkin>>();
            KnifeSkinsSearchBySkinID = new Dictionary<ushort, KnifeSkin>();

            Gadgets = new Dictionary<ushort, Gadget>();
            Perks = new Dictionary<int, Perk>();
            Gloves = new Dictionary<ushort, Glove>();
            Cards = new Dictionary<int, Card>();

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
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PlayersTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `SteamName` TEXT NOT NULL , `AvatarLink` VARCHAR(200) NOT NULL , `XP` INT UNSIGNED NOT NULL DEFAULT '0' , `Level` INT UNSIGNED NOT NULL DEFAULT '1' , `Credits` INT UNSIGNED NOT NULL DEFAULT '0' , `Kills` INT UNSIGNED NOT NULL DEFAULT '0' , `HeadshotKills` INT UNSIGNED NOT NULL DEFAULT '0' , `HighestKillstreak` INT UNSIGNED NOT NULL DEFAULT '0' , `HighestMultiKills` INT UNSIGNED NOT NULL DEFAULT '0' , `KillsConfirmed` INT UNSIGNED NOT NULL DEFAULT '0' , `KillsDenied` INT UNSIGNED NOT NULL DEFAULT '0' , `FlagsCaptured` INT UNSIGNED NOT NULL DEFAULT '0' , `FlagsSaved` INT UNSIGNED NOT NULL DEFAULT '0' , `AreasTaken` INT UNSIGNED NOT NULL DEFAULT '0' , `Deaths` INT UNSIGNED NOT NULL DEFAULT '0' , `Music` BOOLEAN NOT NULL DEFAULT TRUE , PRIMARY KEY (`SteamID`));", Conn).ExecuteScalarAsync();

                    // BASE DATA
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GunsTableName}` ( `GunID` SMALLINT UNSIGNED NOT NULL , `GunName` VARCHAR(255) NOT NULL , `GunDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `MagAmount` TINYINT UNSIGNED NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , `IsPrimary` BOOLEAN NOT NULL , `DefaultAttachments` TEXT NOT NULL , `MaxLevel` INT UNSIGNED NOT NULL , `LevelXPNeeded` TEXT NOT NULL , `LevelRewards` TEXT NOT NULL , PRIMARY KEY (`GunID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{AttachmentsTableName}` ( `AttachmentID` SMALLINT UNSIGNED NOT NULL , `AttachmentName` VARCHAR(255) NOT NULL , `AttachmentDesc` TEXT NOT NULL , `AttachmentType` ENUM('Sights','Grip','Tactical','Barrel','Magazine') NOT NULL , `IconLink` TEXT NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , PRIMARY KEY (`AttachmentID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GunsSkinsTableName}` ( `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT , `GunID` SMALLINT UNSIGNED NOT NULL , `SkinID` SMALLINT UNSIGNED NOT NULL , `SkinName` VARCHAR(255) NOT NULL , `SkinDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , CONSTRAINT `ub_gun_id` FOREIGN KEY (`GunID`) REFERENCES `{GunsTableName}` (`GunID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`ID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{KnivesTableName}` ( `KnifeID` SMALLINT UNSIGNED NOT NULL , `KnifeName` VARCHAR(255) NOT NULL , `KnifeDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , PRIMARY KEY (`KnifeID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{KnivesSkinsTableName}` ( `ID` INT UNSIGNED NOT NULL AUTO_INCREMENT , `KnifeID` SMALLINT UNSIGNED NOT NULL , `SkinID` SMALLINT UNSIGNED NOT NULL , `SkinName` VARCHAR(255) NOT NULL , `SkinDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , CONSTRAINT `ub_knife_id` FOREIGN KEY (`KnifeID`) REFERENCES `{KnivesTableName}` (`KnifeID`) ON DELETE CASCADE ON UPDATE CASCADE , PRIMARY KEY (`ID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{PerksTableName}` ( `PerkID` INT UNSIGNED NOT NULL , `PerkName` VARCHAR(255) NOT NULL , `PerkDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `SkillType` ENUM('OVERKILL','SHARPSHOOTER','DEXTERITY','CARDIO','EXERCISE','DIVING','PARKOUR','SNEAKYBEAKY','TOUGHNESS') NOT NULL , `SkillLevel` INT UNSIGNED NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , PRIMARY KEY (`PerkID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GadgetsTableName}` ( `GadgetID` SMALLINT UNSIGNED NOT NULL , `GadgetName` VARCHAR(255) NOT NULL , `GadgetDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `BuyPrice` INT UNSIGNED NOT NULL , `GiveSeconds` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , PRIMARY KEY (`GadgetID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{KillstreaksTableName}` ( `KillstreakID` INT UNSIGNED NOT NULL , `KillstreakName` VARCHAR(255) NOT NULL , `KillstreakDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `KillstreakRequired` INT UNSIGNED NOT NULL , `BuyAmount` INT UNSIGNED NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , PRIMARY KEY (`KillstreakID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{CardsTableName}` ( `CardID` INT UNSIGNED NOT NULL , `CardName` VARCHAR(255) NOT NULL , `CardDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `CardLink` TEXT NOT NULL , `BuyAmount` INT UNSIGNED NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , PRIMARY KEY (`CardID`));", Conn).ExecuteScalarAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{GlovesTableName}` ( `GloveID` SMALLINT UNSIGNED NOT NULL , `GloveName` VARCHAR(255) NOT NULL , `GloveDesc` TEXT NOT NULL , `IconLink` TEXT NOT NULL , `BuyAmount` INT UNSIGNED NOT NULL , `ScrapAmount` INT UNSIGNED NOT NULL , `IsDefault` BOOLEAN NOT NULL , PRIMARY KEY (`GloveID`));", Conn).ExecuteScalarAsync();
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

                            if (PlayerCache.ContainsKey(steamID))
                            {
                                PlayerCache.Remove(steamID);
                            }

                            PlayerCache.Add(steamID, new PlayerData(steamID, steamName, avatarLink, xp, level, credits, kills, headshotKills, highestKillstreak, highestMultiKills, killsConfirmed, killsDenied, flagsCaptured, flagsSaved, areasTaken, deaths, music));
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

                    if (PlayerCache.TryGetValue(steamID, out PlayerData data))
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
                    if (PlayerCache.TryGetValue(steamID, out PlayerData data))
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

                    if (PlayerCache.TryGetValue(steamID, out PlayerData data))
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
                    if (PlayerCache.TryGetValue(steamID, out PlayerData data))
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
                    if (PlayerCache.TryGetValue(steamID, out PlayerData data))
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
                    if (PlayerCache.TryGetValue(steamID, out PlayerData data))
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
                    if (PlayerCache.TryGetValue(steamID, out PlayerData data))
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
                    if (PlayerCache.TryGetValue(steamID, out PlayerData data))
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
                    if (PlayerCache.TryGetValue(steamID, out PlayerData data))
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
                    if (PlayerCache.TryGetValue(steamID, out PlayerData data))
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
                    if (PlayerCache.TryGetValue(steamID, out PlayerData data))
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
                    if (PlayerCache.TryGetValue(steamID, out PlayerData data))
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
                    if (PlayerCache.TryGetValue(steamID, out PlayerData data))
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

                    var rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{AttachmentsTableName}`;", Conn).ExecuteReaderAsync();
                    Utility.Debug($"{rdr.FieldCount} ATTACHMENTS FOUND, GETTING THEM");
                    try
                    {
                        var gunAttachments = new Dictionary<ushort, GunAttachment>();
                        while (rdr.Read())
                        {
                            if (!ushort.TryParse(rdr[0].ToString(), out ushort attachmentID)) continue;
                            var attachmentName = rdr[1].ToString();
                            var attachmentDesc = rdr[2].ToString();
                            Logger.Log($"GETTING THE ATTACHMENT TYPE {rdr[3]}");
                        }
                    } catch (Exception ex)
                    {
                        Logger.Log("Error reading data from attachments table");
                        Logger.Log(ex);
                    } finally
                    {
                        rdr.Close();
                    }
                    /*try
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

                        }
                    } catch (Exception ex)
                    {
                        Logger.Log("Error reading data from guns table");
                        Logger.Log(ex);
                    } finally
                    {
                        rdr.Close();
                    }*/
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
