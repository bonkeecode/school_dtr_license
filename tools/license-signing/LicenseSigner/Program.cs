using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

string signerDir = AppContext.BaseDirectory;

while (!File.Exists(Path.Combine(signerDir, "license_payload.json")))
{
    var parent = Directory.GetParent(signerDir);

    if (parent == null)
        throw new FileNotFoundException("Could not find license_payload.json.");

    signerDir = parent.FullName;
}

string payloadPath = Path.Combine(signerDir, "license_payload.json");
string privateKeyPath = Path.Combine(signerDir, "private_key.pem");
string outputPath = Path.Combine(signerDir, "school-dtr-licenses.json");

string payload = File.ReadAllText(payloadPath);

using var payloadDoc = JsonDocument.Parse(payload);

string payloadCompact = JsonSerializer.Serialize(
    payloadDoc.RootElement,
    new JsonSerializerOptions { WriteIndented = false }
);

string privateKeyPem = File.ReadAllText(privateKeyPath);

using var rsa = RSA.Create();
rsa.ImportFromPem(privateKeyPem);

byte[] bytes = Encoding.UTF8.GetBytes(payloadCompact);

byte[] signatureBytes = rsa.SignData(
    bytes,
    HashAlgorithmName.SHA256,
    RSASignaturePadding.Pkcs1
);

string signature = Convert.ToBase64String(signatureBytes);

var result = new
{
    payload = JsonDocument.Parse(payloadCompact).RootElement,
    signature
};

string finalJson = JsonSerializer.Serialize(
    result,
    new JsonSerializerOptions { WriteIndented = true }
);

File.WriteAllText(outputPath, finalJson);

Console.WriteLine("Signed license file created:");
Console.WriteLine(outputPath);