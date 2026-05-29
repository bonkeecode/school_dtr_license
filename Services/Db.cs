using MySqlConnector;

namespace SchoolDTR.Services;

public static class Db
{
    public static MySqlConnection GetConnection()
    {
        var s = AppSettingsService.Load();

        // If settings are not yet configured,
        // fallback to AppConfig.ConnectionString
        if (string.IsNullOrWhiteSpace(s.DbHost) ||
            string.IsNullOrWhiteSpace(s.DbName) ||
            string.IsNullOrWhiteSpace(s.DbUser))
        {
            return new MySqlConnection(AppConfig.ConnectionString);
        }

        var csb = new MySqlConnectionStringBuilder
        {
            Server = s.DbHost,
            Database = s.DbName,
            UserID = s.DbUser,
            Password = s.DbPassword ?? "",
            SslMode = MySqlSslMode.None,
            AllowUserVariables = true
        };

        return new MySqlConnection(csb.ConnectionString);
    }
}