using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Mind_Mend.DTOs;

public class BookDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [Required]
    [StringLength(100)]
    public required string Author { get; set; }

    [Required]
    [Url]
    [StringLength(500)]
    public required string BookUrl { get; set; }

    [Required]
    [StringLength(500)]
    public required string Summary { get; set; }

    [Required]
    public required string ImageUrl { get; set; }

    [Required]
    public required string FilePathUuid { get; set; }

    public string? ImageData { get; set; }  // Base64 encoded image data

    // public List<TagDto> Tags { get; set; } = new List<TagDto>();
}

public class CreateBookDto
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [Required]
    [StringLength(100)]
    public required string Author { get; set; }

    [Required]
    [Url]
    [StringLength(500)]
    public required string BookUrl { get; set; }

    [Required]
    public required IFormFile Image { get; set; }

    [Required]
    [StringLength(500)]
    public required string Summary { get; set; }

    // public List<int> TagIds { get; set; } = new List<int>();
}

public class UpdateBookDto
{
    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(100)]
    public string? Author { get; set; }

    [Url]
    [StringLength(500)]
    public string? BookUrl { get; set; }

    [StringLength(500)]
    public string? Summary { get; set; }

    public string? FilePathUuid { get; set; }

    // public List<int>? TagIds { get; set; }
}

public class BookListItemDto
{
    public int Id { get; set; }
    [Required]
    public required string Name { get; set; }
    [Required]
    public required string Author { get; set; }
    [Required]
    public required string BookUrl { get; set; }
    [Required]
    public required string Summary { get; set; }
    [Required]
    public required string ImageUrl { get; set; }
    [Required]
    public required string FilePathUuid { get; set; }
    // public List<TagListItemDto> Tags { get; set; } = new List<TagListItemDto>();
}