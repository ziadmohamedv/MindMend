using Microsoft.EntityFrameworkCore;
using Mind_Mend.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Mind_Mend.Services;

public class PodcastSeederService
{
    private readonly MindMendDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PodcastSeederService> _logger;
    private readonly HttpClient _httpClient;

    public PodcastSeederService(
        MindMendDbContext context,
        IWebHostEnvironment environment,
        ILogger<PodcastSeederService> logger,
        HttpClient httpClient)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
        _httpClient = httpClient;

        // Set user agent to avoid being blocked
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
    }

    public async Task SeedPodcastsFromJsonAsync()
    {
        try
        {
            _logger.LogInformation("Starting podcast seeding process...");

            // Read podcasts from JSON file
            var jsonPath = Path.Combine(_environment.ContentRootPath, "podcast.json");
            if (!File.Exists(jsonPath))
            {
                _logger.LogError("podcast.json file not found at: {JsonPath}", jsonPath);
                return;
            }

            var jsonContent = await File.ReadAllTextAsync(jsonPath);
            var jsonDoc = JsonDocument.Parse(jsonContent);

            // Handle both podcasts and books from the JSON
            var podcasts = new List<PodcastJsonModel>();

            // Parse podcasts array
            if (jsonDoc.RootElement.TryGetProperty("podcasts", out var podcastsArray))
            {
                foreach (var podcastElement in podcastsArray.EnumerateArray())
                {
                    var podcast = ParsePodcastElement(podcastElement, isPodcast: true);
                    if (podcast != null) podcasts.Add(podcast);
                }
            }

            // Parse books array (treating them as podcast-like content)
            if (jsonDoc.RootElement.TryGetProperty("books", out var booksArray))
            {
                foreach (var bookElement in booksArray.EnumerateArray())
                {
                    var book = ParsePodcastElement(bookElement, isPodcast: false);
                    if (book != null) podcasts.Add(book);
                }
            }

            _logger.LogInformation("Found {PodcastCount} valid podcasts/books in JSON", podcasts.Count);

            // Ensure uploads directory exists
            var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);

            // Create fallback image if it doesn't exist
            var fallbackImagePath = await CreateFallbackImageAsync();

            var successCount = 0;
            var skippedCount = 0;
            var errorCount = 0;

            foreach (var podcast in podcasts)
            {
                try
                {
                    // Check if podcast already exists
                    var existingPodcast = await _context.Resources
                        .FirstOrDefaultAsync(r => r.Name == podcast.Name && r.Type == Type.Podcast);

                    if (existingPodcast != null)
                    {
                        _logger.LogDebug("Podcast already exists: {Name}", podcast.Name);
                        skippedCount++;
                        continue;
                    }

                    // Try to extract and download cover image
                    var imageUuid = await ProcessPodcastCoverAsync(podcast, fallbackImagePath);

                    // Create resource entity
                    var resource = new Resource
                    {
                        Name = podcast.Name,
                        Author = podcast.Host ?? "Unknown Host",
                        ContentUrl = podcast.Url ?? "#",
                        Summary = podcast.Description ?? "Mental health podcast",
                        FilePathUuid = imageUuid,
                        Type = Type.Podcast
                    };

                    _context.Resources.Add(resource);
                    await _context.SaveChangesAsync();

                    successCount++;
                    _logger.LogDebug("Successfully added podcast: {Name}", podcast.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing podcast: {Name}", podcast.Name);
                    errorCount++;
                }
            }

            _logger.LogInformation("Podcast seeding completed. Success: {Success}, Skipped: {Skipped}, Errors: {Errors}",
                successCount, skippedCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during podcast seeding process");
            throw;
        }
    }

    private PodcastJsonModel? ParsePodcastElement(JsonElement element, bool isPodcast)
    {
        var podcast = new PodcastJsonModel { IsPodcast = isPodcast };

        // For podcasts, use 'name', for books use 'title'
        if (isPodcast)
        {
            if (element.TryGetProperty("name", out var nameProp))
                podcast.Name = nameProp.GetString();
            if (element.TryGetProperty("host", out var hostProp))
                podcast.Host = hostProp.GetString();
        }
        else
        {
            if (element.TryGetProperty("title", out var titleProp))
                podcast.Name = titleProp.GetString();
            if (element.TryGetProperty("author", out var authorProp))
                podcast.Host = authorProp.GetString();
        }

        if (element.TryGetProperty("description", out var descProp))
            podcast.Description = descProp.GetString();

        if (element.TryGetProperty("url", out var urlProp))
            podcast.Url = urlProp.GetString();

        if (element.TryGetProperty("condition", out var condProp))
        {
            if (condProp.ValueKind == JsonValueKind.Array)
            {
                var conditions = new List<string>();
                foreach (var conditionElement in condProp.EnumerateArray())
                {
                    if (conditionElement.GetString() is string condition)
                        conditions.Add(condition);
                }
                podcast.Conditions = conditions;
            }
            else if (condProp.ValueKind == JsonValueKind.String)
            {
                podcast.Conditions = new List<string> { condProp.GetString()! };
            }
        }

        // Only add podcasts/books that have at least name and some basic info
        if (!string.IsNullOrEmpty(podcast.Name) && !string.IsNullOrEmpty(podcast.Url))
        {
            return podcast;
        }

        return null;
    }

    private async Task<string> ProcessPodcastCoverAsync(PodcastJsonModel podcast, string fallbackImagePath)
    {
        try
        {
            string? imageUrl = null;

            // Try to extract image based on URL type
            if (!string.IsNullOrEmpty(podcast.Url))
            {
                if (podcast.Url.Contains("youtube.com") || podcast.Url.Contains("youtu.be"))
                {
                    imageUrl = await ExtractYouTubeChannelImageAsync(podcast.Url);
                }
                else if (podcast.Url.Contains("amazon.com"))
                {
                    imageUrl = await ExtractAmazonImageAsync(podcast.Url);
                }
            }

            // If we found an image URL, download it
            if (!string.IsNullOrEmpty(imageUrl))
            {
                var downloadedImagePath = await DownloadImageAsync(imageUrl, podcast.Name);
                if (!string.IsNullOrEmpty(downloadedImagePath))
                {
                    _logger.LogDebug("Successfully downloaded image for: {Name}", podcast.Name);
                    return downloadedImagePath;
                }
            }

            // Use fallback image
            var fallbackUuid = await CopyFallbackImageAsync(fallbackImagePath);
            _logger.LogDebug("Using fallback image for: {Name}", podcast.Name);
            return fallbackUuid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing cover for podcast: {Name}", podcast.Name);
            return await CopyFallbackImageAsync(fallbackImagePath);
        }
    }

    private async Task<string?> ExtractYouTubeChannelImageAsync(string youtubeUrl)
    {
        try
        {
            // Extract channel ID or handle from various YouTube URL formats
            string? channelIdentifier = ExtractYouTubeChannelIdentifier(youtubeUrl);
            if (string.IsNullOrEmpty(channelIdentifier))
                return null;

            // Try to get channel page HTML
            var channelPageUrl = youtubeUrl.Contains("/@") ? youtubeUrl : $"https://www.youtube.com/{channelIdentifier}";
            var response = await _httpClient.GetStringAsync(channelPageUrl);

            // Parse HTML to find channel avatar
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            // Try multiple selectors for channel avatar
            var imageSelectors = new[]
            {
                "//link[@rel='image_src']/@href",
                "//meta[@property='og:image']/@content",
                "//img[@id='img']/@src",
                "//img[contains(@class, 'avatar')]/@src"
            };

            foreach (var selector in imageSelectors)
            {
                var imageNode = doc.DocumentNode.SelectSingleNode(selector);
                if (imageNode != null)
                {
                    var imageUrl = imageNode.GetAttributeValue("href", null) ??
                                 imageNode.GetAttributeValue("content", null) ??
                                 imageNode.GetAttributeValue("src", null);

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        // Make sure it's a full URL
                        if (imageUrl.StartsWith("//"))
                            imageUrl = "https:" + imageUrl;
                        else if (imageUrl.StartsWith("/"))
                            imageUrl = "https://www.youtube.com" + imageUrl;

                        _logger.LogDebug("Found YouTube channel image: {ImageUrl}", imageUrl);
                        return imageUrl;
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract YouTube channel image from: {Url}", youtubeUrl);
            return null;
        }
    }

    private async Task<string?> ExtractAmazonImageAsync(string amazonUrl)
    {
        try
        {
            var response = await _httpClient.GetStringAsync(amazonUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            // Try multiple selectors for Amazon product images
            var imageSelectors = new[]
            {
                "//img[@id='landingImage']/@src",
                "//img[@id='ebooksImgBlkFront']/@src",
                "//img[contains(@class, 'a-dynamic-image')]/@src",
                "//meta[@property='og:image']/@content"
            };

            foreach (var selector in imageSelectors)
            {
                var imageNode = doc.DocumentNode.SelectSingleNode(selector);
                if (imageNode != null)
                {
                    var imageUrl = imageNode.GetAttributeValue("src", null) ??
                                 imageNode.GetAttributeValue("content", null);

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        // Get higher resolution version if possible
                        if (imageUrl.Contains("_SX") || imageUrl.Contains("_SY"))
                        {
                            imageUrl = Regex.Replace(imageUrl, @"_S[XY]\d+_", "_SX500_");
                        }

                        _logger.LogDebug("Found Amazon product image: {ImageUrl}", imageUrl);
                        return imageUrl;
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract Amazon image from: {Url}", amazonUrl);
            return null;
        }
    }

    private string? ExtractYouTubeChannelIdentifier(string youtubeUrl)
    {
        // Handle different YouTube URL formats
        var patterns = new[]
        {
            @"youtube\.com/channel/([a-zA-Z0-9_-]+)",
            @"youtube\.com/c/([a-zA-Z0-9_-]+)",
            @"youtube\.com/user/([a-zA-Z0-9_-]+)",
            @"youtube\.com/@([a-zA-Z0-9_-]+)",
            @"youtube\.com/([a-zA-Z0-9_-]+)$"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(youtubeUrl, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    private async Task<string?> DownloadImageAsync(string imageUrl, string podcastName)
    {
        try
        {
            var response = await _httpClient.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();

            var imageBytes = await response.Content.ReadAsByteArrayAsync();

            // Determine file extension from content type or URL
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            var extension = contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                _ => ".jpg"
            };

            // Generate unique filename
            var uuid = Guid.NewGuid().ToString() + extension;
            var destinationPath = Path.Combine(_environment.WebRootPath, "uploads", uuid);

            await File.WriteAllBytesAsync(destinationPath, imageBytes);
            return uuid;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download image from: {ImageUrl}", imageUrl);
            return null;
        }
    }

    private async Task<string> CopyFallbackImageAsync(string fallbackImagePath)
    {
        try
        {
            var fallbackUuid = "fallback-podcast-cover.jpg";
            var fallbackDestination = Path.Combine(_environment.WebRootPath, "uploads", fallbackUuid);

            if (!File.Exists(fallbackDestination) && File.Exists(fallbackImagePath))
            {
                File.Copy(fallbackImagePath, fallbackDestination, true);
            }

            return fallbackUuid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying fallback image");
            return "default-cover.jpg";
        }
    }

    private async Task<string> CreateFallbackImageAsync()
    {
        var fallbackPath = Path.Combine(_environment.WebRootPath, "uploads", "fallback-podcast-cover.jpg");

        if (!File.Exists(fallbackPath))
        {
            // Check if we have any book cover image we can use as fallback
            var coversDir = Path.Combine(_environment.ContentRootPath, "covers");
            if (Directory.Exists(coversDir))
            {
                var firstCover = Directory.GetFiles(coversDir, "*.jpg").FirstOrDefault();
                if (firstCover != null)
                {
                    File.Copy(firstCover, fallbackPath, true);
                    _logger.LogInformation("Created fallback podcast image from: {FirstCover}", Path.GetFileName(firstCover));
                    return fallbackPath;
                }
            }

            // Create a simple placeholder image if no covers available
            await CreateSimplePlaceholderImageAsync(fallbackPath);
        }

        return fallbackPath;
    }

    private async Task CreateSimplePlaceholderImageAsync(string path)
    {
        // Simple 1x1 pixel JPEG placeholder
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
        _logger.LogInformation("Created simple placeholder podcast image at: {Path}", path);
    }

    public async Task<int> GetPodcastsCountAsync()
    {
        return await _context.Resources.CountAsync(r => r.Type == Type.Podcast);
    }

    public async Task ClearAllPodcastsAsync()
    {
        var podcasts = await _context.Resources.Where(r => r.Type == Type.Podcast).ToListAsync();
        _context.Resources.RemoveRange(podcasts);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Cleared {Count} podcasts from database", podcasts.Count);
    }
}

public class PodcastJsonModel
{
    public string? Name { get; set; }
    public string? Host { get; set; }
    public string? Description { get; set; }
    public List<string>? Conditions { get; set; }
    public string? Url { get; set; }
    public bool IsPodcast { get; set; } = true;
}
