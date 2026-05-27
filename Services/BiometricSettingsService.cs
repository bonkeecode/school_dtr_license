using MySqlConnector;
using SchoolDTR.Models;

namespace SchoolDTR.Services;

public static class BiometricSettingsService
{
    public static async Task<BiometricDeviceSettings> GetActiveDeviceAsync()
    {
        await using var conn = Db.GetConnection();
        await conn.OpenAsync();

        const string sql = @"
            SELECT school_id, device_model, device_ip, device_port, machine_number, biometric_serial
            FROM biometric_devices
            WHERE school_id = @school_id AND is_active = 1
            ORDER BY id DESC
            LIMIT 1;";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@school_id", AppConfig.SchoolCode);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new BiometricDeviceSettings
            {
                SchoolId = reader.GetString("school_id"),
                DeviceModel = reader.GetString("device_model"),
                DeviceIp = reader.GetString("device_ip"),
                DevicePort = reader.GetInt32("device_port"),
                MachineNumber = reader.GetInt32("machine_number"),
                DeviceSerial = reader.IsDBNull(reader.GetOrdinal("biometric_serial")) ? null : reader.GetString("biometric_serial")
            };
        }

        return new BiometricDeviceSettings();
    }

    public static async Task SaveAsync(BiometricDeviceSettings settings)
    {
        await using var conn = Db.GetConnection();
        await conn.OpenAsync();

        const string deactivate = @"
            UPDATE biometric_devices
            SET is_active = 0
            WHERE school_id = @school_id;";
        await using (var cmd = new MySqlCommand(deactivate, conn))
        {
            cmd.Parameters.AddWithValue("@school_id", AppConfig.SchoolCode);
            await cmd.ExecuteNonQueryAsync();
        }

        const string insert = @"
            INSERT INTO biometric_devices
                (school_id, device_model, device_ip, device_port, machine_number, biometric_serial, is_active)
            VALUES
                (@school_id, @device_model, @device_ip, @device_port, @machine_number, @biometric_serial, 1);";

        await using (var cmd = new MySqlCommand(insert, conn))
        {
            cmd.Parameters.AddWithValue("@school_id", AppConfig.SchoolCode);
            cmd.Parameters.AddWithValue("@device_model", settings.DeviceModel);
            cmd.Parameters.AddWithValue("@device_ip", settings.DeviceIp);
            cmd.Parameters.AddWithValue("@device_port", settings.DevicePort);
            cmd.Parameters.AddWithValue("@machine_number", settings.MachineNumber);
            cmd.Parameters.AddWithValue("@biometric_serial", string.IsNullOrWhiteSpace(settings.DeviceSerial) ? DBNull.Value : settings.DeviceSerial);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
