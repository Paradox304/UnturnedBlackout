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
    var rdr = new MySqlCommand()
}
