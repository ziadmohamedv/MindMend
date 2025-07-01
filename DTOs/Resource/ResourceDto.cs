using System.ComponentModel.DataAnnotations;

namespace Mind_Mend.DTOs;

// Used for GET responses
public class ResourceDto
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
    public required string ContentUrl { get; set; }  // URL to the content (book or podcast)

    [Required]
    [StringLength(500)]
    public required string Summary { get; set; }

    [Required]
    public required string ImageUrl { get; set; }  // URL to the cover image

    [Required]
    public required string FilePathUuid { get; set; }

    [Required]
    public required Type Type { get; set; }

    // public List<TagDto> Tags { get; set; } = new List<TagDto>();
}

// Used for creating new resources
public class CreateResourceDto
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
    public required string ContentUrl { get; set; }  // URL to the content (book or podcast)

    [Required]
    public required IFormFile Image { get; set; }

    [Required]
    [StringLength(500)]
    public required string Summary { get; set; }

    [Required]
    public required Type Type { get; set; }

    // FilePathUuid is generated in the controller
    // public List<int> TagIds { get; set; } = new List<int>();
}

// Used for updating existing resources
public class UpdateResourceDto
{
    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(100)]
    public string? Author { get; set; }

    [Url]
    [StringLength(500)]
    public string? ContentUrl { get; set; }  // URL to the content (book or podcast)

    [StringLength(500)]
    public string? Summary { get; set; }

    public string? FilePathUuid { get; set; }

    public Type? Type { get; set; }

    // public List<int>? TagIds { get; set; }
}

// Used for returning resource details in lists or nested objects
public class ResourceListItemDto
{
    public int Id { get; set; }
    [Required]
    public required string Name { get; set; }
    [Required]
    public required string Author { get; set; }
    [Required]
    public required string ContentUrl { get; set; }  // URL to the content (book or podcast)
    [Required]
    public required string Summary { get; set; }
    [Required]
    public required string ImageUrl { get; set; }  // URL to the cover image
    [Required]
    public required string FilePathUuid { get; set; }
    [Required]
    public required Type Type { get; set; }
    // public List<TagListItemDto> Tags { get; set; } = new List<TagListItemDto>();
}