namespace Oz.Domain.Entities;

public class PasswordRecovery
{
    public Guid Id { get; set; }
    public Guid AdminId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; }
    public int Attempts { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Admin Admin { get; set; } = null!;
}
