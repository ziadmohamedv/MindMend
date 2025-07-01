#!/usr/bin/env python3
"""
Book Database Seeder

This script calls the API endpoint to seed the database with books from book.json
"""

import requests
import time

def seed_books(base_url="https://localhost:7140"):
    """Seed books into the database"""
    
    print("📚 Starting book database seeding process...")
    
    try:
        # Check current book count
        print("🔍 Checking current book count...")
        count_response = requests.get(f"{base_url}/api/Seed/books/count", verify=False)
        
        if count_response.status_code == 200:
            current_count = count_response.json().get('totalBooks', 0)
            print(f"📊 Current books in database: {current_count}")
        else:
            print(f"⚠️ Could not get current count: {count_response.status_code}")
            current_count = "unknown"
        
        # Seed books
        print("🌱 Seeding books from book.json...")
        seed_response = requests.post(f"{base_url}/api/Seed/books", verify=False)
        
        if seed_response.status_code == 200:
            result = seed_response.json()
            print(f"✅ {result.get('message', 'Success')}")
            print(f"📈 Total books after seeding: {result.get('totalBooks', 'unknown')}")
        else:
            print(f"❌ Seeding failed: {seed_response.status_code}")
            try:
                error = seed_response.json()
                print(f"Error details: {error}")
            except:
                print(f"Response text: {seed_response.text}")
                
    except requests.exceptions.ConnectionError:
        print("❌ Could not connect to the API. Make sure the server is running.")
        print("   Default URL: https://localhost:7140")
        print("   You can also try: http://localhost:5000")
    except Exception as e:
        print(f"❌ Unexpected error: {e}")

def refresh_books(base_url="https://localhost:7140"):
    """Clear existing books and re-seed from JSON"""
    
    print("🔄 Refreshing book database...")
    
    try:
        refresh_response = requests.post(f"{base_url}/api/Seed/books/refresh", verify=False)
        
        if refresh_response.status_code == 200:
            result = refresh_response.json()
            print(f"✅ {result.get('message', 'Success')}")
            print(f"📈 Total books after refresh: {result.get('totalBooks', 'unknown')}")
        else:
            print(f"❌ Refresh failed: {refresh_response.status_code}")
            try:
                error = refresh_response.json()
                print(f"Error details: {error}")
            except:
                print(f"Response text: {refresh_response.text}")
                
    except requests.exceptions.ConnectionError:
        print("❌ Could not connect to the API. Make sure the server is running.")
    except Exception as e:
        print(f"❌ Unexpected error: {e}")

def main():
    print("Mind-Mend Book Database Seeder")
    print("=" * 50)
    
    print("\nOptions:")
    print("1. Seed books (add new books only)")
    print("2. Refresh books (clear and re-seed all)")
    print("3. Check book count")
    
    choice = input("\nEnter your choice (1-3): ").strip()
    
    base_url = input("Enter API URL (press Enter for https://localhost:7140): ").strip()
    if not base_url:
        base_url = "https://localhost:7140"
    
    # Disable SSL warnings for localhost
    import urllib3
    urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)
    
    if choice == "1":
        seed_books(base_url)
    elif choice == "2":
        confirm = input("⚠️ This will delete all existing books. Continue? (yes/no): ").lower().strip()
        if confirm == "yes":
            refresh_books(base_url)
        else:
            print("❌ Operation cancelled.")
    elif choice == "3":
        try:
            count_response = requests.get(f"{base_url}/api/Seed/books/count", verify=False)
            if count_response.status_code == 200:
                current_count = count_response.json().get('totalBooks', 0)
                print(f"📊 Current books in database: {current_count}")
            else:
                print(f"❌ Could not get count: {count_response.status_code}")
        except Exception as e:
            print(f"❌ Error: {e}")
    else:
        print("❌ Invalid choice.")

if __name__ == "__main__":
    main()
