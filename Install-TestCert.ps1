$script = {
    $certPath = "C:\Users\monty\Documents\Dealer Desk\DealerDeskLocalCert.cer"
    try {
        $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($certPath)
        $store = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root", "LocalMachine")
        $store.Open("ReadWrite")
        $store.Add($cert)
        $store.Close()
        Write-Host "Success! The Dealer Desk test certificate was installed to the Trusted Root." -ForegroundColor Green
    } catch {
        Write-Host "Failed to install certificate: $_" -ForegroundColor Red
    }
    Write-Host "Press any key to close this window..."
    $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
}

$encoded = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($script.ToString()))
Start-Process powershell.exe -ArgumentList "-NoProfile -ExecutionPolicy Bypass -EncodedCommand $encoded" -Verb RunAs
