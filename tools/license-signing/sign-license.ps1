$payloadPath = "license_payload.json"
$privateKeyPath = "private_key.pem"
$outputPath = "school-dtr-licenses.json"

# Read payload
$payload = Get-Content $payloadPath -Raw
$payloadCompact = ($payload | ConvertFrom-Json | ConvertTo-Json -Compress -Depth 20)

# Read PEM private key
$pem = Get-Content $privateKeyPath -Raw
$pem = $pem.Replace("-----BEGIN PRIVATE KEY-----", "")
$pem = $pem.Replace("-----END PRIVATE KEY-----", "")
$pem = $pem.Replace("`r", "")
$pem = $pem.Replace("`n", "")

$keyBytes = [Convert]::FromBase64String($pem)

# Create RSA and import PKCS8
$rsa = [System.Security.Cryptography.RSA]::Create()
$rsa.ImportPkcs8PrivateKey($keyBytes, [ref]0)

# Sign payload
$bytes = [System.Text.Encoding]::UTF8.GetBytes($payloadCompact)

$signatureBytes = $rsa.SignData(
    $bytes,
    [System.Security.Cryptography.HashAlgorithmName]::SHA256,
    [System.Security.Cryptography.RSASignaturePadding]::Pkcs1
)

$signature = [Convert]::ToBase64String($signatureBytes)

# Create signed JSON
$result = [ordered]@{
    payload = ($payloadCompact | ConvertFrom-Json)
    signature = $signature
}

$result | ConvertTo-Json -Depth 20 | Set-Content $outputPath -Encoding UTF8

Write-Host ""
Write-Host "Signed license file created:"
Write-Host $outputPath