using System.Diagnostics;
using System.Text.Json;
using MySqlConnector;
using SchoolDTR.Models;

namespace SchoolDTR.Services;

public static class BiometricFetchService
{
    public static async Task<BiometricFetchResult> FetchLogsAsync(DateTime fromDate, DateTime toDate)
    {
        var device = await BiometricSettingsService.GetActiveDeviceAsync();
        long fetchLogId = await StartFetchLogAsync(device);

        try
        {
            await EnsureRawLogDuplicateProtectionAsync();

            string scriptPath = Path.Combine(AppContext.BaseDirectory, "tools", "fetch_zkteco_k14.py");
            if (!File.Exists(scriptPath))
            {
                scriptPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "tools", "fetch_zkteco_k14.py"));
            }

            if (!File.Exists(scriptPath))
                throw new FileNotFoundException("Python fetcher not found.", scriptPath);

            string args = $"\"{scriptPath}\" --school {AppConfig.SchoolCode} --ip {device.DeviceIp} --port {device.DevicePort} --machine {device.MachineNumber} --from {fromDate:yyyy-MM-dd} --to {toDate:yyyy-MM-dd}";

            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            psi.Environment["DTR_MYSQL_HOST"] = "localhost";
            psi.Environment["DTR_MYSQL_PORT"] = "3306";
            psi.Environment["DTR_MYSQL_DB"] = "school_dtr_305680";
            psi.Environment["DTR_MYSQL_USER"] = "root";
            psi.Environment["DTR_MYSQL_PASSWORD"] = "";

            using var process = Process.Start(psi) ?? throw new InvalidOperationException("Unable to start Python fetcher.");
            string stdout = await process.StandardOutput.ReadToEndAsync();
            string stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new InvalidOperationException(stderr.Trim().Length > 0 ? stderr : stdout);

            var result = JsonSerializer.Deserialize<BiometricFetchResult>(stdout, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new BiometricFetchResult { Success = false, Message = "Fetcher returned empty response." };

            result.RawOutput = stdout;
            await FinishFetchLogAsync(fetchLogId, result);
            return result;
        }
        catch (Exception ex)
        {
            var result = new BiometricFetchResult
            {
                Success = false,
                Message = ex.Message,
                RawOutput = ex.ToString()
            };

            await FinishFetchLogAsync(fetchLogId, result);
            return result;
        }
    }

    private static async Task EnsureRawLogDuplicateProtectionAsync()
    {
        await using var conn = Db.GetConnection();
        await conn.OpenAsync();

        // Normalize nullable fields first so the unique key can properly detect duplicates.
        const string normalizeSql = @"
            UPDATE biometric_raw_logs
            SET punch_type = IFNULL(punch_type, ''),
                device_serial = IFNULL(device_serial, '')
            WHERE punch_type IS NULL
               OR device_serial IS NULL;";

        await using (var normalizeCmd = new MySqlCommand(normalizeSql, conn))
        {
            await normalizeCmd.ExecuteNonQueryAsync();
        }

        // Remove existing duplicates, keeping the lowest ID.
        const string deleteDuplicatesSql = @"
            DELETE r1
            FROM biometric_raw_logs r1
            INNER JOIN biometric_raw_logs r2
                ON r1.id > r2.id
                AND r1.school_id = r2.school_id
                AND r1.biometric_user_id = r2.biometric_user_id
                AND r1.punch_time = r2.punch_time
                AND IFNULL(r1.punch_type, '') = IFNULL(r2.punch_type, '')
                AND IFNULL(r1.device_serial, '') = IFNULL(r2.device_serial, '');";

        await using (var deleteCmd = new MySqlCommand(deleteDuplicatesSql, conn))
        {
            await deleteCmd.ExecuteNonQueryAsync();
        }

        // Add permanent duplicate protection.
        // Ignore error 1061 = duplicate key name already exists.
        const string alterNullSql = @"
            ALTER TABLE biometric_raw_logs
            MODIFY punch_type varchar(30) NOT NULL DEFAULT '',
            MODIFY device_serial varchar(100) NOT NULL DEFAULT '';";

        try
        {
            await using var alterNullCmd = new MySqlCommand(alterNullSql, conn);
            await alterNullCmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Safe to ignore if table definition is already compatible.
        }

        const string uniqueKeySql = @"
            ALTER TABLE biometric_raw_logs
            ADD UNIQUE KEY uq_biometric_raw_unique (
                school_id,
                biometric_user_id,
                punch_time,
                punch_type,
                device_serial
            );";

        try
        {
            await using var keyCmd = new MySqlCommand(uniqueKeySql, conn);
            await keyCmd.ExecuteNonQueryAsync();
        }
        catch (MySqlException ex) when (ex.Number == 1061)
        {
            // Unique key already exists.
        }
    }

    private static async Task<long> StartFetchLogAsync(BiometricDeviceSettings device)
    {
        await using var conn = Db.GetConnection();
        await conn.OpenAsync();

        const string sql = @"
            INSERT INTO biometric_fetch_logs
                (school_id, device_ip, fetch_started_at, status)
            VALUES
                (@school_id, @device_ip, NOW(), 'RUNNING');
            SELECT LAST_INSERT_ID();";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@school_id", AppConfig.SchoolCode);
        cmd.Parameters.AddWithValue("@device_ip", device.DeviceIp);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }

    private static async Task FinishFetchLogAsync(long id, BiometricFetchResult result)
    {
        await using var conn = Db.GetConnection();
        await conn.OpenAsync();

        const string sql = @"
            UPDATE biometric_fetch_logs
            SET fetch_finished_at = NOW(),
                total_logs = @total_logs,
                inserted_logs = @inserted_logs,
                duplicate_logs = @duplicate_logs,
                status = @status,
                error_message = @error_message
            WHERE id = @id;";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@total_logs", result.TotalLogs);
        cmd.Parameters.AddWithValue("@inserted_logs", result.InsertedLogs);
        cmd.Parameters.AddWithValue("@duplicate_logs", result.DuplicateLogs);
        cmd.Parameters.AddWithValue("@status", result.Success ? "SUCCESS" : "FAILED");
        cmd.Parameters.AddWithValue("@error_message", result.Success ? DBNull.Value : result.Message);
        await cmd.ExecuteNonQueryAsync();
    }
}