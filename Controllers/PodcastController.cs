using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mind_Mend.Data;
using Mind_Mend.DTOs;
using Microsoft.AspNetCore.StaticFiles;

namespace Mind_Mend.Controllers;

[Route("api/[controller]")]
[ApiController]
// [Authorize(AuthenticationSchemes = "Bearer", Roles = "Doctor,Therapist")]
public class PodcastController(MindMendDbContext context, IWebHostEnvironment environment, IContentTypeProvider contentTypeProvider) : ControllerBase
{
    private readonly MindMendDbContext _context = context;
    private readonly IWebHostEnvironment _environment = environment;
    private readonly IContentTypeProvider _contentTypeProvider = contentTypeProvider;

    // GET: api/Podcast
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PodcastListItemDto>>> GetPodcasts()
    {
        var podcasts = await _context.Resources
            .Where(r => r.Type == Type.Podcast)
            // .Include(r => r.Tags)
            .ToListAsync();

        return podcasts.Select(MapToListItemDto).ToList();
    }

    // GET: api/Podcast/5
    [HttpGet("{id}")]
    public async Task<ActionResult<PodcastDto>> GetPodcast(int id)
    {
        var podcast = await _context.Resources
            // .Include(r => r.Tags)
            .FirstOrDefaultAsync(r => r.Id == id && r.Type == Type.Podcast);

        if (podcast == null)
        {
            return NotFound();
        }

        var imagePath = Path.Combine(_environment.WebRootPath, "uploads", podcast.FilePathUuid);
        string? imageData = null;

        if (System.IO.File.Exists(imagePath))
        {
            var imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);
            string extension = Path.GetExtension(imagePath).ToLowerInvariant();
            string contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
            imageData = $"data:{contentType};base64,{Convert.ToBase64String(imageBytes)}";
        }

        return MapToDto(podcast, imageData);
    }

    // GET: api/Podcast/image/{uuid}
    [HttpGet("image/{uuid}")]
    public async Task<IActionResult> GetImage(string uuid)
    {
        var podcast = await _context.Resources
            .FirstOrDefaultAsync(r => r.FilePathUuid == uuid && r.Type == Type.Podcast);

        if (podcast == null)
        {
            return NotFound();
        }

        var imagePath = Path.Combine(_environment.WebRootPath, "uploads", podcast.FilePathUuid);
        if (!System.IO.File.Exists(imagePath))
        {
            return NotFound();
        }

        if (!_contentTypeProvider.TryGetContentType(imagePath, out string? contentType))
        {
            contentType = "application/octet-stream";
        }

        var imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);
        return File(imageBytes, contentType);
    }

    // POST: api/Podcast
    [HttpPost]
    public async Task<ActionResult<PodcastDto>> CreatePodcast([FromForm] CreateBookDto createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Get the file extension from the uploaded image
        string fileExtension = Path.GetExtension(createDto.Image.FileName).ToLowerInvariant();
        if (!IsValidImageExtension(fileExtension))
        {
            return BadRequest("Invalid image format. Supported formats are: .jpg, .jpeg, .png, .gif, .bmp, .webp");
        }

        var uuid = Guid.NewGuid().ToString() + fileExtension;  // Include the file extension in the UUID
        var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);

        var filePath = Path.Combine(uploadsDir, uuid);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await createDto.Image.CopyToAsync(stream);
        }

        var podcast = new Resource
        {
            Name = createDto.Name,
            Author = createDto.Author,
            ContentUrl = createDto.BookUrl,
            Summary = createDto.Summary,
            FilePathUuid = uuid,
            Type = Type.Podcast
        };

        _context.Resources.Add(podcast);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPodcast), new { id = podcast.Id }, MapToDto(podcast));
    }

    // PUT: api/Podcast/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePodcast(int id, UpdatePodcastDto updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var podcast = await _context.Resources
            .FirstOrDefaultAsync(r => r.Id == id && r.Type == Type.Podcast);

        if (podcast == null)
        {
            return NotFound();
        }

        if (updateDto.Name != null)
        {
            podcast.Name = updateDto.Name;
        }
        if (updateDto.Author != null)
        {
            podcast.Author = updateDto.Author;
        }
        if (updateDto.PodcastUrl != null)
        {
            podcast.ContentUrl = updateDto.PodcastUrl;
        }
        if (updateDto.Summary != null)
        {
            podcast.Summary = updateDto.Summary;
        }
        if (updateDto.FilePathUuid != null)
        {
            podcast.FilePathUuid = updateDto.FilePathUuid;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!PodcastExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    // DELETE: api/Podcast/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePodcast(int id)
    {
        var podcast = await _context.Resources
            .FirstOrDefaultAsync(r => r.Id == id && r.Type == Type.Podcast);

        if (podcast == null)
        {
            return NotFound();
        }

        // Delete the image file if it exists
        var imagePath = Path.Combine(_environment.WebRootPath, "uploads", podcast.FilePathUuid);
        if (System.IO.File.Exists(imagePath))
        {
            System.IO.File.Delete(imagePath);
        }
        _context.Resources.Remove(podcast);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/Podcast/cover/{name}
    [HttpGet("cover/{name}")]
    public async Task<IActionResult> GetCoverByName(string name)
    {
        // Find podcast by name (case-insensitive)
        var podcast = await _context.Resources
            .FirstOrDefaultAsync(r => r.Type == Type.Podcast &&
                                    r.Name.ToLower() == name.ToLower());

        if (podcast == null)
        {
            // Return fallback image if podcast not found
            var fallbackPath = Path.Combine(_environment.WebRootPath, "uploads", "fallback-podcast.jpg");
            if (System.IO.File.Exists(fallbackPath))
            {
                var fallbackBytes = await System.IO.File.ReadAllBytesAsync(fallbackPath);
                return File(fallbackBytes, "image/jpeg");
            }
            return NotFound("Podcast not found and no fallback image available");
        }

        // Try to serve the podcast's cover image
        var imagePath = Path.Combine(_environment.WebRootPath, "uploads", podcast.FilePathUuid);
        if (System.IO.File.Exists(imagePath))
        {
            if (!_contentTypeProvider.TryGetContentType(imagePath, out string? contentType))
            {
                contentType = "application/octet-stream";
            }

            var imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);
            return File(imageBytes, contentType);
        }

        // Return fallback image if podcast's image not found
        var fallbackImagePath = Path.Combine(_environment.WebRootPath, "uploads", "fallback-podcast.jpg");
        if (System.IO.File.Exists(fallbackImagePath))
        {
            var fallbackImageBytes = await System.IO.File.ReadAllBytesAsync(fallbackImagePath);
            return File(fallbackImageBytes, "image/jpeg");
        }

        return NotFound("Podcast image not found and no fallback image available");
    }

    private bool PodcastExists(int id)
    {
        return _context.Resources.Any(e => e.Id == id && e.Type == Type.Podcast);
    }

    private static PodcastDto MapToDto(Resource podcast, string? imageData = null)
    {
        return new PodcastDto
        {
            Id = podcast.Id,
            Name = podcast.Name,
            Author = podcast.Author,
            BookUrl = podcast.ContentUrl,
            Summary = podcast.Summary,
            ImageUrl = $"/api/Podcast/image/{podcast.FilePathUuid}",
            FilePathUuid = podcast.FilePathUuid,
            ImageData = imageData
        };
    }

    private static PodcastListItemDto MapToListItemDto(Resource podcast)
    {
        return new PodcastListItemDto
        {
            Id = podcast.Id,
            Name = podcast.Name,
            Author = podcast.Author,
            BookUrl = podcast.ContentUrl,
            Summary = podcast.Summary,
            ImageUrl = $"/api/Podcast/image/{podcast.FilePathUuid}",
            FilePathUuid = podcast.FilePathUuid
        };
    }

    private static bool IsValidImageExtension(string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => true,
            _ => false
        };
    }

    // private static TagDto MapToTagDto(Tag tag)
    // {
    //     return new TagDto
    //     {
    //         Id = tag.Id,
    //         Name = tag.Name
    //     };
    // }

    // private static TagListItemDto MapToTagListItemDto(Tag tag)
    // {
    //     return new TagListItemDto
    //     {
    //         Id = tag.Id,
    //         Name = tag.Name
    //     };
    // }
}