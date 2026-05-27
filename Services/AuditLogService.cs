using MySqlConnector;

namespace SchoolDTR.Services;

public static class AuditLogService
{
    public static void EnsureTable()
    {
        using var conn = Db.GetConnection();
        conn.Open();

        using var cmd = new MySqlCommand(@"
            CREATE TABLE IF NOT EXISTS audit_logs (
                id BIGINT AUTO_INCREMENT PRIMARY KEY,
                school_id VARCHAR(20) NOT NULL,
                action VARCHAR(100) NOT NULL,
                description TEXT NULL,
                performed_by VARCHAR(100) NULL,
                computer_name VARCHAR(100) NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                KEY idx_school_id (school_id),
                KEY idx_action (action),
                KEY idx_created_at (created_at)
            );
        ", conn);

        cmd.ExecuteNonQuery();
    }

    public static void Log(string actionType, string description, string performedBy = "SYSTEM")
    {
        EnsureTable();

        var settings = AppSettingsService.Load();

        using var conn = Db.GetConnection();
        conn.Open();

        using var cmd = new MySqlCommand(@"
            INSERT INTO audit_logs
                (school_id, action, description, performed_by, computer_name)
            VALUES
                (@school_id, @action, @description, @performed_by, @computer_name);
        ", conn);

        cmd.Parameters.AddWithValue("@school_id", settings.SchoolId);
        cmd.Parameters.AddWithValue("@action", actionType);
        cmd.Parameters.AddWithValue("@description", description);
        cmd.Parameters.AddWithValue("@performed_by", performedBy);
        cmd.Parameters.AddWithValue("@computer_name", Environment.MachineName);

        cmd.ExecuteNonQuery();
    }
}