namespace SchoolDTR;

public static class AppConfig
{
    public const string SchoolCode = "305680";
    public const string SchoolName = "City of Mati National High School";

    // Replace this after uploading github-license/licenses.json to GitHub.
    public const string GitHubLicenseUrl =
        "https://raw.githubusercontent.com/YOUR_USERNAME/YOUR_REPO/main/licenses.json";

    public const string ConnectionString =
        "Server=localhost;Port=3306;Database=school_dtr_305680;Uid=root;Pwd=;Allow User Variables=True;";

    // Temporary. Later, get this from the actual biometric device.
    public const string DefaultBiometricSerial = "TO_BE_REPLACED_WITH_DEVICE_SERIAL";
    public const string LicenseJsonUrl =
    "https://raw.githubusercontent.com/bonkeecode/school_dtr_license/refs/heads/main/tools/school-dtr-licenses.json";
}
