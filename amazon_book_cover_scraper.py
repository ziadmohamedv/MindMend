#!/usr/bin/env python3
"""
Amazon Book Cover Scraper

This script reads books from book.json and searches Amazon for each book
to extract and download their cover images.
"""

import json
import os
import requests
from urllib.parse import urlencode, quote_plus
import time
import re
from bs4 import BeautifulSoup
import urllib.request
from pathlib import Path

class AmazonBookCoverScraper:
    def __init__(self):
        self.session = requests.Session()
        # Set a user agent to avoid being blocked
        self.session.headers.update({
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36'
        })
        self.delay = 2  # Delay between requests to be respectful
        
    def search_amazon_book(self, title, author=None):
        """Search for a book on Amazon and return the first result URL"""
        try:
            # Construct search query
            query = title
            if author and author != "غير محدد":  # Skip "Unknown" authors
                query += f" {author}"
            
            # Amazon search URL
            search_url = f"https://www.amazon.com/s?{urlencode({'k': query, 'i': 'stripbooks'})}"
            
            print(f"Searching for: {query}")
            
            response = self.session.get(search_url)
            response.raise_for_status()
            
            soup = BeautifulSoup(response.content, 'html.parser')
            
            # Find the first book result
            book_links = soup.find_all('a', {'class': 's-link-style'})
            
            if not book_links:
                # Try alternative selector
                book_links = soup.find_all('h2', {'class': 's-result-item'})
                if book_links:
                    link = book_links[0].find('a')
                    if link:
                        return f"https://www.amazon.com{link['href']}"
            else:
                return f"https://www.amazon.com{book_links[0]['href']}"
                
            return None
            
        except Exception as e:
            print(f"Error searching for book '{title}': {e}")
            return None
    
    def extract_cover_image_url(self, book_url):
        """Extract the cover image URL from an Amazon book page"""
        try:
            response = self.session.get(book_url)
            response.raise_for_status()
            
            soup = BeautifulSoup(response.content, 'html.parser')
            
            # Try multiple selectors for cover images
            selectors = [
                '#landingImage',
                '#ebooksImgBlkFront',
                '.a-dynamic-image',
                '[data-a-image-name="landingImage"]',
                'img[alt*="cover"]',
                'img[alt*="Cover"]'
            ]
            
            for selector in selectors:
                img = soup.select_one(selector)
                if img and img.get('src'):
                    # Get the highest resolution version
                    src = img['src']
                    # Replace size parameters to get larger image
                    if '_SX' in src or '_SY' in src:
                        src = re.sub(r'_S[XY]\d+_', '_SX500_', src)
                    return src
                elif img and img.get('data-src'):
                    src = img['data-src']
                    if '_SX' in src or '_SY' in src:
                        src = re.sub(r'_S[XY]\d+_', '_SX500_', src)
                    return src
            
            return None
            
        except Exception as e:
            print(f"Error extracting cover image from {book_url}: {e}")
            return None
    
    def download_image(self, image_url, filename):
        """Download an image from URL and save it"""
        try:
            # Create covers directory if it doesn't exist
            covers_dir = Path("covers")
            covers_dir.mkdir(exist_ok=True)
            
            filepath = covers_dir / filename
            
            response = self.session.get(image_url)
            response.raise_for_status()
            
            with open(filepath, 'wb') as f:
                f.write(response.content)
            
            print(f"Downloaded: {filepath}")
            return str(filepath)
            
        except Exception as e:
            print(f"Error downloading image {image_url}: {e}")
            return None
    
    def sanitize_filename(self, filename):
        """Sanitize filename for saving"""
        # Remove invalid characters for Windows/Unix
        invalid_chars = '<>:"/\\|?*'
        for char in invalid_chars:
            filename = filename.replace(char, '_')
        return filename[:100]  # Limit length
    
    def process_books(self, json_file='book.json'):
        """Process all books from the JSON file"""
        try:
            with open(json_file, 'r', encoding='utf-8') as f:
                data = json.load(f)
            
            books = data.get('books', [])
            print(f"Found {len(books)} books to process")
            
            results = []
            
            for i, book in enumerate(books, 1):
                print(f"\n[{i}/{len(books)}] Processing: {book['title']}")
                
                # Check if we already have the URL (for books that already have Amazon URLs)
                if 'amazon.com' in book.get('url', ''):
                    book_url = book['url']
                    print(f"Using existing Amazon URL: {book_url}")
                else:
                    # Search for the book on Amazon
                    book_url = self.search_amazon_book(book['title'], book.get('author'))
                    if not book_url:
                        print(f"Could not find Amazon page for: {book['title']}")
                        results.append({
                            'title': book['title'],
                            'author': book.get('author', ''),
                            'status': 'not_found',
                            'amazon_url': None,
                            'cover_image': None
                        })
                        continue
                
                # Extract cover image URL
                cover_url = self.extract_cover_image_url(book_url)
                if not cover_url:
                    print(f"Could not find cover image for: {book['title']}")
                    results.append({
                        'title': book['title'],
                        'author': book.get('author', ''),
                        'status': 'no_cover',
                        'amazon_url': book_url,
                        'cover_image': None
                    })
                    continue
                
                # Download the cover image
                filename = f"{self.sanitize_filename(book['title'])}.jpg"
                local_path = self.download_image(cover_url, filename)
                
                if local_path:
                    results.append({
                        'title': book['title'],
                        'author': book.get('author', ''),
                        'status': 'success',
                        'amazon_url': book_url,
                        'cover_image': local_path,
                        'cover_url': cover_url
                    })
                else:
                    results.append({
                        'title': book['title'],
                        'author': book.get('author', ''),
                        'status': 'download_failed',
                        'amazon_url': book_url,
                        'cover_image': None,
                        'cover_url': cover_url
                    })
                
                # Be respectful to Amazon's servers
                time.sleep(self.delay)
            
            # Save results
            with open('cover_extraction_results.json', 'w', encoding='utf-8') as f:
                json.dump(results, f, indent=2, ensure_ascii=False)
            
            # Print summary
            success_count = sum(1 for r in results if r['status'] == 'success')
            print(f"\n=== SUMMARY ===")
            print(f"Total books processed: {len(books)}")
            print(f"Successfully downloaded covers: {success_count}")
            print(f"Failed to find: {sum(1 for r in results if r['status'] == 'not_found')}")
            print(f"No cover found: {sum(1 for r in results if r['status'] == 'no_cover')}")
            print(f"Download failed: {sum(1 for r in results if r['status'] == 'download_failed')}")
            print(f"Results saved to: cover_extraction_results.json")
            print(f"Cover images saved to: covers/ directory")
            
            return results
            
        except Exception as e:
            print(f"Error processing books: {e}")
            return []

def main():
    scraper = AmazonBookCoverScraper()
    
    # Check if book.json exists
    if not os.path.exists('book.json'):
        print("Error: book.json file not found!")
        print("Please make sure the book.json file is in the same directory as this script.")
        return
    
    print("Amazon Book Cover Scraper")
    print("=" * 40)
    print("This script will:")
    print("1. Read books from book.json")
    print("2. Search for each book on Amazon (if not already an Amazon URL)")
    print("3. Extract and download cover images")
    print("4. Save results to cover_extraction_results.json")
    print("5. Save cover images to covers/ directory")
    print("\nNote: This process may take a while due to respectful delays between requests.")
    
    input("\nPress Enter to continue...")
    
    results = scraper.process_books()

if __name__ == "__main__":
    main()
