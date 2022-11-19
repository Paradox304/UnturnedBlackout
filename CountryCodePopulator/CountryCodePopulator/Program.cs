using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualBasic.CompilerServices;
using MySql.Data.MySqlClient;

var euMainBuilder = new MySqlConnectionStringBuilder
{
    Server = "priv-01.modern-hosting.com",
    Port = 3306,
    Database = "s965_BlackoutEU",
    UserID = "u965_JCaQjHTfJU",
    Password = "^EF^2xWP@t+8^cSXqSsszGS7",
    MaximumPoolSize = 500
};

var devtestBuilder = new MySqlConnectionStringBuilder
{
    Server = "priv-01.modern-hosting.com",
    Port = 3306,
    Database = "s1547_BlackoutEU",
    UserID = "u1547_IqbJaUmqKF",
    Password = "KqExSQhpO.ld=XTVkjKnA35M",
    MaximumPoolSize = 500
};

var conn = new MySqlConnection(euMainBuilder.ConnectionString);
conn.Open();



var cmd = new MySqlCommand("SELECT `UB_Players`.SteamID, `Moderation_IPInfo`.`CountryCode` FROM `Moderation_IPInfo` INNER JOIN `Moderation_PlayerInfo` ON `Moderation_PlayerInfo`.`IP` = `Moderation_IPInfo`.`IP` INNER JOIN `UB_Players` ON `UB_Players`.`SteamID` = `Moderation_PlayerInfo`.`PlayerID`",
    conn);

var rdr = cmd.ExecuteReader();
Dictionary<ulong, string> codes = new();
while (rdr.Read())
{
    if (!ulong.TryParse(rdr[0].ToString(), out var id))
        continue;

    var code = rdr[1].ToString();
    codes.Add(id, code);
    Console.WriteLine($"ID: {id}, Code: {code}");
}

rdr.Close();
var updateString = "";
foreach (var code in codes)
    updateString += $"UPDATE `UB_Players` SET `CountryCode` = '{code.Value}' WHERE `SteamID` = {code.Key};\n";

Console.WriteLine(updateString);

cmd = new(updateString, conn);
cmd.ExecuteScalar();

/* ACHIEVEMENT SCRIPT
var achievementID = 10;
var gunID = 13237;
var cmd = new MySqlCommand($"SELECT `SteamID`, `GunKills` FROM `UB_Players_Guns` WHERE `GunID` = {gunID} AND `GunKills` > 0", conn);
Dictionary<ulong, int> achievementInfo = new();
var rdr = cmd.ExecuteReader();
while (rdr.Read())
{
    if (!ulong.TryParse(rdr[0].ToString(), out var id))
        continue;

    if (!int.TryParse(rdr[1].ToString(), out var kills))
        continue;

    achievementInfo.Add(id, kills);
    Console.WriteLine($"ID: {id}, Kills: {kills}");
}

rdr.Close();

var updateString = "";
foreach (var info in achievementInfo)
    updateString += $"INSERT INTO `UB_Players_Achievements` (`SteamID`, `AchievementID`, `CurrentTier`, `Amount`) VALUES ({info.Key}, {achievementID}, 0, {info.Value});\n";

Console.WriteLine(updateString);
*/



conn.Close();