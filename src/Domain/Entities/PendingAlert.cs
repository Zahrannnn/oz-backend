namespace Oz.Domain.Entities;

public class PendingAlert
{
    public long Id { get; set; }
    public long VariantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string EmailHash { get; set; } = string.Empty;
    public bool Notified { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? NotifiedAt { get; set; }

    public Variant Variant { get; set; } = null!;
}
