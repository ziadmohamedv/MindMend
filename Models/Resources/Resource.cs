using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Resource
{
    public int Id { get; set; }
    [Required]
    public required string Name { get; set; }
    [Required]
    public required string Author { get; set; }
    [Required]
    public required string ContentUrl { get; set; }
    [Required]
    public required string Summary { get; set; }
    [Required]
    public required string FilePathUuid { get; set; }
    [Required]
    public Type Type { get; set; }
    // [Required]
    // public Type Type { get; set; }
    // public virtual List<Tag> Tags { get; set; } = new List<Tag>();
}

public enum Type
{
    Book,
    Podcast
}