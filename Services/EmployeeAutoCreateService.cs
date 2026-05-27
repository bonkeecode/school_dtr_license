using MySqlConnector;

namespace SchoolDTR.Services;

public static class EmployeeAutoCreateService
{
    public static async Task<int> CreateMissingEmployeesFromRawLogsAsync()
    {
        await using var conn = Db.GetConnection();
        await conn.OpenAsync();

        const string sql = @"
            INSERT INTO employees
                (school_id, employee_no, biometric_user_id, full_name, position_title, is_active, created_at, updated_at)
            SELECT DISTINCT
                r.school_id,
                r.biometric_user_id,
                r.biometric_user_id,
                COALESCE(NULLIF(TRIM(r.employee_name), ''), CONCAT('Employee ', r.biometric_user_id)),
                '',
                1,
                NOW(),
                NOW()
            FROM biometric_raw_logs r
            WHERE r.biometric_user_id IS NOT NULL
              AND r.biometric_user_id <> ''
              AND NOT EXISTS (
                    SELECT 1
                    FROM employees e
                    WHERE e.school_id = r.school_id
                      AND e.biometric_user_id = r.biometric_user_id
              );

            SELECT ROW_COUNT();
        ";

        await using var cmd = new MySqlCommand(sql, conn);
        var created = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        await UpdateEmployeeNamesFromRawLogsAsync(conn);

        return created;
    }

    private static async Task UpdateEmployeeNamesFromRawLogsAsync(MySqlConnection conn)
    {
        const string sql = @"
            UPDATE employees e
            JOIN (
                SELECT 
                    school_id,
                    biometric_user_id,
                    MAX(NULLIF(TRIM(employee_name), '')) AS employee_name
                FROM biometric_raw_logs
                WHERE employee_name IS NOT NULL
                  AND TRIM(employee_name) <> ''
                GROUP BY school_id, biometric_user_id
            ) r
                ON r.school_id = e.school_id
               AND r.biometric_user_id = e.biometric_user_id
            SET e.full_name = r.employee_name,
                e.updated_at = NOW()
            WHERE r.employee_name IS NOT NULL
              AND r.employee_name <> ''
              AND e.full_name <> r.employee_name;
        ";

        await using var cmd = new MySqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }
}