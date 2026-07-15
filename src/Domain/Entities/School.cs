namespace Oz.Domain.Entities;

/// <summary>
/// Canonical school types — unified across admin, storefront, and DB.
/// Arabic labels: حكومي، تجريبي، عربي، لغات، دولي، خاص
/// </summary>
public enum SchoolType : byte
{
    Governmental = 1,
    Experimental = 2,
    Arabic = 3,
    Language = 4,
    International = 5,
    Private = 6
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
