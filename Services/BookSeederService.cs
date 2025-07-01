using Microsoft.EntityFrameworkCore;
using Mind_Mend.Data;
using Mind_Mend.Models;
using System.Text.Json;

namespace Mind_Mend.Services;

public class BookSeederService
{
    private readonly MindMendDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<BookSeederService> _logger;

    public BookSeederService(
        MindMendDbContext context,
        IWebHostEnvironment environment,
        ILogger<BookSeederService> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    public async Task SeedBooksFromJsonAsync()
    {
        try
        {
            _logger.LogInformation("Starting book seeding process...");

            // Read books from JSON file
            var jsonPath = Path.Combine(_environment.ContentRootPath, "book.json");
            if (!File.Exists(jsonPath))
            {
                _logger.LogError("book.json file not found at: {JsonPath}", jsonPath);
                return;
            }

            var jsonContent = await File.ReadAllTextAsync(jsonPath);
            var jsonDoc = JsonDocument.Parse(jsonContent);
            var booksArray = jsonDoc.RootElement.GetProperty("books");

            var books = new List<BookJsonModel>();
            foreach (var bookElement in booksArray.EnumerateArray())
            {
                var book = new BookJsonModel();

                if (bookElement.TryGetProperty("title", out var titleProp))
                    book.Title = titleProp.GetString();

                if (bookElement.TryGetProperty("author", out var authorProp))
                    book.Author = authorProp.GetString();

                if (bookElement.TryGetProperty("description", out var descProp))
                    book.Description = descProp.GetString();

                if (bookElement.TryGetProperty("condition", out var condProp))
                    book.Condition = condProp.GetString();

                if (bookElement.TryGetProperty("url", out var urlProp))
                    book.Url = urlProp.GetString();

                // Only add books that have at least title and some basic info
                if (!string.IsNullOrEmpty(book.Title) && !string.IsNullOrEmpty(book.Author))
                {
                    books.Add(book);
                }
            }

            _logger.LogInformation("Found {BookCount} valid books in JSON", books.Count);

            // Ensure uploads directory exists
            var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);

            // Create fallback image if it doesn't exist
            var fallbackImagePath = await CreateFallbackImageAsync();

            var successCount = 0;
            var skippedCount = 0;
            var errorCount = 0;

            foreach (var book in books)
            {
                try
                {
                    // Check if book already exists
                    var existingBook = await _context.Resources
                        .FirstOrDefaultAsync(r => r.Name == book.Title && r.Type == Type.Book);

                    if (existingBook != null)
                    {
                        _logger.LogDebug("Book already exists: {Title}", book.Title);
                        skippedCount++;
                        continue;
                    }                    // Try to find and copy cover image
                    var imageUuid = ProcessBookCover(book.Title!, fallbackImagePath);

                    // Create resource entity
                    var resource = new Resource
                    {
                        Name = book.Title!,
                        Author = book.Author ?? "غير محدد",
                        ContentUrl = book.Url ?? "#",
                        Summary = book.Description ?? "كتاب متخصص في الصحة النفسية",
                        FilePathUuid = imageUuid,
                        Type = Type.Book
                    };

                    _context.Resources.Add(resource);
                    await _context.SaveChangesAsync();

                    successCount++;
                    _logger.LogDebug("Successfully added book: {Title}", book.Title);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing book: {Title}", book.Title);
                    errorCount++;
                }
            }

            _logger.LogInformation("Book seeding completed. Success: {Success}, Skipped: {Skipped}, Errors: {Errors}",
                successCount, skippedCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during book seeding process");
            throw;
        }
    }
    private string ProcessBookCover(string bookTitle, string fallbackImagePath)
    {
        try
        {
            // Sanitize the book title to match our cover naming convention
            var sanitizedTitle = SanitizeFilename(bookTitle);
            var expectedCoverPath = Path.Combine(_environment.ContentRootPath, "covers", $"{sanitizedTitle}.jpg");

            string sourceImagePath;
            string imageExtension = ".jpg";

            // Check if cover image exists
            if (File.Exists(expectedCoverPath))
            {
                sourceImagePath = expectedCoverPath;
                _logger.LogDebug("Found cover image for: {Title}", bookTitle);
            }
            else
            {
                // Use fallback image
                sourceImagePath = fallbackImagePath;
                imageExtension = Path.GetExtension(fallbackImagePath);
                _logger.LogDebug("Using fallback image for: {Title}", bookTitle);
            }

            // Generate unique filename for uploads directory
            var uuid = Guid.NewGuid().ToString() + imageExtension;
            var destinationPath = Path.Combine(_environment.WebRootPath, "uploads", uuid);

            // Copy image to uploads directory
            File.Copy(sourceImagePath, destinationPath, true);

            return uuid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing cover for book: {Title}", bookTitle);

            // Return fallback image UUID if something goes wrong
            var fallbackUuid = "fallback-cover.jpg";
            var fallbackDestination = Path.Combine(_environment.WebRootPath, "uploads", fallbackUuid);

            if (!File.Exists(fallbackDestination) && File.Exists(fallbackImagePath))
            {
                File.Copy(fallbackImagePath, fallbackDestination, true);
            }

            return fallbackUuid;
        }
    }

    private async Task<string> CreateFallbackImageAsync()
    {
        var fallbackPath = Path.Combine(_environment.WebRootPath, "uploads", "fallback-book-cover.jpg");

        if (!File.Exists(fallbackPath))
        {
            // Check if we have any cover image we can use as fallback
            var coversDir = Path.Combine(_environment.ContentRootPath, "covers");
            if (Directory.Exists(coversDir))
            {
                var firstCover = Directory.GetFiles(coversDir, "*.jpg").FirstOrDefault();
                if (firstCover != null)
                {
                    File.Copy(firstCover, fallbackPath, true);
                    _logger.LogInformation("Created fallback image from: {FirstCover}", Path.GetFileName(firstCover));
                    return fallbackPath;
                }
            }

            // If no covers directory, create a simple placeholder
            await CreateSimplePlaceholderImageAsync(fallbackPath);
        }

        return fallbackPath;
    }

    private async Task CreateSimplePlaceholderImageAsync(string path)
    {
        // Create a simple 1x1 pixel image as placeholder
        // In a real scenario, you'd want to create or use a proper placeholder image
        var placeholderBytes = new byte[] {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x01, 0x00, 0x48,
            0x00, 0x48, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43, 0x00, 0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08,
            0x07, 0x07, 0x07, 0x09, 0x09, 0x08, 0x0A, 0x0C, 0x14, 0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12,
            0x13, 0x0F, 0x14, 0x1D, 0x1A, 0x1F, 0x1E, 0x1D, 0x1A, 0x1C, 0x1C, 0x20, 0x24, 0x2E, 0x27, 0x20,
            0x22, 0x2C, 0x23, 0x1C, 0x1C, 0x28, 0x37, 0x29, 0x2C, 0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27,
            0x39, 0x3D, 0x38, 0x32, 0x3C, 0x2E, 0x33, 0x34, 0x32, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x00, 0x01,
            0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0x02, 0x11, 0x01, 0x03, 0x11, 0x01, 0xFF, 0xC4, 0x00, 0x1F,
            0x00, 0x00, 0x01, 0x05, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0xFF, 0xC4, 0x00,
            0xB5, 0x10, 0x00, 0x02, 0x01, 0x03, 0x03, 0x02, 0x04, 0x03, 0x05, 0x05, 0x04, 0x04, 0x00, 0x00,
            0x01, 0x7D, 0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12, 0x21, 0x31, 0x41, 0x06, 0x13, 0x51,
            0x61, 0x07, 0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xA1, 0x08, 0x23, 0x42, 0xB1, 0xC1, 0x15, 0x52,
            0xD1, 0xF0, 0x24, 0x33, 0x62, 0x72, 0x82, 0x09, 0x0A, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x25, 0x26,
            0x27, 0x28, 0x29, 0x2A, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x43, 0x44, 0x45, 0x46, 0x47,
            0x48, 0x49, 0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x63, 0x64, 0x65, 0x66, 0x67,
            0x68, 0x69, 0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x83, 0x84, 0x85, 0x86, 0x87,
            0x88, 0x89, 0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5,
            0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3,
            0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA,
            0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6,
            0xF7, 0xF8, 0xF9, 0xFA, 0xFF, 0xDA, 0x00, 0x0C, 0x03, 0x01, 0x00, 0x02, 0x11, 0x03, 0x11, 0x00,
            0x3F, 0x00, 0xF7, 0xFA, 0x28, 0xA2, 0x8A, 0x00, 0x28, 0xA2, 0x8A, 0x00, 0x28, 0xA2, 0x8A, 0x00,
            0xFF, 0xD9
        };

        await File.WriteAllBytesAsync(path, placeholderBytes);
        _logger.LogInformation("Created simple placeholder image at: {Path}", path);
    }

    private static string SanitizeFilename(string filename)
    {
        // Same logic as in the scraper
        var invalidChars = "<>:\"/\\|?*";
        foreach (var invalidChar in invalidChars)
        {
            filename = filename.Replace(invalidChar, '_');
        }
        return filename.Length > 100 ? filename[..100] : filename;
    }

    public async Task<int> GetBooksCountAsync()
    {
        return await _context.Resources.CountAsync(r => r.Type == Type.Book);
    }

    public async Task ClearAllBooksAsync()
    {
        var books = await _context.Resources.Where(r => r.Type == Type.Book).ToListAsync();
        _context.Resources.RemoveRange(books);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Cleared {Count} books from database", books.Count);
    }
}

public class BookJsonModel
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Description { get; set; }
    public string? Condition { get; set; }
    public string? Url { get; set; }
}
