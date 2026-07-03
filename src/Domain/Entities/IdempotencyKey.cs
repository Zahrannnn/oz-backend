namespace Oz.Domain.Entities;

public class IdempotencyKey
{
    public long Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? RequestHash { get; set; }
    public int ResponseStatus { get; set; }
    public string? ResponseBody { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
