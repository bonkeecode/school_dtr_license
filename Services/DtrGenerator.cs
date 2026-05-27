using System;
using System.Data;
using MySqlConnector;

namespace SchoolDTR.Services;

public static class DtrGenerator
{
    public static void GenerateMonth(int year, int month)
    {
        using var conn = Db.GetConnection();
        conn.Open();

        EnsureDtrTable(conn);

        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        var employees = LoadActiveEmployees(conn);

        foreach (DataRow emp in employees.Rows)
        {
            string employeeNo = Convert.ToString(emp["employee_no"]) ?? "";
            string fullName = Convert.ToString(emp["full_name"]) ?? "";
            string biometricUserId = Convert.ToString(emp["biometric_user_id"]) ?? "";

            for (var date = start; date <= end; date = date.AddDays(1))
            {
                var slots = GetDailySlots(conn, biometricUserId, date);

                UpsertDtrRow(
                    conn,
                    employeeNo,
                    fullName,
                    date,
                    slots.MorningIn,
                    slots.MorningOut,
                    slots.AfternoonIn,
                    slots.AfternoonOut
                );
            }
        }

        DtrEventApplier.ApplyEventsAndWeekends(year, month);
    }

    private static void EnsureDtrTable(MySqlConnection conn)
    {
        using var cmd = new MySqlCommand(@"
            CREATE TABLE IF NOT EXISTS biometric_dtr (
                id INT AUTO_INCREMENT PRIMARY KEY,
                employee_id VARCHAR(50) NOT NULL,
                employee_name VARCHAR(255) NOT NULL,
                log_date DATE NOT NULL,
                morning_in VARCHAR(50) NULL,
                morning_out VARCHAR(50) NULL,
                afternoon_in VARCHAR(50) NULL,
                afternoon_out VARCHAR(50) NULL,
                remarks VARCHAR(255) NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at DATETIME NULL,
                UNIQUE KEY uq_employee_date (employee_id, log_date)
            );
        ", conn);

        cmd.ExecuteNonQuery();
    }

    private static DataTable LoadActiveEmployees(MySqlConnection conn)
    {
        using var cmd = new MySqlCommand(@"
            SELECT
                employee_no,
                biometric_user_id,
                full_name
            FROM employees
            WHERE is_active = 1
            ORDER BY full_name ASC;
        ", conn);

        using var da = new MySqlDataAdapter(cmd);
        var dt = new DataTable();
        da.Fill(dt);

        return dt;
    }

        private static DailySlots GetDailySlots(
            MySqlConnection conn,
            string biometricUserId,
            DateTime date)
        {
            if (string.IsNullOrWhiteSpace(biometricUserId))
            {
                return new DailySlots();
            }

            using var cmd = new MySqlCommand(@"
                SELECT punch_time
                FROM biometric_raw_logs
                WHERE biometric_user_id = @biometric_user_id
                AND DATE(punch_time) = @log_date
                ORDER BY punch_time ASC;
            ", conn);

            cmd.Parameters.AddWithValue("@biometric_user_id", biometricUserId);
            cmd.Parameters.AddWithValue("@log_date", date.Date);

            using var reader = cmd.ExecuteReader();

            DateTime? morningIn = null;
            DateTime? morningOut = null;
            DateTime? afternoonIn = null;
            DateTime? afternoonOut = null;

            while (reader.Read())
            {
                var log = reader.GetDateTime("punch_time");
                var time = log.TimeOfDay;

                // ===============================
                // MORNING IN
                // 04:00 AM - 11:59 AM
                // Earliest log
                // ===============================
                if (time >= new TimeSpan(4, 0, 0) &&
                    time <= new TimeSpan(11, 59, 59))
                {
                    if (morningIn == null || log < morningIn)
                        morningIn = log;
                }

                // ===============================
                // NOON WINDOW
                // 12:00 PM - 02:59 PM
                // Morning OUT = earliest
                // Afternoon IN = latest
                // ===============================
                if (time >= new TimeSpan(12, 0, 0) &&
                    time <= new TimeSpan(14, 59, 59))
                {
                    // Morning OUT = earliest noon punch
                    if (morningOut == null || log < morningOut)
                        morningOut = log;

                    // Afternoon IN = latest noon punch
                    if (afternoonIn == null || log > afternoonIn)
                        afternoonIn = log;
                }

                // ===============================
                // AFTERNOON OUT
                // 03:00 PM onwards
                // Latest log
                // ===============================
                if (time >= new TimeSpan(15, 0, 0))
                {
                    if (afternoonOut == null || log > afternoonOut)
                        afternoonOut = log;
                }
            }

            return new DailySlots
            {
                MorningIn = FormatTime(morningIn),
                MorningOut = FormatTime(morningOut),
                AfternoonIn = FormatTime(afternoonIn),
                AfternoonOut = FormatTime(afternoonOut)
            };
        }

    private static void UpsertDtrRow(
        MySqlConnection conn,
        string employeeNo,
        string employeeName,
        DateTime logDate,
        string morningIn,
        string morningOut,
        string afternoonIn,
        string afternoonOut)
    {
        using var cmd = new MySqlCommand(@"
            INSERT INTO biometric_dtr
                (
                    employee_id,
                    employee_name,
                    log_date,
                    morning_in,
                    morning_out,
                    afternoon_in,
                    afternoon_out,
                    remarks
                )
            VALUES
                (
                    @employee_id,
                    @employee_name,
                    @log_date,
                    @morning_in,
                    @morning_out,
                    @afternoon_in,
                    @afternoon_out,
                    ''
                )
            ON DUPLICATE KEY UPDATE
                employee_name = VALUES(employee_name),
                morning_in = VALUES(morning_in),
                morning_out = VALUES(morning_out),
                afternoon_in = VALUES(afternoon_in),
                afternoon_out = VALUES(afternoon_out),
                remarks = '',
                updated_at = NOW();
        ", conn);

        cmd.Parameters.AddWithValue("@employee_id", employeeNo);
        cmd.Parameters.AddWithValue("@employee_name", employeeName);
        cmd.Parameters.AddWithValue("@log_date", logDate.Date);
        cmd.Parameters.AddWithValue("@morning_in", morningIn);
        cmd.Parameters.AddWithValue("@morning_out", morningOut);
        cmd.Parameters.AddWithValue("@afternoon_in", afternoonIn);
        cmd.Parameters.AddWithValue("@afternoon_out", afternoonOut);

        cmd.ExecuteNonQuery();
    }

    private static string FormatTime(DateTime? value)
    {
        return value.HasValue ? value.Value.ToString("hh:mm tt") : "";
    }

    private class DailySlots
    {
        public string MorningIn { get; set; } = "";
        public string MorningOut { get; set; } = "";
        public string AfternoonIn { get; set; } = "";
        public string AfternoonOut { get; set; } = "";
    }
}