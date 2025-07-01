@echo off
echo 🎧 Starting Podcast Seeding Process...
echo.

echo 📚 Building and running the application...
dotnet build
if %errorlevel% neq 0 (
    echo ❌ Build failed!
    pause
    exit /b 1
)

echo.
echo 🚀 Starting the API server...
start /b dotnet run
timeout /t 10 /nobreak >nul

echo.
echo 📡 Seeding podcasts...
python seed_podcasts.py

echo.
echo ✅ Podcast seeding process completed!
echo 📊 Check the output above for results.
echo.
pause
