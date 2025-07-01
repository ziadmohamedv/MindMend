#!/usr/bin/env python3
"""
Book Cover Renamer

This script renames the cover images to match the exact book titles from book.json
"""

import json
import os
import shutil
from pathlib import Path
import re

def sanitize_filename(filename):
    """Sanitize filename for saving (same logic as original scraper)"""
    invalid_chars = '<>:"/\\|?*'
    for char in invalid_chars:
        filename = filename.replace(char, '_')
    return filename[:100]  # Limit length

def find_matching_image(title, covers_dir):
    """Find the existing cover image that matches the book title"""
    sanitized_title = sanitize_filename(title)
    expected_filename = f"{sanitized_title}.jpg"
    
    # Check if exact match exists
    if (covers_dir / expected_filename).exists():
        return expected_filename
    
    # If not found, try to find a close match
    for image_file in covers_dir.glob("*.jpg"):
        # Remove file extension and compare
        image_name = image_file.stem
        if image_name.lower() == sanitized_title.lower():
            return image_file.name
    
    return None

def rename_cover_images():
    """Rename cover images to match book titles from book.json"""
    
    # Read the book.json file
    try:
        with open('book.json', 'r', encoding='utf-8') as f:
            data = json.load(f)
    except FileNotFoundError:
        print("Error: book.json not found!")
        return
    except json.JSONDecodeError:
        print("Error: Invalid JSON in book.json!")
        return
    
    books = data.get('books', [])
    covers_dir = Path('covers')
    
    if not covers_dir.exists():
        print("Error: covers directory not found!")
        return
    
    print(f"Found {len(books)} books in book.json")
    print(f"Processing cover images in: {covers_dir}")
    
    # Create backup directory
    backup_dir = Path('covers_backup')
    if not backup_dir.exists():
        backup_dir.mkdir()
        print(f"Created backup directory: {backup_dir}")
    
    # Get list of existing cover images
    existing_images = list(covers_dir.glob("*.jpg"))
    print(f"Found {len(existing_images)} cover images")
    
    # Read the extraction results to map successful downloads
    results_mapping = {}
    if os.path.exists('cover_extraction_results.json'):
        try:
            with open('cover_extraction_results.json', 'r', encoding='utf-8') as f:
                results = json.load(f)
                for result in results:
                    if result.get('status') == 'success' and result.get('cover_image'):
                        # Extract just the filename from the path
                        cover_path = result['cover_image']
                        if '/' in cover_path or '\\' in cover_path:
                            filename = Path(cover_path).name
                        else:
                            filename = cover_path
                        results_mapping[result['title']] = filename
        except:
            print("Warning: Could not read cover_extraction_results.json")
    
    renamed_count = 0
    not_found_count = 0
    
    for i, book in enumerate(books, 1):
        title = book['title']
        print(f"\n[{i}/{len(books)}] Processing: {title}")
        
        # First, try to find the image using the results mapping
        current_filename = results_mapping.get(title)
        if current_filename and (covers_dir / current_filename).exists():
            current_path = covers_dir / current_filename
        else:
            # Fall back to finding by sanitized title match
            current_filename = find_matching_image(title, covers_dir)
            if current_filename:
                current_path = covers_dir / current_filename
            else:
                print(f"  ❌ No cover image found for: {title}")
                not_found_count += 1
                continue
        
        # Generate the new filename based on exact book title
        new_filename = f"{sanitize_filename(title)}.jpg"
        new_path = covers_dir / new_filename
        
        # Skip if already correctly named
        if current_path.name == new_filename:
            print(f"  ✅ Already correctly named: {new_filename}")
            continue
        
        try:
            # Create backup
            backup_path = backup_dir / current_path.name
            shutil.copy2(current_path, backup_path)
            
            # Rename the file
            current_path.rename(new_path)
            print(f"  ✅ Renamed: {current_path.name} → {new_filename}")
            renamed_count += 1
            
        except Exception as e:
            print(f"  ❌ Error renaming {current_path.name}: {e}")
    
    # Summary
    print(f"\n=== RENAMING SUMMARY ===")
    print(f"Total books processed: {len(books)}")
    print(f"Successfully renamed: {renamed_count}")
    print(f"No cover image found: {not_found_count}")
    print(f"Already correctly named: {len(books) - renamed_count - not_found_count}")
    print(f"Backup copies saved to: {backup_dir}/")
    
    # Show final count of cover images
    final_images = list(covers_dir.glob("*.jpg"))
    print(f"Final count of cover images: {len(final_images)}")

def create_mapping_report():
    """Create a report showing the mapping between book titles and cover images"""
    
    try:
        with open('book.json', 'r', encoding='utf-8') as f:
            data = json.load(f)
    except:
        print("Error reading book.json")
        return
    
    books = data.get('books', [])
    covers_dir = Path('covers')
    
    mapping_report = []
    
    for book in books:
        title = book['title']
        author = book.get('author', 'Unknown')
        condition = book.get('condition', 'Unknown')
        
        # Check if cover exists
        expected_filename = f"{sanitize_filename(title)}.jpg"
        cover_path = covers_dir / expected_filename
        
        mapping_report.append({
            'title': title,
            'author': author,
            'condition': condition,
            'cover_filename': expected_filename,
            'cover_exists': cover_path.exists(),
            'cover_path': str(cover_path) if cover_path.exists() else None
        })
    
    # Save the mapping report
    with open('book_cover_mapping.json', 'w', encoding='utf-8') as f:
        json.dump(mapping_report, f, indent=2, ensure_ascii=False)
    
    # Print summary
    total_books = len(mapping_report)
    with_covers = sum(1 for item in mapping_report if item['cover_exists'])
    without_covers = total_books - with_covers
    
    print(f"\n=== MAPPING REPORT ===")
    print(f"Total books: {total_books}")
    print(f"Books with covers: {with_covers}")
    print(f"Books without covers: {without_covers}")
    print(f"Coverage: {(with_covers/total_books)*100:.1f}%")
    print(f"Mapping report saved to: book_cover_mapping.json")

def main():
    print("Book Cover Renamer")
    print("=" * 50)
    print("This script will:")
    print("1. Read book titles from book.json")
    print("2. Find matching cover images in covers/ directory")
    print("3. Rename cover images to match exact book titles")
    print("4. Create backups of original files")
    print("5. Generate a mapping report")
    print("\nStarting process...")
    
    # Step 1: Rename the cover images
    rename_cover_images()
    
    # Step 2: Create mapping report
    create_mapping_report()
    
    print("\n✅ Process completed!")
    print("Check book_cover_mapping.json for the complete mapping report.")

if __name__ == "__main__":
    main()
