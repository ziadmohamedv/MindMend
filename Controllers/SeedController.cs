using Microsoft.AspNetCore.Mvc;
using Mind_Mend.Services;

namespace Mind_Mend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SeedController : ControllerBase
{
    private readonly BookSeederService _bookSeederService;
    private readonly PodcastSeederService _podcastSeederService;
    private readonly ILogger<SeedController> _logger;

    public SeedController(BookSeederService bookSeederService, PodcastSeederService podcastSeederService, ILogger<SeedController> logger)
    {
        _bookSeederService = bookSeederService;
        _podcastSeederService = podcastSeederService;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the database with books from book.json
    /// </summary>
    [HttpPost("books")]
    public async Task<IActionResult> SeedBooks()
    {
        try
        {
            _logger.LogInformation("Book seeding requested");
            await _bookSeederService.SeedBooksFromJsonAsync();

            var count = await _bookSeederService.GetBooksCountAsync();
            return Ok(new
            {
                message = "Books seeded successfully",
                totalBooks = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding books");
            return StatusCode(500, new { message = "Error seeding books", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets the current count of books in the database
    /// </summary>
    [HttpGet("books/count")]
    public async Task<IActionResult> GetBooksCount()
    {
        try
        {
            var count = await _bookSeederService.GetBooksCountAsync();
            return Ok(new { totalBooks = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting books count");
            return StatusCode(500, new { message = "Error getting books count", error = ex.Message });
        }
    }

    /// <summary>
    /// Clears all books from the database (use with caution)
    /// </summary>
    [HttpDelete("books")]
    public async Task<IActionResult> ClearBooks()
    {
        try
        {
            _logger.LogWarning("Book clearing requested");
            await _bookSeederService.ClearAllBooksAsync();
            return Ok(new { message = "All books cleared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing books");
            return StatusCode(500, new { message = "Error clearing books", error = ex.Message });
        }
    }

    /// <summary>
    /// Re-seeds books (clears existing and adds from JSON)
    /// </summary>
    [HttpPost("books/refresh")]
    public async Task<IActionResult> RefreshBooks()
    {
        try
        {
            _logger.LogInformation("Book refresh requested");

            // Clear existing books
            await _bookSeederService.ClearAllBooksAsync();

            // Seed new books
            await _bookSeederService.SeedBooksFromJsonAsync();

            var count = await _bookSeederService.GetBooksCountAsync();
            return Ok(new
            {
                message = "Books refreshed successfully",
                totalBooks = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing books"); return StatusCode(500, new { message = "Error refreshing books", error = ex.Message });
        }
    }

    /// <summary>
    /// Seeds the database with podcasts from podcast.json
    /// </summary>
    [HttpPost("podcasts")]
    public async Task<IActionResult> SeedPodcasts()
    {
        try
        {
            _logger.LogInformation("Podcast seeding requested");
            await _podcastSeederService.SeedPodcastsFromJsonAsync();

            var count = await _podcastSeederService.GetPodcastsCountAsync();
            return Ok(new
            {
                message = "Podcasts seeded successfully",
                totalPodcasts = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding podcasts");
            return StatusCode(500, new { message = "Error seeding podcasts", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets the current count of podcasts in the database
    /// </summary>
    [HttpGet("podcasts/count")]
    public async Task<IActionResult> GetPodcastsCount()
    {
        try
        {
            var count = await _podcastSeederService.GetPodcastsCountAsync();
            return Ok(new { totalPodcasts = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting podcasts count");
            return StatusCode(500, new { message = "Error getting podcasts count", error = ex.Message });
        }
    }

    /// <summary>
    /// Clears all podcasts from the database (use with caution)
    /// </summary>
    [HttpDelete("podcasts")]
    public async Task<IActionResult> ClearPodcasts()
    {
        try
        {
            _logger.LogWarning("Podcast clearing requested");
            await _podcastSeederService.ClearAllPodcastsAsync();
            return Ok(new { message = "All podcasts cleared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing podcasts");
            return StatusCode(500, new { message = "Error clearing podcasts", error = ex.Message });
        }
    }

    /// <summary>
    /// Re-seeds podcasts (clears existing and adds from JSON)
    /// </summary>
    [HttpPost("podcasts/refresh")]
    public async Task<IActionResult> RefreshPodcasts()
    {
        try
        {
            _logger.LogInformation("Podcast refresh requested");

            // Clear existing podcasts
            await _podcastSeederService.ClearAllPodcastsAsync();

            // Seed new podcasts
            await _podcastSeederService.SeedPodcastsFromJsonAsync();

            var count = await _podcastSeederService.GetPodcastsCountAsync();
            return Ok(new
            {
                message = "Podcasts refreshed successfully",
                totalPodcasts = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing podcasts");
            return StatusCode(500, new { message = "Error refreshing podcasts", error = ex.Message });
        }
    }
}
