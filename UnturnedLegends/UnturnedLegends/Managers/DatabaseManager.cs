using MySql.Data.MySqlClient;
using Rocket.Core.Logging;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedLegends.Database;

namespace UnturnedLegends.Managers
{
    public class DatabaseManager
    {
        public string ConnectionString { get; set; }
        public Config Config { get; set; }

        public Dictionary<CSteamID, PlayerData> PlayerCache { get; set; }

        public DatabaseManager()
        {
            Config = Plugin.Instance.Configuration.Instance;
            ConnectionString = ConnectionString = $"server={Config.DatabaseHost};user={Config.DatabaseUsername};database={Config.DatabaseName};port={Config.DatabasePort};password={Config.DatabasePassword}";

            PlayerCache = new Dictionary<CSteamID, PlayerData>();
        }

        public async Task LoadDatabaseAsync()
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await Conn.OpenAsync();
                    await new MySqlCommand($"CREATE TABLE IF NOT EXISTS `{Config.PlayersTableName}` ( `SteamID` BIGINT UNSIGNED NOT NULL , `SteamName` VARCHAR(65) NOT NULL , `AvatarLink` VARCHAR(200) NOT NULL , `XP` INT UNSIGNED NOT NULL DEFAULT '0' , `Credits` INT UNSIGNED NOT NULL DEFAULT '0' , `Kills` INT UNSIGNED NOT NULL DEFAULT '0' , `Deaths` INT UNSIGNED NOT NULL DEFAULT '0' , PRIMARY KEY (`SteamID`));").ExecuteScalarAsync();
                } catch (Exception ex)
                {
                    Logger.Log("Error loading database");
                    Logger.Log(ex);
                } finally
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
                    var cmd = new MySqlCommand($"INSERT INTO `{Config.PlayersTableName}` ( `SteamID` , `SteamName` , `AvatarLink` ) VALUES ({steamID}, @name, '{avatarLink}') ON DUPLICATE KEY UPDATE `SteamName` = @name AND `AvatarLink` = '{avatarLink}';", Conn);
                    cmd.Parameters.AddWithValue("@name", steamName);
                    await cmd.ExecuteScalarAsync();
                } catch (Exception ex)
                {
                    Logger.Log($"Error adding player with Steam ID {steamID}, Steam Name {steamName}, avatar link {avatarLink}");
                    Logger.Log(ex);
                } finally
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
                    var rdr = (MySqlDataReader)await new MySqlCommand($"SELECT * FROM `{Config.PlayersTableName}` WHERE `SteamID` = {steamID};", Conn).ExecuteReaderAsync();

                    try
                    {
                        while (await rdr.ReadAsync())
                        {
                            var steamName = rdr[1].ToString();
                            var avatarLink = rdr[2].ToString();
                            if (!uint.TryParse(rdr[3].ToString(), out uint xp)) continue;
                            if (!uint.TryParse(rdr[4].ToString(), out uint credits)) continue;
                            if (!uint.TryParse(rdr[5].ToString(), out uint kills)) continue;
                            if (!uint.TryParse(rdr[6].ToString(), out uint deaths)) continue;

                            if (PlayerCache.ContainsKey(steamID))
                            {
                                PlayerCache.Remove(steamID);
                            }

                            PlayerCache.Add(steamID, new PlayerData(steamID, steamName, avatarLink, xp, credits, kills, deaths));
                        }
                    } catch (Exception ex)
                    {
                        Logger.Log($"Getting player data with Steam ID {steamID}");
                        Logger.Log(ex);
                    } finally
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

        public async Task UpdatePlayerXPAsync(CSteamID steamID, uint newXP)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await new MySqlCommand($"UPDATE `{Config.PlayersTableName}` SET `XP` = {newXP} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    if (PlayerCache.TryGetValue(steamID, out PlayerData data))
                    {
                        data.XP = newXP;
                    }
                } catch (Exception ex)
                {
                    Logger.Log($"Error changing player with steam id {steamID} xp to {newXP}");
                    Logger.Log(ex);
                } finally
                {
                    await Conn.CloseAsync();
                }
            }
        }

        public async Task UpdatePlayerCreditsAsync(CSteamID steamID, uint newCredits)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await new MySqlCommand($"UPDATE `{Config.PlayersTableName}` SET `Credits` = {newCredits} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    if (PlayerCache.TryGetValue(steamID, out PlayerData data))
                    {
                        data.Credits = newCredits;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error changing player with steam id {steamID} credits to {newCredits}");
                    Logger.Log(ex);
                }
                finally
                {
                    await Conn.CloseAsync();
                }
            }
        }

        public async Task UpdatePlayerKillsAsync(CSteamID steamID, uint newKills)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await new MySqlCommand($"UPDATE `{Config.PlayersTableName}` SET `Kills` = {newKills} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    if (PlayerCache.TryGetValue(steamID, out PlayerData data))
                    {
                        data.Kills = newKills;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error changing player with steam id {steamID} kills to {newKills}");
                    Logger.Log(ex);
                }
                finally
                {
                    await Conn.CloseAsync();
                }
            }
        }

        public async Task UpdatePlayerDeathsAsync(CSteamID steamID, uint newDeaths)
        {
            using (MySqlConnection Conn = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await new MySqlCommand($"UPDATE `{Config.PlayersTableName}` SET `Deaths` = {newDeaths} WHERE `SteamID` = {steamID};", Conn).ExecuteScalarAsync();
                    if (PlayerCache.TryGetValue(steamID, out PlayerData data))
                    {
                        data.Deaths = newDeaths;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error changing player with steam id {steamID} kills to {newDeaths}");
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
