$ErrorActionPreference = "Stop"

$JsonFile = "school-dtr-licenses.json"
$PrivateKeyFile = "private_key.pem"

try {
    if (!(Test-Path $JsonFile)) {
        throw "JSON file not found: $JsonFile"
    }

    if (!(Test-Path $PrivateKeyFile)) {
        throw "Private key not found: $PrivateKeyFile"
    }

    $json = Get-Content $JsonFile -Raw | ConvertFrom-Json
    $payloadJson = $json.payload | ConvertTo-Json -Depth 50 -Compress

    [System.IO.File]::WriteAllText("payload.tmp.json", $payloadJson, [System.Text.UTF8Encoding]::new($false))

    $OpenSsl = "C:\Program Files\Git\usr\bin\openssl.exe"

if (!(Test-Path $OpenSsl)) {
    throw "OpenSSL not found. Install Git for Windows or OpenSSL."
}

& $OpenSsl dgst -sha256 -sign $PrivateKeyFile -out signature.bin payload.tmp.json

    if ($LASTEXITCODE -ne 0) {
        throw "OpenSSL signing failed. Check if OpenSSL is installed and private_key.pem is valid."
    }

    $signature = [Convert]::ToBase64String([System.IO.File]::ReadAllBytes("signature.bin"))

    $json.signature = $signature
    $json | ConvertTo-Json -Depth 50 | Set-Content $JsonFile -Encoding UTF8

    Remove-Item payload.tmp.json -ErrorAction SilentlyContinue
    Remove-Item signature.bin -ErrorAction SilentlyContinue

    Write-Host ""
    Write-Host "License file signed successfully!"
    Write-Host ""
    Write-Host "New Signature:"
    Write-Host $signature
}
catch {
    Write-Host ""
    Write-Host "SIGNING FAILED:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    pause
    exit 1
}