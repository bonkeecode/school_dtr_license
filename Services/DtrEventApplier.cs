using System;
using System.Collections.Generic;
using MySqlConnector;

namespace SchoolDTR.Services;

public static class DtrEventApplier
{
    public static void ApplyEventsAndWeekends(int year, int month)
    {
        using var conn = Db.GetConnection();
        conn.Open();

        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        EnsureEventDayScopeColumn(conn);
        NormalizeDtrEmployeeColumns(conn, start, end);

        // Order matters:
        // 1. Weekends first
        // 2. Assigned events after, so events override time logs/weekends
        ApplyWeekends(conn, start, end);
        ApplyAssignedEvents(conn, start, end);
    }

    private static void EnsureEventDayScopeColumn(MySqlConnection conn)
    {
        try
        {
            using var cmd = new MySqlCommand(@"
                ALTER TABLE dtr_events
                ADD COLUMN day_scope VARCHAR(20)
                NOT NULL DEFAULT 'WHOLE_DAY'
                AFTER event_type;
            ", conn);

            cmd.ExecuteNonQuery();
        }
        catch
        {
            // Column already exists or table is not created yet.
            // EventForm also creates this column. This is only a safe fallback.
        }
    }

    private static void NormalizeDtrEmployeeColumns(
        MySqlConnection conn,
        DateTime start,
        DateTime end)
    {
        using var cmd = new MySqlCommand(@"
            UPDATE biometric_dtr d
            LEFT JOIN employees e
                ON e.employee_no = COALESCE(NULLIF(d.employee_no, ''), NULLIF(d.employee_id, ''))
            SET
                d.employee_no = COALESCE(NULLIF(d.employee_no, ''), NULLIF(d.employee_id, '')),
                d.employee_id = COALESCE(NULLIF(d.employee_id, ''), NULLIF(d.employee_no, '')),
                d.biometric_user_id = COALESCE(NULLIF(d.biometric_user_id, ''), e.biometric_user_id)
            WHERE d.id > 0
              AND d.log_date BETWEEN @start AND @end;
        ", conn);

        cmd.Parameters.AddWithValue("@start", start.Date);
        cmd.Parameters.AddWithValue("@end", end.Date);
        cmd.ExecuteNonQuery();
    }

    private static void ApplyWeekends(MySqlConnection conn, DateTime start, DateTime end)
    {
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday &&
                date.DayOfWeek != DayOfWeek.Sunday)
                continue;

            string label = date.DayOfWeek == DayOfWeek.Saturday
                ? "Saturday"
                : "Sunday";

            using var weekendCmd = new MySqlCommand(@"
                UPDATE biometric_dtr
                SET
                    morning_in = @label,
                    morning_out = @label,
                    afternoon_in = @label,
                    afternoon_out = @label,
                    remarks = ''
                WHERE log_date = @log_date;
            ", conn);

            weekendCmd.Parameters.AddWithValue("@label", label);
            weekendCmd.Parameters.AddWithValue("@log_date", date.Date);
            weekendCmd.ExecuteNonQuery();
        }
    }

    private static void ApplyAssignedEvents(MySqlConnection conn, DateTime start, DateTime end)
    {
        var assignments = new List<EventAssignment>();

        using (var selectCmd = new MySqlCommand(@"
            SELECT
                e.id,
                e.event_title,
                e.date_from,
                e.date_to,
                COALESCE(NULLIF(e.day_scope, ''), 'WHOLE_DAY') AS day_scope,
                a.employee_no
            FROM dtr_events e
            INNER JOIN dtr_event_assignments a
                ON a.event_id = e.id
            WHERE e.date_from <= @end
              AND e.date_to >= @start
            ORDER BY e.date_from ASC, e.id ASC;
        ", conn))
        {
            selectCmd.Parameters.AddWithValue("@start", start.Date);
            selectCmd.Parameters.AddWithValue("@end", end.Date);

            using var reader = selectCmd.ExecuteReader();

            while (reader.Read())
            {
                assignments.Add(new EventAssignment
                {
                    EventTitle = Convert.ToString(reader["event_title"]) ?? "",
                    DateFrom = Convert.ToDateTime(reader["date_from"]).Date,
                    DateTo = Convert.ToDateTime(reader["date_to"]).Date,
                    DayScope = NormalizeDayScope(Convert.ToString(reader["day_scope"])),
                    EmployeeNo = Convert.ToString(reader["employee_no"]) ?? ""
                });
            }
        }

        foreach (var item in assignments)
        {
            if (string.IsNullOrWhiteSpace(item.EmployeeNo))
                continue;

            var from = item.DateFrom < start ? start : item.DateFrom;
            var to = item.DateTo > end ? end : item.DateTo;

            string updateSql = item.DayScope switch
            {
                "MORNING" => @"
                    UPDATE biometric_dtr
                    SET
                        morning_in = @label,
                        morning_out = @label,
                        remarks = ''
                    WHERE COALESCE(NULLIF(employee_no, ''), NULLIF(employee_id, '')) = @employee_no
                      AND log_date BETWEEN @from AND @to;
                ",

                "AFTERNOON" => @"
                    UPDATE biometric_dtr
                    SET
                        afternoon_in = @label,
                        afternoon_out = @label,
                        remarks = ''
                    WHERE COALESCE(NULLIF(employee_no, ''), NULLIF(employee_id, '')) = @employee_no
                      AND log_date BETWEEN @from AND @to;
                ",

                _ => @"
                    UPDATE biometric_dtr
                    SET
                        morning_in = @label,
                        morning_out = @label,
                        afternoon_in = @label,
                        afternoon_out = @label,
                        remarks = ''
                    WHERE COALESCE(NULLIF(employee_no, ''), NULLIF(employee_id, '')) = @employee_no
                      AND log_date BETWEEN @from AND @to;
                "
            };

            using var eventUpdateCmd = new MySqlCommand(updateSql, conn);

            eventUpdateCmd.Parameters.AddWithValue("@label", item.EventTitle);
            eventUpdateCmd.Parameters.AddWithValue("@employee_no", item.EmployeeNo);
            eventUpdateCmd.Parameters.AddWithValue("@from", from.Date);
            eventUpdateCmd.Parameters.AddWithValue("@to", to.Date);

            eventUpdateCmd.ExecuteNonQuery();
        }
    }

    private static string NormalizeDayScope(string? value)
    {
        value = (value ?? "").Trim().ToUpperInvariant();

        return value switch
        {
            "MORNING" => "MORNING",
            "AFTERNOON" => "AFTERNOON",
            _ => "WHOLE_DAY"
        };
    }

    private class EventAssignment
    {
        public string EventTitle { get; set; } = "";
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string DayScope { get; set; } = "WHOLE_DAY";
        public string EmployeeNo { get; set; } = "";
    }
}
