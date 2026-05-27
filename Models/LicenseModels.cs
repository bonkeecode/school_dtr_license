namespace SchoolDTR.Models;

public class LicenseRoot
{
    public string? app { get; set; }
    public List<LicenseRecord> licenses { get; set; } = new();
}

public class LicenseRecord
{
    public string? school_code { get; set; }
    public string? school_name { get; set; }
    public string? device_hash { get; set; }
    public string? biometric_hash { get; set; }
    public string? status { get; set; }
    public DateTime expiry { get; set; }
}
