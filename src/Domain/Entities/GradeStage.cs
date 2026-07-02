namespace Oz.Domain.Entities;

public class GradeStage
{
    public long Id { get; set; }
    public long SchoolId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }

    public School School { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
