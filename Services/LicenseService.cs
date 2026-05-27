using MySqlConnector;

namespace SchoolDTR.Services;

public static class LicenseService
{
    public static bool IsLicensed()
    {
        var hash = MachineFingerprintService.GetMachineHash();

        using var conn = Db.GetConnection();
        conn.Open();

        using var cmd = new MySqlCommand(@"
            SELECT COUNT(*)
            FROM system_license
            WHERE school_id = @school_id
              AND machine_hash = @machine_hash
              AND is_active = 1;
        ", conn);

        cmd.Parameters.AddWithValue("@school_id", AppConfig.SchoolCode);
        cmd.Parameters.AddWithValue("@machine_hash", hash);

        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }
}