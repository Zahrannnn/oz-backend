namespace Oz.Domain.Entities;

public enum EmailStatus : byte
{
    Pending = 0,
    Success = 1,
    Failed = 2
}

public class EmailLog
{
    public long Id { get; set; }
    public long? OrderId { get; set; }
    public long? VariantId { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public EmailStatus Status { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; }
}
