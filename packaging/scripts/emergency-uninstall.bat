@echo off
echo ===============================================
echo Owlet Emergency Uninstall Script
echo ===============================================
echo.
echo This script will completely remove Owlet from your system.
echo.
set /p confirm="Are you sure you want to continue? (Y/N): "
if /i not "%confirm%"=="Y" goto :abort

echo.
echo Stopping Owlet service...
sc stop OwletService 2>nul
timeout /t 10 /nobreak >nul

echo Removing service registration...
sc delete OwletService 2>nul

echo Removing firewall rules...
netsh advfirewall firewall delete rule name="Owlet Document Service - HTTP" 2>nul

echo Removing program files...
if exist "C:\Program Files\Owlet" (
    rmdir /s /q "C:\Program Files\Owlet" 2>nul
)

echo Removing data directory...
if exist "C:\ProgramData\Owlet" (
    rmdir /s /q "C:\ProgramData\Owlet" 2>nul
)

echo Removing registry entries...
reg delete "HKLM\SYSTEM\CurrentControlSet\Services\OwletService" /f 2>nul
reg delete "HKLM\SOFTWARE\Owlet" /f 2>nul
reg delete "HKCU\Software\Owlet" /f 2>nul

echo Removing event log source...
reg delete "HKLM\SYSTEM\CurrentControlSet\Services\EventLog\Application\Owlet Service" /f 2>nul

echo.
echo ===============================================
echo Emergency uninstall completed.
echo ===============================================
echo.
echo Please reboot your computer to complete the removal.
pause
goto :end

:abort
echo.
echo Uninstall cancelled by user.
pause

:end
