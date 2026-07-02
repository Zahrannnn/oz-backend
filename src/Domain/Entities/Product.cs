namespace Oz.Domain.Entities;

public enum Gender : byte
{
    Boys = 1,
    Girls = 2,
    Unisex = 3
}

public class Product
{
    public long Id { get; set; }
    public long SchoolId { get; set; }
    public long GradeStageId { get; set; }
    public long ItemTypeId { get; set; }
    public Gender Gender { get; set; }
    public string? Color { get; set; }
    public bool IsInSet { get; set; } = false;
    public bool IsArchived { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public School School { get; set; } = null!;
    public GradeStage GradeStage { get; set; } = null!;
    public ItemType ItemType { get; set; } = null!;
    public ICollection<Variant> Variants { get; set; } = new List<Variant>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
