@echo off
echo Installing required packages...
pip install -r requirements.txt

echo.
echo Running Amazon Book Cover Scraper...
python amazon_book_cover_scraper.py

echo.
echo Script completed. Check the covers/ directory for downloaded images.
pause
