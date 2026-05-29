using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SchoolDTR.Services;

public static class LicenseSignatureService
{
    // Replace this later with your real public key.
private const string PublicKeyPem = """
-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA6p5Vr6AXA8LajibqrZMe
b1TOLHaKG9hqsxAt9aHAsORRY1xPkU04UAqGn/QQKu3ZyZHeTWnnTsB9i6cMk+7A
paxKg7btOcu7MYGfUaOp1R2ssmPul6MEhv8wybBrgzMWBV4/XxGroId26Uc3SnaU
Bg0OTHWI0DYw18CafgVuRujcaQv/8pdRZ6/55msKOIdvRXzzY5vVu3bKJmOB2xj9
sSlHAZmGq26W7lPFsBeVxhWh+vG9jDeGK6maX3GWgmzS1OEDig9G0avozQ3LpWiD
NUltn4FBUS+whe4IOwE4BP6bjsGb6D1edSCuYvMH3noJv9XQVbd2tR9axhQ+dYgu
0QIDAQAB
-----END PUBLIC KEY-----
""";

    public static bool VerifyPayload(JsonElement payload, string signatureBase64)
    {
        try
        {
            string payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
            byte[] signatureBytes = Convert.FromBase64String(signatureBase64);

            using var rsa = RSA.Create();
            rsa.ImportFromPem(PublicKeyPem);

            return rsa.VerifyData(
                payloadBytes,
                signatureBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );
        }
        catch
        {
            return false;
        }
    }
}