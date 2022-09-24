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
using (var connection = new MySqlConnection(connectionString))
{
    connection.Open();
    var rdr = new MySqlCommand("SELECT `Moderation_PlayerInfo`.`PlayerID` , `Moderation_IPInfo`.`CountryCode` FROM `Moderation_IPInfo` INNER JOIN `Moderation_PlayerInfo` ON `Moderation_IPInfo`.`IP` = `Moderation_PlayerInfo`.`IP`;", connection).ExecuteReader();
    var playerIPs = new Dictionary<ulong, string>();
    while (rdr.Read())
    {
        if (!ulong.TryParse(rdr[0].ToString(), out ulong steamID))
        {
            continue;
        }

        var countryCode = rdr[1].ToString();

        playerIPs.Add(steamID, countryCode);
    }
    rdr.Close();

    foreach (var playerIP in playerIPs)
    {
        Console.WriteLine($"PlayerIP: {playerIP.Key}, CountryCode: {playerIP.Value}");
        await new MySqlCommand($"UPDATE `UB_Players` SET `CountryCode` = '{playerIP.Value}' WHERE `SteamID` = {playerIP.Key};", connection).ExecuteScalarAsync();
    }
}
