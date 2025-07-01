using System.ComponentModel.DataAnnotations;

public class Tag
{
    [Key]
    public int Id { get; set; }
    public required string Name { get; set; }
    // public virtual List<Resource> Resources { get; set; } = new List<Resource>();
}
