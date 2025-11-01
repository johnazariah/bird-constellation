@echo off
echo ===============================================
echo Owlet Service Status Report
echo ===============================================
echo.

echo Service Status:
sc query OwletService 2>nul
if errorlevel 1 (
    echo   Service not found or not installed
) else (
    echo.
    echo Service Configuration:
    sc qc OwletService 2>nul
)

echo.
echo ===============================================
echo Network Status:
echo ===============================================
echo.
echo Checking HTTP port availability...
netstat -an | findstr ":5555" 2>nul
if errorlevel 1 (
    echo   Port 5555 is not in use
) else (
    echo   Port 5555 is active
)

echo.
echo ===============================================
echo File System Status:
echo ===============================================
echo.
echo Installation Directory:
if exist "C:\Program Files\Owlet\bin\Owlet.Service.exe" (
    echo   Service executable found
) else (
    echo   Service executable not found
)

echo.
echo Data Directory:
if exist "C:\ProgramData\Owlet" (
    echo   Data directory exists
) else (
    echo   Data directory not found
)

echo.
echo Log Directory:
if exist "C:\ProgramData\Owlet\Logs" (
    echo   Log directory exists
) else (
    echo   Log directory not found
)

echo.
echo ===============================================
echo Report completed: %DATE% %TIME%
echo ===============================================
pause
