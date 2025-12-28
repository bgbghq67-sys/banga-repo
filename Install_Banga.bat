@echo off
title Banga Photobooth Installer
color 0A

echo ==================================================
echo      BANGA PHOTOBOOTH - FULL SETUP INSTALLER
echo ==================================================
echo.
echo This script will:
echo 1. Install DNP Printer Drivers
echo 2. Install Required Fonts
echo 3. Create Desktop Shortcuts
echo 4. Check for System Prerequisites
echo.
pause

:: ------------------------------------------------------
:: 1. Printer Driver Installation
:: ------------------------------------------------------
echo.
echo [1/4] Installing DNP DS-RX1HS Printer Drivers...
echo --------------------------------------------------
if exist "Drivers\DNP_Driver\DRIVER_RX1HS_WIN_11 v1.14\11\DriverInstall.CMD" (
    echo Found driver installer. Launching...
    pushd "Drivers\DNP_Driver\DRIVER_RX1HS_WIN_11 v1.14\11"
    
    :: Run the DNP installer
    call "DriverInstall.CMD"
    
    popd
    echo Printer Driver installation process completed.
) else (
    echo [WARNING] Printer driver installer not found at expected path!
    echo Please install DNP drivers manually from the Drivers folder.
)

:: ------------------------------------------------------
:: 2. Font Installation
:: ------------------------------------------------------
echo.
echo [2/4] Installing Application Fonts...
echo --------------------------------------------------
echo Installing Poppins fonts...

set "FONT_DIR=%~dp0Assets\Fonts"
set "PS_SCRIPT=%TEMP%\install_fonts.ps1"

echo $fonts = (New-Object -ComObject Shell.Application).Namespace(0x14) > "%PS_SCRIPT%"
echo Get-ChildItem "%FONT_DIR%\*.ttf" ^| ForEach-Object { >> "%PS_SCRIPT%"
echo     $fonts.CopyHere($_.FullName, 0x10) >> "%PS_SCRIPT%"
echo     Write-Host "Installed: " $_.Name >> "%PS_SCRIPT%"
echo } >> "%PS_SCRIPT%"

powershell -ExecutionPolicy Bypass -File "%PS_SCRIPT%"
del "%PS_SCRIPT%"
echo Fonts installed.

:: ------------------------------------------------------
:: 3. Desktop Shortcut Creation
:: ------------------------------------------------------
echo.
echo [3/4] Creating Desktop Shortcut...
echo --------------------------------------------------
set SCRIPT="%TEMP%\create_shortcut.vbs"
echo Set oWS = WScript.CreateObject("WScript.Shell") >> %SCRIPT%
echo sLinkFile = "%USERPROFILE%\Desktop\Banga Photobooth.lnk" >> %SCRIPT%
echo Set oLink = oWS.CreateShortcut(sLinkFile) >> %SCRIPT%
echo oLink.TargetPath = "%~dp0Banga Photobooth.exe" >> %SCRIPT%
echo oLink.WorkingDirectory = "%~dp0" >> %SCRIPT%
echo oLink.Description = "Banga Photobooth Application" >> %SCRIPT%
echo oLink.Save >> %SCRIPT%

cscript /nologo %SCRIPT%
del %SCRIPT%
echo Shortcut created on Desktop.

:: ------------------------------------------------------
:: 4. Final Checks & Instructions
:: ------------------------------------------------------
echo.
echo [4/4] Final Configuration Instructions
echo --------------------------------------------------
echo.
echo [CRITICAL] PLEASE CHECK THE FOLLOWING MANUALLY:
echo.
echo 1. PRINTER SETTINGS:
echo    - Go to Control Panel ^> Devices and Printers
echo    - Right-click 'DNP DS-RX1HS' ^> Printing Preferences
echo    - Set Paper Size to '4x6'
echo    - Set Orientation to 'Landscape' or 'Portrait' as needed
echo    - (Optional) Enable '2 inch cut' if doing strips
echo.
echo 2. CAMERA:
echo    - Install 'Canon EOS Webcam Utility' (Required!)
echo    - Connect Canon EOS 2000D via USB
echo    - Set Mode Dial to 'Movie Mode' (Video Icon)
echo    - CLOSE standard 'EOS Utility' if it opens
echo.
echo 3. INTERNET:
echo    - Ensure PC is connected to Wi-Fi for QR Code uploads
echo.
echo ==================================================
echo           INSTALLATION COMPLETE!
echo ==================================================
echo You can now run 'Banga Photobooth' from your Desktop.
echo.
pause
