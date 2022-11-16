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

var devConn = new MySqlConnection(devtestBuilder.ConnectionString);
devConn.Open();
var comm = new MySqlCommand($"SELECT `SteamID` FROM `UB_Players` WHERE `Level` >= 19 AND `Level` <= 36;", devConn);
var idLevel19 = new List<ulong>();
var rdr = comm.ExecuteReader();
while (rdr.Read())
{
    if (ulong.TryParse(rdr[0].ToString(), out var id))
        idLevel19.Add(id);
}
rdr.Close();

comm = new MySqlCommand($"SELECT `SteamID` FROM `UB_Players` WHERE `Level` >= 37 AND `Level` <= 54;", devConn);
var idLevel37 = new List<ulong>();
rdr = comm.ExecuteReader();
while (rdr.Read())
{
    if (ulong.TryParse(rdr[0].ToString(), out var id))
        idLevel37.Add(id);
}
rdr.Close();

comm = new MySqlCommand($"SELECT `SteamID` FROM `UB_Players` WHERE `Level` >= 55 AND `Level` <= 72;", devConn);
var idLevel55 = new List<ulong>();
rdr = comm.ExecuteReader();
while (rdr.Read())
{
    if (ulong.TryParse(rdr[0].ToString(), out var id))
        idLevel55.Add(id);
}
rdr.Close();

comm = new MySqlCommand($"SELECT `SteamID` FROM `UB_Players` WHERE `Level` > 72;", devConn);
var idLevel72 = new List<ulong>();
rdr = comm.ExecuteReader();
while (rdr.Read())
{
    if (ulong.TryParse(rdr[0].ToString(), out var id))
        idLevel72.Add(id);
}
rdr.Close();

Console.WriteLine($"Level 19-36: {idLevel19.Count}");
Console.WriteLine($"Level 37-52: {idLevel37.Count}");
Console.WriteLine($"Level 55-72: {idLevel55.Count}");
Console.WriteLine($"Level 72+: {idLevel72.Count}");

devConn.Close();
var mainConn = new MySqlConnection(euMainBuilder.ConnectionString);
mainConn.Open();
foreach (var id in idLevel72)
{
    Console.WriteLine($"Giving rewards to {id} for being in level 72+");
    MySqlCommand cmd = new($"INSERT IGNORE INTO `UB_Players` ( `SteamID` , `SteamName` , `AvatarLink` , `CountryCode` , `MuteExpiry`, `MuteReason` , `Hotkeys` ) VALUES ({id}, 'Temp', 'Temp' , 'NA' , {DateTimeOffset.UtcNow.ToUnixTimeSeconds()} , ' ', '4,3,5,6,7' )", mainConn);
    cmd.ExecuteScalar();
    cmd = new($"INSERT INTO `UB_Players_Cards` (`SteamID`, `CardID`, `IsBought`, `IsUnlocked`) VALUES ({id}, 1000, 1, 0);", mainConn);
    cmd.ExecuteScalar();
    cmd = new($"INSERT INTO `UB_Players_Guns_Skins` (`SteamID`, `SkinIDs`) VALUES ({id}, '54,') ON DUPLICATE KEY UPDATE `SkinIDs` = CONCAT(`SkinIDs`,'54,');", mainConn);
    cmd.ExecuteScalar();
    cmd = new($"INSERT INTO `UB_Players_Guns_Charms` (`SteamID`, `CharmID`, `IsBought`, `IsUnlocked`) VALUES ({id}, 54055, 1, 0);", mainConn);
    cmd.ExecuteScalar();
    cmd = new($"INSERT INTO `UB_Players_Cards` (`SteamID`, `CardID`, `IsBought`, `IsUnlocked`) VALUES ({id}, 1001, 1, 0);", mainConn);
    cmd.ExecuteScalar();
}
mainConn.Close();