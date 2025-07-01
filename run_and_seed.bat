@echo off
echo Starting Mind-Mend API Server...
echo Make sure to run this first before seeding books.
echo.
echo Starting the server in background...
start "Mind-Mend API" dotnet run

echo Waiting for server to start...
timeout /t 10 /nobreak

echo.
echo Running book seeder...
python seed_books.py

pause
