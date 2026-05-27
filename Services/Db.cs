using MySqlConnector;

namespace SchoolDTR.Services;

public static class Db
{
    public static MySqlConnection GetConnection()
    {
        var s = AppSettingsService.Load();

        var csb = new MySqlConnectionStringBuilder
        {
            Server = s.DbHost,
            Database = s.DbName,
            UserID = s.DbUser,
            Password = s.DbPassword,
            SslMode = MySqlSslMode.None,
            AllowUserVariables = true
        };

        return new MySqlConnection(csb.ConnectionString);
    }
}