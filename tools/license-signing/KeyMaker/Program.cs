using System.Security.Cryptography;

string outputDir = Directory.GetParent(AppContext.BaseDirectory)!
    .Parent!.Parent!.Parent!.FullName;

using var rsa = RSA.Create(2048);

string privatePem = rsa.ExportPkcs8PrivateKeyPem();
string publicPem = rsa.ExportSubjectPublicKeyInfoPem();

File.WriteAllText(Path.Combine(outputDir, "private_key.pem"), privatePem);
File.WriteAllText(Path.Combine(outputDir, "public_key.pem"), publicPem);

Console.WriteLine("Keys generated:");
Console.WriteLine(Path.Combine(outputDir, "private_key.pem"));
Console.WriteLine(Path.Combine(outputDir, "public_key.pem"));