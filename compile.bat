:: make the dist folder if it doesn't exist
if not exist dist mkdir dist

:: ensure the dist folder is empty
if exist dist\* del /Q dist\*

:: compile the launcher code
%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe^
    /target:winexe^
    /out:.\dist\launcher.exe^
    .\src\launcher.cs

:: compile the install code
%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe^
    /target:winexe^
    /out:.\dist\install.exe^
    .\src\install.cs
