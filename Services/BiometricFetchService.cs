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
            string scriptPath = Path.Combine(AppContext.BaseDirectory, "tools", "fetch_zkteco_k14.py");
            if (!File.Exists(scriptPath))
            {
                // During development, allow running from project folder too.
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
