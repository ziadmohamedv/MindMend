using Mind_Mend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mind_Mend.DTOs;
using Microsoft.AspNetCore.StaticFiles;

namespace Mind_Mend.Controllers;

[Route("api/[controller]")]
[ApiController]
// [Authorize(AuthenticationSchemes = "Bearer", Roles = "Doctor,Therapist")]
public class ResourceController(MindMendDbContext context, IWebHostEnvironment environment, IContentTypeProvider contentTypeProvider) : ControllerBase
{
    private readonly MindMendDbContext _context = context;
    private readonly IWebHostEnvironment _environment = environment;
    private readonly IContentTypeProvider _contentTypeProvider = contentTypeProvider;

    // GET: api/Resource
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ResourceListItemDto>>> GetResources()
    {
        var resources = await _context.Resources.ToListAsync();
        return resources.Select(MapToListItemDto).ToList();
    }

    // GET: api/Resource/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ResourceDto>> GetResource(int id)
    {
        var resource = await _context.Resources.FirstOrDefaultAsync(r => r.Id == id);

        if (resource == null)
        {
            return NotFound();
        }

        return MapToDto(resource);
    }

    // GET: api/Resource/image/{uuid}
    [HttpGet("image/{uuid}")]
    public async Task<IActionResult> GetImage(string uuid)
    {
        var resource = await _context.Resources
            .FirstOrDefaultAsync(r => r.FilePathUuid == uuid);

        if (resource == null)
        {
            return NotFound();
        }

        var imagePath = Path.Combine(_environment.WebRootPath, "uploads", resource.FilePathUuid);
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

    // POST: api/Resource
    [HttpPost]
    public async Task<ActionResult<ResourceDto>> CreateResource([FromForm] CreateResourceDto createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var uuid = Guid.NewGuid().ToString();
        var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);

        var filePath = Path.Combine(uploadsDir, uuid);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await createDto.Image.CopyToAsync(stream);
        }

        var resource = new Resource
        {
            Name = createDto.Name,
            Author = createDto.Author,
            ContentUrl = createDto.ContentUrl,
            Summary = createDto.Summary,
            FilePathUuid = uuid,
            Type = createDto.Type
        };

        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetResource), new { id = resource.Id }, MapToDto(resource));
    }

    // PUT: api/Resource/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateResource(int id, UpdateResourceDto updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var resource = await _context.Resources.FirstOrDefaultAsync(r => r.Id == id);

        if (resource == null)
        {
            return NotFound();
        }

        if (updateDto.Name != null)
        {
            resource.Name = updateDto.Name;
        }
        if (updateDto.Author != null)
        {
            resource.Author = updateDto.Author;
        }
        if (updateDto.ContentUrl != null)
        {
            resource.ContentUrl = updateDto.ContentUrl;
        }
        if (updateDto.Summary != null)
        {
            resource.Summary = updateDto.Summary;
        }
        if (updateDto.FilePathUuid != null)
        {
            resource.FilePathUuid = updateDto.FilePathUuid;
        }
        if (updateDto.Type.HasValue)
        {
            resource.Type = updateDto.Type.Value;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ResourceExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    // DELETE: api/Resource/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteResource(int id)
    {
        var resource = await _context.Resources.FindAsync(id);
        if (resource == null)
        {
            return NotFound();
        }

        // Delete the image file if it exists
        var imagePath = Path.Combine(_environment.WebRootPath, "uploads", resource.FilePathUuid);
        if (System.IO.File.Exists(imagePath))
        {
            System.IO.File.Delete(imagePath);
        }

        _context.Resources.Remove(resource);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ResourceExists(int id)
    {
        return _context.Resources.Any(e => e.Id == id);
    }

    private static ResourceDto MapToDto(Resource resource)
    {
        return new ResourceDto
        {
            Id = resource.Id,
            Name = resource.Name,
            Author = resource.Author,
            ContentUrl = resource.ContentUrl,
            Summary = resource.Summary,
            ImageUrl = $"/api/Resource/image/{resource.FilePathUuid}",
            FilePathUuid = resource.FilePathUuid,
            Type = resource.Type
        };
    }

    private static ResourceListItemDto MapToListItemDto(Resource resource)
    {
        return new ResourceListItemDto
        {
            Id = resource.Id,
            Name = resource.Name,
            Author = resource.Author,
            ContentUrl = resource.ContentUrl,
            Summary = resource.Summary,
            ImageUrl = $"/api/Resource/image/{resource.FilePathUuid}",
            FilePathUuid = resource.FilePathUuid,
            Type = resource.Type
        };
    }
}