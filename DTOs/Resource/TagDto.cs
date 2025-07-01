using System.ComponentModel.DataAnnotations;

namespace Mind_Mend.DTOs;

// Used for GET responses when tag is the main entity
public class TagDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public required string Name { get; set; }
}

// Used for returning tag details in lists or nested objects
public class TagListItemDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

// Used for creating new tags
public class CreateTagDto
{
    [Required]
    [StringLength(50)]
    public required string Name { get; set; }
}

// Used for updating existing tags
public class UpdateTagDto
{
    [Required]
    [StringLength(50)]
    public required string Name { get; set; }
}