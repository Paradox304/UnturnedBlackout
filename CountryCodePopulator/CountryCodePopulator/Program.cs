using Microsoft.VisualBasic.CompilerServices;
using MySql.Data.MySqlClient;

var builder = new MySqlConnectionStringBuilder()
{
    Server = "priv-01.modern-hosting.com",
    Port = 3306,
    Database = "s965_BlackoutEU",
    UserID = "u965_JCaQjHTfJU",
    Password = "^EF^2xWP@t+8^cSXqSsszGS7",
    MaximumPoolSize = 500
};

var connectionString = builder.ConnectionString;
var conn = new MySqlConnection(connectionString);
conn.Open();
/*var rdr = new MySqlCommand($"SELECT * FROM `UB_Achievements`;", conn).ExecuteReader();
var achievements = new Dictionary<int, string>();
while (rdr.Read())
{
    if (!int.TryParse(rdr[0].ToString(), out var achievementID))
    {
        continue;
    }

    var conditions = rdr[2].ToString();
    if (string.IsNullOrEmpty(conditions))
    {
        continue;
    }
    achievements.Add(achievementID, conditions);
}

rdr.Close();

var previousConditions = new string[] { "guntype", "targetmk", "targetks", "winkills" };
var newConditions = new string[] { "gun_type", "target_mk", "target_ks", "win_kills" };
foreach (var achievement in achievements)
{
    Console.WriteLine($"Achievement ID: {achievement.Key}, conditions: {achievement.Value}");
    var conditions = achievement.Value;
    for (var i = 0; i < previousConditions.Length; i++)
    {
        conditions = conditions.Replace(previousConditions[i], newConditions[i]);
    }
    Console.WriteLine($"Achievement ID: {achievement.Key}, new conditions: {conditions}");
    new MySqlCommand($"UPDATE `UB_Achievements` SET `AchievementConditions` = '{conditions}' WHERE `AchievementID` = {achievement.Key};", conn).ExecuteNonQuery();
}*/
var rdr = new MySqlCommand($"SELECT * FROM `UB_Battlepass`;", conn).ExecuteReader();
var battlepass = new Dictionary<int, (string, string)>();
while (rdr.Read())
{
    if (!int.TryParse(rdr[0].ToString(), out var tierID))
    {
        continue;
    }
    
    var freeRewards = rdr[1].ToString();
    var premiumRewards = rdr[2].ToString();
    
    battlepass.Add(tierID, (freeRewards, premiumRewards));
}

rdr.Close();

var previousRewards = new string[] { "guncharm", "gunskin" };
var newRewards = new string[] { "gun_charm", "gun_skin" };
foreach (var bp in battlepass)
{
    Console.WriteLine($"Battlepass ID: {bp.Key}, free rewards: {bp.Value.Item1}, premium rewards: {bp.Value.Item2}");
    var freeRewards = bp.Value.Item1;
    var premiumRewards = bp.Value.Item2;
    for (var i = 0; i < previousRewards.Length; i++)
    {
        freeRewards = freeRewards.Replace(previousRewards[i], newRewards[i]);
        premiumRewards = premiumRewards.Replace(previousRewards[i], newRewards[i]);
    }

    Console.WriteLine($"Battlepass ID: {bp.Key}, new free rewards: {bp.Value.Item1}, new premium rewards: {bp.Value.Item2}");
    new MySqlCommand($"UPDATE `UB_Battlepass` SET `FreeReward` = '{freeRewards}', `PremiumReward` = '{premiumRewards}' WHERE `TierID` = {bp.Key};", conn).ExecuteNonQuery();
}