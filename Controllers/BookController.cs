using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mind_Mend.Data;
using Mind_Mend.DTOs;

namespace Mind_Mend.Controllers;

[Route("api/[controller]")]
[ApiController]
// [Authorize(AuthenticationSchemes = "Bearer", Roles = "Doctor,Therapist")]
public class BookController(MindMendDbContext context, IWebHostEnvironment environment) : ControllerBase
{
    private readonly MindMendDbContext _context = context;
    private readonly IWebHostEnvironment _environment = environment;

    // GET: api/Book
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookListItemDto>>> GetBooks()
    {
        var books = await _context.Resources
            .Where(r => r.Type == Type.Book)
            // .Include(r => r.Tags)
            .ToListAsync();

        return books.Select(MapToListItemDto).ToList();
    }

    // GET: api/Book/5
    [HttpGet("{id}")]
    public async Task<ActionResult<BookDto>> GetBook(int id)
    {
        var book = await _context.Resources
            // .Include(r => r.Tags)
            .FirstOrDefaultAsync(r => r.Id == id && r.Type == Type.Book);

        if (book == null)
        {
            return NotFound();
        }

        var imagePath = Path.Combine(_environment.WebRootPath, "uploads", book.FilePathUuid);
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

        return MapToDto(book, imageData);
    }

    // GET: api/Book/image/{uuid}
    [HttpGet("image/{uuid}")]
    public async Task<IActionResult> GetImage(string uuid)
    {
        var book = await _context.Resources
            .FirstOrDefaultAsync(r => r.FilePathUuid == uuid && r.Type == Type.Book);

        if (book == null)
        {
            return NotFound();
        }

        var imagePath = Path.Combine(_environment.WebRootPath, "uploads", book.FilePathUuid);
        if (!System.IO.File.Exists(imagePath))
        {
            return NotFound();
        }

        // Get the file extension and determine the content type
        string extension = Path.GetExtension(imagePath).ToLowerInvariant();
        string contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream" // fallback for unknown types
        };

        var imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);
        return File(imageBytes, contentType);
    }

    // POST: api/Book
    [HttpPost]
    public async Task<ActionResult<BookDto>> CreateBook([FromForm] CreateBookDto createDto)
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

        var book = new Resource
        {
            Name = createDto.Name,
            Author = createDto.Author,
            ContentUrl = createDto.BookUrl,
            Summary = createDto.Summary,
            FilePathUuid = uuid,  // This now includes the file extension
            Type = Type.Book
        };

        _context.Resources.Add(book);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBook), new { id = book.Id }, MapToDto(book));
    }

    private static bool IsValidImageExtension(string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => true,
            _ => false
        };
    }

    // PUT: api/Book/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBook(int id, UpdateBookDto updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var book = await _context.Resources
            .FirstOrDefaultAsync(r => r.Id == id && r.Type == Type.Book);

        if (book == null)
        {
            return NotFound();
        }

        if (updateDto.Name != null)
        {
            book.Name = updateDto.Name;
        }
        if (updateDto.Author != null)
        {
            book.Author = updateDto.Author;
        }
        if (updateDto.BookUrl != null)
        {
            book.ContentUrl = updateDto.BookUrl;
        }
        if (updateDto.Summary != null)
        {
            book.Summary = updateDto.Summary;
        }
        if (updateDto.FilePathUuid != null)
        {
            book.FilePathUuid = updateDto.FilePathUuid;
        }

        // if (updateDto.TagIds != null)
        // {
        //     var tags = await _context.Tags
        //         .Where(t => updateDto.TagIds.Contains(t.Id))
        //         .ToListAsync();
        //     book.Tags.Clear();
        //     book.Tags.AddRange(tags);
        // }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!BookExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    // DELETE: api/Book/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBook(int id)
    {
        var book = await _context.Resources
            .FirstOrDefaultAsync(r => r.Id == id && r.Type == Type.Book);

        if (book == null)
        {
            return NotFound();
        }

        _context.Resources.Remove(book);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool BookExists(int id)
    {
        return _context.Resources.Any(e => e.Id == id && e.Type == Type.Book);
    }

    private static BookDto MapToDto(Resource book, string? imageData = null)
    {
        return new BookDto
        {
            Id = book.Id,
            Name = book.Name,
            Author = book.Author,
            BookUrl = book.ContentUrl,
            Summary = book.Summary,
            ImageUrl = $"/api/Book/image/{book.FilePathUuid}",
            FilePathUuid = book.FilePathUuid,
            ImageData = imageData
        };
    }

    private static BookListItemDto MapToListItemDto(Resource book)
    {
        return new BookListItemDto
        {
            Id = book.Id,
            Name = book.Name,
            Author = book.Author,
            BookUrl = book.ContentUrl,
            Summary = book.Summary,
            ImageUrl = $"/api/Book/image/{book.FilePathUuid}",
            FilePathUuid = book.FilePathUuid
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

    // GET: api/Book/cover/{title}
    [HttpGet("cover/{title}")]
    public async Task<IActionResult> GetCoverByTitle(string title)
    {
        try
        {
            // Sanitize the title
            var sanitizedTitle = SanitizeFilename(title);

            // First, try to find the image in covers directory
            var coversPath = Path.Combine(_environment.ContentRootPath, "covers", $"{sanitizedTitle}.jpg");

            if (System.IO.File.Exists(coversPath))
            {
                var imageBytes = await System.IO.File.ReadAllBytesAsync(coversPath);
                return File(imageBytes, "image/jpeg");
            }

            // If not found, try fallback image
            var fallbackPath = Path.Combine(_environment.WebRootPath, "uploads", "fallback-book-cover.jpg");
            if (System.IO.File.Exists(fallbackPath))
            {
                var fallbackBytes = await System.IO.File.ReadAllBytesAsync(fallbackPath);
                return File(fallbackBytes, "image/jpeg");
            }

            return NotFound("Cover image not found");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving cover: {ex.Message}");
        }
    }

    private static string SanitizeFilename(string filename)
    {
        var invalidChars = "<>:\"/\\|?*";
        foreach (var invalidChar in invalidChars)
        {
            filename = filename.Replace(invalidChar, '_');
        }
        return filename.Length > 100 ? filename[..100] : filename;
    }
}