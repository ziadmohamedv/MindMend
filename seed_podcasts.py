#!/usr/bin/env python3
"""
Script to seed podcasts via the ASP.NET Core API
"""

import requests
import json
import sys

def seed_podcasts():
    """Seed podcasts by calling the API endpoint"""
    api_url = "http://localhost:5000/api/Seed/podcasts"
    
    print("🎧 Starting podcast seeding...")
    print(f"📡 Calling API: {api_url}")
    
    try:
        response = requests.post(api_url, timeout=300)  # 5 minute timeout for image processing
        
        if response.status_code == 200:
            result = response.json()
            print(f"✅ {result['message']}")
            print(f"📊 Total podcasts: {result['totalPodcasts']}")
            return True
        else:
            print(f"❌ Error: {response.status_code}")
            if response.content:
                try:
                    error_data = response.json()
                    print(f"Error details: {error_data}")
                except:
                    print(f"Error content: {response.text}")
            return False
            
    except requests.exceptions.ConnectionError:
        print("❌ Connection error. Make sure the ASP.NET Core API is running on http://localhost:5000")
        return False
    except requests.exceptions.Timeout:
        print("⏰ Request timed out. Podcast seeding may take a while due to image processing.")
        return False
    except Exception as e:
        print(f"❌ Unexpected error: {e}")
        return False

def get_podcasts_count():
    """Get current count of podcasts"""
    api_url = "http://localhost:5000/api/Seed/podcasts/count"
    
    try:
        response = requests.get(api_url)
        if response.status_code == 200:
            result = response.json()
            print(f"📊 Current podcasts in database: {result['totalPodcasts']}")
            return result['totalPodcasts']
        else:
            print(f"❌ Error getting count: {response.status_code}")
            return None
    except Exception as e:
        print(f"❌ Error getting count: {e}")
        return None

def clear_podcasts():
    """Clear all podcasts"""
    api_url = "http://localhost:5000/api/Seed/podcasts"
    
    print("🗑️ Clearing all podcasts...")
    
    try:
        response = requests.delete(api_url)
        if response.status_code == 200:
            result = response.json()
            print(f"✅ {result['message']}")
            return True
        else:
            print(f"❌ Error clearing podcasts: {response.status_code}")
            return False
    except Exception as e:
        print(f"❌ Error clearing podcasts: {e}")
        return False

def refresh_podcasts():
    """Refresh podcasts (clear and re-seed)"""
    api_url = "http://localhost:5000/api/Seed/podcasts/refresh"
    
    print("🔄 Refreshing podcasts...")
    
    try:
        response = requests.post(api_url, timeout=300)
        if response.status_code == 200:
            result = response.json()
            print(f"✅ {result['message']}")
            print(f"📊 Total podcasts: {result['totalPodcasts']}")
            return True
        else:
            print(f"❌ Error refreshing podcasts: {response.status_code}")
            return False
    except Exception as e:
        print(f"❌ Error refreshing podcasts: {e}")
        return False

def main():
    """Main function"""
    if len(sys.argv) > 1:
        command = sys.argv[1].lower()
        
        if command == "count":
            get_podcasts_count()
        elif command == "clear":
            clear_podcasts()
        elif command == "refresh":
            refresh_podcasts()
        elif command == "seed":
            seed_podcasts()
        else:
            print("Usage: python seed_podcasts.py [seed|count|clear|refresh]")
            print("  seed    - Seed podcasts from podcast.json")
            print("  count   - Get current podcast count")
            print("  clear   - Clear all podcasts")
            print("  refresh - Clear and re-seed podcasts")
    else:
        # Default action is to seed
        print("📚 Starting podcast seeding process...")
        print("📊 Current state:")
        get_podcasts_count()
        print()
        
        if seed_podcasts():
            print()
            print("📊 Final state:")
            get_podcasts_count()
        else:
            print("\n❌ Seeding failed. Check if the API is running and try again.")

if __name__ == "__main__":
    main()
