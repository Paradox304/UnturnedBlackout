﻿using Microsoft.VisualBasic.CompilerServices;
using MySql.Data.MySqlClient;

var builder = new MySqlConnectionStringBuilder
{
    Server = "priv-01.modern-hosting.com",
    Port = 3306,
    Database = "s965_BlackoutEU",
    UserID = "u965_JCaQjHTfJU",
    Password = "^EF^2xWP@t+8^cSXqSsszGS7",
    MaximumPoolSize = 500
};

var conn = new MySqlConnection(builder.ConnectionString);
var ids = new HashSet<ulong> { 76561199152276021,76561198107038880,76561198798473149,76561198251214248,76561198323574646,76561198174191506,76561198963444202,76561198830226541,76561198243988472,76561198400901301,76561198386984426,76561198111602436,76561199013348511,76561199253038322,76561198332324840,76561198323280948,76561198179728865,76561198264733298,76561198254340734,76561198121235471,76561199095550200,76561199069134766,76561198989313457,76561198881260187,76561198168152384,76561198216651697,76561198030143214,76561199004748843,76561198124191527,76561198244389036,76561198277468534,76561198164628882,76561199015319396,76561198086856835,76561198311389616,76561198201963548,76561198113709888,76561198367660843,76561198152116306,76561198314722938,76561198093176197,76561198386984426,76561199013554067,76561198258106019,76561198930415360,76561198182276818,76561198330365024,76561199060945034,76561198874216271,76561199389888261,76561198833009999,76561198431566032,76561198309911731,76561198799232970,76561199194329877,76561198083676205,76561198977339333,76561198120529874 };
conn.Open();
foreach (var id in ids)
{
    Console.WriteLine($"Giving 30 days prime to {id}");
    MySqlCommand cmd = new($"INSERT IGNORE INTO `UB_Players` ( `SteamID` , `SteamName` , `AvatarLink` , `CountryCode` , `MuteExpiry`, `MuteReason` , `Hotkeys` ) VALUES ({id}, 'Temp', 'Temp' , 'NA' , {DateTimeOffset.UtcNow.ToUnixTimeSeconds()} , ' ', '4,3,5,6,7' )", conn);
    cmd.ExecuteScalar();
    cmd = new($"UPDATE `UB_Players` SET `HasPrime` = 1, `PrimeExpiry` = 1671465600, `PrimeLastDailyReward` = 1668700800 WHERE `SteamID` = {id};", conn);
    cmd.ExecuteScalar();
}
conn.Close();
