using MySqlConnector;

namespace SchoolDTR.Services;

public static class Db
{
    // ==========================================================
    // DATABASE CONNECTION
    // ==========================================================
    // Update this if database credentials change.
    //
    // Example:
    // server=localhost;
    // database=school_dtr_305680;
    // uid=root;
    // pwd=YOUR_PASSWORD;
    // ==========================================================

    private const string ConnectionString =
        "server=localhost;database=school_dtr_305680;uid=root;pwd=#P4ssword1;";

    public static MySqlConnection GetConnection()
    {
        return new MySqlConnection(ConnectionString);
    }
}