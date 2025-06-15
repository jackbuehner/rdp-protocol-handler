Param(
    [switch]$Unpackaged = $false,
    [switch]$ForStore = $false,
    [string]$ForceVersion = $null, # Optional parameter to force a specific version
    [string]$ForcePublisher = $null # Optional parameter to force a specific publisher
)

function Find-CertificateByEkuAndPublisher {
  [CmdletBinding()]
  param (
      [Parameter(Mandatory=$true)]
      [string]$CertStoreLocation, # e.g., Cert:\LocalMachine or Cert:\CurrentUser

      [Parameter(Mandatory=$true)]
      [string]$CertStoreName,   # e.g., My, WebHosting

      [Parameter(Mandatory=$true)]
      [string[]]$RequiredEkus,  # Array of EKU OID strings, e.g., "1.3.6.1.5.5.7.3.3", "1.3.6.1.4.1.311.10.3.13"

      [Parameter(Mandatory=$true)]
      [string]$RequiredPublisher # The value the Publisher field must match
  )

  # Build the full path to the store
  $fullCertStorePath = "$CertStoreLocation\$CertStoreName"

  Write-Verbose "Searching for certificate in '$fullCertStorePath' with EKUs: $($RequiredEkus -join ', ') and Publisher: '$RequiredPublisher'"

  try {
      # Get certificates from the specified store and filter by EKU and Publisher
      $cert = Get-ChildItem -Path $fullCertStorePath | Where-Object {
          # Check if the certificate has any ExtendedKeyUsages AND if all required EKUs are present
          ($null -ne $_.EnhancedKeyUsageList) -and `
          (($_.EnhancedKeyUsageList | Select-Object -ExpandProperty ObjectId) | ForEach-Object {
              $certEkuOid = $_
              $RequiredEkus -contains $certEkuOid
          }) -notcontains $false -and `
          # Check if the Publisher matches the required value
          $_.Issuer -like $RequiredPublisher
      }

      # Return the found certificate object (or $null if not found)
      return $cert

  } catch {
      Write-Error "An error occurred while searching for the certificate: $($_.Exception.Message)"
      # Return $null on error
      return $null
  }
}

# check if the windows SDK is installed
$windowsSdkManifestPath = "C:\Program Files (x86)\Windows Kits\10\SDKManifest.xml"
if (-not (Test-Path -Path $windowsSdkManifestPath)) {
    Write-Output "Windows SDK not found. Please download it from Microsft's website and install the following features:"
    Write-Output ' - Windows SDK Signing Tools for Desktop Apps'
    Write-Output ' - Windows SDK for UWP Managed Apps'
    Write-Output ' - Windows SDK for Desktop C++ x86 apps'

    Write-Output ""
    Write-Output 'Exiting...'
    exit 1
}

# get the platform version from the windows SDK manifest file
$windowsSdkManifest = [xml](Get-Content $windowsSdkManifestPath)
$platformVersion = $windowsSdkManifest.FileList | Where-Object { $_.PlatformIdentity -match 'UAP, Version=(\d+\.\d+\.\d+\.\d+)' } | ForEach-Object { $_.PlatformIdentity -replace 'UAP, Version=', '' }
Write-Host "Fouund Windows SDK with Platform Version: $platformVersion"

# check that makepri.exe is available
$makepriPath = "C:\Program Files (x86)\Windows Kits\10\bin\$platformVersion\x64\makepri.exe"
if (-not (Test-Path -Path $makepriPath)) {
    Write-Output "makepri.exe not found. Please download the Windows SDK from Microsft's website and install the following features:"
    Write-Output ' - Windows SDK Signing Tools for Desktop Apps'
    Write-Output ' - Windows SDK for UWP Managed Apps'
    Write-Output ' - Windows SDK for Desktop C++ x86 apps'

    Write-Output ""
    Write-Output 'Exiting...'
    exit 1
}

# check that signtool.exe is available
$signtoolPath = "C:\Program Files (x86)\Windows Kits\10\bin\$platformVersion\x64\signtool.exe"
if (-not (Test-Path -Path $signtoolPath)) {
    Write-Output "signtool.exe not found. Please download the Windows SDK from Microsft's website and install the following features:"
    Write-Output ' - Windows SDK Signing Tools for Desktop Apps'
    Write-Output ' - Windows SDK for UWP Managed Apps'
    Write-Output ' - Windows SDK for Desktop C++ x86 apps'

    Write-Output ""
    Write-Output 'Exiting...'
    exit 1
}

# check that makeappx.exe is available
$makeappxPath = "C:\Program Files (x86)\Windows Kits\10\bin\$platformVersion\x64\makeappx.exe"
if (-not (Test-Path -Path $makeappxPath)) {
    Write-Output "makeappx.exe not found. Please download the Windows SDK from Microsft's website and install the following features:"
    Write-Output ' - Windows SDK Signing Tools for Desktop Apps'
    Write-Output ' - Windows SDK for UWP Managed Apps'
    Write-Output ' - Windows SDK for Desktop C++ x86 apps'

    Write-Output ""
    Write-Output 'Exiting...'
    exit 1
}

# check that makecert.exe is available
$makecertPath = "C:\Program Files (x86)\Windows Kits\10\bin\$platformVersion\x64\makecert.exe"
if (-not (Test-Path -Path $makecertPath)) {
    Write-Output "makecert.exe not found. Please download the Windows SDK from Microsft's website and install the following features:"
    Write-Output ' - Windows SDK Signing Tools for Desktop Apps'
    Write-Output ' - Windows SDK for UWP Managed Apps'
    Write-Output ' - Windows SDK for Desktop C++ x86 apps'

    Write-Output ""
    Write-Output 'Exiting...'
    exit 1
}








$packageDir = ".\src\package"

if ($ForStore) {
    $distDir = ".\dist\msft-store"
} else {
    $distDir = ".\dist"
}

# read the manifest xml file
$manifestPath = "$packageDir\appxmanifest.xml"
$xml = [xml](Get-Content $manifestPath)

$version = $ForceVersion
if (-not $version) {
    # if no version is provided, use the current date and time
    $version = (Get-Date).ToString("yyyy.Mdd.Hmm.0") # Microsft Store requires the last part to be 0
}
$xml.Package.Identity.Version = $version

# set the identity name in appxmanifest.xml
if ($ForStore) {
    $xml.Package.Identity.Name = "17225JackBuehner.RDPProtocolHandler"
} else {
    $xml.Package.Identity.Name = "JackBuehner.RDPProtocolHandler"
}

# Set the publisher in appxmanifest.xml
if ($ForcePublisher) {
    $publisher = $ForcePublisher
} elseif ($ForStore) {
    $publisher = "21E4C6BE-57F6-4999-923A-E201D6663071"
} else {
    $publisher = [System.Net.Dns]::GetHostName()
}
$xml.Package.Identity.Publisher = "CN=$publisher"

# set the publisher display name in appxmanifest.xml
if ($ForStore) {
    $xml.Package.Properties.PublisherDisplayName = "Jack Buehner"
} else {
    $xml.Package.Properties.PublisherDisplayName = "Jack Buehner"
}

# read the app display name from the manifest file
$AppName = $xml.Package.Properties.DisplayName

# save the updated manifest file
$xml.Save($manifestPath)
Write-Output "Manifest version set to: $version"
Write-Output "Publisher set to: $publisher"
Write-Output ""

# find a certificate with the needed EKU
$requiredEkusForSigning = @(
"1.3.6.1.5.5.7.3.3",      # code signing
"1.3.6.1.4.1.311.10.3.13" # lifestime signing
)
function Get-Certificate {
$result = Find-CertificateByEkuAndPublisher -CertStoreLocation Cert:\CurrentUser -CertStoreName My -RequiredEkus $requiredEkusForSigning -RequiredPublisher "CN=$publisher"
if ($result) {
    return $result | Select-Object -First 1
}
return $null
}
$signingCert = Get-Certificate

if ($null -eq $signingCert) {
    Write-Host "No signing certificate found with the specified EKUs in Cert:\CurrentUser\My"
    Write-Host "A new certificate will be created."

    # Create a new self-signed certificate
    Write-Output "Creating a new self-signed certificate..."
    Write-Host $publisher
    & $makecertPath /n "CN=$publisher" /r /h 0 /eku "1.3.6.1.5.5.7.3.3,1.3.6.1.4.1.311.10.3.13" /e "12/31/2099" /sk "MyKey" /sr CurrentUser /ss My
    Write-Output "Self-signed certificate created."
    $signingCert = Get-Certificate
    Write-Host ""
}

# compile the launcher code
$cscopilerPath = "$env:SystemRoot\Microsoft.NET\Framework\v4.0.30319\csc.exe"
Write-Host "Compiling launcher code..."
& $cscopilerPath /target:winexe /out:$packageDir\rdp_launcher.exe .\src\*.cs
Write-Host ""

# sign the launcher executable
Write-Output 'Signing launcher executable...'
Set-AuthenticodeSignature -FilePath $packageDir\rdp_launcher.exe -Certificate $signingCert

# delete the package metadata folder before generating the pri file
# this file is generated by windows when the appxmanifest.xml file
# is registered as an unpackaged app
$metadataPath = "./src\microsoft.system.package.metadata"
if (Test-Path -Path $metadataPath) {
    Remove-Item -Path $metadataPath -Force
}

# run makepri.exe to generate the pri file
Write-Output 'Generating pri file...'
$priConfigPath = "$packageDir\priconfig.xml"
& $makepriPath createconfig /ConfigXml $priConfigPath /dq "en-US" /Overwrite
Write-Output "Created pri config file: $priConfigPath"
$priPath = "$packageDir\resources.pri"
& $makepriPath new /ProjectRoot "$packageDir" /ConfigXml $priConfigPath /OutputFile $priPath /Overwrite
Write-Output "Created pri file: $priPath"

# if the Unpackaged parameter is set to true, install the masix package as an unpackaged app
if ($Unpackaged) {
    Write-Output 'Installing package as an unpackaged app...'
    Add-AppxPackage -Register "$packageDir\appxmanifest.xml" -ForceUpdateFromAnyVersion
}

# otherwise, package it
else {
  # create the msix package
  Write-Output 'Creating msix package...'
  & $makeappxPath pack /d "$packageDir" /p "$distDir\$AppName.msix" /Overwrite
  Write-Host ""

  # if not ForStore, sign the msix package
  if (-not $ForStore) {        
        # sign the msix package
        Write-Output 'Signing msix package...'
        & $signtoolPath sign /a /v /fd sha256 /s My /sha1 $signingCert.Thumbprint /v "$distDir\$AppName.msix"
        Write-Output "Signed msix package: installer\$AppName.msix"
        Write-Output ""
    
        # export the certificate (.cer) to the installer folder
        $certExportPath = "$distDir\$AppName.cer"
        $certificateToExport = Get-ChildItem Cert:\CurrentUser\My, Cert:\LocalMachine\My | Where-Object {$_.Thumbprint -eq $signingCert.Thumbprint}
        Export-Certificate -Cert $certificateToExport -FilePath $certExportPath -Type CER | Out-Null
        Write-Output "Exported certificate to: $certExportPath"
        # export a ps1 file that can be used to install the certificate
        $installCertScriptPath = "$distDir\install_cert.bat"
        $installCertScriptContent = @"
@echo off

rem Relaunch the script with elevated privileges if not already running as admin
if not "%1"=="elevated" (
    echo Requesting administrative privileges...
    powershell -Command "Start-Process '%~f0' -ArgumentList 'elevated' -Verb RunAs"
    exit /b
)

set certName=$AppName.cer
set certPath=%~dp0%certName%

if exist "%certPath%" (
    certutil -addstore "root" "%certPath%"
    if %errorlevel% equ 0 (
        echo Certificate installed successfully.
    ) else (
        echo Failed to install certificate. Error code: %errorlevel%
    )
) else (
    echo Certificate file not found at: %certPath%
)
"@
    Set-Content -Path $installCertScriptPath -Value $installCertScriptContent -Force
  }
}

Write-Output 'DONE'
