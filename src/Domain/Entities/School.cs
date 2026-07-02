namespace Oz.Domain.Entities;

public enum SchoolType : byte
{
    Arabic = 1,
    Experimental = 2,
    AzharEldelta = 3,
    ElHoda = 4,
    ElTegara = 5,
    Custom = 6
}

public class School
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SchoolType Type { get; set; }
    public bool IsArchived { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<GradeStage> GradeStages { get; set; } = new List<GradeStage>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
