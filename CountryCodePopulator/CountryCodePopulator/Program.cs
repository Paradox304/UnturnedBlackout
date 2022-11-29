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


var battlepassOwners = new List<ulong> { 76561197982005542,76561198036946691,76561198038051477,76561198040122416,76561198083676205,76561198086128742,76561198086856835,76561198110740255,76561198120529874,76561198133139284,76561198139710867,76561198156895521,76561198159605937,76561198168220253,76561198182276818,76561198198455042,76561198218724607,76561198242595648,76561198290606016,76561198292732096,76561198303472239,76561198306667431,76561198313930674,76561198330365024,76561198342052675,76561198344165451,76561198345971463,76561198350709139,76561198379778447,76561198435215982,76561198799232970,76561198801206583,76561198801628097,76561198816151077,76561198874926866,76561198930962593,76561198953647836,76561198977339333,76561199046171127,76561199059297976,76561199060945034,76561199088440010,76561199114647209,76561199126584740,76561199188974404,76561199207128597,76561199373710078,76561199389888261 };
var claimedRewards = new List<ulong> { 76561197982005542,76561198038051477,76561198040122416,76561198083676205,76561198086128742,76561198086856835,76561198110740255,76561198120529874,76561198133139284,76561198139710867,76561198156895521,76561198168220253,76561198182276818,76561198198455042,76561198242595648,76561198292732096,76561198303472239,76561198306667431,76561198313930674,76561198330365024,76561198342052675,76561198344165451,76561198345971463,76561198350709139,76561198379778447,76561198435215982,76561198799232970,76561198801206583,76561198801628097,76561198816151077,76561198874926866,76561198930962593,76561198953647836,76561198977339333,76561199046171127,76561199059297976,76561199060945034,76561199088440010,76561199126584740,76561199188974404,76561199207128597,76561199373710078,76561199389888261 };
foreach (var claimReward in claimedRewards)
{
    if (!battlepassOwners.Contains(claimReward))
        Console.WriteLine($"This guy claimed a reward but didnt have battlepass {claimReward}");
}


/*
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

ACHIEVEMENT SCRIPT
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
conn.Close();
*/