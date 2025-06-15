# Run this script on a Windows machine with the Windows SDK installed.
# It will create a code-signing certificate.
# This certificate is used in the GitHub Actions workflow to sign the Windows installer.

# get the platform version from the windows SDK manifest file
$windowsSdkManifestPath = "C:\Program Files (x86)\Windows Kits\10\SDKManifest.xml"
$windowsSdkManifest = [xml](Get-Content $windowsSdkManifestPath)
$platformVersion = $windowsSdkManifest.FileList | Where-Object { $_.PlatformIdentity -match 'UAP, Version=(\d+\.\d+\.\d+\.\d+)' } | ForEach-Object { $_.PlatformIdentity -replace 'UAP, Version=', '' }
Write-Host "Fouund Windows SDK with Platform Version: $platformVersion"

# get the paths for makecert and pvk2pfx
$makecertPath = "C:\Program Files (x86)\Windows Kits\10\bin\$platformVersion\x64\makecert.exe"
$pvk2pfxPath = "C:\Program Files (x86)\Windows Kits\10\bin\$platformVersion\x64\pvk2pfx.exe"

# remove existing cert files
Remove-Item -Path 'cert.cer', 'cert.pvk', 'cert.pfx', 'cert.base64.txt'  -ErrorAction SilentlyContinue

& $makecertPath /pe /n "CN=Jack Buehner" /r /h 0 /eku "1.3.6.1.5.5.7.3.3,1.3.6.1.4.1.311.10.3.13" /e "12/31/2099" cert.cer -sv cert.pvk

& $pvk2pfxPath -pvk cert.pvk -spc cert.cer -pfx cert.pfx 

& certutil -encode cert.pfx cert.base64.txt
