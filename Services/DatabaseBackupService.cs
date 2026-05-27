using MySqlConnector;
using System.Text;

namespace SchoolDTR.Services;

public static class DatabaseBackupService
{
    public static string Backup()
    {
        var settings = AppSettingsService.Load();

        var backupDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "SchoolDTR_Backups"
        );

        Directory.CreateDirectory(backupDir);

        var filePath = Path.Combine(
            backupDir,
            $"school_dtr_backup_{DateTime.Now:yyyyMMdd_HHmmss}.sql"
        );

        using var conn = Db.GetConnection();
        conn.Open();

        var tables = GetTables(conn);

        var sb = new StringBuilder();

        sb.AppendLine("-- School DTR Database Backup");
        sb.AppendLine($"-- School: {settings.SchoolName}");
        sb.AppendLine($"-- School ID: {settings.SchoolId}");
        sb.AppendLine($"-- Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        foreach (var table in tables)
        {
            BackupTable(conn, table, sb);
        }

        File.WriteAllText(filePath, sb.ToString());

        return filePath;
    }

    private static List<string> GetTables(MySqlConnection conn)
    {
        var tables = new List<string>();

        using var cmd = new MySqlCommand("SHOW TABLES;", conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
            tables.Add(reader.GetString(0));

        return tables;
    }

    private static void BackupTable(MySqlConnection conn, string table, StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine($"-- Table: {table}");
        sb.AppendLine($"DROP TABLE IF EXISTS `{table}`;");

        using (var createCmd = new MySqlCommand($"SHOW CREATE TABLE `{table}`;", conn))
        using (var reader = createCmd.ExecuteReader())
        {
            if (reader.Read())
            {
                sb.AppendLine(reader.GetString(1) + ";");
            }
        }

        using var dataCmd = new MySqlCommand($"SELECT * FROM `{table}`;", conn);
        using var dataReader = dataCmd.ExecuteReader();

        while (dataReader.Read())
        {
            var values = new List<string>();

            for (int i = 0; i < dataReader.FieldCount; i++)
            {
                if (dataReader.IsDBNull(i))
                {
                    values.Add("NULL");
                }
                else
                {
                    var value = dataReader.GetValue(i).ToString()?
                        .Replace("\\", "\\\\")
                        .Replace("'", "\\'");

                    values.Add($"'{value}'");
                }
            }

            sb.AppendLine($"INSERT INTO `{table}` VALUES ({string.Join(",", values)});");
        }
    }
}